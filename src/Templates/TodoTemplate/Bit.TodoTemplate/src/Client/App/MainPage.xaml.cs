//-:cnd:noEmit
namespace TodoTemplate.Client.App;

public partial class MainPage
{
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IBitDeviceCoordinator _deviceCoordinator;

    public MainPage(IExceptionHandler exceptionHandler, IBitDeviceCoordinator deviceCoordinator)
    {
        _exceptionHandler = exceptionHandler;
        _deviceCoordinator = deviceCoordinator;

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
            
            handler.PlatformView.ScrollView.Bounces = false;
            
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
            
            handler.PlatformView.OverScrollMode = Android.Views.OverScrollMode.Never;

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
                var webView2 = (Microsoft.UI.Xaml.Controls.WebView2)blazorWebView.Handler!.PlatformView!;
                await webView2!.EnsureCoreWebView2Async();

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
                await _deviceCoordinator.ApplyTheme(AppInfo.Current.RequestedTheme is AppTheme.Dark);
            }
            catch (Exception exp)
            {
                _exceptionHandler.Handle(exp);
            }
        });
    }
}
