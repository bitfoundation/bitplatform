using Microsoft.AspNetCore.Components.WebView;

namespace Bit.BbScanner.Samples.Maui;

public partial class MainPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void OnBlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
#if ANDROID
        // Android's WebView ignores a pinch unless its built-in zoom is on; its on-screen zoom buttons stay hidden.
        e.WebView.Settings.BuiltInZoomControls = true;
        e.WebView.Settings.DisplayZoomControls = false;

        // The scanner's camera: Android asks the app, not the user, whether the page may have it. The client
        // BlazorWebView installed can only be read back from Android 8; before that the page goes without the camera
        // rather than without the file chooser that client provides.
        if (OperatingSystem.IsAndroidVersionAtLeast(26) && e.WebView.WebChromeClient is not CameraWebChromeClient)
        {
            e.WebView.SetWebChromeClient(new CameraWebChromeClient(e.WebView.WebChromeClient));
        }
#endif
    }
}
