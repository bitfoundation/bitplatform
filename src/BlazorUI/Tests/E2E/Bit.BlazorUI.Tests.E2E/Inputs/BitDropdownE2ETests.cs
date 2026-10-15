namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The core of BitDropdown over its Items API (<c>/e2e/dropdown</c>): it prerenders, opens and positions its callout,
/// selects in single and multi select mode with the two-way bindings and every selection event, keeps the disabled
/// and read-only contracts, binds IsOpen, reselects, closes on select or not, compares values through a
/// ValueComparer, clears through the button, the Escape key and the public methods, opens on focus, takes the base
/// parameters and the BitParams cascade, mirrors its selection into a native form post, and unmounts cleanly.
/// </summary>
[TestClass]
public class BitDropdownE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown";

    [TestMethod]
    public async Task Prerender_TheServerHtml_AlreadyContainsTheDropdown()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"dd-single\"");
        StringAssert.Contains(html, "bit-drp-wrp");
        StringAssert.Contains(html, "role=\"combobox\"");
        StringAssert.Contains(html, "Select an item");
        // The status line is still the prerendered one: the dropdown is in the HTML before any circuit exists.
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Url);

        await Expect(Trigger("dd-single")).ToHaveAttributeAsync("aria-expanded", "false");
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Single_ClickingTheField_OpensTheCalloutBelowItInTheBodyAndFocusesTheFirstItem()
    {
        await OpenAsync(Url);

        var trigger = Trigger("dd-single");

        await Expect(trigger).ToHaveAttributeAsync("role", "combobox");
        await Expect(trigger).ToHaveAttributeAsync("aria-haspopup", "listbox");
        await Expect(trigger).ToHaveAttributeAsync("tabindex", "0");

        var callout = await OpenDropdownAsync("dd-single");
        await WaitForAnimationsAsync(callout);

        await Expect(Page.Locator("#single-click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#single-open-count")).ToHaveTextAsync("1");

        var listBox = ListBox(callout);
        var listBoxId = await listBox.GetAttributeAsync("id");
        await Expect(trigger).ToHaveAttributeAsync("aria-controls", listBoxId!);
        await Expect(Root("dd-single")).ToHaveAttributeAsync("aria-owns", listBoxId!);

        // Moved out of its section into the body, so no ancestor can clip it.
        Assert.IsFalse(await callout.EvaluateAsync<bool>("el => document.getElementById('basic-single').contains(el)"));
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        await Expect(await OverlayAsync("dd-single")).ToBeVisibleAsync();

        var fieldBox = await BoxAsync(trigger);
        var calloutBox = await BoxAsync(callout);

        Assert.AreEqual(fieldBox.Y + fieldBox.Height, calloutBox.Y, 3, "The callout sits right under the field.");
        Assert.AreEqual(fieldBox.X, calloutBox.X, 1, "The callout starts where the field starts.");
        Assert.AreEqual(fieldBox.Width, calloutBox.Width, 1, "The callout is as wide as the field.");

        // Nothing is selected, so the first enabled item takes the focus.
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Single_PickingAnItem_SelectsItFiresTheEventsClosesAndReturnsTheFocus()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-single");

        await Option(callout, "Banana").ClickAsync();

        await Expect(Page.Locator("#single-value")).ToHaveTextAsync("f-ban");
        await Expect(Page.Locator("#single-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#single-change-last")).ToHaveTextAsync("f-ban");
        await Expect(Page.Locator("#single-select-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#single-select-last")).ToHaveTextAsync("Banana");
        await Expect(Page.Locator("#single-close-count")).ToHaveTextAsync("1");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Trigger("dd-single")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Trigger("dd-single")).ToBeFocusedAsync();
        await Expect(TextContainer("dd-single")).ToHaveTextAsync("Banana");
        await Expect(Root("dd-single")).ToHaveClassAsync(HasClass("bit-drp-hvl"));

        // The selection travels in the hidden native select too.
        await Expect(Page.Locator("#dd-single select[name='single-select'] option")).ToHaveAttributeAsync("value", "f-ban");

        // Reopening lands on the selected item, which says it is selected.
        callout = await OpenDropdownAsync("dd-single");
        await Expect(Option(callout, "Banana")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Option(callout, "Banana")).ToHaveClassAsync(HasClass("bit-drp-sel"));
        await Expect(Option(callout, "Apple")).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();

        var selectedBg = await StyleAsync(Option(callout, "Banana"), "background-color");
        var restBg = await StyleAsync(Option(callout, "Grape"), "background-color");
        Assert.AreNotEqual(restBg, selectedBg, "A selected row is filled.");
    }

    [TestMethod]
    public async Task Single_ADisabledItem_CannotBePicked()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-single");
        var orange = Option(callout, "Orange");

        await Expect(orange).ToBeDisabledAsync();
        await Expect(orange).ToHaveAttributeAsync("aria-disabled", "true");

        await orange.ClickAsync(new() { Force = true });

        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#single-value")).ToHaveTextAsync("");
        await Expect(Page.Locator("#single-select-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Single_HeadersAndDividers_AreNotOptionsAndNameTheirGroup()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-single");

        await Expect(Options(callout)).ToHaveCountAsync(7);
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Fruits", "Vegetables"]);
        await Expect(callout.Locator(".bit-drp-sep")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(callout.Locator(".bit-drp-sep")).ToHaveAttributeAsync("role", "presentation");

        var carrot = Option(callout, "Carrot");
        await Expect(carrot).ToHaveAttributeAsync("aria-setsize", "7");
        await Expect(carrot).ToHaveAttributeAsync("aria-posinset", "6");

        var groupId = await carrot.GetAttributeAsync("aria-describedby");
        Assert.IsFalse(string.IsNullOrEmpty(groupId));
        await Expect(Page.Locator($"[id='{groupId}']")).ToHaveTextAsync("Vegetables");

        var appleGroup = await Option(callout, "Apple").GetAttributeAsync("aria-describedby");
        await Expect(Page.Locator($"[id='{appleGroup}']")).ToHaveTextAsync("Fruits");
    }

    [TestMethod]
    public async Task Single_AValueSetByThePage_ShowsInTheFieldWithoutFiringOnChange()
    {
        await OpenAsync(Url);

        await Expect(TextContainer("dd-single")).ToHaveTextAsync("Select an item");

        await Page.Locator("#btn-single-set").ClickAsync();

        await Expect(TextContainer("dd-single")).ToHaveTextAsync("Carrot");
        await Expect(Root("dd-single")).ToHaveClassAsync(HasClass("bit-drp-hvl"));
        await Expect(HiddenSelect("dd-single").Locator("option")).ToHaveAttributeAsync("value", "v-car");
        await Expect(Page.Locator("#single-change-count")).ToHaveTextAsync("0");

        var callout = await OpenDropdownAsync("dd-single");
        await Expect(Option(callout, "Carrot")).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        await Page.Locator("#btn-single-null").ClickAsync();

        await Expect(TextContainer("dd-single")).ToHaveTextAsync("Select an item");
        await Expect(Root("dd-single")).Not.ToHaveClassAsync(HasClass("bit-drp-hvl"));
        await Expect(HiddenSelect("dd-single").Locator("option")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Single_TheLabel_NamesTheFieldAndTheListAndAClickOnItFocusesTheField()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#dd-single > label.bit-drp-lbl");
        await Expect(label).ToHaveTextAsync("Single select");

        var labelId = await label.GetAttributeAsync("id");
        var labelledBy = await Trigger("dd-single").GetAttributeAsync("aria-labelledby");
        StringAssert.StartsWith(labelledBy, labelId);

        await label.ClickAsync();

        await Expect(Trigger("dd-single")).ToBeFocusedAsync();
        await Expect(Page.Locator("#single-focusin-count")).ToHaveTextAsync("1");
        await Expect(Trigger("dd-single")).ToHaveAttributeAsync("aria-expanded", "false");

        var callout = await CalloutAsync("dd-single");
        await Expect(ListBox(callout)).ToHaveAttributeAsync("aria-labelledby", labelId!);
    }

    [TestMethod]
    public async Task Single_FocusInAndFocusOut_FireAsTheFocusEntersAndLeaves()
    {
        await OpenAsync(Url);

        await Trigger("dd-single").FocusAsync();
        await Expect(Page.Locator("#single-focusin-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#single-focusout-count")).ToHaveTextAsync("0");

        await Page.Locator("#after-single").FocusAsync();
        await Expect(Page.Locator("#single-focusout-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#single-focusin-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Single_AClickOutsideTheCallout_ClosesItAndReturnsTheFocus()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-single");

        var viewport = Page.ViewportSize!;
        await Page.Mouse.ClickAsync(viewport.Width - 5, viewport.Height - 5);

        await Expect(callout).ToBeHiddenAsync();
        await Expect(await OverlayAsync("dd-single")).ToBeHiddenAsync();
        await Expect(Trigger("dd-single")).ToBeFocusedAsync();
        await Expect(Page.Locator("#single-close-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#single-value")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Multi_PickingItems_TogglesThemFiresTheEventsAndKeepsTheCalloutOpen()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-multi");

        await Expect(ListBox(callout)).ToHaveAttributeAsync("aria-multiselectable", "true");

        var uncheckedBox = await StyleAsync(Option(callout, "Apple").Locator(".bit-drp-chb"), "background-color");

        await Option(callout, "Apple").ClickAsync();
        await Expect(Page.Locator("#multi-values")).ToHaveTextAsync("f-app");
        await Option(callout, "Banana").ClickAsync();

        await Expect(Page.Locator("#multi-values")).ToHaveTextAsync("f-app,f-ban");
        await Expect(Page.Locator("#multi-change-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#multi-change-last")).ToHaveTextAsync("f-app,f-ban");
        await Expect(Page.Locator("#multi-select-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#multi-select-last")).ToHaveTextAsync("Banana");
        await Expect(Page.Locator("#multi-deselect-count")).ToHaveTextAsync("0");

        await Expect(callout).ToBeVisibleAsync();
        await Expect(TextContainer("dd-multi")).ToHaveTextAsync("Apple, Banana");
        await Expect(Option(callout, "Apple")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Option(callout, "Grape")).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(callout.Locator(".bit-drp-iwr.bit-drp-chd")).ToHaveCountAsync(2);

        var checkedBox = await StyleAsync(Option(callout, "Apple").Locator(".bit-drp-chb"), "background-color");
        Assert.AreNotEqual(uncheckedBox, checkedBox, "The check box of a selected item is filled.");

        await Option(callout, "Apple").ClickAsync();

        await Expect(Page.Locator("#multi-values")).ToHaveTextAsync("f-ban");
        await Expect(Page.Locator("#multi-deselect-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#multi-deselect-last")).ToHaveTextAsync("Apple");
        await Expect(Page.Locator("#multi-select-count")).ToHaveTextAsync("3");
        await Expect(Page.Locator("#multi-change-count")).ToHaveTextAsync("3");
        await Expect(TextContainer("dd-multi")).ToHaveTextAsync("Banana");
    }

    [TestMethod]
    public async Task Multi_TheSelection_IsShownInTheOrderItWasMade()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-multi");

        await Option(callout, "Carrot").ClickAsync();
        await Expect(Page.Locator("#multi-values")).ToHaveTextAsync("v-car");
        await Option(callout, "Apple").ClickAsync();

        await Expect(Page.Locator("#multi-values")).ToHaveTextAsync("v-car,f-app");
        await Expect(TextContainer("dd-multi")).ToHaveTextAsync("Carrot, Apple");
    }

    [TestMethod]
    public async Task Multi_ValuesSetByThePage_ShowInTheFieldAndTheNativeSelect()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-multi-set").ClickAsync();

        await Expect(TextContainer("dd-multi")).ToHaveTextAsync("Banana, Lettuce");
        await Expect(HiddenSelect("dd-multi")).ToHaveAttributeAsync("multiple", "");
        await Expect(HiddenSelect("dd-multi").Locator("option")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#multi-change-count")).ToHaveTextAsync("0");

        var callout = await OpenDropdownAsync("dd-multi");
        await Expect(Option(callout, "Banana")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Option(callout, "Lettuce")).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        await Page.Locator("#btn-multi-clear").ClickAsync();

        await Expect(TextContainer("dd-multi")).ToHaveTextAsync("Select items");
        await Expect(HiddenSelect("dd-multi").Locator("option")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkup_DropsThePageActivationHandlers_AndComesBackWhenEnabled()
    {
        await OpenAsync(Url);

        var root = Root("dd-disabled");
        var trigger = Trigger("dd-disabled");

        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(trigger).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(trigger).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(HiddenSelect("dd-disabled")).ToHaveAttributeAsync("disabled", "");
        await Expect(HiddenSelect("dd-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(root.Locator(".bit-drp-clr")).ToHaveCountAsync(0);
        await Expect(TextContainer("dd-disabled")).ToHaveTextAsync("Apple");

        var disabledColor = await StyleAsync(trigger, "color");
        var disabledBackground = await StyleAsync(trigger, "background-color");

        await trigger.ClickAsync(new() { Force = true });
        await trigger.HoverAsync(new() { Force = true });

        await Expect(Page.Locator("#disabled-hovers")).Not.ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-mousedowns")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#disabled-open-count")).ToHaveTextAsync("0");
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(await CalloutAsync("dd-disabled")).ToBeHiddenAsync();

        await Page.Locator("#btn-toggle-disabled").ClickAsync();

        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(trigger).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(trigger).ToHaveAttributeAsync("tabindex", "0");
        await Expect(root.Locator(".bit-drp-clr")).ToHaveCountAsync(1);
        // The colors transition back, so they are waited for rather than read once.
        await Expect(trigger).Not.ToHaveCSSAsync("background-color", disabledBackground);
        await Expect(trigger).Not.ToHaveCSSAsync("color", disabledColor);

        await OpenDropdownAsync("dd-disabled");

        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-mousedowns")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#disabled-open-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ReadOnly_TheCalloutCanBeBrowsedButTheSelectionCannotChange()
    {
        await OpenAsync(Url);

        await Expect(Root("dd-readonly")).ToHaveClassAsync(HasClass("bit-drp-rol"));
        await Expect(Trigger("dd-readonly")).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Trigger("dd-readonly")).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Root("dd-readonly").Locator(".bit-drp-clr")).ToHaveCountAsync(0);

        var callout = await OpenDropdownAsync("dd-readonly");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-drp-rol"));

        await Option(callout, "Banana").ClickAsync();

        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#readonly-value")).ToHaveTextAsync("f-app");
        await Expect(Page.Locator("#readonly-select-count")).ToHaveTextAsync("0");
        await Expect(TextContainer("dd-readonly")).ToHaveTextAsync("Apple");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        // The chips of a read-only dropdown carry no remove button.
        await Expect(Root("dd-readonly-chips").Locator(".bit-drp-chp")).ToHaveTextAsync(["Apple", "Banana"]);
        await Expect(Root("dd-readonly-chips").Locator(".bit-drp-crb")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task IsOpen_IsTwoWayBound_FromThePageAndFromTheDropdown()
    {
        await OpenAsync(Url);

        var callout = await CalloutAsync("dd-isopen");

        await Page.Locator("#btn-open").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Trigger("dd-isopen")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#isopen-state")).ToHaveTextAsync("True");

        // Closing it from the dropdown writes back.
        var viewport = Page.ViewportSize!;
        await Page.Mouse.ClickAsync(viewport.Width - 5, viewport.Height - 5);
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#isopen-state")).ToHaveTextAsync("False");

        await Trigger("dd-isopen").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Page.Locator("#isopen-state")).ToHaveTextAsync("True");

        // The overlay covers the page's button while the list is open.
        await ScriptClickAsync("#btn-close");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Trigger("dd-isopen")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#isopen-state")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Reselectable_PickingTheSelectedItemAgain_FiresOnSelectItemOnlyWhenAsked()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-reselect");
        await Option(callout, "Apple").ClickAsync();
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#reselect-count")).ToHaveTextAsync("1");

        var other = await OpenDropdownAsync("dd-noreselect");
        await Option(other, "Apple").ClickAsync();
        await Expect(other).ToBeHiddenAsync();
        await Expect(Page.Locator("#noreselect-count")).ToHaveTextAsync("0");

        other = await OpenDropdownAsync("dd-noreselect");
        await Option(other, "Banana").ClickAsync();
        await Expect(Page.Locator("#noreselect-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task CloseOnSelect_FalseKeepsASingleSelectOpen_TrueClosesAMultiSelect()
    {
        await OpenAsync(Url);

        var single = await OpenDropdownAsync("dd-cos-single");
        await Option(single, "Apple").ClickAsync();

        await Expect(Page.Locator("#cos-single-value")).ToHaveTextAsync("f-app");
        await Expect(single).ToBeVisibleAsync();
        await Expect(Option(single, "Apple")).ToBeFocusedAsync();

        await Option(single, "Grape").ClickAsync();
        await Expect(Page.Locator("#cos-single-value")).ToHaveTextAsync("f-gra");
        await Expect(single).ToBeVisibleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(single).ToBeHiddenAsync();

        var multi = await OpenDropdownAsync("dd-cos-multi");
        await Option(multi, "Apple").ClickAsync();

        await Expect(Page.Locator("#cos-multi-values")).ToHaveTextAsync("f-app");
        await Expect(multi).ToBeHiddenAsync();
        await Expect(Trigger("dd-cos-multi")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task ValueComparer_AValueDifferingInCase_SelectsTheItem()
    {
        await OpenAsync(Url);

        await Expect(TextContainer("dd-comparer")).ToHaveTextAsync("Apple");

        var callout = await OpenDropdownAsync("dd-comparer");
        await Expect(Option(callout, "Apple")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        await Option(callout, "Banana").ClickAsync();

        await Expect(Page.Locator("#comparer-value")).ToHaveTextAsync("f-ban");
        await Expect(TextContainer("dd-comparer")).ToHaveTextAsync("Banana");
    }

    [TestMethod]
    public async Task ClearButton_ClearsTheSingleSelection_AndFiresOnClear()
    {
        await OpenAsync(Url);

        var clear = Root("dd-clear-single").Locator("button.bit-drp-clr");

        await Expect(clear).ToHaveAttributeAsync("aria-label", "Remove the selection");
        await Expect(clear).ToHaveAttributeAsync("title", "Remove the selection");
        await Expect(clear).ToHaveAttributeAsync("type", "button");

        await clear.ClickAsync();

        await Expect(Page.Locator("#clear-single-value")).ToHaveTextAsync("");
        await Expect(Page.Locator("#clear-single-count")).ToHaveTextAsync("1");
        await Expect(clear).ToHaveCountAsync(0);
        await Expect(Trigger("dd-clear-single")).ToBeFocusedAsync();
        await Expect(Trigger("dd-clear-single")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Root("dd-clear-single")).Not.ToHaveClassAsync(HasClass("bit-drp-hvl"));
    }

    [TestMethod]
    public async Task ClearButton_ClearsTheMultiSelection_AndEnterOnItDoesNotOpenTheList()
    {
        await OpenAsync(Url);

        var clear = Root("dd-clear-multi").Locator("button.bit-drp-clr");
        await Expect(clear).ToHaveAttributeAsync("aria-label", "Clear selection");

        await clear.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#clear-multi-values")).ToHaveTextAsync("");
        await Expect(Page.Locator("#clear-multi-count")).ToHaveTextAsync("1");
        await Expect(Trigger("dd-clear-multi")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(await CalloutAsync("dd-clear-multi")).ToBeHiddenAsync();
        await Expect(Trigger("dd-clear-multi")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task ClearOnEscape_TheFirstEscapeClosesTheList_TheNextOneClears()
    {
        await OpenAsync(Url);

        var trigger = Trigger("dd-clear-esc");

        // A selection to clear: the field claims its Escape from any surface around it.
        await Expect(trigger).ToHaveAttributeAsync("data-bit-esc", "claim");

        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        var callout = await CalloutAsync("dd-clear-esc");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(Page.Locator("#clear-esc-value")).ToHaveTextAsync("f-ban");
        await Expect(Page.Locator("#clear-esc-count")).ToHaveTextAsync("0");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("#clear-esc-value")).ToHaveTextAsync("");
        await Expect(Page.Locator("#clear-esc-count")).ToHaveTextAsync("1");
        await Expect(trigger).Not.ToHaveAttributeAsync("data-bit-esc", "claim");

        // Nothing left to clear: a further Escape does nothing.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#clear-esc-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task ClearButton_OnAOneWayBoundValue_RefusesTheClear()
    {
        await OpenAsync(Url);

        var clear = Root("dd-clear-oneway").Locator("button.bit-drp-clr");

        await clear.ClickAsync();

        await Expect(clear).ToBeVisibleAsync();
        await Expect(TextContainer("dd-clear-oneway")).ToHaveTextAsync("Apple");
        await Expect(Page.Locator("#clear-oneway-count")).ToHaveTextAsync("0");

        // A refused clear moves the focus nowhere: it stays where the click left it - on the button, except in WebKit,
        // where a click does not focus a button at all.
        if (IsWebKit is false)
        {
            await Expect(clear).ToBeFocusedAsync();
        }
    }

    [TestMethod]
    public async Task Methods_SelectItemUnselectItemAndClearAsync_ActLikeTheUser()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-m-select").ClickAsync();
        await Expect(Page.Locator("#m-value")).ToHaveTextAsync("v-car");
        await Expect(TextContainer("dd-methods")).ToHaveTextAsync("Carrot");
        await Expect(Page.Locator("#m-select-count")).ToHaveTextAsync("1");

        // An item already selected is left alone.
        await Page.Locator("#btn-m-select").ClickAsync();
        await Page.Locator("#btn-m-read").ClickAsync();
        await Expect(Page.Locator("#m-selected")).ToHaveTextAsync("Carrot");
        await Expect(Page.Locator("#m-select-count")).ToHaveTextAsync("1");

        // In single select mode unselecting is the clear.
        await Page.Locator("#btn-m-unselect").ClickAsync();
        await Expect(Page.Locator("#m-value")).ToHaveTextAsync("");
        await Expect(Page.Locator("#m-clear-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-m-select").ClickAsync();
        await Expect(Page.Locator("#m-value")).ToHaveTextAsync("v-car");
        await Page.Locator("#btn-m-clear").ClickAsync();
        await Expect(Page.Locator("#m-value")).ToHaveTextAsync("");
        await Expect(Page.Locator("#m-clear-count")).ToHaveTextAsync("2");
        await Page.Locator("#btn-m-read").ClickAsync();
        await Expect(Page.Locator("#m-selected")).ToHaveTextAsync("(none)");

        await Page.Locator("#btn-mm-select").ClickAsync();
        await Expect(Page.Locator("#mm-values")).ToHaveTextAsync("v-bro");
        await Page.Locator("#btn-m-read").ClickAsync();
        await Expect(Page.Locator("#mm-selected")).ToHaveTextAsync("Broccoli");
        await Expect(TextContainer("dd-methods-multi")).ToHaveTextAsync("Broccoli");

        await Page.Locator("#btn-mm-unselect").ClickAsync();
        await Expect(Page.Locator("#mm-values")).ToHaveTextAsync("");
        await Expect(Page.Locator("#mm-deselect-count")).ToHaveTextAsync("1");

        // Unselecting an item that is not selected is a no-op.
        await Page.Locator("#btn-mm-unselect").ClickAsync();
        await Page.Locator("#btn-m-read").ClickAsync();
        await Expect(Page.Locator("#mm-selected")).ToHaveTextAsync("");
        await Expect(Page.Locator("#mm-deselect-count")).ToHaveTextAsync("1");

        // Nothing was opened by any of it.
        await Expect(await CalloutAsync("dd-methods")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Methods_FocusAsync_FocusesTheField_AndRefreshItemsAsyncIsANoOpWithoutAProvider()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-m-focus").ClickAsync();
        await Expect(Trigger("dd-methods")).ToBeFocusedAsync();
        await Expect(Trigger("dd-methods")).ToHaveAttributeAsync("aria-expanded", "false");

        await Page.Locator("#btn-m-refresh").ClickAsync();
        await Expect(Page.Locator("#m-selected")).ToHaveTextAsync("refreshed:");
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Methods_TheItemsCarryTheirIsSelectedFlag()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-m-select").ClickAsync();
        await Expect(Page.Locator("#m-value")).ToHaveTextAsync("v-car");
        await Page.Locator("#btn-mm-select").ClickAsync();
        await Expect(Page.Locator("#mm-values")).ToHaveTextAsync("v-bro");

        await Page.Locator("#btn-m-read").ClickAsync();
        await Expect(Page.Locator("#m-flags")).ToHaveTextAsync("Carrot,Broccoli");

        await Page.Locator("#btn-m-clear").ClickAsync();
        await Expect(Page.Locator("#m-value")).ToHaveTextAsync("");
        await Page.Locator("#btn-m-read").ClickAsync();
        await Expect(Page.Locator("#m-flags")).ToHaveTextAsync("Broccoli");
    }

    [TestMethod]
    public async Task Methods_SearchInputElementAndFocusSearchInputAsync_FollowTheOpenSearchBox()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-ms-read").ClickAsync();
        await Expect(Page.Locator("#ms-element")).ToHaveTextAsync("absent");

        var callout = await OpenDropdownAsync("dd-methods-search");
        var input = callout.Locator("input.bit-drp-sin");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();

        // The open callout's overlay covers the page's buttons.
        await ScriptClickAsync("#btn-ms-read");
        await Expect(Page.Locator("#ms-element")).ToHaveTextAsync("present");

        await ScriptClickAsync("#btn-ms-focus");
        await Expect(input).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Methods_FocusAsyncWithPreventScroll_FocusesTheFieldWithoutScrollingThePage()
    {
        await OpenAsync(Url);

        await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await ScriptClickAsync("#btn-m-focus-noscroll");

        await Expect(Trigger("dd-methods")).ToBeFocusedAsync();
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => window.scrollY"), "The page stays where it was.");

        await Page.Locator("#after-single").FocusAsync();
        await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await ScriptClickAsync("#btn-m-focus");

        await Expect(Trigger("dd-methods")).ToBeFocusedAsync();
        Assert.IsTrue(await Page.EvaluateAsync<double>("() => window.scrollY") > 0, "A plain FocusAsync scrolls the field into view.");
    }

    [TestMethod]
    [DataRow("/e2e/dropdown/autofocus", false)]
    [DataRow("/e2e/dropdown/autofocus?combo=true", true)]
    public async Task AutoFocus_TheDropdownTakesTheFocusOnceItIsInteractive(string path, bool combo)
    {
        await OpenAsync(path);

        var target = combo ? ComboInput("dd-af") : Trigger("dd-af");

        await Expect(target).ToBeFocusedAsync();
        await Expect(target).ToHaveAttributeAsync("role", "combobox");
        await Expect(await CalloutAsync("dd-af")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task OpenOnFocus_TabbingIn_OpensTheList_AndThePickThatClosesItDoesNotReopenIt()
    {
        await OpenAsync(Url);

        await Page.Locator("#before-oof").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        var trigger = Trigger("dd-oof");
        var callout = await CalloutAsync("dd-oof");

        await Expect(trigger).ToBeFocusedAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Apple")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Option(callout, "Banana")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#oof-value")).ToHaveTextAsync("f-ban");
        await Expect(trigger).ToBeFocusedAsync();
        await Expect(callout).ToBeHiddenAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Base_IdClassStyleAriaLabelTabIndexAndAttributes_ReachTheirElements()
    {
        await OpenAsync(Url);

        var root = Root("dd-base");

        await Expect(root).ToHaveClassAsync(HasClass("bit-drp"));
        await Expect(root).ToHaveClassAsync(HasClass("my-dropdown"));
        await Expect(root).ToHaveAttributeAsync("data-test", "base-root");
        await Expect(root).ToHaveCSSAsync("margin-top", "7px");

        var trigger = Trigger("dd-base");
        await Expect(trigger).ToHaveAttributeAsync("aria-label", "Base dropdown");
        await Expect(trigger).Not.ToHaveAttributeAsync("aria-labelledby", new System.Text.RegularExpressions.Regex(".+"));
        await Expect(trigger).ToHaveAttributeAsync("tabindex", "3");

        await Expect(HiddenSelect("dd-base")).ToHaveAttributeAsync("data-input", "base-select");
        await Expect(HiddenSelect("dd-base")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(HiddenSelect("dd-base")).ToHaveAttributeAsync("tabindex", "-1");

        var callout = await CalloutAsync("dd-base");
        await Expect(ListBox(callout)).ToHaveAttributeAsync("aria-label", "Base dropdown");
    }

    [TestMethod]
    public async Task Base_Visibility_HidesOrCollapsesTheRootAtRuntime()
    {
        await OpenAsync(Url);

        var root = Root("dd-base");

        await Expect(root).ToHaveCSSAsync("visibility", "visible");

        await Page.Locator("#btn-vis-hidden").ClickAsync();
        await Expect(root).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await BoxAsync(root)).Height > 0, "A hidden dropdown keeps its room.");

        await Page.Locator("#btn-vis-collapsed").ClickAsync();
        await Expect(root).ToHaveCSSAsync("display", "none");
        await Expect(root).ToBeHiddenAsync();

        await Page.Locator("#btn-vis-visible").ClickAsync();
        await Expect(root).ToBeVisibleAsync();
        await Expect(root).ToHaveCSSAsync("display", "flex");
    }

    [TestMethod]
    public async Task Base_Name_MirrorsTheSelectionIntoANativeFormPost()
    {
        await OpenAsync(Url);

        await Expect(HiddenSelect("dd-form")).ToHaveAttributeAsync("name", "fruit");

        var posted = await Page.EvaluateAsync<string[]>("() => new FormData(document.getElementById('native-form')).getAll('fruit')");

        CollectionAssert.AreEqual(new[] { "f-app", "v-car" }, posted);
    }

    [TestMethod]
    public async Task Params_TheCascade_FillsWhatADropdownLeftUnsetAndNothingElse()
    {
        await OpenAsync(Url);

        var cascaded = Root("dd-params-a");
        var own = Root("dd-params-b");
        var outside = Root("dd-params-outside");

        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-drp-lg"));
        await Expect(cascaded.Locator(".bit-drp-clr")).ToHaveCountAsync(1);
        await Expect(Trigger("dd-params-a")).ToHaveCSSAsync("min-height", "40px");

        await Expect(own).ToHaveClassAsync(HasClass("bit-drp-lg"));
        await Expect(TextContainer("dd-params-b")).ToHaveTextAsync("Own placeholder");
        await Expect(own.Locator(".bit-drp-clr")).ToHaveCountAsync(0);

        await Expect(outside).Not.ToHaveClassAsync(HasClass("bit-drp-lg"));
        await Expect(outside.Locator(".bit-drp-clr")).ToHaveCountAsync(0);
        await Expect(Trigger("dd-params-outside")).ToHaveCSSAsync("min-height", "32px");

        // The cascaded placeholder shows once the cascaded clear button empties the field.
        await cascaded.Locator(".bit-drp-clr").ClickAsync();
        await Expect(TextContainer("dd-params-a")).ToHaveTextAsync("Cascaded placeholder");
    }

    [TestMethod]
    public async Task Lifecycle_ReRendersOnAParameterChange_AndUnmountsCleanlyWhileOpen()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#dd-mount > label")).ToHaveTextAsync("First label");
        await Page.Locator("#btn-rerender").ClickAsync();
        await Expect(Page.Locator("#dd-mount > label")).ToHaveTextAsync("Second label");

        var triggerId = await Trigger("dd-mount").GetAttributeAsync("id");
        var callout = await OpenDropdownAsync("dd-mount");
        await Expect(callout.Locator("input.bit-drp-sin")).ToBeVisibleAsync();

        await ScriptClickAsync("#btn-toggle-mount");

        await Expect(Page.Locator("#mount-state")).ToHaveTextAsync("False");
        await Expect(Root("dd-mount")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"[id='{triggerId}-callout']")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"[id='{triggerId}-overlay']")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mount").ClickAsync();
        await Expect(Root("dd-mount")).ToBeVisibleAsync();

        var reopened = await OpenDropdownAsync("dd-mount");
        await Option(reopened, "Grape").ClickAsync();
        await Expect(TextContainer("dd-mount")).ToHaveTextAsync("Grape");

        await AssertNoServerErrorsAsync();
        Assert.IsFalse(await IsCircuitDeadAsync());
    }
}
