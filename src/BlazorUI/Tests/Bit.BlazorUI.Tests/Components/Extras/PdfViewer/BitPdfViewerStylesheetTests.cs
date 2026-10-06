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

        Assert.AreEqual(38, documented.Length, "The stylesheet does not document the thirty-eight public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
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
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
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
            StringAssert.Contains(demo, $"Name = \"{name}\"", $"{name} is missing from the demo page's CSS variables table.");
        }

        Assert.AreEqual(DocumentedVariables(ReadStylesheet()).Length, DemoVariable().Matches(demo).Count,
                        "The demo page lists a CSS variable the stylesheet does not document.");
    }

    [TestMethod]
    public void BitPdfViewerShouldDrawAThemedFocusIndicatorOnEveryInteractivePart()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "$pdv-focus: var(--bit-PdfViewer-focus-color, #{$clr-pri-focus});");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-btn {"), "@include focus-ring($pdv-focus);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-opt {"), "@include focus-ring($pdv-focus);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-thumb {"), "@include focus-ring($pdv-focus);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-dialog-btn {"), "@include focus-ring($pdv-focus);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-search-input,"), "@include focus-ring($pdv-focus, 0);");

        // The parts that fill (or sit in) a scroll container draw their ring inside, where it cannot be clipped.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-surface {"), "@include pdv-inset-ring;");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-outline-node {"), "@include pdv-inset-ring;");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-attachment {"), "@include pdv-inset-ring;");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-layer {"), "@include pdv-inset-ring;");
        // A link of the document is transparent, and the page clips anything drawn outside it.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-page .bit-pdv-html-page a:focus-visible {"), "@include pdv-inset-ring;");
    }

    [TestMethod]
    public void BitPdfViewerDeterminateProgressShouldFillFromTheInlineStart()
    {
        var stylesheet = ReadStylesheet();

        var bar = SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv-progress-bar.bit-pdv-det {");
        StringAssert.Contains(bar, "animation: none;");
        StringAssert.Contains(bar, "transform-origin: left center;");
        // Mirroring the track, rather than the determinate bar's origin alone, turns the indeterminate sweep too.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-rtl .bit-pdv-progress {"), "transform: scaleX(-1);");
    }

    [TestMethod]
    public void BitPdfViewerRtlToolbarShouldKeepThePageAndZoomStepsLeftToRight()
    {
        // Page navigation and the zoom pair step through numbers, which read left to right in either direction:
        // previous and zoom out on the left, next and zoom in on the right.
        StringAssert.Contains(SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-pdv-ltr {"), "direction: ltr;");
    }

    [TestMethod]
    public void BitPdfViewerShouldKeepThePagesAndTheStatesUnderAForcedPalette()
    {
        var forced = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        // The page is a picture of the document: its invisible selection layer must stay invisible.
        StringAssert.Contains(forced, ".bit-pdv-page,");
        StringAssert.Contains(forced, "forced-color-adjust: none;");
        StringAssert.Contains(forced, "background: Highlight;");
        StringAssert.Contains(forced, "color: GrayText;");
    }

    [TestMethod]
    public void BitPdfViewerShouldPaintTheFindMatchesFromItsOwnStylesheet()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "::highlight(bit-pdv-search) {");
        StringAssert.Contains(stylesheet, "::highlight(bit-pdv-search-current) {");

        // The script no longer injects a stylesheet of its own, which no variable could have reached.
        var script = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "PdfViewer", "BitPdfViewer.ts");
        Assert.IsFalse(script.Contains("::highlight("), "The script still injects the highlight colors.");
    }

    [TestMethod]
    public void BitPdfViewerFullscreenShouldOutrankTheSizeTheHostGave()
    {
        var stylesheet = ReadStylesheet();

        // The size parameters set the variables, so the fullscreen rule wins without !important.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv {"), "height: var(--bit-PdfViewer-height, ");
        var fullscreen = SourceFiles.GetScssBlock(stylesheet, "\n.bit-pdv:fullscreen {");
        StringAssert.Contains(fullscreen, "height: 100%;");
        Assert.IsFalse(fullscreen.Contains("!important"), "The fullscreen height still needs !important.");
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
