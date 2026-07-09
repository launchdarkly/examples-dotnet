using System;
using System.Collections.Generic;
using LaunchDarkly.Observability;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Integrations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HelloDotNet
{
    class Hello
    {
        public static void ShowBanner(){
            Console.WriteLine(
@"        ██
          ██
      ████████
         ███████
██ LAUNCHDARKLY █
         ███████
      ████████
          ██
        ██
");
        }

        static void Main(string[] args)
        {
            bool CI = Environment.GetEnvironmentVariable("CI") != null;

            string SdkKey = Environment.GetEnvironmentVariable("LAUNCHDARKLY_SDK_KEY");

            // Set FeatureFlagKey to the feature flag key you want to evaluate.
            string FeatureFlagKey = "sample-feature";

            if (string.IsNullOrEmpty(SdkKey))
            {
                Console.WriteLine("*** Please set LAUNCHDARKLY_SDK_KEY environment variable to your LaunchDarkly SDK key first\n");
                Environment.Exit(1);
            }

            // The observability plugin registers OpenTelemetry into a dependency-injection
            // service collection and relies on the .NET generic host to run the exporters.
            // A console application creates that host explicitly; an ASP.NET Core app would
            // pass its existing builder.Services instead.
            var hostBuilder = Host.CreateApplicationBuilder(args);

            // Compared to the getting-started example, the only configuration change is
            // adding the observability plugin. By default it exports telemetry to
            // LaunchDarkly; override the endpoint with the OTEL_EXPORTER_OTLP_ENDPOINT
            // environment variable for networks that only allow port 443.
            var ldConfig = Configuration.Builder(SdkKey)
                .Plugins(new PluginConfigurationBuilder()
                    .Add(ObservabilityPlugin.Builder(hostBuilder.Services)
                        .WithServiceName("hello-dotnet-observability")
                        .WithServiceVersion("1.0.0")
                        .Build()))
                .Build();

            var client = new LdClient(ldConfig);

            if (client.Initialized)
            {
                Console.WriteLine("*** SDK successfully initialized!\n");
            }
            else
            {
                Console.WriteLine("*** SDK failed to initialize\n");
                Environment.Exit(1);
            }

            // Building the LdClient above registered the plugin's OpenTelemetry services on
            // the host, so the host must be built after the client. Starting it boots the
            // exporters that ship telemetry to LaunchDarkly.
            var host = hostBuilder.Build();
            host.Start();

            // Set up the evaluation context. This context should appear on your LaunchDarkly contexts
            // dashboard soon after you run the demo.
            var context = Context.Builder("example-user-key")
                .Name("Sandy")
                .Build();

            if (Environment.GetEnvironmentVariable("LAUNCHDARKLY_FLAG_KEY") != null)
            {
                FeatureFlagKey = Environment.GetEnvironmentVariable("LAUNCHDARKLY_FLAG_KEY");
            }

            var flagAttributes = new Dictionary<string, object> { { "flag.key", FeatureFlagKey } };

            // A web framework would create spans automatically for incoming requests; a console
            // application has none, so we start one manually with Observe.StartActivity. The custom
            // metric and log recorded inside it are grouped under this trace in LaunchDarkly.
            using (Observe.StartActivity("evaluate-flag"))
            {
                var flagValue = client.BoolVariation(FeatureFlagKey, context, false);

                Console.WriteLine($"*** The {FeatureFlagKey} feature flag evaluates to {flagValue}.\n");

                Observe.RecordIncr("flag_evaluations", flagAttributes);
                Observe.RecordLog($"Evaluated {FeatureFlagKey}: {flagValue}", LogLevel.Information, flagAttributes);

                if (flagValue) ShowBanner();
            }

            client.FlagTracker.FlagChanged += client.FlagTracker.FlagValueChangeHandler(
                FeatureFlagKey,
                context,
                (sender, changeArgs) => {
                    // Record each change as its own span so the update is visible in LaunchDarkly.
                    using (Observe.StartActivity("flag-changed"))
                    {
                        Console.WriteLine($"*** The {FeatureFlagKey} feature flag evaluates to {changeArgs.NewValue}.\n");

                        Observe.RecordIncr("flag_changes", flagAttributes);

                        if (changeArgs.NewValue.AsBool) ShowBanner();
                    }
                }
            );

            if (CI)
            {
                // In CI just verify startup, then stop the host so any buffered
                // telemetry is flushed before exiting.
                host.StopAsync().GetAwaiter().GetResult();
                client.Dispose();
                return;
            }

            Console.WriteLine("*** Waiting for changes (press Ctrl+C to exit) \n");

            // Block until Ctrl+C. The host's console lifetime handles the signal,
            // stops the OpenTelemetry exporters (flushing buffered telemetry), and
            // then returns so the process exits cleanly. Blocking any other way
            // (for example on a Task that never completes) would leave the process
            // hung at "Application is shutting down..." because nothing would be
            // driving the host's shutdown.
            host.WaitForShutdown();

            client.Dispose();
        }
    }
}
