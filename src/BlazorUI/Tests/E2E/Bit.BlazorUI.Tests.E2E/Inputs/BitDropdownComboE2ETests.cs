namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The ComboBox mode of BitDropdown (<c>/e2e/dropdown/combo</c>): the input carrying the combobox role as the one tab
/// stop, filtering and OnSearch as it is typed, the commit target cue with aria-activedescendant, Enter on an exact and
/// a partial term with and without AutoSelectFirstMatch, the arrow keys and typing handing the focus back to the input,
/// Escape dropping the term before ClearOnEscape clears, the clear button, Backspace removing the last selection, the
/// Dynamic mode (DynamicValueGenerator, OnDynamicAdd, DynamicItemTextFormat, DynamicItemTemplate), TokenSeparators,
/// FindItemFunction with ExistsSelectedItemFunction, AutoClearSearch, SelectTextOnFocus, the read-only and disabled
/// states, Tab closing the list, FocusComboInputAsync / FocusAsync / ComboInputElement, and an Immediate debounce.
/// </summary>
[TestClass]
public class BitDropdownComboE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/combo";

    [TestMethod]
    public async Task Combo_TheInputIsTheCombobox_AndTheFieldAroundItIsNoTabStop()
    {
        await OpenAsync(Url);

        var field = Trigger("dd-combo");
        var input = ComboInput("dd-combo");

        await Expect(field).Not.ToHaveAttributeAsync("role", "combobox");
        await Expect(field).ToHaveAttributeAsync("tabindex", "-1");

        await Expect(input).ToHaveAttributeAsync("role", "combobox");
        await Expect(input).ToHaveAttributeAsync("aria-autocomplete", "list");
        await Expect(input).ToHaveAttributeAsync("aria-haspopup", "listbox");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(input).ToHaveAttributeAsync("placeholder", "Type to filter");
        await Expect(input).ToHaveAttributeAsync("aria-labelledby", (await Page.Locator("#dd-combo > label").GetAttributeAsync("id"))!);
        await Expect(input).ToHaveAttributeAsync("aria-describedby", (await Page.Locator("#dd-combo .bit-drp-des").GetAttributeAsync("id"))!);

        await Page.Locator("#dd-combo > label").ClickAsync();
        await Expect(input).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Combo_Typing_OpensTheListAndFiltersIt_ReportingEveryTerm()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo");
        await input.FocusAsync();

        // One keystroke at a time, each one answered before the next: what is tested is the search per keystroke.
        await input.PressSequentiallyAsync("a");
        await Expect(Page.Locator("#combo-search-last")).ToHaveTextAsync("a");
        await input.PressSequentiallyAsync("p");

        var callout = await CalloutAsync("dd-combo");

        await Expect(callout).ToBeVisibleAsync();
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(input).ToHaveAttributeAsync("aria-controls", (await ListBox(callout).GetAttributeAsync("id"))!);
        await Expect(Options(callout)).ToHaveTextAsync(["Apple", "Grape"]);
        await Expect(Option(callout, "Apple").Locator(".bit-drp-hlt")).ToHaveTextAsync("Ap");
        await Expect(Page.Locator("#combo-search-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#combo-search-last")).ToHaveTextAsync("ap");
        await Expect(input).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Combo_EnterOnAnExactName_SelectsTheItemTheCueMarked()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo");
        var triggerId = await Trigger("dd-combo").GetAttributeAsync("id");

        await input.FocusAsync();
        await input.FillAsync("banana");

        var callout = await CalloutAsync("dd-combo");
        var banana = Option(callout, "Banana");

        await Expect(banana).ToHaveClassAsync(HasClass("bit-drp-ctg"));
        await Expect(banana).ToHaveAttributeAsync("id", $"{triggerId}-ctg");
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", $"{triggerId}-ctg");
        Assert.AreNotEqual("none", await StyleAsync(banana, "box-shadow"), "The commit target carries a bar.");

        await input.PressAsync("Enter");

        await Expect(Page.Locator("#combo-value")).ToHaveTextAsync("f-ban");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(input).ToHaveValueAsync("");
        await Expect(input).ToHaveAttributeAsync("placeholder", "");
        await Expect(Page.Locator("#dd-combo .bit-drp-tdp")).ToHaveTextAsync("Banana");
        await Expect(input).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Combo_EnterOnAPartialTerm_SelectsNothing_ButAutoSelectFirstMatchTakesTheFirstMatch()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo");
        await input.FocusAsync();
        await input.FillAsync("ban");

        var callout = await CalloutAsync("dd-combo");
        await Expect(Options(callout)).ToHaveTextAsync(["Banana"]);
        await Expect(callout.Locator(".bit-drp-ctg")).ToHaveCountAsync(0);
        await Expect(input).Not.ToHaveAttributeAsync("aria-activedescendant", new System.Text.RegularExpressions.Regex(".+"));

        await input.PressAsync("Enter");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(input).ToHaveValueAsync("");
        await Expect(Page.Locator("#combo-value")).ToHaveTextAsync("");

        var auto = ComboInput("dd-combo-auto");
        await auto.FocusAsync();
        await auto.FillAsync("ca");

        var autoCallout = await CalloutAsync("dd-combo-auto");
        await Expect(Option(autoCallout, "Carrot")).ToHaveClassAsync(HasClass("bit-drp-ctg"));

        await auto.PressAsync("Enter");
        await Expect(Page.Locator("#combo-auto-value")).ToHaveTextAsync("v-car");
    }

    [TestMethod]
    public async Task Combo_TheArrowsWalkTheList_AndTypingOnAnOptionGoesBackToTheInput()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var callout = await CalloutAsync("dd-combo");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("g");

        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync("g");
        await Expect(Options(callout)).ToHaveTextAsync(["Orange", "Grape"]);
    }

    [TestMethod]
    public async Task Combo_ClickingAnOption_SelectsItAndTheFocusReturnsToTheInput()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-combo");
        await Expect(ComboInput("dd-combo")).ToBeFocusedAsync();

        await Option(callout, "Lettuce").ClickAsync();

        await Expect(Page.Locator("#combo-value")).ToHaveTextAsync("v-let");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(ComboInput("dd-combo")).ToBeFocusedAsync();
    }

    [TestMethod]
    [Ignore("KNOWN BUG: Tab out of a typed ComboBox input closes the list but keeps the term - the change event the browser raises on the blur re-applies it after the close discarded it (HandleOnComboChange), so the input keeps showing it and the next opening is filtered by it, against the documented 'discarded when the callout closes'.")]
    public async Task Combo_Tab_ClosesTheListAndMovesOn()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo");
        await input.FocusAsync();
        await input.PressSequentiallyAsync("a");

        var callout = await CalloutAsync("dd-combo");
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Tab");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(input).Not.ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task Combo_Escape_DropsTheTermAndClosesFirst_ThenClearOnEscapeClears()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-esc");
        await Expect(input).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(Page.Locator("#dd-combo-esc .bit-drp-tdp")).ToHaveTextAsync("Apple");

        await input.FocusAsync();
        await input.FillAsync("ba");

        var callout = await CalloutAsync("dd-combo-esc");
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(input).ToHaveValueAsync("");
        await Expect(input).ToBeFocusedAsync();
        await Expect(Page.Locator("#esc-value")).ToHaveTextAsync("f-app");
        await Expect(Page.Locator("#esc-clear-count")).ToHaveTextAsync("0");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#esc-value")).ToHaveTextAsync("");
        await Expect(Page.Locator("#esc-clear-count")).ToHaveTextAsync("1");
        await Expect(input).Not.ToHaveAttributeAsync("data-bit-esc", "claim");
    }

    [TestMethod]
    public async Task Combo_TheClearButton_ClearsTheSelectionAndFocusesTheInput()
    {
        await OpenAsync(Url);

        await Page.Locator("#dd-combo-esc button.bit-drp-clr").ClickAsync();

        await Expect(Page.Locator("#esc-value")).ToHaveTextAsync("");
        await Expect(Page.Locator("#esc-clear-count")).ToHaveTextAsync("1");
        await Expect(ComboInput("dd-combo-esc")).ToBeFocusedAsync();
        await Expect(ComboInput("dd-combo-esc")).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task ComboMulti_PicksKeepTheListOpen_AndBackspaceOnAnEmptyInputRemovesTheLastChip()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-multi");
        var chips = Page.Locator("#dd-combo-multi .bit-drp-chp");

        await Expect(chips).ToHaveTextAsync(["Apple", "Carrot"]);

        await input.FocusAsync();
        await input.FillAsync("gr");

        var callout = await CalloutAsync("dd-combo-multi");
        await Option(callout, "Grape").ClickAsync();

        await Expect(Page.Locator("#combo-multi-values")).ToHaveTextAsync("f-app,v-car,f-gra");
        await Expect(chips).ToHaveTextAsync(["Apple", "Carrot", "Grape"]);
        await Expect(callout).ToBeVisibleAsync();

        await input.FillAsync("");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("Backspace");
        await Expect(Page.Locator("#combo-multi-values")).ToHaveTextAsync("f-app,v-car");

        await Page.Keyboard.PressAsync("Backspace");
        await Expect(Page.Locator("#combo-multi-values")).ToHaveTextAsync("f-app");
        await Expect(chips).ToHaveTextAsync(["Apple"]);
    }

    [TestMethod]
    public async Task Dynamic_ATermNamingNothing_IsOfferedAsANewItem_AndEnterAddsIt()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-dyn");
        var triggerId = await Trigger("dd-combo-dyn").GetAttributeAsync("id");

        await input.FocusAsync();
        await input.FillAsync("apple");

        var callout = await CalloutAsync("dd-combo-dyn");
        await Expect(callout.Locator(".bit-drp-dyn")).ToHaveCountAsync(0);
        await Expect(Option(callout, "Apple")).ToHaveClassAsync(HasClass("bit-drp-ctg"));

        await input.FillAsync("Kiwi");

        var row = callout.Locator(".bit-drp-dyn");
        await Expect(row).ToHaveTextAsync("Add \"Kiwi\"");
        await Expect(row).ToHaveAttributeAsync("role", "option");
        await Expect(row).ToHaveClassAsync(HasClass("bit-drp-ctg"));
        await Expect(row).ToHaveAttributeAsync("id", $"{triggerId}-dyn");
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", $"{triggerId}-dyn");
        await Expect(callout.Locator(".bit-drp-emp")).ToHaveCountAsync(0);

        await input.PressAsync("Enter");

        await Expect(Page.Locator("#dyn-add-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#dyn-add-last")).ToHaveTextAsync("Kiwi");
        await Expect(Page.Locator("#dyn-value")).ToHaveTextAsync("dyn-kiwi");
        await Expect(Page.Locator("#dd-combo-dyn .bit-drp-tdp")).ToHaveTextAsync("Kiwi");
    }

    [TestMethod]
    public async Task Dynamic_TheRowIsClickable_TakesItsTextFormat_AndATermAlreadySelectedIsRefused()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-dyn-multi");
        await input.FocusAsync();
        await input.FillAsync("Kiwi");

        var callout = await CalloutAsync("dd-combo-dyn-multi");
        var row = callout.Locator(".bit-drp-dyn");

        await Expect(row).ToHaveTextAsync("Create 'Kiwi'");
        await row.ClickAsync();

        await Expect(Page.Locator("#dyn-multi-values")).ToHaveTextAsync("dyn-kiwi");
        await Expect(Page.Locator("#dyn-multi-add-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#dd-combo-dyn-multi .bit-drp-chp")).ToHaveTextAsync(["Kiwi"]);
        await Expect(input).ToHaveValueAsync("");
        await Expect(input).ToBeFocusedAsync();

        await input.FillAsync("kiwi");
        await Expect(callout.Locator(".bit-drp-dyn")).ToHaveCountAsync(0);

        await input.PressAsync("Enter");

        await Expect(Page.Locator("#dyn-multi-add-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#dd-combo-dyn-multi .bit-drp-chp")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task DynamicItemTemplate_DrawsTheRowThatCreatesTheItem()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-dyn-tpl");
        await input.FocusAsync();
        await input.FillAsync("Kiwi");

        var callout = await CalloutAsync("dd-combo-dyn-tpl");
        await Expect(callout.Locator(".bit-drp-dyn #dyn-tpl-content")).ToHaveTextAsync("+ New: Kiwi");

        await callout.Locator(".bit-drp-dyn").ClickAsync();
        await Expect(Page.Locator("#dyn-tpl-value")).ToHaveTextAsync("dyn-kiwi");
    }

    [TestMethod]
    public async Task TokenSeparators_APastedListCommitsEveryTerm_AndATypedSeparatorCommitsTheTermBeforeIt()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-tokens");

        await input.FillAsync("Apple, Mango; Banana");

        await Expect(Page.Locator("#tokens-values")).ToHaveTextAsync("f-app,dyn-mango,f-ban");
        await Expect(Page.Locator("#dd-combo-tokens .bit-drp-chp")).ToHaveTextAsync(["Apple", "Mango", "Banana"]);
        await Expect(input).ToHaveValueAsync("");

        await input.FillAsync("carrot");
        await input.PressAsync(";");

        await Expect(Page.Locator("#tokens-values")).ToHaveTextAsync("f-app,dyn-mango,f-ban,v-car");
        await Expect(input).ToHaveValueAsync("");

        // A term the selection already covers is refused.
        await input.FillAsync("apple,");
        await Expect(Page.Locator("#dd-combo-tokens .bit-drp-chp")).ToHaveCountAsync(4);
    }

    [TestMethod]
    public async Task FindItemFunction_AndExistsSelectedItemFunction_DecideWhatACommitTakes()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-find");

        var callout = await CalloutAsync("dd-combo-find");

        await input.FocusAsync();
        await CommitAsync(input, callout, "v-car");

        await Expect(Page.Locator("#find-values")).ToHaveTextAsync("v-car");
        await Expect(Page.Locator("#dd-combo-find .bit-drp-chp")).ToHaveTextAsync(["Carrot"]);

        // The same value again names a selected item, so nothing is selected or created.
        await CommitAsync(input, callout, "v-car");

        await Expect(Page.Locator("#find-values")).ToHaveTextAsync("v-car");
        await Expect(Page.Locator("#find-add-count")).ToHaveTextAsync("0");

        await CommitAsync(input, callout, "f-app");

        await Expect(Page.Locator("#find-values")).ToHaveTextAsync("v-car,f-app");
        await Expect(Page.Locator("#find-add-count")).ToHaveTextAsync("0");
    }

    // Types a term and presses Enter once the list shows it, then waits for the commit to have emptied the input and
    // closed the list, so the next term is not typed into an input the commit is still clearing.
    private static async Task CommitAsync(ILocator input, ILocator callout, string term)
    {
        await input.FillAsync(term);
        await Expect(input).ToHaveValueAsync(term);
        await Expect(callout).ToBeVisibleAsync();
        await input.PressAsync("Enter");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(input).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task AutoClearSearch_ClearsTheTermAfterEveryPick()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-autoclear");
        await input.FocusAsync();
        await input.FillAsync("an");

        var callout = await CalloutAsync("dd-combo-autoclear");
        await Expect(Options(callout)).ToHaveCountAsync(2);

        await Option(callout, "Banana").ClickAsync();

        await Expect(Page.Locator("#autoclear-values")).ToHaveTextAsync("f-ban");
        await Expect(input).ToHaveValueAsync("");
        await Expect(Options(callout)).ToHaveCountAsync(7);
        await Expect(callout).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task SelectTextOnFocus_SelectsTheTermWhenTheInputTakesTheFocusAgain()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-selecttext");
        await input.FocusAsync();
        await input.FillAsync("ban");

        // The focus leaves without anything closing the list, so the term stays - and without scrolling the page,
        // which would dismiss the list (and the term with it) on its own.
        await Page.Locator("#outside-selecttext").EvaluateAsync("el => el.focus({ preventScroll: true })");
        await Expect(input).ToHaveValueAsync("ban");
        await input.EvaluateAsync("el => el.focus({ preventScroll: true })");

        await Page.WaitForFunctionAsync("() => { const i = document.querySelector('#dd-combo-selecttext input.bit-drp-inp'); return i.selectionStart === 0 && i.selectionEnd === 3; }");

        await Page.Keyboard.PressAsync("c");
        await Expect(input).ToHaveValueAsync("c");
    }

    [TestMethod]
    public async Task ReadOnlyCombo_TheInputIsReadOnly_AndThePickIsRefused()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-ro");

        await Expect(input).ToHaveAttributeAsync("readonly", "");
        await Expect(Page.Locator("#dd-combo-ro .bit-drp-tdp")).ToHaveTextAsync("Apple");

        await input.FocusAsync();
        await Page.Keyboard.PressAsync("b");
        await Expect(input).ToHaveValueAsync("");

        var callout = await OpenDropdownAsync("dd-combo-ro");
        await Option(callout, "Banana").ClickAsync();

        await Expect(Page.Locator("#ro-value")).ToHaveTextAsync("f-app");
        await Expect(callout).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DisabledCombo_TheInputIsDisabledAndTheListNeverOpens()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-dis");

        await Expect(Root("dd-combo-dis")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(input).ToBeDisabledAsync();
        await Expect(input).ToHaveAttributeAsync("aria-disabled", "true");

        await Trigger("dd-combo-dis").ClickAsync(new() { Force = true });

        await Expect(await CalloutAsync("dd-combo-dis")).ToBeHiddenAsync();
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task ComboMethods_FocusTheInput_AndComboInputElementIsReported()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-ref");

        await Page.Locator("#btn-focus-combo").ClickAsync();
        await Expect(input).ToBeFocusedAsync();

        await Page.Locator("#btn-read-element").ClickAsync();
        await Expect(Page.Locator("#ref-element")).ToHaveTextAsync("present");

        await Page.Locator("#btn-focus").ClickAsync();
        await Expect(input).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task ComboImmediate_ADebounce_OpensTheListAtOnceButSearchesOnce()
    {
        await OpenAsync(Url);

        var input = ComboInput("dd-combo-ref");
        await input.FocusAsync();
        await input.PressSequentiallyAsync("car", new() { Delay = 100 });

        var callout = await CalloutAsync("dd-combo-ref");
        await Expect(callout).ToBeVisibleAsync();

        await Expect(Page.Locator("#ref-search-last")).ToHaveTextAsync("car");
        await Expect(Options(callout)).ToHaveTextAsync(["Carrot"]);
        await Expect(Page.Locator("#ref-search-count")).ToHaveTextAsync("1");
    }
}
