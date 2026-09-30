using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the Escape dismissal of BitOverlay, driven with real keyboard input.
///
/// The Overlay does not take the focus when it opens, so the key is watched for on the window and handed to the
/// topmost open Overlay alone (Utils.watchLayerEscape). bUnit cannot see that - it has no window and no focus -
/// so the behavior is pinned down here, in a real browser.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitOverlayBrowserTests
public class BitOverlayBrowserTests : PerformanceTestBase
{
    [TestMethod]
    public async Task BitOverlay_Escape_ClosesAnOverlayOpenedFromAButtonThatStillHoldsTheFocus()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/overlay-escape");
        await WaitForStatus("Ready");

        var button = Page.Locator("#btn-open-lower");
        await button.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForStatus("Lower open");
        await Expect(Page.Locator(".bit-ovl-opn")).ToHaveCountAsync(1);

        // The focus stayed on the button behind the layer.
        await Expect(button).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#lower-state")).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task BitOverlay_Escape_ClosesTheTopmostOverlayOnly()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/overlay-escape");
        await WaitForStatus("Ready");

        await Page.Locator("#btn-open-both").ClickAsync();
        await WaitForStatus("Both open");
        await Expect(Page.Locator(".bit-ovl-opn")).ToHaveCountAsync(2);

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#upper-state")).ToHaveTextAsync("closed");
        await Expect(Page.Locator("#lower-state")).ToHaveTextAsync("open");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#lower-state")).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task BitOverlay_Escape_NeverFallsThroughABlockingOverlay()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/overlay-escape");
        await WaitForStatus("Ready");

        await Page.Locator("#chk-upper-blocking").CheckAsync();
        await Page.Locator("#btn-open-both").ClickAsync();
        await WaitForStatus("Both open");
        await Expect(Page.Locator(".bit-ovl-opn")).ToHaveCountAsync(2);

        await Page.Keyboard.PressAsync("Escape");

        // Give a wrongly routed press the time to have closed something.
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#upper-state")).ToHaveTextAsync("open");
        await Expect(Page.Locator("#lower-state")).ToHaveTextAsync("open");
    }

    [TestMethod]
    public async Task BitOverlay_Escape_NeverFallsThroughABlockingOverlayToTheOneHoldingTheFocus()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/overlay-escape");
        await WaitForStatus("Ready");

        await Page.Locator("#chk-upper-blocking").CheckAsync();
        await Page.Locator("#btn-open-both").ClickAsync();
        await WaitForStatus("Both open");
        await Expect(Page.Locator(".bit-ovl-opn")).ToHaveCountAsync(2);

        // The focus is inside the lower Overlay, which the upper one covers.
        var input = Page.Locator("#lower-input");
        await input.FocusAsync();
        await Expect(input).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        // Give a wrongly routed press the time to have closed something.
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#upper-state")).ToHaveTextAsync("open");
        await Expect(Page.Locator("#lower-state")).ToHaveTextAsync("open");
    }
}
