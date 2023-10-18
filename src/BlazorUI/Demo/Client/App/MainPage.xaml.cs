namespace Bit.BlazorUI.Demo.Client.App;

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
    }

    private async void ContentPage_Loaded(object sender, EventArgs e)
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
            await _deviceCoordinator.SetDeviceTheme(AppInfo.Current.RequestedTheme is AppTheme.Dark);
        }
        catch (Exception exp)
        {
            _exceptionHandler.Handle(exp);
        }
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
    }
}
