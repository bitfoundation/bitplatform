using Microsoft.Extensions.Logging;

namespace Bit.Bmotion.Samples.Maui;

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
        // there is no second (client) container to register the Bmotion services in as well. Its
        // JS runtime is asynchronous, as on Server, so the browser plays the animations here too,
        // with no network between the engine and the WebView.
        builder.Services.AddCoreServices();

        return builder.Build();
    }
}
