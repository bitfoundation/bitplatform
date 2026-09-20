using Bit.Bswup.Tests.E2E.Infrastructure;

namespace Bit.Bswup.Tests.E2E;

/// <summary>
/// How the installed worker answers the page's requests: the URL lists an app configures (prohibited,
/// server-handled, server-rendered), asset navigations, and ranged requests for cached media.
/// </summary>
[TestClass]
public class WorkerRoutingTests : BswupTest
{
    private const string ExtraAsset = "_content/Bit.Bswup.Tests.Harness/harness-extra.json";

    protected override string Mode => "wasm";

    [TestMethod]
    public async Task A_prohibited_url_is_answered_with_403_by_the_worker_and_never_reaches_the_server()
    {
        // The browser logs every non-2xx response the page fetches.
        AllowConsoleError("status of 403");
        await Session.SetOptionsAsync(new { workerSettings = new { prohibitedUrls = new[] { HarnessSession.Regex(@"harness-extra\.json") } } });
        await InstallAsync();
        await Session.ClearRequestsAsync();

        var outcome = await FetchAsync(AppUrl(ExtraAsset));

        Assert.AreEqual(403, outcome.Status, outcome.ToString());
        Assert.AreEqual("This URL is prohibited!", outcome.Text);
        Assert.IsFalse((await Session.RequestsAsync()).Any(r => r.Path.Contains("harness-extra", StringComparison.Ordinal)), "The prohibited request reached the server.");
    }

    [TestMethod]
    public async Task A_server_handled_url_goes_to_the_server_even_when_it_is_cached()
    {
        await Session.SetOptionsAsync(new
        {
            workerSettings = new
            {
                serverHandledUrls = new[] { HarnessSession.Regex(@"\/api\/"), HarnessSession.Regex(@"\/_harness\/"), HarnessSession.Regex(@"harness-extra\.json") },
            },
        });
        await InstallAsync();
        await Session.ClearRequestsAsync();

        Assert.AreEqual(200, (await FetchAsync(AppUrl(ExtraAsset))).Status);
        Assert.AreEqual(200, (await FetchAsync(AppUrl("_content/Bit.Bswup.Tests.Harness/harness.js"))).Status);

        var requests = await Session.RequestsAsync();
        Assert.IsTrue(requests.Any(r => r.Path.EndsWith("harness-extra.json", StringComparison.Ordinal)), "The server-handled URL was answered without the server.");
        Assert.IsFalse(requests.Any(r => r.Path.EndsWith("harness.js", StringComparison.Ordinal)), "A cached asset reached the server.");
    }

    [TestMethod]
    public async Task A_server_rendered_url_is_navigated_on_the_server_and_other_routes_are_answered_from_the_cache()
    {
        await Session.SetOptionsAsync(new { workerSettings = new { serverRenderedUrls = new[] { HarnessSession.Regex(@"\/privacy$") } } });
        await InstallAsync();
        await Session.ClearRequestsAsync();

        await GotoAsync("privacy");
        await WaitForInteractiveAsync();
        await GotoAsync("counter");
        await WaitForInteractiveAsync();

        var documents = (await Session.RequestsAsync()).Where(r => r.Destination == "document").Select(r => r.Path).ToArray();
        CollectionAssert.AreEqual(new[] { "/privacy" }, documents, $"Documents requested: {string.Join(", ", documents)}");
    }

    [TestMethod]
    public async Task A_navigation_to_a_cached_asset_shows_that_asset_even_offline()
    {
        await InstallAsync();
        await WaitForAssetAsync(ExtraAsset);

        AllowNetworkErrors();
        await Session.SetOfflineAsync(true);

        var response = await Page.GotoAsync(AppUrl(ExtraAsset));

        Assert.AreEqual(200, response!.Status);
        StringAssert.Contains(await response.TextAsync(), "\"alphabet\"");
    }

    [TestMethod]
    public async Task A_ranged_request_for_a_cached_asset_is_answered_with_206_and_the_requested_bytes()
    {
        await InstallAsync();
        await WaitForAssetAsync(ExtraAsset);

        AllowNetworkErrors();
        await Session.SetOfflineAsync(true);

        var whole = await FetchAsync(AppUrl(ExtraAsset));
        var ranged = await FetchAsync(AppUrl(ExtraAsset), headers: new() { ["Range"] = "bytes=4-13" });

        Assert.AreEqual(206, ranged.Status, ranged.ToString());
        Assert.AreEqual(whole.Text.Substring(4, 10), ranged.Text);
        Assert.AreEqual($"bytes 4-13/{System.Text.Encoding.UTF8.GetByteCount(whole.Text)}", ranged.ContentRange);
    }

    private async Task WaitForAssetAsync(string url)
    {
        var manifest = await Session.ManifestAsync(AppPath);
        var key = PrecacheKeys(manifest).Single(k => k.Contains(url, StringComparison.Ordinal));
        await EventuallyAsync(() => CachedUrlsAsync(BucketName(manifest.Version)), urls => urls.Contains(key), $"{url} to be cached");
    }
}
