//+:cnd:noEmit
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Server.Shared.Infrastructure.Services;

/// <summary>
/// An implementation of this interface can update how the current request is cached.
/// </summary>
public class AppResponseCachePolicy(IHostEnvironment env, ServerSharedSettings settings) : IOutputCachePolicy
{
    /// <summary>
    /// The header a CDN reads the cache-tags of a response from. Cloudflare associates them with the cached object
    /// and strips the header before the response reaches the visitor.
    /// </summary>
    public const string CacheTagHeaderName = "Cache-Tag";

    /// <summary>
    /// CDN rejects a longer cache-tag in a purge call.
    /// </summary>
    private const int MaxCacheTagLength = 1024;

    /// <summary>
    /// The tag both the ASP.NET Core output cache entry and the CDN edge entry are stored under, so that a single
    /// <c>ResponseCacheService.PurgeCache("/product/5")</c> invalidates both.
    /// </summary>
    public static string CreateCacheTag(string relativePath)
    {
        var path = new Uri(CacheTagBaseUri, relativePath).AbsolutePath;

        return path.ToLowerInvariant().Replace(",", "%2c");
    }

    /// <summary>
    /// Only there to let <see cref="CreateCacheTag"/> canonicalize a relative path through <see cref="Uri"/>; the host
    /// is never part of a tag.
    /// </summary>
    private static readonly Uri CacheTagBaseUri = new("http://localhost");

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
        context.CacheVaryByRules.VaryByHost = true;
        // Origin is here because the CORS middleware runs before the output cache middleware and writes an
        // Access-Control-Allow-Origin echoing the caller's Origin. The output cache stores and replays every response
        // header (it only skips Request-Id, Content-Length and Age), so without this rule the first caller's
        // Access-Control-Allow-Origin would be replayed to every other origin and their browsers would reject it.
        context.CacheVaryByRules.HeaderNames = new[] { HeaderNames.Origin, "X-Origin" };
        if (CultureInfoManager.InvariantGlobalization is false)
        {
            context.CacheVaryByRules.VaryByValues.Add("Culture", CultureInfo.CurrentUICulture.Name);
        }

        //#if (multitenant == true)
        // An authenticated request resolves its tenant from the user's claim rather than from the host (See TenantProvider),
        // and tenant scoped entities are filtered by that tenant (See AppDbContext.ConfigureTenantAwareEntity). Without this
        // rule two users of different tenants on the same host would share a single entry, so a UserAgnostic endpoint like
        // ProductViewController would serve one tenant's rows to another.
        if (context.HttpContext.User.GetTenantId() is Guid currentTenantId)
        {
            context.CacheVaryByRules.VaryByValues.Add("Tenant", currentTenantId.ToString());
        }
        //#endif

        // The bare path, with neither the culture nor the query string in it, because ResponseCacheService.PurgeCache is
        // always called with bare paths ("/product/5") while the dimensions above each give a request its own entry:
        // QueryKeys = "*" splits by query string, VaryByValues["Culture"] splits by culture, and /fa-IR/product/5 is a
        // different path from /en-US/product/5 to begin with. Tagging an entry with any of that would leave
        // /product/5?utm_source=x - or the whole Persian half of the site - unpurgeable for the rest of its lifetime.
        // One tag for all of them means one purge clears every variant of the page, which is the point.
        var cacheTag = CreateCacheTag(new Uri(context.HttpContext.Request.GetUri().GetUrlWithoutCulture()).AbsolutePath);

        var sharedMaxAge = responseCacheAtt.SharedMaxAge == -1 ? responseCacheAtt.MaxAge : responseCacheAtt.SharedMaxAge;

        var clientCacheTtl = responseCacheAtt.MaxAge;
        var edgeCacheTtl = sharedMaxAge;
        var outputCacheTtl = sharedMaxAge;

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

        if (context.HttpContext.IsBlazorPageContext() && CultureInfoManager.InvariantGlobalization is false)
        {
            // Note: Currently, we are not keeping the current culture in the URL.
            // The edge and client caches do not support such variations, although the output cache does.
            // As a temporary solution, client and edge caching are disabled for pre-rendered pages.
            edgeCacheTtl = -1;
            clientCacheTtl = -1;
        }

        if (context.HttpContext.Request.IsLightHouseRequest() || context.HttpContext.Request.IsCrawlerClient())
        {
            // These callers get a DIFFERENT document: App.razor omits every script tag for them, so a benchmark is not
            // charged for the blazor bundle and a crawler is not handed one it has no use for. Nothing in the cache key
            // varies by user agent (See CacheVaryByRules above), so storing that response would hand the next ordinary
            // visitor a page with no scripts - a shell that can never boot. Their responses are therefore theirs alone:
            // never written to the output cache, never offered to a CDN.
            edgeCacheTtl = -1;
            outputCacheTtl = -1;
        }

        if (cacheTag.Length > MaxCacheTagLength)
        {
            // The tag is what ResponseCacheService purges the edge entry by, and a tag this long cannot be passed to
            // the purge API, so the entry would stay on the edge until it expired on its own. An edge entry that can
            // never be invalidated is worse than no edge entry at all. The output cache is not affected: it holds the
            // whole tag and is purged in process.
            edgeCacheTtl = -1;
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
            // Note: a CDN may ignore this. Cloudflare, for one, does not consider Vary in caching decisions unless the
            // header is Accept-Encoding or a Cache Rules Vary setting naming origin/x-origin has been configured on the
            // zone. Without that rule the edge keeps a single variant per URL and hands it to callers of every origin.
            context.HttpContext.Response.Headers.Append(HeaderNames.Vary, "Origin, X-Origin");

            if (edgeCacheTtl > 0)
            {
                // What ResponseCacheService.PurgeCache purges the edge entry by. Unlike a purge by URL, a purge by tag
                // reaches every query string variant the URL was cached under and every hostname of the zone in a
                // single API call, which also keeps the purge within the rate limit of Cloudflare's free plan.
                context.HttpContext.Response.Headers[CacheTagHeaderName] = cacheTag;
            }

            context.HttpContext.Response.OnStarting(static state =>
            {
                var response = (HttpResponse)state;

                if (IsResponseCacheable(response) is false)
                {
                    response.GetTypedHeaders().CacheControl = new() { NoStore = true, Private = true };
                    // Nothing is going to be cached under it, and a CDN that does not consume the header would
                    // otherwise pass it on to the visitor.
                    response.Headers.Remove(CacheTagHeaderName);
                }

                return Task.CompletedTask;
            }, context.HttpContext.Response);
        }

        // ASP.NET Core Output Cache
        if (outputCacheTtl > 0)
        {
            context.Tags.Add(cacheTag);
            context.AllowCacheLookup = true;
            context.AllowCacheStorage = true;
            context.ResponseExpirationTimeSpan = TimeSpan.FromSeconds(outputCacheTtl);
        }

        var sharedCache = outputCacheTtl > 0 || edgeCacheTtl > 0;

        context.HttpContext.Items["AppResponseCachePolicy__SharedCacheEnabled"] = sharedCache;
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
        // Keeps the response out of ASP.NET Core's output cache. This runs after the endpoint has produced the response,
        // which is generally too late to touch the headers, but the middleware re-reads AllowCacheStorage when it goes to
        // store the body, so clearing it here still prevents the entry from being written. The matching Cache-Control
        // downgrade for browsers and CDNs is handled by the OnStarting callback registered in CacheRequestAsync.
        if (IsResponseCacheable(context.HttpContext.Response) is false)
        {
            context.AllowCacheStorage = false;
        }
    }

    /// <summary>
    /// A response that reports a failure or hands out cookies belongs to the caller that triggered it and may not be
    /// stored in any cache. Otherwise a 404 of a product that gets created a minute later stays alive on the edge for
    /// days, and one caller's cookies get replayed to everyone else.
    /// The culture cookie is exempt because <see cref="CacheRequestAsync"/> varies the cache by culture.
    /// </summary>
    private static bool IsResponseCacheable(HttpResponse response)
    {
        return response.StatusCode is StatusCodes.Status200OK
            && response.GetTypedHeaders().SetCookie.Any(sc => sc.Name != CookieRequestCultureProvider.DefaultCookieName) is false;
    }
}
