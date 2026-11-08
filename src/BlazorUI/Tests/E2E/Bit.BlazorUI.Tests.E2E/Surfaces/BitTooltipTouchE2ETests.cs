using System.Diagnostics;

namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// Drives BitTooltip (/e2e/tooltip) with a finger on a touch device: a tap shows the tooltip and TouchHideDelay takes it
/// away again (the tap still pressing the anchor), TouchShowDelay turns the tooltip into a long press that a quick tap
/// does not reach, and NoTouch leaves the tap to the anchor alone. The long press is held through the Chromium DevTools
/// protocol, which is the one way to keep a real touch down; the other engines skip that test.
/// </summary>
[TestClass]
public class BitTooltipTouchE2ETests : E2ETestBase
{
    private const string Url = "/e2e/tooltip";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 1024, Height = 768 },
        HasTouch = true,
    };

    private ILocator Tip(string id) => Page.Locator($"#{id} [role=tooltip]");

    [TestMethod]
    public async Task Tap_ShowsTheTooltip_AndTouchHideDelayTakesItAwayAgain()
    {
        await OpenAsync(Url);

        // The tooltip is on the screen for the 800ms of TouchHideDelay only, and an expectation polling for it looks
        // again only a second after its third look, so on a loaded runner it can step right over the whole of it - the
        // first look delayed past the shown tooltip, the next one landing after it is gone. So the page watches for it
        // from before the tap, by what Playwright counts as visible: a box with a size, and visible. It looks whenever
        // anything about the tooltip changes (a mutation, a transition), and on a timer besides - for the fade-in, which
        // turns it visible with no mutation of its own - rather than on animation frames alone, which a loaded runner
        // throttles to fewer than the 800ms leaves room for.
        await Page.EvaluateAsync(@"() => {
            const tip = document.querySelector('#ttp-touch [role=tooltip]');
            const seen = window.__e2eTouchTip = { shown: false };
            const started = performance.now();
            let timer = 0;
            const observer = new MutationObserver(() => look());
            const events = ['transitionrun', 'transitionstart', 'transitionend', 'animationstart', 'animationend'];
            const stop = () => {
                observer.disconnect();
                clearTimeout(timer);
                events.forEach(e => tip.removeEventListener(e, look, true));
            };
            function look() {
                if (seen.shown) return;
                const box = tip.getBoundingClientRect();
                if (box.width > 0 && box.height > 0 && getComputedStyle(tip).visibility === 'visible') {
                    seen.shown = true;
                    stop();
                    return;
                }
                if (performance.now() - started >= 60000) stop();
            }
            const tick = () => {
                look();
                if (!seen.shown && performance.now() - started < 60000) timer = setTimeout(tick, 10);
            };
            observer.observe(tip, { attributes: true, childList: true, subtree: true, characterData: true });
            // The class that shows it can be put on an ancestor of the tooltip as well.
            for (let node = tip.parentElement; node; node = node.parentElement) {
                observer.observe(node, { attributes: true });
            }
            events.forEach(e => tip.addEventListener(e, look, true));
            tick();
        }");

        // Timed from the tap, which the 800ms TouchHideDelay can only start after, rather than from the moment the
        // tooltip was seen: an expectation can see it shown half a second late on a loaded runner, which would leave
        // little of the delay to measure.
        var watch = Stopwatch.StartNew();
        await Page.Locator("#ttp-touch-anchor").TapAsync();
        await Page.WaitForFunctionAsync("() => window.__e2eTouchTip.shown");
        await Expect(Page.Locator("#touch-anchor-clicks")).ToHaveTextAsync("1");

        await Expect(Tip("ttp-touch")).ToBeHiddenAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 700, $"Hidden {watch.ElapsedMilliseconds}ms after the tap that showed it.");
        await Expect(Page.Locator("#touch-shows")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task NoTouch_LeavesTheTapToTheAnchorAlone()
    {
        await OpenAsync(Url);

        await Page.Locator("#ttp-notouch-anchor").TapAsync();
        await Expect(Page.Locator("#notouch-anchor-clicks")).ToHaveTextAsync("1");
        await NextFramesAsync();
        await Expect(Tip("ttp-notouch")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task TouchShowDelay_AQuickTapIsNotEnough_ALongPressIs()
    {
        if (IsChromium is false) Assert.Inconclusive($"A touch is held down through the Chromium DevTools protocol, which {BrowserName} does not have.");

        await OpenAsync(Url);

        await Page.Locator("#ttp-longpress-anchor").TapAsync();
        await Page.WaitForTimeoutAsync(900);
        await Expect(Tip("ttp-longpress")).ToBeHiddenAsync();

        var box = await Page.Locator("#ttp-longpress-anchor").EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.x + r.width / 2, r.y + r.height / 2]; }");
        var session = await Page.Context.NewCDPSessionAsync(Page);
        await session.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchStart",
            ["touchPoints"] = new object[] { new Dictionary<string, object> { ["x"] = box[0], ["y"] = box[1], ["id"] = 1 } }
        });

        await Expect(Tip("ttp-longpress")).ToBeVisibleAsync();

        await session.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = "touchEnd", ["touchPoints"] = Array.Empty<object>() });
    }
}
