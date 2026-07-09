# OpenAI Chat Completions Example

This example demonstrates how to use the LaunchDarkly AI SDK for .NET with the [OpenAI Chat Completions API](https://platform.openai.com/docs/api-reference/chat).

It also wires up the LaunchDarkly [observability plugin](https://launchdarkly.com/docs/sdk/observability/dotnet). The example sets the `OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY` environment variable to `true` so the OpenAI SDK emits OpenTelemetry spans and metrics (under the `OpenAI.ChatClient` source and meter), and registers those with the plugin so the model call's traces and token-usage metrics are exported to LaunchDarkly alongside the AI Config tracker events.

## Prerequisites

- .NET 8.0 SDK
- A LaunchDarkly account and a server-side SDK key
- An [OpenAI API key](https://platform.openai.com/api-keys)

## Setup

1. [Create an AI Config](https://launchdarkly.com/docs/home/ai-configs/create) in LaunchDarkly with the key `sample-completion`. Select OpenAI as the provider, a model such as `gpt-4`, and add a system message.
2. Copy `.env.example` to `.env` and fill in your keys:
   ```sh
   cp .env.example .env
   ```
3. Restore dependencies:
   ```sh
   dotnet restore
   ```

## Run

```sh
dotnet run
```
