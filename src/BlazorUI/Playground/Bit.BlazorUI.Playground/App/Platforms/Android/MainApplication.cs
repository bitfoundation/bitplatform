using Android.App;
using Android.Runtime;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using System;

[assembly: UsesPermission(Android.Manifest.Permission.Internet)]

namespace Bit.BlazorUI.Playground.Web
{
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
            .CreateMauiAppBuilder()
            .Build();
    }
}
