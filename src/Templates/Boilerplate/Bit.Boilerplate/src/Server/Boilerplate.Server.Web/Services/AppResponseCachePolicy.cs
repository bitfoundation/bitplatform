//+:cnd:noEmit
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Server.Web.Services;

/// <summary>
/// An implementation of this interface can update how the current request is cached.
/// </summary>
public class AppResponseCachePolicy(IHostEnvironment env, ServerWebSettings settings) : IOutputCachePolicy
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
        if (CultureInfoManager.EnglishUSOnly is false)
        {
            context.CacheVaryByRules.VaryByValues.Add("Culture", CultureInfo.CurrentUICulture.Name);
        }

        var requestUrl = new Uri(context.HttpContext.Request.GetUri().GetUrlWithoutCulture()).PathAndQuery;

        if (responseCacheAtt.SharedMaxAge == -1)
        {
            responseCacheAtt.SharedMaxAge = responseCacheAtt.MaxAge;
        }

        var clientCacheTtl = responseCacheAtt.MaxAge;
        var edgeCacheTtl = responseCacheAtt.SharedMaxAge;
        var outputCacheTtl = responseCacheAtt.SharedMaxAge;

        if (settings.ResponseCaching?.EnableCdnEdgeCaching is false)
        {
            edgeCacheTtl = -1;
        }
        if (settings.ResponseCaching?.EnableOutputCaching is false)
        {
            outputCacheTtl = -1;
        }
        if (env.IsDevelopment())
        {
            clientCacheTtl = -1;
        }

        if (context.HttpContext.User.IsAuthenticated() && responseCacheAtt.UserAgnostic is false)
        {
            // See UserAgnostic's comment.
            edgeCacheTtl = -1;
            outputCacheTtl = -1;
        }

        if (context.HttpContext.IsBlazorPageContext() && CultureInfoManager.EnglishUSOnly is false)
        {
            // Note: Currently, we are not keeping the current culture in the URL. 
            // The edge and client caches do not support such variations, although the output cache does. 
            // As a temporary solution, client and edge caching are disabled for pre-rendered pages.
            edgeCacheTtl = -1;
            clientCacheTtl = -1;
        }

        if (context.HttpContext.Request.IsLightHouseRequest())
        {
            edgeCacheTtl = -1;
            outputCacheTtl = -1;
        }

        // Edge - Client Cache
        if (clientCacheTtl != -1 || edgeCacheTtl != -1)
        {
            context.HttpContext.Response.GetTypedHeaders().CacheControl = new()
            {
                Public = edgeCacheTtl > 0,
                Private = edgeCacheTtl <= 0,
                MaxAge = clientCacheTtl == -1 ? null : TimeSpan.FromSeconds(clientCacheTtl),
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

        context.HttpContext.Items["AppResponseCachePolicy__DisableStreamPrerendering"] = outputCacheTtl > 0 || edgeCacheTtl > 0;
        context.HttpContext.Response.Headers.TryAdd("App-Cache-Response", FormattableString.Invariant($"Output:{outputCacheTtl},Edge:{edgeCacheTtl},Client:{clientCacheTtl}"));
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
