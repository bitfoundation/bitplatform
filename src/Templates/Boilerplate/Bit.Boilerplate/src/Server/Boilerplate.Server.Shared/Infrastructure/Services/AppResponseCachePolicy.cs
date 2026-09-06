//+:cnd:noEmit
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Server.Shared.Infrastructure.Services;

/// <summary>
/// An implementation of this interface can update how the current request is cached.
/// </summary>
public partial class AppResponseCachePolicy(IHostEnvironment env, ServerSharedSettings settings) : IOutputCachePolicy
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
    /// Fills <see cref="AppResponseCacheAttribute.CacheTagTemplate"/>'s <c>{routeValue}</c> placeholders from
    /// <paramref name="httpContext"/>. Usable here because the output cache middleware runs after routing, so this tag
    /// reaches the output cache as well as the CDN. A placeholder naming a route value the request does not carry
    /// leaves the template unusable, and the caller falls back to the path.
    /// </summary>
    public static string? CreateCacheTagFromTemplate(HttpContext httpContext, string template)
    {
        var routeValues = httpContext.Request.RouteValues;
        var unresolved = false;

        var tag = CacheTagPlaceholder().Replace(template, match =>
        {
            if (routeValues.TryGetValue(match.Groups["name"].Value, out var value) && value is not null)
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

            unresolved = true;
            return "";
        });

        return unresolved ? null : tag.ToLowerInvariant().Replace(",", "%2c");
    }

    [GeneratedRegex(@"\{(?<name>[^{}]+)\}")]
    private static partial Regex CacheTagPlaceholder();

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

        // Only a page's body is culture dependent - its text is translated and its direction flips. An api response
        // carries data, and anything culture shaped in it is formatted by whoever renders it (See
        // ProductDto.FormattedPrice), so varying the api by culture would split every entry eleven ways to store
        // eleven identical bodies.
        if (CultureInfoManager.InvariantGlobalization is false && context.HttpContext.IsBlazorPageContext())
        {
            context.CacheVaryByRules.VaryByValues.Add("Culture", FormattableString.Invariant($"{CultureInfo.CurrentCulture.Name}|{CultureInfo.CurrentUICulture.Name}"));
        }

        //#if (multitenant == true)
        // An authenticated request resolves its tenant from the user's claim rather than from the host (See TenantProvider),
        // and tenant scoped entities are filtered by that tenant (See AppDbContext.ConfigureTenantAwareEntity). Without this
        // rule two users of different tenants on the same host would share a single entry, so a UserAgnostic endpoint like
        // ProductViewController would serve one tenant's rows to another.
        var currentTenantId = context.HttpContext.User.GetTenantId();
        if (currentTenantId is not null)
        {
            context.CacheVaryByRules.VaryByValues.Add("Tenant", currentTenantId.Value.ToString());
        }
        //#endif

        // The bare path, with neither the culture nor the query string in it, because ResponseCacheService.PurgeCache is
        // always called with bare paths ("/product/5") while the dimensions above each give a request its own entry:
        // QueryKeys = "*" splits by query string, VaryByValues["Culture"] splits by culture, and /fa-IR/product/5 is a
        // different path from /en-US/product/5 to begin with. Tagging an entry with any of that would leave
        // /product/5?utm_source=x - or the whole Persian half of the site - unpurgeable for the rest of its lifetime.
        // One tag for all of them means one purge clears every variant of the page, which is the point.
        var cacheTag = (responseCacheAtt.CacheTagTemplate is not null ? CreateCacheTagFromTemplate(context.HttpContext, responseCacheAtt.CacheTagTemplate) : null)
                       ?? CreateCacheTag(new Uri(context.HttpContext.Request.GetUri().GetUrlWithoutCulture()).AbsolutePath);

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
        if (responseCacheAtt.SkipOutputCache)
        {
            // A file response (See AttachmentController.GetAttachment): too big for the byte-bounded memory cache the
            // output cache stores bodies in. The edge keeps caching it. See SkipOutputCache.
            outputCacheTtl = -1;
        }
        if (env.IsDevelopment())
        {
            clientCacheTtl = -1;
        }

        if (context.HttpContext.User.IsAuthenticated() && responseCacheAtt.UserAgnostic is false)
        {
            // See UserAgnostic's comment. The private caches are no more per-user than the shared ones: one browser
            // profile and one running app each span every user who signs in on that device, and both key on the URL.
            // So a body that depends on who asked for it may not carry a client max-age either.
            edgeCacheTtl = -1;
            outputCacheTtl = -1;
            clientCacheTtl = -1;
        }

        //#if (multitenant == true)
        if (currentTenantId is not null)
        {
            // The Tenant rule above is a VaryByValues entry, and that is an output cache concept: it never becomes a
            // response header, so the output cache is the only cache that can see it. Every other cache keys on the
            // URL, which for an authenticated caller is identical across tenants - a CDN would hand tenant A's body
            // to tenant B, and the browser's own cache would replay it to whoever signs in next on that profile,
            // across a restart. Anonymous callers are unaffected: their tenant comes from the host, so it is already
            // in the URL, and that is the traffic these caches exist for.
            clientCacheTtl = -1;
            edgeCacheTtl = -1;
        }
        //#endif

        if (context.HttpContext.IsBlazorPageContext() && CultureInfoManager.InvariantGlobalization is false
            && context.HttpContext.Request.GetUri().GetCulture() is null)
        {
            // A page url naming no culture renders in whatever the cookie or Accept-Language resolved to - dimensions
            // the browser cache and CDN edge cannot key on. Server.Web's UseCultureUrlRedirection normally 302s such
            // requests onto /{culture}/... before they get here; this is the backstop for the ones it lets through
            // (the `no-prerender` app-shell request). The output cache is unaffected: it varies by culture itself.
            edgeCacheTtl = -1;
            clientCacheTtl = -1;
        }

        if (context.HttpContext.IsBlazorPageContext() &&
            (context.HttpContext.Request.IsLightHouseRequest() || context.HttpContext.Request.IsCrawlerClient()))
        {
            // These callers get a DIFFERENT document: App.razor omits every script tag for them, so a benchmark is not
            // charged for the blazor bundle and a crawler is not handed one it has no use for. Nothing in the cache key
            // varies by user agent (See CacheVaryByRules above), so storing that response would hand the next ordinary
            // visitor a page with no scripts - a shell that can never boot. Their responses are therefore theirs alone:
            // never written to the output cache, never offered to a CDN.
            // The page check is part of the condition because the script omission only happens in App.razor: every other
            // cacheable endpoint - the sitemaps, llms.txt, products.xml, the api - produces the same bytes for every
            // user agent, and those are the documents crawlers request most, so excluding them would mean the one caller
            // the 7 day sitemap cache exists for is the one caller never served from it.
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

        if (clientCacheTtl == -1 && edgeCacheTtl == -1 && outputCacheTtl == -1)
        {
            // Neither block below runs for this response, so nothing would tell caches anything at all - while one of
            // the reasons above may be that it must not be SHARED (an authenticated caller on an endpoint that is not
            // UserAgnostic), and a directive-less 200 is free to be stored by a shared cache on the way out. When a
            // client ttl IS set, the header below already says `private` next to its max-age, so this covers only the
            // case where no Cache-Control is written at all.
            context.HttpContext.Response.GetTypedHeaders().CacheControl = new() { Private = true };
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

                response.Headers.Remove("Pragma");

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
    /// The culture cookie is exempt: the output cache varies by culture, and an edge entry only ever lives under a
    /// culture-prefixed url (See Server.Web's <c>UseCultureUrlRedirection</c>) - a replayed culture Set-Cookie names
    /// that url's own culture, the value the origin would have written for any caller.
    /// </summary>
    private static bool IsResponseCacheable(HttpResponse response)
    {
        return response.StatusCode is StatusCodes.Status200OK
            && response.GetTypedHeaders().SetCookie.Any(sc => sc.Name != CookieRequestCultureProvider.DefaultCookieName) is false;
    }
}
