# triaxis.BluetoothLE

Simple cross-platform Bluetooth LE library

## Basic Usage

1. reference the `triaxis.BluetoothLE` nuget from your MAUI or Xamarin application
1. make the `IBluetoothLE` available to the application

    MAUI: call the `IServiceCollection.AddBluetoothLE ()` extension method during application startup:

    ```csharp
        var builder = MauiApp.CreateBuilder();
        // register platform-specific IBluetoothLE implementation in the container
        builder.Services.AddBluetoothLE();
    ```

    Xamarin: you can use the the DependencyService to register the `triaxis.BluetoothLE.Platform` class as an `IBluetoothLE` implementation

1. observe the `IBluetoothLE.WhenAdapterChanges()` observable - it will always return at least the current adapter state
1. when the returned `IAdapter.State` == `AdapterState.On`, you can initiate a scan for advertisements using `IAdapter.Scan()` which returns another observable of `IAdvertisement`s
1. you can use the `IAdvertisement`s to either just monitor nearby devices through the data exposed by the interface, or use the `IAdvertisement.Peripheral` to connect to the device

## Platform-specific requirements

### Android

For Android applications to be able to access Bluetooth LE, at least one of the location permissions must be requested, and the application must also ask the system for location access.

A simple trick to handle the location request is to use the Xamarin/MAUI essentials to retrieve the current location in the `MainActivity`

```csharp
    protected override void OnPostResume()
    {
        base.OnPostResume();

        _ = Geolocation.GetLastKnownLocationAsync();
    }
```

### iOS, MacCatalyst

A reason string has to be provided in the corresponding `Info.plist` file, e.g.:

```xml
	<key>NSBluetoothAlwaysUsageDescription</key>
	<string>This app scans for Bluetooth devices</string>
```

## Example

See the [DeviceBrowser](./examples/maui/DeviceBrowser) example for a very simple MAUI application that just scans for advertising devices

## License

This package is licensed under the [MIT License](./LICENSE.txt)

Copyright &copy; 2022 triaxis s.r.o.
