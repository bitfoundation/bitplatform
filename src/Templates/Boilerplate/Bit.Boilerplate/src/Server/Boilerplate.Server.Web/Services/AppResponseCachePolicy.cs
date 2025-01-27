//+:cnd:noEmit
using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Server.Web.Services;

public class AppResponseCachePolicy(IHostEnvironment env) : IOutputCachePolicy
{
    public async ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var responseCacheAtt = context.HttpContext.GetResponseCacheAttribute();

        if (responseCacheAtt is null)
        {
            context.EnableOutputCaching = false;
            return;
        }

        if (responseCacheAtt.MaxAge == -1 && responseCacheAtt.SharedMaxAge == -1)
            throw new InvalidOperationException("Invalid configuration: Both MaxAge and SharedMaxAge are unset. At least one of them must be specified in the ResponseCache attribute.");

        var requestUrl = new Uri(context.HttpContext.Request.GetUri().GetUrlWithoutCulture()).PathAndQuery;

        if (responseCacheAtt.SharedMaxAge == -1)
        {
            responseCacheAtt.SharedMaxAge = responseCacheAtt.MaxAge;
        }

        var browserCacheTtl = responseCacheAtt.MaxAge;
        var edgeCacheTtl = responseCacheAtt.SharedMaxAge;
        var outputCacheTtl = responseCacheAtt.SharedMaxAge;

        if (context.HttpContext.User.IsAuthenticated() && responseCacheAtt.UserAgnostic is false)
        {
            edgeCacheTtl = -1;
        }

        if (responseCacheAtt.ResourceKind is Shared.Attributes.ResourceKind.Page || CultureInfoManager.MultilingualEnabled)
        {
            // Note: Edge caching for page responses is not supported when `CultureInfoManager.MultilingualEnabled` is enabled.
            edgeCacheTtl = -1;
        }

        if (browserCacheTtl != -1 || edgeCacheTtl != -1)
        {
            context.HttpContext.Response.GetTypedHeaders().CacheControl = new()
            {
                Public = edgeCacheTtl > 0,
                MaxAge = browserCacheTtl == -1 ? null : TimeSpan.FromSeconds(browserCacheTtl),
                SharedMaxAge = edgeCacheTtl == -1 ? null : TimeSpan.FromSeconds(edgeCacheTtl)
            };
            context.HttpContext.Response.Headers.Remove("Pragma");
        }

        if (env.IsDevelopment() // To enhance the developer experience, return here to make it easier for developers to debug cacheable pages.
            || outputCacheTtl == -1)
        {
            context.EnableOutputCaching = false;
            return;
        }

        context.Tags.Add(requestUrl);
        context.EnableOutputCaching = true;
        context.ResponseExpirationTimeSpan = TimeSpan.FromSeconds(outputCacheTtl);

        if (CultureInfoManager.MultilingualEnabled)
        {
            context.CacheVaryByRules.VaryByValues.Add("Culture", CultureInfo.CurrentUICulture.Name);
        }
    }

    public async ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }

    public async ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }
}
