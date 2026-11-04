// [mirror] cache-friendly antiforgery - keep in sync with:
// - src/Server/Boilerplate.Server.Shared/Infrastructure/Services/SharedResponseCacheCompatibleAntiforgery.cs
using Microsoft.AspNetCore.Antiforgery;

namespace Boilerplate.LandingPage.Server.Infrastructure.Services;

public sealed class LandingAntiforgery(IServiceProvider serviceProvider) : IAntiforgery
{
    private readonly IAntiforgery original = (IAntiforgery)ActivatorUtilities.CreateInstance(serviceProvider, typeof(IAntiforgery).Assembly.GetType("Microsoft.AspNetCore.Antiforgery.DefaultAntiforgery", throwOnError: true)!);

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => original.GetTokens(httpContext);

    // Every page render asks for tokens, and storing them would add the cookie and no-store headers that keep a CDN out
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => original.GetTokens(httpContext);

    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => original.IsRequestValidAsync(httpContext);

    public Task ValidateRequestAsync(HttpContext httpContext) => original.ValidateRequestAsync(httpContext);

    public void SetCookieTokenAndHeader(HttpContext httpContext) { }
}
