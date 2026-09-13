//+:cnd:noEmit
using Microsoft.Maui.Platform;
using Microsoft.Extensions.Options;
using Microsoft.Maui.LifecycleEvents;
using Boilerplate.Client.Core.Styles;
using Boilerplate.Client.Maui.Infrastructure.Services;
using Maui.AppStores;
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

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            CultureInfoManager.SetCurrentCulture(
                Preferences.Get("Culture", null) ?? // 1- User settings (the key MauiStorageService persists)
                CultureInfo.CurrentUICulture.Name); // 2- OS settings
        }

        var builder = MauiApp.CreateBuilder();
        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Maui");

        //+:cnd:noEmit
        builder
            .UseMauiApp<App>()
            .UseAppStoreInfo()
            //#if (sentry == true)
            .UseSentry(options =>
            {
                builder.Configuration.DynamicBind("Logging:Sentry", options);
            })
            //#endif
            ;

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

        mauiApp.Services.GetService<IStartupValidator>()?.Validate();

        mauiApp.Services.GetRequiredService<PubSubService>()
            .Subscribe(ClientAppMessages.PAGE_DATA_CHANGED, async (args) =>
            {
                var (title, _, __) = ((string?, string?, bool))args!;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Application.Current!.Windows.First().Title = title ?? "Boilerplate";
                });
            });

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
                .ContinueWith(initialization =>
                {
                    if (initialization.IsFaulted)
                    {
                        // Otherwise the failure is only ever seen as an unobserved task exception minutes later,
                        // and the continuation below would go on to dereference a null CoreWebView2.
                        LogException(initialization.Exception, reportedBy: nameof(webView.EnsureCoreWebView2Async));
                        return;
                    }

                    _ = Application.Current!.Dispatcher.DispatchAsync(() =>
                    {
                        webView.CoreWebView2.PermissionRequested += HandlePermissionRequested;
                    });
                }, TaskScheduler.Default);

#elif iOS || Mac
            webView.NavigationDelegate = new CustomWKNavigationDelegate();
            webView.Configuration.AllowsInlineMediaPlayback = true;

            webView.BackgroundColor = UIColor.Clear;
            webView.ScrollView.Bounces = false;
            webView.Opaque = false;

            if (DeviceInfo.Current.Version >= new Version(16, 4))
            {
                webView.Inspectable = true;
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

            Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);

            if (AppEnvironment.IsDevelopment())
            {
                settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
            }

            if (webView.WebChromeClient is not Platforms.Android.AppWebChromeClient)
            {
                webView.SetWebChromeClient(new Platforms.Android.AppWebChromeClient(webView.WebChromeClient));
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

#if Windows
    /// <summary>
    /// Answers one permission kind and leaves the rest to WebView2's own prompt.
    /// <para>
    /// The android head takes the same position deliberately (see AppWebChromeClient.OnPermissionRequest): an allow
    /// list of one, widened a resource at a time. Answering every kind with Allow and setting Handled suppresses
    /// that prompt, so camera, geolocation and clipboard-read would be granted silently to any script running in
    /// the page - including third party scripts such as the ad SDK, which runs in the app's own origin.
    /// </para>
    /// </summary>
    private static void HandlePermissionRequested(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2PermissionRequestedEventArgs args)
    {
        if (args.PermissionKind is not (Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Microphone
                             or Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.ClipboardRead
                             or Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind.Notifications)) return;

        args.Handled = true;
        args.State = Microsoft.Web.WebView2.Core.CoreWebView2PermissionState.Allow;
    }
#endif

    internal static void LogException(object? error, string reportedBy)
    {
        if (IPlatformApplication.Current?.Services is IServiceProvider services && error is Exception exp)
        {
            services.GetRequiredService<ClientExceptionHandlerBase>().Handle(exp, parameters: new()
            {
                { nameof(reportedBy), reportedBy }
            }, displayKind: AppEnvironment.IsDevelopment() ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
        }
        else
        {
            _ = Console.Error.WriteLineAsync(error?.ToString() ?? "Unknown error");
        }
    }
}
