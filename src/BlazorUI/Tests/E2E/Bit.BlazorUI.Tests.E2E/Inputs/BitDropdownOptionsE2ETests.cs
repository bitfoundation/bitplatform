namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The Options API of BitDropdown (<c>/e2e/dropdown/options</c>): BitDropdownOption children rendered in markup order
/// with headers, dividers and disabled options, the Options alias with multi select chips, options added, removed,
/// renamed and disabled at runtime, every option parameter (Id, Class, Style, Title, AriaLabel, IsHidden, IconName,
/// Icon, Data), a highlighted search keeping the groups, the Dynamic ComboBox creating a BitDropdownOption, a
/// virtualized list of options and the loading state over options.
/// </summary>
[TestClass]
public class BitDropdownOptionsE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/options";

    [TestMethod]
    public async Task Options_RenderTheListInMarkupOrder_AndPickingOneSelectsIt()
    {
        await OpenAsync(Url);

        await Expect(TextContainer("dd-opt")).ToHaveTextAsync("Select an option");

        var callout = await OpenDropdownAsync("dd-opt");

        await Expect(Options(callout)).ToHaveTextAsync(["Apple", "Banana", "Orange", "Broccoli", "Carrot"]);
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Fruits", "Vegetables"]);
        await Expect(callout.Locator(".bit-drp-sep")).ToHaveCountAsync(1);
        await Expect(Option(callout, "Orange")).ToBeDisabledAsync();
        await Expect(Option(callout, "Carrot")).ToHaveAttributeAsync("aria-setsize", "5");
        await Expect(Option(callout, "Carrot")).ToHaveAttributeAsync("aria-posinset", "5");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Option(callout, "Broccoli").ClickAsync();

        await Expect(Page.Locator("#opt-value")).ToHaveTextAsync("v-bro");
        await Expect(Page.Locator("#opt-selected")).ToHaveTextAsync("Broccoli");
        await Expect(TextContainer("dd-opt")).ToHaveTextAsync("Broccoli");
        await Expect(callout).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task OptionsAlias_RendersAMultiSelectWithChips()
    {
        await OpenAsync(Url);

        var chips = Page.Locator("#dd-opt-multi .bit-drp-chp");
        await Expect(chips).ToHaveTextAsync(["Banana"]);

        var callout = await OpenDropdownAsync("dd-opt-multi");
        await Expect(Option(callout, "Banana")).ToHaveAttributeAsync("aria-selected", "true");

        await Option(callout, "Grape").ClickAsync();

        await Expect(Page.Locator("#opt-multi-values")).ToHaveTextAsync("f-ban,f-gra");
        await Expect(chips).ToHaveTextAsync(["Banana", "Grape"]);
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        await Page.Locator("#dd-opt-multi").GetByRole(AriaRole.Button, new() { Name = "Remove Banana" }).ClickAsync();
        await Expect(Page.Locator("#opt-multi-values")).ToHaveTextAsync("f-gra");
    }

    [TestMethod]
    public async Task Options_AddedAndRemovedAtRuntime_JoinAndLeaveTheListAndTheSelection()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-opt-runtime");
        await Expect(Options(callout)).ToHaveTextAsync(["First", "Last"]);
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        await Page.Locator("#btn-add-kiwi").ClickAsync();

        callout = await OpenDropdownAsync("dd-opt-runtime");
        await Expect(Options(callout)).ToHaveTextAsync(["First", "Kiwi", "Last"]);
        await Option(callout, "Kiwi").ClickAsync();

        await Expect(Page.Locator("#runtime-value")).ToHaveTextAsync("kiwi");
        await Expect(TextContainer("dd-opt-runtime")).ToHaveTextAsync("Kiwi");

        // The selected option leaves the list, and the field no longer names it.
        await Page.Locator("#btn-remove-kiwi").ClickAsync();
        await Expect(TextContainer("dd-opt-runtime")).ToHaveTextAsync("");
        await Expect(Root("dd-opt-runtime")).Not.ToHaveClassAsync(HasClass("bit-drp-hvl"));

        // It comes back as the selection the bound value still names.
        await Page.Locator("#btn-add-kiwi").ClickAsync();
        await Expect(TextContainer("dd-opt-runtime")).ToHaveTextAsync("Kiwi");
        await Expect(Root("dd-opt-runtime")).ToHaveClassAsync(HasClass("bit-drp-hvl"));
    }

    [TestMethod]
    public async Task Options_ParametersChangedAtRuntime_ReachTheListAndTheField()
    {
        await OpenAsync(Url);

        await Expect(TextContainer("dd-opt-runtime")).ToHaveTextAsync("First");

        await Page.Locator("#btn-rename-first").ClickAsync();
        await Expect(TextContainer("dd-opt-runtime")).ToHaveTextAsync("First renamed");

        await Page.Locator("#btn-disable-first").ClickAsync();

        var callout = await OpenDropdownAsync("dd-opt-runtime");
        await Expect(Option(callout, "First renamed")).ToBeDisabledAsync();
        await Expect(Option(callout, "Last")).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task OptionProperties_ReachTheOption()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-opt-props");
        var styled = Option(callout, "Styled aria");

        await Expect(styled).ToHaveAttributeAsync("id", "opt-styled");
        await Expect(styled).ToHaveAttributeAsync("title", "Styled title");
        await Expect(styled.Locator("xpath=..")).ToHaveClassAsync(HasClass("opt-cls"));
        await Expect(styled.Locator("xpath=..")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(styled).ToHaveAttributeAsync("aria-describedby", "opt-group-header");
        await Expect(callout.Locator("#opt-group-header")).ToHaveTextAsync("Group");
        await Expect(callout.Locator("[role=option]", new() { HasTextString = "Hidden" })).ToBeHiddenAsync();
        await Expect(styled).ToHaveAttributeAsync("aria-setsize", "3");
        await Expect(callout.Locator("[role=option]", new() { HasTextString = "With icon" }).Locator("i.bit-drp-iic")).ToHaveClassAsync(HasClass("bit-icon--Heart"));
        await Expect(callout.Locator("[role=option]", new() { HasTextString = "External icon" }).Locator("i.bit-drp-iic")).ToHaveClassAsync(HasClass("ext-opt-icon"));

        await styled.ClickAsync();

        await Expect(Page.Locator("#props-value")).ToHaveTextAsync("s");
        await Expect(Page.Locator("#props-data")).ToHaveTextAsync("styled-data");
    }

    [TestMethod]
    public async Task OptionsSearch_FiltersTheOptions_HighlightsTheMatch_AndKeepsTheirGroups()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-opt-search");
        var input = callout.Locator("input.bit-drp-sin");

        await input.FillAsync("ap");

        await Expect(Options(callout)).ToHaveTextAsync(["Apple", "Grape"]);
        await Expect(Option(callout, "Grape").Locator(".bit-drp-hlt")).ToHaveTextAsync("ap");
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Fruits"]);

        await input.FillAsync("car");

        await Expect(Options(callout)).ToHaveTextAsync(["Carrot"]);
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Vegetables"]);
    }

    [TestMethod]
    public async Task OptionsCombo_Dynamic_CreatesAnOptionOutOfTheTypedText()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-opt-combo");
        await input.FocusAsync();
        await input.FillAsync("Kiwi");

        var callout = await CalloutAsync("dd-opt-combo");
        await Expect(callout.Locator(".bit-drp-dyn")).ToHaveTextAsync("Add \"Kiwi\"");

        await input.PressAsync("Enter");

        await Expect(Page.Locator("#opt-combo-added")).ToHaveTextAsync("Kiwi");
        await Expect(Page.Locator("#opt-combo-value")).ToHaveTextAsync("dyn-kiwi");
        await Expect(Page.Locator("#dd-opt-combo .bit-drp-tdp")).ToHaveTextAsync("Kiwi");
    }

    [TestMethod]
    public async Task OptionsVirtualize_RendersAWindowOfTheOptions()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-opt-virt");

        await Expect(Option(callout, "Option 001")).ToBeFocusedAsync();
        var rendered = await Options(callout).CountAsync();
        Assert.IsTrue(rendered < 80, $"Only a window of the 300 options is rendered, was {rendered}.");

        await ListBox(callout).EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(Option(callout, "Option 300")).ToBeVisibleAsync();
        await WaitForStableListAsync(callout);

        await Option(callout, "Option 300").ClickAsync();
        await Expect(Page.Locator("#opt-virt-value")).ToHaveTextAsync("o300");
    }

    [TestMethod]
    public async Task OptionsLoading_ShowsTheLoadingState_AndThenTheRegisteredOptions()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-opt-loading");
        await Expect(callout.Locator(".bit-drp-ldc")).ToBeVisibleAsync();
        await Expect(Options(callout)).ToHaveCountAsync(0);

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        await Page.Locator("#btn-stop-loading").ClickAsync();

        callout = await OpenDropdownAsync("dd-opt-loading");
        await Expect(Options(callout)).ToHaveTextAsync(["Apple", "Banana", "Grape"]);
    }
}
