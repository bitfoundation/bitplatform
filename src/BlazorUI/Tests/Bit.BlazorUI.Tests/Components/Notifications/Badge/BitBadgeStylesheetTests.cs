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
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
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

        StringAssert.Contains(stylesheet, "\n    &.bit-dis {", "The disabled badge has no rule of its own.");

        var block = SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-dis {");

        // The disabled colors are handed over from the disabled tokens alone, never through the public color variables...
        StringAssert.Contains(block, "--bit-bdg-dis-txt: var(--bit-bdg-clr-dis-text);");
        StringAssert.Contains(block, "--bit-bdg-dis-bg: var(--bit-bdg-clr-bg-dis);");
        StringAssert.Contains(block, "--bit-bdg-dis-brd: var(--bit-bdg-clr-brd-dis);");
        Assert.IsFalse(block.Contains("--bit-Badge-"));

        // ...and the badge reads them ahead of those variables, so a re-tinted badge that is disabled still reads as
        // disabled, while the rule painting it keeps the weight of a single class a Classes.Badge class can compete with.
        StringAssert.Contains(stylesheet, "color: var(--bit-bdg-dis-txt, var(--bit-Badge-color, ");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-bdg-dis-bg, var(--bit-Badge-background, ");
        StringAssert.Contains(stylesheet, "border-color: var(--bit-bdg-dis-brd, var(--bit-Badge-border-color, ");
        Assert.IsFalse(stylesheet.Contains("\n.bit-bdg.bit-dis .bit-bdg-ctn {"), "The disabled colors are set by a heavier rule of their own.");
    }

    [TestMethod]
    public void BitBadgeShouldHandTheDotToTheBadgeWithoutOutweighingAClassOfItsOwn()
    {
        var stylesheet = ReadStylesheet();

        // A dot is a circle of its own size whatever the public height, padding and radius say, read first by the one
        // rule painting the badge rather than set by a heavier rule, so a Classes.Badge class can still re-size it.
        StringAssert.Contains(stylesheet, "height: var(--bit-bdg-dot-size, var(--bit-Badge-height, ");
        StringAssert.Contains(stylesheet, "padding: var(--bit-bdg-dot-padding, var(--bit-Badge-padding, ");
        StringAssert.Contains(stylesheet, "border-radius: var(--bit-bdg-dot-radius, var(--bit-Badge-radius, ");
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

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Notifications", "Badge", "BitBadge.scss");

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
