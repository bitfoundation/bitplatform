using Microsoft.Extensions.Logging;

namespace Bit.Blow.Samples.Maui;

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

        // One app, one DI container: the BlazorWebView renders every component in this process, so a drag on the
        // canvas is handled in-process by native .NET, with no network in between, through the standard IJSRuntime.
        builder.Services.AddCoreServices();

        return builder.Build();
    }
}
