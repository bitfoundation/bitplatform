//+:cnd:noEmit
using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// 
/// The default Boilerplate project template includes:
/// 1. `Static` file caching on browsers and CDN edge servers.
/// 2. Caching JSON and dynamic responses on CDN edge servers and ASP.NET Core's Output Cache by using `AppResponseCache` attribute in controllers like `StatisticsController` and `AttachmentController`
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

        await PurgeCloudflareCache(relativePaths);
    }

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
}
