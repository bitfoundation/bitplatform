using Microsoft.Extensions.Logging;

namespace Bit.Butil.Samples.Maui;

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
        // there is no second (client) container to register the Butil services in as well.
        builder.Services.AddCoreServices();

        return builder.Build();
    }
}
