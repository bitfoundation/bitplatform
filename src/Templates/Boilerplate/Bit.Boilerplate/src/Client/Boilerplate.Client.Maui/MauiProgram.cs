//+:cnd:noEmit
using Microsoft.Maui.Platform;
using Microsoft.Maui.LifecycleEvents;
//#if (notification == true)
using Plugin.LocalNotification;
//#endif
using Boilerplate.Client.Core.Styles;
using Boilerplate.Client.Maui.Services;
//#if (framework == 'net9.0')
using Maui.AppStores;
using Maui.InAppReviews;
using Maui.Android.InAppUpdates;
//#endif
//-:cnd:noEmit
#if iOS || Mac
using UIKit;
using WebKit;
using Foundation;
#endif

namespace Boilerplate.Client.Maui;

public static partial class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject, reportedBy: nameof(AppDomain.UnhandledException));
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogException(e.Exception, nameof(TaskScheduler.UnobservedTaskException));
            e.SetObserved();
        };

        AppPlatform.IsBlazorHybrid = true;
#if iOS
        AppPlatform.IsIosOnMacOS = NSProcessInfo.ProcessInfo.IsiOSApplicationOnMac;
#endif
        ITelemetryContext.Current = new MauiTelemetryContext();

        var builder = MauiApp.CreateBuilder();

        //+:cnd:noEmit
        builder
            .UseMauiApp<App>()
            //#if (framework == 'net9.0')
            .UseInAppReviews()
            .UseAppStoreInfo()
            .UseAndroidInAppUpdates()
            //#endif
            //#if (sentry == true)
            .UseSentry(options =>
            {
                var configuration = new ConfigurationBuilder().AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Maui").Build();
                configuration.GetRequiredSection("Logging:Sentry").Bind(options);
            })
            //#endif
            .Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Maui");

        //#if (notification == true)
        if (AppPlatform.IsWindows is false)
        {
            builder.UseLocalNotification();
        }
        //#endif
        //-:cnd:noEmit

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

                        _ = Core.Components.Routes.OpenUniversalLink(url);

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
            var webViewBackgroundColor = AppInfo.Current.RequestedTheme == AppTheme.Dark ?
                ThemeColors.PrimaryDarkBgColor : ThemeColors.PrimaryLightBgColor;
#if Windows
            webView.DefaultBackgroundColor = Color.FromArgb(webViewBackgroundColor).ToWindowsColor();

            webView.EnsureCoreWebView2Async()
                .AsTask()
                .ContinueWith(async _ =>
                {
                    await Application.Current!.Dispatcher.DispatchAsync(() =>
                    {
                        webView.CoreWebView2.PermissionRequested += async (sender, args) =>
                        {
                            args.Handled = true;
                            args.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
                        };
                        if (AppEnvironment.IsDev() is false)
                        {
                            var settings = webView.CoreWebView2.Settings;
                            settings.IsZoomControlEnabled = false;
                            settings.AreBrowserAcceleratorKeysEnabled = false;
                        }
                    });
                });

#elif iOS || Mac
            webView.NavigationDelegate = new CustomWKNavigationDelegate();
            webView.Configuration.AllowsInlineMediaPlayback = true;

            webView.BackgroundColor = UIColor.Clear;
            webView.ScrollView.Bounces = false;
            webView.Opaque = false;

            if ((DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst && DeviceInfo.Current.Version >= new Version(13, 3))
                || (DeviceInfo.Current.Platform == DevicePlatform.iOS && DeviceInfo.Current.Version >= new Version(16, 4)))
            {
                webView.SetValueForKey(NSObject.FromObject(true), new NSString("inspectable"));
            }
#elif Android
            webView.SetBackgroundColor(Android.Graphics.Color.ParseColor(webViewBackgroundColor));

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

            if (AppEnvironment.IsDev())
            {
                settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
            }

            settings.BlockNetworkLoads = settings.BlockNetworkImage = false;
#endif
        });

        AppContext.SetSwitch("BlazorWebView.AndroidFireAndForgetAsync", isEnabled: true);
    }

#if iOS || Mac
    public partial class CustomWKNavigationDelegate : WKNavigationDelegate
    {
        public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, WKWebpagePreferences preferences, Action<WKNavigationActionPolicy, WKWebpagePreferences> decisionHandler)
        {
            if (navigationAction.NavigationType is WKNavigationType.LinkActivated)
            {
                // https://developer.apple.com/documentation/webkit/wknavigationtype/linkactivated#discussion
                _ = Browser.OpenAsync(navigationAction.Request.Url!);
                decisionHandler.Invoke(WKNavigationActionPolicy.Cancel, preferences);
            }
            else
            {
                // To open Google reCAPTCHA and similar elements directly within the webview.
                decisionHandler.Invoke(WKNavigationActionPolicy.Allow, preferences);
            }
        }
    }
#endif

    private static void LogException(object? error, string reportedBy)
    {
        if (IPlatformApplication.Current?.Services is IServiceProvider services && error is Exception exp)
        {
            services.GetRequiredService<IExceptionHandler>().Handle(exp, parameters: new()
            {
                { nameof(reportedBy), reportedBy }
            }, displayKind: AppEnvironment.IsDev() ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
        }
        else
        {
            _ = Console.Error.WriteLineAsync(error?.ToString() ?? "Unknown error");
        }
    }
}
