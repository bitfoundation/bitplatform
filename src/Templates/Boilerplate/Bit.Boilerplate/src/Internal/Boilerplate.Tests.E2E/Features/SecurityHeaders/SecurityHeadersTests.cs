using System.Text.RegularExpressions;

namespace Boilerplate.Tests.E2E.Features.SecurityHeaders;

/// <summary>
/// The security baseline as the deployments actually answer it. Three places write it and must agree: Server.Shared's
/// UseSecurityHeaders (Server.Web and Server.Api), Client.Web's wwwroot/staticwebapp.config.json (Azure Static Web Apps)
/// and wwwroot/_headers (Cloudflare Pages) - the static hosts never run the middleware, so they repeat every header.
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class SecurityHeadersTests
{
    /// <summary>What every response must carry, verbatim. Strict-Transport-Security is checked apart: Cloudflare raises it.</summary>
    private static readonly Dictionary<string, string> baseline = new(StringComparer.OrdinalIgnoreCase)
    {
        ["X-Content-Type-Options"] = "nosniff",
        ["X-XSS-Protection"] = "1; mode=block",
        ["X-Frame-Options"] = "SAMEORIGIN",
        ["Referrer-Policy"] = "strict-origin-when-cross-origin",
        ["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(self), payment=(), usb=(), display-capture=()",
        ["Cross-Origin-Resource-Policy"] = "cross-origin",
        ["Content-Security-Policy"] = "object-src 'none'; frame-ancestors 'self'; form-action 'self'; worker-src 'self'; upgrade-insecure-requests;",
    };

    /// <summary>30 days: what the three sources write. Cloudflare may only make it longer.</summary>
    private const int minHstsMaxAge = 2592000;

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(DeployedApps.AdminPanel, "en-US/", DisplayName = "AdminPanel page (Server.Web, App.razor)")]
    [DataRow(DeployedApps.Todo, "en-US/", DisplayName = "Todo page (Server.Web, App.razor)")]
    [DataRow(DeployedApps.Sales, "", DisplayName = "Sales culture redirect (302)")]
    [DataRow(DeployedApps.Sales, "sitemap.xml", DisplayName = "Sales sitemap.xml")]
    [DataRow(DeployedApps.Sales, "openapi/v1.json", DisplayName = "Sales OpenAPI document (integrated API)")]
    [DataRow(DeployedApps.AdminPanelApi, "openapi/v1.json", DisplayName = "AdminPanelApi OpenAPI document")]
    [DataRow(DeployedApps.AdminPanelApi, "scalar/v1", DisplayName = "AdminPanelApi Scalar page")]
    [DataRow(DeployedApps.TodoApi, "e2e/route/that-does-not-exist", DisplayName = "TodoApi 404")]
    [DataRow(DeployedApps.AdminPanelWasmStandalone, "", DisplayName = "AdminPanelWasmStandalone index.html (Static Web App)")]
    [DataRow(DeployedApps.TodoAot, "_framework/blazor.webassembly.js", DisplayName = "TodoAot framework script (Static Web App)")]
    [DataRow(DeployedApps.TodoOffline, "offline-todo", DisplayName = "TodoOffline navigation fallback (Static Web App)")]
    public async Task Response_Should_CarryTheSecurityBaseline(string host, string path)
    {
        using var response = await Send(HttpMethod.Get, new Uri(new Uri(host), path));

        AssertBaseline(response);
    }

    /// <summary>
    /// A model validation 400 goes through UseExceptionHandler, whose Response.Clear() drops every header written on the
    /// way in - the reason UseSecurityHeaders writes them from OnStarting.
    /// </summary>
    [TestMethod]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi (standalone API)")]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (integrated API)")]
    public async Task ExceptionHandlerResponse_Should_StillCarryTheSecurityBaseline(string host)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(host), "api/v1/Identity/SignIn"))
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        using var response = await Send(request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "An empty sign in should be a validation error.");
        AssertBaseline(response);
    }

    [TestMethod]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (Cloudflare)")]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi (Cloudflare)")]
    [DataRow(DeployedApps.AdminPanelWasmStandalone, DisplayName = "AdminPanelWasmStandalone (Static Web App)")]
    public async Task PlainHttp_Should_BeRedirectedToHttps(string host)
    {
        var httpUri = new UriBuilder(host) { Scheme = Uri.UriSchemeHttp, Port = -1 }.Uri;

        using var response = await Send(HttpMethod.Get, httpUri);

        Assert.IsTrue(response.StatusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.PermanentRedirect or HttpStatusCode.Redirect or HttpStatusCode.TemporaryRedirect,
            $"{httpUri} answered {response.StatusCode}.");
        Assert.AreEqual(Uri.UriSchemeHttps, response.Headers.Location!.Scheme, $"{httpUri} redirected to {response.Headers.Location}.");
    }

    /// <summary>
    /// The app-wide policy: ContentSecurityPolicy.razor, rendered into App.razor's head. It is stricter than the header's
    /// (default-src, script-src, connect-src ...) and a browser enforces both. It must name the app's own API, or the
    /// app could not call it.
    /// </summary>
    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo (standalone API)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (standalone API)")]
    [DataRow(App.Sales, DisplayName = "Sales (integrated API)")]
    public async Task PrerenderedPage_Should_CarryTheAppWideCspMetaTag(App app)
    {
        using var response = await Send(HttpMethod.Get, new Uri(new Uri(DeployedApps.AddressOf(app)), "en-US/"));
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        var match = CspMetaTag().Match(html);
        Assert.IsTrue(match.Success, $"{app}'s prerendered page has no Content-Security-Policy meta tag.");

        var directives = WebUtility.HtmlDecode(match.Groups["content"].Value)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => d.Split(' ', 2))
            .ToDictionary(d => d[0], d => d.Length > 1 ? d[1] : "");

        Assert.AreEqual("'none'", directives["object-src"]);
        Assert.AreEqual("'self'", directives["worker-src"]);
        Assert.IsTrue(directives.ContainsKey("upgrade-insecure-requests"), "Production pages should upgrade insecure requests.");
        Assert.Contains("'self'", directives["default-src"]);
        Assert.Contains(DeployedApps.ApiOf(app), directives["connect-src"], $"{app} could not reach its own API.");
        Assert.Contains(DeployedApps.ApiOf(app), directives["form-action"]);
        // Meta policies ignore frame-ancestors, which is why the header carries it.
        Assert.IsFalse(directives.ContainsKey("frame-ancestors"), "frame-ancestors is ignored in a meta tag; it belongs in the header.");
    }

    /// <summary>
    /// staticwebapp.config.json and _headers both declare application/json for this extensionless file, and Server.Web
    /// serves it so (See Program.Middlewares' .well-known branch). Apple's validators expect JSON.
    /// </summary>
    [TestMethod]
    [DataRow(DeployedApps.AdminPanel, DisplayName = "AdminPanel (Server.Web)")]
    [DataRow(DeployedApps.AdminPanelWasmStandalone, DisplayName = "AdminPanelWasmStandalone (Static Web App)")]
    public async Task AppleAppSiteAssociation_Should_BeServedAsJson(string host)
    {
        using var response = await Send(HttpMethod.Get, new Uri(new Uri(host), ".well-known/apple-app-site-association"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(response.Content.Headers.ContentType, $"{host} serves apple-app-site-association with no Content-Type.");
        Assert.AreEqual("application/json", response.Content.Headers.ContentType.MediaType,
            $"{host} serves apple-app-site-association as {response.Content.Headers.ContentType}.");
    }

    private void AssertBaseline(HttpResponseMessage response)
    {
        var uri = response.RequestMessage!.RequestUri;

        foreach (var (name, expected) in baseline)
        {
            Assert.AreEqual(expected, HeaderOf(response, name), $"{name} on {uri} ({(int)response.StatusCode}).");
        }

        var hsts = HeaderOf(response, "Strict-Transport-Security");
        Assert.IsNotNull(hsts, $"Strict-Transport-Security is missing on {uri}.");
        var maxAge = int.Parse(HstsMaxAge().Match(hsts).Groups["seconds"].Value);
        Assert.IsGreaterThanOrEqualTo(minHstsMaxAge, maxAge, $"Strict-Transport-Security on {uri}: {hsts}");

        // Nothing that names the stack behind the proxy.
        foreach (var name in (string[])["X-Powered-By", "X-AspNet-Version", "X-AspNetMvc-Version"])
        {
            Assert.IsNull(HeaderOf(response, name), $"{name} on {uri}: {HeaderOf(response, name)}");
        }
        Assert.IsFalse(HeaderOf(response, "Server") is string server && (server.Contains("Kestrel", StringComparison.OrdinalIgnoreCase) || server.Contains("IIS", StringComparison.OrdinalIgnoreCase)),
            $"Server on {uri}: {HeaderOf(response, "Server")}");
    }

    private static string? HeaderOf(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values) || response.Content.Headers.TryGetValues(name, out values))
            return string.Join(", ", values);

        return null;
    }

    private Task<HttpResponseMessage> Send(HttpMethod method, Uri uri) => Send(new HttpRequestMessage(method, uri));

    /// <summary>Redirects are not followed: a redirect is a response of its own, and has to carry the baseline too.</summary>
    private async Task<HttpResponseMessage> Send(HttpRequestMessage request)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromMinutes(2) };

        return await httpClient.SendAsync(request, TestContext.CancellationToken);
    }

    [GeneratedRegex("""<meta http-equiv="Content-Security-Policy" content="(?<content>[^"]*)""")]
    private static partial Regex CspMetaTag();

    [GeneratedRegex(@"max-age=(?<seconds>\d+)")]
    private static partial Regex HstsMaxAge();
}
