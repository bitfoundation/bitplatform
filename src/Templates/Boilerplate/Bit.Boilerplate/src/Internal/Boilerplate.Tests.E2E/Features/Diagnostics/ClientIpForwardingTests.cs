using Microsoft.Playwright.TestAdapter;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Diagnostics;

/// <summary>
/// The api is reached through a Cloudflare Tunnel, so the client it reports is whatever
/// <c>ForwardedHeaders:KnownIPNetworks</c> lets the connector tell it - the address the rate limiter partitions on and
/// the one <c>UserSession.IP</c> shows a user about their own sessions.
/// <para>
/// Asked over every path, since a plain request, a prerender, a websocket upgrade and /dev-mcp each reach the origin
/// their own way and a forwarding rule right for one can be wrong for another.
/// </para>
/// </summary>
[TestClass, Retry(2)]
public partial class ClientIpForwardingTests : AppTestBase
{
    /// <summary>Stops at the closing tag too: the raw prerendered html has no whitespace between the ip and it.</summary>
    private static readonly Regex ClientIpLine = new(@"Client IP:\s*([^\s<]+)", RegexOptions.Compiled);

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// The ip reported back to the browser, first over http then over the websocket the same page holds open. The
    /// page shows one report at a time, so the assertions run twice against what is on screen.
    /// <para>
    /// Inconclusive on webkit: under playwright's webkit the page's main thread freezes on /diagnostic before the
    /// report asks the api anything - not even <c>body.innerText</c> can be read - while real Safari answers fine. Open
    /// issue, cause not found yet.
    /// </para>
    /// </summary>
    [TestMethod, TestCategory(TestCategories.Web)]
    public async Task DiagnosticPage_Should_ReportTheClientsPublicIp_OverHttpAndOverTheSocket()
    {
        if (PlaywrightSettingsProvider.BrowserName is Microsoft.Playwright.BrowserType.Webkit)
            Assert.Inconclusive("Playwright's webkit freezes on /diagnostic, real Safari does not (See the summary).");

        var publicIps = await PublicIpProvider.Resolve(TestContext.CancellationToken);

        var page = await OpenApp(App.AdminPanel);
        await page.GotoAsync(new Uri(new Uri(DeployedApps.AdminPanel), "diagnostic").ToString());

        var report = page.Locator(".diagnostic-report");
        var patience = new LocatorAssertionsToContainTextOptions { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds };

        // The page runs itself over http on load. Matched by name, so the second half can't pass on the first's answer.
        await Expect(report).ToContainTextAsync("Via: Http", patience);
        await AssertPublicIp(report, publicIps);

        await page.GetByRole(AriaRole.Button, new() { Name = "Ask over SignalR" }).ClickAsync();

        await Expect(report).ToContainTextAsync("Via: SignalR", patience);
        await AssertPublicIp(report, publicIps);
    }

    private async Task AssertPublicIp(ILocator report, IReadOnlyCollection<string> publicIps)
    {
        var rendered = await report.InnerTextAsync();

        var reportedIp = ReadClientIps(rendered).Single();

        Assert.Contains(reportedIp, publicIps,
            $"The api reported '{reportedIp}' as the browser's ip, which is none of [{string.Join(", ", publicIps)}]. Report:{Environment.NewLine}{rendered}");
    }

    /// <summary>
    /// Fetched with a plain http client, so only the server-side render runs - where the ip travels furthest. Todo's
    /// web app calls a separate api, Sales' api is part of the same app, so the two rows cover a hop that exists and
    /// one that does not.
    /// </summary>
    [TestMethod, TestCategory(TestCategories.Api)]
    [DataRow(App.Todo, DisplayName = "Todo (standalone API)")]
    [DataRow(App.Sales, DisplayName = "Sales (integrated API)")]
    public async Task PrerenderedDiagnosticPage_Should_ReportTheClientsPublicIp(App app)
    {
        var publicIps = await PublicIpProvider.Resolve(TestContext.CancellationToken);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        var html = await httpClient.GetStringAsync(new Uri(new Uri(DeployedApps.AddressOf(app)), "diagnostic"), TestContext.CancellationToken);

        Assert.Contains("Client IP:", html, $"{app}'s prerendered html carries no diagnostic report, so the page never ran server-side.");

        var reportedIp = ReadClientIps(WebUtility.HtmlDecode(html)).First();

        Assert.Contains(reportedIp, publicIps,
            $"{app}'s prerender resolved '{reportedIp}' as the caller's ip, which is none of [{string.Join(", ", publicIps)}].");
    }

    /// <summary>The same question over <c>/dev-mcp</c>, from this process rather than a browser.</summary>
    [TestMethod, TestCategory(TestCategories.Api)]
    public async Task DevMcp_Should_ReportTheClientsPublicIp()
    {
        var publicIps = await PublicIpProvider.Resolve(TestContext.CancellationToken);

        var mcp = (await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken)).McpClient!;

        var report = await mcp.GetDiagnosticReport(TestContext.CancellationToken);

        Assert.Contains("Via: DevMcp", report,
            $"GetDiagnosticReport answered without saying which path it came in on, so this deployment predates it. Report:{Environment.NewLine}{report}");

        var reportedIp = ReadClientIps(report).First();

        Assert.Contains(reportedIp, publicIps,
            $"The dev mcp reported '{reportedIp}' as this caller's ip, which is none of [{string.Join(", ", publicIps)}]. Report:{Environment.NewLine}{report}");
    }

    /// <summary>Every ip the report names, one per path it was asked over.</summary>
    private static string[] ReadClientIps(string report)
    {
        var matches = ClientIpLine.Matches(report);

        Assert.IsNotEmpty(matches, $"No 'Client IP' line in:{Environment.NewLine}{report}");

        return [.. matches.Select(match => PublicIpProvider.Normalize(match.Groups[1].Value))];
    }
}
