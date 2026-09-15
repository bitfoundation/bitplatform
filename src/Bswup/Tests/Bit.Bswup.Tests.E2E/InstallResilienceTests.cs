using Bit.Bswup.Tests.E2E.Infrastructure;

namespace Bit.Bswup.Tests.E2E;

/// <summary>
/// Install-time settings and failures: passive caching, Subresource Integrity against the bytes the host
/// really serves, transient failures, a broken manifest, and an install that goes silent.
/// </summary>
[TestClass]
public class InstallResilienceTests : BswupTest
{
    protected override string Mode => "wasm";

    [TestMethod]
    public async Task Passive_mode_caches_only_what_the_app_requested()
    {
        await Session.SetOptionsAsync(new { workerSettings = new { isPassive = true } });
        await InstallAsync();

        var manifest = await Session.ManifestAsync(AppPath);
        var bucket = BucketName(manifest.Version);
        var precache = PrecacheKeys(manifest);

        // The app's own boot requests went through the worker once it controlled the page.
        var cached = await EventuallyAsync(() => CachedUrlsAsync(bucket), urls => urls.Any(url => url.Contains("/_framework/", StringComparison.Ordinal)),
            "the requested framework files to be cached");

        // Give a (wrongly) started bulk download the time to show.
        await Task.Delay(3000);
        cached = await CachedUrlsAsync(bucket);

        Assert.IsTrue(cached.Length < precache.Length, $"Passive mode cached {cached.Length} of the {precache.Length} manifest assets.");
        Assert.IsFalse(cached.Any(url => url.Contains("harness-extra.json", StringComparison.Ordinal)), "Passive mode cached an asset the app never requested.");
    }

    [TestMethod]
    public async Task An_integrity_checked_install_accepts_the_bytes_the_host_serves()
    {
        await Session.SetOptionsAsync(new { workerSettings = new { enableIntegrityCheck = true } });
        await InstallAsync();

        var manifest = await Session.ManifestAsync(AppPath);
        var expected = PrecacheKeys(manifest);
        await EventuallyAsync(() => CachedUrlsAsync(BucketName(manifest.Version)), urls => expected.All(urls.Contains), "every asset to pass its integrity check");

        var errors = (await EventsAsync()).Where(e => e.Type == "ERROR").ToArray();
        Assert.AreEqual(0, errors.Length, string.Join(", ", errors.Select(e => e.ToString())));
    }

    [TestMethod]
    public async Task An_integrity_checked_install_rejects_tampered_bytes()
    {
        AllowConsoleError("install error");
        AllowConsoleError("integrity");

        await Session.SetOptionsAsync(new { workerSettings = new { enableIntegrityCheck = true } });
        await Session.AddFaultAsync(new { path = @"harness-extra\.json", corrupt = true });

        await InstallAsync();

        var rejected = await WaitForEventAsync("ERROR", e => e.Reason == "integrity");
        StringAssert.Contains(rejected.Url, "harness-extra.json");
        Assert.IsFalse(rejected.Fatal, "Under lax tolerance a tampered asset is skipped, not fatal.");

        var manifest = await Session.ManifestAsync(AppPath);
        Assert.IsFalse((await CachedUrlsAsync(BucketName(manifest.Version))).Any(url => url.Contains("harness-extra.json", StringComparison.Ordinal)),
            "Tampered bytes were cached.");
    }

    [TestMethod]
    public async Task A_transient_failure_is_retried_until_the_asset_downloads()
    {
        await Session.AddFaultAsync(new { path = @"harness-extra\.json", status = 503, times = 2 });

        await InstallAsync();

        var manifest = await Session.ManifestAsync(AppPath);
        var key = PrecacheKeys(manifest).Single(url => url.Contains("harness-extra.json", StringComparison.Ordinal));
        await EventuallyAsync(() => CachedUrlsAsync(BucketName(manifest.Version)), urls => urls.Contains(key), "the retried asset to be cached");

        Assert.IsFalse((await EventsAsync()).Any(e => e.Type == "ERROR"), "A failure that a retry recovered was reported.");

        var attempts = (await Session.RequestsAsync()).Where(r => r.Path.EndsWith("harness-extra.json", StringComparison.Ordinal)).Select(r => r.Status).ToArray();
        CollectionAssert.AreEqual(new[] { 503, 503, 200 }, attempts, $"Attempts: {string.Join(", ", attempts)}");
    }

    [TestMethod]
    public async Task An_unavailable_assets_manifest_fails_the_install_and_the_app_still_starts()
    {
        AllowConsoleError("install error");
        AllowConsoleError("Failed to load resource");

        await Session.AddFaultAsync(new { path = @"service-worker-assets\.js", status = 500 });

        await GotoAsync();
        await WaitForInteractiveAsync();

        var error = await WaitForEventAsync("ERROR", e => e.Reason == "manifest");
        Assert.IsTrue(error.Fatal, "An invalid manifest was not reported as fatal.");
        Assert.IsFalse(await IsControlledAsync(), "A worker without a manifest took control of the page.");
        Assert.AreEqual(0, (await BswupCacheNamesAsync()).Length);
    }

    [TestMethod]
    public async Task The_stall_watchdog_starts_the_app_when_a_first_install_goes_silent()
    {
        AllowNetworkErrors();

        // The worker script blocks on importing its manifest, so the page hears nothing at all.
        await Session.SetOptionsAsync(new { scriptAttributes = new Dictionary<string, string> { ["stallTimeout"] = "3" } });
        await Session.AddFaultAsync(new { path = @"service-worker-assets\.js", delayMs = 90_000 });

        var started = DateTime.UtcNow;
        await GotoAsync();
        await WaitForInteractiveAsync();

        var elapsed = DateTime.UtcNow - started;
        Assert.IsTrue(elapsed < TimeSpan.FromSeconds(45), $"The app started after {elapsed}, not after the 3 second stall timeout.");
        Assert.IsFalse(await IsControlledAsync());
    }
}
