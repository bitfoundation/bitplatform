namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of the two other ways a BitListBox is given its items (/e2e/listbox/apis): objects of a custom class
/// read through NameSelectors, and BitListBoxOption components - above all an option rendered after the others, which the
/// list box puts back in the order of the markup by reading the document, and the parameters of an option changing.
/// </summary>
[TestClass]
public class BitListBoxApisE2ETests : E2ETestBase
{
    private const string Url = "/e2e/listbox/apis";

    private ILocator Options(string id) => Page.Locator($"#{id} [role=option]");

    private ILocator Texts(string id) => Page.Locator($"#{id} .bit-lbx-txt");

    [TestMethod]
    public async Task CustomItems_AreReadThroughTheNameSelectors_AndChosenWithTheKeyboard()
    {
        await OpenAsync(Url);

        await Expect(Texts("lbx-custom")).ToHaveTextAsync(["Apple", "Banana", "Cherry"]);
        await Expect(Options("lbx-custom").Nth(1)).ToHaveAttributeAsync("aria-disabled", "true");

        await Options("lbx-custom").Nth(0).ClickAsync();
        await Expect(Page.Locator("#custom-value")).ToHaveTextAsync("1");

        // The value is read by name, and the keyboard passes the item the selector disables by.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Options("lbx-custom").Nth(2)).ToBeFocusedAsync();
        await Expect(Page.Locator("#custom-value")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task AnOptionRenderedAfterTheOthers_TakesItsPlaceInTheMarkupOrder()
    {
        await OpenAsync(Url);

        await Options("lbx-options").Nth(1).ClickAsync();
        await Expect(Page.Locator("#option-values")).ToHaveTextAsync("cherry");

        // Banana registers last, but the list box reads the order of the markers in the document and puts it between
        // the two; Cherry stays selected.
        await Page.Locator("#btn-middle").ClickAsync();

        await Expect(Texts("lbx-options")).ToHaveTextAsync(["Apple", "Banana", "Cherry"]);
        await Expect(Options("lbx-options").Nth(2)).ToHaveAttributeAsync("aria-selected", "true");

        // The keyboard walks the items in that order too.
        await Options("lbx-options").Nth(0).ClickAsync();
        await Expect(Page.Locator("#option-values")).ToHaveTextAsync("cherry,apple");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Options("lbx-options").Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#option-values")).ToHaveTextAsync("cherry,apple,banana");

        await Page.Locator("#btn-middle").ClickAsync();
        await Expect(Texts("lbx-options")).ToHaveTextAsync(["Apple", "Cherry"]);
    }

    [TestMethod]
    public async Task AChangeToTheParametersOfAnOption_ShowsInItsItem()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-last").ClickAsync();

        await Expect(Texts("lbx-options")).ToHaveTextAsync(["Apple", "Sour cherry"]);
        await Expect(Options("lbx-options").Nth(1)).ToHaveAttributeAsync("aria-disabled", "true");

        // The markers the options render are hidden from everyone; only the items are read.
        await Expect(Page.Locator("#lbx-options [data-bit-lbx-opt]").First).ToBeHiddenAsync();
        await AssertNoAxeViolationsAsync("#lbx-options");
        await AssertNoAxeViolationsAsync("#lbx-custom");
    }
}
