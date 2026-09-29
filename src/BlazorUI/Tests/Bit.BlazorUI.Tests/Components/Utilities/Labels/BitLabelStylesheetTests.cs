using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Labels;

/// <summary>
/// Pins the public --bit-Label-* variables and the accessibility rules of the stylesheet, which a bUnit render cannot
/// see: every variable the header documents is read with a fallback, none of them is ever declared (so they keep
/// inheriting from :root and the ancestors), nothing is read that the header does not document, and the parameters,
/// the disabled state, the forced-colors mode and the visually hidden label behave as documented.
/// </summary>
[TestClass]
public partial class BitLabelStylesheetTests
{
    [TestMethod]
    public void BitLabelShouldReadEveryPublicVariableItDocuments()
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
    public void BitLabelShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitLabelShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Label-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitLabelShouldInheritTheColorOfItsContainerByDefault()
    {
        StringAssert.Contains(ReadStylesheet(), "color: var(--bit-Label-color, inherit);");
    }

    [TestMethod,
        DataRow("sm"),
        DataRow("md"),
        DataRow("lg")]
    public void BitLabelShouldLetTheSizeWinOverTheFontSizeVariable(string size)
    {
        var block = Block(ReadStylesheet(), $"\n.bit-lbl-{size} {{");

        StringAssert.Contains(block, "font-size: $tg-fs-");
        Assert.IsFalse(block.Contains("--bit-Label-"), $"The {size} size reads a public variable.");
    }

    [TestMethod]
    public void BitLabelShouldPaintTheColorsInTheReadableForegroundOfTheirRole()
    {
        var block = Block(ReadStylesheet(), "\n    .bit-lbl-#{$role} {");

        StringAssert.Contains(block, "color: role($tokens, fg);");
        Assert.IsFalse(block.Contains("--bit-Label-"), "The Color classes read a public variable.");
    }

    [TestMethod]
    public void BitLabelShouldKeepADisabledLabelInTheDisabledColor()
    {
        var block = Block(ReadStylesheet(), "\n    &.bit-dis {");

        StringAssert.Contains(block, "color: $clr-fg-dis;");
        Assert.IsFalse(block.Contains("--bit-Label-"));
    }

    [TestMethod]
    public void BitLabelShouldDimTheIndicatorsOfADisabledLabel()
    {
        var block = Block(ReadStylesheet(), "\n    &.bit-dis {");

        // Inheriting rather than naming the disabled color is what also carries the GrayText of a forced-colors mode.
        StringAssert.Contains(block, ".bit-lbl-rqi,\n        .bit-lbl-opi {\n            color: inherit;");
    }

    [TestMethod]
    public void BitLabelShouldKeepTheDisabledAndFocusedStatesInForcedColors()
    {
        var block = Block(ReadStylesheet(), "\n@media (forced-colors: active) {");

        StringAssert.Contains(block, ".bit-lbl.bit-dis {\n        color: GrayText;");
        StringAssert.Contains(block, ".bit-lbl:focus-visible {\n        outline-color: Highlight;");
    }

    [TestMethod]
    public void BitLabelShouldBringAVisuallyHiddenLabelBackWhileTheFocusIsInsideIt()
    {
        var stylesheet = ReadStylesheet();

        // The clipping is only applied while nothing inside the label has the focus, so a control it wraps is never
        // focused out of sight.
        StringAssert.Contains(stylesheet, ".bit-lbl-vhd:not(:focus-within) {");
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"\.bit-lbl-vhd\s*\{"), "The visually hidden label is clipped regardless of the focus.");
    }

    [TestMethod]
    public void BitLabelShouldKeepTheIndicatorGapLogical()
    {
        var stylesheet = ReadStylesheet();

        foreach (var indicator in new[] { "rqi", "opi" })
        {
            var block = Block(stylesheet, $"\n.bit-lbl-{indicator} {{");

            StringAssert.Contains(block, "margin-inline-start: var(--bit-Label-indicator-gap, ");
        }
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

    // The header comment is where the variables are documented, so only what follows it is searched for declarations.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Utilities", "Label", "BitLabel.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-Label-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Label-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Label-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
