using Bit.BlazorUI.Tests.E2E.Buttons;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of a Responsive BitStepper (/e2e/stepper/responsive) on a phone: a touch screen at a phone's
/// viewport, where three steps with a description each are wider than the screen in a row. A Responsive stepper lays
/// them out from top to bottom, with the line under the middle of the indicators as a vertical stepper draws it, and
/// stays inside its container; one that is not Responsive is still the row that sticks out. The row a Responsive stepper
/// stays on a wider screen is covered by BitStepperE2ETests.
/// </summary>
[TestClass]
public class BitStepperMobileE2ETests : E2ETestBase
{
    private const string Url = "/e2e/stepper/responsive";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
        // Firefox has no mobile emulation; the viewport is what the stylesheet reads anyway.
        IsMobile = IsFirefox is false
    };

    private ILocator Indicators(string id) => Page.Locator($"#{id} .bit-stp-ind");

    private ILocator Connectors(string id) => Page.Locator($"#{id} .bit-stp-con");

    private static void Near(double expected, double actual, string what, double tolerance = 1.5)
    {
        Assert.IsTrue(Math.Abs(expected - actual) < tolerance, $"{what}: expected about {expected}, got {actual}.");
    }

    [TestMethod]
    public async Task Responsive_StacksTheSteps_WithTheLineUnderTheMiddleOfTheIndicators()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#stp-responsive")).ToHaveCSSAsync("flex-direction", "column");

        var first = await Indicators("stp-responsive").Nth(0).BoxAsync();
        var second = await Indicators("stp-responsive").Nth(1).BoxAsync();
        var third = await Indicators("stp-responsive").Nth(2).BoxAsync();
        var line = await Connectors("stp-responsive").Nth(0).BoxAsync();

        Near(first.X, second.X, "the indicators in one column");
        Near(first.X, third.X, "the indicators in one column");
        Assert.IsTrue(second.Y > first.Y + first.Height, "the second step under the first.");
        Assert.IsTrue(third.Y > second.Y + second.Height, "the third step under the second.");

        Assert.IsTrue(line.Height >= 16, $"the line runs down between the two steps ({line.Height}px).");
        Assert.IsTrue(line.Width < 8, $"the line is as thin as a vertical stepper's ({line.Width}px).");
        Assert.IsTrue(line.Y >= first.Y + first.Height && line.Y + line.Height <= second.Y, "the line sits between the indicators.");
        Near(first.X + first.Width / 2, line.X + line.Width / 2, "the line under the middle of the indicator");

        // Laid out the way a vertical stepper is.
        var vertical = await Indicators("stp-responsive-vertical").Nth(1).BoxAsync();
        var verticalLine = await Connectors("stp-responsive-vertical").Nth(0).BoxAsync();
        Near(vertical.X, second.X, "the indicators where a vertical stepper puts them");
        Near(verticalLine.Width, line.Width, "the line as thick as a vertical stepper's");
        Near(verticalLine.Height, line.Height, "the line as long as a vertical stepper's");
    }

    [TestMethod]
    public async Task Responsive_FitsThePhone_WhereTheRowSticksOutOfItsContainer()
    {
        await OpenAsync(Url);

        // Each element's own width against its container's: the row is wider than the box it is clipped by.
        var widths = await Page.EvaluateAsync<double[]>(@"() => {
            const w = s => document.querySelector(s).getBoundingClientRect().width;
            const sw = s => document.querySelector(s).scrollWidth;
            return [w('#responsive-box'), sw('#stp-responsive'), w('#fixed-box'), sw('#stp-fixed'), document.documentElement.scrollWidth, window.innerWidth];
        }");

        Assert.IsTrue(widths[1] <= widths[0] + 1, $"the Responsive stepper ({widths[1]}px) fits its container ({widths[0]}px).");
        Assert.IsTrue(widths[3] > widths[2] + 1, $"a stepper that is not Responsive ({widths[3]}px) is still the row wider than its container ({widths[2]}px).");
        Assert.IsTrue(widths[4] <= widths[5] + 1, $"nothing on the page is wider than the screen ({widths[4]}px of {widths[5]}px).");

        // Every step of the Responsive stepper is on the screen.
        foreach (var indicator in await Indicators("stp-responsive").AllAsync())
        {
            var box = await indicator.BoxAsync();
            Assert.IsTrue(box.X >= 0 && box.X + box.Width <= widths[5], $"an indicator inside the screen ({box.X}px).");
        }

        await Expect(Page.Locator("#stp-responsive .bit-stp-txt").Last).ToBeInViewportAsync(new() { Ratio = 1 });
    }

    [TestMethod]
    public async Task Responsive_TheStepsStillGoWhereTheyAreTapped()
    {
        await OpenAsync(Url);

        var buttons = Page.Locator("#stp-responsive .bit-stp-btn");

        await buttons.Nth(2).TapAsync();
        await Expect(Page.Locator("#responsive-index")).ToHaveTextAsync("2");

        await Expect(Page.Locator("#stp-responsive > li").Nth(2)).ToHaveClassAsync(HasClass("bit-stp-cur"));
        await Expect(buttons.Nth(2)).ToHaveAttributeAsync("aria-current", "step");
    }
}
