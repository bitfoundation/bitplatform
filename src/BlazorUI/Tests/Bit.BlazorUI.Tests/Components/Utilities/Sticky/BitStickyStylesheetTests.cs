using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Sticky;

/// <summary>
/// Pins the public --bit-Sticky-* variables and the rules of the stylesheet a bUnit render cannot see: every variable
/// the header documents is read with a fallback, none of them is ever declared (so they keep inheriting from :root and
/// the scrolling container), nothing is read that the header does not document, and the elevation, forced-colors and
/// print rules behave as documented.
/// </summary>
[TestClass]
public partial class BitStickyStylesheetTests
{
    [TestMethod]
    public void BitStickyShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.AreEqual(10, documented.Length, "The stylesheet does not document the ten public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitStickyShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitStickyShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Sticky-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitStickyShouldReadTheLegacyZIndexVariableOnlyBehindThePublicOne()
    {
        var stylesheet = ReadStylesheet();
        var root = Block(stylesheet, "\n.bit-stk {");

        // --bit-stk-zin was documented by earlier versions, so a stylesheet setting it keeps working, but the public
        // variable wins over it, and it is no longer declared on the root, which would shadow the public variable
        // inherited from an ancestor.
        StringAssert.Contains(root, "z-index: var(--bit-Sticky-z-index, var(--bit-stk-zin, 1));");
        Assert.IsFalse(root.Contains("--bit-stk-zin:"), "The legacy z-index variable is declared on the root.");

        // Declared on the root, it never reached the element from an ancestor; registered as a property that does
        // not inherit, a stale value left on :root still does not.
        StringAssert.Contains(Block(stylesheet, "\n@property --bit-stk-zin {"), "inherits: false;");
    }

    [TestMethod]
    public void BitStickyShouldReadTheOffsetVariablesOnTheMatchingLogicalEdges()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(Block(stylesheet, "\n.bit-stk-top {"), "inset-block-start: var(--bit-Sticky-offset-top, 0);");
        StringAssert.Contains(Block(stylesheet, "\n.bit-stk-btm {"), "inset-block-end: var(--bit-Sticky-offset-bottom, 0);");
        StringAssert.Contains(Block(stylesheet, "\n.bit-stk-srt {"), "inset-inline-start: var(--bit-Sticky-offset-start, 0);");
        StringAssert.Contains(Block(stylesheet, "\n.bit-stk-end {"), "inset-inline-end: var(--bit-Sticky-offset-end, 0);");

        var both = Block(stylesheet, "\n.bit-stk-tab {");
        StringAssert.Contains(both, "inset-block-start: var(--bit-Sticky-offset-top, 0);");
        StringAssert.Contains(both, "inset-block-end: var(--bit-Sticky-offset-bottom, 0);");

        var sides = Block(stylesheet, "\n.bit-stk-sae {");
        StringAssert.Contains(sides, "inset-inline-start: var(--bit-Sticky-offset-start, 0);");
        StringAssert.Contains(sides, "inset-inline-end: var(--bit-Sticky-offset-end, 0);");
    }

    [TestMethod]
    public void BitStickyShouldElevateFromTheThemeTokensOnlyWhileStuck()
    {
        var stylesheet = ReadStylesheet();
        var elevated = Block(stylesheet, "\n.bit-stk-elv {");

        // Every layer starts transparent on each elevated root, so a nested sticky never inherits a shadow.
        StringAssert.Contains(elevated, "--bit-stk-shd-top: 0 0 transparent;");
        StringAssert.Contains(elevated, "--bit-stk-shd-rgt: 0 0 transparent;");

        StringAssert.Contains(Block(stylesheet, "\n    &.bit-stk-stc {"), "box-shadow: var(--bit-stk-shd-top), var(--bit-stk-shd-btm), var(--bit-stk-shd-lft), var(--bit-stk-shd-rgt);");
        StringAssert.Contains(Block(stylesheet, "\n    &.bit-stk-stc-top {"), "var(--bit-Sticky-shadow-top, #{$box-shadow-appbar-top})");
        StringAssert.Contains(Block(stylesheet, "\n    &.bit-stk-stc-btm {"), "var(--bit-Sticky-shadow-bottom, #{$box-shadow-appbar-bottom})");
        StringAssert.Contains(Block(stylesheet, "\n    &.bit-stk-stc-lft {"), "var(--bit-Sticky-shadow-left, #{$box-shadow-card})");
        StringAssert.Contains(Block(stylesheet, "\n    &.bit-stk-stc-rgt {"), "var(--bit-Sticky-shadow-right, #{$box-shadow-card})");

        StringAssert.Contains(elevated, "transition: box-shadow $mot-duration $mot-easing;");
    }

    [TestMethod]
    public void BitStickyShouldLetABroughtBackgroundWinOverTheStuckSurface()
    {
        // :where() keeps the surface at zero specificity, below any Class the page gives the element.
        StringAssert.Contains(Block(ReadStylesheet(), "\n:where(.bit-stk-elv.bit-stk-stc) {"), "background-color: var(--bit-Sticky-background, #{$clr-bg-pri});");
    }

    [TestMethod]
    public void BitStickyShouldOutlineAPinnedElevatedStickyInForcedColors()
    {
        var forced = Block(ReadStylesheet(), "\n@media (forced-colors: active) {");

        StringAssert.Contains(forced, ".bit-stk-elv.bit-stk-stc {");
        StringAssert.Contains(forced, "outline: $shp-border-width $shp-border-style CanvasText;");
        // Drawn inside the box, so the pin flipping it on and off moves nothing.
        StringAssert.Contains(forced, "outline-offset: calc(-1 * #{$shp-border-width});");
    }

    [TestMethod]
    public void BitStickyShouldNotPinOnPaper()
    {
        var print = Block(ReadStylesheet(), "\n@media print {");

        StringAssert.Contains(print, "position: static;");
        StringAssert.Contains(print, "box-shadow: none;");
    }

    private static string Block(string stylesheet, string opening)
    {
        var start = stylesheet.IndexOf(opening, System.StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, $"No rule opens with {opening.Trim()}.");

        var indent = opening[1..].Length - opening[1..].TrimStart().Length;
        var end = stylesheet.IndexOf("\n" + new string(' ', indent) + "}", start + opening.Length, System.StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Utilities", "Sticky", "BitSticky.scss");

    [GeneratedRegex(@"^//\s+(--bit-Sticky-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Sticky-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Sticky-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
