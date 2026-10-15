namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The keyboard of BitDropdown (<c>/e2e/dropdown/keyboard</c>): Enter, Space and the arrows opening on the selected or
/// the first item, Alt+ArrowDown revealing the list without the focus, the arrows walking it past disabled items with
/// wrap and with NoWrapNavigation, Home / End from the closed field and inside the list, PageUp / PageDown, the
/// type-ahead (one character, a repeated one cycling, a buffer, the reset after a pause), Enter and Space picking,
/// Escape / Alt+ArrowUp / Tab closing with the focus back on the field, the focus ring of a keyboard-focused item,
/// Space and Ctrl+A in multi select mode, and the Escape of a dropdown inside a BitDialog being answered by the
/// dropdown before the dialog.
/// </summary>
[TestClass]
public class BitDropdownKeyboardE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/keyboard";

    [TestMethod]
    [DataRow("Enter")]
    [DataRow(" ")]
    [DataRow("ArrowDown")]
    [DataRow("ArrowUp")]
    public async Task OpeningKeys_OpenTheListOnTheFirstItem(string key)
    {
        await OpenAsync(Url);

        await Trigger("dd-kb").FocusAsync();
        await Page.Keyboard.PressAsync(key);

        var callout = await CalloutAsync("dd-kb");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Trigger("dd-kb")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();
        await Expect(Page.Locator("#kb-open-count")).ToHaveTextAsync("1");

        // The page did not scroll under the key the dropdown handled.
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => window.scrollY"));
    }

    [TestMethod]
    public async Task OpeningKeys_LandOnTheSelectedItem()
    {
        await OpenAsync(Url);

        await Trigger("dd-kb-selected").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var callout = await CalloutAsync("dd-kb-selected");
        await Expect(Option(callout, "Broccoli")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AltArrowDown_RevealsTheListWithoutMovingTheFocus_AndEnterOnTheFieldClosesIt()
    {
        await OpenAsync(Url);

        var trigger = Trigger("dd-kb");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowDown");

        var callout = await CalloutAsync("dd-kb");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(trigger).ToHaveAttributeAsync("data-bit-esc", "claim");

        await Page.Keyboard.PressAsync("Enter");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(trigger).Not.ToHaveAttributeAsync("data-bit-esc", "claim");
    }

    [TestMethod]
    public async Task Arrows_WalkTheItems_SkipTheDisabledOne_AndWrapAroundTheEnds()
    {
        await OpenAsync(Url);

        await Trigger("dd-kb").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var callout = await CalloutAsync("dd-kb");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Grape")).ToBeFocusedAsync();

        // The row the arrows reached carries a focus ring of its own.
        await Expect(Option(callout, "Grape")).ToHaveCSSAsync("outline-style", "solid");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Option(callout, "Lettuce")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task NoWrapNavigation_StopsTheArrowsAtTheEnds()
    {
        await OpenAsync(Url);

        await Trigger("dd-kb-nowrap").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var callout = await CalloutAsync("dd-kb-nowrap");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(Option(callout, "Lettuce")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Lettuce")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task HomeAndEnd_JumpToTheEnds_FromTheClosedFieldAndInsideTheList()
    {
        await OpenAsync(Url);

        var trigger = Trigger("dd-kb");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("End");

        var callout = await CalloutAsync("dd-kb");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Option(callout, "Lettuce")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Home");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();
        // The close hands the focus back to the field in a round trip of its own; a Home pressed before it lands is
        // pressed on the hidden option, which WebKit gets to before the round trip returns.
        await Expect(trigger).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Home");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task PageDownAndPageUp_MoveTenItemsAtATime()
    {
        await OpenAsync(Url);

        await Trigger("dd-kb-long").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var callout = await CalloutAsync("dd-kb-long");
        await Expect(Option(callout, "Item 01")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Option(callout, "Item 11")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Option(callout, "Item 21")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Option(callout, "Item 30")).ToBeFocusedAsync();

        // The focused item is scrolled into the list's view - waited for, since the list may still be settling.
        await Page.WaitForFunctionAsync("([list, item]) => item.getBoundingClientRect().bottom <= list.getBoundingClientRect().bottom + 1",
                                        new[] { await ListBox(callout).ElementHandleAsync(), await Option(callout, "Item 30").ElementHandleAsync() });

        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Option(callout, "Item 20")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Option(callout, "Item 10")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Option(callout, "Item 01")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TypeAhead_JumpsToTheMatchingItem_CyclesOnARepeatedCharacter_AndMatchesABuffer()
    {
        await OpenAsync(Url);

        var trigger = Trigger("dd-kb");
        await trigger.FocusAsync();

        // A character typed on the closed field opens it on the item it starts.
        await Page.Keyboard.PressAsync("c");

        var callout = await CalloutAsync("dd-kb");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Option(callout, "Carrot")).ToBeFocusedAsync();

        // The buffer starts over after a pause: the pause itself is what is tested, so it is waited out.
        await Page.WaitForTimeoutAsync(700);
        await Page.Keyboard.PressAsync("b");
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("b");
        await Expect(Option(callout, "Broccoli")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("b");
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();

        await Page.WaitForTimeoutAsync(700);
        await Page.Keyboard.TypeAsync("gr");
        await Expect(Option(callout, "Grape")).ToBeFocusedAsync();

        await Page.WaitForTimeoutAsync(700);
        await Page.Keyboard.TypeAsync("br");
        await Expect(Option(callout, "Broccoli")).ToBeFocusedAsync();
    }

    [TestMethod]
    [DataRow("Enter")]
    [DataRow(" ")]
    public async Task EnterAndSpace_PickTheFocusedItem(string key)
    {
        await OpenAsync(Url);

        await Trigger("dd-kb").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var callout = await CalloutAsync("dd-kb");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync(key);

        await Expect(Page.Locator("#kb-value")).ToHaveTextAsync("f-ban");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Trigger("dd-kb")).ToBeFocusedAsync();
    }

    [TestMethod]
    [DataRow("Escape")]
    [DataRow("Alt+ArrowUp")]
    [DataRow("Tab")]
    public async Task ClosingKeysInsideTheList_CloseItAndHandTheFocusBackToTheField(string key)
    {
        await OpenAsync(Url);

        await Trigger("dd-kb").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var callout = await CalloutAsync("dd-kb");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync(key);

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Trigger("dd-kb")).ToBeFocusedAsync();
        await Expect(Trigger("dd-kb")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#kb-close-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#kb-value")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Tab_FromAFieldWhoseListIsRevealed_ClosesTheListAndMovesOn()
    {
        await OpenAsync(Url);

        await Trigger("dd-kb").FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowDown");

        var callout = await CalloutAsync("dd-kb");
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Tab");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#after-kb")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TabOrder_TheDropdownIsOneStopBetweenItsNeighbours()
    {
        await OpenAsync(Url);

        await Page.Locator("#before-kb").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Trigger("dd-kb")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-kb")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Trigger("dd-kb")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task MultiSelect_SpaceAndEnterToggleTheFocusedItem_AndTheListStaysOpen()
    {
        await OpenAsync(Url);

        await Trigger("dd-kb-multi").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        var callout = await CalloutAsync("dd-kb-multi");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#kb-multi-values")).ToHaveTextAsync("f-app");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#kb-multi-values")).ToHaveTextAsync("f-app,f-ban");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#kb-multi-values")).ToHaveTextAsync("f-ban");
    }

    [TestMethod]
    public async Task MultiSelect_CtrlA_SelectsEveryEnabledItem_AndClearsThemWhenAllAreSelected()
    {
        await OpenAsync(Url);

        await Trigger("dd-kb-multi").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        var callout = await CalloutAsync("dd-kb-multi");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Control+a");
        await Expect(Page.Locator("#kb-multi-values")).ToHaveTextAsync("f-app,f-ban,f-gra,v-bro,v-car,v-let");

        // The page text was not selected by the shortcut the dropdown answered.
        Assert.AreEqual(string.Empty, await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));

        await Page.Keyboard.PressAsync("Control+a");
        await Expect(Page.Locator("#kb-multi-values")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Dialog_EscapeClosesTheListFirst_AndTheDialogOnTheNextPress()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator(".bit-dlg-ctn")).ToBeVisibleAsync();

        var callout = await OpenDropdownAsync("dd-dlg");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(Page.Locator(".bit-dlg-ctn")).ToBeVisibleAsync();
        await Expect(Trigger("dd-dlg")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
        await Expect(Page.Locator(".bit-dlg-ctn")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Dialog_EscapeBeforeTheOpeningFocusLands_KeepsTheFocusInTheDialog()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator(".bit-dlg-ctn")).ToBeVisibleAsync();

        // The press goes in as soon as the list is on the screen, without waiting for the focus the opening hands to
        // its option in a round trip of its own: that focus can land on the option after the field took the press, and
        // the close the press runs would then hide the option holding the focus and drop it to the body, where the
        // dialog never hears the next Escape.
        await Trigger("dd-dlg").ClickAsync();
        var callout = await CalloutAsync("dd-dlg");
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await Expect(Trigger("dd-dlg")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Dialog_EscapeInAComboDropsTheTermFirst_AndTheDialogOnTheNextPress()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator(".bit-dlg-ctn")).ToBeVisibleAsync();

        var input = ComboInput("dd-dlg-combo");
        await input.FocusAsync();
        await input.FillAsync("ap");

        var callout = await CalloutAsync("dd-dlg-combo");
        await Expect(callout).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(input).ToHaveValueAsync("");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Dialog_ClearOnEscapeClearsFirst_AndTheDialogClosesOnTheNextPress()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator(".bit-dlg-ctn")).ToBeVisibleAsync();

        await Trigger("dd-dlg-clear").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#dlg-clear-value")).ToHaveTextAsync("");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Dialog_ThePickedItem_IsSelectedWithoutClosingTheDialog()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-dialog").ClickAsync();

        var callout = await OpenDropdownAsync("dd-dlg");

        // The open list sits above the dialog.
        var calloutZ = await callout.EvaluateAsync<int>("el => parseInt(getComputedStyle(el).zIndex)");
        var dialogZ = await Page.Locator(".bit-dlg").First.EvaluateAsync<int>("el => { for (let e = el; e; e = e.parentElement) { const z = parseInt(getComputedStyle(e).zIndex); if (!isNaN(z)) return z; } return 0; }");
        Assert.IsTrue(calloutZ > dialogZ, $"The callout ({calloutZ}) stacks above the dialog ({dialogZ}).");

        await Option(callout, "Carrot").ClickAsync();

        await Expect(Page.Locator("#dlg-value")).ToHaveTextAsync("v-car");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
    }
}
