using Boilerplate.Server.Web;
using Boilerplate.Tests.Services;
using Boilerplate.Server.Api.Services;
using Boilerplate.Client.Core.Services.Contracts;

namespace Microsoft.AspNetCore.Builder;

public static partial class WebApplicationBuilderExtensions
{
    public static void AddTestProjectServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.AddServerWebProjectServices();

        services.AddScoped<IStorageService, TestStorageService>();
        services.AddTransient<IAuthTokenProvider, TestAuthTokenProvider>();
        services.AddTransient<GoogleRecaptchaHttpClient, FakeGoogleRecaptchaHttpClient>();

        services.AddTransient(sp =>
        {
            return new HttpClient(sp.GetRequiredService<HttpMessageHandler>())
            {
                BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>().GetServerAddress(), UriKind.Absolute)
            };
        });
    }
}
