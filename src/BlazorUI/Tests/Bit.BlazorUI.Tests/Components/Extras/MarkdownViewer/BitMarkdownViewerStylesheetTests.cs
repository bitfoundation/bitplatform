using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownViewer;

/// <summary>
/// Pins the public --bit-MarkdownViewer-* variables and the rules of the stylesheet a bUnit render cannot see: every
/// variable the header documents is read with a fallback and listed on the demo page, none of them is ever declared
/// (so they keep inheriting from :root), the element rules stop at a template's output, and what a background tells
/// apart survives forced colors.
/// </summary>
[TestClass]
public partial class BitMarkdownViewerStylesheetTests
{
    [TestMethod]
    public void BitMarkdownViewerShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.HasCount(36, documented, "The stylesheet does not document the thirty-six public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitMarkdownViewerShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-MarkdownViewer-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitMarkdownViewerDemoShouldListEveryPublicVariable()
    {
        var documented = DocumentedVariables(ReadStylesheet()).Order().ToArray();
        var listed = DemoVariable().Matches(ReadDemo()).Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

        CollectionAssert.AreEqual(documented, listed, "The demo page's CSS variables table and the stylesheet header disagree.");
    }

    [TestMethod]
    public void BitMarkdownViewerElementRulesShouldStopAtATemplatesOutput()
    {
        var stylesheet = ReadStylesheet();

        // One boundary, written once and used by every element rule, including the reset.
        StringAssert.Contains(stylesheet, "$_own: ':not(:where(:is(.bit-mdv-tpl, .math) *:not(.bit-mdv-tpl .bit-mdv, .bit-mdv-tpl .bit-mdv *)))';");
        StringAssert.Contains(stylesheet, "@include own('*:not(.bit-mdv)') {\n        all: revert;");

        // A bare descendant rule would reach into a template and restyle the components drawn there.
        Assert.IsFalse(Regex.IsMatch(RulesOf(stylesheet), @"^\s+(\*|a|p|pre|code|table|h[1-6]|ul|ol|li)\s*[,{]", RegexOptions.Multiline),
                       "An element is styled with a bare descendant rule, which reaches a template's output.");
        StringAssert.Contains(Block(stylesheet, "\n.bit-mdv {"), ".bit-mdv-tpl {\n        display: contents;");
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepLinksDistinguishableWithoutColor()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "text-decoration-line: var(--bit-MarkdownViewer-link-decoration, underline);");
        // The titles of the alerts are read on the page, so they take the role foregrounds rather than the fills.
        StringAssert.Contains(stylesheet, "var(--bit-MarkdownViewer-warning-color, #{$clr-wrn-fg})");
        Assert.IsFalse(stylesheet.Contains("color: $clr-wrn;"), "An alert title is painted in the warning fill, which is not legible on the page.");
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepWhatBackgroundsTellApartInForcedColors()
    {
        var stylesheet = ReadStylesheet();
        var forced = Block(stylesheet, "\n@media (forced-colors: active) {");

        StringAssert.Contains(forced, "outline: $shp-border-width solid transparent;");
        StringAssert.Contains(forced, "background-color: Mark;");

        // The rule is a border, which forced colors keep, rather than a background, which they paint over.
        StringAssert.Contains(stylesheet, "border-top: var(--bit-MarkdownViewer-rule-thickness, 0.25em) $shp-border-style var(--bit-MarkdownViewer-rule-color, #{$clr-brd-pri});");
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRingEveryFocusableItRenders()
    {
        var stylesheet = ReadStylesheet();

        // Links, code blocks, interactive task boxes, table scroll regions and the summaries of the collapsible containers.
        Assert.AreEqual(5, Regex.Matches(stylesheet, @"&:focus-visible \{\n\s+@include focus-ring;").Count);
    }

    private static string Block(string stylesheet, string opening)
    {
        var start = stylesheet.IndexOf(opening, System.StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, start, $"No rule opens with {opening.Trim()}.");

        var indent = opening[1..].Length - opening[1..].TrimStart().Length;
        var end = stylesheet.IndexOf("\n" + new string(' ', indent) + "}", start + opening.Length, System.StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    // The header comment is where the variables are documented, so only what is not a comment is searched.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        return Read(thisFile, "Bit.BlazorUI.Extras", "Components", "MarkdownViewer", "BitMarkdownViewer.scss");
    }

    private static string ReadDemo([CallerFilePath] string thisFile = "")
    {
        return Read(thisFile, "Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "MarkdownViewer", "BitMarkdownViewerDemo.razor.cs");
    }

    private static string Read(string thisFile, params string[] relative)
    {
        var path = Path.GetFullPath(Path.Combine([Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..", .. relative]));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-MarkdownViewer-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-MarkdownViewer-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-MarkdownViewer-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();

    [GeneratedRegex(@"Name = ""(--bit-MarkdownViewer-[a-z-]+)""")]
    private static partial Regex DemoVariable();
}
