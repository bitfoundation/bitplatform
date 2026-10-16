namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The custom item API of BitDropdown (<c>/e2e/dropdown/custom</c>): items of the app's own classes read through
/// NameSelectors by selector (AriaLabel, Class, Id, Data, IsDisabled, IsHidden, ItemType, Style, Text, Title, Value, Icon,
/// IconName) and by property name, the IsSelected property written back onto the items, multi select chips with a
/// highlighted search, the Dynamic ComboBox creating a custom item through TextSetter / ValueSetter and through the
/// property names, and the NameSelectors of a BitParams cascade.
/// </summary>
[TestClass]
public class BitDropdownCustomE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/custom";

    [TestMethod]
    public async Task Selectors_ReadEveryMemberOfTheCustomItem()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-cus");
        var apple = Option(callout, "Apple aria");

        await Expect(apple).ToHaveAttributeAsync("id", "cus-apple");
        await Expect(apple).ToHaveAttributeAsync("title", "Apple title");
        await Expect(apple).ToHaveAttributeAsync("aria-describedby", "cus-header");
        await Expect(apple.Locator("xpath=..")).ToHaveClassAsync(HasClass("cus-cls"));
        await Expect(apple.Locator("xpath=..")).ToHaveCSSAsync("color", "rgb(0, 0, 255)");
        await Expect(apple.Locator("i.bit-drp-iic")).ToHaveClassAsync(HasClass("bit-icon--Heart"));
        await Expect(callout.Locator("[role=option]", new() { HasTextString = "Banana" }).Locator("i.bit-drp-iic")).ToHaveClassAsync(HasClass("cus-ext-icon"));
        await Expect(Option(callout, "Orange")).ToBeDisabledAsync();
        await Expect(callout.Locator("[role=option]", new() { HasTextString = "Hidden" })).ToBeHiddenAsync();
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Fruits", "Vegetables"]);
        await Expect(callout.Locator(".bit-drp-sep")).ToHaveCountAsync(1);
        await Expect(apple).ToHaveAttributeAsync("aria-setsize", "4");

        await apple.ClickAsync();

        await Expect(Page.Locator("#cus-value")).ToHaveTextAsync("f-app");
        await Expect(Page.Locator("#cus-payload")).ToHaveTextAsync("apple-payload");
        await Expect(TextContainer("dd-cus")).ToHaveTextAsync("Apple");

        // The selection is written back onto the items through the IsSelected property name.
        await Page.Locator("#btn-read-flags").ClickAsync();
        await Expect(Page.Locator("#cus-selected")).ToHaveTextAsync("Apple");
    }

    [TestMethod]
    public async Task PropertyNames_ReadTheMembersByName()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-cus-names");

        await Expect(Options(callout)).ToHaveTextAsync(["Mango", "Peach", "Plum"]);
        await Expect(Option(callout, "Peach")).ToBeDisabledAsync();

        await Option(callout, "Plum").ClickAsync();

        await Expect(Page.Locator("#names-value")).ToHaveTextAsync("3");
        await Expect(TextContainer("dd-cus-names")).ToHaveTextAsync("Plum");
    }

    [TestMethod]
    public async Task CustomMulti_ChipsAndAHighlightedSearch()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#dd-cus-multi .bit-drp-chp")).ToHaveTextAsync(["Carrot"]);

        var callout = await OpenDropdownAsync("dd-cus-multi");
        await callout.Locator("input.bit-drp-sin").FillAsync("an");

        await Expect(Options(callout)).ToHaveCountAsync(2);
        await Expect(callout.Locator("[role=option]", new() { HasTextString = "Banana" }).Locator(".bit-drp-hlt")).ToHaveTextAsync("an");

        await callout.Locator("[role=option]", new() { HasTextString = "Banana" }).ClickAsync();

        await Expect(Page.Locator("#cus-multi-values")).ToHaveTextAsync("v-car,f-ban");
        await Expect(Page.Locator("#dd-cus-multi .bit-drp-chp")).ToHaveTextAsync(["Carrot", "Banana"]);
    }

    [TestMethod]
    [DataRow("dd-cus-dyn", "Kiwi", "#cus-dyn-added", "Kiwi|dyn-Kiwi", "#cus-dyn-value", "dyn-Kiwi")]
    [DataRow("dd-cus-dyn-names", "Lime", "#cus-dyn-names-added", "Lime|named-Lime", "#cus-dyn-names-value", "named-Lime")]
    public async Task DynamicCombo_CreatesACustomItem(string id, string text, string addedId, string added, string valueId, string value)
    {
        await OpenAsync(Url);

        var input = ComboInput(id);
        await input.FocusAsync();
        await input.FillAsync(text);

        var callout = await CalloutAsync(id);
        await Expect(callout.Locator(".bit-drp-dyn")).ToHaveTextAsync($"Add \"{text}\"");

        await input.PressAsync("Enter");

        await Expect(Page.Locator(addedId)).ToHaveTextAsync(added);
        await Expect(Page.Locator(valueId)).ToHaveTextAsync(value);
        await Expect(Page.Locator($"#{id} .bit-drp-tdp")).ToHaveTextAsync(text);
    }

    [TestMethod]
    public async Task BitParams_HandsTheNameSelectorsDown()
    {
        await OpenAsync(Url);

        await Expect(TextContainer("dd-cus-params")).ToHaveTextAsync("Cascaded placeholder");

        var callout = await OpenDropdownAsync("dd-cus-params");

        // Only what the cascade names is read: nothing is disabled or hidden by members it does not select.
        await Expect(Options(callout)).ToHaveTextAsync(["Apple", "Banana", "Orange", "Hidden", "Carrot"]);
        await Expect(Option(callout, "Apple")).ToHaveAttributeAsync("id", "cus-apple");
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Fruits", "Vegetables"]);

        await Option(callout, "Banana").ClickAsync();
        await Expect(Page.Locator("#cus-params-value")).ToHaveTextAsync("f-ban");
    }
}
