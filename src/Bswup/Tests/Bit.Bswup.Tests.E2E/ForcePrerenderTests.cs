using Bit.Bswup.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bswup.Tests.E2E;

/// <summary>
/// An app configured like the AlwaysPrerender preset (forcePrerender, passive caching, no noPrerenderQuery): the
/// server renders every navigation, and when it cannot answer - the connection refused, or a gateway in front of
/// it answering 502 - the installed app still opens from the cached default document (issue #15479). Answers of
/// the app's own, errors included, still reach the page.
/// </summary>
[TestClass]
public class ForcePrerenderTests : BswupTest
{
    // The prerendered WebAssembly host: the cached default document can start the app on its own.
    protected override string Mode => "wasm";

    [TestInitialize]
    public Task UseAlwaysPrerenderAsync() =>
        // The harness default noPrerenderQuery is removed so the preset's (none) applies, as in an app.
        Session.SetOptionsAsync(new { workerSettings = new { mode = "AlwaysPrerender", noPrerenderQuery = (string?)null } });

    [TestMethod]
    public async Task A_navigation_the_server_answers_is_rendered_by_the_server()
    {
        await InstallAsync();
        await Session.ClearRequestsAsync();

        var response = await GotoAsync("counter");
        await WaitForInteractiveAsync();

        Assert.AreEqual(200, response!.Status);
        await Expect(Page.Locator("#page-counter")).ToBeVisibleAsync();
        // The worker sends the navigation on with fetch(), so Chromium reports it as destination "empty", not
        // "document" - the mode, path and query stay those of the page's navigation.
        var requests = await Session.RequestsAsync();
        Assert.IsTrue(requests.Any(r => r.FetchMode == "navigate" && r.Path == "/counter" && r.Query.Length == 0), $"The navigation did not reach the server as the page sent it: {string.Join(", ", requests.Select(r => $"{r} mode={r.FetchMode}"))}");
    }

    [TestMethod]
    public async Task The_app_opens_from_the_cache_when_the_server_refuses_the_connection()
    {
        await InstallCachingTheDefaultDocumentAsync();

        AllowNetworkErrors();
        await Session.AddFaultAsync(new { path = "^/counter$", abort = true });

        var response = await GotoAsync("counter");
        await WaitForInteractiveAsync();

        Assert.AreEqual(200, response!.Status);
        await Expect(Page.Locator("#page-counter")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task The_app_opens_from_the_cache_when_a_gateway_answers_502()
    {
        await InstallCachingTheDefaultDocumentAsync();

        AllowConsoleError("status of 502");
        await Session.AddFaultAsync(new { path = "^/counter$", status = 502 });

        var response = await GotoAsync("counter");
        await WaitForInteractiveAsync();

        Assert.AreEqual(200, response!.Status);
        await Expect(Page.Locator("#page-counter")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task The_app_opens_from_the_default_document_a_non_passive_install_precached_when_the_server_refuses_the_connection()
    {
        await Session.SetOptionsAsync(new { workerSettings = new { mode = "AlwaysPrerender", noPrerenderQuery = (string?)null, isPassive = false } });
        await InstallAsync();
        var manifest = await Session.ManifestAsync(AppPath);
        await EventuallyAsync(() => CachedUrlsAsync(BucketName(manifest.Version)), urls => urls.Contains(Session.Origin + "/"), "the default document to be precached");

        AllowNetworkErrors();
        await Session.AddFaultAsync(new { path = "^/counter$", abort = true });

        var response = await GotoAsync("counter");
        await WaitForInteractiveAsync();

        Assert.AreEqual(200, response!.Status);
        await Expect(Page.Locator("#page-counter")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task An_error_of_the_apps_own_reaches_the_page_instead_of_the_cache()
    {
        await InstallCachingTheDefaultDocumentAsync();

        AllowConsoleError("status of 500");
        await Session.AddFaultAsync(new { path = "^/counter$", status = 500 });

        // The fault answers with no body, which Chromium shows as its own error page for that status; Playwright
        // reports it as ERR_HTTP_RESPONSE_CODE_FAILURE. The cached default document would have loaded with 200,
        // and a failure in the worker would have been ERR_FAILED.
        IResponse? diagResponse = null; PlaywrightException? diagThrown = null;
        try { diagResponse = await GotoAsync("counter"); } catch (PlaywrightException ex) { diagThrown = ex; }
        await Page.WaitForTimeoutAsync(3000);
        var diagHtml = await Page.ContentAsync();
        Assert.Fail($"DIAG thrown={diagThrown?.Message?.Split((char)10)[0]} status={diagResponse?.Status} fromSW={diagResponse?.FromServiceWorker} url={Page.Url} counter={await Page.Locator("#page-counter").CountAsync()} title={await Page.TitleAsync()} htmlLength={diagHtml.Length} html={(diagHtml.Length > 400 ? diagHtml[..400] : diagHtml).Replace((char)10, (char)32)}");
        var error = await Assert.ThrowsExactlyAsync<PlaywrightException>(() => GotoAsync("counter"));
        StringAssert.Contains(error.Message, "ERR_HTTP_RESPONSE_CODE_FAILURE");
        Assert.AreEqual(0, await Page.Locator("#page-counter").CountAsync(), "The cached default document answered an error the server sent.");
    }

    /// <summary>
    /// Installs the app and opens it once more under the worker's control: a passive install caches nothing, and
    /// the default document is cached in the background after the first navigation the server answers.
    /// </summary>
    private async Task InstallCachingTheDefaultDocumentAsync()
    {
        await InstallAsync();
        await GotoAsync();
        await WaitForInteractiveAsync();

        var manifest = await Session.ManifestAsync(AppPath);
        var shell = Session.Origin + "/";
        await EventuallyAsync(() => CachedUrlsAsync(BucketName(manifest.Version)), urls => urls.Contains(shell), "the default document to be cached");

        // The AlwaysPrerender preset never asks the server for a non-prerendered document.
        var shellRequests = (await Session.RequestsAsync()).Where(r => r.Path == "/").ToArray();
        Assert.IsFalse(shellRequests.Any(r => r.Query.Contains("prerender", StringComparison.OrdinalIgnoreCase)), $"A non-prerendered document was requested: {string.Join(", ", shellRequests.Select(r => r.ToString()))}");
    }
}
