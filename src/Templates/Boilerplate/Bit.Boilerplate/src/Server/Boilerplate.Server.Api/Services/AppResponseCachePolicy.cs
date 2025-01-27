//+:cnd:noEmit
using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Server.Api.Services;

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
            // See UserAgnostic comments.
            edgeCacheTtl = -1;
            outputCacheTtl = -1;
        }

        //#if (api == "Integrated")
        if (context.HttpContext.IsBlazorPageContext() && CultureInfoManager.MultilingualEnabled)
        {
            // Note: Currently, we are not keeping the current culture in the URL. 
            // The edge and browser caches do not support such variations, although the output cache does. 
            // As a temporary solution, browser and edge caching are disabled for pre-rendered pages.
            edgeCacheTtl = -1;
            browserCacheTtl = -1;
        }
        //#endif

        // Edge - Browser Cache
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

        // ASP.NET Core Output Cache
        if (env.IsDevelopment() is false // To enhance the developer experience, return from here to make it easier for developers to debug cacheable pages.
            && outputCacheTtl != -1)
        {
            context.Tags.Add(requestUrl);
            context.EnableOutputCaching = true;
            context.ResponseExpirationTimeSpan = TimeSpan.FromSeconds(outputCacheTtl);

            if (CultureInfoManager.MultilingualEnabled)
            {
                context.CacheVaryByRules.VaryByValues.Add("Culture", CultureInfo.CurrentUICulture.Name);
            }
        }
        else
        {
            context.EnableOutputCaching = false;
        }
    }

    public async ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }

    public async ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }
}
