namespace Boilerplate.Tests.E2E.Features.ErrorPages;

/// <summary>
/// What a prerendering deployment answers before any WebAssembly runs - a crawler, a link preview, a browser with
/// scripts off. Server.Web's Handle40XStatusCodes redirects Blazor's bodiless 401/404 to NotAuthorizedPage /
/// NotFoundPage and keeps the status code on them.
/// <para>
/// AdminPanel is not among the rows: admin-sample.cd.yml does not set <c>WebAppRender.PrerenderEnabled</c>, so it
/// serves a shell and only the client router knows, once WebAssembly is up, that a route is missing or needs an
/// account. WebErrorPageTests covers it there.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class ErrorPageStatusCodeTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    [DataRow(App.Sales, DisplayName = "Sales (integrated API)")]
    public async Task UnknownRoute_Should_Answer404_WithTheNotFoundPage(App app)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        using var response = await httpClient.GetAsync(new Uri(new Uri(DeployedApps.AddressOf(app)), "/e2e/route/that-does-not-exist"), TestContext.CancellationToken);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, $"{app} answered {response.RequestMessage!.RequestUri}.");
        Assert.AreEqual(PageUrls.NotFound, PathWithoutCulture(response.RequestMessage!.RequestUri!), "The 404 is served from NotFoundPage's own url.");
        Assert.Contains(AppStrings.NotFoundText, html, $"{app}'s 404 carries no NotFoundPage markup - a status code with nothing to show.");
    }

    /// <summary>
    /// Status and destination only: NotAuthorizedPage prerenders a loader, because whether the visitor is signed out or
    /// merely lacks a feature is only known once a token refresh has been tried in the browser.
    /// </summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    [DataRow(App.Sales, DisplayName = "Sales (integrated API)")]
    public async Task SignedInOnlyPage_Should_Answer401_OnTheNotAuthorizedPage_ForAnAnonymousRequest(App app)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        using var response = await httpClient.GetAsync(new Uri(new Uri(DeployedApps.AddressOf(app)), PageUrls.Settings), TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, $"{app} answered {response.RequestMessage!.RequestUri}.");

        var landedOn = response.RequestMessage!.RequestUri!;
        Assert.AreEqual(PageUrls.NotAuthorized, PathWithoutCulture(landedOn));

        var query = System.Web.HttpUtility.ParseQueryString(landedOn.Query);
        Assert.AreEqual(PageUrls.Settings, PathWithoutCulture(new Uri(new Uri(DeployedApps.AddressOf(app)), query["return-url"])),
            "Signing in from there has to come back to the page that was asked for.");
        Assert.AreEqual("false", query["isForbidden"], "An anonymous visitor is unauthenticated (401), not forbidden (403).");
    }

    /// <summary>A request naming no culture is redirected to one that does, e.g. /en-US/not-found.</summary>
    private static string PathWithoutCulture(Uri uri)
    {
        var path = uri.AbsolutePath;

        var culture = CultureInfoManager.SupportedCultures
            .Select(sc => sc.Culture.Name)
            .FirstOrDefault(name => path.StartsWith($"/{name}/", StringComparison.OrdinalIgnoreCase));

        return culture is null ? path : path[(culture.Length + 1)..];
    }
}
