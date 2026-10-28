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
        // here, not on WebAssembly, so BbScanner reports NotSupported for the camera, while BbBarcodeDecoder (the
        // decoder page) runs in-process at native speed.
        builder.Services.AddCoreServices();

        return builder.Build();
    }
}
