using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Navs;

/// <summary>
/// End-to-end tests of the two other ways a BitStepper is given its steps (/e2e/stepper/apis): objects of a custom class
/// read through NameSelectors, and BitStepperOption components - above all an option rendered after the others, which
/// the stepper puts back in the order of the markup by reading the document, and the parameters of an option changing.
/// </summary>
[TestClass]
public class BitStepperApisE2ETests : E2ETestBase
{
    private const string Url = "/e2e/stepper/apis";

    private ILocator Texts(string id) => Page.Locator($"#{id} .bit-stp-txt");

    private ILocator Steps(string id) => Page.Locator($"#{id} > li.bit-stp-stp");

    [TestMethod]
    public async Task CustomItems_AreReadThroughTheNameSelectors_AndGoneTo()
    {
        await OpenAsync(Url);

        await Expect(Texts("stp-custom")).ToHaveTextAsync(["Plan", "Build", "Ship"]);
        await Expect(Page.Locator("#stp-custom .bit-stp-dsc")).ToHaveTextAsync("What to build");
        await Expect(Steps("stp-custom").Nth(1)).ToHaveClassAsync(new Regex(@"\bbit-stp-err\b"));

        await Page.Locator("#stp-custom .bit-stp-btn").Nth(2).ClickAsync();

        await Expect(Page.Locator("#custom-index")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#custom-clicked")).ToHaveTextAsync("Ship");
        await Expect(Steps("stp-custom").Nth(0)).ToHaveClassAsync(new Regex(@"\bbit-stp-cmp\b"));
    }

    [TestMethod]
    public async Task AnOptionRenderedAfterTheOthers_TakesItsPlaceInTheMarkupOrder()
    {
        await OpenAsync(Url);

        await Expect(Texts("stp-options")).ToHaveTextAsync(["First", "Last"]);
        await Expect(Steps("stp-options").Nth(1)).ToHaveClassAsync(new Regex(@"\bbit-stp-cur\b"));

        // The middle option registers last, but the stepper reads the order of the markers in the document and puts it
        // between the two: second, numbered 2, and now the current step of index 1.
        await Page.Locator("#btn-middle").ClickAsync();

        await Expect(Texts("stp-options")).ToHaveTextAsync(["First", "Middle", "Last"]);
        await Expect(Steps("stp-options").Nth(1)).ToHaveClassAsync(new Regex(@"\bbit-stp-cur\b"));
        await Expect(Page.Locator("#stp-options .bit-stp-num")).ToHaveTextAsync(["2", "3"]);
        await Expect(Page.Locator("#stp-options .bit-stp-con")).ToHaveCountAsync(2);

        // Its own click runs, and the stepper goes there.
        await Page.Locator("#stp-options .bit-stp-btn").Nth(2).ClickAsync();
        await Expect(Page.Locator("#options-index")).ToHaveTextAsync("2");
        await Page.Locator("#stp-options .bit-stp-btn").Nth(1).ClickAsync();
        await Expect(Page.Locator("#middle-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#options-index")).ToHaveTextAsync("1");

        await Page.Locator("#btn-middle").ClickAsync();

        await Expect(Texts("stp-options")).ToHaveTextAsync(["First", "Last"]);
    }

    [TestMethod]
    public async Task AChangeToTheParametersOfAnOption_ShowsInItsStep()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-last").ClickAsync();

        await Expect(Texts("stp-options")).ToHaveTextAsync(["First", "Done"]);
        await Expect(Steps("stp-options").Nth(1)).ToHaveClassAsync(new Regex(@"\bbit-stp-err\b"));

        // The markers the options render are hidden from everyone; only the steps are read.
        await Expect(Page.Locator("#stp-options > li").First).ToBeHiddenAsync();
        await AssertNoAxeViolationsAsync("#stp-options");
        await AssertNoAxeViolationsAsync("#stp-custom");
    }
}
