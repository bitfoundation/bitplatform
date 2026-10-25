using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// Drives BitLabel (/e2e/label) in a real browser: the native label binding (For and a wrapped control focus and name
/// the control), the required and optional marks with their texts and templates, Element (a group caption on another
/// tag, the invalid fallback), NoWrap, NoSelect, VisuallyHidden and Visibility, every Color and Size, the public
/// --bit-Label-* variables and the parameters winning over them, the BitParams cascade, Style/Class/Styles/Classes,
/// the splatted attributes, the focus ring of a label given a TabIndex, RTL (explicit and inherited), the prerender
/// and the runtime re-renders and unmount.
/// </summary>
[TestClass]
public class BitLabelE2ETests : E2ETestBase
{
    private const string Path = "/e2e/label";

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheLabelBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Path}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<label id=\"lbl-basic\" class=\"bit-lbl\">");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Path);
        await Expect(Page.Locator("#lbl-basic")).ToHaveTextAsync("I'm a Label");
    }

    [TestMethod]
    public async Task Basic_RendersANativeLabel_AtTheFieldLabelSizeAndWeight()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-basic");
        Assert.AreEqual("LABEL", await label.EvaluateAsync<string>("el => el.tagName"));
        await Expect(label).ToHaveClassAsync(HasClass("bit-lbl"));
        await Expect(label).ToHaveCSSAsync("display", "block");
        await Expect(label).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-tpg-fs-sm)"));
        await Expect(label).ToHaveCSSAsync("font-weight", await ResolveAsync("font-weight", "var(--bit-tpg-field-label-font-weight)"));
    }

    [TestMethod]
    public async Task Disabled_DimsTheCaption_AndStillNamesItsControl()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-disabled");
        await Expect(label).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(label).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-dis)"));
        await Expect(label).ToHaveCSSAsync("cursor", "default");

        await label.ClickAsync();

        await Expect(Page.Locator("#disabled-input")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task For_ClickingTheCaption_FocusesTheNamedControl()
    {
        await OpenAsync(Path);

        await Page.Locator("#lbl-for").ClickAsync();

        await Expect(Page.Locator("#for-input")).ToBeFocusedAsync();
        await Expect(Page.GetByLabel("A Label for an input", new() { Exact = true })).ToHaveIdAsync("for-input");
    }

    [TestMethod]
    public async Task Wrapping_ClickingTheCaption_TogglesTheWrappedControl()
    {
        await OpenAsync(Path);

        var checkbox = Page.Locator("#wrap-checkbox");
        await Expect(checkbox).Not.ToBeCheckedAsync();

        await Page.Locator("#wrap-caption").ClickAsync();
        await Expect(checkbox).ToBeCheckedAsync();

        await Page.Locator("#wrap-caption").ClickAsync();
        await Expect(checkbox).Not.ToBeCheckedAsync();

        await Expect(Page.GetByLabel("A Label wrapping its own control")).ToHaveIdAsync("wrap-checkbox");
    }

    [TestMethod]
    public async Task Required_RendersAnAsteriskHiddenFromScreenReaders_InTheRequiredColor()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-required");
        var mark = label.Locator(".bit-lbl-rqi");

        await Expect(label).ToHaveClassAsync(HasClass("bit-lbl-req"));
        await Expect(mark).ToHaveTextAsync("*");
        await Expect(mark).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(mark).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-req)"));
        Assert.IsTrue(await PixelsAsync(mark, "margin-left") > 0, "The mark keeps a gap after the caption.");

        // The asterisk is a decoration: the control's name is the caption alone.
        await Expect(Page.GetByRole(AriaRole.Textbox, new() { Name = "Email", Exact = true })).ToHaveIdAsync("required-input");
    }

    [TestMethod]
    public async Task RequiredText_ReplacesTheAsterisk_AndIsSpoken()
    {
        await OpenAsync(Path);

        var mark = Page.Locator("#lbl-required-text .bit-lbl-rqi");

        await Expect(mark).ToHaveTextAsync("(required)");
        await Expect(mark).Not.ToHaveAttributeAsync("aria-hidden", new Regex(".*"));
    }

    [TestMethod]
    public async Task RequiredTemplate_RendersItsContentAsTheMark()
    {
        await OpenAsync(Path);

        var mark = Page.Locator("#lbl-required-template .bit-lbl-rqi");

        await Expect(mark.Locator("#required-template-content")).ToHaveTextAsync("must");
        await Expect(mark).Not.ToHaveAttributeAsync("aria-hidden", new Regex(".*"));
    }

    [TestMethod]
    public async Task RequiredAndDisabled_TheMarkDimsWithTheCaption()
    {
        await OpenAsync(Path);

        var disabledColor = await ResolveAsync("color", "var(--bit-clr-fg-dis)");

        await Expect(Page.Locator("#lbl-required-disabled")).ToHaveCSSAsync("color", disabledColor);
        await Expect(Page.Locator("#lbl-required-disabled .bit-lbl-rqi")).ToHaveCSSAsync("color", disabledColor);
    }

    [TestMethod]
    public async Task Optional_RendersASpokenOptionalText_InARegularWeight()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-optional");
        var mark = label.Locator(".bit-lbl-opi");

        await Expect(label).ToHaveClassAsync(HasClass("bit-lbl-opt"));
        await Expect(mark).ToHaveTextAsync("(optional)");
        await Expect(mark).Not.ToHaveAttributeAsync("aria-hidden", new Regex(".*"));
        await Expect(mark).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-sec)"));
        await Expect(mark).ToHaveCSSAsync("font-weight", await ResolveAsync("font-weight", "var(--bit-tpg-fw-regular)"));
    }

    [TestMethod]
    public async Task OptionalTextAndTemplate_ReplaceTheOptionalText()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lbl-optional-text .bit-lbl-opi")).ToHaveTextAsync("- if you have one");
        await Expect(Page.Locator("#lbl-optional-template .bit-lbl-opi #optional-template-content")).ToHaveTextAsync("maybe");
    }

    [TestMethod]
    public async Task RequiredAndOptional_OnlyTheRequiredMarkIsRendered()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-both");

        await Expect(label).ToHaveClassAsync(HasClass("bit-lbl-req"));
        await Expect(label).Not.ToHaveClassAsync(HasClass("bit-lbl-opt"));
        await Expect(label.Locator(".bit-lbl-rqi")).ToHaveCountAsync(1);
        await Expect(label.Locator(".bit-lbl-opi")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Element_Div_RendersAGroupCaptionWithTheLabelsLook_AndDropsFor()
    {
        await OpenAsync(Path);

        var caption = Page.Locator("#lbl-group");

        Assert.AreEqual("DIV", await caption.EvaluateAsync<string>("el => el.tagName"));
        await Expect(caption).Not.ToHaveAttributeAsync("for", new Regex(".*"));
        await Expect(caption).ToHaveClassAsync(HasClass("bit-lbl"));
        await Expect(caption).ToHaveCSSAsync("font-weight", await ComputedStyleAsync("#lbl-basic", "font-weight"));
        await Expect(caption).ToHaveCSSAsync("font-size", await ComputedStyleAsync("#lbl-basic", "font-size"));

        // The radio group is named by it through aria-labelledby, the hidden asterisk left out.
        await Expect(Page.GetByRole(AriaRole.Radiogroup, new() { Name = "Favorite color", Exact = true })).ToHaveIdAsync("radio-group");
    }

    [TestMethod]
    public async Task Element_Legend_CaptionsItsFieldset()
    {
        await OpenAsync(Path);

        var legend = Page.Locator("#lbl-legend");

        Assert.AreEqual("LEGEND", await legend.EvaluateAsync<string>("el => el.tagName"));
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Delivery notes" })).ToHaveIdAsync("fieldset");
    }

    [TestMethod]
    public async Task Element_InvalidOrPadded_FallsBackToALabelOrIsTrimmed()
    {
        await OpenAsync(Path);

        var invalid = Page.Locator("#lbl-invalid");
        Assert.AreEqual("LABEL", await invalid.EvaluateAsync<string>("el => el.tagName"));
        await Expect(invalid).ToHaveAttributeAsync("for", "invalid-input");

        await invalid.ClickAsync();
        await Expect(Page.Locator("#invalid-input")).ToBeFocusedAsync();

        Assert.AreEqual("SPAN", await Page.Locator("#lbl-span").EvaluateAsync<string>("el => el.tagName"));
    }

    [TestMethod]
    public async Task NoWrap_KeepsTheCaptionOnOneLine_AndTruncatesItButNeverTheMark()
    {
        await OpenAsync(Path);

        var wrapping = await Page.Locator("#lbl-wrapping").BoundingBoxAsync();
        var noWrap = Page.Locator("#lbl-nowrap");
        var noWrapBox = await noWrap.BoundingBoxAsync();

        Assert.IsTrue(wrapping!.Height > noWrapBox!.Height * 1.5, $"The wrapping label ({wrapping.Height}px) is several lines, the NoWrap one ({noWrapBox.Height}px) one.");

        await Expect(noWrap).ToHaveClassAsync(HasClass("bit-lbl-nwr"));
        await Expect(noWrap).ToHaveCSSAsync("white-space", "nowrap");

        var text = noWrap.Locator(".bit-lbl-row > .bit-lbl-txt");
        await Expect(text).ToHaveCSSAsync("text-overflow", "ellipsis");
        Assert.IsTrue(await text.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The caption is cut.");

        // The mark keeps its size and stays inside the label's box.
        var mark = await noWrap.Locator(".bit-lbl-row > .bit-lbl-rqi").BoundingBoxAsync();
        Assert.IsTrue(mark!.Width > 0);
        Assert.IsTrue(mark.X + mark.Width <= noWrapBox.X + noWrapBox.Width + 0.5, "The required mark stays in sight.");
    }

    [TestMethod]
    public async Task NoSelect_DoubleClickingTheCaption_SelectsNothing()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lbl-noselect")).ToHaveClassAsync(HasClass("bit-lbl-nsl"));
        await Expect(Page.Locator("#lbl-noselect")).ToHaveCSSAsync("user-select", "none");

        await Page.Locator("#selectable-caption").DblClickAsync();
        Assert.AreNotEqual("", (await SelectionAsync()).Trim(), "A double click selects a word of a plain label.");

        await Page.EvaluateAsync("() => getSelection().removeAllRanges()");

        await Page.Locator("#noselect-caption").DblClickAsync();
        Assert.AreEqual("", (await SelectionAsync()).Trim(), "A NoSelect label's caption is never selected.");
    }

    [TestMethod]
    public async Task VisuallyHidden_TakesTheLabelOffThePage_AndKeepsNamingItsControl()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-vh");
        var box = await label.BoundingBoxAsync();

        await Expect(label).ToHaveClassAsync(HasClass("bit-lbl-vhd"));
        Assert.IsTrue(box!.Width <= 1 && box.Height <= 1, $"The label is a clipped pixel ({box.Width}x{box.Height}).");
        await Expect(label).ToHaveCSSAsync("position", "absolute");
        await Expect(Page.GetByLabel("Search the documentation")).ToHaveIdAsync("vh-input");
    }

    [TestMethod]
    public async Task VisuallyHidden_ALabelWrappingItsControl_ComesBackWhileTheControlHasTheFocus()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-vh-wrap");

        Assert.IsTrue((await label.BoundingBoxAsync())!.Width <= 1);

        await Page.Locator("#vh-checkbox").FocusAsync();
        await Expect(label).ToHaveCSSAsync("position", "static");
        Assert.IsTrue((await label.BoundingBoxAsync())!.Width > 50, "The label shows while its checkbox has the focus.");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-vh")).ToBeFocusedAsync();
        await Expect(label).ToHaveCSSAsync("position", "absolute");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheBox_CollapsedRemovesIt_AndBothFollowARuntimeChange()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lbl-visible")).ToBeVisibleAsync();

        await Expect(Page.Locator("#lbl-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Page.Locator("#lbl-hidden")).ToBeHiddenAsync();
        Assert.IsTrue((await Page.Locator("#lbl-hidden").BoundingBoxAsync())!.Height > 0, "A hidden label keeps its room.");

        await Expect(Page.Locator("#lbl-collapsed")).ToHaveCSSAsync("display", "none");
        Assert.IsNull(await Page.Locator("#lbl-collapsed").BoundingBoxAsync());

        var runtime = Page.Locator("#lbl-visibility-runtime");
        await Expect(runtime).ToBeVisibleAsync();

        await Page.Locator("#btn-visibility-hidden").ClickAsync();
        await Expect(runtime).ToHaveCSSAsync("visibility", "hidden");

        await Page.Locator("#btn-visibility-collapsed").ClickAsync();
        await Expect(runtime).ToHaveCSSAsync("display", "none");

        await Page.Locator("#btn-visibility-visible").ClickAsync();
        await Expect(runtime).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task CssVariables_SetOnAContainer_RestyleEveryLabelInside()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lbl-css-required")).ToHaveCSSAsync("font-weight", "300");
        await Expect(Page.Locator("#lbl-css-optional")).ToHaveCSSAsync("font-weight", "300");
        await Expect(Page.Locator("#lbl-css-required .bit-lbl-rqi")).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        await Expect(Page.Locator("#lbl-css-required .bit-lbl-rqi")).ToHaveCSSAsync("margin-left", "7px");
        await Expect(Page.Locator("#lbl-css-optional .bit-lbl-opi")).ToHaveCSSAsync("color", "rgb(40, 50, 60)");
        await Expect(Page.Locator("#lbl-css-optional .bit-lbl-opi")).ToHaveCSSAsync("margin-left", "7px");
    }

    [TestMethod]
    public async Task CssVariables_SetOnOneLabelsStyle_RestyleThatLabel()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-css-own");

        await Expect(label).ToHaveCSSAsync("font-size", "21px");
        await Expect(label).ToHaveCSSAsync("color", "rgb(1, 128, 2)");
        await Expect(label).ToHaveCSSAsync("padding", "3px 5px");
        await Expect(label).ToHaveCSSAsync("line-height", "31px");

        // The variables did not leak to a label outside it.
        Assert.AreNotEqual("21px", await ComputedStyleAsync("#lbl-basic", "font-size"));
    }

    [TestMethod]
    public async Task CssVariables_ColorAndSizeParameters_WinOverTheVariables()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-css-color-wins");

        await Expect(label).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-err-fg)"));
        await Expect(label).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-tpg-fs-xs)"));
    }

    [TestMethod]
    public async Task CssVariables_TheThemesFieldLabelWeight_SetsTheDefaultWeight()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lbl-css-theme-weight")).ToHaveCSSAsync("font-weight", "700");
    }

    [TestMethod]
    public async Task TabIndex_TabbingToTheLabel_ShowsTheFocusRingInTheFocusColor()
    {
        await OpenAsync(Path);

        await Page.Locator("#before-focusable").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        var label = Page.Locator("#lbl-focusable");
        await Expect(label).ToBeFocusedAsync();
        await Expect(label).ToHaveAttributeAsync("tabindex", "0");
        StringAssert.Contains(await ComputedStyleAsync("#lbl-focusable", "box-shadow"), "rgb(200, 0, 100)");
    }

    [TestMethod]
    public async Task BitParams_CascadeTheirDefaults_AndALabelsOwnValueWins()
    {
        await OpenAsync(Path);

        foreach (var id in new[] { "#lbl-params-1", "#lbl-params-2" })
        {
            await Expect(Page.Locator($"{id} .bit-lbl-rqi")).ToHaveTextAsync("(required)");
            await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass("bit-lbl-pri"));
        }

        await Expect(Page.Locator("#lbl-params-3 .bit-lbl-rqi")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#lbl-params-3 .bit-lbl-opi")).ToHaveTextAsync("(optional)");

        await Expect(Page.Locator("#lbl-params-4")).ToHaveClassAsync(HasClass("bit-lbl-suc"));
        await Expect(Page.Locator("#lbl-params-4")).Not.ToHaveClassAsync(HasClass("bit-lbl-pri"));
        await Expect(Page.Locator("#lbl-params-4")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-suc-fg)"));
    }

    [TestMethod]
    [DataRow("Primary", "pri", "--bit-clr-pri-fg")]
    [DataRow("Secondary", "sec", "--bit-clr-sec-fg")]
    [DataRow("Tertiary", "ter", "--bit-clr-ter-fg")]
    [DataRow("Info", "inf", "--bit-clr-inf-fg")]
    [DataRow("Success", "suc", "--bit-clr-suc-fg")]
    [DataRow("Warning", "wrn", "--bit-clr-wrn-fg")]
    [DataRow("SevereWarning", "swr", "--bit-clr-swr-fg")]
    [DataRow("Error", "err", "--bit-clr-err-fg")]
    [DataRow("PrimaryBackground", "pbg", "--bit-clr-fg-pri")]
    [DataRow("SecondaryBackground", "sbg", "--bit-clr-fg-pri")]
    [DataRow("TertiaryBackground", "tbg", "--bit-clr-fg-pri")]
    [DataRow("PrimaryForeground", "pfg", "--bit-clr-fg-pri")]
    [DataRow("SecondaryForeground", "sfg", "--bit-clr-fg-sec")]
    [DataRow("TertiaryForeground", "tfg", "--bit-clr-fg-ter")]
    [DataRow("PrimaryBorder", "pbr", "--bit-clr-fg-pri")]
    [DataRow("SecondaryBorder", "sbr", "--bit-clr-fg-pri")]
    [DataRow("TertiaryBorder", "tbr", "--bit-clr-fg-pri")]
    public async Task Color_EveryValue_SetsTheRoleClassAndReadsItsTextColor(string color, string suffix, string token)
    {
        await OpenAsync(Path);

        var label = Page.Locator($"#lbl-color-{color}");
        var expected = await ResolveAsync("color", $"var({token})");

        await Expect(label).ToHaveClassAsync(HasClass($"bit-lbl-{suffix}"));
        await Expect(label).ToHaveCSSAsync("color", expected);

        // The uncolored label takes its container's color instead.
        await Expect(Page.Locator("#lbl-color-none")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        Assert.AreNotEqual("rgb(1, 2, 3)", expected);
    }

    [TestMethod]
    public async Task Color_Error_KeepsTheRequiredMarkInItsOwnColor()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lbl-color-Error .bit-lbl-rqi")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-req)"));
    }

    [TestMethod]
    [DataRow("Small", "sm", "--bit-tpg-fs-xs")]
    [DataRow("Medium", "md", "--bit-tpg-fs-sm")]
    [DataRow("Large", "lg", "--bit-tpg-fs-md")]
    public async Task Size_EveryValue_MovesTheCaptionAlongTheTypeRamp(string size, string suffix, string token)
    {
        await OpenAsync(Path);

        var label = Page.Locator($"#lbl-size-{size}");

        await Expect(label).ToHaveClassAsync(HasClass($"bit-lbl-{suffix}"));
        await Expect(label).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", $"var({token})"));
    }

    [TestMethod]
    public async Task Size_Unset_IsTheMediumSize_AndTheSizesGrow()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lbl-size-none")).ToHaveCSSAsync("font-size", await ComputedStyleAsync("#lbl-size-Medium", "font-size"));

        var small = await PixelsAsync(Page.Locator("#lbl-size-Small"), "font-size");
        var medium = await PixelsAsync(Page.Locator("#lbl-size-Medium"), "font-size");
        var large = await PixelsAsync(Page.Locator("#lbl-size-Large"), "font-size");

        Assert.IsTrue(small < medium && medium < large, $"{small} < {medium} < {large}");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lbl-style")).ToHaveCSSAsync("color", "rgb(30, 144, 255)");
        await Expect(Page.Locator("#lbl-style")).ToHaveCSSAsync("font-weight", "700");
        await Expect(Page.Locator("#lbl-class")).ToHaveClassAsync(HasClass("e2e-label-class"));
        await Expect(Page.Locator("#lbl-class")).ToHaveClassAsync(HasClass("bit-lbl"));
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheRootAndTheIndicators()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lbl-styles")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Page.Locator("#lbl-styles .bit-lbl-rqi")).ToHaveCSSAsync("color", "rgb(138, 43, 226)");
        await Expect(Page.Locator("#lbl-styles .bit-lbl-rqi")).ToHaveCSSAsync("font-size", "19px");

        await Expect(Page.Locator("#lbl-classes")).ToHaveClassAsync(HasClass("e2e-root-class"));
        await Expect(Page.Locator("#lbl-classes .bit-lbl-opi")).ToHaveClassAsync(HasClass("e2e-optional-class"));
        await Expect(Page.Locator("#lbl-classes-required .bit-lbl-rqi")).ToHaveClassAsync(HasClass("e2e-required-class"));
    }

    [TestMethod]
    public async Task HtmlAttributesAndAriaLabel_AreRenderedOnTheRoot()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-attrs");

        await Expect(label).ToHaveAttributeAsync("aria-label", "An aria name");
        await Expect(label).ToHaveAttributeAsync("data-e2e", "splatted");
        await Expect(label).ToHaveAttributeAsync("title", "A title");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_LaysTheLabelOutRightToLeft_WithTheMarkAfterTheCaption()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-rtl-required");

        await Expect(label).ToHaveAttributeAsync("dir", "rtl");
        await Expect(label).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(label).ToHaveCSSAsync("direction", "rtl");

        // After the caption is to its left in a right-to-left line, the gap on the mark's inline-start (right) side.
        Assert.IsTrue(await MarkIsLeftOfCaptionAsync("#lbl-rtl-required", ".bit-lbl-rqi"), "The mark follows the caption, to its left.");
        Assert.IsTrue(await PixelsAsync(label.Locator(".bit-lbl-rqi"), "margin-right") > 0);
        Assert.AreEqual(0, await PixelsAsync(label.Locator(".bit-lbl-rqi"), "margin-left"));

        // The caption starts at the right edge of the label.
        var box = await Page.Locator("#lbl-rtl").BoundingBoxAsync();
        var textRight = await Page.Locator("#lbl-rtl").EvaluateAsync<double>("el => { const r = document.createRange(); r.selectNodeContents(el); return r.getBoundingClientRect().right; }");
        Assert.IsTrue(Math.Abs(box!.X + box.Width - textRight) < 2, "The caption is laid out from the right.");

        Assert.IsFalse(await MarkIsLeftOfCaptionAsync("#lbl-ltr-required", ".bit-lbl-rqi"), "Left to right, the mark is to the caption's right.");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_PutsTheMarkAfterTheCaption()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-rtl-inherited");

        await Expect(label).Not.ToHaveAttributeAsync("dir", new Regex(".*"));
        await Expect(label).ToHaveCSSAsync("direction", "rtl");
        await Expect(label.Locator(".bit-lbl-opi")).ToHaveTextAsync("(اختیاری)");
        Assert.IsTrue(await MarkIsLeftOfCaptionAsync("#lbl-rtl-inherited", ".bit-lbl-opi"));
    }

    [TestMethod]
    public async Task Runtime_ChangingTheParameters_ReRendersTheLabel()
    {
        await OpenAsync(Path);

        var label = Page.Locator("#lbl-runtime");

        await Expect(label.Locator(".bit-lbl-rqi")).ToHaveCountAsync(0);
        await Page.Locator("#btn-toggle-required").ClickAsync();
        await Expect(label.Locator(".bit-lbl-rqi")).ToHaveTextAsync("*");
        await Expect(label).ToHaveClassAsync(HasClass("bit-lbl-req"));

        await Page.Locator("#btn-set-color").ClickAsync();
        await Expect(label).ToHaveClassAsync(HasClass("bit-lbl-err"));
        await Expect(label).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-err-fg)"));

        await Page.Locator("#btn-set-size").ClickAsync();
        await Expect(label).ToHaveClassAsync(HasClass("bit-lbl-lg"));
        await Expect(label).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-tpg-fs-md)"));

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(label).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(label).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-dis)"));
        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(label).Not.ToHaveClassAsync(HasClass("bit-dis"));

        await Page.Locator("#btn-toggle-nowrap").ClickAsync();
        await Expect(label.Locator(".bit-lbl-row > .bit-lbl-txt")).ToHaveTextAsync("Runtime label");

        await Page.Locator("#btn-set-text").ClickAsync();
        await Expect(label.Locator(".bit-lbl-txt")).ToHaveTextAsync("Changed text");

        await Page.Locator("#btn-toggle-required").ClickAsync();
        await Expect(label.Locator(".bit-lbl-rqi")).ToHaveCountAsync(0);
        await Expect(label).Not.ToHaveClassAsync(HasClass("bit-lbl-req"));
    }

    [TestMethod]
    public async Task Unmount_RemovesTheLabel_WithoutAnError()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#lbl-mounted")).ToBeVisibleAsync();

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#lbl-mounted")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#lbl-mounted")).ToBeVisibleAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private Task<bool> MarkIsLeftOfCaptionAsync(string labelSelector, string markSelector)
    {
        return Page.Locator(labelSelector).EvaluateAsync<bool>(
            @"(el, mark) => {
                const text = [...el.childNodes].find(n => n.nodeType === Node.TEXT_NODE && n.textContent.trim());
                const range = document.createRange();
                range.selectNodeContents(text);
                const caption = range.getBoundingClientRect();
                const indicator = el.querySelector(mark).getBoundingClientRect();
                return indicator.right <= caption.left + 1;
            }", markSelector);
    }

    private Task<string> SelectionAsync() => Page.EvaluateAsync<string>("() => getSelection().toString()");

    private static async Task<double> PixelsAsync(ILocator locator, string property)
    {
        var value = await locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");

        return double.Parse(value.Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture);
    }

    // What a CSS value - a theme token, typically - resolves to on this page, read off a probe element.
    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(
            @"([property, value]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, value);
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return resolved;
            }", new[] { property, value });
    }
}
