using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// Pins the public --bit-PdfViewer-* variables and the rules of the stylesheet a bUnit render cannot see: every
/// variable the header documents is read with a fallback and listed on the demo page, none of them is ever declared
/// (so they keep inheriting from :root), nothing is read that the header does not document, every interactive part
/// draws a themed focus indicator, and the document survives a forced palette.
/// </summary>
[TestClass]
public partial class BitPdfViewerStylesheetTests
{
    [TestMethod]
    public void BitPdfViewerShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.HasCount(39, documented, "The stylesheet does not document the thirty-nine public variables.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}, ", stylesheet, $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitPdfViewerShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitPdfViewerShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-PdfViewer-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitPdfViewerDemoShouldListEveryPublicVariable()
    {
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "PdfViewer", "BitPdfViewerDemo.razor.cs");

        foreach (var name in DocumentedVariables(ReadStylesheet()))
        {
            Assert.Contains($"Name = \"{name}\"", demo, $"{name} is missing from the demo page's CSS variables table.");
        }

        Assert.HasCount(DocumentedVariables(ReadStylesheet()).Length, DemoVariable().Matches(demo), "The demo page lists a CSS variable the stylesheet does not document.");
    }

    [TestMethod]
    public void BitPdfViewerShouldDrawAThemedFocusIndicatorOnEveryInteractivePart()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("$pdv-focus: var(--bit-PdfViewer-focus-color, #{$clr-pri-focus});", stylesheet);
        Assert.Contains("$pdv-focus-ring: focus-ring-color($pdv-focus);", stylesheet);
        Assert.Contains("@include focus-ring-own($pdv-focus-ring);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-btn {"));
        Assert.Contains("@include focus-ring-own($pdv-focus-ring);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-opt {"));
        Assert.Contains("@include focus-ring-own($pdv-focus-ring);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-thumb {"));
        Assert.Contains("@include focus-ring-own($pdv-focus-ring);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-dialog-btn {"));
        Assert.Contains("@include focus-ring($pdv-focus, 0);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-search-input,"));

        // The parts that fill (or sit in) a scroll container draw their ring inside, where it cannot be clipped.
        Assert.Contains("@include pdv-inset-ring;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-surface {"));
        Assert.Contains("@include pdv-inset-ring;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-outline-node {"));
        Assert.Contains("@include pdv-inset-ring;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-attachment {"));
        Assert.Contains("@include pdv-inset-ring;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-layer {"));
        // A link of the document is transparent, and the page clips anything drawn outside it.
        Assert.Contains("@include pdv-inset-ring;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-page .bit-pdv-html-page a:focus-visible {"));
    }

    [TestMethod]
    public void BitPdfViewerDeterminateProgressShouldFillFromTheInlineStart()
    {
        var stylesheet = ReadStylesheet();

        var bar = SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-progress-bar.bit-pdv-det {");
        Assert.Contains("animation: none;", bar);
        Assert.Contains("transform-origin: left center;", bar);
        // Mirroring the track, rather than the determinate bar's origin alone, turns the indeterminate sweep too - in the
        // direction the track is laid out in, whatever made it right to left.
        Assert.Contains("transform: scaleX(-1);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-progress:is(:dir(rtl), .bit-pdv.bit-rtl > *) {"));
    }

    [TestMethod]
    public void BitPdfViewerRtlToolbarShouldKeepThePageAndZoomStepsLeftToRight()
    {
        // Page navigation and the zoom pair step through numbers, which read left to right in either direction:
        // previous and zoom out on the left, next and zoom in on the right.
        Assert.Contains("direction: ltr;", SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pdv-ltr {"));
    }

    [TestMethod]
    public void BitPdfViewerShouldKeepThePagesAndTheStatesUnderAForcedPalette()
    {
        var forced = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        // The page is a picture of the document: its invisible selection layer must stay invisible.
        Assert.Contains(".bit-pdv-page,", forced);
        Assert.Contains("forced-color-adjust: none;", forced);
        Assert.Contains("background: Highlight;", forced);
        Assert.Contains("color: GrayText;", forced);
    }

    [TestMethod]
    public void BitPdfViewerShouldPaintTheFindMatchesFromItsOwnStylesheet()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("::highlight(bit-pdv-search) {", stylesheet);
        Assert.Contains("::highlight(bit-pdv-search-current) {", stylesheet);

        // The script no longer injects a stylesheet of its own, which no variable could have reached.
        var script = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "PdfViewer", "BitPdfViewer.ts");
        Assert.DoesNotContain("::highlight(", script, "The script still injects the highlight colors.");
    }

    [TestMethod]
    public void BitPdfViewerFullscreenShouldOutrankTheSizeTheHostGave()
    {
        var stylesheet = ReadStylesheet();

        // The size parameters set the variables, so the fullscreen rule wins without !important.
        Assert.Contains("height: var(--bit-PdfViewer-height, ", SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv {"));
        var fullscreen = SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv:fullscreen {");
        Assert.Contains("height: 100%;", fullscreen);
        Assert.DoesNotContain("!important", fullscreen, "The fullscreen height still needs !important.");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "PdfViewer", "BitPdfViewer.scss");

    [GeneratedRegex(@"^//\s+(--bit-PdfViewer-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-PdfViewer-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-PdfViewer-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();

    [GeneratedRegex(@"Name = ""--bit-PdfViewer-[a-z-]+""")]
    private static partial Regex DemoVariable();
}
