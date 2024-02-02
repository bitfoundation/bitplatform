using Boilerplate.Client.Web.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Boilerplate.Client.Web;

public static partial class Program
{
    public static void ConfigureServices(this WebAssemblyHostBuilder builder)
    {
        // Services being registered here can get injected in web (blazor web assembly & blazor server)

        var services = builder.Services;
        var configuration = builder.Configuration;

        configuration.AddClientConfigurations();

        Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.RelativeOrAbsolute, out var apiServerAddress);

        if (apiServerAddress!.IsAbsoluteUri is false)
        {
            apiServerAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), apiServerAddress);
        }

        services.AddTransient(sp => new HttpClient(sp.GetRequiredKeyedService<HttpMessageHandler>("DefaultMessageHandler")) { BaseAddress = apiServerAddress });

        services.AddTransient<IBitDeviceCoordinator, WebDeviceCoordinator>();
        services.AddTransient<IExceptionHandler, WebExceptionHandler>();

        services.AddClientCoreProjectServices();
    }
}
