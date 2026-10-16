namespace Bit.BlazorUI.Tests.E2E.Buttons;

/// <summary>
/// The end-to-end tests of the floating BitButton against /e2e/button/float, a page of its own since a floating
/// button sits over whatever is under it: Float pinned to the viewport at every FloatPosition, FloatOffset as a
/// length and as a bare number, FloatAbsolute pinned inside its container, the elevation, and Draggable - the pointer
/// drag and the click it swallows, the arrow keys and their coarser Shift step, the clamping to the box the button is
/// positioned in, the reset when the anchor changes, turning dragging and floating off at runtime, and unmounting.
/// </summary>
[TestClass]
public class BitButtonFloatE2ETests : E2ETestBase
{
    private const double Tolerance = 1.5;

    private ILocator FloatBtn => Page.Locator("#float-btn");

    private Task<double> ViewportWidth() => Page.EvaluateAsync<double>("() => document.documentElement.clientWidth");

    private Task<double> ViewportHeight() => Page.EvaluateAsync<double>("() => document.documentElement.clientHeight");

    // a round trip through the circuit, after which every event sent before it has been handled
    private async Task PingAsync()
    {
        var pings = int.Parse(await Page.Locator("#pings").TextContentAsync() ?? "0");
        await Page.Locator("#btn-ping").ClickAsync();
        await Expect(Page.Locator("#pings")).ToHaveTextAsync((pings + 1).ToString());
    }

    [TestMethod]
    [DataRow("TopLeft", "tlf")]
    [DataRow("TopCenter", "tcr")]
    [DataRow("TopRight", "trg")]
    [DataRow("TopStart", "tst")]
    [DataRow("TopEnd", "ten")]
    [DataRow("CenterLeft", "clf")]
    [DataRow("Center", "ctr")]
    [DataRow("CenterRight", "crg")]
    [DataRow("CenterStart", "cst")]
    [DataRow("CenterEnd", "cen")]
    [DataRow("BottomLeft", "blf")]
    [DataRow("BottomCenter", "bcr")]
    [DataRow("BottomRight", "brg")]
    [DataRow("BottomStart", "bst")]
    [DataRow("BottomEnd", "ben")]
    public async Task FloatPosition_EachValue_PinsTheButtonToThatAnchorOfTheViewport(string position, string suffix)
    {
        await OpenAsync("/e2e/button/float");

        await Page.Locator("#float-position").SelectOptionAsync(position);

        await Expect(FloatBtn).ToHaveClassAsync(HasClass($"bit-btn-{suffix}"));
        await Expect(FloatBtn).ToHaveClassAsync(HasClass("bit-btn-ffx"));
        await Expect(FloatBtn).ToHaveCSSAsync("position", "fixed");
        await NextFramesAsync();

        var box = await FloatBtn.BoxAsync();
        var width = await ViewportWidth();
        var height = await ViewportHeight();
        const double offset = 16;

        var vertical = position.StartsWith("Top") ? "top" : position.StartsWith("Bottom") ? "bottom" : "center";
        var horizontal = position.EndsWith("Left") || position.EndsWith("Start") ? "left"
                       : position.EndsWith("Right") || position.EndsWith("End") ? "right"
                       : "center";

        switch (vertical)
        {
            case "top": ButtonsE2EHelpers.AreClose(offset, box.Y, Tolerance, "top"); break;
            case "bottom": ButtonsE2EHelpers.AreClose(height - offset, box.Y + box.Height, Tolerance, "bottom"); break;
            default: ButtonsE2EHelpers.AreClose(height / 2, box.Y + box.Height / 2, Tolerance, "vertical center"); break;
        }

        switch (horizontal)
        {
            case "left": ButtonsE2EHelpers.AreClose(offset, box.X, Tolerance, "left"); break;
            case "right": ButtonsE2EHelpers.AreClose(width - offset, box.X + box.Width, Tolerance, "right"); break;
            default: ButtonsE2EHelpers.AreClose(width / 2, box.X + box.Width / 2, Tolerance, "horizontal center"); break;
        }
    }

    [TestMethod]
    public async Task FloatOffset_AsANumberOrALength_SetsTheDistanceFromTheAnchor()
    {
        await OpenAsync("/e2e/button/float");

        var width = await ViewportWidth();

        await Page.Locator("#float-offset").FillAsync("40");
        await Expect(FloatBtn).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("--bit-Button-float-offset:\\s*40px"));
        await NextFramesAsync();
        ButtonsE2EHelpers.AreClose(width - 40, (await FloatBtn.BoxAsync()).X + (await FloatBtn.BoxAsync()).Width, Tolerance, "a bare number is read as pixels");

        await Page.Locator("#float-offset").FillAsync("5rem");
        await Expect(FloatBtn).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("--bit-Button-float-offset:\\s*5rem"));
        await NextFramesAsync();
        var rem = await Page.ResolvePxAsync("5rem");
        ButtonsE2EHelpers.AreClose(width - rem, (await FloatBtn.BoxAsync()).X + (await FloatBtn.BoxAsync()).Width, Tolerance, "a CSS length is used as it is");

        await Page.Locator("#float-offset").FillAsync("");
        await Expect(FloatBtn).Not.ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("float-offset"));
        await NextFramesAsync();
        var spacing = await Page.ResolvePxAsync("calc(var(--bit-spa-scaling-factor) * 2)");
        ButtonsE2EHelpers.AreClose(width - spacing, (await FloatBtn.BoxAsync()).X + (await FloatBtn.BoxAsync()).Width, Tolerance, "no offset falls back to the default spacing");
    }

    [TestMethod]
    public async Task Float_IsRaisedOffTheContentWithTheFloatShadow()
    {
        await OpenAsync("/e2e/button/float");

        Assert.AreNotEqual("none", await FloatBtn.StyleAsync("box-shadow"));
        Assert.AreNotEqual("auto", await FloatBtn.StyleAsync("z-index"));

        await Page.Locator("#btn-toggle-float").ClickAsync();
        await Expect(FloatBtn).Not.ToHaveClassAsync(HasClass("bit-btn-ffx"));
        await Expect(FloatBtn).ToHaveCSSAsync("position", "static");
        await Expect(FloatBtn).ToHaveCSSAsync("box-shadow", "none");
    }

    [TestMethod]
    public async Task FloatAbsolute_InAContainer_PinsTheButtonInsideItAtItsPositionAndOffset()
    {
        await OpenAsync("/e2e/button/float");

        var button = Page.Locator("#float-abs-btn");
        var container = Page.Locator("#float-container");

        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-fab"));
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-brg"));
        await Expect(button).ToHaveCSSAsync("position", "absolute");

        // the container has a 1px border, and an absolutely positioned box is placed inside its padding box
        var box = await button.BoxAsync();
        var outer = await container.BoxAsync();
        ButtonsE2EHelpers.AreClose(outer.X + outer.Width - 1 - 10, box.X + box.Width, Tolerance, "right");
        ButtonsE2EHelpers.AreClose(outer.Y + outer.Height - 1 - 10, box.Y + box.Height, Tolerance, "bottom");

        await Page.Locator("#btn-abs-topleft").ClickAsync();
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-tlf"));
        await NextFramesAsync();
        box = await button.BoxAsync();
        ButtonsE2EHelpers.AreClose(outer.X + 1 + 10, box.X, Tolerance, "left");
        ButtonsE2EHelpers.AreClose(outer.Y + 1 + 10, box.Y, Tolerance, "top");

        await Page.Locator("#btn-abs-offset").ClickAsync();
        await Expect(button).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("--bit-Button-float-offset:\\s*30px"));
        await NextFramesAsync();
        box = await button.BoxAsync();
        ButtonsE2EHelpers.AreClose(outer.X + 1 + 30, box.X, Tolerance, "left after the new offset");

        await button.ClickAsync();
        await Expect(Page.Locator("#float-abs-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Draggable_WhenFloating_SaysSoWithTheCursorAndTheKeyShortcuts()
    {
        await OpenAsync("/e2e/button/float");

        await Expect(FloatBtn).ToHaveClassAsync(HasClass("bit-btn-drg"));
        await Expect(FloatBtn).ToHaveCSSAsync("cursor", "grab");
        await Expect(FloatBtn).ToHaveAttributeAsync("aria-keyshortcuts", "ArrowUp ArrowDown ArrowLeft ArrowRight");
        await Expect(FloatBtn).ToHaveAccessibleNameAsync("Add");

        await FloatBtn.ClickAsync();
        await Expect(Page.Locator("#float-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Draggable_DraggedByThePointer_MovesWithItAndSwallowsTheClickThatEndsTheDrag()
    {
        await OpenAsync("/e2e/button/float");

        var before = await FloatBtn.BoxAsync();
        var x = before.X + before.Width / 2;
        var y = before.Y + before.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x - 120, y - 90, new() { Steps = 12 });
        await Page.Mouse.UpAsync();

        var after = await FloatBtn.BoxAsync();
        ButtonsE2EHelpers.AreClose(before.X - 120, after.X, Tolerance, "dragged left");
        ButtonsE2EHelpers.AreClose(before.Y - 90, after.Y, Tolerance, "dragged up");
        await Expect(FloatBtn).ToHaveCSSAsync("cursor", "grab");

        await PingAsync();
        await Expect(Page.Locator("#float-clicks")).ToHaveTextAsync("0");

        await FloatBtn.ClickAsync();
        await Expect(Page.Locator("#float-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Draggable_WithTheArrowKeys_MovesByAStepAndShiftMovesByACoarserOne()
    {
        await OpenAsync("/e2e/button/float");

        await Page.Locator("#float-position").SelectOptionAsync("Center");
        await Expect(FloatBtn).ToHaveClassAsync(HasClass("bit-btn-ctr"));
        await NextFramesAsync();

        await FloatBtn.FocusAsync();
        var start = await FloatBtn.BoxAsync();

        await Page.Keyboard.PressAsync("ArrowLeft");
        ButtonsE2EHelpers.AreClose(start.X - 8, (await FloatBtn.BoxAsync()).X, Tolerance, "ArrowLeft");

        await Page.Keyboard.PressAsync("ArrowUp");
        ButtonsE2EHelpers.AreClose(start.Y - 8, (await FloatBtn.BoxAsync()).Y, Tolerance, "ArrowUp");

        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        ButtonsE2EHelpers.AreClose(start.X - 8 + 24, (await FloatBtn.BoxAsync()).X, Tolerance, "Shift+ArrowRight");

        await Page.Keyboard.PressAsync("Shift+ArrowDown");
        ButtonsE2EHelpers.AreClose(start.Y - 8 + 24, (await FloatBtn.BoxAsync()).Y, Tolerance, "Shift+ArrowDown");

        await Page.Keyboard.PressAsync("Control+ArrowDown");
        ButtonsE2EHelpers.AreClose(start.Y - 8 + 24, (await FloatBtn.BoxAsync()).Y, Tolerance, "a modified arrow does not move it");

        await Expect(FloatBtn).ToBeFocusedAsync();
        await PingAsync();
        await Expect(Page.Locator("#float-clicks")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Draggable_MovedPastAnEdge_IsKeptInsideTheViewport()
    {
        await OpenAsync("/e2e/button/float");

        var width = await ViewportWidth();
        var height = await ViewportHeight();

        await FloatBtn.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await Page.Keyboard.PressAsync("Shift+ArrowDown");

        var box = await FloatBtn.BoxAsync();
        ButtonsE2EHelpers.AreClose(width, box.X + box.Width, Tolerance, "clamped to the right edge");
        ButtonsE2EHelpers.AreClose(height, box.Y + box.Height, Tolerance, "clamped to the bottom edge");

        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        // the pointer stays inside the viewport (an engine dispatches nothing past its edge); its corner is far
        // enough for the button to be carried past the top-left edge it is clamped to
        await Page.Mouse.MoveAsync(1, 1, new() { Steps = 10 });
        await Page.Mouse.UpAsync();

        box = await FloatBtn.BoxAsync();
        ButtonsE2EHelpers.AreClose(0, box.X, Tolerance, "clamped to the left edge");
        ButtonsE2EHelpers.AreClose(0, box.Y, Tolerance, "clamped to the top edge");
    }

    [TestMethod]
    public async Task Draggable_AfterAMove_SettingTheAnchorAgainDropsTheMove()
    {
        await OpenAsync("/e2e/button/float");

        await FloatBtn.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowLeft");
        await Page.Keyboard.PressAsync("Shift+ArrowUp");
        await Expect(FloatBtn).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("left:"));

        await Page.Locator("#float-position").SelectOptionAsync("TopLeft");

        await Expect(FloatBtn).ToHaveClassAsync(HasClass("bit-btn-tlf"));
        await Expect(FloatBtn).Not.ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("left:"));
        await NextFramesAsync();
        var box = await FloatBtn.BoxAsync();
        ButtonsE2EHelpers.AreClose(16, box.X, Tolerance, "back at the new anchor's left");
        ButtonsE2EHelpers.AreClose(16, box.Y, Tolerance, "back at the new anchor's top");

        await FloatBtn.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(FloatBtn).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("left:"));
        await Page.Locator("#float-offset").FillAsync("30");
        await Expect(FloatBtn).Not.ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("left:"));
        await NextFramesAsync();
        ButtonsE2EHelpers.AreClose(30, (await FloatBtn.BoxAsync()).X, Tolerance, "a new offset drops the move too");
    }

    [TestMethod]
    public async Task Draggable_TurnedOff_StopsAnsweringTheKeysAndThePointer()
    {
        await OpenAsync("/e2e/button/float");

        await Page.Locator("#btn-toggle-draggable").ClickAsync();
        await Expect(Page.Locator("#float-draggable")).ToHaveTextAsync("False");
        await Expect(FloatBtn).Not.ToHaveClassAsync(HasClass("bit-btn-drg"));
        await Expect(FloatBtn).Not.ToHaveAttributeAsync("aria-keyshortcuts", ButtonsE2EHelpers.AnyValue);
        await Expect(FloatBtn).ToHaveCSSAsync("cursor", "pointer");

        var before = await FloatBtn.BoxAsync();
        await FloatBtn.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");

        var x = before.X + before.Width / 2;
        var y = before.Y + before.Height / 2;
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x - 100, y - 100, new() { Steps = 10 });
        await Page.Mouse.UpAsync();

        var after = await FloatBtn.BoxAsync();
        ButtonsE2EHelpers.AreClose(before.X, after.X, 0.5, "x unchanged");
        ButtonsE2EHelpers.AreClose(before.Y, after.Y, 0.5, "y unchanged");

        await Page.Locator("#btn-toggle-draggable").ClickAsync();
        await Expect(FloatBtn).ToHaveClassAsync(HasClass("bit-btn-drg"));
        await FloatBtn.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        ButtonsE2EHelpers.AreClose(before.X - 8, (await FloatBtn.BoxAsync()).X, Tolerance, "dragging is back");
    }

    [TestMethod]
    public async Task Draggable_WithoutFloat_IsNotDraggable()
    {
        await OpenAsync("/e2e/button/float");

        await Page.Locator("#btn-toggle-float").ClickAsync();

        await Expect(FloatBtn).Not.ToHaveClassAsync(HasClass("bit-btn-drg"));
        await Expect(FloatBtn).Not.ToHaveClassAsync(HasClass("bit-btn-brg"));
        await Expect(FloatBtn).Not.ToHaveAttributeAsync("aria-keyshortcuts", ButtonsE2EHelpers.AnyValue);

        var before = await FloatBtn.BoxAsync();
        await FloatBtn.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        ButtonsE2EHelpers.AreClose(before.X, (await FloatBtn.BoxAsync()).X, 0.5, "a button in the flow does not move");
    }

    [TestMethod]
    public async Task Unmount_ADraggableFloatingButton_LeavesNoErrorBehind()
    {
        await OpenAsync("/e2e/button/float");

        await FloatBtn.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");

        await Page.Locator("#btn-remove-float").ClickAsync();
        await Expect(FloatBtn).ToHaveCountAsync(0);
        await PingAsync();

        await Page.Locator("#btn-remove-float").ClickAsync();
        await Expect(FloatBtn).ToBeVisibleAsync();
        await FloatBtn.FocusAsync();
        var before = await FloatBtn.BoxAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        ButtonsE2EHelpers.AreClose(before.Y - 8, (await FloatBtn.BoxAsync()).Y, Tolerance, "a remounted button is draggable again");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }
}
