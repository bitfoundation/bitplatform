//+:cnd:noEmit
using Microsoft.AspNetCore.OutputCaching;
using Boilerplate.Server.Shared.Infrastructure.Services;

namespace Boilerplate.Server.Api.Infrastructure.Services;

/// <summary>
/// 
/// The default Boilerplate project template includes:
/// 1. `Static` file caching on browsers and CDN edge servers.
/// 2. Caching JSON and dynamic files responses on CDN edge servers and ASP.NET Core's Output Cache by using `AppResponseCache` attribute in controllers like `StatisticsController`, `AttachmentController` and minimal apis.
/// 3. Caching pre-rendered HTML results of Blazor pages on CDN edge servers and ASP.NET Core's Output by using `AppResponseCache` attribute in pages like HomePage.razor
/// 
/// - Note: Both the output cache and the CDN edge cache are purged by tag, and the tag is the request path alone
///   (See <see cref="AppResponseCachePolicy.CreateCacheTag(string)"/>). So the path passed to <see cref="PurgeCache(string[])"/>
///   must match the request path exactly, while its query string is irrelevant: every variant of the path is purged at once.
/// </summary>
public partial class ResponseCacheService
{
    // Required by AddHttpClient<ResponseCacheService> even when no CDN is configured: the typed client factory only
    // knows how to build this service through a constructor that takes an HttpClient.
    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private IOutputCacheStore outputCacheStore = default!;
    //#if (cloudflare == true)
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;
    //#else
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    //#endif

    public async Task PurgeCache(params string[] relativePaths)
    {
        // Both the output cache entry and the CDN edge entry are stored under this tag (See AppResponseCachePolicy).
        var tags = relativePaths.Select(AppResponseCachePolicy.CreateCacheTag).Distinct().ToArray();

        foreach (var tag in tags)
        {
            await outputCacheStore.EvictByTagAsync(tag, default);
        }
        //#if (cloudflare == true)
        await PurgeCloudflareCache(tags);
        //#else
        // If you're using CDN like GCore or others, make sure to purge the Edge Cache of your CDN.
        // The Cloudflare Cache API is already integrated into the Boilerplate, but for other CDNs,
        // you'll need to implement the caching logic yourself. AppResponseCachePolicy already stamps every edge
        // cacheable response with the tag to purge it by, under Cloudflare's `Cache-Tag` header name; other CDNs read
        // the same idea from a different header (Fastly's `Surrogate-Key`, Akamai's `Edge-Cache-Tag`, ...).
        if (httpContextAccessor.HttpContext!.Request.IsFromCDN())
        {
            throw new NotImplementedException();
        }
        //#endif
    }

    //#if (module == "Sales" || module == "Admin")
    public async Task PurgeProductCache(int shortId)
    {
        await PurgeCache(PageUrls.Home, $"{PageUrls.Product}/{shortId}", $"/api/v1/ProductView/Get/{shortId}");
    }
    public async Task PurgeHomePage()
    {
        await PurgeCache(PageUrls.Home);
    }
    //#endif

    //#if (cloudflare == true)
    /// <summary>
    /// Purges the edge cache by cache-tag, the way the output cache is purged. <see cref="AppResponseCachePolicy"/>
    /// stamps every edge cacheable response with a <c>Cache-Tag</c> header carrying the same tag, so one call here
    /// invalidates the page under every query string it was cached with and on every hostname of the zone - neither of
    /// which a purge by URL could do, since the edge keeps a separate entry per full URL.
    /// Purge by tag is available on all Cloudflare plans, including the free one.
    /// </summary>
    private async Task PurgeCloudflareCache(string[] tags)
    {
        if (serverApiSettings?.Cloudflare?.Configured is not true)
            return;

        var apiToken = serverApiSettings.Cloudflare.ApiToken;

        foreach (var zoneId in serverApiSettings.Cloudflare.ZoneIds)
        {
            // Cloudflare accepts at most 100 tags per purge call.
            foreach (var tagsChunk in tags.Chunk(100))
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{zoneId}/purge_cache");
                request.Headers.Add("Authorization", $"Bearer {apiToken}");
                request.Content = JsonContent.Create(new { tags = tagsChunk });
                using var response = await httpClient.SendAsync(request);

                // The status code alone does not say whether the purge happened: Cloudflare reports that in the body's
                // `success` flag and gives the reason in `errors`. A purge that quietly did nothing is the worst
                // outcome here, since the stale page then sits on the edge until it expires on its own.
                if (response.IsSuccessStatusCode is false)
                    throw new InvalidOperationException($"Cloudflare cache purge of zone '{zoneId}' failed with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

                var result = await response.Content.ReadFromJsonAsync(jsonSerializerOptions.GetTypeInfo<CloudflarePurgeResponse>());

                if (result?.Success is not true)
                {
                    var errors = result?.Errors?.Select(err => $"{err.Code}: {err.Message}") ?? [];
                    throw new InvalidOperationException($"Cloudflare cache purge of zone '{zoneId}' was rejected. {string.Join(" | ", errors)}".Trim());
                }
            }
        }
    }
    //#endif
}
