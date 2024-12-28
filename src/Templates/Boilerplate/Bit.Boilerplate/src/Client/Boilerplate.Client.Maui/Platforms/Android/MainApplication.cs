//-:cnd:noEmit
using Android.App;
using Android.Runtime;

[assembly: UsesPermission(Android.Manifest.Permission.Internet)]
[assembly: UsesPermission(Android.Manifest.Permission.AccessNetworkState)]

//+:cnd:noEmit
//#if (notification == true)
// https://github.com/thudugala/Plugin.LocalNotification/wiki/1.-Usage-10.0.0--.Net-MAUI#android-specific-setup
[assembly: UsesPermission(Android.Manifest.Permission.PostNotifications)]
[assembly: UsesPermission(Android.Manifest.Permission.Vibrate)]
[assembly: UsesPermission(Android.Manifest.Permission.WakeLock)]
[assembly: UsesPermission(Android.Manifest.Permission.ReceiveBootCompleted)]
//#endif
//-:cnd:noEmit

namespace Boilerplate.Client.Maui.Platforms.Android;

[Application(
#if Development
    UsesCleartextTraffic = true,
#endif
    AllowBackup = true,
    SupportsRtl = true
)]
public partial class MainApplication(IntPtr handle, JniHandleOwnership ownership) 
    : MauiApplication(handle, ownership)
{
    protected override MauiApp CreateMauiApp() => MauiProgram
        .CreateMauiApp();
}
