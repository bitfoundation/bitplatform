//+:cnd:noEmit
using Microsoft.AspNetCore.OutputCaching;
//#if (module == "Admin")
using Boilerplate.Server.Api.Controllers.Dashboard;
//#endif

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// 
/// The default Boilerplate project template includes:
/// 1. `Static` file caching on browsers and CDN edge servers by setting `MaxAge` in `Server.Web\Program.Middlewares.cs`.
/// 2. `Dynamic` file caching on browsers and CDN edge servers by setting `MaxAge` in `AttachmentController`.
/// 3. Caching JSON responses on CDN edge servers by setting `SharedMaxAge` in controllers like `DashboardController`
/// 4. ASP.NET Core Output Caching of pre-rendered HTML results for Blazor pages. See `BlazorOutputCachePolicy`
/// 5. Caching pre-rendered HTML responses of blazor pages on CDN edge servers by setting `SharedMaxAge` in `Server.Web\Program.Middlewares.cs`.
/// 
/// ### Key Notes on `MaxAge` vs `SharedMaxAge` while setting `CacheControl`
/// 
/// **MaxAge**  
/// - Provides better performance by caching in both the browser and CDN.  
/// - Use cautiously, as browser caches cannot be purged programmatically. To mitigate this, append pre-calculated values (e.g., the file's hash or a DTO's concurrency stamp) as a `?v=` query string in the URL.  
/// 
/// **SharedMaxAge**  
/// - Restricts caching to edge servers (currently only supported by Cloudflare) without browser caching.  
/// - Cache entries can be purged programmatically using <see cref="PurgeCloudflareCache(string[])"/> or manually via the Cloudflare dashboard or CI/CD tasks (e.g., jakejarvis/cloudflare-purge-action).  
/// 
/// - Note: The request URL must exactly match the URL passed to <see cref="PurgeCache(string[])"/> for successful purging.  
/// </summary>
public partial class ResponseCacheService
{
    //#if (module == "Admin")
    [AutoInject] private IUrlHelper urlHelper = default!;
    //#endif
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

    //#if (module == "Admin")
    public string[] GetDashboardPurgeUrls()
    {
        var dashboardController = nameof(DashboardController).Replace("Controller", "");
        return [urlHelper.Action(nameof(DashboardController.GetOverallAnalyticsStatsData), dashboardController)!,
                urlHelper.Action(nameof(DashboardController.GetProductsCountPerCategoryStats), dashboardController)!,
                urlHelper.Action(nameof(DashboardController.GetProductsPercentagePerCategoryStats), dashboardController)!];
    }
    //#endif
}
