using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;

namespace Boilerplate.Server.Shared.Infrastructure.Services;

/// <summary>
/// <para>
/// By design, the anti-forgery mechanism generates a validation cookie and sends a <c>Set-Cookie</c> header
/// for <strong>EVERY</strong> request, even for <strong>Anonymous (unauthenticated)</strong> users.
/// </para>
/// <para>
/// This presence of the <c>Set-Cookie</c> header forces CDNs to treat the response as private/dynamic,
/// effectively <strong>disabling caching completely</strong> for pre-rendered public pages and APIs.
/// </para>
/// <para>
/// This implementation wraps the default ASP.NET Core antiforgery service and prevents it from sending Set-Cookie
/// for requests where shared CDN/Output caching is enabled.
/// </para>
/// </summary>
public class SharedResponseCacheCompatibleAntiforgery : IAntiforgery
{
    private readonly IAntiforgery originalAntiforgeryImplementation;

    public SharedResponseCacheCompatibleAntiforgery(IServiceProvider serviceProvider)
        => originalAntiforgeryImplementation = (IAntiforgery)ActivatorUtilities.CreateInstance(serviceProvider, typeof(IAntiforgery).Assembly.GetType("Microsoft.AspNetCore.Antiforgery.DefaultAntiforgery", throwOnError: true)!);

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => originalAntiforgeryImplementation.GetTokens(httpContext);

    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => originalAntiforgeryImplementation.IsRequestValidAsync(httpContext);

    public void SetCookieTokenAndHeader(HttpContext httpContext)
    {
        if (httpContext.IsSharedResponseCacheEnabled())
            return; // Set-Cookie would prevent caching by CDNs

        originalAntiforgeryImplementation.SetCookieTokenAndHeader(httpContext);
    }

    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
    {
        if (httpContext.IsSharedResponseCacheEnabled())
            return originalAntiforgeryImplementation.GetTokens(httpContext); // Generate tokens without setting the cookie

        return originalAntiforgeryImplementation.GetAndStoreTokens(httpContext);
    }

    public Task ValidateRequestAsync(HttpContext httpContext) => originalAntiforgeryImplementation.ValidateRequestAsync(httpContext);
}
