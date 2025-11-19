//+:cnd:noEmit
using Boilerplate.Tests.Services;
using Boilerplate.Client.Core.Services.Contracts;
using Boilerplate.Client.Core.Services.HttpMessageHandlers;

namespace Microsoft.AspNetCore.Builder;

public static partial class WebApplicationBuilderExtensions
{
    public static void AddTestProjectServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.AddServerWebProjectServices();

        // Register test-specific services for all tests here

        services.AddScoped<IStorageService, TestStorageService>();
        services.AddTransient<IAuthTokenProvider, TestAuthTokenProvider>();

        services.AddTransient<HttpClient>(sp =>
        {
            var handlerFactory = sp.GetRequiredService<HttpMessageHandlersChainFactory>();
            return new HttpClient(handlerFactory.Invoke())
            {
                BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>().GetServerAddress(), UriKind.Absolute)
            };
        });
    }
}
