using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for an Escape pressed in a surface opened from inside another one, driven with real keyboard
/// and mouse input.
///
/// The first press closes the inner surface alone and the second the outer one. No surface stops the key, so it bubbles
/// on from the inner one's root through the outer one's, and the outer one leaves it alone only because the inner one
/// recorded itself as its owner on the way (Utils._escapeSurfaceOwners). Those are native listeners bUnit cannot run, so
/// the behavior is pinned down here, in every engine (BROWSER=chromium, firefox, webkit).
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitNestedSurfaceEscapeBrowserTests
public class BitNestedSurfaceEscapeBrowserTests : PerformanceTestBase
{
    [TestMethod]
    public async Task Dialog_InADropMenuPopup_ClosesFirstAndThePopupClosesOnTheNextPress()
    {
        await OpenPage("menu-dialog");

        await OpenOuter(Page.Locator("#outer-trigger button.bit-drm-btn"));
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    [TestMethod]
    public async Task DropMenuPopup_InADialog_ClosesFirstAndTheDialogClosesOnTheNextPress()
    {
        await OpenPage("dialog-menu");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#inner-trigger button.bit-drm-btn"));

        await AssertOneSurfaceClosesPerPress();
    }

    [TestMethod]
    public async Task Dialog_InADialog_ClosesFirstAndTheOuterDialogClosesOnTheNextPress()
    {
        await OpenPage("dialog-dialog");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    [TestMethod]
    public async Task Dialog_InAnOverlay_ClosesFirstAndTheOverlayClosesOnTheNextPress()
    {
        await OpenPage("overlay-dialog");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    [TestMethod]
    public async Task Panel_InAnOverlay_ClosesFirstAndTheOverlayClosesOnTheNextPress()
    {
        await OpenPage("overlay-panel");

        await OpenOuter(Page.Locator("#btn-open-outer"));
        await OpenInner(Page.Locator("#btn-open-inner"));

        await AssertOneSurfaceClosesPerPress();
    }

    private async Task OpenPage(string @case)
    {
        await Page.GotoAsync($"{BaseUrl}/regression/nested-surface-escape?case={@case}");
        await WaitForStatus("Ready");
    }

    private async Task OpenOuter(ILocator opener)
    {
        await opener.ClickAsync();
        await Expect(Page.Locator("#outer-state")).ToHaveTextAsync("open");
        await Settle();
    }

    // The press is made from a field of the inner surface, which is where a user typing in it would make it.
    private async Task OpenInner(ILocator opener)
    {
        await opener.ClickAsync();
        await Expect(Page.Locator("#inner-state")).ToHaveTextAsync("open");
        await Settle();

        var input = Page.Locator("#inner-input");
        await input.FocusAsync();
        await Expect(input).ToBeFocusedAsync();
    }

    private async Task AssertOneSurfaceClosesPerPress()
    {
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#inner-state")).ToHaveTextAsync("closed");

        // Give a wrongly routed press the time to have closed the outer surface too.
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#outer-state")).ToHaveTextAsync("open");

        // No focus is moved by hand here: the second press is made from wherever closing the inner surface left it.
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#outer-state")).ToHaveTextAsync("closed");
    }

    // The components start listening in calls to the browser of their own once they have rendered, which nothing on
    // the page reports; a key pressed before they land is a key nothing was listening for yet.
    private Task Settle() => Page.WaitForTimeoutAsync(500);
}
