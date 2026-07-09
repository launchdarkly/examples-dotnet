using DotNetEnv;
using LaunchDarkly.Observability;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Ai;
using LaunchDarkly.Sdk.Server.Ai.Adapters;
using LaunchDarkly.Sdk.Server.Ai.Config;
using LaunchDarkly.Sdk.Server.Ai.Tracking;
using LaunchDarkly.Sdk.Server.Integrations;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenAI.Chat;

Env.TraversePath().Load();

// Enable the OpenAI SDK's experimental OpenTelemetry instrumentation so its chat
// completions emit spans and metrics. This must be set before the ChatClient is
// used; the observability plugin configured below exports the telemetry to
// LaunchDarkly.
Environment.SetEnvironmentVariable("OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY", "true");

var sdkKey = Environment.GetEnvironmentVariable("LAUNCHDARKLY_SDK_KEY");
if (string.IsNullOrEmpty(sdkKey))
{
    Console.Error.WriteLine(
        "LaunchDarkly SDK key is required: set the LAUNCHDARKLY_SDK_KEY environment variable and try again.");
    return;
}

var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(openAiKey))
{
    Console.Error.WriteLine(
        "OpenAI API key is required: set the OPENAI_API_KEY environment variable and try again.");
    return;
}

// Set completionKey to the AI config key you want to evaluate.
var completionKey = Environment.GetEnvironmentVariable("LAUNCHDARKLY_COMPLETION_KEY")
    ?? "sample-completion";

// The observability plugin registers OpenTelemetry into a dependency-injection
// service collection and relies on the .NET generic host to run the exporters.
var hostBuilder = Host.CreateApplicationBuilder(args);

var ldClient = new LdClient(Configuration.Builder(sdkKey)
    .Plugins(new PluginConfigurationBuilder()
        .Add(ObservabilityPlugin.Builder(hostBuilder.Services)
            .WithServiceName("openai-chat-completions")
            .WithServiceVersion("1.0.0")
            // The OpenAI SDK emits telemetry under the "OpenAI.ChatClient" activity
            // source and meter. Add them so the plugin exports the model call's
            // spans and token-usage metrics alongside the AI Config tracker events.
            .WithExtendedTracingConfig(tracing => tracing.AddSource("OpenAI.ChatClient"))
            .WithExtendedMeterConfiguration(metrics => metrics.AddMeter("OpenAI.ChatClient"))
            .Build()))
    .Build());
if (!ldClient.Initialized)
{
    Console.Error.WriteLine(
        "*** SDK failed to initialize. Please check your internet connection and SDK credential for any typo.");
    ldClient.Dispose();
    return;
}
Console.WriteLine("*** SDK successfully initialized!");

// Building the LdClient registered the plugin's OpenTelemetry services on the host,
// so the host must be built after the client. Starting it boots the exporters.
using var host = hostBuilder.Build();
await host.StartAsync();

var aiClient = new LdAiClient(new LdClientAdapter(ldClient));

// Set up the evaluation context. This context should appear on your
// LaunchDarkly contexts dashboard soon after you run the demo.
var context = Context.Builder(ContextKind.Of("user"), "example-user-key")
    .Name("Sandy")
    .Build();

try
{
    // Pass a defaultValue for improved resiliency when the AI config is
    // unavailable or LaunchDarkly is unreachable; omit for a disabled default.
    // Example:
    //   var defaultValue = LdAiCompletionConfigDefault.New()
    //       .Enable()
    //       .SetModelName("gpt-4")
    //       .SetModelProviderName("openai")
    //       .AddMessage("You are a helpful assistant.", LdAiConfigTypes.Role.System)
    //       .Build();
    var config = aiClient.CompletionConfig(
        completionKey,
        context,
        variables: new Dictionary<string, object>
        {
            ["myUserVariable"] = "Testing Variable"
        });

    if (!config.Enabled)
    {
        Console.WriteLine(
            $"AI config '{completionKey}' is disabled. Verify the config key exists in " +
            "your LaunchDarkly project and is not targeting a disabled variation.");
        return;
    }

    var tracker = config.CreateTracker();

    var modelName = string.IsNullOrEmpty(config.Model.Name) ? "gpt-4" : config.Model.Name;
    var chatClient = new ChatClient(modelName, openAiKey);

    var sampleQuestion = "What can you help me with?";
    var messages = config.Messages
        .Select(ToOpenAiMessage)
        .Append(new UserChatMessage(sampleQuestion))
        .ToList();

    Console.WriteLine();
    Console.WriteLine($"Sending sample question to {modelName}: \"{sampleQuestion}\"");
    Console.WriteLine("Waiting for response...");

    var completion = await tracker.TrackMetricsOf(
        result => new AiMetrics(
            success: true,
            tokens: new Usage(
                Total: result.Value.Usage.TotalTokenCount,
                Input: result.Value.Usage.InputTokenCount,
                Output: result.Value.Usage.OutputTokenCount)),
        async () => await chatClient.CompleteChatAsync(messages));

    var aiResponse = completion.Value.Content[0].Text;
    Console.WriteLine();
    Console.WriteLine("Model response:");
    Console.WriteLine(aiResponse);

    PrintSummary(tracker.Summary);
}
catch (Exception ex)
{
    // In production, sanitize before logging — provider errors may include credentials.
    Console.Error.WriteLine($"Error: {ex.Message}");
}
finally
{
    ldClient.FlushAndWait(TimeSpan.FromSeconds(5));
    // Stop the host so the OpenTelemetry exporters flush any buffered spans and
    // metrics to LaunchDarkly before the process exits.
    await host.StopAsync();
    ldClient.Dispose();
}

static ChatMessage ToOpenAiMessage(LdAiConfigTypes.Message m) => m.Role switch
{
    LdAiConfigTypes.Role.System => new SystemChatMessage(m.Content),
    LdAiConfigTypes.Role.Assistant => new AssistantChatMessage(m.Content),
    LdAiConfigTypes.Role.User => new UserChatMessage(m.Content),
    _ => new UserChatMessage(m.Content)
};

static void PrintSummary(MetricSummary summary)
{
    Console.WriteLine();
    Console.WriteLine("Done! The AI config was evaluated and the following metrics were tracked:");
    Console.WriteLine($"  Duration:      {summary.DurationMs}ms");
    Console.WriteLine($"  Success:       {summary.Success}");
    if (summary.Tokens is { } tokens)
    {
        Console.WriteLine($"  Input tokens:  {tokens.Input}");
        Console.WriteLine($"  Output tokens: {tokens.Output}");
        Console.WriteLine($"  Total tokens:  {tokens.Total}");
    }
}
