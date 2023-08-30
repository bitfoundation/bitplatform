//-:cnd:noEmit
namespace TodoTemplate.Client.App;

public partial class MainPage
{
    private readonly IExceptionHandler _exceptionHandler;

    public MainPage(IExceptionHandler exceptionHandler)
    {
        _exceptionHandler = exceptionHandler;

        InitializeComponent();

        SetupBlazorWebView();
        SetupStatusBar();
    }

    private void SetupBlazorWebView()
    {
        BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("CustomBlazorWebViewMapper", (handler, view) =>
        {
#if WINDOWS
            if (AppInfo.Current.RequestedTheme == AppTheme.Dark)
            {
                handler.PlatformView.DefaultBackgroundColor = Microsoft.UI.Colors.Black;
            }
#elif IOS || MACCATALYST
            handler.PlatformView.Configuration.AllowsInlineMediaPlayback = true;
            
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.Opaque = false;
#if DEBUG
            if ((DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst && DeviceInfo.Current.Version >= new Version(13, 3))
                || (DeviceInfo.Current.Platform == DevicePlatform.iOS && DeviceInfo.Current.Version >= new Version(16, 4)))
            {
                handler.PlatformView.SetValueForKey(Foundation.NSObject.FromObject(true), new Foundation.NSString("inspectable"));
            }
#endif
#elif ANDROID
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);

            Android.Webkit.WebSettings settings = handler.PlatformView.Settings;

            settings.AllowFileAccessFromFileURLs =
                settings.AllowUniversalAccessFromFileURLs =
                settings.AllowContentAccess =
                settings.AllowFileAccess =
                settings.DatabaseEnabled =
                settings.JavaScriptCanOpenWindowsAutomatically =
                settings.DomStorageEnabled = true;

#if DEBUG
            settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
#endif

            settings.BlockNetworkLoads =
                settings.BlockNetworkImage = false;
#endif
        });

        Loaded += async delegate
        {
            try
            {
#if WINDOWS && RELEASE
                var webView2 = (blazorWebView.Handler.PlatformView as Microsoft.UI.Xaml.Controls.WebView2);
                await webView2.EnsureCoreWebView2Async();

                var settings = webView2.CoreWebView2.Settings;
                settings.IsZoomControlEnabled = false;
                settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
            }
            catch (Exception exp)
            {
                _exceptionHandler.Handle(exp);
            }
        };
    }

    private void SetupStatusBar()
    {

        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), async (handler, view) =>
        {
            try
            {
#if ANDROID
                var window = handler.PlatformView.Window;
                if (window != null && Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
                {
                    window.ClearFlags(Android.Views.WindowManagerFlags.TranslucentStatus);
                    window.AddFlags(Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds);
                    window.SetStatusBarColor(Android.Graphics.Color.Transparent);

                    window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)(Android.Views.SystemUiFlags.LayoutFullscreen | Android.Views.SystemUiFlags.LayoutStable | Android.Views.SystemUiFlags.LightStatusBar);
                    if (AppInfo.Current.RequestedTheme == AppTheme.Dark)
                    {
                        window.DecorView.SystemUiVisibility &= ~(Android.Views.StatusBarVisibility)Android.Views.SystemUiFlags.LightStatusBar;
                    }
                }
#elif MACCATALYST
                var window = handler.PlatformView.WindowScene;
                if (window != null)
                {
                    window.Titlebar!.TitleVisibility = UIKit.UITitlebarTitleVisibility.Hidden;
                }
#elif IOS
                var statusBarStyle = AppInfo.Current.RequestedTheme == AppTheme.Dark ? UIKit.UIStatusBarStyle.LightContent : UIKit.UIStatusBarStyle.DarkContent;
                await Device.InvokeOnMainThreadAsync(() =>
                {
                    UIKit.UIApplication.SharedApplication.SetStatusBarStyle(statusBarStyle, false);
                    Platform.GetCurrentUIViewController().SetNeedsStatusBarAppearanceUpdate();
                });
#elif WINDOWS
                var window = handler.PlatformView;
                if (window != null)
                {
                    window.ExtendsContentIntoTitleBar = true;
                }
#endif
            }
            catch (Exception exp)
            {
                _exceptionHandler.Handle(exp);
            }
        });
    }
}
