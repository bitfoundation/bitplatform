using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Bit.BbScanner;

public partial class BbScannerHost
{
    partial void SetWebViewTransparent(BlazorWebView webView, bool transparent)
    {
        if (webView.Handler?.PlatformView is not Android.Webkit.WebView view)
        {
            return;
        }

        if (transparent)
        {
            // The page's colour is painted by the host instead.
            view.SetBackgroundColor(Android.Graphics.Color.Transparent);
            return;
        }

        // The WebView's own canvas is white; a background the app gave the BlazorWebView goes back on over it.
        view.SetBackgroundColor(Android.Graphics.Color.White);
        if (((IView)webView).Background is not null)
        {
            webView.Handler.UpdateValue(nameof(IView.Background));
        }
    }
}
