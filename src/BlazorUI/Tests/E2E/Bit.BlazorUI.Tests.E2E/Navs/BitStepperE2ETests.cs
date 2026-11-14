using System.Text.RegularExpressions;
using Bit.BlazorUI.Tests.E2E.Buttons;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of BitStepper (/e2e/stepper) in a real browser: the steps laid out in a row with the lines between
/// them, the way already come drawn in the active color, a click and the keyboard moving the current step, the page
/// moving it forward, the names a screen reader reads and axe; the vertical layout with its line under the middle of the
/// indicators; a linear stepper keeping the steps ahead out of reach and out of the tab order; a step in error and a
/// disabled one; right to left; and forced colors.
/// </summary>
[TestClass]
public class BitStepperE2ETests : E2ETestBase
{
    private const string Url = "/e2e/stepper";

    private ILocator Steps(string id) => Page.Locator($"#{id} > li");

    private ILocator Indicators(string id) => Page.Locator($"#{id} .bit-stp-ind");

    private ILocator Buttons(string id) => Page.Locator($"#{id} .bit-stp-btn");

    private ILocator Connectors(string id) => Page.Locator($"#{id} .bit-stp-con");

    private static void Near(double expected, double actual, string what, double tolerance = 1.5)
    {
        Assert.IsTrue(Math.Abs(expected - actual) < tolerance, $"{what}: expected about {expected}, got {actual}.");
    }

    [TestMethod]
    public async Task TheSteps_StandInARow_WithTheWayAlreadyComeInTheActiveColor()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-next").ClickAsync();
        await Page.Locator("#btn-next").ClickAsync();
        await Expect(Page.Locator("#basic-index")).ToHaveTextAsync("2");

        // One row, from start to end, with a line between every two indicators.
        var boxes = new List<LocatorBoundingBoxResult>();
        for (var i = 0; i < 4; i++) boxes.Add(await Indicators("stp-basic").Nth(i).BoxAsync());

        for (var i = 1; i < 4; i++)
        {
            Near(boxes[0].Y, boxes[i].Y, $"indicator {i} on the row");
            Assert.IsTrue(boxes[i].X > boxes[i - 1].X + boxes[i - 1].Width, $"indicator {i} after the one before it");
        }

        await Expect(Connectors("stp-basic")).ToHaveCountAsync(3);
        var line = await Connectors("stp-basic").Nth(0).BoxAsync();
        Assert.IsTrue(line.Width > 8, "the line fills the room up to the next step.");

        // Each step takes what its label needs and an equal share of what is left: the lines come out equally long, and
        // a label with room enough does not wrap.
        for (var i = 1; i < 3; i++)
        {
            Near(line.Width, (await Connectors("stp-basic").Nth(i).BoxAsync()).Width, $"line {i} as long as the first", 2);
        }

        var description = await Page.Locator("#stp-basic .bit-stp-dsc").BoxAsync();
        var lineHeight = await Page.Locator("#stp-basic .bit-stp-dsc").EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).lineHeight) || el.getBoundingClientRect().height");
        Assert.IsTrue(description.Height < lineHeight * 1.5, $"the description stays on one line ({description.Height}px).");

        // The completed and the current indicators are filled with the primary color, an upcoming one is not; the
        // lines are drawn in it up to the current step.
        var primary = await Page.ResolveColorAsync("var(--bit-clr-pri)");
        await Expect(Indicators("stp-basic").Nth(0)).ToHaveCSSAsync("background-color", primary);
        await Expect(Indicators("stp-basic").Nth(2)).ToHaveCSSAsync("background-color", primary);
        await Expect(Indicators("stp-basic").Nth(3)).Not.ToHaveCSSAsync("background-color", primary);
        await Expect(Connectors("stp-basic").Nth(1)).ToHaveCSSAsync("background-color", primary);
        await Expect(Connectors("stp-basic").Nth(2)).Not.ToHaveCSSAsync("background-color", primary);
    }

    [TestMethod]
    public async Task AStep_IsNamedWithItsState_AndTheCurrentOneSaysSo()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-next").ClickAsync();
        await Expect(Page.Locator("#basic-index")).ToHaveTextAsync("1");

        // The indicator is drawn, not read: the text, the description and the state are what the step is called.
        await Expect(Buttons("stp-basic").Nth(0)).ToHaveAccessibleNameAsync(new Regex(@"^Account\s*Your details\s*Completed$"));
        await Expect(Buttons("stp-basic").Nth(1)).ToHaveAccessibleNameAsync("Address");
        await Expect(Buttons("stp-basic").Nth(1)).ToHaveAttributeAsync("aria-current", "step");
        await Expect(Page.Locator("#stp-basic")).ToHaveAttributeAsync("role", "list");

        await AssertNoAxeViolationsAsync("#stp-basic");
        await AssertNoAxeViolationsAsync("#stp-states");
        await AssertNoAxeViolationsAsync("#stp-linear");
    }

    [TestMethod]
    public async Task AClickOrTheKeyboard_GoesToAStep()
    {
        await OpenAsync(Url);

        await Buttons("stp-basic").Nth(2).ClickAsync();

        await Expect(Page.Locator("#basic-index")).ToHaveTextAsync("2");
        await Expect(Steps("stp-basic").Nth(2)).ToHaveClassAsync(new Regex(@"\bbit-stp-cur\b"));
        await Expect(Steps("stp-basic").Nth(1)).ToHaveClassAsync(new Regex(@"\bbit-stp-cmp\b"));

        // Every step is a button in the tab order, and Enter goes to it.
        await Buttons("stp-basic").Nth(2).FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Buttons("stp-basic").Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#basic-index")).ToHaveTextAsync("1");
        await Expect(Buttons("stp-basic").Nth(1)).ToHaveAttributeAsync("aria-current", "step");
    }

    [TestMethod]
    public async Task ALinearStepper_KeepsTheStepsAheadOutOfReach()
    {
        await OpenAsync(Url);

        // Only the steps already come to are in the tab order: the two done and the current one.
        await Page.Locator("#before-linear").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Buttons("stp-linear").Nth(0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Buttons("stp-linear").Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Buttons("stp-linear").Nth(2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-linear")).ToBeFocusedAsync();

        // A click ahead goes nowhere; one back does.
        await Buttons("stp-linear").Nth(3).ClickAsync();
        await Expect(Page.Locator("#linear-index")).ToHaveTextAsync("2");

        await Buttons("stp-linear").Nth(0).ClickAsync();
        await Expect(Page.Locator("#linear-index")).ToHaveTextAsync("0");
        await Expect(Buttons("stp-linear").Nth(1)).Not.ToHaveAttributeAsync("type", "button");
    }

    [TestMethod]
    public async Task AStepInError_IsDrawnInTheErrorColor_AndADisabledOneIsSkipped()
    {
        await OpenAsync(Url);

        var error = await Page.ResolveColorAsync("var(--bit-clr-err)");
        await Expect(Indicators("stp-states").Nth(1)).ToHaveCSSAsync("background-color", error);
        await Expect(Page.Locator("#stp-states .bit-stp-err .bit-stp-txt")).ToHaveCSSAsync("color", error);
        await Expect(Buttons("stp-states").Nth(1)).ToHaveAccessibleNameAsync(new Regex(@"^Address\s*Error$"));

        // The disabled step is a disabled button, so Tab goes past it.
        await Expect(Buttons("stp-states").Nth(3)).ToBeDisabledAsync();
        await Buttons("stp-states").Nth(2).FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Buttons("stp-states").Nth(4)).ToBeFocusedAsync();
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
            var textEnd = await Page.Locator("#stp-narrow > li").Nth(i).Locator(".bit-stp-lbl")
                                    .EvaluateAsync<double>("el => { const r = document.createRange(); r.selectNodeContents(el); return Math.max(...[...r.getClientRects()].map(x => x.right)); }");
            var line = await Connectors("stp-narrow").Nth(i).BoxAsync();

            Assert.IsTrue(textEnd <= line.X + 0.5, $"label {i} ends at {textEnd}, the line starts at {line.X}.");
            Assert.IsTrue(line.Width >= 15, $"line {i} keeps its room ({line.Width}px).");
        }

        // The long descriptions wrap to make that room.
        var description = await Page.Locator("#stp-narrow .bit-stp-dsc").First.BoxAsync();
        Assert.IsTrue(description.Height > 20, "the description wraps.");
    }

    [TestMethod]
    public async Task ADisabledStepper_DrawsEveryStepAndLineInTheDisabledPalette()
    {
        await OpenAsync(Url);

        var primary = await Page.ResolveColorAsync("var(--bit-clr-pri)");
        var disabledBorder = await Page.ResolveColorAsync("var(--bit-clr-brd-dis)");

        await Expect(Page.Locator("#stp-disabled")).ToHaveClassAsync(new Regex(@"\bbit-dis\b"));
        await Expect(Indicators("stp-disabled").Nth(0)).Not.ToHaveCSSAsync("background-color", primary);
        await Expect(Indicators("stp-disabled").Nth(2)).ToHaveCSSAsync("border-color", disabledBorder);
        await Expect(Connectors("stp-disabled").Nth(0)).ToHaveCSSAsync("background-color", disabledBorder);
        await Expect(Page.Locator("#stp-disabled button:not([disabled])")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AVerticalStepper_StacksTheSteps_WithTheLineUnderTheMiddleOfTheIndicators()
    {
        await OpenAsync(Url);

        var first = await Indicators("stp-vertical").Nth(0).BoxAsync();
        var second = await Indicators("stp-vertical").Nth(1).BoxAsync();
        var line = await Connectors("stp-vertical").Nth(0).BoxAsync();

        Near(first.X, second.X, "the indicators in one column");
        Assert.IsTrue(second.Y > first.Y + first.Height, "the second step under the first.");

        Assert.IsTrue(line.Height >= 16, "the line runs down between the two steps.");
        Assert.IsTrue(line.Y >= first.Y + first.Height && line.Y + line.Height <= second.Y, "the line sits between the indicators.");
        Near(first.X + first.Width / 2, line.X + line.Width / 2, "the line under the middle of the indicator");
    }

    [TestMethod]
    public async Task ARightToLeftStepper_RunsFromTheRight()
    {
        await OpenAsync(Url);

        var first = await Indicators("stp-rtl").Nth(0).BoxAsync();
        var last = await Indicators("stp-rtl").Nth(3).BoxAsync();

        Assert.IsTrue(first.X > last.X, "the first step is on the right.");

        // The text of a step sits on the inner side of its indicator, the left here.
        var text = await Page.Locator("#stp-rtl .bit-stp-txt").Nth(0).BoxAsync();
        Assert.IsTrue(text.X + text.Width <= first.X + 1, "the text is on the left of the indicator.");
    }

    [TestMethod]
    public async Task ForcedColors_DrawTheActiveIndicatorsInHighlight()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(Url);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var highlight = await Page.ResolveColorAsync("Highlight");
        await Expect(Indicators("stp-vertical").Nth(1)).ToHaveCSSAsync("background-color", highlight);
        await Expect(Indicators("stp-vertical").Nth(0)).ToHaveCSSAsync("background-color", highlight);
        await Expect(Indicators("stp-states").Nth(3)).ToHaveCSSAsync("border-color", await Page.ResolveColorAsync("GrayText"));
    }
}
