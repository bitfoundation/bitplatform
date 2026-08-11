using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;

namespace Boilerplate.Server.Shared.Infrastructure.Services;

/// <summary>
/// <para>
/// ASP.NET Core's antiforgery asks for a token on <strong>every</strong> component render, not only on pages that
/// contain a form: <c>RazorComponentEndpointInvoker</c> calls <see cref="IAntiforgery.GetAndStoreTokens"/> itself. That
/// call makes the response uncacheable in two ways, even for an <strong>anonymous</strong> visitor: it writes
/// <c>Cache-Control: no-cache, no-store</c> plus <c>Pragma: no-cache</c> <strong>unconditionally</strong>, and on the
/// first request of a client that holds no antiforgery cookie yet it also sends a <c>Set-Cookie</c> - which
/// <see cref="AppResponseCachePolicy"/> treats as "belongs to this caller alone" and refuses to store.
/// </para>
/// <para>
/// So this wrapper is what makes a pre-rendered public page shared-cacheable at all. On a request where CDN edge or
/// output caching is on it takes the <see cref="IAntiforgery.GetTokens"/> path, which writes none of those three
/// headers.
/// </para>
/// <para>
/// The trade it makes, deliberately: the request token handed out on that path is bound to a cookie token that was
/// never sent, so it can only validate for a caller who already holds an antiforgery cookie from some other,
/// uncached response. An antiforgery-validated POST from a first-time visitor therefore fails <strong>closed</strong>
/// with a 400 rather than being accepted unchecked - <see cref="IAntiforgery.ValidateRequestAsync"/> and
/// <see cref="IAntiforgery.IsRequestValidAsync"/> are passed through untouched. Nothing in the template posts a
/// server-rendered form (no <c>@formname</c> / <c>AntiforgeryToken</c> anywhere), so this costs nothing today; a page
/// that adds one must not also carry <c>[AppResponseCache]</c>.
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
            return originalAntiforgeryImplementation.GetTokens(httpContext); // No cookie, and no no-store/Pragma headers either. See the trade in this class's summary.

        return originalAntiforgeryImplementation.GetAndStoreTokens(httpContext);
    }

    public Task ValidateRequestAsync(HttpContext httpContext) => originalAntiforgeryImplementation.ValidateRequestAsync(httpContext);
}
