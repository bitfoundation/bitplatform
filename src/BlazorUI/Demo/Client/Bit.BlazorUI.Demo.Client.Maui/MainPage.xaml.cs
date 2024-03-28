namespace Bit.BlazorUI.Demo.Client.Maui;

public partial class MainPage
{
    private readonly IExceptionHandler exceptionHandler;
    private readonly IBitDeviceCoordinator deviceCoordinator;

    public MainPage(IExceptionHandler exceptionHandler, IBitDeviceCoordinator deviceCoordinator)
    {
        this.exceptionHandler = exceptionHandler;
        this.deviceCoordinator = deviceCoordinator;

        InitializeComponent();

        SetupBlazorWebView();
        SetupStatusBar();
    }

    private void SetupBlazorWebView()
    {
        BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("CustomBlazorWebViewMapper", (handler, view) =>
        {
            var webView = handler.PlatformView;
#if WINDOWS
            if (AppInfo.Current.RequestedTheme == AppTheme.Dark)
            {
                webView.DefaultBackgroundColor = Microsoft.UI.Colors.Black;
            }

            Loaded += async delegate
            {
                if (BuildConfiguration.IsRelease())
                {
                    await webView.EnsureCoreWebView2Async();
                    var settings = webView.CoreWebView2.Settings;
                    settings.IsZoomControlEnabled = false;
                    settings.AreBrowserAcceleratorKeysEnabled = false;
                }
            };

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

    private void SetupStatusBar()
    {
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), async (handler, view) =>
        {
            try
            {
                await deviceCoordinator.ApplyTheme(AppInfo.Current.RequestedTheme is AppTheme.Dark);
            }
            catch (Exception exp)
            {
                exceptionHandler.Handle(exp);
            }
        });
    }
}
