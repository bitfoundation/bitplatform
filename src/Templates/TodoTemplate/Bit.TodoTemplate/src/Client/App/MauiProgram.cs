//-:cnd:noEmit
using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace TodoTemplate.Client.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
#if !BlazorHybrid
        throw new InvalidOperationException("Please switch to blazor hybrid as described in https://bitplatform.dev/templates/hosting-models");
#endif

        var builder = MauiApp.CreateBuilder();
        var assembly = typeof(MainLayout).GetTypeInfo().Assembly;

        builder
            .UseMauiApp<App>()
            .Configuration.AddJsonFile(new EmbeddedFileProvider(assembly), "appsettings.json", optional: false, false);

        var services = builder.Services;

        services.AddMauiBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        services.AddScoped(sp =>
        {
            HttpClient httpClient = new(sp.GetRequiredService<AppHttpClientHandler>())
            {
                BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>().GetApiServerAddress())
            };

            return httpClient;
        });

        services.AddTransient<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.AddSharedServices();
        services.AddClientSharedServices();
        services.AddClientAppServices();

        var mauiApp = builder.Build();

        return mauiApp;
    }
}
