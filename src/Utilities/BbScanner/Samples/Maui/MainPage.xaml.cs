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
#endif
    }
}
