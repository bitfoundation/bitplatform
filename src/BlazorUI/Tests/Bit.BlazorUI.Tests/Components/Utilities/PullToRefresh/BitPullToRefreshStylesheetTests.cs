using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.PullToRefresh;

/// <summary>
/// Pins the public --bit-PullToRefresh-* variables and the rules of the stylesheet a bUnit render cannot see: every
/// variable the header documents is read with a fallback, none of them is ever declared (so they keep inheriting from
/// :root and the ancestors), nothing is read that the header does not document, and the release cue, the color
/// precedence, the motion tokens and the forced-colors mode behave as documented.
/// </summary>
[TestClass]
public partial class BitPullToRefreshStylesheetTests
{
    [TestMethod]
    public void BitPullToRefreshShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitPullToRefreshShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitPullToRefreshShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-PullToRefresh-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitPullToRefreshShouldLetTheColorParametersWinOverTheColorVariable()
    {
        var stylesheet = ReadStylesheet();

        // Color and CustomColor write --bit-ptr-color inline on the root; the root resets it, so a nested instance
        // does not inherit the color its host was given.
        StringAssert.Contains(Block(stylesheet, "\n.bit-ptr-spn {"), "color: var(--bit-ptr-color, var(--bit-PullToRefresh-color, #{$clr-fg-pri}));");
        StringAssert.Contains(Block(stylesheet, "\n.bit-ptr {"), "--bit-ptr-color: initial;");
    }

    [TestMethod]
    public void BitPullToRefreshShouldSizeTheIndicatorFromTheVariablesTimesTheProgress()
    {
        var stylesheet = ReadStylesheet();

        var disc = Block(stylesheet, "\n.bit-ptr-spw {");
        StringAssert.Contains(disc, "width: calc(var(--bit-PullToRefresh-indicator-size, #{$siz-ctrl-md}) * var(--bit-ptr-prg, 0));");
        StringAssert.Contains(disc, "height: calc(var(--bit-PullToRefresh-indicator-size, #{$siz-ctrl-md}) * var(--bit-ptr-prg, 0));");

        var glyph = Block(stylesheet, "\n.bit-ptr-spn {");
        StringAssert.Contains(glyph, "width: calc(var(--bit-PullToRefresh-glyph-size, #{$siz-icon-lg}) * var(--bit-ptr-prg, 0));");
        StringAssert.Contains(glyph, "height: calc(var(--bit-PullToRefresh-glyph-size, #{$siz-icon-lg}) * var(--bit-ptr-prg, 0));");
    }

    [TestMethod]
    public void BitPullToRefreshShouldDrawThePullFromWhatTheScriptWrites()
    {
        var stylesheet = ReadStylesheet();

        // The script writes the offset and the turn on the strip as the finger moves; the parts only read them.
        StringAssert.Contains(Block(stylesheet, "\n.bit-ptr-spw {"), "margin-top: var(--bit-ptr-off, 0px);");
        StringAssert.Contains(Block(stylesheet, "\n.bit-ptr-spn {"), "transform: rotate(var(--bit-ptr-rot, 0deg));");

        // A released pull (held by the script) and a running or complete refresh (rendered by the component) both pin
        // the indicator at its full size, so neither round trip in between draws it at the size the pull last had.
        var held = Block(stylesheet, "\n.bit-ptr-hld .bit-ptr-spw,\n.bit-ptr-swr,\n.bit-ptr-cmp {");
        StringAssert.Contains(held, "--bit-ptr-prg: 1;");
        StringAssert.Contains(held, "--bit-ptr-off: 0px;");
        StringAssert.Contains(held, "--bit-ptr-rot: 0deg;");
    }

    [TestMethod]
    public void BitPullToRefreshShouldFadeTheGlyphUntilReleasingWouldRefresh()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(Block(stylesheet, "\n.bit-ptr-spn {"), "opacity: var(--bit-PullToRefresh-pull-opacity, 0.6);");
        StringAssert.Contains(stylesheet, ".bit-ptr-crl,\n.bit-ptr-swr,\n.bit-ptr-cmp {\n    .bit-ptr-spn {\n        opacity: 1;");
    }

    [TestMethod]
    public void BitPullToRefreshShouldTakeItsShapeAndElevationFromTheTheme()
    {
        var disc = Block(ReadStylesheet(), "\n.bit-ptr-spw {");

        StringAssert.Contains(disc, "border-radius: var(--bit-PullToRefresh-indicator-radius, #{$shp-radius-full});");
        StringAssert.Contains(disc, "box-shadow: var(--bit-PullToRefresh-indicator-shadow, #{$box-shadow-popup});");
        StringAssert.Contains(disc, "background-color: var(--bit-PullToRefresh-indicator-background, #{$clr-bg-pri});");
    }

    [TestMethod]
    public void BitPullToRefreshShouldUseTheMotionTokens()
    {
        var stylesheet = ReadStylesheet();

        // A literal easing would not follow a preset's motion, and a literal duration would not collapse under
        // reduced motion.
        Assert.IsFalse(Regex.IsMatch(RulesOf(stylesheet), @"\b(linear|ease|ease-in|ease-out|ease-in-out|cubic-bezier)\b"), "A literal easing is used.");
        Assert.IsFalse(Regex.IsMatch(RulesOf(stylesheet), @"transition:[^;]*\d+m?s\b"), "A literal duration is used.");
    }

    [TestMethod]
    public void BitPullToRefreshShouldGiveTheDiscAnEdgeInForcedColors()
    {
        var block = Block(ReadStylesheet(), "\n@media (forced-colors: active) {");

        StringAssert.Contains(block, ".bit-ptr-spw {\n        border: calc(1px * var(--bit-ptr-prg, 0)) solid CanvasText;");
        StringAssert.Contains(block, ".bit-ptr-crl {\n        border-color: Highlight;");
    }

    [TestMethod]
    public void BitPullToRefreshShouldListEveryPublicVariableOnItsDemoPage()
    {
        var demo = ReadDemoPage();

        var listed = Regex.Matches(demo, @"Name = ""(--bit-PullToRefresh-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(DocumentedVariables(ReadStylesheet()), listed);
    }

    [TestMethod]
    public void BitPullToRefreshShouldKeepAMousePullFromSelectingText()
    {
        var block = Block(ReadStylesheet(), "\n.bit-ptr-drg {");

        StringAssert.Contains(block, "user-select: none;");
        StringAssert.Contains(block, "-webkit-user-select: none;");
        StringAssert.Contains(block, "cursor: grabbing;");
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

    // The comments are where the variables are documented, so only the rules are searched.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Utilities", "PullToRefresh", "BitPullToRefresh.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static string ReadDemoPage([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components",
                                                 "Utilities", "PullToRefresh", "BitPullToRefreshDemo.razor.cs"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path);
    }

    [GeneratedRegex(@"^//\s+(--bit-PullToRefresh-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-PullToRefresh-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-PullToRefresh-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
