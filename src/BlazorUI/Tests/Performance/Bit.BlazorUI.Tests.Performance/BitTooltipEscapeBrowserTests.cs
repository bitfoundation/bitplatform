using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the Escape that dismisses a tooltip the pointer rests on (Utils.dismissTooltipsOnEscape),
/// driven with real keyboard and mouse input.
///
/// The press is the tooltip's alone - the hover-opened DropMenu or the Overlay it sits in stays, and closes on the next
/// press - whichever of them started listening first; a swipe in progress under the tooltip takes the press instead; and
/// a listener of the app's own on the window still hears it. The order of the listeners is the script's, which bUnit
/// cannot run - so the behavior is pinned down here, in every engine (BROWSER=chromium, firefox, webkit).
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitTooltipEscapeBrowserTests
public class BitTooltipEscapeBrowserTests : PerformanceTestBase
{
    private ILocator ShownTooltip => Page.Locator(".bit-ttp-wrp.bit-ttp-vis");

    [TestMethod]
    public async Task Tooltip_InAHoverOpenedDropMenu_IsDismissedFirstAndTheMenuClosesOnTheNextPress()
    {
        await OpenPage("menu");

        await Page.Locator("#menu").HoverAsync();
        await Expect(Page.Locator("#menu-state")).ToHaveTextAsync("open");

        await Page.Locator("#menu-tip-anchor").HoverAsync();
        await Expect(ShownTooltip).ToHaveCountAsync(1);
        await Settle();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(ShownTooltip).ToHaveCountAsync(0);

        // Give a wrongly routed press the time to have closed the menu.
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#menu-state")).ToHaveTextAsync("open");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#menu-state")).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task Tooltip_InAnOverlay_IsDismissedFirstAndTheOverlayClosesOnTheNextPress()
    {
        await OpenPage("overlay");

        await Page.Locator("#btn-open-overlay").ClickAsync();
        await Expect(Page.Locator("#overlay-state")).ToHaveTextAsync("open");
        await Settle();

        await Page.Locator("#overlay-tip-anchor").HoverAsync();
        await Expect(ShownTooltip).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("Escape");

        await Expect(ShownTooltip).ToHaveCountAsync(0);

        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#overlay-state")).ToHaveTextAsync("open");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#overlay-state")).ToHaveTextAsync("closed");
    }

    [TestMethod]
    public async Task Tooltip_OverASwipeInProgress_LeavesTheEscapeToTheSwipe()
    {
        await OpenPage("swipe");

        var box = await Page.Locator("#trap").BoundingBoxAsync();
        Assert.IsNotNull(box);

        var x = box.X + 20;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Expect(ShownTooltip).ToHaveCountAsync(1);

        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 120, y, new() { Steps = 10 });

        // The tooltip is shown, or not, as the browser decides for a pressed pointer; whichever it is, the press is
        // the swipe's and the tooltip is left as it was.
        var shownDuringSwipe = await ShownTooltip.CountAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#end")).ToHaveTextAsync("canceled");
        await Page.WaitForTimeoutAsync(300);
        await Expect(ShownTooltip).ToHaveCountAsync(shownDuringSwipe);

        await Page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task Tooltip_LeavesTheEscapeItTakesToTheAppsOwnListenerOnTheWindow()
    {
        await OpenPage();

        // Added after the library has started, in the capture phase on the window: behind the library's own listeners.
        await Page.EvaluateAsync(@"() => {
            window.__escapes = [];
            window.addEventListener('keydown', e => { if (e.key === 'Escape') window.__escapes.push(e.defaultPrevented); }, true);
        }");

        await Page.Locator("#page-tip-anchor").HoverAsync();
        await Expect(ShownTooltip).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("Escape");

        await Expect(ShownTooltip).ToHaveCountAsync(0);

        // The app hears the press, as one that was taken.
        CollectionAssert.AreEqual(new[] { true }, await Page.EvaluateAsync<bool[]>("() => window.__escapes"));
    }

    private async Task OpenPage(string? @case = null)
    {
        await Page.GotoAsync($"{BaseUrl}/regression/tooltip-escape{(@case is null ? "" : $"?case={@case}")}");
        await WaitForStatus("Ready");
        await Settle();
    }

    // The components start listening in calls to the browser of their own once they have rendered, which nothing on
    // the page reports; a key pressed before they land is a key nothing was listening for yet.
    private Task Settle() => Page.WaitForTimeoutAsync(500);
}
