using Microsoft.Playwright.TestAdapter;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Diagnostics;

/// <summary>
/// The api is reached through a Cloudflare Tunnel, so the client it reports is whatever
/// <c>ForwardedHeaders:KnownIPNetworks</c> lets the connector tell it - the address the rate limiter partitions on and
/// the one <c>UserSession.IP</c> shows a user about their own sessions.
/// <para>
/// Asked over every path, since a plain request, a prerender, a websocket upgrade and /dev-mcp each reach the origin
/// their own way and a forwarding rule right for one can be wrong for another. Each path also has to agree on who is
/// calling: a token that reaches http but not the hub's handshake is the same kind of per-path gap.
/// </para>
/// </summary>
[TestClass, Retry(2)]
public partial class ClientIpForwardingTests : AppTestBase
{
    /// <summary>Stops at the closing tag too: the raw prerendered html has no whitespace between the ip and it.</summary>
    private static readonly Regex ClientIpLine = new(@"Client IP:\s*([^\s<]+)", RegexOptions.Compiled);

    /// <summary>With the colon: the device context above the server's sections lists a UserSessionId of its own.</summary>
    private static readonly Regex UserSessionIdLine = new(@"UserSessionId:\s*([0-9a-fA-F-]{36})", RegexOptions.Compiled);

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// The ip and the caller reported back to the browser, first over http then over the websocket the same page holds
    /// open - once anonymous, once signed in. The page shows one report at a time, so the assertions run twice against
    /// what is on screen.
    /// <para>
    /// Inconclusive on webkit: under playwright's webkit the page's main thread freezes on /diagnostic before the
    /// report asks the api anything - not even <c>body.innerText</c> can be read - while real Safari answers fine. Open
    /// issue, cause not found yet.
    /// </para>
    /// </summary>
    [TestMethod, TestCategory(TestCategories.Web)]
    [DataRow(false, DisplayName = "Anonymous")]
    [DataRow(true, DisplayName = "Authenticated")]
    public async Task DiagnosticPage_Should_ReportTheClientsPublicIpAndIdentity_OverHttpAndOverTheSocket(bool signedIn)
    {
        if (PlaywrightSettingsProvider.BrowserName is Microsoft.Playwright.BrowserType.Webkit)
            Assert.Inconclusive("Playwright's webkit freezes on /diagnostic, real Safari does not (See the summary).");

        // The session is deleted at cleanup through the database.
        if (signedIn)
            await SkipWithoutGlobalAdminCredentials();

        var page = await OpenApp(App.AdminPanel);

        // The browser's, which is not this machine's when the browser runs on another one.
        var publicIps = await PublicIpProvider.ResolveFromBrowser(page.Context);

        Guid? sessionId = null;

        if (signedIn)
        {
            await WaitUntilInteractive(page);
            await SignIn(page, StoreUser.Email, StoreUser.Password);
            sessionId = await GetSessionId(page);
        }

        // A full load, so both the http client and the hub's handshake start from the stored token.
        await page.GotoAsync(new Uri(new Uri(DeployedApps.AdminPanel), "diagnostic").ToString());

        var report = page.Locator(".diagnostic-report");
        var serverSections = report.Locator(".report");
        var patience = new LocatorAssertionsToContainTextOptions { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds };

        // The page runs itself over http on load. Matched by name, so the second half can't pass on the first's answer.
        await Expect(serverSections).ToContainTextAsync("Via: Http", patience);
        await AssertPublicIp(report, publicIps);
        AssertCaller(await serverSections.InnerTextAsync(), signedIn, sessionId);

        await page.GetByRole(AriaRole.Button, new() { Name = "Ask over SignalR" }).ClickAsync();

        await Expect(serverSections).ToContainTextAsync("Via: SignalR", patience);
        await AssertPublicIp(report, publicIps);
        AssertCaller(await serverSections.InnerTextAsync(), signedIn, sessionId);
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
    /// one that does not. Nothing to sign in with here, so only the anonymous half.
    /// </summary>
    [TestMethod, TestCategory(TestCategories.Api)]
    [DataRow(App.Todo, DisplayName = "Todo (standalone API)")]
    [DataRow(App.Sales, DisplayName = "Sales (integrated API)")]
    public async Task PrerenderedDiagnosticPage_Should_ReportTheClientsPublicIp_AndAnAnonymousCaller(App app)
    {
        var publicIps = await PublicIpProvider.Resolve(TestContext.CancellationToken);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        var html = await httpClient.GetStringAsync(new Uri(new Uri(DeployedApps.AddressOf(app)), "diagnostic"), TestContext.CancellationToken);

        Assert.Contains("Client IP:", html, $"{app}'s prerendered html carries no diagnostic report, so the page never ran server-side.");

        var decoded = WebUtility.HtmlDecode(html);

        var reportedIp = ReadClientIps(decoded).First();

        Assert.Contains(reportedIp, publicIps,
            $"{app}'s prerender resolved '{reportedIp}' as the caller's ip, which is none of [{string.Join(", ", publicIps)}].");

        AssertCaller(decoded, signedIn: false, sessionId: null);
    }

    /// <summary>
    /// The same question over <c>/dev-mcp</c>, from this process rather than a browser. Authenticated only: the endpoint
    /// authorizes nobody else.
    /// </summary>
    [TestMethod, TestCategory(TestCategories.Api)]
    public async Task DevMcp_Should_ReportTheClientsPublicIp_AndItsAuthenticatedSession()
    {
        var publicIps = await PublicIpProvider.Resolve(TestContext.CancellationToken);

        var mcp = (await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken)).McpClient!;

        var report = await mcp.GetDiagnosticReport(TestContext.CancellationToken);

        Assert.Contains("Via: DevMcp", report,
            $"GetDiagnosticReport answered without saying which path it came in on, so this deployment predates it. Report:{Environment.NewLine}{report}");

        var reportedIp = ReadClientIps(report).First();

        Assert.Contains(reportedIp, publicIps,
            $"The dev mcp reported '{reportedIp}' as this caller's ip, which is none of [{string.Join(", ", publicIps)}]. Report:{Environment.NewLine}{report}");

        AssertCaller(report, signedIn: true, sessionId: null);
    }

    /// <summary>
    /// <paramref name="serverReport"/> says whether the call was authenticated, and names a session exactly when it
    /// was - <paramref name="sessionId"/> when the test knows which one.
    /// </summary>
    private static void AssertCaller(string serverReport, bool signedIn, Guid? sessionId)
    {
        Assert.Contains($"IsAuthenticated: {signedIn.ToString().ToLowerInvariant()}", serverReport,
            $"The report does not say the call was {(signedIn ? "authenticated" : "anonymous")}. Report:{Environment.NewLine}{serverReport}");

        var reportedSessionIds = UserSessionIdLine.Matches(serverReport).Select(match => Guid.Parse(match.Groups[1].Value)).ToArray();

        if (signedIn is false)
        {
            Assert.IsEmpty(reportedSessionIds, $"An anonymous call was reported with a user session. Report:{Environment.NewLine}{serverReport}");
            return;
        }

        Assert.HasCount(1, reportedSessionIds, $"An authenticated call has to name exactly one user session. Report:{Environment.NewLine}{serverReport}");

        if (sessionId is not null)
        {
            Assert.AreEqual(sessionId.Value, reportedSessionIds[0], "The report names a session other than the one this page signed in with.");
        }
    }

    /// <summary>Every ip the report names, one per path it was asked over.</summary>
    private static string[] ReadClientIps(string report)
    {
        var matches = ClientIpLine.Matches(report);

        Assert.IsNotEmpty(matches, $"No 'Client IP' line in:{Environment.NewLine}{report}");

        return [.. matches.Select(match => PublicIpProvider.Normalize(match.Groups[1].Value))];
    }
}
