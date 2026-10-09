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

        CollectionAssert.AreEquivalent(documented, read.ToList(), "The documented and the consumed --bit-Pivot-* variables differ.");
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

        CollectionAssert.AreEquivalent(documented, published, "The componentCssVariables table of the demo page is out of step with the stylesheet.");
    }

    [TestMethod]
    public void BitPivotShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt {");

        // The accent is resolved once per pivot on its root: the role an explicit Color publishes first, then the
        // public variable, then the primary role an unset Color stands for.
        StringAssert.Contains(root, "--bit-pvt-clr: var(--bit-pvt-role-main, var(--bit-Pivot-color, #{$clr-pri}));");
        StringAssert.Contains(root, "--bit-pvt-clr-focus: var(--bit-pvt-role-focus, var(--bit-Pivot-focus-color, #{$clr-pri-focus}));");
        StringAssert.Contains(root, "--bit-pvt-clr-dis: var(--bit-pvt-role-dis, var(--bit-Pivot-disabled-color, #{$clr-pri-dis}));");
        StringAssert.Contains(root, "--bit-pvt-clr-dis-text: var(--bit-pvt-role-dis-text, var(--bit-Pivot-disabled-text-color, #{$clr-pri-dis-text}));");

        // The indicator and the colors of a Tab item are the Color's as well, ahead of their own variables.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-pvt-role-main, var(--bit-Pivot-indicator-color, var(--bit-Pivot-color, #{$clr-pri})));");
        // (A hovered item is painted from the state variable its hover sets, read first by the rule at rest.)
        StringAssert.Contains(stylesheet, "color: var(--bit-pvt-itm-clr, var(--bit-pvt-role-text, var(--bit-Pivot-item-selected-color, #{$clr-pri-text})));");
        StringAssert.Contains(stylesheet, "--bit-pvt-itm-bg: var(--bit-pvt-role-hover, var(--bit-Pivot-item-hover-background, #{$clr-pri-hover}));");

        // An explicit Size publishes the font size and the item height, which an unset one leaves to the variables.
        StringAssert.Contains(stylesheet, "font-size: var(--bit-pvt-fs, var(--bit-Pivot-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "height: var(--bit-pvt-ih, var(--bit-Pivot-item-height, #{$siz-tab}));");

        // The role classes publish the bare role and nothing that reads a public variable.
        var roles = SourceFiles.GetScssBlock(stylesheet, "\n    .bit-pvt-#{$role} {");
        StringAssert.Contains(roles, "--bit-pvt-role-main: #{role($tokens, main)};");
        Assert.IsFalse(roles.Contains("--bit-Pivot-"), "A role class reads a public variable ahead of the role.");

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
            StringAssert.Contains(root, $"{property}: initial;");
        }

        var rootAt = stylesheet.IndexOf("\n.bit-pvt {", System.StringComparison.Ordinal);
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n    .bit-pvt-#{$role} {", System.StringComparison.Ordinal), "The color classes are declared ahead of the root that resets them.");
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n.bit-pvt-md {", System.StringComparison.Ordinal), "The size classes are declared ahead of the root that resets them.");
    }

    [TestMethod]
    public void BitPivotShouldKeepANestedPivotFromInheritingTheGapAndSizeOfTheOneAroundIt()
    {
        var stylesheet = ReadStylesheet();

        // The Gap parameter is written inline on the root, and a custom property inherits: every pivot resets it.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt {"), "--bit-pvt-gap: initial;");

        // The size is a step declared on each root rather than a rule reaching into the header of a pivot, which
        // would reach the header of a pivot nested in its panel as well.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-sm {"), "--bit-pvt-fs:");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-md {"), "--bit-pvt-fs:");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-lg {"), "--bit-pvt-fs:");
    }

    [TestMethod]
    public void BitPivotSizesShouldScaleTheHeightOffTheTabToken()
    {
        var stylesheet = ReadStylesheet();

        // A Small header that is as tall as a Medium one is not a size at all.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-sm {"), "--bit-pvt-ih: calc(#{$siz-tab} * 0.75);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-md {"), "--bit-pvt-ih: #{$siz-tab};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-lg {"), "--bit-pvt-ih: calc(#{$siz-tab} * 1.25);");

        // An explicit Size wins over the public variable, which restyles the medium height an unset one stands for.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvti {"), "height: var(--bit-pvt-ih, var(--bit-Pivot-item-height, #{$siz-tab}));");
    }

    [TestMethod]
    public void BitPivotOverflowMenuShouldFollowThePopupTokens()
    {
        var stylesheet = ReadStylesheet();

        // The rows of a popup list are one of the size families of the design system, per size like the items.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-sm {"), "--bit-pvt-mh: #{$siz-item-sm};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-md {"), "--bit-pvt-mh: #{$siz-item-md};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-lg {"), "--bit-pvt-mh: #{$siz-item-lg};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-mni {"), "min-height: var(--bit-pvt-mh, #{$siz-item-md});");

        // A popup enters on the decelerating curve, like every other callout of the library.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-mnc {"), "animation-timing-function: $mot-easing-decelerate;");

        // The menu scrolls, so a ring drawn around the outside of a row would be clipped to one edge of it.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pvt-mni {"), "box-shadow: inset 0 0 0 #{$shp-focus-ring-width} var(--bit-pvt-clr-focus);");
    }

    [TestMethod]
    public void BitPivotOutlineShouldAlwaysDrawTheRuleItsSelectedTabOpensOnto()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pvt-oln {");

        // Fluent's tab divider is none, and an outlined tab opening onto nothing is a floating box.
        StringAssert.Contains(block, "height: var(--bit-Pivot-divider-thickness, #{$shp-border-width});");
        StringAssert.Contains(block, "width: var(--bit-Pivot-divider-thickness, #{$shp-border-width});");

        // Every position opens the selected tab on the edge facing the panel.
        StringAssert.Contains(block, "@include outline-edges(block-start, block-end,");
        StringAssert.Contains(block, "@include outline-edges(block-end, block-start,");
        StringAssert.Contains(block, "@include outline-edges(inline-start, inline-end,");
        StringAssert.Contains(block, "@include outline-edges(inline-end, inline-start,");
    }

    [TestMethod]
    public void BitPivotSlideHeaderShouldStayScrollableForTouch()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pvt-sld {");

        // Clipped (overflow: hidden) the header would move with the buttons only, never with a swipe.
        StringAssert.Contains(block, "overflow-x: auto;");
        StringAssert.Contains(block, "scrollbar-width: none;");
    }

    [TestMethod]
    public void BitPivotDismissButtonShouldMeetTheMinimumTargetSize()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pvti-dbt {");

        // spacing(3) is 24px at the default scaling, the floor of WCAG 2.2 SC 2.5.8 for a target inside another one.
        StringAssert.Contains(block, "width: var(--bit-Pivot-dismiss-size, #{spacing(3)});");
        StringAssert.Contains(block, "height: var(--bit-Pivot-dismiss-size, #{spacing(3)});");
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

        Assert.AreEqual(0, leaks.Count, $"Descendant selectors: {string.Join(" | ", leaks)}");
    }

    [TestMethod]
    public void BitPivotShouldDrawTheFocusRingInsideATabTheHeaderClips()
    {
        // The Menu, Slide and Scroll headers clip what overflows them, which would cut an outer ring off.
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pvt-mnu,\n.bit-pvt-sld,\n.bit-pvt-scr {");

        StringAssert.Contains(block, "#{$tab}:focus-visible");
        StringAssert.Contains(block, "box-shadow: inset 0 0 0 #{$shp-focus-ring-width} var(--bit-pvt-clr-focus);");
        StringAssert.Contains(block, "outline-offset: calc(-1 * #{$shp-focus-ring-width});");
    }

    [TestMethod]
    public void BitPivotShouldDrawTheDividerFromTheThemeToken()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pvt-hwr {");

        StringAssert.Contains(block, "height: var(--bit-Pivot-divider-thickness, #{$siz-tab-divider});");
        StringAssert.Contains(block, "background-color: var(--bit-Pivot-divider-color, #{$clr-brd-sec});");
    }



    private static (System.Collections.Generic.List<string> Documented, string Body) SplitStylesheet()
    {
        var stylesheet = ReadStylesheet();

        // The documentation is the run of comment lines before the first rule.
        var start = stylesheet.IndexOf("\n.bit-pvt {", System.StringComparison.Ordinal);
        Assert.IsTrue(start > 0, "The root rule was not found in the stylesheet.");

        var header = stylesheet[..start];
        var body = stylesheet[start..];

        var documented = Regex.Matches(header, @"^//\s+(--bit-Pivot-[a-z-]+)", RegexOptions.Multiline)
                              .Select(m => m.Groups[1].Value)
                              .ToList();

        Assert.IsTrue(documented.Count > 0, "The stylesheet documents no public variable.");
        Assert.AreEqual(documented.Count, documented.Distinct().Count(), "A public variable is documented twice.");

        return (documented, body);
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Navs", "Pivot", "BitPivot.scss");
}
