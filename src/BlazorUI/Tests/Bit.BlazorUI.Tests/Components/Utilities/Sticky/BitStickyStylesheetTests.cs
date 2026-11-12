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

        Assert.HasCount(10, documented, "The stylesheet does not document the ten public variables.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}, ", stylesheet, $"{name} is documented but never read with a fallback.");
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
            Assert.Contains(name, documented, $"{name} is read but not documented in the header.");
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
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-stk {");

        // --bit-stk-zin was documented by earlier versions, so a stylesheet setting it keeps working, but the public
        // variable wins over it, and it is no longer declared on the root, which would shadow the public variable
        // inherited from an ancestor.
        Assert.Contains("z-index: var(--bit-Sticky-z-index, var(--bit-stk-zin, 1));", root);
        Assert.DoesNotContain("--bit-stk-zin:", root, "The legacy z-index variable is declared on the root.");

        // Declared on the root, it never reached the element from an ancestor; registered as a property that does
        // not inherit, a stale value left on :root still does not.
        Assert.Contains("inherits: false;", SourceFiles.GetScssBlock(stylesheet, "\n@property --bit-stk-zin {"));
    }

    [TestMethod]
    public void BitStickyShouldReadTheOffsetVariablesOnTheMatchingLogicalEdges()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("inset-block-start: var(--bit-Sticky-offset-top, 0);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-stk-top {"));
        Assert.Contains("inset-block-end: var(--bit-Sticky-offset-bottom, 0);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-stk-btm {"));
        Assert.Contains("inset-inline-start: var(--bit-Sticky-offset-start, 0);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-stk-srt {"));
        Assert.Contains("inset-inline-end: var(--bit-Sticky-offset-end, 0);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-stk-end {"));

        var both = SourceFiles.GetScssBlock(stylesheet, "\n.bit-stk-tab {");
        Assert.Contains("inset-block-start: var(--bit-Sticky-offset-top, 0);", both);
        Assert.Contains("inset-block-end: var(--bit-Sticky-offset-bottom, 0);", both);

        var sides = SourceFiles.GetScssBlock(stylesheet, "\n.bit-stk-sae {");
        Assert.Contains("inset-inline-start: var(--bit-Sticky-offset-start, 0);", sides);
        Assert.Contains("inset-inline-end: var(--bit-Sticky-offset-end, 0);", sides);
    }

    [TestMethod]
    public void BitStickyShouldElevateFromTheThemeTokensOnlyWhileStuck()
    {
        var stylesheet = ReadStylesheet();
        var elevated = SourceFiles.GetScssBlock(stylesheet, "\n.bit-stk-elv {");

        // Every layer starts transparent on each elevated root, so a nested sticky never inherits a shadow.
        Assert.Contains("--bit-stk-shd-top: 0 0 transparent;", elevated);
        Assert.Contains("--bit-stk-shd-rgt: 0 0 transparent;", elevated);

        Assert.Contains("box-shadow: var(--bit-stk-shd-top), var(--bit-stk-shd-btm), var(--bit-stk-shd-lft), var(--bit-stk-shd-rgt);", SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-stk-stc {"));
        Assert.Contains("var(--bit-Sticky-shadow-top, #{$box-shadow-appbar-top})", SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-stk-stc-top {"));
        Assert.Contains("var(--bit-Sticky-shadow-bottom, #{$box-shadow-appbar-bottom})", SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-stk-stc-btm {"));
        Assert.Contains("var(--bit-Sticky-shadow-left, #{$box-shadow-card})", SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-stk-stc-lft {"));
        Assert.Contains("var(--bit-Sticky-shadow-right, #{$box-shadow-card})", SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-stk-stc-rgt {"));

        Assert.Contains("transition: box-shadow $mot-duration $mot-easing;", elevated);
    }

    [TestMethod]
    public void BitStickyShouldLetABroughtBackgroundWinOverTheStuckSurface()
    {
        // :where() keeps the surface at zero specificity, below any Class the page gives the element.
        Assert.Contains("background-color: var(--bit-Sticky-background, #{$clr-bg-pri});", SourceFiles.GetScssBlock(ReadStylesheet(), "\n:where(.bit-stk-elv.bit-stk-stc) {"));
    }

    [TestMethod]
    public void BitStickyShouldOutlineAPinnedElevatedStickyInForcedColors()
    {
        var forced = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        Assert.Contains(".bit-stk-elv.bit-stk-stc {", forced);
        Assert.Contains("outline: $shp-border-width $shp-border-style CanvasText;", forced);
        // Drawn inside the box, so the pin flipping it on and off moves nothing.
        Assert.Contains("outline-offset: calc(-1 * #{$shp-border-width});", forced);
    }

    [TestMethod]
    public void BitStickyShouldNotPinOnPaper()
    {
        var print = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media print {");

        Assert.Contains("position: static;", print);
        Assert.Contains("box-shadow: none;", print);
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
