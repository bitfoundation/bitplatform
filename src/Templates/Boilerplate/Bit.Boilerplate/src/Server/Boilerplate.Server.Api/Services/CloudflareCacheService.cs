//+:cnd:noEmit
using Boilerplate.Server.Api.Controllers;
//#if (module == "Admin")
using Boilerplate.Server.Api.Controllers.Dashboard;
//#endif

namespace Boilerplate.Server.Api.Services;

/// <summary>
/// To control HTTP response caching, you can use the following approaches within your API Controllers:
///
/// Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
/// {
///     Public = true,
///     MaxAge = TimeSpan.FromDays(1),
///     SharedMaxAge = TimeSpan.FromDays(7)
/// };
/// Sample: StatisticsController.cs
/// 
/// **In Razor Components**:
/// «CloudflareCache MaxAge="TimeSpan.FromDays(1)" SharedMaxAge="TimeSpan.FromDays(7)" Public="true" /»
/// Sample: TermsPage.razor
///
/// ### Caching Options
///
/// 1. **Cache on Both Cloudflare Edge and Browsers**:
///    - Set `MaxAge`
///     This provides better performance but should be used cautiously because browser caches cannot be purged programmatically from the server.
///     Example: <see cref="AttachmentController.GetProfileImage(Guid, CancellationToken)"/> uses this approach by appending
///         a user's concurrency stamp (which changes when the user updates their profile) as a `?v=` query string.
///         This makes it safe to cache the profile picture in both browser and Cloudflare caches.
///
/// 2. **Cache Only on Cloudflare Edge**:
///    - Set `SharedMaxAge` instead of `MaxAge`
///     This ensures caching only on edge servers(e.g., Cloudflare), without storing the response in browser caches.
///     You can later programmatically purge this cache entry using <see cref = "PurgeCache(string[])" /> or manually from Cloudflare dashboard or by jakejarvis/cloudflare-purge-action task in CI/CD pipeline.
///     
/// Note: The URL of the client's request must exactly match the URL provided to the Cloudflare purge cache API.
/// </summary>
public partial class CloudflareCacheService
{
    //#if (module == "Admin")
    [AutoInject] private IUrlHelper urlHelper = default!;
    //#endif
    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private ServerApiSettings serverApiSettings = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    public async Task PurgeCache(params string[] relativePaths)
    {
        if (serverApiSettings?.Cloudflare?.Configured is not true)
            return;

        var zoneId = serverApiSettings.Cloudflare.ZoneId;
        var apiToken = serverApiSettings.Cloudflare.ApiToken;

        var files = serverApiSettings.Cloudflare.AdditionalDomains
            .Union([httpContextAccessor.HttpContext!.Request.GetBaseUrl()])
            .SelectMany(baseUri => relativePaths.Select(path => new Uri(baseUri, path)))
            .ToArray();

        using HttpRequestMessage request = new(HttpMethod.Post, $"{zoneId}/purge_cache");
        request.Headers.Add("Authorization", $"Bearer {apiToken}");
        request.Content = JsonContent.Create(new { files });

        using HttpResponseMessage response = await httpClient.SendAsync(request);

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
