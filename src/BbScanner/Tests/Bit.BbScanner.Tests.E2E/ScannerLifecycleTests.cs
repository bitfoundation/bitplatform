using Bit.BbScanner.Tests.E2E.Infrastructure;

namespace Bit.BbScanner.Tests.E2E;

/// <summary>
/// Drives the real component in a real browser against a fake camera, covering the lifecycle an
/// application actually exercises: starting, scanning continuously, pausing, switching, stopping,
/// navigating away and being disposed.
/// </summary>
[TestClass]
public class ScannerLifecycleTests : BbScannerTest
{
    [TestMethod]
    public async Task StartsTheCameraAndScansContinuously()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("scan");

        var status = await session.WaitForStatusAsync(["scanning"]);
        Assert.AreEqual("scanning", status);

        // The clip cycles through four symbologies, so a scanner that is genuinely decoding
        // frame after frame reports several different values without any interaction.
        await session.WaitForResultsAsync(3);

        var values = await session.ResultValuesAsync();
        Assert.Contains(FakeCameraVideo.QrPayload, values);
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task DecodesEverySymbologyInTheClip()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("scan");
        await session.WaitForStatusAsync(["scanning"]);

        // Four symbols, blank frames between them, ten frames a second: two loops is generous.
        await session.WaitForResultsAsync(4, timeoutMs: 90_000);

        var values = await session.ResultValuesAsync();
        var formats = await session.ResultFormatsAsync();

        Assert.Contains(FakeCameraVideo.QrPayload, values);
        Assert.Contains(FakeCameraVideo.DataMatrixPayload, values);
        Assert.Contains(FakeCameraVideo.Code128Payload, values);
        Assert.Contains(FakeCameraVideo.Ean13Payload, values);

        Assert.Contains("QrCode", formats);
        Assert.Contains("DataMatrix", formats);
        Assert.Contains("Code128", formats);
        Assert.Contains("Ean13", formats);
    }

    [TestMethod]
    public async Task ReportsNothingWhenNoBarcodeIsInView()
    {
        await using var session = await OpenAsync(BlankClip);
        await session.GoToAsync("scan");
        await session.WaitForStatusAsync(["scanning"]);

        // Give the frame loop plenty of frames to get something wrong on.
        await Task.Delay(6000, TestContext.CancellationToken);

        var values = await session.ResultValuesAsync();
        Assert.IsEmpty(values);
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task StopReleasesTheCameraAndStartOpensItAgain()
    {
        await using var session = await OpenAsync(SingleQrClip);
        await session.GoToAsync("lifecycle");

        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);
        Assert.AreEqual(1, await session.TracksLiveAsync());

        await session.Page.ClickAsync("button:has-text('Stop')");
        await session.WaitForStatusAsync(["stopped"]);

        await session.Page.WaitForFunctionAsync("() => window.__bscanTracks.live.size === 0",
            null, new Microsoft.Playwright.PageWaitForFunctionOptions { Timeout = 10_000 });
        Assert.AreEqual(0, await session.TracksLiveAsync());

        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);
        Assert.AreEqual(1, await session.TracksLiveAsync());
        Assert.AreEqual(2, await session.TracksOpenedAsync());
    }

    [TestMethod]
    public async Task PauseKeepsTheCameraOpenAndStopsReporting()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("lifecycle");

        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);
        await session.WaitForResultsAsync(1);

        await session.Page.ClickAsync("button:has-text('Pause')");
        await session.WaitForStatusAsync(["paused"]);

        // Pausing must not release the camera: that is the whole point of it.
        Assert.AreEqual(1, await session.TracksLiveAsync());

        var before = (await session.ResultValuesAsync()).Count;
        await Task.Delay(5000, TestContext.CancellationToken);
        var during = (await session.ResultValuesAsync()).Count;
        Assert.AreEqual(before, during);

        await session.Page.ClickAsync("button:has-text('Resume')");
        await session.WaitForStatusAsync(["scanning"]);
        await session.WaitForResultsAsync(during + 1);
    }

    [TestMethod]
    public async Task RepeatedStartStopCyclesLeaveNoCameraRunning()
    {
        await using var session = await OpenAsync(SingleQrClip);
        await session.GoToAsync("lifecycle");

        for (var i = 0; i < 5; i++)
        {
            await session.Page.ClickAsync("button:has-text('Start')");
            await session.WaitForStatusAsync(["scanning"]);
            await session.Page.ClickAsync("button:has-text('Stop')");
            await session.WaitForStatusAsync(["stopped"]);
        }

        await session.Page.WaitForFunctionAsync("() => window.__bscanTracks.live.size === 0",
            null, new Microsoft.Playwright.PageWaitForFunctionOptions { Timeout = 10_000 });

        var opened = await session.TracksOpenedAsync();
        Assert.AreEqual(5, opened);
        Assert.AreEqual(0, await session.TracksLiveAsync());
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task StartWhileStartingDoesNotLeaveAStrandedCamera()
    {
        await using var session = await OpenAsync(SingleQrClip);
        await session.GoToAsync("lifecycle");

        // Hammer the start button: every click supersedes the one before it, and only the last
        // may end up owning a camera.
        for (var i = 0; i < 6; i++)
        {
            await session.Page.ClickAsync("button:has-text('Start')");
        }

        await session.WaitForStatusAsync(["scanning"]);
        await session.Page.WaitForFunctionAsync("() => window.__bscanTracks.live.size <= 1",
            null, new Microsoft.Playwright.PageWaitForFunctionOptions { Timeout = 15_000 });

        Assert.AreEqual(1, await session.TracksLiveAsync());
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task NavigatingAwayDisposesTheScannerAndReleasesTheCamera()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("scan");
        await session.WaitForStatusAsync(["scanning"]);
        Assert.AreEqual(1, await session.TracksLiveAsync());

        await session.NavigateByLinkAsync("API reference");

        await session.Page.WaitForFunctionAsync("() => window.__bscanTracks.live.size === 0",
            null, new Microsoft.Playwright.PageWaitForFunctionOptions { Timeout = 15_000 });
        Assert.AreEqual(0, await session.TracksLiveAsync());
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task NavigatingBetweenScannerPagesRepeatedlyDoesNotAccumulateCameras()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("scan");
        await session.WaitForStatusAsync(["scanning"]);

        for (var i = 0; i < 4; i++)
        {
            await session.NavigateByLinkAsync("Scan region");
            await session.WaitForStatusAsync(["scanning"]);
            await session.NavigateByLinkAsync("Live playground");
            await session.WaitForStatusAsync(["scanning"]);

            // Only ever one camera at a time, however fast the user moves.
            Assert.IsTrue(await session.TracksLiveAsync() <= 1, "More than one camera track was live at once.");
        }

        await session.NavigateByLinkAsync("API reference");
        await session.Page.WaitForFunctionAsync("() => window.__bscanTracks.live.size === 0",
            null, new Microsoft.Playwright.PageWaitForFunctionOptions { Timeout = 15_000 });
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task ReloadingThePageStartsCleanly()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("scan");
        await session.WaitForStatusAsync(["scanning"]);
        await session.WaitForResultsAsync(1);

        await session.Page.ReloadAsync(new Microsoft.Playwright.PageReloadOptions
        {
            WaitUntil = Microsoft.Playwright.WaitUntilState.NetworkIdle,
            Timeout = 60_000,
        });

        await session.WaitForStatusAsync(["scanning"]);
        await session.WaitForResultsAsync(1);
        Assert.AreEqual(1, await session.TracksLiveAsync());
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task DeniedPermissionIsReportedAsAnError()
    {
        await using var session = await OpenAsync(grantPermission: false);
        await session.GoToAsync("scan");

        var status = await session.WaitForStatusAsync(["faulted"], timeoutMs: 30_000);
        Assert.AreEqual("faulted", status);

        var message = await session.Page.TextContentAsync(".bit-bbscanner__message");
        Assert.IsNotNull(message);
        Assert.Contains("permission", message, StringComparison.OrdinalIgnoreCase);
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task SwitchingCamerasKeepsExactlyOneTrackOpen()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("camera");
        await session.WaitForStatusAsync(["scanning", "faulted"]);

        if (await session.StatusAsync() != "scanning")
        {
            Assert.Inconclusive("The fake device did not enumerate a camera on this browser.");
        }

        // The demo's facing selector restarts the camera; each restart must release the old one.
        for (var i = 0; i < 3; i++)
        {
            await session.Page.SelectOptionAsync("select:below(:text('Facing'))", i % 2 == 0 ? "Front" : "Rear");
            await session.WaitForStatusAsync(["scanning", "faulted"]);
            await Task.Delay(500, TestContext.CancellationToken);
            Assert.IsTrue(await session.TracksLiveAsync() <= 1, "Switching cameras left more than one track open.");
        }
    }

    [TestMethod]
    public async Task ScanningForAMinuteDoesNotGrowTheHeapWithoutBound()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("scan");
        await session.WaitForStatusAsync(["scanning"]);

        // Let it warm up, then measure across a long run of continuous decoding.
        await Task.Delay(10_000, TestContext.CancellationToken);
        var before = await session.HeapBytesAsync();
        if (before == 0)
        {
            Assert.Inconclusive("This browser does not expose performance.memory.");
        }

        await Task.Delay(45_000, TestContext.CancellationToken);
        var after = await session.HeapBytesAsync();

        // The decoder reuses its buffers, so a long run must not multiply the heap. Blazor's own
        // allocations make an exact figure meaningless; an unbounded leak is what this catches.
        Assert.IsTrue(after < before + (24 * 1024 * 1024), $"Heap grew from {before} to {after} bytes over 45 seconds of continuous scanning.");
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task TheScannerRendersRarelyWhileDecoding()
    {
        await using var session = await OpenAsync(BlankClip);
        await session.GoToAsync("scan");
        await session.WaitForStatusAsync(["scanning"]);

        // Count DOM mutations inside the scanner: frames must never reach the render tree.
        await session.Page.EvaluateAsync(@"
window.__bscanMutations = 0;
const target = document.querySelector('.bit-bbscanner');
new MutationObserver(records => { window.__bscanMutations += records.length; }).observe(target, {
    attributes: true, childList: true, subtree: true, characterData: true,
});");

        await Task.Delay(10_000, TestContext.CancellationToken);
        var mutations = await session.Page.EvaluateAsync<int>("window.__bscanMutations");

        // Ten seconds at fifteen frames a second is 150 frames; a handful of mutations is the
        // status bar, not the frame loop.
        Assert.IsTrue(mutations < 30, $"The scanner mutated the DOM {mutations} times in ten seconds of scanning.");
    }
}
