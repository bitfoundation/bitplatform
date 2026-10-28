using Microsoft.Extensions.Logging;

namespace Bit.Bonboarding.Samples.Maui;

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

        // One app, one DI container: the BlazorWebView renders every component in this process, so the services are
        // registered here alone. The tours run exactly as in a browser - the library's one module measures, observes
        // and routes keys inside the WebView - and progress is kept in the WebView's localStorage, which lives in the
        // app's own data and survives a restart.
        builder.Services.AddCoreServices();

        return builder.Build();
    }
}
