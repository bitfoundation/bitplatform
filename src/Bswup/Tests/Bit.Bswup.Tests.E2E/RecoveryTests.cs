using Bit.Bswup.Tests.E2E.Infrastructure;

namespace Bit.Bswup.Tests.E2E;

/// <summary>
/// Getting a client out of a bad state: the <c>BitBswup.forceRefresh()</c> reset, the cleanup worker that backs
/// an app out of Bswup, and a hard reload that bypasses the worker.
/// </summary>
[TestClass]
public class RecoveryTests : BswupTest
{
    protected override string Mode => "wasm";

    [TestMethod]
    public async Task ForceRefresh_reinstalls_the_app_and_keeps_caches_the_app_owns()
    {
        await InstallAsync();
        var bucket = BucketName((await Session.ManifestAsync(AppPath)).Version);
        await Page.EvaluateAsync("async () => { const cache = await caches.open('my-app-data'); await cache.put('/my-data', new Response('kept')); }");

        await Page.EvaluateAsync("() => { BitBswup.forceRefresh(); }");

        await ExpectLoadsAsync(2);
        await WaitForControlledAsync();
        await WaitForInteractiveAsync();

        // A reinstall from scratch: the reloaded page went through a first install again.
        var finished = await WaitForEventAsync("DOWNLOAD_FINISHED");
        Assert.IsTrue(finished.FirstInstall, "forceRefresh did not remove the registration.");

        var names = await Page.EvaluateAsync<string[]>("() => caches.keys()");
        CollectionAssert.Contains(names, "my-app-data", "forceRefresh deleted a cache the app owns.");
        CollectionAssert.Contains(names, bucket);
    }

    [TestMethod]
    public async Task The_cleanup_worker_backs_the_app_out_of_Bswup()
    {
        AllowConsoleError("progress handler");

        await InstallAsync();
        Assert.AreEqual(1, (await BswupCacheNamesAsync()).Length);

        await Session.SetOptionsAsync(new { worker = "cleanup" });
        await Page.EvaluateAsync("() => { BitBswup.checkForUpdate(); }");

        // The cleanup worker takes over the controlled tab, which reloads once to detach from it.
        await ExpectLoadsAsync(2);
        await WaitForInteractiveAsync();

        await EventuallyAsync(() => BswupCacheNamesAsync(), names => names.Length == 0, "the Bswup caches to be purged");
        await AssertNoFurtherReloadAsync(2);

        // Nothing is answered from a Bswup cache any more: a formerly precached asset goes to the server.
        await Session.ClearRequestsAsync();
        Assert.AreEqual(200, (await FetchAsync(AppUrl("_content/Bit.Bswup.Tests.Harness/harness-extra.json"))).Status);
        Assert.IsTrue((await Session.RequestsAsync()).Any(r => r.Path.EndsWith("harness-extra.json", StringComparison.Ordinal)),
            "A formerly precached asset was still answered without the server.");

        // Later loads stay quiet - no reload loop, no caches - while the page still references bit-bswup.js.
        // Deliberately not asserted: that the registration is gone. In Chromium the tab that reloads on the takeover
        // loads under the cleanup worker before its teardown runs, is not yet a client the teardown can message, and
        // its own register() call revives the uninstalling registration - so the fetch-less worker stays registered.
        await GotoAsync("counter");
        await WaitForInteractiveAsync();
        await ExpectLoadsAsync(3);
        await AssertNoFurtherReloadAsync(3);
        Assert.AreEqual(0, (await BswupCacheNamesAsync()).Length);
    }

    [TestMethod]
    public async Task A_hard_reload_starts_the_app_without_the_worker()
    {
        await InstallAsync();

        var cdp = await Context.NewCDPSessionAsync(Page);
        await cdp.SendAsync("Page.reload", new Dictionary<string, object> { ["ignoreCache"] = true });

        await ExpectLoadsAsync(2);
        await WaitForInteractiveAsync();
        Assert.IsFalse(await IsControlledAsync(), "The hard reload was controlled; the scenario did not happen.");
    }
}
