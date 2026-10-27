using Microsoft.Extensions.Logging;

namespace Bit.BbScanner.Samples.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // One app, one DI container: the BlazorWebView renders every component in this process. .NET runs natively
        // here, not on WebAssembly: BbScanner opens the camera in the WebView and the WebView sends it each frame, and
        // the frame loop and the decoder run here at native speed, as BbBarcodeDecoder does on the decoder page. The
        // camera needs the platform's permission, set up in the Platforms folder (and in MainPage on Android).
        builder.Services.AddCoreServices();

        return builder.Build();
    }
}
