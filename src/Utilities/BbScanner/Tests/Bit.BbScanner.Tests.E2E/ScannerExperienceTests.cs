using Bit.BbScanner.Tests.E2E.Infrastructure;

namespace Bit.BbScanner.Tests.E2E;

/// <summary>
/// What the scanner does for the person holding the device: its interface in their language, keyboard focus that
/// survives stopping, a page sent to the background and brought back, and the vibration or beep of a scan.
/// </summary>
/// <remarks>
/// Headless Chromium never hides a page, so hiding one is simulated: <c>document.visibilityState</c> is overridden
/// and <c>visibilitychange</c> raised, which is all the bridge reads. A phone ending a backgrounded page's camera is
/// simulated by ending the track. What a particular phone actually does in the background is for a device to show.
/// </remarks>
[TestClass]
public class ScannerExperienceTests : BbScannerTest
{
    private const string SetHidden = @"hidden => {
        if (hidden) {
            Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => 'hidden' });
            Object.defineProperty(document, 'hidden', { configurable: true, get: () => true });
        } else {
            delete document.visibilityState;
            delete document.hidden;
        }
        document.dispatchEvent(new Event('visibilitychange'));
    }";

    private const string EndTracks = "() => { for (const t of window.__bscanTracks.live) { t.stop(); t.dispatchEvent(new Event('ended')); } }";

    [TestMethod]
    public async Task TheInterfaceIsInTheLanguageTheApplicationChose()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("customize");
        await session.WaitForStatusAsync(["scanning"]);

        await session.Page.SelectOptionAsync("#scanner-texts", "es");
        await session.Page.Locator(".bit-bbscanner__button", new() { HasText = "Detener" }).WaitForAsync(new() { Timeout = 10_000 });
        Assert.Contains("Escaneando", await session.Page.TextContentAsync(".bit-bbscanner__status") ?? "");
        Assert.Contains("Escanear imagen", await session.Page.TextContentAsync(".bit-bbscanner__controls") ?? "");

        // A right-to-left language: the texts change and the controls run from the right.
        await session.Page.SelectOptionAsync("#scanner-texts", "fa");
        await session.Page.Locator(".bit-bbscanner__button", new() { HasText = "توقف" }).WaitForAsync(new() { Timeout = 10_000 });
        var direction = await session.Page.EvaluateAsync<string>("() => getComputedStyle(document.querySelector('.bit-bbscanner__controls')).direction");
        Assert.AreEqual("rtl", direction);

        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task StoppingFromTheKeyboardLeavesFocusOnTheButtonThatStartsAgain()
    {
        await using var session = await OpenAsync();
        await session.GoToAsync("customize");
        await session.WaitForStatusAsync(["scanning"]);

        await session.Page.FocusAsync(".bit-bbscanner__button:has-text('Stop')");
        await session.Page.Keyboard.PressAsync("Enter");
        await session.WaitForStatusAsync(["stopped"]);

        // The button that was pressed is still the focused element, now offering to start: focus did not fall back
        // to the top of the page, which is where a keyboard user would otherwise have to find their way back from.
        var focused = await session.Page.EvaluateAsync<string>(
            "() => document.activeElement && document.activeElement.classList.contains('bit-bbscanner__button') ? document.activeElement.textContent.trim() : ''");
        Assert.AreEqual("Start", focused);

        await session.Page.Keyboard.PressAsync("Enter");
        await session.WaitForStatusAsync(["scanning"]);
    }

    [TestMethod]
    public async Task ACameraEndedWhileThePageWasHiddenIsReopenedWhenItReturns()
    {
        await using var session = await OpenAsync(SingleQrClip);
        await session.GoToAsync("lifecycle");
        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);
        await session.WaitForResultsAsync(1);

        // What a phone does to a page it sends to the background: the page is hidden and its camera ended.
        await session.Page.EvaluateAsync(SetHidden, true);
        await session.Page.EvaluateAsync(EndTracks);

        // Not faulted: on its way back, and holding no camera meanwhile.
        Assert.AreEqual("starting", await session.WaitForStatusAsync(["starting", "faulted"], timeoutMs: 20_000));
        Assert.AreEqual(0, await session.TracksLiveAsync());

        await session.Page.EvaluateAsync(SetHidden, false);
        await session.WaitForStatusAsync(["scanning"], timeoutMs: 20_000);
        Assert.AreEqual(1, await session.TracksLiveAsync());
        Assert.AreEqual(2, await session.TracksOpenedAsync());

        // And it is decoding again, not just showing the camera: reopening forgets what was reported, so the symbol
        // still in view is reported a second time.
        await session.WaitForResultsAsync(2, timeoutMs: 30_000);
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task OncePerSessionStaysOnceAcrossACameraReopenedAfterTheBackground()
    {
        // A stocktake under an infinite window: a phone locked and unlocked mid-count must not count again what it had.
        await using var session = await OpenAsync(SingleQrClip);
        await session.GoToAsync("lifecycle");
        await session.Page.CheckAsync("#once-per-session");
        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);
        await session.WaitForResultsAsync(1);

        await session.Page.EvaluateAsync(SetHidden, true);
        await session.Page.EvaluateAsync(EndTracks);
        await session.WaitForStatusAsync(["starting"], timeoutMs: 20_000);
        await session.Page.EvaluateAsync(SetHidden, false);
        await session.WaitForStatusAsync(["scanning"], timeoutMs: 20_000);
        Assert.AreEqual(2, await session.TracksOpenedAsync());

        // The same symbol stays in view the whole time; several seconds of decoding later it is still counted once.
        await session.Page.WaitForTimeoutAsync(6_000);
        Assert.HasCount(1, await session.ResultValuesAsync());
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task AScannerPausedInTheBackgroundComesBackPaused()
    {
        await using var session = await OpenAsync(SingleQrClip);
        await session.GoToAsync("lifecycle");
        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);
        await session.Page.ClickAsync("button:has-text('Pause')");
        await session.WaitForStatusAsync(["paused"]);

        await session.Page.EvaluateAsync(SetHidden, true);
        await session.Page.EvaluateAsync(EndTracks);
        await session.WaitForStatusAsync(["starting"], timeoutMs: 20_000);

        await session.Page.EvaluateAsync(SetHidden, false);
        await session.WaitForStatusAsync(["paused"], timeoutMs: 20_000);
        Assert.AreEqual(1, await session.TracksLiveAsync());
    }

    [TestMethod]
    public async Task ReleasingTheCameraWhileHiddenTurnsItOffAndBackOn()
    {
        await using var session = await OpenAsync(SingleQrClip);
        await session.GoToAsync("lifecycle");
        await session.Page.CheckAsync("#release-when-hidden");
        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);
        Assert.AreEqual(1, await session.TracksLiveAsync());

        await session.Page.EvaluateAsync(SetHidden, true);
        await session.WaitForStatusAsync(["starting"], timeoutMs: 20_000);
        await session.Page.WaitForFunctionAsync("() => window.__bscanTracks.live.size === 0",
            null, new Microsoft.Playwright.PageWaitForFunctionOptions { Timeout = 10_000 });

        await session.Page.EvaluateAsync(SetHidden, false);
        await session.WaitForStatusAsync(["scanning"], timeoutMs: 20_000);
        Assert.AreEqual(1, await session.TracksLiveAsync());
        Assert.AreEqual(2, await session.TracksOpenedAsync());
        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task WithoutTheOptionAHiddenPageKeepsItsCamera()
    {
        await using var session = await OpenAsync(SingleQrClip);
        await session.GoToAsync("lifecycle");
        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);

        await session.Page.EvaluateAsync(SetHidden, true);
        await Task.Delay(3000, TestContext.CancellationToken);

        Assert.AreEqual("scanning", await session.StatusAsync());
        Assert.AreEqual(1, await session.TracksLiveAsync());
        Assert.AreEqual(1, await session.TracksOpenedAsync());
    }

    [TestMethod]
    public async Task ThePermissionCanBeCheckedWithoutOpeningTheCamera()
    {
        await using var granted = await OpenAsync(BlankClip);
        await granted.GoToAsync("errors");
        await granted.Page.ClickAsync("#check-permission");
        Assert.AreEqual("Granted", (await granted.Page.Locator("#camera-permission").TextContentAsync())?.Trim());
        Assert.AreEqual(0, await granted.TracksOpenedAsync(), "Asking about the permission opened the camera.");

        await using var refused = await OpenAsync(BlankClip, grantPermission: false);
        await refused.GoToAsync("errors");
        await refused.Page.ClickAsync("#check-permission");
        var state = (await refused.Page.Locator("#camera-permission").TextContentAsync())?.Trim();
        Assert.IsTrue(state is "Prompt" or "Denied", $"A browser that has not been given the camera reported '{state}'.");
        Assert.AreEqual(0, await refused.TracksOpenedAsync());
    }

    [TestMethod]
    public async Task StoppingLetsGoOfTheLastFrame()
    {
        await using var session = await OpenAsync(SingleQrClip);
        await session.GoToAsync("lifecycle");

        // Restarting after a stop has to rebuild the frame readback the stop released; scanning again proves it did.
        for (var i = 0; i < 2; i++)
        {
            await session.Page.ClickAsync("button:has-text('Start')");
            await session.WaitForStatusAsync(["scanning"]);
            await session.WaitForResultsAsync(i + 1, timeoutMs: 30_000);
            await session.Page.ClickAsync("button:has-text('Stop')");
            await session.WaitForStatusAsync(["stopped"]);
        }

        Assert.IsEmpty(session.PageErrors);
    }

    [TestMethod]
    public async Task AScanVibratesAndBeepsOnlyWhenAskedTo()
    {
        await using var session = await OpenAsync(SingleQrClip);

        // Count the calls rather than feel them: vibrate is replaced, and every tone the page plays starts an
        // oscillator.
        await session.Page.AddInitScriptAsync(@"
window.__bscanFeedback = { vibrations: 0, tones: 0 };
navigator.vibrate = () => { window.__bscanFeedback.vibrations++; return true; };
const createOscillator = AudioContext.prototype.createOscillator;
AudioContext.prototype.createOscillator = function () { window.__bscanFeedback.tones++; return createOscillator.call(this); };
");
        await session.GoToAsync("lifecycle");
        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);
        await session.WaitForResultsAsync(1);
        Assert.AreEqual(0, await session.Page.EvaluateAsync<int>("window.__bscanFeedback.vibrations + window.__bscanFeedback.tones"));

        // Restarting forgets the value already reported, so the same symbol is reported again - now with feedback.
        await session.Page.ClickAsync("button:has-text('Stop')");
        await session.WaitForStatusAsync(["stopped"]);
        await session.Page.CheckAsync("#feedback-vibrate");
        await session.Page.CheckAsync("#feedback-beep");
        await session.Page.ClickAsync("button:has-text('Start')");
        await session.WaitForStatusAsync(["scanning"]);
        await session.WaitForResultsAsync(2, timeoutMs: 30_000);

        var reported = (await session.ResultValuesAsync()).Count - 1;
        var vibrations = await session.Page.EvaluateAsync<int>("window.__bscanFeedback.vibrations");
        var tones = await session.Page.EvaluateAsync<int>("window.__bscanFeedback.tones");

        // One signal for each scan reported: not one for each frame the symbol was decoded in.
        Assert.IsGreaterThanOrEqualTo(1, vibrations);
        Assert.IsLessThanOrEqualTo(reported, vibrations, "More vibrations than scans reported.");
        Assert.AreEqual(vibrations, tones);
        Assert.IsEmpty(session.PageErrors);
    }
}
