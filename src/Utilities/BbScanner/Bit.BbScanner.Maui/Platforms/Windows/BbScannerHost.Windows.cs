using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Bit.BbScanner;

public partial class BbScannerHost
{
    private Windows.UI.Color? _webViewBackground;

    partial void SetWebViewTransparent(BlazorWebView webView, bool transparent)
    {
        if (webView.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.WebView2 view)
        {
            return;
        }

        if (transparent)
        {
            _webViewBackground = view.DefaultBackgroundColor;
            view.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
        }
        else if (_webViewBackground is { } background)
        {
            view.DefaultBackgroundColor = background;
        }
    }
}
