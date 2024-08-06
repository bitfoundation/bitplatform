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

        var serverAddress = configuration.GetServerAddress(baseUrl: new Uri(builder.HostEnvironment.BaseAddress, UriKind.Absolute));

        services.TryAddSingleton(sp => new HttpClient(sp.GetRequiredKeyedService<DelegatingHandler>("DefaultMessageHandler")) { BaseAddress = serverAddress });

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
