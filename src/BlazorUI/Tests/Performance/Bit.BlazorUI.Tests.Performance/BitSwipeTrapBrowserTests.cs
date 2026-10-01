using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for BitSwipeTrap, driven with real keyboard and mouse input.
///
/// The arrow keys, the Escape that cancels a swipe, the touch-action a disabled trap gives back, the recovery from a
/// gesture whose end never arrived and the throttle of OnMove all live in the script and the stylesheet, which bUnit
/// cannot run - so the behavior is pinned down here, in a real browser.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitSwipeTrapBrowserTests
public class BitSwipeTrapBrowserTests : PerformanceTestBase
{
    [TestMethod]
    public async Task BitSwipeTrap_KeyboardTrigger_TriggersAlongTheLockedAxisOnly()
    {
        await GoToThePage();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#trap")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Page.Locator("#trigger")).ToHaveTextAsync("Right:keyboard");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Page.Locator("#trigger")).ToHaveTextAsync("Left:keyboard");

        // The trap is locked horizontally, so the vertical keys are not its own.
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#triggers")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task BitSwipeTrap_KeyboardTrigger_LeavesTheKeysOfADescendantAlone()
    {
        await GoToThePage();

        await Page.Locator("#inner-input").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#triggers")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task BitSwipeTrap_Escape_CancelsAMouseSwipeInProgress()
    {
        await GoToThePage();

        var box = await Page.Locator("#trap").BoundingBoxAsync();
        Assert.IsNotNull(box);

        var x = box.X + 20;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 120, y, new() { Steps = 10 });

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#end")).ToHaveTextAsync("canceled");

        // The release that follows belongs to no gesture any more: nothing triggers, though it went past the trigger point.
        await Page.Mouse.UpAsync();
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#triggers")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#end")).ToHaveTextAsync("canceled");
    }

    [TestMethod]
    public async Task BitSwipeTrap_MouseSwipe_TriggersPastTheTriggerPoint()
    {
        await GoToThePage();

        var box = await Page.Locator("#trap").BoundingBoxAsync();
        Assert.IsNotNull(box);

        var x = box.X + 20;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 120, y, new() { Steps = 10 });
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#trigger")).ToHaveTextAsync("Right:mouse");
        await Expect(Page.Locator("#end")).ToHaveTextAsync("released");
    }

    [TestMethod]
    public async Task BitSwipeTrap_Disabled_GivesBothAxesBackToTheBrowser()
    {
        await GoToThePage();

        var touchAction = await Page.EvaluateAsync<string>("() => getComputedStyle(document.getElementById('disabled-trap')).touchAction");
        Assert.AreEqual("auto", touchAction);

        // And a disabled trap is no tab stop.
        Assert.IsNull(await Page.Locator("#disabled-trap").GetAttributeAsync("tabindex"));
    }

    [TestMethod]
    public async Task BitSwipeTrap_PressOfTheTrackedPointer_CallsOffAGestureWhoseEndNeverArrived()
    {
        await GoToThePage();

        // A release that never reaches the trap (its target was removed from the page) is simulated by two presses of
        // the same pointer: the second proves the first gesture over, so it is ended as canceled and the trap takes the
        // new one instead of ignoring every press from then on.
        // Synthetic events go out at once, so they wait for the script to have set the trap up first.
        await Page.WaitForFunctionAsync("() => BitBlazorUI.SwipeTrap._swipeTraps.some(t => t.element.id === 'trap')");
        await DispatchPointer("pointerdown");
        await DispatchPointer("pointerdown");

        await Expect(Page.Locator("#end")).ToHaveTextAsync("canceled");
        await Expect(Page.Locator("#starts")).ToHaveTextAsync("2");

        await DispatchPointer("pointerup");
        await Expect(Page.Locator("#end")).ToHaveTextAsync("released");
    }

    [TestMethod]
    public async Task BitSwipeTrap_LostPointerCapture_CancelsTheSwipe()
    {
        await GoToThePage();

        await Page.EvaluateAsync("() => document.addEventListener('pointerdown', e => window.__swipePointerId = e.pointerId, true)");

        var box = await Page.Locator("#trap").BoundingBoxAsync();
        Assert.IsNotNull(box);

        var x = box.X + 20;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 120, y, new() { Steps = 10 });

        // The swipe is trapped and so captured; taking the capture away leaves no release for the trap to see.
        // The loss is processed, and lostpointercapture fired, before the next pointer event.
        await Page.EvaluateAsync("() => document.getElementById('trap').releasePointerCapture(window.__swipePointerId)");
        await Page.Mouse.MoveAsync(x + 121, y);
        await Expect(Page.Locator("#end")).ToHaveTextAsync("canceled");

        await Page.Mouse.UpAsync();
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#triggers")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task BitSwipeTrap_Throttle_StillDeliversTheLatestMoveOfAWindow()
    {
        await GoToThePage();

        var box = await Page.Locator("#throttled-trap").BoundingBoxAsync();
        Assert.IsNotNull(box);

        var x = box.X + 20;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 100, y, new() { Steps = 5 });

        // The pointer rests, still pressed: the move held for the end of the window reports where it rests.
        await Expect(Page.Locator("#throttled-move")).ToHaveTextAsync("100");

        await Page.Mouse.UpAsync();
    }

    private async Task DispatchPointer(string type)
    {
        await Page.EvaluateAsync(@"type => {
            const trap = document.getElementById('trap');
            const box = trap.getBoundingClientRect();
            trap.dispatchEvent(new PointerEvent(type, {
                pointerId: 7, pointerType: 'pen', button: 0, isPrimary: true, bubbles: true, cancelable: true,
                clientX: box.x + 20, clientY: box.y + 20
            }));
        }", type);
    }

    private async Task GoToThePage()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/swipetrap-keyboard");
        await WaitForStatus("Ready");
    }
}
