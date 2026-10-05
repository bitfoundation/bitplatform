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

        Assert.AreEqual(33, documented.Length, "The stylesheet does not document the thirty-three public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
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
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
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
        Assert.IsFalse(body.Contains("Consolas"), "A literal monospace stack is used instead of $tg-font-family-mono.");
        Assert.IsFalse(body.Contains("z-index: 9999"), "A literal z-index is used instead of a z-index token.");
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
        var root = Block(ReadStylesheet(), "\n.bit-rte {");

        // Clip cuts the corners like hidden but makes no scroll container, which is what lets a StickyToolbar stick
        // to the page rather than to this frame.
        StringAssert.Contains(root, "overflow: clip;");
    }

    [TestMethod]
    public void BitRichTextEditorStickyToolbarShouldPinBelowTheOffset()
    {
        var sticky = Block(ReadStylesheet(), "\n.bit-rte-stk > .bit-rte-tlb {");

        StringAssert.Contains(sticky, "position: sticky;");
        StringAssert.Contains(sticky, "top: var(--bit-RichTextEditor-toolbar-sticky-offset, 0);");
    }

    [TestMethod]
    public void BitRichTextEditorFullScreenShouldLiftTheSizeCaps()
    {
        var fullScreen = Block(ReadStylesheet(), "\n.bit-rte-fsc .bit-rte-edt,");

        StringAssert.Contains(fullScreen, "max-height: none;");
        StringAssert.Contains(Block(ReadStylesheet(), "\n.bit-rte-fsc {"), "z-index: $zindex-modal;");
    }

    [TestMethod]
    public void BitRichTextEditorShouldKeepItsStatesInForcedColors()
    {
        var forced = Block(ReadStylesheet(), "\n@media (forced-colors: active) {");

        StringAssert.Contains(forced, "background: Highlight;");
        StringAssert.Contains(forced, "background: Mark;");
        StringAssert.Contains(forced, "color: GrayText;");
    }

    [TestMethod]
    public void BitRichTextEditorShouldDrawItsFocusInTheFocusColor()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "$rte-focus: var(--bit-RichTextEditor-focus-color, #{$clr-pri-focus});");
        StringAssert.Contains(Block(stylesheet, "\n.bit-rte-btn {"), "@include focus-ring($rte-focus);");
        StringAssert.Contains(Block(stylesheet, "\n.bit-rte-edt {"), "@include rte-inset-ring;");
    }

    [TestMethod]
    public void BitRichTextEditorResizeHandleShouldBeStyledByTheStylesheet()
    {
        var handle = Block(ReadStylesheet(), "\n.bit-rte-resize-handle {");

        StringAssert.Contains(handle, "background: $clr-pri;");
        StringAssert.Contains(handle, "touch-action: none;");
        StringAssert.Contains(handle, "z-index: calc(#{$zindex-modal} + 1);");
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
