using Android.App;
using Android.Content.PM;
using Android.OS;

[assembly: UsesPermission(Android.Manifest.Permission.AccessCoarseLocation)]
[assembly: UsesPermission(Android.Manifest.Permission.BluetoothScan)]

namespace DeviceBrowser;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnPostResume()
    {
        //ensure we have the nearby devices privileges
        if (CheckCallingOrSelfPermission(Android.Manifest.Permission.BluetoothScan) != Permission.Granted)
            RequestPermissions(new string[] { Android.Manifest.Permission.BluetoothScan }, 0);

        base.OnPostResume();

        // fast way to ensure we have the location privileges
        Geolocation.GetLastKnownLocationAsync();
    }
}
