using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Server.Web.Services;

public class AppResponseCachePolicy(IHostEnvironment env, ILogger<AppResponseCachePolicy> logger) : IOutputCachePolicy
{
    public async ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var responseCacheAtt = context.HttpContext.GetResponseCacheAttribute();

        if (responseCacheAtt is null || context.HttpContext.User.IsAuthenticated() is true)
        {
            context.EnableOutputCaching = false;
            return;
        }

        var requestUrl = new Uri(context.HttpContext.Request.GetUri().GetUrlWithoutCulture()).PathAndQuery;

        if (env.IsDevelopment())
        {
            context.EnableOutputCaching = false;
            logger.LogInformation("In production, the result response of {Url} url would be cached.", requestUrl);
            return; // To enhance the developer experience, return here to make it easier for developers to debug cacheable pages.
        }

        var duration = TimeSpan.FromSeconds(responseCacheAtt.MaxAge > 0 ? responseCacheAtt.MaxAge : responseCacheAtt.SharedMaxAge);

        context.ResponseExpirationTimeSpan = duration;
        context.Tags.Add(requestUrl);

        //#if (cloudflare == true)
        if (responseCacheAtt.ResourceKind is Shared.Attributes.ResourceKind.Page &&
            CultureInfoManager.MultilingualEnabled)
        {
            responseCacheAtt.SharedMaxAge = 0; // Edge caching for page responses is not supported when `CultureInfoManager.MultilingualEnabled` is set to `true`.
        }
        //#endif

        context.HttpContext.Response.GetTypedHeaders().CacheControl = new()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(responseCacheAtt.MaxAge),
            //#if (cloudflare == true)
            SharedMaxAge = TimeSpan.FromSeconds(responseCacheAtt.SharedMaxAge)
            //#endif
        };
        context.HttpContext.Response.Headers.Remove("Pragma");
    }

    public async ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }

    public async ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }
}
