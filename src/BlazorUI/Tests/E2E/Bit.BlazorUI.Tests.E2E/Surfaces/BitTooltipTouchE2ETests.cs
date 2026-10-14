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

        await Page.Locator("#ttp-touch-anchor").TapAsync();
        await Expect(Tip("ttp-touch")).ToBeVisibleAsync();
        await Expect(Page.Locator("#touch-anchor-clicks")).ToHaveTextAsync("1");

        var watch = Stopwatch.StartNew();
        await Expect(Tip("ttp-touch")).ToBeHiddenAsync();
        Assert.IsTrue(watch.ElapsedMilliseconds >= 500, $"Hidden {watch.ElapsedMilliseconds}ms after it was shown by the tap.");
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
