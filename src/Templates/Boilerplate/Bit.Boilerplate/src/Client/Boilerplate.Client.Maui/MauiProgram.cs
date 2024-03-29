//-:cnd:noEmit
using Boilerplate.Client.Core;
using Maui.Android.InAppUpdates;
using Maui.AppStores;
using Maui.InAppReviews;
using Microsoft.Maui.LifecycleEvents;

namespace Boilerplate.Client.Maui;

public static partial class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppRenderMode.IsBlazorHybrid = true;

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseAndroidInAppUpdates()
            .UseInAppReviews()
            .UseAppStoreInfo()
            .Configuration.AddClientConfigurations();

        builder.ConfigureServices();

        builder.ConfigureLifecycleEvents(lifecycle =>
        {
#if iOS || Mac
            lifecycle.AddiOS(ios =>
            {
                bool HandleAppLink(Foundation.NSUserActivity? userActivity)
                {
                    if (userActivity is not null && userActivity.ActivityType == Foundation.NSUserActivityType.BrowsingWeb && userActivity.WebPageUrl is not null)
                    {
                        var url = $"{userActivity.WebPageUrl.Path}?{userActivity.WebPageUrl.Query}";

                        var _ = Routes.OpenUniversalLink(url);

                        return true;
                    }

                    return false;
                }

                ios.FinishedLaunching((app, data)
                    => HandleAppLink(app.UserActivity));

                ios.ContinueUserActivity((app, userActivity, handler)
                    => HandleAppLink(userActivity));

                if (OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13))
                {
                    ios.SceneWillConnect((scene, sceneSession, sceneConnectionOptions)
                        => HandleAppLink(sceneConnectionOptions.UserActivities.ToArray()
                            .FirstOrDefault(a => a.ActivityType == Foundation.NSUserActivityType.BrowsingWeb)));

                    ios.SceneContinueUserActivity((scene, userActivity)
                        => HandleAppLink(userActivity));
                }
            });
#endif
        });

        SetupBlazorWebView();

        var mauiApp = builder.Build();

        return mauiApp;
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
    }
}
