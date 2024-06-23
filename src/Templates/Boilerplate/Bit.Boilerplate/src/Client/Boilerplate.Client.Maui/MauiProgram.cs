//-:cnd:noEmit
using Maui.AppStores;
using Maui.InAppReviews;
using Maui.Android.InAppUpdates;
using Microsoft.Maui.LifecycleEvents;
using Boilerplate.Client.Core;
#if IOS || MACCATALYST
using UIKit;
using WebKit;
using Foundation;
#endif

namespace Boilerplate.Client.Maui;

public static partial class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        //+:cnd:noEmit
        //#if (appCenter == true)
        string? appCenterSecret = null;
        if (appCenterSecret is not null)
        {
            Microsoft.AppCenter.AppCenter.Start(appCenterSecret, typeof(Microsoft.AppCenter.Crashes.Crashes), typeof(Microsoft.AppCenter.Analytics.Analytics));
        }
        //#endif
        //-:cnd:noEmit

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
                bool HandleAppLink(NSUserActivity? userActivity)
                {
                    if (userActivity is not null && userActivity.ActivityType == NSUserActivityType.BrowsingWeb && userActivity.WebPageUrl is not null)
                    {
                        var url = $"{userActivity.WebPageUrl.Path}?{userActivity.WebPageUrl.Query}";

                        _ = Routes.OpenUniversalLink(url);

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
                            .FirstOrDefault(a => a.ActivityType == NSUserActivityType.BrowsingWeb)));

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
            webView.NavigationDelegate = new CustomWKNavigationDelegate();
            webView.Configuration.AllowsInlineMediaPlayback = true;

            webView.BackgroundColor = UIColor.Clear;
            webView.ScrollView.Bounces = false;
            webView.Opaque = false;

            if (BuildConfiguration.IsDebug())
            {
                if ((DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst && DeviceInfo.Current.Version >= new Version(13, 3))
                    || (DeviceInfo.Current.Platform == DevicePlatform.iOS && DeviceInfo.Current.Version >= new Version(16, 4)))
                {
                    webView.SetValueForKey(NSObject.FromObject(true), new NSString("inspectable"));
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

#if IOS || MACCATALYST
    public class CustomWKNavigationDelegate : WKNavigationDelegate
    {
        public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, WKWebpagePreferences preferences, Action<WKNavigationActionPolicy, WKWebpagePreferences> decisionHandler)
        {
            // To open Google reCAPTCHA and similar elements directly within the webview.
            decisionHandler?.Invoke(WKNavigationActionPolicy.Allow, preferences);

            if (navigationAction.NavigationType is WKNavigationType.LinkActivated)
            {
                // https://developer.apple.com/documentation/webkit/wknavigationtype/linkactivated#discussion
                _ = Browser.OpenAsync(navigationAction.Request.Url!);
            }
        }
    }
#endif
}
