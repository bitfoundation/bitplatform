//+:cnd:noEmit
using Boilerplate.Server.Api;
using Boilerplate.Client.Web;
using Boilerplate.Server.Services;
using Boilerplate.Client.Core.Services.Contracts;

namespace Boilerplate.Server;

public static partial class Program
{
    private static void ConfigureServices(this WebApplicationBuilder builder)
    {
        // Services being registered here can get injected in server project only.

        var services = builder.Services;
        var configuration = builder.Configuration;
        var env = builder.Environment;

        AddBlazor(builder);

        services.AddHttpContextAccessor();

        builder.ConfigureApiServices();

        services.AddClientWebProjectServices();
    }

    private static void AddBlazor(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.TryAddTransient<IAuthTokenProvider, ServerSideAuthTokenProvider>();

        services.TryAddTransient(sp =>
        {
            Uri.TryCreate(configuration.GetServerAddress(), UriKind.RelativeOrAbsolute, out var serverAddress);

            if (serverAddress!.IsAbsoluteUri is false)
            {
                serverAddress = new Uri(sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.GetBaseUrl(), serverAddress);
            }

            return new HttpClient(sp.GetRequiredKeyedService<DelegatingHandler>("DefaultMessageHandler"))
            {
                BaseAddress = serverAddress
            };
        });

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddMvc();
    }
}
