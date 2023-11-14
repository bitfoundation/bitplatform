//-:cnd:noEmit

using System.Reflection;
using Boilerplate.Client.Core.Services.HttpMessageHandlers;
using Microsoft.Extensions.FileProviders;

namespace Boilerplate.Client.App;

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
            .Configuration.AddClientConfigurations();

        var services = builder.Services;

        services.AddMauiBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        Uri.TryCreate(builder.Configuration.GetApiServerAddress(), UriKind.Absolute, out var apiServerAddress);

        services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<LocalizationDelegatingHandler>();
            HttpClient httpClient = new(handler)
            {
                BaseAddress = apiServerAddress
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
