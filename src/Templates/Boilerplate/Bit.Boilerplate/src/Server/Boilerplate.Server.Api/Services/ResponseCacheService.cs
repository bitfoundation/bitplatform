//+:cnd:noEmit
using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// 
/// The default Boilerplate project template includes:
/// 1. `Static` file caching on browsers and CDN edge servers.
/// 2. Caching JSON and dynamic files responses on CDN edge servers and ASP.NET Core's Output Cache by using `AppResponseCache` attribute in controllers like `StatisticsController`, `AttachmentController` and minimal apis.
/// 3. Caching pre-rendered HTML results of Blazor pages on CDN edge servers and ASP.NET Core's Output by using `AppResponseCache` attribute in pages like HomePage.razor
/// 
/// - Note: The request URL must exactly match the URL passed to <see cref="PurgeCache(string[])"/> for successful purging.  
/// </summary>
public partial class ResponseCacheService
{
    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private IOutputCacheStore outputCacheStore = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    public async Task PurgeCache(params string[] relativePaths)
    {
        foreach (var relativePath in relativePaths)
        {
            await outputCacheStore.EvictByTagAsync(relativePath, default);
        }
        //#if (cloudflare == true)
        await PurgeCloudflareCache(relativePaths);
        //#else
        // If you're using CDNs like GCore or others, make sure to purge the Edge Cache of your CDN.
        // The Cloudflare Cache API is already integrated into the Boilerplate, but for other CDNs, 
        // you'll need to implement the caching logic yourself.
        if (httpContextAccessor.HttpContext!.Request.IsFromCDN())
        {
            throw new NotImplementedException();
        }
        //#endif
    }

    //#if (cloudflare == true)
    private async Task PurgeCloudflareCache(string[] relativePaths)
    {
        if (serverApiSettings?.Cloudflare?.Configured is not true)
            return;

        var zoneId = serverApiSettings.Cloudflare.ZoneId;
        var apiToken = serverApiSettings.Cloudflare.ApiToken;

        var files = serverApiSettings.Cloudflare.AdditionalDomains
            .Union([httpContextAccessor.HttpContext!.Request.GetBaseUrl()])
            .SelectMany(baseUri => relativePaths.Select(path => new Uri(baseUri, path)))
            .ToArray();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{zoneId}/purge_cache");
        request.Headers.Add("Authorization", $"Bearer {apiToken}");
        request.Content = JsonContent.Create(new { files });
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
    //#endif
}
