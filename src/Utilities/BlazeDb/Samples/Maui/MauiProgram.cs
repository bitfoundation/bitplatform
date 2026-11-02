using Microsoft.Extensions.Logging;

namespace Bit.BlazeDb.Samples.Maui;

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

        // One app, one DI container: the BlazorWebView renders every component in this process, so the
        // database lives here too - in files in the app's data directory, not in the WebView's storage.
        builder.Services.AddCoreServices<MauiSampleDatabase>();

        return builder.Build();
    }
}
