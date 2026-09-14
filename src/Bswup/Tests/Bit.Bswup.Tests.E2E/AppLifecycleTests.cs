using Bit.Bswup.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bswup.Tests.E2E;

/// <summary>
/// What every Bswup app has to get right, whatever hosts it: the first install, a later visit, booting offline,
/// an update, and installs that fail. Each host variant - the Blazor Web App in every interactive render mode
/// and the standalone WebAssembly app on a sub-path - runs this same list, so a feature that works in one host
/// and not another shows up as exactly that.
/// </summary>
public abstract class AppLifecycleTests : BswupTest
{
    /// <summary>Whether the app can start with the network gone: only a WebAssembly app runs entirely from its cache.</summary>
    protected virtual bool BootsOffline => Mode is "wasm" or "wasm-noprerender";

    /// <summary>Whether navigations are left to the server (forcePrerender), as an app rendering on the server needs.</summary>
    protected virtual bool NavigationsReachServer => AppPath == "/" && Mode is "auto" or "server";

    /// <summary>Whether the page runs the built-in BswupProgress UI (the Blazor Web App) or a hand-written handler (the standalone app).</summary>
    protected bool UsesProgressComponent => AppPath == "/";

    [TestMethod]
    public async Task A_first_visit_installs_the_worker_precaches_the_manifest_and_starts_the_app_without_a_reload()
    {
        await InstallAsync();

        // The first install completes in place: Blazor starts in the document the user opened.
        await ExpectLoadsAsync(1);

        var events = await EventsAsync();
        var finished = events.Single(e => e.Type == "DOWNLOAD_FINISHED");
        Assert.IsTrue(finished.FirstInstall, $"downloadFinished was not a first install: {string.Join(", ", events.Select(e => e.ToString()))}");
        Assert.AreEqual(100, events.Where(e => e.Type == "DOWNLOAD_PROGRESS").Max(e => e.Percent) ?? 0, 0.001);
        Assert.IsFalse(events.Any(e => e.Type == "ERROR"), $"The install reported errors: {string.Join(", ", events.Where(e => e.Type == "ERROR"))}");

        var manifest = await Session.ManifestAsync(AppPath);
        var bucket = BucketName(manifest.Version);
        CollectionAssert.AreEqual(new[] { bucket }, await BswupCacheNamesAsync());

        var expected = PrecacheKeys(manifest);
        var cached = await EventuallyAsync(() => CachedUrlsAsync(bucket), urls => expected.All(urls.Contains), $"every manifest asset in {bucket}");

        var workerScripts = cached.Where(url => url.Contains("service-worker.js", StringComparison.Ordinal) || url.Contains("bit-bswup.sw", StringComparison.Ordinal)).ToArray();
        Assert.AreEqual(0, workerScripts.Length, $"Service-worker scripts were precached: {string.Join(", ", workerScripts)}");

        if (UsesProgressComponent)
        {
            await Expect(Page.Locator("#bit-bswup")).ToBeHiddenAsync();
            await Expect(Page.Locator("#bit-bswup-error")).ToBeHiddenAsync();
        }
    }

    [TestMethod]
    public async Task A_later_visit_is_served_by_the_worker_without_downloading_the_app_again()
    {
        await InstallAsync();
        var manifest = await Session.ManifestAsync(AppPath);
        await WaitForPrecacheAsync(manifest);
        await Session.ClearRequestsAsync();

        await GotoAsync("counter");
        await WaitForInteractiveAsync();
        await ExpectLoadsAsync(2);
        Assert.IsTrue(await IsControlledAsync());

        await Page.Locator("#increment").ClickAsync();
        await Expect(Page.Locator("#count")).ToHaveTextAsync("1");

        // The precached assets only: the worker scripts in the manifest are never cached - the browser's own update
        // check on navigation fetches them (bit-bswup.sw.js included, as an import of service-worker.js).
        var assetPaths = PrecachedAssets(manifest).Select(asset => AppPath + asset.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requests = await Session.RequestsAsync();

        var downloaded = requests.Where(r => assetPaths.Contains(r.Path)).ToArray();
        Assert.AreEqual(0, downloaded.Length, $"A later visit downloaded cached assets again: {string.Join(", ", downloaded.Select(r => r.ToString()))}");

        if (NavigationsReachServer is false)
        {
            var documents = requests.Where(r => r.Destination == "document").ToArray();
            Assert.AreEqual(0, documents.Length, $"The navigation was not answered from the cache: {string.Join(", ", documents.Select(r => r.ToString()))}");
        }
    }

    [TestMethod]
    public async Task The_app_starts_offline_from_the_cache()
    {
        if (BootsOffline is false)
            Assert.Inconclusive($"The {Mode} render mode needs its server to run the app.");

        await InstallAsync();
        await WaitForPrecacheAsync(await Session.ManifestAsync(AppPath));

        AllowNetworkErrors();
        await Session.SetOfflineAsync(true);

        // A deep link the app was never opened at: the worker answers it with the cached app shell.
        await GotoAsync("counter");
        await WaitForInteractiveAsync();
        await ExpectLoadsAsync(2);

        await Page.Locator("#increment").ClickAsync();
        await Expect(Page.Locator("#count")).ToHaveTextAsync("1");

        var version = await FetchAsync(AppUrl("harness-version.txt"));
        Assert.AreEqual("v1", version.Text, $"The hash-less external asset was not served from the cache: {version}");
    }

    [TestMethod]
    public async Task A_new_version_is_downloaded_activated_and_served_after_a_reload()
    {
        await InstallAsync();
        var original = await Session.ManifestAsync(AppPath);
        await WaitForPrecacheAsync(original);

        await Session.PublishVersionAsync(2);
        await Session.ClearRequestsAsync();
        await CheckForUpdateAsync();

        if (UsesProgressComponent)
        {
            // BswupProgress's default AutoReload: the finished update activates and the page reloads on its own.
        }
        else
        {
            // The standalone app's handler prompts: accepting it activates the staged version.
            await Page.Locator("#bit-bswup-reload").ClickAsync(new() { Timeout = 120_000 });
        }

        await ExpectLoadsAsync(2);
        await WaitForInteractiveAsync();
        await WaitForControlledAsync();
        await AssertNoFurtherReloadAsync(2);

        var updated = await Session.ManifestAsync(AppPath);
        await EventuallyAsync(() => BswupCacheNamesAsync(), names => names.SequenceEqual([BucketName(updated.Version)]),
            $"only the {BucketName(updated.Version)} bucket (the previous version's pruned)");

        // Unchanged hashed assets move to the new bucket instead of being downloaded again.
        var redownloaded = (await Session.RequestsAsync()).Where(r => r.Path.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.AreEqual(0, redownloaded.Length, $"The update downloaded unchanged assets: {string.Join(", ", redownloaded.Select(r => r.ToString()))}");

        // Hash-less assets are refreshed on every update: the new version's copy is the one cached.
        AllowNetworkErrors();
        await Session.SetOfflineAsync(true);
        var version = await FetchAsync(AppUrl("harness-version.txt"));
        Assert.AreEqual("v2", version.Text, $"The update did not refresh the hash-less external asset: {version}");
    }

    [TestMethod]
    public async Task An_update_check_without_a_new_version_reports_the_app_up_to_date()
    {
        await InstallAsync();

        await Page.EvaluateAsync("() => BitBswup.checkForUpdate()");

        await WaitForEventAsync("UPDATE_NOT_FOUND");
        await ExpectLoadsAsync(1);
    }

    [TestMethod]
    public async Task An_update_check_that_cannot_reach_the_server_is_reported_and_the_app_keeps_running()
    {
        await InstallAsync();

        AllowNetworkErrors();
        AllowConsoleError("checkForUpdate failed");
        await Session.SetOfflineAsync(true);

        await Page.EvaluateAsync("() => BitBswup.checkForUpdate()");

        await WaitForEventAsync("UPDATE_CHECK_FAILED");
        Assert.IsFalse((await EventsAsync()).Any(e => e.Type == "ERROR"), "A failed update check was reported as an install error.");
        if (UsesProgressComponent)
        {
            await Expect(Page.Locator("#bit-bswup-error")).ToBeHiddenAsync();
            await Expect(Page.Locator("#bit-bswup")).ToBeHiddenAsync();
        }
        await ExpectLoadsAsync(1);
        await WaitForInteractiveAsync();
    }

    [TestMethod]
    public async Task A_first_install_aborted_under_strict_tolerance_still_starts_the_app_without_a_worker()
    {
        AllowConsoleError("install error");
        AllowConsoleError("Blazor.start");

        await Session.SetOptionsAsync(new { workerSettings = new { errorTolerance = "strict" } });
        await Session.AddFaultAsync(new { path = @"harness-extra\.json", status = 404 });

        await GotoAsync();

        await WaitForInteractiveAsync();
        var aborted = await WaitForEventAsync("ERROR", e => e.Reason == "install-aborted");
        Assert.IsTrue(aborted.Fatal, "A strict abort was not reported as fatal.");
        Assert.IsTrue(aborted.FirstInstall, "A failed first install was not reported as one.");

        var failedAsset = (await EventsAsync()).FirstOrDefault(e => e.Type == "ERROR" && e.Reason == "fetch");
        Assert.IsNotNull(failedAsset, "The failing asset itself was not reported.");
        StringAssert.Contains(failedAsset.Url, "harness-extra.json");

        Assert.IsFalse(await IsControlledAsync(), "An aborted install ended up controlling the page.");
        await EventuallyAsync(() => BswupCacheNamesAsync(), names => names.Length == 0, "the partially filled bucket to be discarded");

        if (UsesProgressComponent)
        {
            await Expect(Page.Locator("#bit-bswup-error")).ToBeVisibleAsync();
            // A strict abort is usually transient, so the failure panel offers a retry.
            await Expect(Page.Locator("#bit-bswup-error-retry")).ToBeVisibleAsync();
        }
    }

    [TestMethod]
    public async Task An_asset_failing_under_lax_tolerance_is_skipped_and_cached_on_first_use()
    {
        AllowConsoleError("install error");

        await Session.AddFaultAsync(new { path = @"harness-extra\.json", status = 404 });

        await InstallAsync();

        var skipped = await WaitForEventAsync("ERROR", e => e.Reason == "fetch");
        Assert.IsFalse(skipped.Fatal, "A lax asset failure was reported as fatal.");
        if (UsesProgressComponent)
        {
            await Expect(Page.Locator("#bit-bswup-error")).ToBeHiddenAsync();
        }

        var manifest = await Session.ManifestAsync(AppPath);
        var key = PrecacheKeys(manifest).Single(url => url.Contains("harness-extra.json", StringComparison.Ordinal));
        var bucket = BucketName(manifest.Version);
        CollectionAssert.DoesNotContain(await CachedUrlsAsync(bucket), key);

        await Session.ClearFaultsAsync();
        var extra = await FetchAsync(AppUrl("_content/Bit.Bswup.Tests.Harness/harness-extra.json"));
        Assert.AreEqual(200, extra.Status, extra.ToString());

        await EventuallyAsync(() => CachedUrlsAsync(bucket), urls => urls.Contains(key), $"{key} to be cached by its first use");
    }

    /// <summary>Starts an update check without waiting for it: an accepted update reloads the page under it.</summary>
    protected Task CheckForUpdateAsync(Microsoft.Playwright.IPage? page = null) =>
        (page ?? Page).EvaluateAsync("() => { BitBswup.checkForUpdate(); }");

    /// <summary>
    /// Waits for every manifest asset to be in its bucket. The page starts before the post-start top-up has
    /// finished, and a test that changes the deployment right away would otherwise race it.
    /// </summary>
    protected Task WaitForPrecacheAsync(HarnessSession.AssetsManifest manifest)
    {
        var expected = PrecacheKeys(manifest);
        return EventuallyAsync(() => CachedUrlsAsync(BucketName(manifest.Version)), urls => expected.All(urls.Contains), "the precache to complete");
    }
}

[TestClass]
public class WebAssemblyLifecycleTests : AppLifecycleTests
{
    protected override string Mode => "wasm";
}

[TestClass]
public class WebAssemblyNoPrerenderLifecycleTests : AppLifecycleTests
{
    protected override string Mode => "wasm-noprerender";
}

[TestClass]
public class AutoLifecycleTests : AppLifecycleTests
{
    protected override string Mode => "auto";
}

[TestClass]
public class ServerLifecycleTests : AppLifecycleTests
{
    protected override string Mode => "server";
}

[TestClass]
public class StandaloneLifecycleTests : AppLifecycleTests
{
    // Every host process serves the standalone app; the WebAssembly one is started for other suites anyway.
    protected override string Mode => "wasm";

    protected override string AppPath => "/standalone/";
}
