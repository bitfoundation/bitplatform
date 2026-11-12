using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.RichTextEditor;

/// <summary>
/// Pins the public --bit-RichTextEditor-* variables and the rules of the stylesheet a bUnit render cannot see:
/// every variable the header documents is read with a fallback, none of them is ever declared (so they keep
/// inheriting from :root), nothing is read that the header does not document, nothing is hard-coded that a theme
/// should decide, the layout mirrors in RTL, a StickyToolbar can stick, and the states survive a forced palette.
/// </summary>
[TestClass]
public partial class BitRichTextEditorStylesheetTests
{
    [TestMethod]
    public void BitRichTextEditorShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.HasCount(33, documented, "The stylesheet does not document the thirty-three public variables.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}, ", stylesheet, $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitRichTextEditorShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitRichTextEditorShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-RichTextEditor-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitRichTextEditorShouldNotHardCodeWhatTheThemeDecides()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(HexColor().IsMatch(body), "A literal color is hard-coded instead of read from a theme token.");
        Assert.DoesNotContain("Consolas", body, "A literal monospace stack is used instead of $tg-font-family-mono.");
        Assert.DoesNotContain("z-index: 9999", body, "A literal z-index is used instead of a z-index token.");
        Assert.IsFalse(LiteralFontWeight().IsMatch(body), "A literal font weight is used instead of the weight ramp.");
    }

    [TestMethod]
    public void BitRichTextEditorShouldOnlyUseLogicalSidesSoItMirrorsInRtl()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(PhysicalSide().IsMatch(body), "A physical left/right property keeps the layout from mirroring in RTL.");
    }

    [TestMethod]
    public void BitRichTextEditorRootShouldClipWithoutBecomingAScrollContainer()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-rte {");

        // Clip cuts the corners like hidden but makes no scroll container, which is what lets a StickyToolbar stick
        // to the page rather than to this frame.
        Assert.Contains("overflow: clip;", root);
    }

    [TestMethod]
    public void BitRichTextEditorStickyToolbarShouldPinBelowTheOffset()
    {
        var sticky = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-rte-stk > .bit-rte-tlb {");

        Assert.Contains("position: sticky;", sticky);
        Assert.Contains("top: var(--bit-RichTextEditor-toolbar-sticky-offset, 0);", sticky);
    }

    [TestMethod]
    public void BitRichTextEditorFullScreenShouldLiftTheSizeCaps()
    {
        var fullScreen = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-rte-fsc .bit-rte-edt,");

        Assert.Contains("max-height: none;", fullScreen);
        Assert.Contains("z-index: $zindex-modal;", SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-rte-fsc {"));
    }

    [TestMethod]
    public void BitRichTextEditorShouldKeepItsStatesInForcedColors()
    {
        var forced = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        Assert.Contains("background: Highlight;", forced);
        Assert.Contains("background: Mark;", forced);
        Assert.Contains("color: GrayText;", forced);
    }

    [TestMethod]
    public void BitRichTextEditorShouldDrawItsFocusInTheFocusColor()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("$rte-focus: var(--bit-RichTextEditor-focus-color, #{$clr-pri-focus});", stylesheet);
        Assert.Contains("$rte-focus-ring: focus-ring-color($rte-focus);", stylesheet);
        Assert.Contains("@include focus-ring-own($rte-focus-ring);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-rte-btn {"));
        Assert.Contains("@include rte-inset-ring;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-rte-edt {"));
    }

    [TestMethod]
    public void BitRichTextEditorResizeHandleShouldBeStyledByTheStylesheet()
    {
        var handle = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-rte-resize-handle {");

        Assert.Contains("background: $clr-pri;", handle);
        Assert.Contains("touch-action: none;", handle);
        Assert.Contains("z-index: calc(#{$zindex-modal} + 1);", handle);
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "RichTextEditor", "BitRichTextEditor.scss");

    [GeneratedRegex(@"^//\s+(--bit-RichTextEditor-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-RichTextEditor-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-RichTextEditor-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();

    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b(?![{])")]
    private static partial Regex HexColor();

    [GeneratedRegex(@"font-weight:\s*\d")]
    private static partial Regex LiteralFontWeight();

    [GeneratedRegex(@"(^|\s)(margin|padding|border)-(left|right)\s*:|text-align:\s*(left|right)|(^|\s)(left|right)\s*:\s*\d", RegexOptions.Multiline)]
    private static partial Regex PhysicalSide();
}
