﻿//-:cnd:noEmit
namespace AdminPanel.Client.App;

public partial class MainPage
{
    private readonly IExceptionHandler _exceptionHandler;

    public MainPage(IExceptionHandler exceptionHandler)
    {
        _exceptionHandler = exceptionHandler;

        InitializeComponent();

        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
#if MACCATALYST
            if (handler != null)
            {
                handler.PlatformView.WindowScene.Titlebar.TitleVisibility = UIKit.UITitlebarTitleVisibility.Hidden;
            }
#endif
        });

        BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("CustomBlazorWebViewMapper", (handler, view) =>
        {
#if IOS || MACCATALYST
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.Opaque = false;
#if DEBUG
            handler.PlatformView.SetValueForKey(Foundation.NSObject.FromObject(true), new Foundation.NSString("inspectable"));
#endif
#endif

#if ANDROID
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
        }
        catch (Exception exp)
        {
            _exceptionHandler.Handle(exp);
        }
    }
}
