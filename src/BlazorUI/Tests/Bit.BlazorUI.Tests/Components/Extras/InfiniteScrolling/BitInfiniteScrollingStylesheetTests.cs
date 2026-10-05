using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.InfiniteScrolling;

/// <summary>
/// Pins the public --bit-InfiniteScrolling-* variables and the rules of the stylesheet a bUnit render cannot see:
/// every variable the header documents is read with a fallback, none of them is ever declared (so they keep
/// inheriting from :root), nothing is read that the header does not document, the spinner slows down rather than
/// stops under reduced motion, and the list prints at the length of what it has loaded.
/// </summary>
[TestClass]
public partial class BitInfiniteScrollingStylesheetTests
{
    [TestMethod]
    public void BitInfiniteScrollingShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.AreEqual(16, documented.Length, "The stylesheet does not document the sixteen public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitInfiniteScrollingShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-InfiniteScrolling-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldTurnTheSpinnerWithTheSpinnerTokens()
    {
        var spinner = Block(ReadStylesheet(), "\n.bit-isc-spn {");

        // The spinner tokens slow a looping animation down under reduced motion instead of collapsing it to zero.
        StringAssert.Contains(spinner, "animation: bit-isc-spin $mot-duration-spinner $mot-easing-spinner infinite;");
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldKeepItsIndicatorsInForcedColors()
    {
        var forced = Block(ReadStylesheet(), "\n@media (forced-colors: active) {");

        StringAssert.Contains(forced, "border-block-start-color: CanvasText;");
        StringAssert.Contains(forced, "color: GrayText;");

        // A button with no box of its own would read as plain text in a forced palette.
        StringAssert.Contains(forced, "border: $shp-border-width $shp-border-style ButtonText;");

        // The ring of a feed's article becomes the system's own focus color.
        StringAssert.Contains(Block(ReadStylesheet(), "\n.bit-isc-art {"), "outline: #{$shp-focus-ring-width} solid Highlight;");
    }

    [TestMethod]
    public void BitInfiniteScrollingFeedArticleShouldDrawItsRingAboveItsContent()
    {
        var article = Block(ReadStylesheet(), "\n.bit-isc-art {");

        // An inset box-shadow is painted under the article's children, so an item with a background would hide it.
        Assert.IsFalse(article.Contains("box-shadow"), "The ring of an article is a box-shadow, which its content covers.");
        StringAssert.Contains(article, "outline-offset: calc(-1 * #{$shp-focus-ring-width});");
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldAlignTheLoadingBlockAsText()
    {
        var stylesheet = ReadStylesheet();

        // The text-align variable takes values (justify, match-parent) that a flex alignment rejects.
        Assert.IsFalse(stylesheet.Contains("justify-content: var(--bit-InfiniteScrolling-status-text-align"),
                       "The text-align variable is read as a flex alignment.");
        StringAssert.Contains(Block(stylesheet, "\n.bit-isc-spn {"), "display: inline-block;");
    }

    [TestMethod]
    public void BitInfiniteScrollingButtonShouldStayFocusableAndUnclippedWhileItLoads()
    {
        var stylesheet = ReadStylesheet();
        var button = Block(stylesheet, "\n.bit-isc-btn {");

        // The busy button is marked aria-disabled, never disabled, so the focus that pressed it is kept.
        StringAssert.Contains(button, "&[aria-disabled=\"true\"] {");
        Assert.IsFalse(button.Contains(":disabled"), "The busy button is styled through :disabled, which it never is.");

        // A box-shadow ring is not scrolled to, so the button keeps room for it at either end of the list.
        StringAssert.Contains(button, "margin-block: $isc-ring-room;");
        StringAssert.Contains(stylesheet, "$isc-ring-room: calc(#{$shp-focus-ring-offset} + #{$shp-focus-ring-width});");
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldPrintAtTheLengthOfItsItems()
    {
        var print = Block(ReadStylesheet(), "\n@media print {");

        StringAssert.Contains(print, "overflow: visible !important;");
        StringAssert.Contains(print, "max-height: none !important;");
        StringAssert.Contains(print, ".bit-isc-btn");
        StringAssert.Contains(print, "display: none !important;");
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

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "InfiniteScrolling", "BitInfiniteScrolling.scss");

    [GeneratedRegex(@"^//\s+(--bit-InfiniteScrolling-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-InfiniteScrolling-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-InfiniteScrolling-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
