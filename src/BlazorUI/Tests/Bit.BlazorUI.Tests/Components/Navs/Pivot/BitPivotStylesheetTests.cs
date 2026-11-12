using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Pivot;

/// <summary>
/// Pins the public --bit-Pivot-* custom properties, which a bUnit render cannot see: the list the stylesheet
/// documents, the ones it actually reads, and the table the demo page (and so the MCP server) publishes have to
/// be the same list, and none of them may be declared by the component itself or it would stop inheriting.
/// </summary>
[TestClass]
public class BitPivotStylesheetTests
{
    private static readonly Regex PublicVariable = new(@"--bit-Pivot-[a-z-]+[a-z]");

    [TestMethod]
    public void BitPivotShouldReadEveryPublicVariableItDocuments()
    {
        var (documented, body) = SplitStylesheet();

        var read = PublicVariable.Matches(body).Select(m => m.Value).ToHashSet();

        Assert.AreSequenceEqual(documented, read.ToList(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, "The documented and the consumed --bit-Pivot-* variables differ.");
    }

    [TestMethod]
    public void BitPivotShouldNeverDeclareAPublicVariable()
    {
        var (_, body) = SplitStylesheet();

        Assert.IsFalse(Regex.IsMatch(body, @"(^|[\s;{])--bit-Pivot-[a-z-]+\s*:", RegexOptions.Multiline),
                       "A --bit-Pivot-* variable is declared, so a value set on an ancestor would no longer reach the pivot.");
    }

    [TestMethod]
    public void BitPivotDemoShouldPublishEveryPublicVariable()
    {
        var (documented, _) = SplitStylesheet();

        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Navs", "Pivot", "BitPivotDemo.razor.cs");

        var published = Regex.Matches(demo, @"Name\s*=\s*""(--bit-Pivot-[a-z-]+)""").Select(m => m.Groups[1].Value).ToList();

        Assert.AreSequenceEqual(documented, published, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, "The componentCssVariables table of the demo page is out of step with the stylesheet.");
    }

    [TestMethod]
    public void BitPivotShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt {");

        // The accent is resolved once per pivot on its root: the role an explicit Color publishes first, then the
        // public variable, then the primary role an unset Color stands for.
        Assert.Contains("--bit-pvt-clr: var(--bit-pvt-role-main, var(--bit-Pivot-color, #{$clr-pri}));", root);
        Assert.Contains("--bit-pvt-clr-focus: var(--bit-pvt-role-focus, var(--bit-Pivot-focus-color));", root);
        Assert.Contains("--bit-pvt-clr-dis: var(--bit-pvt-role-dis, var(--bit-Pivot-disabled-color, #{$clr-pri-dis}));", root);
        Assert.Contains("--bit-pvt-clr-dis-text: var(--bit-pvt-role-dis-text, var(--bit-Pivot-disabled-text-color, #{$clr-pri-dis-text}));", root);

        // The indicator and the colors of a Tab item are the Color's as well, ahead of their own variables.
        Assert.Contains("background-color: var(--bit-pvt-role-main, var(--bit-Pivot-indicator-color, var(--bit-Pivot-color, #{$clr-pri})));", stylesheet);
        // (A hovered item is painted from the state variable its hover sets, read first by the rule at rest.)
        Assert.Contains("color: var(--bit-pvt-itm-clr, var(--bit-pvt-role-text, var(--bit-Pivot-item-selected-color, #{$clr-pri-text})));", stylesheet);
        Assert.Contains("--bit-pvt-itm-bg: var(--bit-pvt-role-hover, var(--bit-Pivot-item-hover-background, #{$clr-pri-hover}));", stylesheet);

        // An explicit Size publishes the font size and the item height, which an unset one leaves to the variables.
        Assert.Contains("font-size: var(--bit-pvt-fs, var(--bit-Pivot-font-size, #{$tg-fs-sm}));", stylesheet);
        Assert.Contains("height: var(--bit-pvt-ih, var(--bit-Pivot-item-height, #{$siz-tab}));", stylesheet);

        // The role classes publish the bare role and nothing that reads a public variable.
        var roles = SourceFiles.GetScssBlock(stylesheet, "\n    .bit-pvt-#{$role} {");
        Assert.Contains("--bit-pvt-role-main: #{role($tokens, main)};", roles);
        Assert.DoesNotContain("--bit-Pivot-", roles, "A role class reads a public variable ahead of the role.");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Pivot-[a-z-]+, var\(--bit-pvt-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitPivotShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt {");

        // A pivot nested in the panel of another one must not inherit the outer pivot's Color or Size: each root starts
        // the values those classes publish out unset, and the classes - declared further down at the same weight -
        // still win on the root that carries them.
        foreach (var property in new[] { "--bit-pvt-role-main", "--bit-pvt-role-hover", "--bit-pvt-role-focus", "--bit-pvt-role-text",
                                         "--bit-pvt-role-dis", "--bit-pvt-role-dis-text", "--bit-pvt-fs", "--bit-pvt-ih", "--bit-pvt-is", "--bit-pvt-mh" })
        {
            Assert.Contains($"{property}: initial;", root);
        }

        var rootAt = stylesheet.IndexOf("\n.bit-pvt {", System.StringComparison.Ordinal);
        Assert.IsLessThan(stylesheet.IndexOf("\n    .bit-pvt-#{$role} {", System.StringComparison.Ordinal), rootAt, "The color classes are declared ahead of the root that resets them.");
        Assert.IsLessThan(stylesheet.IndexOf("\n.bit-pvt-md {", System.StringComparison.Ordinal), rootAt, "The size classes are declared ahead of the root that resets them.");
    }

    [TestMethod]
    public void BitPivotShouldKeepANestedPivotFromInheritingTheGapAndSizeOfTheOneAroundIt()
    {
        var stylesheet = ReadStylesheet();

        // The Gap parameter is written inline on the root, and a custom property inherits: every pivot resets it.
        Assert.Contains("--bit-pvt-gap: initial;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt {"));

        // The size is a step declared on each root rather than a rule reaching into the header of a pivot, which
        // would reach the header of a pivot nested in its panel as well.
        Assert.Contains("--bit-pvt-fs:", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-sm {"));
        Assert.Contains("--bit-pvt-fs:", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-md {"));
        Assert.Contains("--bit-pvt-fs:", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-lg {"));
    }

    [TestMethod]
    public void BitPivotSizesShouldScaleTheHeightOffTheTabToken()
    {
        var stylesheet = ReadStylesheet();

        // A Small header that is as tall as a Medium one is not a size at all.
        Assert.Contains("--bit-pvt-ih: calc(#{$siz-tab} * 0.75);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-sm {"));
        Assert.Contains("--bit-pvt-ih: #{$siz-tab};", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-md {"));
        Assert.Contains("--bit-pvt-ih: calc(#{$siz-tab} * 1.25);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-lg {"));

        // An explicit Size wins over the public variable, which restyles the medium height an unset one stands for.
        Assert.Contains("height: var(--bit-pvt-ih, var(--bit-Pivot-item-height, #{$siz-tab}));", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvti {"));
    }

    [TestMethod]
    public void BitPivotOverflowMenuShouldFollowThePopupTokens()
    {
        var stylesheet = ReadStylesheet();

        // The rows of a popup list are one of the size families of the design system, per size like the items.
        Assert.Contains("--bit-pvt-mh: #{$siz-item-sm};", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-sm {"));
        Assert.Contains("--bit-pvt-mh: #{$siz-item-md};", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-md {"));
        Assert.Contains("--bit-pvt-mh: #{$siz-item-lg};", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-lg {"));
        Assert.Contains("min-height: var(--bit-pvt-mh, #{$siz-item-md});", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-mni {"));

        // A popup enters on the decelerating curve, like every other callout of the library.
        Assert.Contains("animation-timing-function: $mot-easing-decelerate;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-mnc {"));

        // The menu scrolls, so a ring drawn around the outside of a row would be clipped to one edge of it.
        var row = SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-mni {");
        Assert.Contains("box-shadow: none;", row);
        Assert.Contains("@include focus-ring-inset(var(--bit-pvt-clr-focus, #{$clr-pri-focus}));", row);
    }

    [TestMethod]
    public void BitPivotOutlineShouldAlwaysDrawTheRuleItsSelectedTabOpensOnto()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pvt-oln {");

        // Fluent's tab divider is none, and an outlined tab opening onto nothing is a floating box.
        Assert.Contains("height: var(--bit-Pivot-divider-thickness, #{$shp-border-width});", block);
        Assert.Contains("width: var(--bit-Pivot-divider-thickness, #{$shp-border-width});", block);

        // Every position opens the selected tab on the edge facing the panel.
        Assert.Contains("@include outline-edges(block-start, block-end,", block);
        Assert.Contains("@include outline-edges(block-end, block-start,", block);
        Assert.Contains("@include outline-edges(inline-start, inline-end,", block);
        Assert.Contains("@include outline-edges(inline-end, inline-start,", block);
    }

    [TestMethod]
    public void BitPivotSlideHeaderShouldStayScrollableForTouch()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pvt-sld {");

        // Clipped (overflow: hidden) the header would move with the buttons only, never with a swipe.
        Assert.Contains("overflow-x: auto;", block);
        Assert.Contains("scrollbar-width: none;", block);
    }

    [TestMethod]
    public void BitPivotDismissButtonShouldMeetTheMinimumTargetSize()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pvti-dbt {");

        // spacing(3) is 24px at the default scaling, the floor of WCAG 2.2 SC 2.5.8 for a target inside another one.
        Assert.Contains("width: var(--bit-Pivot-dismiss-size, #{spacing(3)});", block);
        Assert.Contains("height: var(--bit-Pivot-dismiss-size, #{spacing(3)});", block);
    }



    [TestMethod]
    public void BitPivotShouldNeverReachAPartThroughAPlainDescendantSelector()
    {
        // A rule nested in another one and written as a plain class is a descendant selector, which would reach
        // into the panel of the pivot as well - and so into a pivot nested there, taking on the position, the
        // header type and the state of the one around it. The parts of a pivot are reached from its root with
        // child combinators only (the in-header / in-tablist paths, or & and >).
        var (_, body) = SplitStylesheet();

        var leaks = new System.Collections.Generic.List<string>();

        foreach (var rule in SourceFiles.GetScssRules(body).Where(r => r.Header.StartsWith('@') is false))
        {
            var parent = rule.Ancestors.LastOrDefault(s => s.StartsWith('@') is false);
            if (parent is null) continue;

            foreach (var part in rule.Header.Split(',').Select(p => p.Trim()))
            {
                if (part.StartsWith('.')) leaks.Add($"{parent} {{ {part} }}");
            }
        }

        Assert.IsEmpty(leaks, $"Descendant selectors: {string.Join(" | ", leaks)}");
    }

    [TestMethod]
    public void BitPivotShouldDrawTheFocusRingInsideATabTheHeaderClips()
    {
        // The Menu, Slide and Scroll headers clip what overflows them, which would cut an outer ring off.
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pvt-mnu,\n.bit-pvt-sld,\n.bit-pvt-scr {");

        Assert.Contains("#{$tab}:focus-visible", block);

        // The outer ring of focus-ring-own is taken off and the inset one, which also keeps the forced-colors outline
        // inside the tab, drawn instead.
        Assert.Contains("box-shadow: none;", block);
        Assert.Contains("@include focus-ring-inset(var(--bit-pvt-clr-focus, #{$clr-pri-focus}));", block);
    }

    [TestMethod]
    public void BitPivotShouldDrawTheDividerFromTheThemeToken()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pvt-hwr {");

        Assert.Contains("height: var(--bit-Pivot-divider-thickness, #{$siz-tab-divider});", block);
        Assert.Contains("background-color: var(--bit-Pivot-divider-color, #{$clr-brd-sec});", block);
    }

    [TestMethod]
    public void BitPivotShouldFrameTheSelectedOutlineTabAloneInForcedColors()
    {
        const string forcedColors = "@media (forced-colors: active)";

        var stylesheet = ReadStylesheet();
        var rules = SourceFiles.GetScssRules(SourceFiles.GetScssBlock(stylesheet, "\n@mixin outline-edges(")).Skip(1).ToArray();
        var outline = SourceFiles.GetScssRules(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-oln {")).Skip(1).ToArray();
        static string Declarations(SourceFiles.ScssRule[] rules, string header) =>
            string.Join("; ", rules.Single(r => r.Ancestors.Contains(forcedColors) && Regex.Replace(r.Header, @"\s+", " ") == header).Declarations);

        // Every tab hides the edges it reserves; the selected one is framed, open on the panel's side, and accented in the
        // system selection color.
        Assert.AreEqual("@include forced-hidden-border", Declarations(rules, "#{$itm}"));
        Assert.AreEqual("border-color: CanvasText; border-#{$near}-color: Canvas; border-#{$far}-color: Highlight", Declarations(rules, "#{$sel}"));
        Assert.AreEqual("border-#{$far}-color: GrayText", Declarations(rules, "&.bit-dis #{$sel}, #{$sel}.bit-dis"));

        // A hidden edge lying over the rule would cut it under every tab, so the rule is laid over the tabs there, and the
        // selected tab - open to it - and a focused one, whose ring the rule would cover, over the rule.
        Assert.AreEqual("z-index: 1", Declarations(outline, "#{$hwr}::before"));
        Assert.AreEqual("z-index: 2", Declarations(outline, "#{$sel}"));
        Assert.AreEqual("z-index: 2", Declarations(outline, "&:focus-visible"));

        // The forced rules weigh what the ones they replace do, so they win by coming after every one of them.
        var forced = rules.Select(r => r.Header == forcedColors || r.Ancestors.Contains(forcedColors)).ToArray();
        Assert.IsTrue(forced.SkipWhile(f => f is false).All(f => f), "A rule of the outline edges comes after their forced-colors block.");
    }



    private static (System.Collections.Generic.List<string> Documented, string Body) SplitStylesheet()
    {
        var stylesheet = ReadStylesheet();

        // The documentation is the run of comment lines before the first rule.
        var start = stylesheet.IndexOf("\n.bit-pvt {", System.StringComparison.Ordinal);
        Assert.IsGreaterThan(0, start, "The root rule was not found in the stylesheet.");

        var header = stylesheet[..start];
        var body = stylesheet[start..];

        var documented = Regex.Matches(header, @"^//\s+(--bit-Pivot-[a-z-]+)", RegexOptions.Multiline)
                              .Select(m => m.Groups[1].Value)
                              .ToList();

        Assert.IsNotEmpty(documented, "The stylesheet documents no public variable.");
        Assert.HasCount(documented.Count, documented.Distinct(), "A public variable is documented twice.");

        return (documented, body);
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Navs", "Pivot", "BitPivot.scss");
}
