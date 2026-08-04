# LaunchDarkly sample .NET client-side application

We've built a simple console application that demonstrates how LaunchDarkly's client-side .NET SDK works.

## Build instructions

1. Set the environment variable `LAUNCHDARKLY_MOBILE_KEY` to your LaunchDarkly mobile key. If there is an existing boolean feature flag in your LaunchDarkly project that you want to evaluate, set `LAUNCHDARKLY_FLAG_KEY` to the flag key; otherwise, a boolean flag of `sample-feature` will be assumed.

    ```bash
    export LAUNCHDARKLY_MOBILE_KEY="my-mobile-key"
    export LAUNCHDARKLY_FLAG_KEY="my-boolean-flag"
    ```

    Alternatively, you can set the `mobileKey` and `flagKey` constants directly in `Program.cs`.

2. Run the application from the command line:

    ```bash
    dotnet run
    ```

You should receive the message "The <flagKey> feature flag evaluates to <flagValue>.". The application will run continuously and react to flag changes in LaunchDarkly.
