using System.Text.RegularExpressions;
using Bit.BlazorUI.Tests.E2E.Buttons;
using static Bit.BlazorUI.Tests.E2E.Navs.StepperE2EHelpers;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitStepper (/e2e/stepper) in a real browser: the steps laid out in a row with the lines between
/// them, the way already come drawn in the active color, a click and the keyboard moving the current step, the page
/// moving it forward, the names a screen reader reads and axe; the vertical layout with its line under the middle of the
/// indicators; a linear stepper keeping the steps ahead out of reach and out of the tab order; a step in error and a
/// disabled one; right to left; and forced colors. Responsive has a page of its own (/e2e/stepper/responsive): the row it
/// stands in once it is wide enough and the column it takes in a narrow container on a wide screen are covered here, its
/// vertical layout on a phone by BitStepperMobileE2ETests.
/// </summary>
[TestClass]
[E2ERetry]
public class BitStepperE2ETests : E2ETestBase
{
    private const string Url = "/e2e/stepper";

    [TestMethod]
    public async Task TheSteps_StandInARow_WithTheWayAlreadyComeInTheActiveColor()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-next").ClickAsync();
        await Page.Locator("#btn-next").ClickAsync();
        await Expect(Page.Locator("#basic-index")).ToHaveTextAsync("2");

        // One row, from start to end, with a line between every two indicators.
        var boxes = new List<LocatorBoundingBoxResult>();
        for (var i = 0; i < 4; i++) boxes.Add(await Page.StepperIndicators("stp-basic").Nth(i).BoxAsync());

        for (var i = 1; i < 4; i++)
        {
            Near(boxes[0].Y, boxes[i].Y, $"indicator {i} on the row");
            Assert.IsTrue(boxes[i].X > boxes[i - 1].X + boxes[i - 1].Width, $"indicator {i} after the one before it");
        }

        await Expect(Page.StepperConnectors("stp-basic")).ToHaveCountAsync(3);
        var line = await Page.StepperConnectors("stp-basic").Nth(0).BoxAsync();
        Assert.IsTrue(line.Width > 8, "the line fills the room up to the next step.");

        // Each step takes what its label needs and an equal share of what is left: the lines come out equally long, and
        // a label with room enough does not wrap.
        for (var i = 1; i < 3; i++)
        {
            Near(line.Width, (await Page.StepperConnectors("stp-basic").Nth(i).BoxAsync()).Width, $"line {i} as long as the first", 2);
        }

        var description = await Page.Locator("#stp-basic .bit-str-dsc").BoxAsync();
        var lineHeight = await Page.Locator("#stp-basic .bit-str-dsc").EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).lineHeight) || el.getBoundingClientRect().height");
        Assert.IsTrue(description.Height < lineHeight * 1.5, $"the description stays on one line ({description.Height}px).");

        // The completed and the current indicators are filled with the primary color, an upcoming one is not; the
        // lines are drawn in it up to the current step.
        var primary = await Page.ResolveColorAsync("var(--bit-clr-pri)");
        await Expect(Page.StepperIndicators("stp-basic").Nth(0)).ToHaveCSSAsync("background-color", primary);
        await Expect(Page.StepperIndicators("stp-basic").Nth(2)).ToHaveCSSAsync("background-color", primary);
        await Expect(Page.StepperIndicators("stp-basic").Nth(3)).Not.ToHaveCSSAsync("background-color", primary);
        await Expect(Page.StepperConnectors("stp-basic").Nth(1)).ToHaveCSSAsync("background-color", primary);
        await Expect(Page.StepperConnectors("stp-basic").Nth(2)).Not.ToHaveCSSAsync("background-color", primary);
    }

    [TestMethod]
    public async Task AStep_IsNamedWithItsState_AndTheCurrentOneSaysSo()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-next").ClickAsync();
        await Expect(Page.Locator("#basic-index")).ToHaveTextAsync("1");

        // The indicator is drawn, not read: the text, the description and the state are what the step is called.
        await Expect(Page.StepperButtons("stp-basic").Nth(0)).ToHaveAccessibleNameAsync(new Regex(@"^Account\s*Your details\s*Completed$"));
        await Expect(Page.StepperButtons("stp-basic").Nth(1)).ToHaveAccessibleNameAsync("Address");
        await Expect(Page.StepperButtons("stp-basic").Nth(1)).ToHaveAttributeAsync("aria-current", "step");
        await Expect(Page.Locator("#stp-basic")).ToHaveAttributeAsync("role", "list");

        await AssertNoAxeViolationsAsync("#stp-basic");
        await AssertNoAxeViolationsAsync("#stp-states");
        await AssertNoAxeViolationsAsync("#stp-linear");
    }

    [TestMethod]
    public async Task AClickOrTheKeyboard_GoesToAStep()
    {
        await OpenAsync(Url);

        await Page.StepperButtons("stp-basic").Nth(2).ClickAsync();

        await Expect(Page.Locator("#basic-index")).ToHaveTextAsync("2");
        await Expect(Page.StepperSteps("stp-basic").Nth(2)).ToHaveClassAsync(new Regex(@"\bbit-str-cur\b"));
        await Expect(Page.StepperSteps("stp-basic").Nth(1)).ToHaveClassAsync(new Regex(@"\bbit-str-cmp\b"));

        // Every step is a button in the tab order, either way, and Enter goes to it. The walk starts on a step other than
        // the one just clicked: on WebKit on Linux a Shift+Tab sent from the clicked step, focus() or not, never reached
        // the step before it - the click's point, inside that step, is where sequential navigation went on from, which
        // a focus() of the element the click already focused does not move.
        await Page.StepperButtons("stp-basic").Nth(0).FocusAsync();
        await Expect(Page.StepperButtons("stp-basic").Nth(0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.StepperButtons("stp-basic").Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.StepperButtons("stp-basic").Nth(2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Page.StepperButtons("stp-basic").Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#basic-index")).ToHaveTextAsync("1");
        await Expect(Page.StepperButtons("stp-basic").Nth(1)).ToHaveAttributeAsync("aria-current", "step");
    }

    [TestMethod]
    public async Task ALinearStepper_KeepsTheStepsAheadOutOfReach()
    {
        await OpenAsync(Url);

        // Only the steps already come to are in the tab order: the two done and the current one.
        await Page.Locator("#before-linear").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.StepperButtons("stp-linear").Nth(0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.StepperButtons("stp-linear").Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.StepperButtons("stp-linear").Nth(2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-linear")).ToBeFocusedAsync();

        // A click ahead goes nowhere; one back does.
        await Page.StepperButtons("stp-linear").Nth(3).ClickAsync();
        await Expect(Page.Locator("#linear-index")).ToHaveTextAsync("2");

        await Page.StepperButtons("stp-linear").Nth(0).ClickAsync();
        await Expect(Page.Locator("#linear-index")).ToHaveTextAsync("0");
        await Expect(Page.StepperButtons("stp-linear").Nth(1)).Not.ToHaveAttributeAsync("type", "button");
    }

    [TestMethod]
    public async Task AStepInError_IsDrawnInTheErrorColor_AndADisabledOneIsSkipped()
    {
        await OpenAsync(Url);

        var error = await Page.ResolveColorAsync("var(--bit-clr-err)");
        await Expect(Page.StepperIndicators("stp-states").Nth(1)).ToHaveCSSAsync("background-color", error);
        await Expect(Page.Locator("#stp-states .bit-str-err .bit-str-txt")).ToHaveCSSAsync("color", error);
        await Expect(Page.StepperButtons("stp-states").Nth(1)).ToHaveAccessibleNameAsync(new Regex(@"^Address\s*Error$"));

        // The disabled step is a disabled button, so Tab goes past it.
        await Expect(Page.StepperButtons("stp-states").Nth(3)).ToBeDisabledAsync();
        await Page.StepperButtons("stp-states").Nth(2).FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.StepperButtons("stp-states").Nth(4)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task ARowTooNarrow_WrapsTheLabels_WithoutRunningThemIntoTheLines()
    {
        await OpenAsync(Url);

        // Every label ends before the line after its step, which keeps the room it never shrinks below. What is measured is
        // the text itself (a range over it), not the box of the label, which a step shrunk too far would shrink with it
        // while the text spilled out over the line.
        for (var i = 0; i < 4; i++)
        {
            var textEnd = await Page.Locator("#stp-narrow > li").Nth(i).Locator(".bit-str-lbl")
                                    .EvaluateAsync<double>("el => { const r = document.createRange(); r.selectNodeContents(el); return Math.max(...[...r.getClientRects()].map(x => x.right)); }");
            var line = await Page.StepperConnectors("stp-narrow").Nth(i).BoxAsync();

            Assert.IsTrue(textEnd <= line.X + 0.5, $"label {i} ends at {textEnd}, the line starts at {line.X}.");
            Assert.IsTrue(line.Width >= 15, $"line {i} keeps its room ({line.Width}px).");
        }

        // The long descriptions wrap to make that room.
        var description = await Page.Locator("#stp-narrow .bit-str-dsc").First.BoxAsync();
        Assert.IsTrue(description.Height > 20, "the description wraps.");
    }

    [TestMethod]
    public async Task ADisabledStepper_DrawsEveryStepAndLineInTheDisabledPalette()
    {
        await OpenAsync(Url);

        var primary = await Page.ResolveColorAsync("var(--bit-clr-pri)");
        var disabledBorder = await Page.ResolveColorAsync("var(--bit-clr-brd-dis)");

        await Expect(Page.Locator("#stp-disabled")).ToHaveClassAsync(new Regex(@"\bbit-dis\b"));
        await Expect(Page.StepperIndicators("stp-disabled").Nth(0)).Not.ToHaveCSSAsync("background-color", primary);
        await Expect(Page.StepperIndicators("stp-disabled").Nth(2)).ToHaveCSSAsync("border-color", disabledBorder);
        await Expect(Page.StepperConnectors("stp-disabled").Nth(0)).ToHaveCSSAsync("background-color", disabledBorder);
        await Expect(Page.Locator("#stp-disabled button:not([disabled])")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AVerticalStepper_StacksTheSteps_WithTheLineUnderTheMiddleOfTheIndicators()
    {
        await OpenAsync(Url);

        var first = await Page.StepperIndicators("stp-vertical").Nth(0).BoxAsync();
        var second = await Page.StepperIndicators("stp-vertical").Nth(1).BoxAsync();
        var line = await Page.StepperConnectors("stp-vertical").Nth(0).BoxAsync();

        Near(first.X, second.X, "the indicators in one column");
        Assert.IsTrue(second.Y > first.Y + first.Height, "the second step under the first.");

        Assert.IsTrue(line.Height >= 16, "the line runs down between the two steps.");
        Assert.IsTrue(line.Y >= first.Y + first.Height && line.Y + line.Height <= second.Y, "the line sits between the indicators.");
        Near(first.X + first.Width / 2, line.X + line.Width / 2, "the line under the middle of the indicator");
    }

    [TestMethod]
    public async Task AResponsiveStepper_WideEnough_StandsInARow()
    {
        await OpenAsync("/e2e/stepper/responsive");

        await Expect(Page.Locator("#stp-responsive")).ToHaveCSSAsync("flex-direction", "row");

        var first = await Page.StepperIndicators("stp-responsive").Nth(0).BoxAsync();
        var last = await Page.StepperIndicators("stp-responsive").Nth(2).BoxAsync();
        var line = await Page.StepperConnectors("stp-responsive").Nth(0).BoxAsync();

        // In a container 600px wide or more it is the row of a stepper that is not Responsive.
        Near(first.Y, last.Y, "the indicators in one row");
        Assert.IsTrue(last.X > first.X + first.Width, "the last step after the first.");
        Assert.IsTrue(line.Width >= 16 && line.Height < 8, $"the line runs across between the steps ({line.Width}x{line.Height}px).");

        var fixedLast = await Page.StepperIndicators("stp-fixed").Nth(2).BoxAsync();
        Near(fixedLast.X, last.X, "the steps where a stepper that is not Responsive puts them");
    }

    [TestMethod]
    public async Task AResponsiveStepper_InANarrowContainer_StacksTheSteps_OnAWideScreenToo()
    {
        await OpenAsync("/e2e/stepper/responsive");

        // It is the width of the stepper's container that decides, not the screen's: the screen is wide, the box is not.
        Assert.IsTrue(Page.ViewportSize!.Width >= 600, $"a wide screen ({Page.ViewportSize.Width}px).");
        await Expect(Page.Locator("#stp-responsive-narrow")).ToHaveCSSAsync("flex-direction", "column");

        var first = await Page.StepperIndicators("stp-responsive-narrow").Nth(0).BoxAsync();
        var second = await Page.StepperIndicators("stp-responsive-narrow").Nth(1).BoxAsync();
        var line = await Page.StepperConnectors("stp-responsive-narrow").Nth(0).BoxAsync();

        Near(first.X, second.X, "the indicators in one column");
        Assert.IsTrue(second.Y > first.Y + first.Height, "the second step under the first.");
        Assert.IsTrue(line.Height >= 16 && line.Width < 8, $"the line runs down between the steps ({line.Width}x{line.Height}px).");
        Near(first.X + first.Width / 2, line.X + line.Width / 2, "the line under the middle of the indicator");

        var widths = await Page.EvaluateAsync<double[]>(@"() => [
            document.querySelector('#narrow-box').getBoundingClientRect().width,
            document.querySelector('#stp-responsive-narrow').scrollWidth
        ]");
        Assert.IsTrue(widths[1] <= widths[0] + 1, $"the stepper ({widths[1]}px) fits its container ({widths[0]}px).");
    }

    [TestMethod]
    public async Task AResponsiveCompactStepper_ShowsTheRowWhenWideEnough_AndTheCurrentStepAloneInANarrowContainer()
    {
        await OpenAsync("/e2e/stepper/responsive");

        Assert.IsTrue(Page.ViewportSize!.Width >= 600, $"a wide screen ({Page.ViewportSize.Width}px).");

        // Wide enough: the row of steps, and no compact part.
        await Expect(Page.Locator("#stp-compact > .bit-str-lst")).ToBeVisibleAsync();
        await Expect(Page.Locator("#stp-compact > .bit-str-lst")).ToHaveCSSAsync("flex-direction", "row");
        await Expect(Page.Locator("#stp-compact > .bit-str-sum")).ToBeHiddenAsync();

        // In a narrow container on the same screen: the compact part in place of the column.
        await Expect(Page.Locator("#stp-compact-narrow > .bit-str-lst")).ToBeHiddenAsync();
        await Expect(Page.Locator("#stp-compact-narrow .bit-str-pos")).ToHaveTextAsync("Step 2 of 3");
        await Expect(Page.Locator("#stp-compact-narrow .bit-str-ctx")).ToHaveTextAsync("Destination");

        // A segment per step, side by side across the stepper, the way already come in the active color.
        var segments = Page.Locator("#stp-compact-narrow .bit-str-seg");
        await Expect(segments).ToHaveCountAsync(3);

        var first = await segments.Nth(0).BoxAsync();
        var last = await segments.Nth(2).BoxAsync();
        Near(first.Y, last.Y, "the segments in one row");
        Near(first.Width, last.Width, "the segments equally wide");

        var colors = await segments.EvaluateAllAsync<string[]>("els => els.map(e => getComputedStyle(e).backgroundColor)");
        Assert.AreEqual(colors[0], colors[1], "the completed and the current segment in the active color.");
        Assert.AreNotEqual(colors[1], colors[2], "an upcoming segment in another color.");

        var widths = await Page.EvaluateAsync<double[]>(@"() => [
            document.querySelector('#compact-narrow-box').getBoundingClientRect().width,
            document.querySelector('#stp-compact-narrow').scrollWidth
        ]");
        Assert.IsTrue(widths[1] <= widths[0] + 1, $"the stepper ({widths[1]}px) fits its container ({widths[0]}px).");
    }

    [TestMethod]
    public async Task ARightToLeftStepper_RunsFromTheRight()
    {
        await OpenAsync(Url);

        var first = await Page.StepperIndicators("stp-rtl").Nth(0).BoxAsync();
        var last = await Page.StepperIndicators("stp-rtl").Nth(3).BoxAsync();

        Assert.IsTrue(first.X > last.X, "the first step is on the right.");

        // The text of a step sits on the inner side of its indicator, the left here.
        var text = await Page.Locator("#stp-rtl .bit-str-txt").Nth(0).BoxAsync();
        Assert.IsTrue(text.X + text.Width <= first.X + 1, "the text is on the left of the indicator.");
    }

    [TestMethod]
    public async Task ForcedColors_DrawTheActiveIndicatorsInHighlight()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Url);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var highlight = await Page.ResolveColorAsync("Highlight");
        await Expect(Page.StepperIndicators("stp-vertical").Nth(1)).ToHaveCSSAsync("background-color", highlight);
        await Expect(Page.StepperIndicators("stp-vertical").Nth(0)).ToHaveCSSAsync("background-color", highlight);
        await Expect(Page.StepperIndicators("stp-states").Nth(3)).ToHaveCSSAsync("border-color", await Page.ResolveColorAsync("GrayText"));
    }
}
