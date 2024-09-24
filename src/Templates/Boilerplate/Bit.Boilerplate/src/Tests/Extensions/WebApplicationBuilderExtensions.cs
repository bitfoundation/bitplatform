using Boilerplate.Server.Web;
using Boilerplate.Tests.Services;
using Boilerplate.Client.Core.Services.Contracts;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.Builder;

public static partial class WebApplicationBuilderExtensions
{
    public static void AddTestProjectServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.TryAddSingleton<IStorageService, TestStorageService>();
        services.TryAddTransient<IAuthTokenProvider, TestTokenProvider>();

        services.TryAddTransient(sp =>
        {
            return new HttpClient(sp.GetRequiredService<HttpMessageHandler>())
            {
                BaseAddress = new Uri(sp.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First())
            };
        });

        builder.AddServerWebProjectServices();
    }
}
