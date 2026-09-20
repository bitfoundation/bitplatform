using Bit.Bswup.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bswup.Tests.E2E;

/// <summary>
/// Updates beyond the basic flow every host runs: prompting instead of reloading, several tabs, an update
/// already staged when a page loads, polling, a failed update, and two apps sharing an origin.
/// </summary>
[TestClass]
public class UpdateTests : BswupTest
{
    protected override string Mode => "wasm";

    [TestMethod]
    public async Task With_AutoReload_off_a_finished_update_waits_for_the_user_to_accept_it()
    {
        await Session.SetOptionsAsync(new { progress = new { autoReload = false } });
        await InstallReadyAsync();

        await Session.PublishVersionAsync(2);
        await StartUpdateCheckAsync();

        await Expect(Page.Locator("#bit-bswup-reload")).ToBeVisibleAsync(new() { Timeout = 120_000 });
        await Expect(Page.Locator("#bit-bswup-reload-status")).ToHaveTextAsync("A new version is ready to install.");
        await Task.Delay(2000);
        await ExpectLoadsAsync(1);

        await Page.Locator("#bit-bswup-reload").ClickAsync();

        await ExpectLoadsAsync(2);
        await WaitForInteractiveAsync();
        await AssertNoFurtherReloadAsync(2);
        var manifest = await Session.ManifestAsync(AppPath);
        await EventuallyAsync(() => BswupCacheNamesAsync(), names => names.SequenceEqual([BucketName(manifest.Version)]), "the new version's bucket");
    }

    [TestMethod]
    public async Task Accepting_an_update_in_one_tab_reloads_every_other_tab_onto_it()
    {
        await Session.SetOptionsAsync(new { progress = new { autoReload = false } });
        await InstallReadyAsync();

        var other = await NewPageAsync();
        await GotoAsync("counter", other);
        await WaitForControlledAsync(other);
        await WaitForInteractiveAsync(other);

        await Session.PublishVersionAsync(2);
        await StartUpdateCheckAsync();
        await Expect(Page.Locator("#bit-bswup-reload")).ToBeVisibleAsync(new() { Timeout = 120_000 });

        await Page.Locator("#bit-bswup-reload").ClickAsync();

        await ExpectLoadsAsync(2);
        await ExpectLoadsAsync(2, other);
        await WaitForInteractiveAsync();
        await WaitForInteractiveAsync(other);
        await Expect(other.Locator("#page-counter")).ToBeVisibleAsync();
        await AssertNoFurtherReloadAsync(2);
        await AssertNoFurtherReloadAsync(2, other);
    }

    [TestMethod]
    public async Task An_update_already_staged_when_a_page_loads_is_announced_there()
    {
        await Session.SetOptionsAsync(new { progress = new { autoReload = false } });
        await InstallReadyAsync();

        await Session.PublishVersionAsync(2);
        await StartUpdateCheckAsync();
        await Expect(Page.Locator("#bit-bswup-reload")).ToBeVisibleAsync(new() { Timeout = 120_000 });

        // A reload keeps the old worker in control (a waiting worker never takes over on navigation), so the
        // new document starts on the old version and has to learn about the staged one from the registration.
        await Page.ReloadAsync();

        await ExpectLoadsAsync(2);
        await WaitForInteractiveAsync();
        await Expect(Page.Locator("#bit-bswup-reload")).ToBeVisibleAsync();
        await WaitForEventAsync("UPDATE_READY");
    }

    [TestMethod]
    public async Task Update_polling_finds_and_applies_a_new_version_on_its_own()
    {
        await Session.SetOptionsAsync(new { scriptAttributes = new Dictionary<string, string> { ["updateInterval"] = "2" } });
        await InstallReadyAsync();

        await Session.PublishVersionAsync(2);

        await ExpectLoadsAsync(2);
        await WaitForInteractiveAsync();
        await AssertNoFurtherReloadAsync(2);
        var manifest = await Session.ManifestAsync(AppPath);
        await EventuallyAsync(() => BswupCacheNamesAsync(), names => names.SequenceEqual([BucketName(manifest.Version)]), "the polled update's bucket");
    }

    [TestMethod]
    public async Task A_failed_update_leaves_the_running_app_and_its_cache_alone()
    {
        AllowConsoleError("install error");

        await InstallReadyAsync();
        var original = await Session.ManifestAsync(AppPath);

        // An update migrates the unchanged hashed assets from the previous bucket without a request, so the fault goes
        // on a hash-less asset: those are downloaded again on every update.
        await Session.SetOptionsAsync(new { workerSettings = new { errorTolerance = "strict" } });
        await Session.AddFaultAsync(new { path = @"harness-version\.txt", status = 404 });
        await Session.PublishVersionAsync(2);
        await StartUpdateCheckAsync();

        var aborted = await WaitForEventAsync("ERROR", e => e.Reason == "install-aborted");
        Assert.IsFalse(aborted.FirstInstall, "A failed update was reported as a failed first install.");

        // The previous version keeps running and serving: no failure panel over the app, no prompt, no reload.
        await Expect(Page.Locator("#bit-bswup-error")).ToBeHiddenAsync();
        await Expect(Page.Locator("#bit-bswup")).ToBeHiddenAsync();
        await Expect(Page.Locator("#bit-bswup-reload")).ToBeHiddenAsync();
        await ExpectLoadsAsync(1);
        Assert.IsTrue(await IsControlledAsync());
        CollectionAssert.AreEqual(new[] { BucketName(original.Version) }, await BswupCacheNamesAsync());
    }

    [TestMethod]
    public async Task Two_apps_on_one_origin_install_and_update_without_touching_each_others_caches()
    {
        // The sub-path app first: once the root app's worker is registered, it would also control /standalone/
        // until the sub-path app registered a narrower scope of its own.
        var standalone = await NewPageAsync();
        await standalone.GotoAsync(Session.Origin + "/standalone/", new() { Timeout = 120_000 });
        await WaitForControlledAsync(standalone);
        await WaitForInteractiveAsync(standalone);

        await InstallReadyAsync();

        var standaloneBucket = $"bit-bswup:/standalone/ - {(await Session.ManifestAsync("/standalone/")).Version}";
        await EventuallyAsync(() => BswupCacheNamesAsync(), names => names.Contains(standaloneBucket) && names.Contains(BucketName(RootVersion)),
            "both apps' buckets");

        await Session.PublishVersionAsync(2);
        await StartUpdateCheckAsync();
        await ExpectLoadsAsync(2);
        await WaitForInteractiveAsync();

        var updatedRoot = BucketName((await Session.ManifestAsync(AppPath)).Version);
        var names = await EventuallyAsync(() => BswupCacheNamesAsync(), names => names.Contains(updatedRoot) && names.Contains(BucketName(RootVersion)) is false,
            "the root app's update to replace its own bucket");
        CollectionAssert.Contains(names, standaloneBucket, "Updating the root app pruned the sub-path app's cache.");

        // And the sub-path app still runs from its own worker and cache.
        await standalone.ReloadAsync();
        await WaitForInteractiveAsync(standalone);
        Assert.AreEqual($"{Session.Origin}/standalone/", await standalone.EvaluateAsync<string>("async () => (await navigator.serviceWorker.getRegistration()).scope"));
    }

    private string RootVersion { get; set; } = string.Empty;

    /// <summary>Installs the root app and waits for its precache to be complete, so a new version can be published.</summary>
    private async Task InstallReadyAsync()
    {
        await InstallAsync();
        var manifest = await Session.ManifestAsync(AppPath);
        RootVersion = manifest.Version;
        var expected = PrecacheKeys(manifest);
        await EventuallyAsync(() => CachedUrlsAsync(BucketName(manifest.Version)), urls => expected.All(urls.Contains), "the precache to complete");
    }

    private Task StartUpdateCheckAsync() => Page.EvaluateAsync("() => { BitBswup.checkForUpdate(); }");
}
