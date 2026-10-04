using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownEditor;

/// <summary>
/// Pins the public --bit-MarkdownEditor-* variables and the rules of the stylesheet a bUnit render cannot see:
/// every variable the header documents is read with a fallback, none of them is ever declared (so they keep
/// inheriting from :root), nothing is read that the header does not document, the root lets a menu hang out of a
/// short editor, the split panes stack on a narrow body, and the states survive a forced palette.
/// </summary>
[TestClass]
public partial class BitMarkdownEditorStylesheetTests
{
    [TestMethod]
    public void BitMarkdownEditorShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.AreEqual(30, documented.Length, "The stylesheet does not document the thirty public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitMarkdownEditorShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitMarkdownEditorShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-MarkdownEditor-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitMarkdownEditorRootShouldNotClipTheToolbarMenus()
    {
        var root = Block(ReadStylesheet(), "\n.bit-mde {");

        Assert.IsFalse(root.Contains("overflow"), "The root clips its children, which cuts a toolbar menu off at the bottom of a short editor.");

        // The parts that paint a background round the corners they share with the root instead.
        StringAssert.Contains(ReadStylesheet(), ".bit-mde > .bit-mde-tlb:first-child,");
        StringAssert.Contains(ReadStylesheet(), "border-start-start-radius: inherit;");
        StringAssert.Contains(ReadStylesheet(), "border-end-end-radius: inherit;");
    }

    [TestMethod]
    public void BitMarkdownEditorShouldStackTheSplitPanesOnANarrowBody()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(Block(stylesheet, "\n.bit-mde-bdy {"), "flex-wrap: wrap;");
        StringAssert.Contains(Block(stylesheet, "\n.bit-mde-pne {"), "flex: 1 1 var(--bit-MarkdownEditor-pane-min-width, 18rem);");

        // The textarea fills its pane by flex, since a wrapped body leaves a percentage height unresolved.
        var textArea = Block(stylesheet, "\n.bit-mde-txa {");
        StringAssert.Contains(textArea, "flex: 1 1 auto;");
        Assert.IsFalse(textArea.Contains("height: 100%"), "The textarea still fills its pane by a percentage height.");
    }

    [TestMethod]
    public void BitMarkdownEditorAutoHeightShouldLeaveTheBodyToTheScript()
    {
        var autoHeight = Block(ReadStylesheet(), "\n.bit-mde-ahg .bit-mde-bdy {");

        StringAssert.Contains(autoHeight, "resize: none;");
        StringAssert.Contains(autoHeight, "height: auto;");

        // Full-screen comes after it, so a full-screen editor still fills the viewport.
        Assert.IsTrue(ReadStylesheet().IndexOf("\n.bit-mde-fsc .bit-mde-bdy {", System.StringComparison.Ordinal) >
                      ReadStylesheet().IndexOf("\n.bit-mde-ahg .bit-mde-bdy {", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitMarkdownEditorStickyToolbarShouldPinBelowTheOffset()
    {
        var stylesheet = ReadStylesheet();

        var sticky = Block(stylesheet, "\n.bit-mde-stk > .bit-mde-tlb {");
        StringAssert.Contains(sticky, "position: sticky;");
        StringAssert.Contains(sticky, "top: var(--bit-MarkdownEditor-toolbar-sticky-offset, 0);");

        // Full-screen gives the editor the viewport, so there is nothing to stick to there.
        StringAssert.Contains(Block(stylesheet, "\n.bit-mde-fsc.bit-mde-stk > .bit-mde-tlb {"), "position: static;");
    }

    [TestMethod]
    public void BitMarkdownEditorShouldKeepItsStatesInForcedColors()
    {
        var forced = Block(ReadStylesheet(), "\n@media (forced-colors: active) {");

        StringAssert.Contains(forced, "background: Highlight;");
        StringAssert.Contains(forced, "color: GrayText;");
        StringAssert.Contains(forced, "background: CanvasText;");
    }

    [TestMethod]
    public void BitMarkdownEditorShouldDrawItsFocusInTheFocusColor()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "$mde-focus: var(--bit-MarkdownEditor-focus-color, #{$clr-pri-focus});");
        StringAssert.Contains(Block(stylesheet, "\n.bit-mde-btn {"), "@include focus-ring($mde-focus);");
        StringAssert.Contains(Block(stylesheet, "\n.bit-mde-txa {"), "@include mde-inset-ring;");
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

    // The header comment is where the variables are documented, so only what is not a comment is searched for declarations.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI.Extras", "Components", "MarkdownEditor", "BitMarkdownEditor.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-MarkdownEditor-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-MarkdownEditor-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-MarkdownEditor-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
