#if IOS || MACCATALYST
using Microsoft.AspNetCore.Components.WebView.Maui;
using UIKit;
using WebKit;

namespace Bit.BbScanner;

public partial class BbScannerHost
{
    private bool _opaque = true;
    private UIColor? _webViewBackground;
    private UIColor? _scrollViewBackground;

    partial void SetWebViewTransparent(BlazorWebView webView, bool transparent)
    {
        if (webView.Handler?.PlatformView is not WKWebView view)
        {
            return;
        }

        if (transparent)
        {
            _opaque = view.Opaque;
            _webViewBackground = view.BackgroundColor;
            _scrollViewBackground = view.ScrollView.BackgroundColor;
            view.Opaque = false;
            view.BackgroundColor = UIColor.Clear;
            view.ScrollView.BackgroundColor = UIColor.Clear;
        }
        else
        {
            view.Opaque = _opaque;
            view.BackgroundColor = _webViewBackground;
            view.ScrollView.BackgroundColor = _scrollViewBackground;
        }
    }
}
#endif
