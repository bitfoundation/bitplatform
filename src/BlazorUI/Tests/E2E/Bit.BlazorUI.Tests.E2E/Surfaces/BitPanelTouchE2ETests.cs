namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// End-to-end tests of BitPanel on a touch phone (/e2e/panel at a 390x844 viewport with touch): the swipe gesture the
/// panel registers on touch devices dismisses it towards its own edge past the SwipeTrigger and snaps it back short of it,
/// reports OnSwipeStart/Move/End, honours NoSwipe and leaves a drag that starts on a field alone; a tap on the overlay
/// dismisses it; the 85% cap leaves a strip of the page beside a panel larger than the phone; and a touch drag over the
/// overlay of a panel that leaves the page scrolling is handed to the named scroller.
/// </summary>
[TestClass]
public class BitPanelTouchE2ETests : E2ETestBase
{
    private const string PagePath = "/e2e/panel";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
    };

    private ILocator Container(string id) => Page.Locator($"#{id} > .bit-pnl-cnt");

    private async Task OpenPanelAsync(string buttonId, string panelId)
    {
        await Page.Locator($"#{buttonId}").TapAsync();
        await Expect(Container(panelId)).ToHaveClassAsync(HasClass("bit-pnl-opn"));
        await Page.Locator($"#{panelId}").EvaluateAsync("el => Promise.all(el.getAnimations({ subtree: true }).map(a => a.finished.catch(() => null)))");
    }

    private async Task OpenSwipeAsync(string placement = "End", bool noSwipe = false)
    {
        await OpenAsync(PagePath);

        var touchable = await Page.EvaluateAsync<bool>("() => { try { new Touch({ identifier: 1, target: document.body }); return 'ontouchstart' in window; } catch { return false; } }");
        if (touchable is false) Assert.Inconclusive("This engine cannot construct the Touch events the swipe is driven with.");

        await Page.Locator("#swipe-placement-select").SelectOptionAsync(placement);
        if (noSwipe) await Page.Locator("#noswipe-check").CheckAsync();
        await OpenPanelAsync("btn-swipe-open", "swipe-panel");
    }

    // A one-finger drag from the middle of the element, in ten steps, the way a finger moves: touchstart, touchmove and
    // touchend, each dispatched on the element the finger is on so it bubbles up to the panel.
    private Task TouchDragAsync(string selector, double dx, double dy)
    {
        return Page.EvaluateAsync(@"async ([selector, dx, dy]) => {
            const target = document.querySelector(selector);
            const rect = target.getBoundingClientRect();
            const x0 = rect.left + rect.width / 2;
            const y0 = rect.top + rect.height / 2;
            const touch = (x, y) => new Touch({ identifier: 1, target, clientX: x, clientY: y, screenX: x, screenY: y, pageX: x, pageY: y });
            const fire = (type, current, list) => target.dispatchEvent(new TouchEvent(type,
                { touches: list, targetTouches: list, changedTouches: [current], bubbles: true, cancelable: true, composed: true }));
            const frame = () => new Promise(resolve => setTimeout(resolve, 16));

            let current = touch(x0, y0);
            fire('touchstart', current, [current]);
            await frame();
            for (let i = 1; i <= 10; i++) {
                current = touch(x0 + dx * i / 10, y0 + dy * i / 10);
                fire('touchmove', current, [current]);
                await frame();
            }
            fire('touchend', current, []);
        }", new object[] { selector, dx, dy });
    }

    [TestMethod]
    public async Task Swipe_FingerDraggedTowardsTheEdgePastTheTrigger_DismissesThePanel()
    {
        await OpenSwipeAsync();

        await TouchDragAsync("#swipe-text", 200, 0);

        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#swipe-log")).ToHaveTextAsync("OnSwipeStart,OnDismissing:Swipe,OnSwipeEnd");
        await Expect(Page.Locator("#swipe-end")).ToHaveTextAsync("200");
        Assert.IsTrue(int.Parse((await Page.Locator("#swipe-moves").TextContentAsync())!) > 0);
        await Expect(Container("swipe-panel")).Not.ToHaveClassAsync(HasClass("bit-pnl-opn"));
    }

    [TestMethod]
    public async Task Swipe_FingerFollowedByThePanel_WhileTheDragLasts()
    {
        await OpenSwipeAsync();

        // Half a drag: the finger is still down, and the panel has moved with it.
        await Page.EvaluateAsync(@"() => {
            const target = document.querySelector('#swipe-text');
            const rect = target.getBoundingClientRect();
            const x0 = rect.left + rect.width / 2, y0 = rect.top + rect.height / 2;
            const touch = (x, y) => new Touch({ identifier: 1, target, clientX: x, clientY: y, screenX: x, screenY: y });
            const fire = (type, t) => target.dispatchEvent(new TouchEvent(type, { touches: [t], targetTouches: [t], changedTouches: [t], bubbles: true, cancelable: true }));
            fire('touchstart', touch(x0, y0));
            fire('touchmove', touch(x0 + 10, y0));
            fire('touchmove', touch(x0 + 60, y0));
        }");

        await Expect(Container("swipe-panel")).ToHaveCSSAsync("transform", "matrix(1, 0, 0, 1, 60, 0)");
    }

    [TestMethod]
    public async Task Swipe_FingerDraggedShortOfTheTrigger_SnapsBack()
    {
        await OpenSwipeAsync();

        await TouchDragAsync("#swipe-text", 40, 0);

        await Expect(Page.Locator("#swipe-log")).ToHaveTextAsync("OnSwipeStart,OnSwipeEnd");
        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("True");
        await Expect(Container("swipe-panel")).ToHaveCSSAsync("transform", "none");
    }

    [TestMethod]
    [DataRow("Start", -200, 0)]
    [DataRow("Bottom", 0, 200)]
    [DataRow("Top", 0, -200)]
    public async Task Swipe_EachPlacement_IsDismissedTowardsItsOwnEdge(string placement, double dx, double dy)
    {
        await OpenSwipeAsync(placement);

        await TouchDragAsync("#swipe-text", dx, dy);

        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#swipe-log")).ToContainTextAsync("OnDismissing:Swipe");
    }

    [TestMethod]
    public async Task NoSwipe_LeavesTheFingerAlone()
    {
        await OpenSwipeAsync(noSwipe: true);

        await TouchDragAsync("#swipe-text", 250, 0);
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#swipe-log")).ToHaveTextAsync("");
        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    [DataRow("#swipe-input")]
    [DataRow("#swipe-noswipe")]
    public async Task Swipe_StartedOnAFieldOrANoSwipeRegion_IsLeftToIt(string selector)
    {
        await OpenSwipeAsync();

        await TouchDragAsync(selector, 250, 0);
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("#swipe-log")).Not.ToContainTextAsync("OnDismissing");
        await Expect(Page.Locator("#swipe-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task OverlayTap_DismissesThePanel()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-basic-open", "basic-panel");

        await Page.Touchscreen.TapAsync(10, 400);

        await Expect(Page.Locator("#basic-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#basic-log")).ToContainTextAsync("OnOverlayClick,OnDismissing:Overlay:m");
    }

    [TestMethod]
    public async Task Size_LargerThanThePhone_IsCappedSoAStripOfThePageShows()
    {
        await OpenAsync(PagePath);
        await OpenPanelAsync("btn-swipe-open", "swipe-panel");

        // Size 300 fits; the default cap is 85% of the screen.
        var width = await Container("swipe-panel").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.AreEqual(300, width, 1);

        await Page.SetViewportSizeAsync(320, 700);
        var capped = await Container("swipe-panel").EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        var viewport = await Page.EvaluateAsync<double>("() => document.documentElement.clientWidth");
        Assert.AreEqual(viewport * 0.85, capped, 1);
    }

    [TestMethod]
    public async Task NoScrollLockWithAScroller_FingerDragOverTheOverlayScrollsThatScroller()
    {
        await OpenAsync(PagePath);

        var touchable = await Page.EvaluateAsync<bool>("() => { try { new Touch({ identifier: 1, target: document.body }); return 'ontouchstart' in window; } catch { return false; } }");
        if (touchable is false) Assert.Inconclusive("This engine cannot construct the Touch events the gesture is driven with.");

        await OpenPanelAsync("btn-forward-open", "forward-panel");

        await TouchDragAsync("#forward-panel > .bit-pnl-ovl", 0, -150);

        await Page.WaitForFunctionAsync("() => document.getElementById('pnl-forward-box').scrollTop > 0");
    }
}
