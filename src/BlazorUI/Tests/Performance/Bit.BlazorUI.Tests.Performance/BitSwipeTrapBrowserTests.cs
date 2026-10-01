using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the keyboard of BitSwipeTrap, driven with real keyboard and mouse input.
///
/// The arrow keys, the Escape that cancels a swipe and the touch-action a disabled trap gives back all live in the
/// script and the stylesheet, which bUnit cannot run - so the behavior is pinned down here, in a real browser.
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

    private async Task GoToThePage()
    {
        await Page.GotoAsync($"{BaseUrl}/regression/swipetrap-keyboard");
        await WaitForStatus("Ready");
    }
}
