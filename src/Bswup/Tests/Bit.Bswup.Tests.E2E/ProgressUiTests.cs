using Bit.Bswup.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bswup.Tests.E2E;

/// <summary>The built-in progress UI - BswupProgress and bit-bswup.progress.js - as the user sees it.</summary>
[TestClass]
public class ProgressUiTests : BswupTest
{
    protected override string Mode => "wasm";

    [TestMethod]
    public async Task The_first_install_splash_reports_the_download_and_goes_away_when_the_app_starts()
    {
        await Session.SetOptionsAsync(new { progress = new { showAssets = true } });

        await InstallAsync();

        await Expect(Page.Locator("#bit-bswup-percent")).ToHaveTextAsync("100%");
        await Expect(Page.Locator("#bit-bswup-progress-bar")).ToHaveAttributeAsync("aria-valuenow", "100");
        Assert.IsTrue(await Page.Locator("#bit-bswup-assets li").CountAsync() > 0, "ShowAssets listed no downloaded asset.");
        await Expect(Page.Locator("#bit-bswup")).ToBeHiddenAsync();
        await Expect(Page.Locator("#bit-bswup-reload")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task A_background_update_shows_the_splash_by_default()
    {
        await Session.SetOptionsAsync(new { progress = new { autoReload = false } });
        await InstallAndUpdateAsync();

        await Expect(Page.Locator("#bit-bswup")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShowOnUpdate_off_keeps_the_splash_away_from_a_background_update()
    {
        await Session.SetOptionsAsync(new { progress = new { autoReload = false, showOnUpdate = false } });
        await InstallAndUpdateAsync();

        await Expect(Page.Locator("#bit-bswup")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task HideApp_hides_the_app_container_while_the_first_install_downloads()
    {
        await Session.SetOptionsAsync(new { progress = new { hideApp = true } });
        // Slow the download down enough to observe it.
        await Session.AddFaultAsync(new { path = @"harness-extra\.json", delayMs = 4000, times = 1 });

        await GotoAsync();

        await Expect(Page.Locator("#app")).ToBeHiddenAsync(new() { Timeout = 60_000 });
        await Expect(Page.Locator("#bit-bswup")).ToBeVisibleAsync();

        await WaitForControlledAsync();
        await WaitForInteractiveAsync();
        await Expect(Page.Locator("#app")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task The_fingerprinted_Blazor_script_is_recognized()
    {
        if (E2EEnvironment.FrameworkFingerprintsBlazorScript(Framework) is false)
            Assert.Inconclusive("blazor.web.js is fingerprinted from .NET 10 on.");

        await Session.SetOptionsAsync(new { fingerprintedBlazorScript = true });

        await InstallAsync();

        var src = await Page.Locator("script[autostart]").GetAttributeAsync("src");
        Assert.AreNotEqual("_framework/blazor.web.js", src, "The page did not reference the fingerprinted script.");
    }

    [TestMethod]
    public async Task A_first_install_completes_without_any_handler_function()
    {
        // No BswupProgress, no progress script, and a handler name nothing defines.
        await Session.SetOptionsAsync(new
        {
            progress = new { placement = "none" },
            scriptAttributes = new Dictionary<string, string> { ["handler"] = "noSuchHandler" },
        });

        var started = DateTime.UtcNow;
        await InstallAsync();

        var elapsed = DateTime.UtcNow - started;
        Assert.IsTrue(elapsed < TimeSpan.FromSeconds(45), $"The install took {elapsed}: it waited for the stall watchdog instead of completing.");
        await ExpectLoadsAsync(1);
    }

    private async Task InstallAndUpdateAsync()
    {
        await InstallAsync();
        var manifest = await Session.ManifestAsync(AppPath);
        var expected = PrecacheKeys(manifest);
        await EventuallyAsync(() => CachedUrlsAsync(BucketName(manifest.Version)), urls => expected.All(urls.Contains), "the precache to complete");

        await Session.PublishVersionAsync(2);
        await Page.EvaluateAsync("() => { BitBswup.checkForUpdate(); }");

        await Expect(Page.Locator("#bit-bswup-reload")).ToBeVisibleAsync(new() { Timeout = 120_000 });
    }
}

/// <summary>
/// BswupProgress rendered by the interactive renderer of an app that prerenders nothing: its element only exists
/// once Blazor has started - which on a first install only happens after the install completed.
/// </summary>
[TestClass]
public class InteractiveProgressUiTests : BswupTest
{
    protected override string Mode => "wasm-noprerender";

    [TestMethod]
    public async Task A_progress_component_rendered_after_start_still_initializes_and_the_install_completes()
    {
        await Session.SetOptionsAsync(new { progress = new { placement = "interactive" } });

        await InstallAsync();

        await Expect(Page.Locator("#bit-bswup")).ToHaveAttributeAsync("data-bit-bswup-initialized", "true");
        await ExpectLoadsAsync(1);
    }
}
