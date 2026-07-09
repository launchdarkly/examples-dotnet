# Observability Plugin

This example builds on the [Hello .NET getting-started example](../../../getting-started/) and adds the LaunchDarkly [observability plugin](https://launchdarkly.com/docs/sdk/observability/dotnet). The only change to the flag-evaluation code is registering the plugin on the SDK configuration; everything else demonstrates how to record custom telemetry from a console application.

It demonstrates:

- Registering `ObservabilityPlugin` on the SDK configuration with `PluginConfigurationBuilder`
- Hosting the plugin's OpenTelemetry exporters in a console app with `Host.CreateApplicationBuilder`
- Creating spans manually with `Observe.StartActivity`
- Recording a custom metric with `Observe.RecordIncr`
- Emitting a structured log with `Observe.RecordLog`

## Spans in a console application

Framework integrations (for example ASP.NET Core) create spans automatically for
incoming requests. A console application has no framework generating requests, so
**nothing produces spans unless you create them yourself** with `Observe.StartActivity`.
This example wraps the flag evaluation, and each flag change, in a manually created
span so the custom metrics and logs recorded inside them are grouped under a trace in
LaunchDarkly.

The plugin also relies on the .NET generic host to run its OpenTelemetry exporters.
This console app creates one with `Host.CreateApplicationBuilder` and calls `host.Start()`
after the `LdClient` is built (constructing the client registers the exporter services on
the host). Because the app then stays running, the exporters flush telemetry on their
normal schedule.

## Prerequisites

- .NET 8.0 SDK
- A LaunchDarkly account and a server-side SDK key
- [Observability enabled](https://launchdarkly.com/docs/sdk/observability/dotnet) for your LaunchDarkly project

## Build instructions

1. Set the environment variable `LAUNCHDARKLY_SDK_KEY` to your LaunchDarkly SDK key. If there is an existing boolean feature flag in your LaunchDarkly project that you want to evaluate, set `LAUNCHDARKLY_FLAG_KEY` to the flag key; otherwise, a boolean flag of `sample-feature` will be assumed.

    ```bash
    export LAUNCHDARKLY_SDK_KEY="1234567890abcdef"
    export LAUNCHDARKLY_FLAG_KEY="my-boolean-flag"
    ```

    By default the plugin exports telemetry to LaunchDarkly. To send it elsewhere (for example on networks that only allow port 443), set `OTEL_EXPORTER_OTLP_ENDPOINT`.

2. Run the application from the command line:

    ```bash
    dotnet run
    ```

You should receive the message "The <flagKey> feature flag evaluates to <flagValue>." The application runs continuously and reacts to flag changes in LaunchDarkly, recording a span, a metric, and a log for each evaluation. The telemetry appears on your LaunchDarkly observability dashboard shortly afterward.

> **Note:** Traces and logs are exported within a few seconds, but metrics are collected by a periodic reader and export on a longer interval (roughly a minute), so they take longer to appear. Keep the application running long enough for the export to occur.
