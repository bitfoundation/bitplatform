
using System.Reflection;
using Bit.BlazorUI.Demo.Client.Core.Services.HttpMessageHandlers;
using Bit.BlazorUI.Demo.Client.Core.Shared;
using Bit.BlazorUI.Demo.Client.Maui.Services;

namespace Bit.BlazorUI.Demo.Client.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        try
        {
            AppRenderMode.IsBlazorHybrid = true;

            // For MacCatalyst there are still some issues with AppCenter
            // https://github.com/microsoft/appcenter-sdk-dotnet/issues/1755
            // https://github.com/microsoft/appcenter-sdk-dotnet/issues/1702
#if Android || iOS || Windows
            string appSecret = OperatingSystem.IsWindows() ? "a206e212-6427-414f-b4f3-83fa5eec4f1d"
                             : OperatingSystem.IsAndroid() ? "c87802e3-0fa5-4938-b539-086b06d40726" : "f76345b1-9069-4477-afbe-a2be2a2ed46d";
            Microsoft.AppCenter.AppCenter.Start(appSecret, typeof(Microsoft.AppCenter.Crashes.Crashes));
#endif
            var builder = MauiApp.CreateBuilder();
            var assembly = typeof(MainLayout).GetTypeInfo().Assembly;

            builder
                .UseMauiApp<App>()
                .Configuration.AddClientConfigurations();

            var services = builder.Services;

            services.AddMauiBlazorWebView();

            if (BuildConfiguration.IsDebug())
            {
                services.AddBlazorWebViewDeveloperTools();
            }

            Uri.TryCreate(builder.Configuration.GetApiServerAddress(), UriKind.Absolute, out var apiServerAddress);

            services.AddTransient(sp =>
            {
                var handler = sp.GetRequiredService<RequestHeadersDelegationHandler>();
                HttpClient httpClient = new(handler)
                {
                    BaseAddress = apiServerAddress
                };
                return httpClient;
            });

            services.AddClientMauiServices();

            SetupBlazorWebView();

            var mauiApp = builder.Build();

            return mauiApp;
        }
        catch (Exception ex)
        {
#if Android || iOS || Windows
            Microsoft.AppCenter.Crashes.Crashes.TrackError(ex);
#endif
            throw;
        }
    }

    private static void SetupBlazorWebView()
    {
        BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("CustomBlazorWebViewMapper", static (handler, view) =>
        {
            var webView = handler.PlatformView;
#if WINDOWS
            if (AppInfo.Current.RequestedTheme == AppTheme.Dark)
            {
                webView.DefaultBackgroundColor = Microsoft.UI.Colors.Black;
            }

            if (BuildConfiguration.IsRelease())
            {
                webView.EnsureCoreWebView2Async()
                    .AsTask()
                    .ContinueWith(async _ =>
                    {
                        await Application.Current!.Dispatcher.DispatchAsync(() =>
                        {
                            var settings = webView.CoreWebView2.Settings;
                            settings.IsZoomControlEnabled = false;
                            settings.AreBrowserAcceleratorKeysEnabled = false;
                        });
                    });
            }

#elif IOS || MACCATALYST
                webView.Configuration.AllowsInlineMediaPlayback = true;

                webView.BackgroundColor = UIKit.UIColor.Clear;
                webView.ScrollView.Bounces = false;
                webView.Opaque = false;

                if (BuildConfiguration.IsDebug())
                {
                    if ((DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst && DeviceInfo.Current.Version >= new Version(13, 3))
                        || (DeviceInfo.Current.Platform == DevicePlatform.iOS && DeviceInfo.Current.Version >= new Version(16, 4)))
                    {
                        webView.SetValueForKey(Foundation.NSObject.FromObject(true), new Foundation.NSString("inspectable"));
                    }
                }
#elif ANDROID
            webView.SetBackgroundColor(Android.Graphics.Color.Transparent);

            webView.OverScrollMode = Android.Views.OverScrollMode.Never;

            webView.HapticFeedbackEnabled = false;

            Android.Webkit.WebSettings settings = webView.Settings;

            settings.AllowFileAccessFromFileURLs =
                settings.AllowUniversalAccessFromFileURLs =
                settings.AllowContentAccess =
                settings.AllowFileAccess =
                settings.DatabaseEnabled =
                settings.JavaScriptCanOpenWindowsAutomatically =
                settings.DomStorageEnabled = true;

            if (BuildConfiguration.IsDebug())
            {
                settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
            }

            settings.BlockNetworkLoads = settings.BlockNetworkImage = false;
#endif
        });

        AppContext.SetSwitch("BlazorWebView.AndroidFireAndForgetAsync", isEnabled: true);
    }
}
