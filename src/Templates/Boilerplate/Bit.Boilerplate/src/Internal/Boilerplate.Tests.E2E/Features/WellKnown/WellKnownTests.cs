namespace Boilerplate.Tests.E2E.Features.WellKnown;

/// <summary>
/// The app links every web deployment publishes under /.well-known/ - Client.Web's wwwroot/.well-known, served by
/// Server.Web's /.well-known branch or by the Static Web App: assetlinks.json (Android App Links) and
/// apple-app-site-association (iOS universal links and Handoff). Without them, a link to the site opens the browser
/// instead of the installed app.
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class WellKnownTests
{
    /// <summary>The Android apps the demos ship (See DeployedApps).</summary>
    private static readonly string[] androidPackages = [DeployedApps.AdminPanelAndroidAppId, DeployedApps.TodoAndroidAppId];

    /// <summary>The iOS apps' application identifiers: the Apple team id, then the bundle id.</summary>
    private static readonly string[] iosAppIds = [$"76WD644YU8.{DeployedApps.AdminPanelAndroidAppId}", $"76WD644YU8.{DeployedApps.TodoAndroidAppId}"];

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (Server.Web)")]
    [DataRow(App.Todo, DisplayName = "Todo (Server.Web)")]
    [DataRow(App.Sales, DisplayName = "Sales (Server.Web)")]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = "AdminPanelWasmStandalone (Static Web App)")]
    [DataRow(App.TodoAot, DisplayName = "TodoAot (Static Web App)")]
    [DataRow(App.TodoSmall, DisplayName = "TodoSmall (Static Web App)")]
    [DataRow(App.TodoOffline, DisplayName = "TodoOffline (Static Web App)")]
    public async Task AssetLinks_Should_LetTheAndroidAppsHandleTheSitesLinks(App app)
    {
        using var document = await GetJson(app, "assetlinks.json");

        var statements = document.RootElement.EnumerateArray().ToArray();

        foreach (var package in androidPackages)
        {
            var statement = statements.SingleOrDefault(s => s.GetProperty("target").GetProperty("package_name").GetString() == package);
            Assert.AreNotEqual(JsonValueKind.Undefined, statement.ValueKind, $"{app}'s assetlinks.json does not name {package}.");

            Assert.Contains("delegate_permission/common.handle_all_urls", statement.GetProperty("relation").EnumerateArray().Select(r => r.GetString()).ToArray());
            Assert.AreEqual("android_app", statement.GetProperty("target").GetProperty("namespace").GetString());
            // Android verifies the installed app's signing certificate against these.
            Assert.IsGreaterThan(0, statement.GetProperty("target").GetProperty("sha256_cert_fingerprints").GetArrayLength(), $"{package} has no certificate fingerprint.");
        }
    }

    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (Server.Web)")]
    [DataRow(App.Todo, DisplayName = "Todo (Server.Web)")]
    [DataRow(App.Sales, DisplayName = "Sales (Server.Web)")]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = "AdminPanelWasmStandalone (Static Web App)")]
    [DataRow(App.TodoAot, DisplayName = "TodoAot (Static Web App)")]
    [DataRow(App.TodoSmall, DisplayName = "TodoSmall (Static Web App)")]
    [DataRow(App.TodoOffline, DisplayName = "TodoOffline (Static Web App)")]
    public async Task AppleAppSiteAssociation_Should_LetTheIosAppsHandleTheSitesLinks(App app)
    {
        using var document = await GetJson(app, "apple-app-site-association");

        var details = document.RootElement.GetProperty("applinks").GetProperty("details").EnumerateArray().ToArray();
        var appIds = details.SelectMany(d => d.TryGetProperty("appIDs", out var ids) ? ids.EnumerateArray().Select(id => id.GetString()) : [d.GetProperty("appID").GetString()]).ToArray();

        foreach (var appId in iosAppIds)
        {
            Assert.Contains(appId, appIds, $"{app}'s apple-app-site-association does not name {appId}.");
        }

        // The server's own paths stay in the browser: an app cannot answer an api call or a health probe.
        var paths = details.SelectMany(d => d.GetProperty("paths").EnumerateArray().Select(p => p.GetString())).ToArray();
        foreach (var serverPath in (string[])["NOT /api/*", "NOT /hangfire", "NOT /.well-known/*"])
        {
            Assert.Contains(serverPath, paths, $"{app}'s apple-app-site-association does not exclude {serverPath}.");
        }

        var continuation = document.RootElement.GetProperty("activitycontinuation").GetProperty("apps").EnumerateArray().Select(a => a.GetString()).ToArray();
        CollectionAssert.IsSubsetOf(iosAppIds, continuation, $"{app}'s apple-app-site-association does not offer Handoff to both apps.");
    }

    /// <summary>
    /// Apple's validators, and older iOS versions, expect JSON. Kept apart from the content checks above, so the known
    /// Static Web Apps behavior (See Security headers.md, finding 2) is reported as what it is.
    /// </summary>
    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (Server.Web)")]
    [DataRow(App.AdminPanelWasmStandalone, DisplayName = "AdminPanelWasmStandalone (Static Web App)")]
    public async Task AppleAppSiteAssociation_Should_BeServedAsJson(App app)
    {
        using var response = await Send(app, "apple-app-site-association");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(response.Content.Headers.ContentType, $"{app} serves apple-app-site-association with no Content-Type.");
        Assert.AreEqual("application/json", response.Content.Headers.ContentType.MediaType, $"{app} serves apple-app-site-association as {response.Content.Headers.ContentType}.");
    }

    private async Task<JsonDocument> GetJson(App app, string file)
    {
        using var response = await Send(app, file);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // Not a redirect: iOS and Android fetch these without following one.
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{app}'s /.well-known/{file} answered {(int)response.StatusCode} {response.Headers.Location}.");

        return JsonDocument.Parse(body);
    }

    private async Task<HttpResponseMessage> Send(App app, string file)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromMinutes(2) };

        return await httpClient.GetAsync(new Uri(new Uri(DeployedApps.AddressOf(app)), $".well-known/{file}"), TestContext.CancellationToken);
    }
}
