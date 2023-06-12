namespace Bit.BlazorUI.Demo.Client.App;

public partial class MainPage
{
    public MainPage()
    {
        InitializeComponent();
        HandleBlazorWebView();
        HandleTitleBar();
    }

    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        try
        {
            HandleTitleBar();
#if WINDOWS && RELEASE
            var webView2 = (blazorWebView.Handler.PlatformView as Microsoft.UI.Xaml.Controls.WebView2);
            await webView2.EnsureCoreWebView2Async();

            var settings = webView2.CoreWebView2.Settings;
            settings.IsZoomControlEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void HandleBlazorWebView()
    {
        BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("CustomBlazorWebViewMapper", (handler, view) =>
        {
#if WINDOWS
            if (AppInfo.Current.RequestedTheme == AppTheme.Dark)
            {
                handler.PlatformView.DefaultBackgroundColor = Microsoft.UI.Colors.Black;
            }
#endif

#if IOS || MACCATALYST
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.Opaque = false;
#endif

#if ANDROID
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
    }

    private void HandleTitleBar()
    {
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
#if ANDROID
            var window = handler.PlatformView.Window;

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                window.ClearFlags(Android.Views.WindowManagerFlags.TranslucentStatus);
                window.AddFlags(Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds);
                window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)(Android.Views.SystemUiFlags.LayoutFullscreen | Android.Views.SystemUiFlags.LayoutStable);
                window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            }
#endif

#if MACCATALYST
            var window = handler.PlatformView.WindowScene;
            if (window != null)
            {
                window.Titlebar.TitleVisibility = UIKit.UITitlebarTitleVisibility.Hidden;
            }
#endif

#if WINDOWS
            var window = handler.PlatformView;
            if (window != null)
            {
                window.ExtendsContentIntoTitleBar = true;
            }
#endif
        });
    }
}
