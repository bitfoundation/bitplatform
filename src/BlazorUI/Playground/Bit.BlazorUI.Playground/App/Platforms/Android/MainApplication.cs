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
        static MainApplication()
        {
            BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("CustomBlazorWebViewMapper", (handler, view) =>
            {
                Android.Webkit.WebSettings settings = handler.PlatformView.Settings;

                settings.AllowFileAccessFromFileURLs =
                    settings.AllowUniversalAccessFromFileURLs =
                    settings.AllowContentAccess =
                    settings.AllowFileAccess =
                    settings.DatabaseEnabled =
                    settings.JavaScriptCanOpenWindowsAutomatically =
                    settings.DomStorageEnabled = true;

                settings.BlockNetworkLoads =
                    settings.BlockNetworkImage = false;
            });
        }

        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {

        }

        protected override MauiApp CreateMauiApp() => MauiProgram
            .CreateMauiAppBuilder()
            .Build();
    }
}
