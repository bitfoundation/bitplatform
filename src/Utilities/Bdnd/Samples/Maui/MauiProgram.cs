using Microsoft.Extensions.Logging;

namespace Bit.Bdnd.Samples.Maui;

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

        // One app, one DI container: the BlazorWebView renders every component in this process, so
        // there is no second (client) container to register the Bdnd services in as well.
        builder.Services.AddCoreServices();

        return builder.Build();
    }
}
