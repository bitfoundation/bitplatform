﻿//-:cnd:noEmit
using Android.App;
using Android.Runtime;

[assembly: UsesPermission(Android.Manifest.Permission.Internet)]
[assembly: UsesPermission(Android.Manifest.Permission.AccessNetworkState)]

namespace Boilerplate.Client.App.Platforms.Android;

[Application(
#if DEBUG
    UsesCleartextTraffic = true
#endif
)]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram
        .CreateMauiApp();
}
