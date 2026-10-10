using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Badge;

/// <summary>
/// Pins the public --bit-Badge-* variables against the stylesheet that reads them, which a bUnit render cannot see:
/// every variable the header documents is read somewhere with a fallback, none of them is ever declared (so they keep
/// inheriting from :root and the ancestors), and nothing is read that the header does not document.
/// </summary>
[TestClass]
public partial class BitBadgeStylesheetTests
{
    [TestMethod]
    public void BitBadgeShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            // The focus color alone is read without a fallback, on purpose: its absence is what hands the ring over
            // to the global --bit-shd-focus-ring (see BitFocusRingStylesheetTests).
            var read = name.EndsWith("-focus-color", StringComparison.Ordinal) ? $"var({name})" : $"var({name}, ";

            StringAssert.Contains(stylesheet, read, $"{name} is documented but never read as it should be.");
        }
    }

    [TestMethod]
    public void BitBadgeShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitBadgeShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Badge-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitBadgeShouldKeepADisabledBadgeInTheDisabledColors()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-dis {", "The disabled badge has no rule of its own.");

        // The disabled colors are handed over from the disabled tokens alone, never through the public color variables...
        StringAssert.Contains(block, "--bit-bdg-dis-txt: var(--bit-bdg-clr-dis-text, #{$clr-pri-dis-text});");
        StringAssert.Contains(block, "--bit-bdg-dis-bg: var(--bit-bdg-clr-bg-dis);");
        StringAssert.Contains(block, "--bit-bdg-dis-brd: var(--bit-bdg-clr-brd-dis);");
        Assert.IsFalse(block.Contains("--bit-Badge-"));

        // ...and the badge reads them ahead of the colors the variants resolve out of the Color and those variables,
        // so a re-tinted badge that is disabled still reads as disabled, while the rule painting it keeps the weight
        // of a single class a Classes.Badge class can compete with.
        StringAssert.Contains(stylesheet, "color: var(--bit-bdg-dis-txt, var(--bit-bdg-cnt-clr-txt));");
        // The hover and press of a clickable badge only move a variable read between the two, so a Classes.Badge
        // class painting the badge keeps that paint under the pointer.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-bdg-dis-bg, var(--bit-bdg-cnt-sbg, var(--bit-bdg-cnt-clr-bg)));");
        StringAssert.Contains(stylesheet, "border-color: var(--bit-bdg-dis-brd, var(--bit-bdg-cnt-sbr, var(--bit-bdg-cnt-clr-brd)));");
        Assert.IsFalse(stylesheet.Contains("\n.bit-bdg.bit-dis .bit-bdg-ctn {"), "The disabled colors are set by a heavier rule of their own.");
    }

    [TestMethod]
    public void BitBadgeShouldHandTheDotToTheBadgeWithoutOutweighingAClassOfItsOwn()
    {
        var stylesheet = ReadStylesheet();

        // A dot is a circle of its own size whatever the public height, padding and radius say, read first by the one
        // rule painting the badge rather than set by a heavier rule, so a Classes.Badge class can still re-size it.
        StringAssert.Contains(stylesheet, "height: var(--bit-bdg-dot-size, var(--bit-bdg-height, var(--bit-Badge-height, ");
        StringAssert.Contains(stylesheet, "padding: var(--bit-bdg-dot-padding, var(--bit-bdg-padding, var(--bit-Badge-padding, ");
        StringAssert.Contains(stylesheet, "border-radius: var(--bit-bdg-dot-radius, var(--bit-bdg-radius, var(--bit-Badge-radius, ");
        Assert.IsFalse(stylesheet.Contains("\n.bit-bdg-dot .bit-bdg-ctn {"), "The dot is set by a heavier rule of its own.");
    }

    [TestMethod,
        DataRow("sm"),
        DataRow("md"),
        DataRow("lg")]
    public void BitBadgeShouldReadItsHeightAndDotFromTheTheme(string size)
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, $"\n.bit-bdg-{size} {{");

        // The height and the dot are design-system decisions, so a preset re-sizes every badge through the theme.
        StringAssert.Contains(block, $"--bit-bdg-height: #{{$siz-badge-{size}}};");
        StringAssert.Contains(block, $"--bit-bdg-dotsize: #{{$siz-badge-dot-{size}}};");
    }

    [TestMethod]
    public void BitBadgeShouldKeepItsOwnLookAwayFromABadgeNestedInItsChildContent()
    {
        var rules = SourceFiles.StripScssComments(ReadStylesheet()).Split('\n');

        // A badge can sit in the child content of another one, so every rule that reaches the badge from a class of
        // the root does it through child combinators: a descendant selector would hand the outer badge's corner,
        // ring, pulse or order to the inner one too.
        var nested = rules.Where(line => NestedBadgeSelector().IsMatch(line) && line.StartsWith('.') is false).ToArray();

        Assert.IsTrue(nested.Length > 0, "No rule reaches the badge from a class of the root.");

        foreach (var line in nested)
        {
            StringAssert.Contains(line, "> .bit-bdg-wrp > ", $"'{line.Trim()}' reaches the badge through a descendant selector.");
        }

        Assert.IsFalse(rules.Any(line => line.StartsWith('.') && DescendantBadgeSelector().IsMatch(line)),
                       "A top-level rule reaches the badge through a descendant selector.");

        // The offsets are handed over through custom properties set on the root's style, which inherit just as well,
        // so each root starts them out unset.
        var stylesheet = ReadStylesheet();
        StringAssert.Contains(stylesheet, "--bit-bdg-ofs-x: initial;");
        StringAssert.Contains(stylesheet, "--bit-bdg-ofs-y: initial;");
    }

    [TestMethod]
    public void BitBadgeShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size or Shape publishes these, so they are read before the variable, which only restyles the
        // default an unset one stands for.
        StringAssert.Contains(stylesheet, "var(--bit-bdg-height, var(--bit-Badge-height, #{$siz-badge-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-bdg-padding, var(--bit-Badge-padding, ");
        StringAssert.Contains(stylesheet, "var(--bit-bdg-fontsize, var(--bit-Badge-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-bdg-radius, var(--bit-Badge-radius, #{$shp-radius-full}))");
        StringAssert.Contains(stylesheet, "var(--bit-bdg-dotsize, var(--bit-Badge-dot-size, #{$siz-badge-dot-md}))");

        // So does an explicit Color, for every color it paints.
        StringAssert.Contains(stylesheet, "--bit-bdg-cnt-clr-bg: var(--bit-bdg-clr, var(--bit-Badge-background, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "--bit-bdg-cnt-clr-txt: var(--bit-bdg-clr-txt, var(--bit-Badge-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-bdg-clr-fcs, var(--bit-Badge-focus-color)))");
        StringAssert.Contains(stylesheet, "var(--bit-bdg-clr, var(--bit-Badge-pulse-color, #{$clr-pri}))");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Badge-[a-z-]+, var\(--bit-bdg-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitBadgeShouldPublishItsColorSizeAndShapeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-bdg {");

        // A badge can sit in the child content of another one, which must not inherit the outer badge's Color, Size or
        // Shape: each root starts the values those classes publish out unset, and the classes - declared further down
        // at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-bdg-clr", "--bit-bdg-clr-txt", "--bit-bdg-clr-dis", "--bit-bdg-clr-dis-text", "--bit-bdg-clr-hover",
                                         "--bit-bdg-clr-active", "--bit-bdg-clr-light", "--bit-bdg-clr-light-hover", "--bit-bdg-clr-fcs",
                                         "--bit-bdg-height", "--bit-bdg-padding", "--bit-bdg-fontsize", "--bit-bdg-dotsize", "--bit-bdg-radius" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    [TestMethod]
    public void BitBadgeShouldStopThePulseUnderReducedMotionUnlessAnimationIsForced()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n.bit-bdg-pls {");

        // An endless attention cue has to be stoppable (WCAG 2.2.2), so reduced motion stops it outright rather than
        // slowing it the way it slows a loader, while ForceAnimation - on the root or on an ancestor - opts back in.
        StringAssert.Contains(block, "@media (prefers-reduced-motion: reduce)");
        StringAssert.Contains(block, "&:not(.bit-fam):not(.bit-fam *) > .bit-bdg-wrp > .bit-bdg-ctn::after {");
        StringAssert.Contains(block, "animation: none;");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Notifications", "Badge", "BitBadge.scss");

    [GeneratedRegex(@"^//\s+(--bit-Badge-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Badge-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Badge-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();

    // A selector, rather than a declaration, that ends on the badge or on the clickable badge.
    [GeneratedRegex(@"\.bit-bdg-(ctn|clk)\b[^;]*\{\s*$")]
    private static partial Regex NestedBadgeSelector();

    // A top-level selector that reaches the badge from an ancestor class without a child combinator.
    [GeneratedRegex(@"\.bit-bdg[a-z-]*\s+\.bit-bdg-(wrp|stl|ctn|clk)\b")]
    private static partial Regex DescendantBadgeSelector();
}
