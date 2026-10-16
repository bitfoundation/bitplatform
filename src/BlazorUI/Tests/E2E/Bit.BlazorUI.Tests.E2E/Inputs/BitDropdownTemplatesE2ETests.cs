namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The templates and icons of BitDropdown (<c>/e2e/dropdown/templates</c>): ItemTemplate (with the check box kept in
/// multi select mode), HeaderTemplate (its wrapper carrying the id the items point at, and the hidden state),
/// TextTemplate, PlaceholderTemplate, LabelTemplate, CaretDownTemplate, the default glyphs, every ...IconName
/// (CaretDown, ClearButton, ItemCheck, SearchBox, SearchBoxClear, ChipsRemove) and every ...Icon taking a BitIconInfo and
/// winning over its name.
/// </summary>
[TestClass]
public class BitDropdownTemplatesE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/templates";

    [TestMethod]
    public async Task ItemAndHeaderTemplates_DrawTheList_AndTheItemsStillPointAtTheirHeader()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-tpl");

        await Expect(callout.Locator("[role=option] .item-tpl")).ToHaveTextAsync(["» Item a (memo)", "» Item b (print)", "» Item c (cart)"]);
        await Expect(callout.Locator(".header-tpl")).ToHaveTextAsync(["## Items", "## More items"]);
        await Expect(callout.Locator(".bit-drp-itx")).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveCountAsync(0);

        var itemC = Option(callout, "» Item c (cart)");
        var groupId = await itemC.GetAttributeAsync("aria-describedby");
        Assert.IsFalse(string.IsNullOrEmpty(groupId));

        var header = callout.Locator($"[id='{groupId}']");
        await Expect(header).ToHaveClassAsync(HasClass("bit-drp-ihw"));
        await Expect(header).ToHaveTextAsync("## More items");
    }

    [TestMethod]
    public async Task TextAndLabelTemplates_DrawTheSelectionAndTheLabel()
    {
        await OpenAsync(Url);

        var label = Page.Locator("#dd-tpl > label");
        await Expect(label.Locator("#lbl-tpl")).ToHaveTextAsync("Custom label");
        StringAssert.StartsWith(await Trigger("dd-tpl").GetAttributeAsync("aria-labelledby"), await label.GetAttributeAsync("id"));
        await Expect(Page.Locator("#text-tpl")).ToHaveCountAsync(0);

        var callout = await OpenDropdownAsync("dd-tpl");
        await Option(callout, "» Item b (print)").ClickAsync();

        await Expect(Page.Locator("#tpl-value")).ToHaveTextAsync("B");
        await Expect(Page.Locator("#dd-tpl #text-tpl")).ToHaveTextAsync("Picked: Item b");
    }

    [TestMethod]
    public async Task CaretDownTemplate_ReplacesTheChevron_AndPlaceholderTemplateTheEmptyText()
    {
        await OpenAsync(Url);

        var icon = Page.Locator("#dd-tpl .bit-drp-wrp > .bit-drp-icn");
        await Expect(icon.Locator("#caret-tpl")).ToHaveTextAsync("v");
        await Expect(icon.Locator("i")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#dd-tpl-ph #ph-tpl")).ToHaveTextAsync("Nothing yet, pick one");
    }

    [TestMethod]
    public async Task ItemTemplate_InMultiSelectMode_KeepsTheCheckBox()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-tpl-multi");
        var item = Option(callout, "» Item a");

        await Expect(item.Locator(".bit-drp-chb")).ToHaveCountAsync(1);
        await Expect(item.Locator(".item-tpl")).ToHaveTextAsync("» Item a");

        await item.ClickAsync();

        await Expect(Page.Locator("#tpl-multi-values")).ToHaveTextAsync("A");
        await Expect(Page.Locator("#dd-tpl-multi #text-tpl-multi")).ToHaveTextAsync("1 picked");
        await Expect(item).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task HeaderTemplate_OfAHiddenHeader_IsHiddenAndNamesNothing()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-tpl-hidden-header");

        await Expect(callout.Locator(".header-tpl", new() { HasTextString = "## Hidden" })).ToBeHiddenAsync();
        await Expect(callout.Locator(".header-tpl", new() { HasTextString = "## Shown" })).ToBeVisibleAsync();
        await Expect(callout.Locator("#shown-header")).ToHaveTextAsync("## Shown");
        await Expect(Option(callout, "First")).ToHaveAttributeAsync("aria-describedby", "shown-header");
        await Expect(Option(callout, "Second")).Not.ToHaveAttributeAsync("aria-describedby", new System.Text.RegularExpressions.Regex(".+"));
    }

    [TestMethod]
    public async Task DefaultIcons_AreTheFluentGlyphs_HiddenFromAssistiveTechnologies()
    {
        await OpenAsync(Url);

        var caret = Page.Locator("#dd-ico-default .bit-drp-wrp > .bit-drp-icn:not(.bit-drp-clr) i");
        await Expect(caret).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(caret).ToHaveClassAsync(HasClass("bit-ico-r90"));
        await Expect(caret).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.AreNotEqual("none", await StyleAsync(caret, "transform"), "The chevron is turned to point down.");

        await Expect(Page.Locator("#dd-ico-default .bit-drp-clr i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
        await Expect(Page.Locator("#dd-ico-default .bit-drp-crb i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        var callout = await OpenDropdownAsync("dd-ico-default");

        await Expect(callout.Locator(".bit-drp-sic i")).ToHaveClassAsync(HasClass("bit-icon--Search"));
        await Expect(callout.Locator(".bit-drp-sab .bit-drp-chm")).ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(Option(callout, "Apple").Locator(".bit-drp-chm")).ToHaveClassAsync(HasClass("bit-icon--Accept"));

        await callout.Locator("input.bit-drp-sin").FillAsync("a");
        await Expect(callout.Locator(".bit-drp-sbc i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
    }

    [TestMethod]
    public async Task IconNames_ReplaceEveryGlyph()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#dd-ico-names .bit-drp-wrp > .bit-drp-icn:not(.bit-drp-clr) i")).ToHaveClassAsync(HasClass("bit-icon--ChevronDownMed"));
        await Expect(Page.Locator("#dd-ico-names .bit-drp-clr i")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));
        await Expect(Page.Locator("#dd-ico-names .bit-drp-crb i")).ToHaveClassAsync(HasClass("bit-icon--Delete"));

        var callout = await OpenDropdownAsync("dd-ico-names");

        await Expect(callout.Locator(".bit-drp-sic i")).ToHaveClassAsync(HasClass("bit-icon--Filter"));
        await Expect(callout.Locator(".bit-drp-sab .bit-drp-chm")).ToHaveClassAsync(HasClass("bit-icon--CheckMark"));
        await Expect(Option(callout, "Banana").Locator(".bit-drp-chm")).ToHaveClassAsync(HasClass("bit-icon--CheckMark"));

        await callout.Locator("input.bit-drp-sin").FillAsync("a");
        await Expect(callout.Locator(".bit-drp-sbc i")).ToHaveClassAsync(HasClass("bit-icon--EraseTool"));
    }

    [TestMethod]
    public async Task ExternalIcons_TakeABitIconInfo_AndWinOverTheirName()
    {
        await OpenAsync(Url);

        var caret = Page.Locator("#dd-ico-ext .bit-drp-wrp > .bit-drp-icn:not(.bit-drp-clr) i");
        await Expect(caret).ToHaveClassAsync(HasClass("ext-caret"));
        await Expect(caret).Not.ToHaveClassAsync(HasClass("bit-icon--ChevronDownMed"));

        await Expect(Page.Locator("#dd-ico-ext .bit-drp-clr i")).ToHaveClassAsync(HasClass("ext-clear"));
        await Expect(Page.Locator("#dd-ico-ext .bit-drp-crb i")).ToHaveClassAsync(HasClass("ext-chip-remove"));

        var callout = await OpenDropdownAsync("dd-ico-ext");

        await Expect(callout.Locator(".bit-drp-sic i")).ToHaveClassAsync(HasClass("ext-search"));
        await Expect(callout.Locator(".bit-drp-sab .bit-drp-chm")).ToHaveClassAsync(HasClass("ext-check"));
        await Expect(Option(callout, "Grape").Locator(".bit-drp-chm")).ToHaveClassAsync(HasClass("ext-check"));

        await callout.Locator("input.bit-drp-sin").FillAsync("a");
        await Expect(callout.Locator(".bit-drp-sbc i")).ToHaveClassAsync(HasClass("ext-search-clear"));
    }
}
