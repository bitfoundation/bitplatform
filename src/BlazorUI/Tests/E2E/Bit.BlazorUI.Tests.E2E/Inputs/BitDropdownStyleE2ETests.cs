using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The look of BitDropdown (<c>/e2e/dropdown/style</c>): every BitColor publishing its class on the field and the
/// callout and painting the accent (the check box of a selected item) with its role, every BitSize sizing the field, the
/// search box and the rows, Styles and Classes reaching every part of the field and the callout (the responsive panel's
/// parts are in <see cref="BitDropdownResponsiveE2ETests"/>), and right-to-left layout from Dir - the field, the callout
/// carrying the direction into the body, its alignment, the commit target bar - and from an ancestor's dir.
/// </summary>
[TestClass]
public class BitDropdownStyleE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/style";

    private static async Task<string> CheckFillAsync(ILocator callout) =>
        await StyleAsync(callout.Locator(".bit-drp-iwr.bit-drp-chd .bit-drp-chb"), "background-color");

    [TestMethod]
    [DataRow("Primary", "pri")]
    [DataRow("Secondary", "sec")]
    [DataRow("Tertiary", "ter")]
    [DataRow("Info", "inf")]
    [DataRow("Success", "suc")]
    [DataRow("Warning", "wrn")]
    [DataRow("SevereWarning", "swr")]
    [DataRow("Error", "err")]
    [DataRow("PrimaryBackground", "pbg")]
    [DataRow("SecondaryBackground", "sbg")]
    [DataRow("TertiaryBackground", "tbg")]
    [DataRow("PrimaryForeground", "pfg")]
    [DataRow("SecondaryForeground", "sfg")]
    [DataRow("TertiaryForeground", "tfg")]
    [DataRow("PrimaryBorder", "pbr")]
    [DataRow("SecondaryBorder", "sbr")]
    [DataRow("TertiaryBorder", "tbr")]
    public async Task Color_PublishesItsClassOnTheFieldAndTheCallout_AndPaintsTheAccent(string color, string suffix)
    {
        await OpenAsync(Url);

        var id = $"dd-color-{color}";

        await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-drp-{suffix}"));
        await Expect(Root("dd-color-none")).Not.ToHaveClassAsync(new Regex(@"(^|\s)bit-drp-(pri|sec|ter|inf|suc|wrn|swr|err|pbg|sbg|tbg|pfg|sfg|tfg|pbr|sbr|tbr)(\s|$)"));

        var defaultCallout = await OpenDropdownAsync("dd-color-none");
        var defaultFill = await CheckFillAsync(defaultCallout);
        await Page.Keyboard.PressAsync("Escape");
        await Expect(defaultCallout).ToBeHiddenAsync();

        var callout = await OpenDropdownAsync(id);
        await Expect(callout).ToHaveClassAsync(HasClass($"bit-drp-{suffix}"));

        var fill = await CheckFillAsync(callout);
        var header = await StyleAsync(callout.Locator(".bit-drp-ihd"), "color");

        Assert.AreEqual(fill, header, "The check box and the group header both take the role's main color.");

        if (color == "Primary")
        {
            Assert.AreEqual(defaultFill, fill, "An unset Color stands for Primary.");
        }
        else
        {
            Assert.AreNotEqual(defaultFill, fill, $"{color} paints the accent in a color of its own.");
        }
    }

    [TestMethod]
    [DataRow("Small", "sm", 26, 30)]
    [DataRow("Medium", "md", 32, 36)]
    [DataRow("Large", "lg", 40, 44)]
    public async Task Size_SizesTheFieldTheSearchBoxAndTheRows(string size, string suffix, int fieldHeight, int rowHeight)
    {
        await OpenAsync(Url);

        var id = $"dd-size-{size}";

        await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-drp-{suffix}"));
        await Expect(Trigger(id)).ToHaveCSSAsync("min-height", $"{fieldHeight}px");

        var callout = await OpenDropdownAsync(id);

        await Expect(callout).ToHaveClassAsync(HasClass($"bit-drp-{suffix}"));
        await Expect(Option(callout, "Apple")).ToHaveCSSAsync("height", $"{rowHeight}px");
        await Expect(callout.Locator(".bit-drp-sb")).ToHaveCSSAsync("height", $"{fieldHeight}px");

        var font = await PxAsync(TextContainer(id), "font-size");
        var defaultFont = await PxAsync(TextContainer("dd-size-none"), "font-size");

        if (size == "Medium") Assert.AreEqual(defaultFont, font, "An unset Size stands for Medium.");
        if (size == "Small") Assert.IsTrue(font < defaultFont);
        if (size == "Large") Assert.IsTrue(font > defaultFont);
    }

    [TestMethod]
    public async Task Size_Unset_IsTheMediumOne()
    {
        await OpenAsync(Url);

        await Expect(Root("dd-size-none")).Not.ToHaveClassAsync(new Regex(@"(^|\s)bit-drp-(sm|md|lg)(\s|$)"));
        await Expect(Trigger("dd-size-none")).ToHaveCSSAsync("min-height", "32px");

        var callout = await OpenDropdownAsync("dd-size-none");
        await Expect(Option(callout, "Apple")).ToHaveCSSAsync("height", "36px");
    }

    private static async Task AssertPartsAsync(ILocator scope, params string[] parts)
    {
        foreach (var part in parts)
        {
            var elements = scope.Locator($".c-{part}");

            await Expect(elements.First).ToBeAttachedAsync();
            await Expect(elements.First).ToHaveAttributeAsync("style", new Regex($"--part-{part}: 1"));
        }
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachEveryPartOfTheField()
    {
        await OpenAsync(Url);

        await AssertPartsAsync(Root("dd-sc-multi"),
            "Label", "Container", "TextContainer", "PrefixContainer", "Prefix", "SuffixContainer", "Suffix",
            "Chips", "OverflowChip", "ChipsRemoveButton", "ChipsRemoveIcon", "ClearButton", "CaretDownIcon",
            "DescriptionContainer", "Description", "ErrorMessageContainer", "ErrorMessage");

        await Expect(Root("dd-sc-multi")).ToHaveClassAsync(HasClass("c-Root"));
        await Expect(Root("dd-sc-multi")).ToHaveClassAsync(HasClass("bit-drp"));
        await Expect(Root("dd-sc-multi")).ToHaveAttributeAsync("style", new Regex("--part-Root: 1"));

        var overlay = await OverlayAsync("dd-sc-multi");
        await Expect(overlay).ToHaveClassAsync(HasClass("c-Overlay"));
        await Expect(overlay).ToHaveAttributeAsync("style", new Regex("--part-Overlay: 1"));
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachEveryPartOfTheCallout()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-sc-multi");

        await Expect(callout).ToHaveClassAsync(HasClass("c-Callout"));
        await Expect(callout).ToHaveAttributeAsync("style", new Regex("--part-Callout: 1"));

        await AssertPartsAsync(callout,
            "CalloutHeader", "CalloutFooter", "SearchBoxContainer", "SearchBoxIconContainer", "SearchBoxIcon",
            "SearchBoxInput", "SelectAllContainer", "SelectAllButton", "SelectAllCheckBox", "SelectAllCheckIcon",
            "SelectAllText", "ScrollContainer", "ItemHeader", "ItemWrapper", "ItemButton", "ItemCheckBox",
            "ItemCheckIcon", "ItemIcon", "ItemText", "ItemDivider");

        await callout.Locator("input.bit-drp-sin").FillAsync("pp");

        await AssertPartsAsync(callout, "ItemHighlight", "SearchBoxClearButtonContainer", "SearchBoxClearButton", "SearchBoxClearIcon");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheSingleSelectEmptyLoadingAndComboParts()
    {
        await OpenAsync(Url);

        var single = await OpenDropdownAsync("dd-sc-single");
        await AssertPartsAsync(single, "ItemButton", "ItemText", "ItemHeader", "ItemDivider", "ItemIcon");
        await single.Locator("input.bit-drp-sin").FillAsync("ap");
        await AssertPartsAsync(single, "SearchHint");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(single).ToBeHiddenAsync();

        var empty = await OpenDropdownAsync("dd-sc-empty");
        await AssertPartsAsync(empty, "EmptyContent");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(empty).ToBeHiddenAsync();

        await AssertPartsAsync(Root("dd-sc-loading"), "Spinner");
        var loading = await OpenDropdownAsync("dd-sc-loading");
        await AssertPartsAsync(loading, "LoadingContainer", "LoadingText", "Spinner");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(loading).ToBeHiddenAsync();

        await AssertPartsAsync(Root("dd-sc-combo"), "ComboBoxInput");
        var input = ComboInput("dd-sc-combo");
        await input.FocusAsync();
        await input.FillAsync("Kiwi");
        await AssertPartsAsync(await CalloutAsync("dd-sc-combo"), "DynamicItem");
    }

    [TestMethod]
    public async Task DirRtl_MirrorsTheField()
    {
        await OpenAsync(Url);

        await Expect(Root("dd-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("dd-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("dd-ltr")).Not.ToHaveClassAsync(HasClass("bit-rtl"));

        var rtlCaret = await BoxAsync(Page.Locator("#dd-rtl .bit-drp-wrp > .bit-drp-icn"));
        var rtlText = await BoxAsync(TextContainer("dd-rtl"));
        Assert.IsTrue(rtlCaret.X < rtlText.X, "The chevron sits at the left end of an RTL field.");

        var ltrCaret = await BoxAsync(Page.Locator("#dd-ltr .bit-drp-wrp > .bit-drp-icn"));
        var ltrText = await BoxAsync(TextContainer("dd-ltr"));
        Assert.IsTrue(ltrCaret.X > ltrText.X, "The chevron sits at the right end of an LTR field.");

        var root = await BoxAsync(Root("dd-rtl"));
        var labelText = await Page.Locator("#dd-rtl > label").EvaluateAsync<double>("el => { const r = document.createRange(); r.selectNodeContents(el); return r.getBoundingClientRect().right; }");
        Assert.AreEqual(root.X + root.Width, labelText, 2, "The label starts at the right edge.");
    }

    [TestMethod]
    public async Task DirRtl_TheCalloutCarriesTheDirectionIntoTheBody()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-rtl");

        await Expect(callout).ToHaveAttributeAsync("dir", "rtl");
        await Expect(callout).ToHaveClassAsync(HasClass("bit-drp-rtl"));

        var box = await BoxAsync(Option(callout, "Banana").Locator(".bit-drp-chb"));
        var text = await BoxAsync(Option(callout, "Banana").Locator(".bit-drp-itx"));
        Assert.IsTrue(box.X > text.X, "The check box sits on the right of the text.");

        var searchIcon = await BoxAsync(callout.Locator(".bit-drp-sic"));
        var searchInput = await BoxAsync(callout.Locator(".bit-drp-sin"));
        Assert.IsTrue(searchIcon.X > searchInput.X, "The search glyph sits at the right end of the search box.");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        var ltr = await OpenDropdownAsync("dd-ltr");
        var ltrBox = await BoxAsync(Option(ltr, "Banana").Locator(".bit-drp-chb"));
        var ltrText = await BoxAsync(Option(ltr, "Banana").Locator(".bit-drp-itx"));
        Assert.IsTrue(ltrBox.X < ltrText.X, "The check box sits on the left of the text in LTR.");
    }

    [TestMethod]
    public async Task DirRtl_ACalloutOfItsOwnWidth_LinesUpWithTheEndOfTheField()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-rtl-preserve");
        await WaitForAnimationsAsync(callout);

        var field = await BoxAsync(Trigger("dd-rtl-preserve"));
        var box = await BoxAsync(callout);

        Assert.IsTrue(box.Width < field.Width - 100, "The callout keeps its own width.");
        Assert.AreEqual(field.X + field.Width, box.X + box.Width, 1.5, "The callout ends where the RTL field starts.");
    }

    [TestMethod]
    public async Task DirRtl_TheCommitTargetBar_MovesToTheStartEdge()
    {
        await OpenAsync(Url);

        async Task<string> TargetShadowAsync(string id)
        {
            var input = ComboInput(id);
            await input.FocusAsync();
            await input.FillAsync("apple");

            var callout = await CalloutAsync(id);
            var target = Option(callout, "Apple");
            await Expect(target).ToHaveClassAsync(HasClass("bit-drp-ctg"));

            var shadow = await StyleAsync(target, "box-shadow");

            await Page.Keyboard.PressAsync("Escape");
            await Expect(callout).ToBeHiddenAsync();

            return shadow;
        }

        var ltr = await TargetShadowAsync("dd-ltr-combo");
        var rtl = await TargetShadowAsync("dd-rtl-combo");

        Assert.AreNotEqual(ltr, rtl);
        StringAssert.Contains(rtl, "-");
        Assert.IsFalse(Regex.IsMatch(ltr, @"inset\s+-|\)\s+-\d|\s-\d+px\s+0px\s+0px\s+0px\s+inset"), $"The LTR bar is drawn on the left edge: {ltr}");
    }

    [TestMethod]
    public async Task InheritedRtl_FromAnAncestor_LaysOutTheFieldAndTheRelocatedCallout()
    {
        await OpenAsync(Url);

        await Expect(Root("dd-rtl-inherit")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("dd-rtl-inherit")).Not.ToHaveAttributeAsync("dir", new Regex(".+"));

        var caret = await BoxAsync(Page.Locator("#dd-rtl-inherit .bit-drp-wrp > .bit-drp-icn"));
        var text = await BoxAsync(TextContainer("dd-rtl-inherit"));
        Assert.IsTrue(caret.X < text.X, "An inherited RTL lays the field out right to left.");

        var callout = await OpenDropdownAsync("dd-rtl-inherit");

        Assert.AreEqual("rtl", await StyleAsync(callout, "direction"), "The callout moved to the body keeps the page's direction.");
        await Expect(callout).Not.ToHaveClassAsync(HasClass("bit-drp-rtl"));

        var box = await BoxAsync(Option(callout, "Banana").Locator(".bit-drp-chb"));
        var itemText = await BoxAsync(Option(callout, "Banana").Locator(".bit-drp-itx"));
        Assert.IsTrue(box.X > itemText.X, "The check box sits on the right of the text.");
    }
}
