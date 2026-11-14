using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// Drives BitLongPress at /e2e/long-press in a real browser with the mouse and the keyboard: a held button raising
/// OnLongPress at its point, a short click left to the content, the click of a long press swallowed once, a press called
/// off by a move past MoveTolerance and kept within it, Delay, TouchOnly and the right button leaving the mouse out, the
/// pressed class, the context-menu keys of KeyboardTrigger (and a field typed in left alone), Disabled, the focus ring
/// variable and axe. Touch is in BitLongPressTouchE2ETests.
/// </summary>
[TestClass]
public class BitLongPressE2ETests : E2ETestBase
{
    private const string Route = "/e2e/long-press";

    private async Task<(float X, float Y)> CenterAsync(string selector)
    {
        var element = Page.Locator(selector);
        await element.ScrollIntoViewIfNeededAsync();
        var box = (await element.BoundingBoxAsync())!;

        return (MathF.Round(box.X + box.Width / 2), MathF.Round(box.Y + box.Height / 2));
    }

    private async Task HoldAsync(float x, float y, int milliseconds, float driftX = 0, MouseButton button = MouseButton.Left)
    {
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync(new() { Button = button });
        if (driftX != 0) await Page.Mouse.MoveAsync(x + driftX, y, new() { Steps = 5 });
        await Page.WaitForTimeoutAsync(milliseconds);
        await Page.Mouse.UpAsync(new() { Button = button });
    }

    [TestMethod]
    public async Task AMouseHeldDown_RaisesOnLongPressAtItsPoint()
    {
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#lpr-basic");
        await HoldAsync(x, y, 800);

        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#basic-last")).ToHaveTextAsync($"mouse|{x},{y}");
    }

    [TestMethod]
    public async Task WhileAPressIsHeld_TheRootCarriesThePressedClass()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#lpr-basic");
        var (x, y) = await CenterAsync("#lpr-basic");

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Expect(root).ToHaveClassAsync(new Regex(@"\bbit-lpr-prs\b"));
        await Expect(root).ToHaveCSSAsync("user-select", "none");

        // Once the press is long, the class is gone even while the button is still down.
        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("1");
        await Expect(root).Not.ToHaveClassAsync(new Regex(@"\bbit-lpr-prs\b"));
        await Page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task AShortClick_RaisesNothing_AndIsTheContentsOwn()
    {
        await OpenAsync(Route);

        await Page.Locator("#basic-child").ClickAsync();

        await Expect(Page.Locator("#basic-child-clicks")).ToHaveTextAsync("1");
        await Page.WaitForTimeoutAsync(700);
        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#lpr-basic")).Not.ToHaveClassAsync(new Regex(@"\bbit-lpr-prs\b"));
    }

    [TestMethod]
    public async Task ALongPressOnAChild_SwallowsTheClickOfItsRelease_AndOnlyThatOne()
    {
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#basic-child");
        await HoldAsync(x, y, 800);

        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("1");
        await Page.WaitForTimeoutAsync(200);
        await Expect(Page.Locator("#basic-child-clicks")).ToHaveTextAsync("0");

        // The next click is the child's again.
        await Page.Locator("#basic-child").ClickAsync();
        await Expect(Page.Locator("#basic-child-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task APressThatMovesPastTheTolerance_IsCalledOff()
    {
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#lpr-basic");
        await HoldAsync(x, y, 800, driftX: 40);

        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#lpr-basic")).Not.ToHaveClassAsync(new Regex(@"\bbit-lpr-prs\b"));

        // A drift within the default 10px is still a press held in place.
        await HoldAsync(x, y, 800, driftX: 6);
        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task APressThePageScrollsUnder_IsCalledOff()
    {
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#lpr-basic");

        // The pointer does not move: the content moves away from under it.
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.EvaluateAsync("() => window.scrollBy(0, 40)");
        await Page.WaitForTimeoutAsync(800);
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#lpr-basic")).Not.ToHaveClassAsync(new Regex(@"\bbit-lpr-prs\b"));
    }

    [TestMethod]
    public async Task APointerTheBrowserCancels_CallsThePressOff()
    {
        await OpenAsync(Route);

        await Page.Locator("#lpr-basic").EvaluateAsync("el => el.addEventListener('pointerdown', e => window.__lprPointerId = e.pointerId)");
        var (x, y) = await CenterAsync("#lpr-basic");

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Locator("#lpr-basic .e2e-lpr-box").EvaluateAsync("el => el.dispatchEvent(new PointerEvent('pointercancel', { bubbles: true, pointerId: window.__lprPointerId }))");
        await Page.WaitForTimeoutAsync(800);
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task DelayAndMoveTolerance_AreHowLongAndHowStillThePressIsHeld()
    {
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#lpr-delay");

        await HoldAsync(x, y, 700);
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#delay-count")).ToHaveTextAsync("0", new() { Timeout = 100 });

        await HoldAsync(x, y, 1500, driftX: 20);
        await Expect(Page.Locator("#delay-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task TouchOnly_AndTheRightButton_LeaveTheMouseOut()
    {
        await OpenAsync(Route);

        var (x, y) = await CenterAsync("#lpr-touchonly");
        await HoldAsync(x, y, 800);
        await Expect(Page.Locator("#touchonly-count")).ToHaveTextAsync("0");

        // The right button is the mouse's secondary action already: a context menu, not a long press.
        var (bx, by) = await CenterAsync("#lpr-basic");
        await HoldAsync(bx, by, 800, button: MouseButton.Right);
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#basic-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task TheContextMenuKeys_RaiseOnLongPressAtTheMiddleOfWhatHasTheFocus()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#lpr-key");
        await Expect(root).ToHaveAttributeAsync("tabindex", "0");
        await Expect(root).ToHaveAttributeAsync("role", "group");
        await Expect(root).ToHaveAttributeAsync("aria-keyshortcuts", "Shift+F10 ContextMenu");

        await root.FocusAsync();
        var (x, y) = await CenterAsync("#lpr-key");

        await Page.Keyboard.PressAsync("Shift+F10");
        await Expect(Page.Locator("#key-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#key-last")).ToHaveTextAsync(new Regex($@"^keyboard\|({x - 1}|{x}|{x + 1}),({y - 1}|{y}|{y + 1})$"));

        await Page.Keyboard.PressAsync("ContextMenu");
        await Expect(Page.Locator("#key-count")).ToHaveTextAsync("2");

        // A field typed in keeps its own context menu, the editing one.
        await Page.Locator("#key-input").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+F10");
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#key-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task ADisabledLongPress_RaisesNothing_AndIsNoTabStop()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#lpr-disabled");
        await Expect(root).ToHaveClassAsync(new Regex(@"\bbit-dis\b"));
        await Expect(root).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));

        var (x, y) = await CenterAsync("#lpr-disabled");
        await HoldAsync(x, y, 800);
        await Expect(root).Not.ToHaveClassAsync(new Regex(@"\bbit-lpr-prs\b"));
        await Expect(Page.Locator("#disabled-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task TheFocusRing_TakesTheFocusColorVariable()
    {
        await OpenAsync(Route);

        await Page.Locator("#key-input").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");

        var root = Page.Locator("#lpr-key");
        await Expect(root).ToBeFocusedAsync();
        StringAssert.Contains(await root.EvaluateAsync<string>("e => getComputedStyle(e).boxShadow + ' ' + getComputedStyle(e).outlineColor"), "255, 0, 0");
    }

    [TestMethod]
    public async Task TheLongPresses_PassAxe()
    {
        await OpenAsync(Route);

        await AssertNoAxeViolationsAsync("#lpr-basic");
        await AssertNoAxeViolationsAsync("#lpr-key");
        await AssertNoAxeViolationsAsync("#lpr-disabled");
    }
}
