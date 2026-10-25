namespace Bit.Bmotion.Tests.E2E;

/// <summary>Hover, tap (pointer and keyboard), focus and pan: overlays, their revert, and their callbacks.</summary>
[TestClass]
public sealed class GesturesTests : BmotionPageTest
{
    protected override string Route => "gestures";

    [TestMethod]
    public async Task Hover_AppliesTheOverlay_AndRevertsWhenThePointerLeaves()
    {
        await ExpectEventAsync("armed:hover");

        await HoverAsync("#hover");
        await ExpectEventAsync("hover-start");
        await WaitForStateAsync("#hover", "s => Math.abs(s.scaleX - 1.5) < 0.01");

        await Page.Mouse.MoveAsync(1, 1);
        await ExpectEventAsync("hover-end");
        await WaitForStateAsync("#hover", "s => Math.abs(s.scaleX - 1) < 0.01");
    }

    [TestMethod]
    public async Task Tap_PressesWhileHeld_AndReportsTheTapOnRelease()
    {
        await ExpectEventAsync("armed:tap");
        var (x, y) = await CentreAsync("#tap");

        await Page.Mouse.MoveAsync((float)x, (float)y);
        await Page.Mouse.DownAsync();
        await ExpectEventAsync("tap-start");
        await WaitForStateAsync("#tap", "s => Math.abs(s.scaleX - 0.5) < 0.01");

        await Page.Mouse.UpAsync();
        await ExpectEventAsync("tap");
        await WaitForStateAsync("#tap", "s => Math.abs(s.scaleX - 1) < 0.01");
        await ExpectEventAsync("tap-cancel", 0);
    }

    [TestMethod]
    public async Task Tap_ReleasedOutsideTheElement_IsCancelled()
    {
        await ExpectEventAsync("armed:tap");
        var (x, y) = await CentreAsync("#tap");

        await Page.Mouse.MoveAsync((float)x, (float)y);
        await Page.Mouse.DownAsync();
        await ExpectEventAsync("tap-start");
        await Page.Mouse.MoveAsync((float)x + 400, (float)y + 300);
        await Page.Mouse.UpAsync();

        await ExpectEventAsync("tap-cancel");
        await ExpectEventAsync("tap", 0);
        await WaitForStateAsync("#tap", "s => Math.abs(s.scaleX - 1) < 0.01");
    }

    [TestMethod]
    public async Task Tap_IsKeyboardAccessible_WithEnterAndSpace()
    {
        await ExpectEventAsync("armed:tap");
        await Page.Locator("#tap").FocusAsync();

        await Page.Keyboard.PressAsync("Enter");
        await ExpectEventAsync("tap");

        await Page.Keyboard.PressAsync(" ");
        await ExpectEventAsync("tap", 2);
        await ExpectEventAsync("tap-start", 2);

        // Space on a non-button element would scroll the page; a tappable element suppresses that.
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => window.scrollY"));
    }

    [TestMethod]
    public async Task Tap_HeldOnTheKeyboardThenBlurred_IsCancelled()
    {
        await ExpectEventAsync("armed:tap");
        await Page.Locator("#tap").FocusAsync();

        await Page.Keyboard.DownAsync("Enter");
        await ExpectEventAsync("tap-start");
        await WaitForStateAsync("#tap", "s => Math.abs(s.scaleX - 0.5) < 0.01");

        await Page.Locator("#focus").FocusAsync();
        await ExpectEventAsync("tap-cancel");
        await Page.Keyboard.UpAsync("Enter");
        await ExpectEventAsync("tap", 0);
    }

    [TestMethod]
    public async Task Focus_AppliesTheOverlay_AndRevertsOnBlur()
    {
        await ExpectEventAsync("armed:focus");

        await Page.Locator("#focus").FocusAsync();
        await ExpectEventAsync("focus-start");
        await WaitForStateAsync("#focus", "s => Math.abs(s.scaleX - 1.2) < 0.01 && s.backgroundColor === 'rgb(0, 128, 0)'");

        await Page.Locator("#focus").BlurAsync();
        await ExpectEventAsync("focus-end");
        await WaitForStateAsync("#focus", "s => Math.abs(s.scaleX - 1) < 0.01");
    }

    [TestMethod]
    public async Task Pan_ReportsStartMovesAndEnd_WithTheOffset_WithoutMovingTheElement()
    {
        await ExpectEventAsync("armed:pan");

        await DragByAsync("#pan", 60, 0);

        await ExpectEventAsync("pan-start");
        await ExpectEventAsync("pan-end");
        await ExpectReadoutAsync("pan-offset", "60,0");
        Assert.IsTrue(await Event("pan").CountAsync() > 1, "Pan should report every move past the threshold.");
        var state = await ProbeAsync("#pan");
        Assert.AreEqual(0, state.X, 0.5, "Pan must not move the element.");
    }

    [TestMethod]
    public async Task GesturePropagationFalse_StopsTheTapReachingTheAncestor()
    {
        await ExpectEventAsync("armed:inner-isolated");
        await ExpectEventAsync("armed:outer");

        await Page.Locator("#inner-isolated").ClickAsync();
        await ExpectEventAsync("inner-isolated-tap-start");

        await Page.Locator("#inner-bubbling").ClickAsync();
        await ExpectEventAsync("inner-bubbling-tap-start");

        // Only the bubbling press reached the card.
        await ExpectEventAsync("outer-tap-start", 1);
    }

    [TestMethod]
    public async Task HoverEnd_ReturnsToTheRestingState_WithoutReplayingItsKeyframes()
    {
        await ExpectEventAsync("revert-keyframes:complete");

        await HoverAsync("#revert-keyframes");
        await WaitForStateAsync("#revert-keyframes", "s => Math.abs(s.scaleX - 1.3) < 0.01");
        await Page.Mouse.MoveAsync(1, 1);
        var samples = await SampleAsync("#revert-keyframes", 400);

        await WaitForStateAsync("#revert-keyframes", "s => Math.abs(s.scaleX - 1) < 0.01");
        Assert.IsTrue(samples.All(s => s.Opacity > 0.99), $"The resting opacity keyframes replayed on hover end: min opacity {samples.Min(s => s.Opacity)}");
    }

    [TestMethod]
    public async Task AGestureEnabledAfterTheFirstRender_TakesEffect()
    {
        await ExpectEventAsync("armed:late");
        await ClickAsync("enable-hover");

        // The listeners are re-attached after the render the click caused, which the page can't announce:
        // hover until the overlay shows, within a bound.
        for (var attempt = 0; ; attempt++)
        {
            await HoverAsync("#late");
            try
            {
                await WaitForStateAsync("#late", "s => Math.abs(s.scaleX - 1.4) < 0.01", timeout: 1_000);
                break;
            }
            catch (AssertFailedException) when (attempt < 5) { }
        }
    }
}
