namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitDropMenu (/e2e/drop-menu) on a phone: a touch screen at a phone's viewport, where a tap opens
/// the Responsive drop menu as a panel that a swipe of the finger dismisses, and where OpenOnHover - there being no
/// hover - turns itself off and leaves the button to toggle the callout on a tap.
/// </summary>
[TestClass]
public class BitDropMenuMobileE2ETests : E2ETestBase
{
    private const string Url = "/e2e/drop-menu";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        // Firefox has no mobile emulation; the touch screen and the viewport are what the drop menu reads anyway.
        IsMobile = IsFirefox is false
    };

    [TestMethod]
    public async Task Responsive_ATapOpensThePanel_AndASwipeOfTheFingerDismissesIt()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit has no Touch constructor, so a swipe of the finger cannot be synthesized there.");

        await OpenAsync(Url);

        var button = Page.Locator("#dm-responsive > .bit-drm-btn");
        await button.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
        await NextFramesAsync();

        await button.TapAsync();

        var callout = Page.Locator($"[id='{await button.GetAttributeAsync("aria-controls")}']");
        await Expect(button).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-drm-end"));
        await Expect(callout).ToHaveCSSAsync("opacity", "1");
        await Expect(callout).ToHaveCSSAsync("transform", new System.Text.RegularExpressions.Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));

        // Measured against the layout viewport the panel is fixed to, which a mobile browser sizes itself.
        var edges = await callout.EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.right, window.innerWidth, r.top, r.height]; }");
        Assert.AreEqual(edges[1], edges[0], 2, "Pinned to the right edge.");
        Assert.AreEqual(0, edges[2], 2, "From the top of the screen.");
        Assert.IsTrue(edges[3] > 300, $"Down the screen ({edges[3]}px).");

        var box = await callout.BoundingBoxAsync();
        Assert.IsNotNull(box);

        // A finger dragged toward the edge the panel is pinned to, past a quarter of its width. The header row of the
        // panel is not its scroll container, so the drag is the panel's own.
        await callout.EvaluateAsync(@"(el, [x0, y, x1]) => {
            const touch = x => new Touch({ identifier: 1, target: el, clientX: x, clientY: y, screenX: x, screenY: y });
            const fire = (type, x, active) => el.dispatchEvent(new TouchEvent(type, {
                touches: active ? [touch(x)] : [], targetTouches: active ? [touch(x)] : [], changedTouches: [touch(x)],
                bubbles: true, cancelable: true
            }));
            fire('touchstart', x0, true);
            for (let i = 1; i <= 10; i++) fire('touchmove', x0 + (x1 - x0) * i / 10, true);
            fire('touchend', x1, false);
        }", new[] { box.X + 10, box.Y + 15, box.X + 10 + box.Width * 0.6 });

        await Expect(button).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#responsive-dismisses")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task OpenOnHover_OnATouchScreen_TheButtonTogglesTheCalloutOnATap()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit's mobile emulation dispatches no click for a tap on the drop menu's overlay (a div without a pointer cursor, which iOS does not treat as clickable), so the tap outside does not close the callout there.");

        await OpenAsync(Url);

        var button = Page.Locator("#dm-hover > .bit-drm-btn");
        await button.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
        await NextFramesAsync();

        var calloutId = await button.GetAttributeAsync("aria-controls");
        var callout = Page.Locator($"[id='{calloutId}']");
        var overlay = Page.Locator($"[id='{calloutId!.Replace("-callout", "-overlay")}']");

        await button.TapAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#hover-opens")).ToHaveTextAsync("1");
        // No hover to close it, so the overlay takes the taps that close it like any other drop menu's.
        await Expect(overlay).Not.ToHaveClassAsync(HasClass("bit-drm-ovl-hov"));

        await Page.Touchscreen.TapAsync(5, 5);
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#hover-dismisses")).ToHaveTextAsync("1");
    }
}
