using Microsoft.AspNetCore.Antiforgery;

namespace Boilerplate.LandingPage.Server.Infrastructure.Services;

// [mirror] src/Server/Boilerplate.Server.Shared/Infrastructure/Services/SharedResponseCacheCompatibleAntiforgery.cs
public sealed class LandingAntiforgery(IServiceProvider serviceProvider) : IAntiforgery
{
    private readonly IAntiforgery original = (IAntiforgery)ActivatorUtilities.CreateInstance(serviceProvider, typeof(IAntiforgery).Assembly.GetType("Microsoft.AspNetCore.Antiforgery.DefaultAntiforgery", throwOnError: true)!);

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => original.GetTokens(httpContext);

    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => original.GetTokens(httpContext);

    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => original.IsRequestValidAsync(httpContext);

    public Task ValidateRequestAsync(HttpContext httpContext) => original.ValidateRequestAsync(httpContext);

    public void SetCookieTokenAndHeader(HttpContext httpContext) { }
}
