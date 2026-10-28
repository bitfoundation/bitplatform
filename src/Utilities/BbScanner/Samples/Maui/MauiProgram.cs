using Microsoft.Extensions.Logging;

namespace Bit.BbScanner.Samples.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // The device's own camera for the scanners - CameraX, AVFoundation or MediaCapture - with its preview drawn
        // behind the BlazorWebView, which MainPage wraps in a BbScannerHost.
        builder.UseBbScanner();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // One app, one DI container: the BlazorWebView renders every component in this process. .NET runs natively
        // here, not on WebAssembly: the camera's frames go from the platform straight to the decoder, which runs here
        // at native speed, as BbBarcodeDecoder does on the decoder page; the page only hosts the scanner's overlays.
        // The camera needs the platform's permission, declared in the Platforms folder.
        builder.Services.AddCoreServices();

        return builder.Build();
    }
}
