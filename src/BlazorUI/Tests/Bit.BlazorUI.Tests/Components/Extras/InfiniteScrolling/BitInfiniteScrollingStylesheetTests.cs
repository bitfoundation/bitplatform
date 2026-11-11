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

        Assert.HasCount(16, documented, "The stylesheet does not document the sixteen public variables.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}, ", stylesheet, $"{name} is documented but never read with a fallback.");
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
            Assert.Contains(name, documented, $"{name} is read but not documented in the header.");
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
        var spinner = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-isc-spn {");

        // The shared ring turns on the spinner tokens, which slow a looping animation down under reduced motion
        // instead of collapsing it to zero (BitSpinnerRingStylesheetTests pins the mixin itself).
        Assert.Contains("@include spinner-ring(", spinner);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldKeepItsIndicatorsInForcedColors()
    {
        var forced = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        // The spinner's track and arc are kept apart by the spinner-ring mixin it is drawn with.
        Assert.Contains("@include spinner-ring(", SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-isc-spn {"));
        Assert.Contains("color: GrayText;", forced);

        // A button with no box of its own would read as plain text in a forced palette.
        Assert.Contains("border: $shp-border-width $shp-border-style ButtonText;", forced);

        // The ring of a feed's article becomes the system's own focus color (focus-ring-inset does it).
        Assert.Contains("@include focus-ring-inset(", SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-isc-art {"));
    }

    [TestMethod]
    public void BitInfiniteScrollingFeedArticleShouldDrawItsRingAboveItsContent()
    {
        var article = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-isc-art {");

        // An inset box-shadow is painted under the article's children, so an item with a background would hide it.
        Assert.DoesNotContain("box-shadow", article, "The ring of an article is a box-shadow, which its content covers.");
        Assert.Contains("@include focus-ring-inset(var(--bit-InfiniteScrolling-item-focus-color, #{$clr-pri-focus}));", article);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldAlignTheLoadingBlockAsText()
    {
        var stylesheet = ReadStylesheet();

        // The text-align variable takes values (justify, match-parent) that a flex alignment rejects.
        Assert.DoesNotContain("justify-content: var(--bit-InfiniteScrolling-status-text-align", stylesheet, "The text-align variable is read as a flex alignment.");
        Assert.Contains("display: inline-block;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-isc-spn {"));
    }

    [TestMethod]
    public void BitInfiniteScrollingButtonShouldStayFocusableAndUnclippedWhileItLoads()
    {
        var stylesheet = ReadStylesheet();
        var button = SourceFiles.GetScssBlock(stylesheet, "\n.bit-isc-btn {");

        // The busy button is marked aria-disabled, never disabled, so the focus that pressed it is kept.
        Assert.Contains("&[aria-disabled=\"true\"] {", button);
        Assert.DoesNotContain(":disabled", button, "The busy button is styled through :disabled, which it never is.");

        // A box-shadow ring is not scrolled to, so the button keeps room for it at either end of the list.
        Assert.Contains("margin-block: $isc-ring-room;", button);
        Assert.Contains("$isc-ring-room: calc(#{$shp-focus-ring-offset} + #{$shp-focus-ring-width});", stylesheet);
    }

    [TestMethod]
    public void BitInfiniteScrollingShouldPrintAtTheLengthOfItsItems()
    {
        var print = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media print {");

        Assert.Contains("overflow: visible !important;", print);
        Assert.Contains("max-height: none !important;", print);
        Assert.Contains(".bit-isc-btn", print);
        Assert.Contains("display: none !important;", print);
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
