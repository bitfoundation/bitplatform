//-:cnd:noEmit
namespace TodoTemplate.Client.App;

public partial class MainPage
{
    public MainPage()
    {
        InitializeComponent();

        BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("CustomBlazorWebViewMapper", (handler, view) =>
        {
#if IOS || MACCATALYST
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.Opaque = false;
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
}
