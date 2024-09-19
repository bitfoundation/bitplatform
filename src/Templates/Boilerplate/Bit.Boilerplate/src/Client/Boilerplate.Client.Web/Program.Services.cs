//+:cnd:noEmit
//#if (appInsights == true)
using BlazorApplicationInsights;
//#endif
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Boilerplate.Client.Web.Services;

namespace Boilerplate.Client.Web;

public static partial class Program
{
    public static void ConfigureServices(this WebAssemblyHostBuilder builder)
    {
        // Services being registered here can get injected in web project only.

        var services = builder.Services;
        var configuration = builder.Configuration;

        configuration.AddClientConfigurations();

        builder.Logging.AddConfiguration(configuration.GetSection("Logging"));

        Uri.TryCreate(configuration.GetServerAddress(), UriKind.RelativeOrAbsolute, out var serverAddress);

        if (serverAddress!.IsAbsoluteUri is false)
        {
            serverAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), serverAddress);
        }

        services.TryAddSingleton(sp => new HttpClient(sp.GetRequiredService<HttpMessageHandler>()) { BaseAddress = serverAddress });

        //#if (appInsights == true)
        services.AddBlazorApplicationInsights(x =>
        {
            x.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        },
        async appInsights =>
        {
            await appInsights.AddTelemetryInitializer(new()
            {
                Tags = new Dictionary<string, object?>()
                {
                    { "ai.application.ver", typeof(Program).Assembly.GetName().Version!.ToString() }
                }
            });
        });
        //#endif

        services.AddClientWebProjectServices();
    }

    public static void AddClientWebProjectServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in both web project and server (during prerendering).

        services.TryAddTransient<IBitDeviceCoordinator, WebDeviceCoordinator>();
        services.TryAddTransient<IExceptionHandler, WebExceptionHandler>();

        services.AddClientCoreProjectServices();
    }
}
