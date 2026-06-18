# LaunchDarkly sample OpenFeature .NET server application

We've built a simple console application that demonstrates how LaunchDarkly's OpenFeature server-side provider works.

## Build instructions

1. Set the environment variable `LAUNCHDARKLY_SDK_KEY` to your LaunchDarkly SDK key. If there is an existing boolean feature flag in your LaunchDarkly project that you want to evaluate, set `LAUNCHDARKLY_FLAG_KEY` to the flag key; otherwise, a boolean flag of `sample-feature` will be assumed.

    ```bash
    export LAUNCHDARKLY_SDK_KEY="1234567890abcdef"
    export LAUNCHDARKLY_FLAG_KEY="my-boolean-flag"
    ```

2. Run the application from the command line:

    ```bash
    dotnet run
    ```

You should see the message `"The <flagKey> feature flag evaluates to <true/false>"`.
