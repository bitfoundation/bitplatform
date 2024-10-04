using Boilerplate.Server.Web;
using Boilerplate.Tests.Services;
using Boilerplate.Client.Core.Services.Contracts;

namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationBuilderExtensions
{
    public static void AddTestProjectServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.TryAddScoped<IStorageService, TestStorageService>();
        services.TryAddTransient<IAuthTokenProvider, TestTokenProvider>();

        services.TryAddTransient(sp =>
        {
            return new HttpClient(sp.GetRequiredService<HttpMessageHandler>())
            {
                BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>().GetServerAddress(), UriKind.Absolute)
            };
        });

        builder.AddServerWebProjectServices();
    }
}
