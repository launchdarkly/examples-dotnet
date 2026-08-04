# LaunchDarkly .NET MAUI example

This example demonstrates LaunchDarkly's client-side .NET SDK in a mobile context using [.NET MAUI](https://learn.microsoft.com/dotnet/maui/), targeting both Android and iOS from a single project.

## Build instructions

1. Install the MAUI workload if you haven't already:

    ```bash
    dotnet workload install maui
    ```

2. Copy the example settings file and set your mobile key:

    ```bash
    cp Resources/Raw/appsettings.example.json Resources/Raw/appsettings.json
    ```

    Then edit `Resources/Raw/appsettings.json` and set your mobile key (and, optionally, a flag key):

    ```json
    {
      "MobileKey": "my-mobile-key",
      "FlagKey": "sample-feature"
    }
    ```

    `appsettings.json` is gitignored so your key is never committed.

3. Build and run for a target platform, for example Android:

    ```bash
    dotnet build -f net8.0-android
    ```

The app displays the value of the feature flag and reacts to flag changes in LaunchDarkly.
