using Microsoft.Extensions.Logging.Configuration;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AdminPanel.Client.Web.Services;
using Microsoft.Extensions.Options;
using AdminPanel.Shared;

namespace AdminPanel.Client.Web;

public static partial class Program
{
    public static void ConfigureServices(this WebAssemblyHostBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddClientWebProjectServices(configuration);

        // The following services are blazor web assembly only.

        builder.Logging.AddConfiguration(configuration.GetSection("Logging"));

        Uri.TryCreate(configuration.GetServerAddress(), UriKind.RelativeOrAbsolute, out var serverAddress);

        if (serverAddress!.IsAbsoluteUri is false)
        {
            serverAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), serverAddress);
        }

        services.AddSessioned(sp => new HttpClient(sp.GetRequiredService<HttpMessageHandler>()) { BaseAddress = serverAddress });
    }

    public static void AddClientWebProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddClientCoreProjectServices(configuration);

        // The following services work both in blazor web assembly and server side for pre-rendering and blazor server.

        services.AddTransient<IBitDeviceCoordinator, WebDeviceCoordinator>();
        services.AddTransient<IExceptionHandler, WebExceptionHandler>();
        services.AddScoped<IPushNotificationService, WebPushNotificationService>();

        services.AddTransient<IPrerenderStateService, WebPrerenderStateService>();

        services.AddOptions<ClientWebSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp => configuration.Get<ClientWebSettings>()!);

        services.AddTransient<IStorageService, BrowserStorageService>();
    }
}
