//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.OAuth;
using Boilerplate.Server.Api.Features.Identity.OAuth.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class OAuthServiceCollectionExtensions
{
    public static IServiceCollection AddOAuth(this IServiceCollection services)
    {
        services.AddScoped<OAuthService>();
        services.AddScoped<OAuthTokenService>();
        services.AddScoped<OAuthClientResolver>();
        services.AddScoped<OAuthRetentionJobRunner>();

        // A metadata document comes from a host we do not control, so its timeout and body limit are written down.
        services.AddHttpClient(OAuthClientResolver.HttpClientName, httpClient =>
        {
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            httpClient.MaxResponseContentBufferSize = 64 * 1024;
        })
        // Redirects off: the destination is checked before the request, and following one arrives somewhere unchecked.
        // Configuring the shared handler rather than replacing it keeps ConfigureHttpClientDefaults' TLS floor here.
        .UseSocketsHttpHandler((handler, _) => handler.AllowAutoRedirect = false);

        return services;
    }
}
