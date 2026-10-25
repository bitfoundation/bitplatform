namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// How BitDropdown presents itself and its selection (<c>/e2e/dropdown/display</c>): Required, Title and the
/// multi select delimiter, Description / AriaDescription / DescriptionTemplate, ErrorMessage / ErrorMessageTemplate /
/// Invalid with their live region, Prefix / Suffix and their templates, FitWidth, NoBorder / Underlined / Transparent,
/// StickyHeaders with MaxHeight, every per-item property (Id, Class, Style, Title, AriaLabel, IsHidden, IconName, Icon,
/// Data), MaxSelectedItems, the select all item, MaxDisplayedItems with its two text formats, HideSelectedItems, the
/// chips with ChipTemplate, and the public <c>--bit-Dropdown-*</c> variables set on an ancestor - reaching the callout
/// in the body too - with an explicit Color, Size and Transparent winning over them.
/// </summary>
[TestClass]
public class BitDropdownDisplayE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/display";

    [TestMethod]
    public async Task Required_MarksTheLabelAndTheField()
    {
        await OpenAsync(Url);

        await Expect(Root("dd-required")).ToHaveClassAsync(HasClass("bit-drp-req"));
        await Expect(Trigger("dd-required")).ToHaveAttributeAsync("aria-required", "true");

        var marker = await Page.Locator("#dd-required .bit-drp-lbl").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        StringAssert.Contains(marker, "*");

        await Expect(Trigger("dd-title")).Not.ToHaveAttributeAsync("aria-required", "true");
    }

    [TestMethod]
    public async Task Title_IsTheTooltipOfTheField_AndTheDelimiterJoinsTheSelection()
    {
        await OpenAsync(Url);

        await Expect(Trigger("dd-title")).ToHaveAttributeAsync("title", "Pick your favorite");
        await Expect(TextContainer("dd-delimiter")).ToHaveTextAsync("Apple - Banana");
    }

    [TestMethod]
    public async Task Description_IsShownUnderTheFieldAndDescribesIt_AlongWithTheHiddenAriaDescription()
    {
        await OpenAsync(Url);

        var description = Page.Locator("#dd-desc .bit-drp-des");
        await Expect(description).ToHaveTextAsync("Only the categories you have access to.");
        await Expect(description).ToBeVisibleAsync();

        var hidden = Page.Locator("#dd-desc .bit-drp-vhd");
        await Expect(hidden).ToHaveTextAsync("Refreshed every morning.");
        var hiddenBox = await BoxAsync(hidden);
        Assert.IsTrue(hiddenBox.Width <= 1 && hiddenBox.Height <= 1, "The aria description is visually hidden.");

        var describedBy = await Trigger("dd-desc").GetAttributeAsync("aria-describedby");
        var ids = describedBy!.Split(' ');
        Assert.AreEqual(2, ids.Length);
        Assert.AreEqual(await description.GetAttributeAsync("id"), ids[0]);
        Assert.AreEqual(await hidden.GetAttributeAsync("id"), ids[1]);

        // The description sits under the field.
        Assert.IsTrue((await BoxAsync(description)).Y >= (await BoxAsync(Trigger("dd-desc"))).Y + (await BoxAsync(Trigger("dd-desc"))).Height - 1);

        var template = Page.Locator("#dd-desc-tpl .bit-drp-des");
        await Expect(template.Locator("#desc-tpl-content")).ToHaveTextAsync("Nothing here is final.");
        await Expect(Trigger("dd-desc-tpl")).ToHaveAttributeAsync("aria-describedby", (await template.GetAttributeAsync("id"))!);
    }

    [TestMethod]
    public async Task ErrorMessage_MarksTheFieldInvalidDescribesItAndIsAnnounced_UntilItIsTakenBack()
    {
        await OpenAsync(Url);

        var root = Root("dd-error");
        var trigger = Trigger("dd-error");
        var message = root.Locator(".bit-drp-erm");

        await Expect(message).ToHaveCountAsync(0);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        var validBorder = await StyleAsync(trigger, "border-top-color");

        await Page.Locator("#btn-set-error").ClickAsync();

        await Expect(message).ToHaveTextAsync("Out of stock");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(trigger).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(root.Locator(".bit-drp-lvr")).ToHaveTextAsync("Out of stock");
        await Expect(root.Locator(".bit-drp-lvr")).ToHaveAttributeAsync("role", "status");
        // The border follows the message color once its transition has run.
        var messageColor = await StyleAsync(message, "color");
        Assert.AreNotEqual(validBorder, messageColor, "The rejection has a color of its own.");
        await Expect(trigger).ToHaveCSSAsync("border-top-color", messageColor);

        var describedBy = (await trigger.GetAttributeAsync("aria-describedby"))!.Split(' ');
        Assert.AreEqual(await message.GetAttributeAsync("id"), describedBy[0], "What is wrong is read first.");
        Assert.AreEqual(await root.Locator(".bit-drp-des").GetAttributeAsync("id"), describedBy[1]);

        await Page.Locator("#btn-clear-error").ClickAsync();

        await Expect(message).ToHaveCountAsync(0);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(trigger).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(root.Locator(".bit-drp-lvr")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task ErrorMessageTemplate_AndInvalid_MarkTheFieldInvalid()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#dd-error-tpl #error-tpl-content")).ToHaveTextAsync("Templated rejection");
        await Expect(Root("dd-error-tpl")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Trigger("dd-error-tpl")).ToHaveAttributeAsync("aria-invalid", "true");
        // The live region announces text only, never a template.
        await Expect(Root("dd-error-tpl").Locator(".bit-drp-lvr")).ToHaveTextAsync("");

        await Expect(Root("dd-invalid")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Trigger("dd-invalid")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("dd-invalid").Locator(".bit-drp-erm")).ToHaveCountAsync(0);

        Assert.AreNotEqual(await StyleAsync(Trigger("dd-valid"), "border-top-color"), await StyleAsync(Trigger("dd-invalid"), "border-top-color"));
    }

    [TestMethod]
    public async Task PrefixAndSuffix_SitAtTheEndsOfTheField_AndTheirTemplatesReplaceThem()
    {
        await OpenAsync(Url);

        var prefix = Page.Locator("#dd-prefix .bit-drp-pre");
        var suffix = Page.Locator("#dd-prefix .bit-drp-suf");

        await Expect(prefix).ToHaveTextAsync("Fruits:");
        await Expect(suffix).ToHaveTextAsync("kg");

        var fieldBox = await BoxAsync(Trigger("dd-prefix"));
        var prefixBox = await BoxAsync(prefix);
        var textBox = await BoxAsync(TextContainer("dd-prefix"));
        var suffixBox = await BoxAsync(suffix);

        Assert.AreEqual(fieldBox.X + 1, prefixBox.X, 1.5, "The prefix starts the field.");
        Assert.IsTrue(prefixBox.X + prefixBox.Width <= textBox.X + 1, "The prefix comes before the text.");
        Assert.IsTrue(suffixBox.X >= textBox.X + textBox.Width - 1, "The suffix comes after the text.");
        Assert.AreEqual(fieldBox.X + fieldBox.Width - 1, suffixBox.X + suffixBox.Width, 1.5, "The suffix ends the field.");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", await StyleAsync(prefix, "background-color"), "The addons are filled.");

        await Expect(Page.Locator("#dd-prefix-tpl .bit-drp-wrp #prefix-tpl-content")).ToHaveTextAsync("PRE");
        await Expect(Page.Locator("#dd-prefix-tpl .bit-drp-wrp #suffix-tpl-content")).ToHaveTextAsync("SUF");
        await Expect(Page.Locator("#dd-prefix-tpl .bit-drp-pre")).ToHaveCountAsync(0);
        Assert.IsTrue((await BoxAsync(Page.Locator("#prefix-tpl-content"))).X < (await BoxAsync(TextContainer("dd-prefix-tpl"))).X);
    }

    [TestMethod]
    public async Task FitWidth_SizesTheDropdownToItsContent()
    {
        await OpenAsync(Url);

        await Expect(Root("dd-fit")).ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("width:fit-content"));

        var fit = await BoxAsync(Root("dd-fit"));
        var full = await BoxAsync(Root("dd-nofit"));

        Assert.AreEqual(600, full.Width, 1);
        Assert.IsTrue(fit.Width < 300, $"A FitWidth dropdown is only as wide as its content, was {fit.Width}px.");
    }

    [TestMethod]
    public async Task Variants_NoBorderUnderlinedAndTransparent_ChangeTheFurnitureOfTheField()
    {
        await OpenAsync(Url);

        var plain = Trigger("dd-plain");
        await Expect(plain).ToHaveCSSAsync("border-top-width", "1px");
        await Expect(plain).ToHaveCSSAsync("border-bottom-width", "1px");
        var plainBackground = await StyleAsync(plain, "background-color");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", plainBackground);
        Assert.AreNotEqual("0px", await StyleAsync(plain, "border-top-left-radius"));

        await Expect(Root("dd-noborder")).ToHaveClassAsync(HasClass("bit-drp-nbd"));
        await Expect(Trigger("dd-noborder")).ToHaveCSSAsync("border-top-style", "none");
        await Expect(Trigger("dd-noborder")).ToHaveCSSAsync("border-bottom-style", "none");

        await Expect(Root("dd-underlined")).ToHaveClassAsync(HasClass("bit-drp-und"));
        await Expect(Trigger("dd-underlined")).ToHaveCSSAsync("border-top-width", "0px");
        await Expect(Trigger("dd-underlined")).ToHaveCSSAsync("border-bottom-width", "1px");
        await Expect(Trigger("dd-underlined")).ToHaveCSSAsync("border-top-left-radius", "0px");

        await Expect(Root("dd-transparent")).ToHaveClassAsync(HasClass("bit-drp-trn"));
        await Expect(Trigger("dd-transparent")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(Trigger("dd-transparent")).ToHaveCSSAsync("border-top-width", "1px");

        // The callout keeps its own surface under every variant.
        var callout = await OpenDropdownAsync("dd-transparent");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", await StyleAsync(callout, "background-color"));
    }

    [TestMethod]
    public async Task StickyHeaders_KeepTheGroupHeaderPinned_WhileMaxHeightCapsTheList()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-sticky");
        var scroller = ListBox(callout);

        Assert.IsTrue(await PxAsync(scroller, "height") <= 200.5, "MaxHeight caps the scrollable list.");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-drp-sth"));

        await scroller.EvaluateAsync("el => el.scrollTop = 150");
        await NextFramesAsync();

        var header = callout.Locator(".bit-drp-ihw", new() { HasTextString = "Group Alpha" });
        await Expect(header).ToHaveCSSAsync("position", "sticky");
        Assert.AreEqual((await BoxAsync(scroller)).Y, (await BoxAsync(header)).Y, 1.5, "The header of the group being scrolled stays at the top.");

        var plain = await OpenPlainAsync();
        var plainScroller = ListBox(plain);
        await plainScroller.EvaluateAsync("el => el.scrollTop = 150");
        await NextFramesAsync();

        var plainHeader = plain.Locator(".bit-drp-ihw", new() { HasTextString = "Group Alpha" });
        Assert.IsTrue((await BoxAsync(plainHeader)).Y < (await BoxAsync(plainScroller)).Y - 50, "A header that is not sticky scrolls away.");
    }

    private async Task<ILocator> OpenPlainAsync()
    {
        await Page.Keyboard.PressAsync("Escape");
        await Expect(await CalloutAsync("dd-sticky")).ToBeHiddenAsync();

        return await OpenDropdownAsync("dd-nosticky");
    }

    [TestMethod]
    public async Task ItemProperties_IdClassStyleTitleAriaLabelHiddenIconsAndData_ReachTheItem()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-itemprops");

        var styled = Option(callout, "Styled aria");
        await Expect(styled).ToHaveAttributeAsync("id", "item-styled");
        await Expect(styled).ToHaveAttributeAsync("title", "Styled title");
        await Expect(styled).ToHaveAttributeAsync("aria-label", "Styled aria");

        var wrapper = styled.Locator("xpath=..");
        await Expect(wrapper).ToHaveClassAsync(HasClass("item-cls"));
        await Expect(wrapper).ToHaveCSSAsync("color", "rgb(255, 0, 0)");

        await Expect(styled).ToHaveAttributeAsync("aria-describedby", "item-group-header");
        await Expect(callout.Locator("#item-group-header")).ToHaveTextAsync("Group");

        // Hidden items and headers take no room and are not counted.
        await Expect(callout.Locator("[role=option]", new() { HasTextString = "Hidden" })).ToBeHiddenAsync();
        await Expect(callout.Locator(".bit-drp-ihd", new() { HasTextString = "Hidden header" })).ToBeHiddenAsync();
        await Expect(styled).ToHaveAttributeAsync("aria-setsize", "4");
        // Items after a hidden header belong to no group.
        await Expect(Option(callout, "Plain")).Not.ToHaveAttributeAsync("aria-describedby", new System.Text.RegularExpressions.Regex(".+"));

        var withIcon = callout.Locator("[role=option]", new() { HasTextString = "With icon" });
        await Expect(withIcon.Locator("i.bit-drp-iic")).ToHaveClassAsync(HasClass("bit-icon--Heart"));
        // The glyph is decoration: hidden from assistive technologies, the item is named by its text alone.
        await Expect(withIcon.Locator("i.bit-drp-iic")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Option(callout, "With icon")).ToHaveCountAsync(1);
        await Expect(callout.Locator("[role=option]", new() { HasTextString = "External icon" }).Locator("i.bit-drp-iic")).ToHaveClassAsync(HasClass("ext-icon-class"));
        await Expect(Option(callout, "Plain").Locator("i.bit-drp-iic")).ToHaveCountAsync(0);

        await styled.ClickAsync();

        await Expect(Page.Locator("#itemprops-value")).ToHaveTextAsync("s");
        await Expect(Page.Locator("#itemprops-data")).ToHaveTextAsync("styled-data");
        await Expect(TextContainer("dd-itemprops")).ToHaveTextAsync("Styled");
    }

    [TestMethod]
    public async Task MaxSelectedItems_DisablesTheRestAtTheLimit_AnnouncesIt_AndFreesThemAgain()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-max");
        var liveRegion = Root("dd-max").Locator(".bit-drp-lvr");

        await Option(callout, "Apple").ClickAsync();
        await Option(callout, "Banana").ClickAsync();

        await Expect(Page.Locator("#max-values")).ToHaveTextAsync("f-app,f-ban");
        await Expect(Option(callout, "Grape")).ToBeDisabledAsync();
        await Expect(Option(callout, "Carrot")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Option(callout, "Apple")).ToBeEnabledAsync();
        await Expect(callout.Locator(".bit-drp-iwr.bit-drp-ids")).ToHaveCountAsync(5);
        await Expect(liveRegion).ToHaveTextAsync("At most 2 picked");

        await Option(callout, "Grape").ClickAsync(new() { Force = true });
        await Expect(Page.Locator("#max-values")).ToHaveTextAsync("f-app,f-ban");

        await Option(callout, "Apple").ClickAsync();

        await Expect(Page.Locator("#max-values")).ToHaveTextAsync("f-ban");
        await Expect(Option(callout, "Grape")).ToBeEnabledAsync();
        await Expect(liveRegion).Not.ToHaveTextAsync("At most 2 picked");
    }

    [TestMethod]
    public async Task SelectAll_SelectsEveryEnabledItem_ShowsAMixedState_AndClearsOnTheNextClick()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-selall");
        var button = callout.Locator("button.bit-drp-sab");

        await Expect(button).ToHaveAttributeAsync("role", "checkbox");
        await Expect(button).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(button).ToHaveTextAsync("Select all");
        await Expect(button).ToHaveAttributeAsync("aria-controls", (await ListBox(callout).GetAttributeAsync("id"))!);
        // It is the first stop of the arrow keys, so a list opened with nothing selected starts on it.
        await Expect(button).ToBeFocusedAsync();

        await button.ClickAsync();

        await Expect(Page.Locator("#selall-values")).ToHaveTextAsync("f-app,f-ban,f-gra,v-bro,v-car,v-let");
        await Expect(Page.Locator("#selall-change-count")).ToHaveTextAsync("1");
        await Expect(button).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Option(callout, "Orange")).ToHaveAttributeAsync("aria-selected", "false");

        await Option(callout, "Apple").ClickAsync();

        await Expect(button).ToHaveAttributeAsync("aria-checked", "mixed");
        await Expect(callout.Locator(".bit-drp-sac")).ToHaveClassAsync(HasClass("bit-drp-sai"));

        await button.ClickAsync();
        await Expect(Page.Locator("#selall-values")).ToHaveTextAsync("f-ban,f-gra,v-bro,v-car,v-let,f-app");
        await Expect(button).ToHaveAttributeAsync("aria-checked", "true");

        await button.ClickAsync();
        await Expect(Page.Locator("#selall-values")).ToHaveTextAsync("");
        await Expect(button).ToHaveAttributeAsync("aria-checked", "false");
    }

    [TestMethod]
    public async Task SelectAll_FollowsTheSearch_TakesItsText_AndGoesAwayWithNothingToSelect()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-selall-text");
        var button = callout.Locator("button.bit-drp-sab");

        await Expect(button).ToHaveTextAsync("Pick everything");

        await callout.Locator("input.bit-drp-sin").FillAsync("an");
        await Expect(Options(callout)).ToHaveCountAsync(2);

        await button.ClickAsync();
        await Expect(Page.Locator("#selall-text-values")).ToHaveTextAsync("f-ban");

        await callout.Locator("input.bit-drp-sin").FillAsync("zzz");
        await Expect(callout.Locator(".bit-drp-emp")).ToHaveTextAsync("No results found");
        await Expect(button).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task SelectAll_StopsAtMaxSelectedItems_AndThenClears()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-selall-max");
        var button = callout.Locator("button.bit-drp-sab");

        await button.ClickAsync();

        await Expect(Page.Locator("#selall-max-values")).ToHaveTextAsync("f-app,f-ban,f-gra");
        await Expect(Option(callout, "Carrot")).ToBeDisabledAsync();
        await Expect(button).ToHaveAttributeAsync("aria-checked", "mixed");

        await button.ClickAsync();
        await Expect(Page.Locator("#selall-max-values")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task MaxDisplayedItems_CollapsesTheRestIntoAnOverflowChip_NamedInItsTitle()
    {
        await OpenAsync(Url);

        var chips = Page.Locator("#dd-maxdisp .bit-drp-chp:not(.bit-drp-ovf)");
        await Expect(chips).ToHaveCountAsync(2);
        await Expect(chips.Nth(0)).ToContainTextAsync("Apple");
        await Expect(chips.Nth(1)).ToContainTextAsync("Banana");

        var overflow = Page.Locator("#dd-maxdisp .bit-drp-ovf");
        await Expect(overflow.Locator("span[aria-hidden=true]")).ToHaveTextAsync("+2");
        await Expect(overflow).ToHaveAttributeAsync("title", "Grape, Carrot");
        await Expect(overflow.Locator(".bit-drp-vhd")).ToHaveTextAsync("Grape, Carrot");
        Assert.AreNotEqual(await StyleAsync(chips.Nth(0), "background-color"), await StyleAsync(overflow, "background-color"), "The overflow chip is painted in the accent.");

        await Expect(Page.Locator("#dd-overflow .bit-drp-ovf span[aria-hidden=true]")).ToHaveTextAsync("and 2 more");
    }

    [TestMethod]
    public async Task SelectedItemsTextFormat_SummarisesTheSelectionPastTheLimit()
    {
        await OpenAsync(Url);

        var summary = Page.Locator("#dd-seltext .bit-drp-tdp > span[aria-hidden=true]");
        await Expect(summary).ToHaveTextAsync("3 picked");
        await Expect(summary).ToHaveAttributeAsync("title", "Apple, Banana, Grape");
        await Expect(Page.Locator("#dd-seltext .bit-drp-tdp .bit-drp-vhd")).ToHaveTextAsync("Apple, Banana, Grape");

        await Expect(Page.Locator("#dd-seltext-default .bit-drp-tdp > span[aria-hidden=true]")).ToHaveTextAsync("3 items selected");

        var callout = await OpenDropdownAsync("dd-seltext-default");
        await Option(callout, "Grape").ClickAsync();

        await Expect(TextContainer("dd-seltext-default")).ToHaveTextAsync("Apple, Banana");
    }

    [TestMethod]
    public async Task HideSelectedItems_TakesThePickedItemsOutOfTheList_AlongWithTheGroupTheyEmptied()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-hidesel");

        await Expect(Options(callout)).ToHaveCountAsync(2);
        await Expect(Option(callout, "Apple")).ToHaveCountAsync(0);
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Fruits", "Vegetables"]);

        await Option(callout, "Banana").ClickAsync();

        await Expect(Page.Locator("#hidesel-values")).ToHaveTextAsync("f-app,f-ban");
        await Expect(Options(callout)).ToHaveCountAsync(1);
        await Expect(callout.Locator(".bit-drp-ihd")).ToHaveTextAsync(["Vegetables"]);
        await Expect(callout.Locator(".bit-drp-sep")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#dd-hidesel .bit-drp-chp")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Chips_RenderTheSelection_AndARemoveButtonTakesOneOutWithTheMouseOrTheKeyboard()
    {
        await OpenAsync(Url);

        var root = Root("dd-chips");
        await Expect(root).ToHaveClassAsync(HasClass("bit-drp-sch"));

        var chips = root.Locator(".bit-drp-chp");
        await Expect(chips).ToHaveTextAsync(["Apple", "Carrot"]);
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", await StyleAsync(chips.First, "background-color"));

        // The field is named after the selection alone, never the remove buttons.
        var labelledBy = (await Trigger("dd-chips").GetAttributeAsync("aria-labelledby"))!.Split(' ');
        await Expect(Page.Locator($"[id='{labelledBy[1]}']")).ToHaveTextAsync("Apple, Carrot");

        var removeApple = root.GetByRole(AriaRole.Button, new() { Name = "Drop Apple", Exact = true });
        await removeApple.ClickAsync();

        await Expect(Page.Locator("#chips-values")).ToHaveTextAsync("v-car");
        await Expect(Page.Locator("#chips-deselected")).ToHaveTextAsync("Apple");
        await Expect(Trigger("dd-chips")).ToBeFocusedAsync();
        await Expect(Trigger("dd-chips")).ToHaveAttributeAsync("aria-expanded", "false");

        var removeCarrot = root.GetByRole(AriaRole.Button, new() { Name = "Drop Carrot", Exact = true });
        await removeCarrot.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator("#chips-values")).ToHaveTextAsync("");
        await Expect(chips).ToHaveCountAsync(0);
        await Expect(Trigger("dd-chips")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task ChipTemplate_DrawsTheChip_AndASingleSelectChipIsRemovableToo()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#dd-chip-tpl .chip-tpl")).ToHaveTextAsync(["[Apple]", "[Broccoli]"]);
        await Expect(Page.Locator("#dd-chip-tpl .bit-drp-crb")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#dd-chip-tpl .bit-drp-crb").First).ToHaveAttributeAsync("aria-label", "Remove Apple");

        await Expect(Page.Locator("#dd-chips-single .bit-drp-chp")).ToHaveTextAsync("Grape");

        await Page.Locator("#dd-chips-single .bit-drp-crb").ClickAsync();

        await Expect(Page.Locator("#chips-single-value")).ToHaveTextAsync("");
        await Expect(Page.Locator("#dd-chips-single .bit-drp-chp")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheFieldAndTheCalloutInTheBody()
    {
        await OpenAsync(Url);

        var trigger = Trigger("dd-vars");

        await Expect(trigger).ToHaveCSSAsync("background-color", "rgb(255, 250, 205)");
        await Expect(trigger).ToHaveCSSAsync("border-top-color", "rgb(0, 0, 255)");
        await Expect(trigger).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(trigger).ToHaveCSSAsync("min-height", "50px");
        await Expect(trigger).ToHaveCSSAsync("color", "rgb(139, 69, 19)");
        await Expect(Page.Locator("#dd-vars .bit-drp-lbl")).ToHaveCSSAsync("color", "rgb(128, 0, 128)");
        await Expect(Page.Locator("#dd-vars .bit-drp-chp")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
        await Expect(Page.Locator("#dd-vars .bit-drp-chp")).ToHaveCSSAsync("border-top-left-radius", "3px");
        await Expect(Trigger("dd-vars-empty")).ToHaveCSSAsync("color", "rgb(0, 128, 128)");

        var callout = await OpenDropdownAsync("dd-vars");

        await Expect(callout).ToHaveCSSAsync("background-color", "rgb(240, 255, 240)");
        await Expect(callout.Locator(".bit-drp-iwr").First).ToHaveCSSAsync("height", "50px");
        await Expect(callout.Locator(".bit-drp-iwr.bit-drp-chd")).ToHaveCSSAsync("background-color", "rgb(255, 215, 0)");
        await Expect(callout.Locator(".bit-drp-iwr.bit-drp-chd .bit-drp-chb")).ToHaveCSSAsync("background-color", "rgb(0, 100, 0)");
        await Expect(callout.Locator(".bit-drp-ihd").First).ToHaveCSSAsync("color", "rgb(255, 0, 255)");
        await Expect(await OverlayAsync("dd-vars")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0.25)");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        Assert.AreNotEqual("rgb(255, 250, 205)", await StyleAsync(Trigger("dd-vars-outside"), "background-color"));
        Assert.AreNotEqual("rgb(255, 192, 203)", await StyleAsync(Page.Locator("#dd-vars-outside .bit-drp-chp"), "background-color"));
    }

    [TestMethod]
    public async Task CssVariables_LoseToAnExplicitColorSizeAndTransparent()
    {
        await OpenAsync(Url);

        await Expect(Root("dd-vars-color")).ToHaveClassAsync(HasClass("bit-drp-err"));
        await Expect(Root("dd-vars-color")).ToHaveClassAsync(HasClass("bit-drp-sm"));
        await Expect(Trigger("dd-vars-color")).ToHaveCSSAsync("min-height", "26px");
        await Expect(Trigger("dd-vars-transparent")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        var callout = await OpenDropdownAsync("dd-vars-color");

        await Expect(callout).ToHaveClassAsync(HasClass("bit-drp-err"));
        await Expect(callout).ToHaveClassAsync(HasClass("bit-drp-sm"));
        await Expect(callout.Locator(".bit-drp-iwr").First).ToHaveCSSAsync("height", "30px");

        var header = await StyleAsync(callout.Locator(".bit-drp-ihd").First, "color");
        var check = await StyleAsync(callout.Locator(".bit-drp-iwr.bit-drp-chd .bit-drp-chb"), "background-color");

        Assert.AreNotEqual("rgb(255, 0, 255)", header, "The Color parameter wins over the header color variable.");
        Assert.AreNotEqual("rgb(0, 100, 0)", check, "The Color parameter wins over the accent variable.");
        Assert.AreEqual(header, check, "Both take the error role.");
    }
}
