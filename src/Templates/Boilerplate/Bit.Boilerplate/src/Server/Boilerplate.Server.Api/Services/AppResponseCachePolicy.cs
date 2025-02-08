//+:cnd:noEmit
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// An implementation of this interface can update how the current request is cached.
/// </summary>
public class AppResponseCachePolicy(IHostEnvironment env) : IOutputCachePolicy
{
    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the cache middleware is invoked.
    /// At that point the cache middleware can still be enabled or disabled for the request.
    /// </summary>
    public async ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var responseCacheAtt = context.HttpContext.GetResponseCacheAttribute();

        if (responseCacheAtt is null)
            return;

        context.AllowLocking = true;
        context.EnableOutputCaching = true;
        context.CacheVaryByRules.QueryKeys = "*";
        if (CultureInfoManager.MultilingualEnabled)
        {
            context.CacheVaryByRules.VaryByValues.Add("Culture", CultureInfo.CurrentUICulture.Name);
        }

        var requestUrl = new Uri(context.HttpContext.Request.GetUri().GetUrlWithoutCulture()).PathAndQuery;

        if (responseCacheAtt.SharedMaxAge == -1)
        {
            responseCacheAtt.SharedMaxAge = responseCacheAtt.MaxAge;
        }

        var browserCacheTtl = responseCacheAtt.MaxAge;
        var edgeCacheTtl = responseCacheAtt.SharedMaxAge;
        var outputCacheTtl = responseCacheAtt.SharedMaxAge;

        if (env.IsDevelopment())
        {
            // To enhance the developer experience, return from here to make it easier for developers to debug cacheable responses.
            edgeCacheTtl = -1;
            outputCacheTtl = -1;
            browserCacheTtl = -1;
        }

        if (context.HttpContext.User.IsAuthenticated() && responseCacheAtt.UserAgnostic is false)
        {
            // See UserAgnostic's comment.
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

        if (context.HttpContext.Request.IsFromCDN() && edgeCacheTtl > 0)
        {
            // The origin backend is hosted behind a CDN, so there's no need to use both output caching and edge caching simultaneously.
            outputCacheTtl = -1;
        }

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
        if (outputCacheTtl > 0)
        {
            context.Tags.Add(requestUrl);
            context.AllowCacheLookup = true;
            context.AllowCacheStorage = true;
            context.ResponseExpirationTimeSpan = TimeSpan.FromSeconds(outputCacheTtl);
        }

        //#if (api == "Integrated")
        context.HttpContext.Items["AppResponseCachePolicy__DisableStreamPrerendering"] = outputCacheTtl > 0 || edgeCacheTtl > 0;
        //#endif
        context.HttpContext.Response.Headers.TryAdd("App-Cache-Response", FormattableString.Invariant($"Output:{outputCacheTtl},Edge:{edgeCacheTtl},Browser:{browserCacheTtl}"));
    }

    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the cached response is used.
    /// At that point the freshness of the cached response can be updated.
    /// </summary>
    public async ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
    {

    }

    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the response is served and can be cached.
    /// At that point cacheability of the response can be updated.
    /// </summary>
    public async ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var response = context.HttpContext.Response;

        if (response.GetTypedHeaders().SetCookie.Any(sc => sc.Name != CookieRequestCultureProvider.DefaultCookieName /* The culture cookie is allowed since caching varies by culture in CacheRequestAsync. */)
            || response.StatusCode is not StatusCodes.Status200OK)
        {
            context.AllowCacheStorage = false;
        }
    }
}
