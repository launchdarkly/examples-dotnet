# LaunchDarkly .NET Server AI SDK examples

Examples for the [`LaunchDarkly.ServerSdk.Ai`](https://www.nuget.org/packages/LaunchDarkly.ServerSdk.Ai) package.

For more comprehensive instructions, you can visit the [AI Configs Quickstart](https://docs.launchdarkly.com/home/ai-configs/quickstart) or the [.NET AI SDK reference guide](https://docs.launchdarkly.com/sdk/ai/dotnet).

Each example is a self-contained .NET console application you can run independently.

## Getting Started

These examples show how to integrate LaunchDarkly AI with different providers.

| Provider | Example                                                  | Description                                                       |
|----------|----------------------------------------------------------|------------------------------------------------------------------|
| OpenAI   | [Chat Completions](./frameworks/openai/chat-completions/) | `CompletionConfig` with OpenAI, automatic metrics tracking       |
| Bedrock  | [Converse](./frameworks/bedrock/converse/)               | `CompletionConfig` with the AWS Bedrock Converse API, metrics tracking |

## Features

These examples focus on the LaunchDarkly AI SDK itself. They are **provider-agnostic** — they retrieve and resolve AI Configs, then exercise the tracker API with synthetic operations rather than calling any model provider.

| Example                                              | Description                                                                              |
|------------------------------------------------------|-----------------------------------------------------------------------------------------|
| [Completion Config](./features/completion-config/)   | Default values, Mustache variables, model parameter extraction, tool enumeration, and `TrackMetricsOf` |
| [Agent Config](./features/agent-config/)             | Agent instructions and tools; `TrackMetricsOf` and `TrackToolCall`                       |
| [Judge Config](./features/judge-config/)             | Judge config retrieval and `TrackJudgeResult`                                            |
