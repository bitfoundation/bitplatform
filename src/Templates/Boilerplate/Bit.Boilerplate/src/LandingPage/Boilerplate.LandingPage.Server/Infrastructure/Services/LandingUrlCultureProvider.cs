using Microsoft.AspNetCore.Localization;

namespace Boilerplate.LandingPage.Server.Infrastructure.Services;

public sealed class LandingUrlCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? "/";

        if (httpContext.Request.Path.StartsWithSegments("/_blazor")
            && Uri.TryCreate(httpContext.Request.Headers.Referer, UriKind.Absolute, out var referer))
        {
            path = referer.AbsolutePath;
        }

        var (culture, _) = LandingCultures.Split(path);

        return Task.FromResult<ProviderCultureResult?>(new(culture.Name));
    }
}
