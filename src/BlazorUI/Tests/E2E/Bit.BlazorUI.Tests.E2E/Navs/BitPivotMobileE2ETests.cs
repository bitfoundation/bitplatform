namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitPivot (/e2e/pivot) on a phone: a touch screen at a phone's viewport, where the overflow menu
/// is opened and closed with taps.
/// </summary>
[TestClass]
public class BitPivotMobileE2ETests : E2ETestBase
{
    private const string Url = "/e2e/pivot";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        // Firefox has no mobile emulation; the touch screen and the viewport are what the pivot reads anyway.
        IsMobile = IsFirefox is false
    };

    [TestMethod]
    public async Task OverflowMenu_ATapOutsideClosesIt()
    {
        await OpenAsync(Url);

        var more = Page.Locator("#pv-menu > .bit-pvt-hwr > .bit-pvt-mor");
        var menu = Page.Locator("#pv-menu > .bit-pvt-hwr > [role='menu']");
        await Expect(more).ToBeVisibleAsync();
        await more.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
        await NextFramesAsync();

        await more.TapAsync();
        await Expect(menu).ToBeVisibleAsync();
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "true");

        // The overlay that takes the tap is a div without a pointer cursor, which WebKit's mobile emulation - like iOS -
        // dispatches a click for only because it carries a click listener of its own.
        await Page.Touchscreen.TapAsync(5, 5);
        await Expect(menu).ToBeHiddenAsync();
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
