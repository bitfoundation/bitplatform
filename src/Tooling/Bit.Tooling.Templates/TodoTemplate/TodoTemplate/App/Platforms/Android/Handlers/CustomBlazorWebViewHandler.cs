using Android.Webkit;

namespace TodoTemplate.App.Platforms.Android.Handlers;

public class CustomBlazorWebViewHandler : BlazorWebViewHandler
{
    protected override WebChromeClient GetWebChromeClient()
    {
        var webChromeClient = base.GetWebChromeClient();

        return webChromeClient;
    }

    protected override WebViewClient GetWebViewClient()
    {
        var webViewClient = base.GetWebViewClient();

        return webViewClient;
    }

    protected override WebView CreateNativeView()
    {
        var webView = base.CreateNativeView();

        return webView;
    }
}
