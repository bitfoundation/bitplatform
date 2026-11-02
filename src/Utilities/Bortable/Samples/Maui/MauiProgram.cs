using Microsoft.Extensions.Logging;

namespace Bit.Bortable.Samples.Maui;

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

        // One app, one DI container: the BlazorWebView renders every component in this process, so each new drop
        // position is decided in-process by native .NET, with no network in between, through the standard IJSRuntime.
        // A mouse drags with the web view's own HTML5 drag and drop, a finger or a pen with pointer events.
        builder.Services.AddCoreServices();

        return builder.Build();
    }
}
