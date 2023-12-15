//-:cnd:noEmit

using System.Reflection;
using Boilerplate.Client.Maui.Services;

namespace Boilerplate.Client.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppRenderMode.IsBlazorHybrid = true;

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .Configuration.AddClientConfigurations();

        var services = builder.Services;

        services.AddMauiBlazorWebView();

        if (BuildConfiguration.IsDebug())
        {
            services.AddBlazorWebViewDeveloperTools();
        }

        Uri.TryCreate(builder.Configuration.GetApiServerAddress(), UriKind.Absolute, out var apiServerAddress);

        services.AddTransient(sp =>
        {
            var handler = sp.GetRequiredKeyedService<HttpMessageHandler>("DefaultMessageHandler");
            HttpClient httpClient = new(handler)
            {
                BaseAddress = apiServerAddress
            };
            return httpClient;
        });

        services.AddClientMauiServices();

        var mauiApp = builder.Build();

        return mauiApp;
    }
}
