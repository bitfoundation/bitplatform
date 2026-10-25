using Microsoft.Extensions.Logging;

namespace Bit.BevTools.Samples.Maui;

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

        // There is no server to request from: the Network page requests a public site instead.
        builder.Services.AddCoreServices(settings => settings.HttpUrl = "https://bitplatform.dev/");

        // A MAUI app registers no host environment BevTools could read, so it says itself when BevTools runs: in Debug
        // builds. It is registered either way, since Routes.razor renders <BevToolsPanel />, which renders nothing when
        // BevTools is off.
        builder.Services.AddBitBevToolsServices(o =>
        {
#if DEBUG
            o.Enabled = true;
#else
            o.Enabled = false;
#endif
        });

        return builder.Build();
    }
}
