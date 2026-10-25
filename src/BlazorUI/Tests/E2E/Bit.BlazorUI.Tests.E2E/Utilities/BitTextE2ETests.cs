using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// Drives BitText (/e2e/text) in a real browser: every Typography variant (its tag, its class and the theme's type ramp
/// it reads), Element (another tag, the invalid and the void fallbacks), every Weight, Italic, Underline,
/// Strikethrough, every Transform, Numeric, Monospace, NoWrap, Block and LineClamp truncation, BreakWord, ForceBreak,
/// every Wrap, Hyphenate, PreserveWhitespace, every Align, Gutter, every Trim, Foreground, Gradient, every Color,
/// AriaLevel, VisuallyHidden (and its focusable content coming back), Lang, NoSelect, Visibility, Disabled, the
/// public --bit-Text-* variables and the parameters winning over them, the BitParams cascade, Style/Class and the
/// splatted class and style, RTL (explicit and inherited), the prerender, runtime re-renders and unmount.
/// </summary>
[TestClass]
public class BitTextE2ETests : E2ETestBase
{
    private const string Path = "/e2e/text";

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheTextBeforeTheCircuitConnects()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Path}")).TextAsync();

        StringAssert.Contains(html, "<div id=\"txt-default\" class=\"bit-txt-subtitle1 bit-txt\">Default (Subtitle1)</div>");
        StringAssert.Contains(html, "<h1 id=\"txt-typo-H1\" class=\"bit-txt-h1 bit-txt\">");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Path);
        await Expect(Page.Locator("#txt-default")).ToHaveTextAsync("Default (Subtitle1)");
    }

    [TestMethod]
    public async Task Default_IsASubtitle1Div()
    {
        await OpenAsync(Path);

        var text = Page.Locator("#txt-default");

        Assert.AreEqual("DIV", await TagAsync(text));
        await Expect(text).ToHaveClassAsync(HasClass("bit-txt-subtitle1"));
        await Expect(text).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-tpg-subtitle1-font-size)"));
    }

    [TestMethod]
    [DataRow("H1", "h1", "H1")]
    [DataRow("H2", "h2", "H2")]
    [DataRow("H3", "h3", "H3")]
    [DataRow("H4", "h4", "H4")]
    [DataRow("H5", "h5", "H5")]
    [DataRow("H6", "h6", "H6")]
    [DataRow("Subtitle1", "subtitle1", "DIV")]
    [DataRow("Subtitle2", "subtitle2", "DIV")]
    [DataRow("Body1", "body1", "P")]
    [DataRow("Body2", "body2", "P")]
    [DataRow("Button", "button", "SPAN")]
    [DataRow("Caption1", "caption1", "SPAN")]
    [DataRow("Caption2", "caption2", "SPAN")]
    [DataRow("Overline", "overline", "SPAN")]
    public async Task Typography_EveryVariant_RendersItsTag_AndReadsItsStepOfTheTypeRamp(string typography, string variant, string tag)
    {
        await OpenAsync(Path);

        var text = Page.Locator($"#txt-typo-{typography}");

        Assert.AreEqual(tag, await TagAsync(text));
        await Expect(text).ToHaveClassAsync(HasClass($"bit-txt-{variant}"));
        await Expect(text).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", $"var(--bit-tpg-{variant}-font-size)"));
        await Expect(text).ToHaveCSSAsync("font-weight", await ResolveAsync("font-weight", $"var(--bit-tpg-{variant}-font-weight)"));
    }

    [TestMethod]
    public async Task Typography_TheHeadingsGetSmallerFromH1ToH6_AndButtonAndOverlineAreUppercaseBlocks()
    {
        await OpenAsync(Path);

        var sizes = new List<double>();
        foreach (var heading in new[] { "H1", "H2", "H3", "H4", "H5", "H6" })
        {
            sizes.Add(Px(await ComputedStyleAsync($"#txt-typo-{heading}", "font-size")));
        }

        for (var i = 1; i < sizes.Count; i++)
        {
            Assert.IsTrue(sizes[i] <= sizes[i - 1], $"H{i + 1} ({sizes[i]}px) is no larger than H{i} ({sizes[i - 1]}px).");
        }
        Assert.IsTrue(sizes[0] > sizes[5]);

        foreach (var id in new[] { "#txt-typo-Button", "#txt-typo-Overline" })
        {
            await Expect(Page.Locator(id)).ToHaveCSSAsync("text-transform", await ResolveAsync("text-transform", $"var(--bit-tpg-{(id.EndsWith("Button") ? "button" : "overline")}-text-transform)"));
            await Expect(Page.Locator(id)).ToHaveCSSAsync("display", await ResolveAsync("display", $"var(--bit-tpg-{(id.EndsWith("Button") ? "button" : "overline")}-display)"));
        }
    }

    [TestMethod]
    public async Task Typography_Inherit_TakesTheTypeOfTheElementAroundIt()
    {
        await OpenAsync(Path);

        var text = Page.Locator("#txt-inherit");

        Assert.AreEqual("P", await TagAsync(text));
        await Expect(text).ToHaveClassAsync(HasClass("bit-txt-inherit"));
        await Expect(text).ToHaveCSSAsync("font-size", "27px");
        await Expect(text).ToHaveCSSAsync("font-weight", "200");
        await Expect(text).ToHaveCSSAsync("font-family", "monospace");
        await Expect(text).ToHaveCSSAsync("letter-spacing", "3px");
    }

    [TestMethod]
    public async Task Element_SetsTheTagApartFromTheLook()
    {
        await OpenAsync(Path);

        var h2 = Page.Locator("#txt-el-h2");
        Assert.AreEqual("H2", await TagAsync(h2));
        await Expect(h2).ToHaveClassAsync(HasClass("bit-txt-h4"));
        await Expect(h2).ToHaveCSSAsync("font-size", await ComputedStyleAsync("#txt-typo-H4", "font-size"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 2, Name = "An h2 drawn at the size of an h4" })).ToHaveIdAsync("txt-el-h2");

        Assert.AreEqual("SPAN", await TagAsync(Page.Locator("#txt-el-span")));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "An h4 look on a span" })).ToHaveCountAsync(0);

        Assert.AreEqual("STRONG", await TagAsync(Page.Locator("#txt-el-strong")));
        Assert.AreEqual("BLOCKQUOTE", await TagAsync(Page.Locator("#txt-el-blockquote")));
        Assert.AreEqual("SECTION", await TagAsync(Page.Locator("#txt-el-padded")));
    }

    [TestMethod]
    public async Task Element_AnInvalidTagFallsBackToTheVariantsTag_AndAVoidOneHoldsNoContent()
    {
        await OpenAsync(Path);

        Assert.AreEqual("P", await TagAsync(Page.Locator("#txt-el-invalid")));
        await Expect(Page.Locator("#txt-el-invalid")).ToHaveTextAsync("Invalid falls back");

        var hr = Page.Locator("#txt-el-void");
        Assert.AreEqual("HR", await TagAsync(hr));
        Assert.AreEqual(0, await hr.EvaluateAsync<int>("el => el.childNodes.length"));
        await Expect(Page.GetByText("A void element holds no content")).ToHaveCountAsync(0);
    }

    [TestMethod]
    [DataRow("Light", "bit-txt-fwl", "var(--bit-tpg-fw-light)")]
    [DataRow("Regular", "bit-txt-fwr", "var(--bit-tpg-fw-regular)")]
    [DataRow("Medium", "bit-txt-fwm", "var(--bit-tpg-fw-medium)")]
    [DataRow("Semibold", "bit-txt-fws", "var(--bit-tpg-fw-semibold)")]
    [DataRow("Bold", "bit-txt-fwb", "var(--bit-tpg-fw-bold)")]
    public async Task Weight_EveryValue_MovesAlongTheWeightScale(string weight, string className, string token)
    {
        await OpenAsync(Path);

        var text = Page.Locator($"#txt-weight-{weight}");

        await Expect(text).ToHaveClassAsync(HasClass(className));
        await Expect(text).ToHaveCSSAsync("font-weight", await ResolveAsync("font-weight", token));
    }

    [TestMethod]
    public async Task Emphasis_ItalicUnderlineAndStrikethrough_CombineFreely()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-italic")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Page.Locator("#txt-underline")).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(Page.Locator("#txt-strike")).ToHaveCSSAsync("text-decoration-line", "line-through");
        await Expect(Page.Locator("#txt-underline-strike")).ToHaveCSSAsync("text-decoration-line", "underline line-through");
        await Expect(Page.Locator("#txt-default")).ToHaveCSSAsync("text-decoration-line", "none");
        await Expect(Page.Locator("#txt-default")).ToHaveCSSAsync("font-style", "normal");
    }

    [TestMethod]
    [DataRow("None", "bit-txt-trn", "none", "mIxEd case transform")]
    [DataRow("Uppercase", "bit-txt-tru", "uppercase", "MIXED CASE TRANSFORM")]
    [DataRow("Lowercase", "bit-txt-trl", "lowercase", "mixed case transform")]
    [DataRow("Capitalize", "bit-txt-trc", "capitalize", "MIxEd Case Transform")]
    public async Task Transform_EveryValue_ChangesTheCaseOnScreenOnly(string transform, string className, string css, string rendered)
    {
        await OpenAsync(Path);

        var text = Page.Locator($"#txt-transform-{transform}");

        await Expect(text).ToHaveClassAsync(HasClass(className));
        await Expect(text).ToHaveCSSAsync("text-transform", css);
        Assert.AreEqual(rendered, await text.InnerTextAsync());

        // The text itself - what is copied and read out - is untouched.
        Assert.AreEqual("mIxEd case transform", await text.EvaluateAsync<string>("el => el.textContent"));
    }

    [TestMethod]
    public async Task Transform_None_UndoesTheUppercaseOfTheOverline()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-overline-none")).ToHaveCSSAsync("text-transform", "none");
    }

    [TestMethod]
    public async Task NumericAndMonospace_UseTabularFiguresAndTheMonospacedFamily()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-numeric")).ToHaveCSSAsync("font-variant-numeric", "lining-nums tabular-nums");
        await Expect(Page.Locator("#txt-proportional")).ToHaveCSSAsync("font-variant-numeric", "normal");

        var tabular = (await Page.Locator("#txt-numeric").BoundingBoxAsync())!.Width;
        var tabular8 = (await Page.Locator("#txt-numeric-8").BoundingBoxAsync())!.Width;
        Assert.AreEqual(tabular, tabular8, 0.5, "Tabular figures are all one width.");

        await Expect(Page.Locator("#txt-monospace")).ToHaveCSSAsync("font-family", await ResolveAsync("font-family", "var(--bit-tpg-font-family-mono)"));
    }

    [TestMethod]
    public async Task NoWrap_KeepsOneLineEndingInAnEllipsis_AndBlockGivesAnInlineVariantABox()
    {
        await OpenAsync(Path);

        var noWrap = Page.Locator("#txt-nowrap");
        await Expect(noWrap).ToHaveClassAsync(HasClass("bit-txt-nowrap"));
        await Expect(noWrap).ToHaveCSSAsync("white-space", "nowrap");
        await Expect(noWrap).ToHaveCSSAsync("text-overflow", "ellipsis");
        Assert.IsTrue(await noWrap.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "The text is cut.");
        Assert.AreEqual(300, Math.Round((await noWrap.BoundingBoxAsync())!.Width));

        // An inline caption has no width to cut against; Block gives it the container's.
        Assert.IsTrue((await Page.Locator("#txt-nowrap-inline").BoundingBoxAsync())!.Width > 300);
        await Expect(Page.Locator("#txt-nowrap-block")).ToHaveClassAsync(HasClass("bit-txt-blk"));
        await Expect(Page.Locator("#txt-nowrap-block")).ToHaveCSSAsync("display", "block");
        Assert.AreEqual(300, Math.Round((await Page.Locator("#txt-nowrap-block").BoundingBoxAsync())!.Width));
    }

    [TestMethod]
    public async Task LineClamp_CutsTheTextAfterThatManyLines()
    {
        await OpenAsync(Path);

        var clamp = Page.Locator("#txt-clamp");
        var lineHeight = Px(await ComputedStyleAsync("#txt-clamp", "line-height"));

        await Expect(clamp).ToHaveClassAsync(HasClass("bit-txt-clp"));
        await Expect(clamp).ToHaveCSSAsync("-webkit-line-clamp", "2");
        await Expect(clamp).ToHaveAttributeAsync("title", "The full text");
        Assert.AreEqual(lineHeight * 2, (await clamp.BoundingBoxAsync())!.Height, 1, "Two lines tall.");
        Assert.IsTrue((await Page.Locator("#txt-unclamped").BoundingBoxAsync())!.Height > lineHeight * 3, "The same text runs longer unclamped.");

        await Expect(Page.Locator("#txt-clamp-zero")).Not.ToHaveClassAsync(HasClass("bit-txt-clp"));
        await Expect(Page.Locator("#txt-clamp-zero")).Not.ToHaveAttributeAsync("style", new Regex(".*"));
    }

    [TestMethod]
    public async Task Wrapping_BreakWordAndForceBreak_KeepALongWordInsideItsBox()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-breakword")).ToHaveCSSAsync("overflow-wrap", "anywhere");
        Assert.IsFalse(await OverflowsAsync("#txt-breakword"), "BreakWord breaks the path.");
        Assert.IsTrue(await OverflowsAsync("#txt-nobreak"), "The same path overflows without it.");

        await Expect(Page.Locator("#txt-forcebreak")).ToHaveCSSAsync("word-break", "break-all");
        Assert.IsFalse(await OverflowsAsync("#txt-forcebreak"));
    }

    [TestMethod]
    [DataRow("Wrap", "bit-txt-wrp", "text-wrap-mode", "wrap")]
    [DataRow("NoWrap", "bit-txt-wnw", "text-wrap-mode", "nowrap")]
    [DataRow("Balance", "bit-txt-wbl", "text-wrap-style", "balance")]
    [DataRow("Pretty", "bit-txt-wpr", "text-wrap-style", "pretty")]
    [DataRow("Stable", "bit-txt-wst", "text-wrap-style", "stable")]
    public async Task Wrap_EveryValue_PicksHowTheLinesBreak(string wrap, string className, string property, string value)
    {
        await OpenAsync(Path);

        if (await Page.EvaluateAsync<bool>("([p, v]) => !CSS.supports(p, v)", new[] { property, value }))
        {
            Assert.Inconclusive($"This engine does not support {property}: {value}.");
        }

        var text = Page.Locator($"#txt-wrap-{wrap}");

        await Expect(text).ToHaveClassAsync(HasClass(className));
        await Expect(text).ToHaveCSSAsync(property, value);

        if (wrap == "NoWrap")
        {
            Assert.IsTrue(await OverflowsAsync("#txt-wrap-NoWrap"), "NoWrap keeps one line, wider than its box.");
        }
    }

    [TestMethod]
    public async Task HyphenateAndPreserveWhitespace()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-hyphenate")).ToHaveCSSAsync("hyphens", "auto");
        await Expect(Page.Locator("#txt-hyphenate")).ToHaveAttributeAsync("lang", "en");

        var preserved = Page.Locator("#txt-pws");
        await Expect(preserved).ToHaveCSSAsync("white-space", "pre-wrap");
        Assert.IsTrue((await preserved.BoundingBoxAsync())!.Height > (await Page.Locator("#txt-no-pws").BoundingBoxAsync())!.Height,
                      "The written line break is kept.");
        StringAssert.Contains(await preserved.InnerTextAsync(), "\n    indented");
    }

    [TestMethod]
    [DataRow("Start", "start")]
    [DataRow("End", "end")]
    [DataRow("Left", "left")]
    [DataRow("Right", "right")]
    [DataRow("Center", "center")]
    [DataRow("Justify", "justify")]
    [DataRow("MatchParent", "right")]
    [DataRow("Inherit", "right")]
    [DataRow("Initial", "start")]
    [DataRow("Revert", "right")]
    [DataRow("RevertLayer", "right")]
    [DataRow("Unset", "right")]
    public async Task Align_EveryValue_AlignsTheLinesInsideTheBox(string align, string computed)
    {
        await OpenAsync(Path);

        // The values are read under a parent aligned right, so the ones that defer to it are told from the ones that do not.
        var text = Page.Locator($"#txt-align-{align}");
        var css = Regex.Replace(align, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();

        await Expect(text).ToHaveAttributeAsync("style", $"text-align:{css}");
        await Expect(text).ToHaveCSSAsync("text-align", computed);
    }

    [TestMethod]
    public async Task Align_JustifyAll_IsWrittenAsItIs()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-align-JustifyAll")).ToHaveAttributeAsync("style", "text-align:justify-all");

        if (await Page.EvaluateAsync<bool>("() => CSS.supports('text-align', 'justify-all')"))
        {
            await Expect(Page.Locator("#txt-align-JustifyAll")).ToHaveCSSAsync("text-align", "justify-all");
        }
    }

    [TestMethod]
    public async Task Gutter_AddsABottomMarginThatScalesWithTheVariant()
    {
        await OpenAsync(Path);

        var heading = Px(await ComputedStyleAsync("#txt-gutter-h5", "margin-bottom"));
        var body = Px(await ComputedStyleAsync("#txt-gutter-body", "margin-bottom"));

        await Expect(Page.Locator("#txt-gutter-h5")).ToHaveClassAsync(HasClass("bit-txt-gutter"));
        Assert.IsTrue(heading > body && body > 0, $"The heading's gutter ({heading}px) is larger than the body's ({body}px).");
        Assert.AreEqual(0, Px(await ComputedStyleAsync("#txt-no-gutter", "margin-bottom")));

        // In em: the ratio of the gutters is the ratio of the font sizes.
        var headingSize = Px(await ComputedStyleAsync("#txt-gutter-h5", "font-size"));
        var bodySize = Px(await ComputedStyleAsync("#txt-gutter-body", "font-size"));
        Assert.AreEqual(headingSize / bodySize, heading / body, 0.05);
    }

    [TestMethod]
    [DataRow("None", "bit-txt-tmn", "none")]
    [DataRow("Start", "bit-txt-tms", "trim-start")]
    [DataRow("End", "bit-txt-tme", "trim-end")]
    [DataRow("Both", "bit-txt-tmb", "trim-both")]
    public async Task Trim_EveryValue_TrimsTheHalfLeading(string trim, string className, string value)
    {
        await OpenAsync(Path);

        if (await Page.EvaluateAsync<bool>("() => !CSS.supports('text-box-trim', 'trim-both')"))
        {
            Assert.Inconclusive("This engine does not support text-box-trim.");
        }

        var text = Page.Locator($"#txt-trim-{trim}");

        await Expect(text).ToHaveClassAsync(HasClass(className));
        await Expect(text).ToHaveCSSAsync("text-box-trim", value);

        if (trim != "None")
        {
            var none = (await Page.Locator("#txt-trim-None").BoundingBoxAsync())!.Height;
            Assert.IsTrue((await text.BoundingBoxAsync())!.Height < none, $"Trim {trim} makes the box shorter than {none}px.");
        }
    }

    [TestMethod]
    [DataRow("Primary", "bit-txt-pfg", "var(--bit-clr-fg-pri)")]
    [DataRow("Secondary", "bit-txt-sfg", "var(--bit-clr-fg-sec)")]
    [DataRow("Tertiary", "bit-txt-tfg", "var(--bit-clr-fg-ter)")]
    [DataRow("Transparent", "bit-txt-rfg", "transparent")]
    public async Task Foreground_EveryKind_UsesTheThemesNeutralTextColor(string kind, string className, string value)
    {
        await OpenAsync(Path);

        var text = Page.Locator($"#txt-fg-{kind}");

        await Expect(text).ToHaveClassAsync(HasClass(className));
        await Expect(text).ToHaveCSSAsync("color", await ResolveAsync("color", value));

        // Unset, the text takes the color around it.
        await Expect(Page.Locator("#txt-fg-none")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
    }

    [TestMethod]
    public async Task Gradient_PaintsTheGlyphs_AndWinsOverColor()
    {
        await OpenAsync(Path);

        var text = Page.Locator("#txt-gradient");

        await Expect(text).ToHaveClassAsync(HasClass("bit-txt-grd"));
        await Expect(text).ToHaveCSSAsync("background-image", "linear-gradient(90deg, rgb(124, 58, 237), rgb(6, 182, 212))");
        await Expect(text).ToHaveCSSAsync("background-clip", "text");
        await Expect(text).ToHaveCSSAsync("-webkit-text-fill-color", "rgba(0, 0, 0, 0)");

        var colored = Page.Locator("#txt-gradient-color");
        await Expect(colored).ToHaveClassAsync(HasClass("bit-txt-err"));
        await Expect(colored).ToHaveCSSAsync("color", "rgba(0, 0, 0, 0)");
        await Expect(colored).ToHaveCSSAsync("-webkit-text-fill-color", "rgba(0, 0, 0, 0)");
        StringAssert.StartsWith(await ComputedStyleAsync("#txt-gradient-color", "background-image"), "radial-gradient");
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
    public async Task Color_EveryValue_PaintsTheTextInItsRole(string color, string suffix, string token)
    {
        await OpenAsync(Path);

        var text = Page.Locator($"#txt-color-{color}");

        await Expect(text).ToHaveClassAsync(HasClass($"bit-txt-{suffix}"));
        await Expect(text).ToHaveCSSAsync("color", await ResolveAsync("color", $"var({token})"));
    }

    [TestMethod]
    public async Task AriaLevel_AnnouncesAHeadingLevel_WithoutChangingTheTagOrTheLook()
    {
        await OpenAsync(Path);

        var div = Page.Locator("#txt-level-div");
        await Expect(div).ToHaveAttributeAsync("role", "heading");
        await Expect(div).ToHaveAttributeAsync("aria-level", "3");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 3, Name = "A div announced as a level 3 heading" })).ToHaveIdAsync("txt-level-div");

        var h2 = Page.Locator("#txt-level-h2");
        await Expect(h2).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        // On a heading tag the level alone is written, overriding the tag's own (Playwright's role engine reads the tag's
        // level for h1-h6, so the attribute is what is checked here).
        await Expect(h2).ToHaveAttributeAsync("aria-level", "4");

        await Expect(Page.Locator("#txt-level-zero")).Not.ToHaveAttributeAsync("aria-level", new Regex(".*"));
        await Expect(Page.Locator("#txt-level-zero")).Not.ToHaveAttributeAsync("role", new Regex(".*"));
    }

    [TestMethod]
    public async Task VisuallyHidden_HidesFromViewOnly_AndFocusableContentComesBackWhileFocused()
    {
        await OpenAsync(Path);

        var hidden = Page.Locator("#txt-vh");
        var box = await hidden.BoundingBoxAsync();
        await Expect(hidden).ToHaveClassAsync(HasClass("bit-txt-vhd"));
        Assert.IsTrue(box!.Width <= 1 && box.Height <= 1);
        await Expect(Page.GetByText("Read out by a screen reader, drawn nowhere.")).ToHaveCountAsync(1);

        var withLink = Page.Locator("#txt-vh-link");
        Assert.IsTrue((await withLink.BoundingBoxAsync())!.Width <= 1);

        await Page.Locator("#before-skip").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#skip-link")).ToBeFocusedAsync();
        Assert.IsTrue((await withLink.BoundingBoxAsync())!.Width > 50, "The skip link shows while it has the keyboard focus.");

        await Page.Keyboard.PressAsync("Tab");
        Assert.IsTrue((await withLink.BoundingBoxAsync())!.Width <= 1);
    }

    [TestMethod]
    public async Task LangNoSelectAriaLabelAndTabIndex()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-lang")).ToHaveAttributeAsync("lang", "fr");

        await Expect(Page.Locator("#txt-noselect")).ToHaveCSSAsync("user-select", "none");
        await Page.Locator("#txt-noselect").DblClickAsync(new() { Position = new() { X = 10, Y = 5 } });
        Assert.AreEqual("", (await Page.EvaluateAsync<string>("() => getSelection().toString()")).Trim());
        await Page.Locator("#txt-selectable").DblClickAsync(new() { Position = new() { X = 10, Y = 5 } });
        Assert.AreNotEqual("", (await Page.EvaluateAsync<string>("() => getSelection().toString()")).Trim());

        await Expect(Page.Locator("#txt-aria-label")).ToHaveAttributeAsync("aria-label", "A named text");
        await Expect(Page.GetByRole(AriaRole.Region, new() { Name = "A named text" })).ToHaveIdAsync("txt-aria-label");

        await Expect(Page.Locator("#txt-tabindex")).ToHaveAttributeAsync("tabindex", "0");
        await Page.Locator("#txt-aria-label").ClickAsync();
        await Page.Locator("#txt-tabindex").FocusAsync();
        await Expect(Page.Locator("#txt-tabindex")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheRoom_CollapsedDropsIt()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-visible")).ToBeVisibleAsync();
        await Expect(Page.Locator("#txt-hidden")).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await Page.Locator("#txt-hidden").BoundingBoxAsync())!.Width > 10);
        await Expect(Page.Locator("#txt-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task Disabled_DimsTheText_AndKeepsItsGradient()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#txt-disabled")).ToHaveCSSAsync("opacity", await ResolveAsync("opacity", "var(--bit-opa-dis)"));

        await Expect(Page.Locator("#txt-disabled-gradient")).ToHaveCSSAsync("opacity", await ResolveAsync("opacity", "var(--bit-opa-dis)"));
        StringAssert.StartsWith(await ComputedStyleAsync("#txt-disabled-gradient", "background-image"), "linear-gradient");

        await Expect(Page.Locator("#txt-disabled-var")).ToHaveCSSAsync("opacity", "0.25");
    }

    [TestMethod]
    public async Task BitParams_CascadeTheirDefaults_AndATextsOwnValueWins()
    {
        await OpenAsync(Path);

        foreach (var id in new[] { "#txt-params-1", "#txt-params-2" })
        {
            Assert.AreEqual("P", await TagAsync(Page.Locator(id)));
            await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass("bit-txt-body1"));
            await Expect(Page.Locator(id)).ToHaveCSSAsync("text-transform", "uppercase");
            await Expect(Page.Locator(id)).ToHaveCSSAsync("font-weight", await ResolveAsync("font-weight", "var(--bit-tpg-fw-semibold)"));
        }

        await Expect(Page.Locator("#txt-params-3")).ToHaveCSSAsync("text-transform", "none");
        await Expect(Page.Locator("#txt-params-3")).ToHaveCSSAsync("font-style", "italic");
        await Expect(Page.Locator("#txt-params-3")).ToHaveClassAsync(HasClass("bit-txt-body1"));

        await Expect(Page.Locator("#txt-params-outside")).ToHaveClassAsync(HasClass("bit-txt-subtitle1"));
    }

    [TestMethod]
    public async Task StyleClassAndSplattedAttributes_AreMergedWithWhatTheComponentBuilds()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-style")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        await Expect(Page.Locator("#txt-style")).ToHaveCSSAsync("font-weight", "700");
        await Expect(Page.Locator("#txt-class")).ToHaveClassAsync(HasClass("e2e-text-class"));

        var splat = Page.Locator("#txt-splat");
        await Expect(splat).ToHaveClassAsync(HasClass("splat-class"));
        await Expect(splat).ToHaveClassAsync(HasClass("own-class"));
        await Expect(splat).ToHaveClassAsync(HasClass("bit-txt"));
        await Expect(splat).ToHaveCSSAsync("width", "250px");
        await Expect(splat).ToHaveCSSAsync("text-align", "center");
        await Expect(splat).ToHaveAttributeAsync("data-e2e", "splatted");
    }

    [TestMethod]
    public async Task CssVariables_SetOnAContainer_RestyleEveryText_AndAnExplicitColorWins()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-css-body")).ToHaveCSSAsync("color", "rgb(15, 118, 110)");
        StringAssert.StartsWith(await ComputedStyleAsync("#txt-css-body", "font-family"), "Arial");
        StringAssert.StartsWith(await ComputedStyleAsync("#txt-css-heading", "font-family"), "Georgia");
        await Expect(Page.Locator("#txt-css-underline")).ToHaveCSSAsync("text-decoration-color", "rgb(245, 158, 11)");
        await Expect(Page.Locator("#txt-css-underline")).ToHaveCSSAsync("text-decoration-thickness", "2px");
        await Expect(Page.Locator("#txt-css-underline")).ToHaveCSSAsync("text-underline-offset", "6px");
        StringAssert.Contains(await ComputedStyleAsync("#txt-css-mono", "font-family"), "Courier New");
        await Expect(Page.Locator("#txt-css-gutter")).ToHaveCSSAsync("margin-bottom", "21px");
        await Expect(Page.Locator("#txt-css-color")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task Rtl_AnExplicitOrInheritedDirection_PutsTheLogicalAlignmentsOnTheRight()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#txt-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#txt-rtl")).ToHaveCSSAsync("direction", "rtl");

        Assert.IsTrue(await TextHugsAsync("#txt-rtl-start", right: true), "Start is the right edge.");
        Assert.IsTrue(await TextHugsAsync("#txt-rtl-end", right: false), "End is the left edge.");
        Assert.IsTrue(await TextHugsAsync("#txt-rtl-inherited", right: true), "An inherited direction too.");
    }

    [TestMethod]
    public async Task Runtime_ChangingTheParameters_ReRendersTheText()
    {
        await OpenAsync(Path);

        var text = Page.Locator("#txt-runtime");

        Assert.AreEqual("DIV", await TagAsync(text));

        await Page.Locator("#btn-set-typography").ClickAsync();
        await Expect(text).ToHaveClassAsync(HasClass("bit-txt-h2"));
        Assert.AreEqual("H2", await TagAsync(Page.Locator("#txt-runtime")));

        await Page.Locator("#btn-set-color").ClickAsync();
        await Expect(text).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-suc)"));

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(text).ToHaveClassAsync(HasClass("bit-dis"));
        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(text).Not.ToHaveClassAsync(HasClass("bit-dis"));

        await Page.Locator("#btn-set-element").ClickAsync();
        await Expect(Page.Locator("section#txt-runtime")).ToHaveCountAsync(1);

        await Page.Locator("#btn-set-text").ClickAsync();
        await Expect(Page.Locator("#txt-runtime")).ToHaveTextAsync("Changed");
    }

    [TestMethod]
    public async Task Unmount_RemovesTheText_WithoutAnError()
    {
        await OpenAsync(Path);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#txt-mounted")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#txt-mounted")).ToHaveTextAsync("Mounted");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private static Task<string> TagAsync(ILocator locator) => locator.EvaluateAsync<string>("el => el.tagName");

    private Task<bool> OverflowsAsync(string selector) => Page.Locator(selector).EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1");

    private Task<bool> TextHugsAsync(string selector, bool right)
    {
        return Page.Locator(selector).EvaluateAsync<bool>(
            @"(el, right) => {
                const range = document.createRange();
                range.selectNodeContents(el);
                const text = range.getBoundingClientRect();
                const box = el.getBoundingClientRect();
                return right ? Math.abs(box.right - text.right) < 2 : Math.abs(box.left - text.left) < 2;
            }", right);
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

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
