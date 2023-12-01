//-:cnd:noEmit

using System.Reflection;
using Boilerplate.Client.Core.Services.HttpMessageHandlers;
using Boilerplate.Client.Maui.Services;

namespace Boilerplate.Client.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        var assembly = typeof(MainLayout).GetTypeInfo().Assembly;

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
            var handler = sp.GetRequiredService<RequestHeadersDelegationHandler>();
            HttpClient httpClient = new(handler)
            {
                BaseAddress = apiServerAddress
            };
            return httpClient;
        });

        services.AddTransient<IStorageService, MauiStorageService>();

        services.AddClientMauiServices();

        var mauiApp = builder.Build();

        return mauiApp;
    }
}
