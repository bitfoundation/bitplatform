using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// Drives BitSeparator (/e2e/separator) in a real browser: the line its ::before / ::after draw (one segment alone,
/// two flanking content), horizontal and vertical layouts (a flex row's stretch, a run of text, a container's height,
/// AutoSize), AlignContent with ContentOffset both ways, Inset, every LineStyle, Thickness, every Size, Color, Border
/// and Background, the public --bit-Separator-* variables and the parameters winning over them, a nested separator
/// never inheriting the outer one's choices, the separator role, its name and orientation, Decorative, Element,
/// the BitParams cascade, Style/Class/Styles/Classes and splatted attributes, Visibility, Disabled, RTL (explicit and
/// inherited), the prerender, runtime re-renders and unmount.
/// </summary>
[TestClass]
public class BitSeparatorE2ETests : E2ETestBase
{
    private const string Path = "/e2e/separator";

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheSeparatorBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Path}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"spr-text\" role=\"separator\" aria-labelledby=\"spr-text-cnt\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Path);
        await Expect(Page.Locator("#spr-text")).ToHaveTextAsync("Text");
    }

    [TestMethod]
    public async Task Basic_AnEmptySeparator_IsOneHairlineAcrossItsContainer()
    {
        await OpenAsync(Path);

        var separator = Page.Locator("#spr-basic");

        await Expect(separator).ToHaveAttributeAsync("role", "separator");
        await Expect(separator).ToHaveClassAsync(HasClass("bit-spr-hrz"));
        await Expect(separator).ToHaveClassAsync(HasClass("bit-spr-ctr"));
        Assert.AreEqual(400, Math.Round((await separator.BoundingBoxAsync())!.Width));

        Assert.AreEqual(await ResolveAsync("border-top-width", "var(--bit-siz-divider)"), await PseudoAsync("#spr-basic", "::before", "border-top-width"));
        Assert.AreEqual(await ResolveAsync("color", "var(--bit-clr-brd-sec)"), await PseudoAsync("#spr-basic", "::before", "border-top-color"));
        Assert.AreEqual("solid", await PseudoAsync("#spr-basic", "::before", "border-top-style"));
        Assert.AreEqual(400, await PseudoWidthAsync("#spr-basic", "::before"), "The one segment is the whole line.");

        // Without content, the trailing segment does not exist.
        Assert.AreEqual("none", await PseudoAsync("#spr-basic", "::after", "content"));
    }

    [TestMethod]
    public async Task Content_SitsBetweenTwoSegments_AndNamesTheSeparator()
    {
        await OpenAsync(Path);

        var separator = Page.Locator("#spr-text");
        var content = separator.Locator("> .bit-spr-cnt");

        await Expect(content).ToHaveTextAsync("Text");
        await Expect(content).ToHaveAttributeAsync("id", "spr-text-cnt");
        await Expect(separator).ToHaveAttributeAsync("aria-labelledby", "spr-text-cnt");
        await Expect(Page.GetByRole(AriaRole.Separator, new() { Name = "Text", Exact = true })).ToHaveIdAsync("spr-text");

        Assert.AreEqual("\"\"", await PseudoAsync("#spr-text", "::after", "content"));

        var before = await PseudoWidthAsync("#spr-text", "::before");
        var after = await PseudoWidthAsync("#spr-text", "::after");
        var contentBox = await content.BoundingBoxAsync();

        Assert.IsTrue(before > 100 && Math.Abs(before - after) < 1, $"Two equal segments flank the content ({before}, {after}).");
        Assert.AreEqual(400, Math.Round(before + after + contentBox!.Width), "The segments and the content fill the line.");

        // The content is any markup: an icon here.
        await Expect(Page.Locator("#spr-icon > .bit-spr-cnt #spr-icon-glyph")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Disabled_DimsTheLineAndTheContent()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#spr-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        Assert.AreEqual(await ResolveAsync("color", "var(--bit-clr-brd-dis)"), await PseudoAsync("#spr-disabled", "::before", "border-top-color"));
        await Expect(Page.Locator("#spr-disabled > .bit-spr-cnt")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-dis)"));
    }

    [TestMethod]
    public async Task Vertical_InAFlexRow_StretchesToTheRow_AndSaysItsOrientation()
    {
        await OpenAsync(Path);

        var separator = Page.Locator("#spr-vertical");

        await Expect(separator).ToHaveClassAsync(HasClass("bit-spr-vrt"));
        await Expect(separator).ToHaveAttributeAsync("aria-orientation", "vertical");
        await Expect(Page.Locator("#spr-basic")).Not.ToHaveAttributeAsync("aria-orientation", new Regex(".*"));

        Assert.AreEqual(60, Math.Round((await separator.BoundingBoxAsync())!.Height));
        Assert.AreEqual(await ResolveAsync("border-left-width", "var(--bit-siz-divider)"), await PseudoAsync("#spr-vertical", "::before", "border-left-width"));
        Assert.AreEqual("0px", await PseudoAsync("#spr-vertical", "::before", "border-top-width"));
    }

    [TestMethod]
    public async Task Vertical_WithContent_FlanksItAboveAndBelow()
    {
        await OpenAsync(Path);

        var box = await Page.Locator("#spr-vertical-content").BoundingBoxAsync();
        var content = await Page.Locator("#spr-vertical-content > .bit-spr-cnt").BoundingBoxAsync();

        Assert.AreEqual(60, Math.Round(box!.Height));
        Assert.IsTrue(content!.Y > box.Y && content.Y + content.Height < box.Y + box.Height, "The content sits inside the line.");
        Assert.IsTrue(Math.Abs(content.Y + content.Height / 2 - (box.Y + box.Height / 2)) < 1.5, "Centered along the line.");
        Assert.IsTrue(await PseudoHeightAsync("#spr-vertical-content", "::before") > 1);
        Assert.IsTrue(await PseudoHeightAsync("#spr-vertical-content", "::after") > 1);
    }

    [TestMethod]
    public async Task Vertical_BetweenRunsOfText_IsAsTallAsTheText_AndAContainerSetsItsHeight()
    {
        await OpenAsync(Path);

        var inline = await Page.Locator("#spr-inline").BoundingBoxAsync();
        var fontSize = double.Parse((await ComputedStyleAsync("#inline-text", "font-size")).Replace("px", ""), CultureInfo.InvariantCulture);

        Assert.IsTrue(inline!.Height >= fontSize - 0.5, $"Between runs of text the line is at least 1em ({inline.Height}px).");
        await Expect(Page.Locator("#spr-inline")).ToHaveCSSAsync("display", "inline-flex");

        Assert.AreEqual(120, Math.Round((await Page.Locator("#spr-vertical-container").BoundingBoxAsync())!.Height));
    }

    [TestMethod]
    public async Task AutoSize_DropsTheFullWidthAndTheStretch()
    {
        await OpenAsync(Path);

        var auto = await Page.Locator("#spr-vertical-autosize").BoundingBoxAsync();
        Assert.IsTrue(auto!.Height < 30, $"A vertical AutoSize separator is not stretched to the 60px row ({auto.Height}px).");
        await Expect(Page.Locator("#spr-vertical-autosize")).ToHaveAttributeAsync("style", new Regex("height:auto;align-self:auto"));

        // In a column that centers its items, a plain separator still spans it while an AutoSize one sizes to its content.
        Assert.AreEqual(400, Math.Round((await Page.Locator("#spr-autosize-full").BoundingBoxAsync())!.Width));
        var horizontal = await Page.Locator("#spr-autosize").BoundingBoxAsync();
        var content = await Page.Locator("#spr-autosize > .bit-spr-cnt").BoundingBoxAsync();
        Assert.IsTrue(horizontal!.Width < 400, $"A horizontal AutoSize separator is not the container's width ({horizontal.Width}px).");
        Assert.IsTrue(horizontal.Width >= content!.Width);
    }

    [TestMethod]
    [DataRow("start", "bit-spr-srt")]
    [DataRow("center", "bit-spr-ctr")]
    [DataRow("end", "bit-spr-end")]
    [DataRow("unset", "bit-spr-ctr")]
    [DataRow("other", "bit-spr-ctr")]
    public async Task AlignContent_PutsTheContentAtTheStartTheMiddleOrTheEnd(string align, string className)
    {
        await OpenAsync(Path);

        var separator = Page.Locator($"#spr-align-{align}");
        var box = await separator.BoundingBoxAsync();
        var content = await separator.Locator("> .bit-spr-cnt").BoundingBoxAsync();

        await Expect(separator).ToHaveClassAsync(HasClass(className));

        switch (className)
        {
            case "bit-spr-srt":
                Assert.AreEqual(box!.X, content!.X, 1, "At the start.");
                Assert.AreEqual(0, await PseudoWidthAsync($"#spr-align-{align}", "::before"), 0.5);
                break;
            case "bit-spr-end":
                Assert.AreEqual(box!.X + box.Width, content!.X + content.Width, 1, "At the end.");
                Assert.AreEqual(0, await PseudoWidthAsync($"#spr-align-{align}", "::after"), 0.5);
                break;
            default:
                Assert.AreEqual(box!.X + box.Width / 2, content!.X + content.Width / 2, 1, "In the middle.");
                break;
        }
    }

    [TestMethod]
    public async Task ContentOffset_PushesStartOrEndContentOffTheEdge_AsALengthOrAPercentOfTheLine()
    {
        await OpenAsync(Path);

        var start = await Page.Locator("#spr-offset-start").BoundingBoxAsync();
        var startContent = await Page.Locator("#spr-offset-start > .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(32, startContent!.X - start!.X, 1);
        Assert.AreEqual(32, await PseudoWidthAsync("#spr-offset-start", "::before"), 0.5);
        await Expect(Page.Locator("#spr-offset-start")).ToHaveAttributeAsync("style", new Regex("--bit-spr-ofs:32px"));

        var end = await Page.Locator("#spr-offset-end").BoundingBoxAsync();
        var endContent = await Page.Locator("#spr-offset-end > .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(40, end!.X + end.Width - (endContent!.X + endContent.Width), 1, "10% of the 400px line.");

        // A centered content has no offset to take.
        var center = await Page.Locator("#spr-offset-center").BoundingBoxAsync();
        var centerContent = await Page.Locator("#spr-offset-center > .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(center!.X + center.Width / 2, centerContent!.X + centerContent.Width / 2, 1);
    }

    [TestMethod]
    public async Task AlignContent_Vertical_PutsTheContentAtTheTopOrTheBottom_AndTheOffsetMeasuresAlongTheLine()
    {
        await OpenAsync(Path);

        var start = await Page.Locator("#spr-valign-start").BoundingBoxAsync();
        var startContent = await Page.Locator("#spr-valign-start > .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(start!.Y, startContent!.Y, 1, "Start is the top.");

        var end = await Page.Locator("#spr-valign-end").BoundingBoxAsync();
        var endContent = await Page.Locator("#spr-valign-end > .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(end!.Y + end.Height, endContent!.Y + endContent.Height, 1, "End is the bottom.");

        var center = await Page.Locator("#spr-valign-center").BoundingBoxAsync();
        var centerContent = await Page.Locator("#spr-valign-center > .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(center!.Y + center.Height / 2, centerContent!.Y + centerContent.Height / 2, 1);

        var offset = await Page.Locator("#spr-valign-offset").BoundingBoxAsync();
        var offsetContent = await Page.Locator("#spr-valign-offset > .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(offset!.Height * 0.25, offsetContent!.Y - offset.Y, 1, "25% of the 200px line.");
    }

    [TestMethod]
    public async Task Inset_HoldsTheLineOffBothEnds_OrOffTheStartAndTheEnd()
    {
        await OpenAsync(Path);

        var full = await Page.Locator("#spr-full").BoundingBoxAsync();
        Assert.AreEqual(400, Math.Round(full!.Width));

        var inset = Page.Locator("#spr-inset");
        Assert.AreEqual(400, Math.Round((await inset.BoundingBoxAsync())!.Width), "The inset stays inside the width (border-box).");
        await Expect(inset).ToHaveCSSAsync("padding-left", "32px");
        await Expect(inset).ToHaveCSSAsync("padding-right", "32px");
        var segments = await PseudoWidthAsync("#spr-inset", "::before") + await PseudoWidthAsync("#spr-inset", "::after");
        var content = await inset.Locator("> .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(400 - 64, segments + content!.Width, 1);

        var startInset = Page.Locator("#spr-inset-start");
        await Expect(startInset).ToHaveCSSAsync("padding-left", "100px");
        await Expect(startInset).ToHaveCSSAsync("padding-right", "0px");

        // A vertical one holds the line off its top and bottom.
        await Expect(Page.Locator("#spr-vertical-inset")).ToHaveCSSAsync("padding-top", "12px");
        await Expect(Page.Locator("#spr-vertical-inset")).ToHaveCSSAsync("padding-bottom", "12px");
        Assert.AreEqual(36, await PseudoHeightAsync("#spr-vertical-inset", "::before"), 1);
    }

    [TestMethod]
    [DataRow("default", null, "solid")]
    [DataRow("Solid", "bit-spr-sld", "solid")]
    [DataRow("Dashed", "bit-spr-dsh", "dashed")]
    [DataRow("Dotted", "bit-spr-dot", "dotted")]
    [DataRow("Double", "bit-spr-dbl", "double")]
    public async Task LineStyle_EveryValue_DrawsBothSegmentsInThatStyle(string style, string? className, string expected)
    {
        await OpenAsync(Path);

        var id = $"#spr-line-{style}";

        if (className is not null) await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass(className));
        Assert.AreEqual(expected, await PseudoAsync(id, "::before", "border-top-style"));
        Assert.AreEqual(expected, await PseudoAsync(id, "::after", "border-top-style"));
    }

    [TestMethod]
    public async Task Thickness_SetsTheWeightOfTheLine_AndWinsOverSize()
    {
        await OpenAsync(Path);

        Assert.AreEqual("8px", await PseudoAsync("#spr-thickness", "::before", "border-top-width"));
        Assert.AreEqual("3px", await PseudoAsync("#spr-line-Dotted", "::before", "border-top-width"));
        Assert.AreEqual("4px", await PseudoAsync("#spr-line-Double", "::after", "border-top-width"));
        Assert.AreEqual("5px", await PseudoAsync("#spr-thickness-wins", "::before", "border-top-width"));

        Assert.AreEqual("3px", await PseudoAsync("#spr-vline-dashed", "::before", "border-left-width"));
        Assert.AreEqual("dashed", await PseudoAsync("#spr-vline-dashed", "::before", "border-left-style"));
    }

    [TestMethod]
    [DataRow("Small", "bit-spr-sm", 1)]
    [DataRow("Medium", "bit-spr-md", 2)]
    [DataRow("Large", "bit-spr-lg", 3)]
    public async Task Size_EveryValue_StepsTheWeightUpFromTheDivider(string size, string className, int factor)
    {
        await OpenAsync(Path);

        var divider = Px(await ResolveAsync("border-top-width", "var(--bit-siz-divider)"));

        await Expect(Page.Locator($"#spr-size-{size}")).ToHaveClassAsync(HasClass(className));
        Assert.AreEqual(divider * factor, Px(await PseudoAsync($"#spr-size-{size}", "::before", "border-top-width")), 0.01);
    }

    [TestMethod]
    public async Task Size_OnAVerticalSeparator_WeighsTheVerticalLine()
    {
        await OpenAsync(Path);

        var divider = Px(await ResolveAsync("border-top-width", "var(--bit-siz-divider)"));

        Assert.AreEqual(divider * 3, Px(await PseudoAsync("#spr-vsize-Large", "::before", "border-left-width")), 0.01);
    }

    [TestMethod]
    [DataRow("Primary", "pri", "--bit-clr-pri")]
    [DataRow("Secondary", "sec", "--bit-clr-sec")]
    [DataRow("Tertiary", "ter", "--bit-clr-ter")]
    [DataRow("Info", "inf", "--bit-clr-inf")]
    [DataRow("Success", "suc", "--bit-clr-suc")]
    [DataRow("Warning", "wrn", "--bit-clr-wrn")]
    [DataRow("SevereWarning", "swr", "--bit-clr-swr")]
    [DataRow("Error", "err", "--bit-clr-err")]
    [DataRow("PrimaryBackground", "pbg", "--bit-clr-bg-pri")]
    [DataRow("SecondaryBackground", "sbg", "--bit-clr-bg-sec")]
    [DataRow("TertiaryBackground", "tbg", "--bit-clr-bg-ter")]
    [DataRow("PrimaryForeground", "pfg", "--bit-clr-fg-pri")]
    [DataRow("SecondaryForeground", "sfg", "--bit-clr-fg-sec")]
    [DataRow("TertiaryForeground", "tfg", "--bit-clr-fg-ter")]
    [DataRow("PrimaryBorder", "pbr", "--bit-clr-brd-pri")]
    [DataRow("SecondaryBorder", "sbr", "--bit-clr-brd-sec")]
    [DataRow("TertiaryBorder", "tbr", "--bit-clr-brd-ter")]
    public async Task Color_EveryValue_PaintsTheLineInItsRole(string color, string suffix, string token)
    {
        await OpenAsync(Path);

        var id = $"#spr-color-{color}";
        var expected = await ResolveAsync("color", $"var({token})");

        await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass($"bit-spr-{suffix}"));
        Assert.AreEqual(expected, await PseudoAsync(id, "::before", "border-top-color"));
        Assert.AreEqual(expected, await PseudoAsync(id, "::after", "border-top-color"));
    }

    [TestMethod]
    [DataRow("Primary", "bit-spr-bpr", "var(--bit-clr-brd-pri)")]
    [DataRow("Secondary", "bit-spr-bsr", "var(--bit-clr-brd-sec)")]
    [DataRow("Tertiary", "bit-spr-btr", "var(--bit-clr-brd-ter)")]
    [DataRow("Transparent", "bit-spr-brr", "transparent")]
    public async Task Border_EveryKind_PaintsTheLineInANeutralTier(string kind, string className, string value)
    {
        await OpenAsync(Path);

        var id = $"#spr-border-{kind}";

        await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass(className));
        Assert.AreEqual(await ResolveAsync("color", value), await PseudoAsync(id, "::before", "border-top-color"));
    }

    [TestMethod]
    public async Task Border_AndColorBothSet_ColorWins()
    {
        await OpenAsync(Path);

        Assert.AreEqual(await ResolveAsync("color", "var(--bit-clr-err)"), await PseudoAsync("#spr-color-wins", "::before", "border-top-color"));
    }

    [TestMethod]
    [DataRow("Primary", "bit-spr-bpg", "var(--bit-clr-bg-pri)")]
    [DataRow("Secondary", "bit-spr-bsg", "var(--bit-clr-bg-sec)")]
    [DataRow("Tertiary", "bit-spr-btg", "var(--bit-clr-bg-ter)")]
    [DataRow("Transparent", "bit-spr-brg", "transparent")]
    public async Task Background_EveryKind_PaintsBehindTheContent(string kind, string className, string value)
    {
        await OpenAsync(Path);

        var separator = Page.Locator($"#spr-background-{kind}");

        await Expect(separator).ToHaveClassAsync(HasClass(className));
        await Expect(separator.Locator("> .bit-spr-cnt")).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", value));
        // The root itself stays unpainted.
        await Expect(separator).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [TestMethod]
    public async Task Nested_ASeparatorInAnotherOnesContent_NeverInheritsItsChoices()
    {
        await OpenAsync(Path);

        Assert.AreEqual("6px", await PseudoAsync("#spr-nested-outer", "::before", "border-top-width"));
        Assert.AreEqual("dashed", await PseudoAsync("#spr-nested-outer", "::before", "border-top-style"));

        Assert.AreEqual(await ResolveAsync("border-top-width", "var(--bit-siz-divider)"), await PseudoAsync("#spr-nested-inner", "::before", "border-top-width"));
        Assert.AreEqual("solid", await PseudoAsync("#spr-nested-inner", "::before", "border-top-style"));
        Assert.AreEqual(await ResolveAsync("color", "var(--bit-clr-brd-sec)"), await PseudoAsync("#spr-nested-inner", "::before", "border-top-color"));
    }

    [TestMethod]
    public async Task Accessibility_AriaLabelNamesTheSeparator_AndWinsOverTheContent()
    {
        await OpenAsync(Path);

        await Expect(Page.GetByRole(AriaRole.Separator, new() { Name = "End of the shipping details" })).ToHaveIdAsync("spr-aria-label");

        var labelled = Page.Locator("#spr-aria-label-content");
        await Expect(labelled).ToHaveAttributeAsync("aria-label", "Label wins");
        await Expect(labelled).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));
        await Expect(Page.GetByRole(AriaRole.Separator, new() { Name = "Label wins" })).ToHaveIdAsync("spr-aria-label-content");
    }

    [TestMethod]
    public async Task Decorative_TakesTheSeparatorOutOfTheAccessibilityTree()
    {
        await OpenAsync(Path);

        var empty = Page.Locator("#spr-decorative");
        await Expect(empty).ToHaveAttributeAsync("role", "none");
        await Expect(empty).ToHaveAttributeAsync("aria-hidden", "true");

        // One with content keeps the content readable as plain text, unnamed and unhidden.
        var withContent = Page.Locator("#spr-decorative-content");
        await Expect(withContent).ToHaveAttributeAsync("role", "none");
        await Expect(withContent).Not.ToHaveAttributeAsync("aria-hidden", new Regex(".*"));
        await Expect(withContent).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));
        await Expect(Page.GetByRole(AriaRole.Separator, new() { Name = "Read as plain text" })).ToHaveCountAsync(0);
        await Expect(Page.GetByText("Read as plain text")).ToBeVisibleAsync();

        // A vertical decorative one says no orientation either.
        await Expect(Page.Locator("#spr-vertical-decorative")).Not.ToHaveAttributeAsync("aria-orientation", new Regex(".*"));
        await Expect(Page.Locator("#spr-vertical-decorative")).ToHaveAttributeAsync("role", "none");

        // An aria-hidden the page splats on is kept as written.
        await Expect(Page.Locator("#spr-splat-hidden")).ToHaveAttributeAsync("aria-hidden", "false");
    }

    [TestMethod]
    public async Task Accessibility_ASplattedAriaLabelledby_IsKeptAsWritten()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#spr-splat-labelledby")).ToHaveAttributeAsync("aria-labelledby", "external-name");
        await Expect(Page.GetByRole(AriaRole.Separator, new() { Name = "An external name" })).ToHaveIdAsync("spr-splat-labelledby");
    }

    [TestMethod]
    public async Task Element_RendersTheSeparatorAsAListItem_AndFallsBackToADivForAVoidOrInvalidTag()
    {
        await OpenAsync(Path);

        var li = Page.Locator("#spr-li");
        Assert.AreEqual("LI", await li.EvaluateAsync<string>("el => el.tagName"));
        await Expect(li).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(li).ToHaveCSSAsync("padding-left", "40px");
        await Expect(Page.Locator("#contacts").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);

        // A menu owns its separators, which stay announced.
        await Expect(Page.Locator("#menu").GetByRole(AriaRole.Separator)).ToHaveCountAsync(1);

        Assert.AreEqual("DIV", await Page.Locator("#spr-hr").EvaluateAsync<string>("el => el.tagName"));
        await Expect(Page.Locator("#spr-hr > .bit-spr-cnt")).ToHaveTextAsync("Void falls back");
        Assert.AreEqual("DIV", await Page.Locator("#spr-invalid").EvaluateAsync<string>("el => el.tagName"));
        Assert.AreEqual("SECTION", await Page.Locator("#spr-span").EvaluateAsync<string>("el => el.tagName"));
    }

    [TestMethod]
    public async Task BitParams_CascadeTheirDefaults_AndASeparatorsOwnValueWins()
    {
        await OpenAsync(Path);

        foreach (var id in new[] { "#spr-params-1", "#spr-params-2" })
        {
            await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass("bit-spr-srt"));
            Assert.AreEqual("dashed", await PseudoAsync(id, "::before", "border-top-style"));
            Assert.AreEqual("2px", await PseudoAsync(id, "::after", "border-top-width"));
        }

        await Expect(Page.Locator("#spr-params-3")).ToHaveClassAsync(HasClass("bit-spr-sld"));
        Assert.AreEqual("solid", await PseudoAsync("#spr-params-3", "::after", "border-top-style"));
        Assert.AreEqual("2px", await PseudoAsync("#spr-params-3", "::after", "border-top-width"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAContainer_RestyleTheLineAndTheContent()
    {
        await OpenAsync(Path);

        var separator = Page.Locator("#spr-css");
        var content = separator.Locator("> .bit-spr-cnt");

        Assert.AreEqual("rgb(10, 20, 30)", await PseudoAsync("#spr-css", "::before", "border-top-color"));
        Assert.AreEqual("7px", await PseudoAsync("#spr-css", "::before", "border-top-width"));
        Assert.AreEqual("dotted", await PseudoAsync("#spr-css", "::after", "border-top-style"));
        await Expect(separator).ToHaveCSSAsync("padding-top", "9px");
        await Expect(separator).ToHaveCSSAsync("padding-left", "11px");
        await Expect(content).ToHaveCSSAsync("padding-left", "13px");
        await Expect(content).ToHaveCSSAsync("color", "rgb(40, 50, 60)");
        await Expect(content).ToHaveCSSAsync("background-color", "rgb(70, 80, 90)");
        await Expect(content).ToHaveCSSAsync("font-size", "17px");
        await Expect(content).ToHaveCSSAsync("font-weight", "700");

        Assert.AreEqual(50, await PseudoWidthAsync("#spr-css-offset", "::before"), 0.5, "--bit-Separator-content-offset");
    }

    [TestMethod]
    public async Task CssVariables_TheParametersWinOverTheVariables()
    {
        await OpenAsync(Path);

        var divider = Px(await ResolveAsync("border-top-width", "var(--bit-siz-divider)"));

        Assert.AreEqual(await ResolveAsync("color", "var(--bit-clr-suc)"), await PseudoAsync("#spr-css-params", "::before", "border-top-color"));
        Assert.AreEqual(divider * 2, Px(await PseudoAsync("#spr-css-params", "::before", "border-top-width")), 0.01);
        Assert.AreEqual("dashed", await PseudoAsync("#spr-css-params", "::before", "border-top-style"));
        await Expect(Page.Locator("#spr-css-params")).ToHaveCSSAsync("padding-left", "3px");
        await Expect(Page.Locator("#spr-css-params > .bit-spr-cnt")).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-bg-ter)"));

        Assert.AreEqual("2px", await PseudoAsync("#spr-css-thickness-param", "::before", "border-top-width"));
        Assert.AreEqual(await ResolveAsync("color", "var(--bit-clr-brd-pri)"), await PseudoAsync("#spr-css-thickness-param", "::before", "border-top-color"));
        Assert.AreEqual(20, await PseudoWidthAsync("#spr-css-thickness-param", "::before"), 0.5);

        // A disabled separator keeps the disabled colors, whatever the variables ask for.
        Assert.AreEqual(await ResolveAsync("color", "var(--bit-clr-brd-dis)"), await PseudoAsync("#spr-css-disabled", "::before", "border-top-color"));
        await Expect(Page.Locator("#spr-css-disabled > .bit-spr-cnt")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-fg-dis)"));
    }

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachTheRootAndTheContent()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#spr-style")).ToHaveCSSAsync("max-width", "320px");
        await Expect(Page.Locator("#spr-class")).ToHaveClassAsync(HasClass("e2e-separator-class"));

        await Expect(Page.Locator("#spr-styles")).ToHaveCSSAsync("text-transform", "uppercase");
        await Expect(Page.Locator("#spr-styles > .bit-spr-cnt")).ToHaveCSSAsync("color", "rgb(30, 144, 255)");
        await Expect(Page.Locator("#spr-styles > .bit-spr-cnt")).ToHaveCSSAsync("font-weight", "600");

        await Expect(Page.Locator("#spr-classes")).ToHaveClassAsync(HasClass("e2e-root-class"));
        await Expect(Page.Locator("#spr-classes > .bit-spr-cnt")).ToHaveClassAsync(HasClass("e2e-content-class"));
    }

    [TestMethod]
    public async Task HtmlAttributes_ASplattedClassAndStyle_AreJoinedWithTheComponentsOwn()
    {
        await OpenAsync(Path);

        var separator = Page.Locator("#spr-splat");

        await Expect(separator).ToHaveClassAsync(HasClass("splat-class"));
        await Expect(separator).ToHaveClassAsync(HasClass("own-class"));
        await Expect(separator).ToHaveClassAsync(HasClass("bit-spr"));
        await Expect(separator).ToHaveCSSAsync("outline-color", "rgb(1, 2, 3)");
        await Expect(separator).ToHaveCSSAsync("opacity", "0.5");
        await Expect(separator).ToHaveAttributeAsync("data-e2e", "splatted");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheRoom_CollapsedTakesItAway()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#spr-hidden")).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await Page.Locator("#spr-hidden").BoundingBoxAsync())!.Height > 0);
        await Expect(Page.Locator("#spr-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_PutsStartContentOnTheRight()
    {
        await OpenAsync(Path);

        var start = Page.Locator("#spr-rtl-start");
        await Expect(start).ToHaveAttributeAsync("dir", "rtl");
        await Expect(start).ToHaveClassAsync(HasClass("bit-rtl"));

        var box = await start.BoundingBoxAsync();
        var content = await start.Locator("> .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(box!.X + box.Width, content!.X + content.Width, 1, "Start is the right edge.");

        var end = await Page.Locator("#spr-rtl-end").BoundingBoxAsync();
        var endContent = await Page.Locator("#spr-rtl-end > .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(end!.X, endContent!.X, 1, "End is the left edge.");

        // The inset's start is the right side.
        await Expect(Page.Locator("#spr-rtl-inset")).ToHaveCSSAsync("padding-right", "100px");
        await Expect(Page.Locator("#spr-rtl-inset")).ToHaveCSSAsync("padding-left", "0px");
    }

    [TestMethod]
    public async Task Rtl_InheritedFromAnAncestor_PutsStartContentOnTheRight()
    {
        await OpenAsync(Path);

        var separator = Page.Locator("#spr-rtl-inherited");
        await Expect(separator).Not.ToHaveAttributeAsync("dir", new Regex(".*"));

        var box = await separator.BoundingBoxAsync();
        var content = await separator.Locator("> .bit-spr-cnt").BoundingBoxAsync();
        Assert.AreEqual(box!.X + box.Width, content!.X + content.Width, 1);
    }

    [TestMethod]
    public async Task Runtime_ChangingTheParameters_ReRendersTheSeparator()
    {
        await OpenAsync(Path);

        var separator = Page.Locator("#spr-runtime");

        await Expect(separator).ToHaveClassAsync(HasClass("bit-spr-hrz"));
        await Page.Locator("#btn-toggle-vertical").ClickAsync();
        await Expect(separator).ToHaveClassAsync(HasClass("bit-spr-vrt"));
        await Expect(separator).ToHaveAttributeAsync("aria-orientation", "vertical");
        Assert.AreEqual(80, Math.Round((await separator.BoundingBoxAsync())!.Height));
        await Page.Locator("#btn-toggle-vertical").ClickAsync();
        await Expect(separator).ToHaveClassAsync(HasClass("bit-spr-hrz"));

        await Page.Locator("#btn-set-color").ClickAsync();
        await Expect(separator).ToHaveClassAsync(HasClass("bit-spr-suc"));
        Assert.AreEqual(await ResolveAsync("color", "var(--bit-clr-suc)"), await PseudoAsync("#spr-runtime", "::before", "border-top-color"));

        await Page.Locator("#btn-set-align").ClickAsync();
        await Expect(separator).ToHaveClassAsync(HasClass("bit-spr-end"));

        await Page.Locator("#btn-toggle-decorative").ClickAsync();
        await Expect(separator).ToHaveAttributeAsync("role", "none");
        await Page.Locator("#btn-toggle-decorative").ClickAsync();
        await Expect(separator).ToHaveAttributeAsync("role", "separator");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(separator).ToHaveClassAsync(HasClass("bit-dis"));
        Assert.AreEqual(await ResolveAsync("color", "var(--bit-clr-brd-dis)"), await PseudoAsync("#spr-runtime", "::before", "border-top-color"));

        await Page.Locator("#btn-toggle-content").ClickAsync();
        await Expect(separator.Locator(".bit-spr-cnt")).ToHaveCountAsync(0);
        await Expect(separator).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));
        await Page.Locator("#btn-toggle-content").ClickAsync();
        await Expect(separator.Locator("#runtime-content")).ToHaveTextAsync("Runtime");
        await Expect(separator).ToHaveAttributeAsync("aria-labelledby", "spr-runtime-cnt");
    }

    [TestMethod]
    public async Task Unmount_RemovesTheSeparator_WithoutAnError()
    {
        await OpenAsync(Path);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#spr-mounted")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#spr-mounted")).ToHaveTextAsync("Mounted");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private Task<string> PseudoAsync(string selector, string pseudo, string property)
    {
        return Page.Locator(selector).EvaluateAsync<string>("(el, [pseudo, property]) => getComputedStyle(el, pseudo).getPropertyValue(property)", new[] { pseudo, property });
    }

    private async Task<double> PseudoWidthAsync(string selector, string pseudo)
    {
        var width = await PseudoAsync(selector, pseudo, "width");

        return width == "auto" ? 0 : Px(width);
    }

    private async Task<double> PseudoHeightAsync(string selector, string pseudo)
    {
        var height = await PseudoAsync(selector, pseudo, "height");

        return height == "auto" ? 0 : Px(height);
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    // What a CSS value - a theme token, typically - resolves to on this page, read off a probe element.
    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(
            @"([property, value]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, value);
                probe.style.borderStyle = 'solid';
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return resolved;
            }", new[] { property, value });
    }
}
