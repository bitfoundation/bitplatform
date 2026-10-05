using System.Linq;
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
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Label-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitLabelShouldInheritTheColorOfItsContainerByDefault()
    {
        StringAssert.Contains(ReadStylesheet(), "color: var(--bit-Label-color, inherit);");
    }

    [TestMethod]
    public void BitLabelShouldTakeItsWeightFromTheFieldLabelToken()
    {
        // The theme's field-label weight is what the inputs caption themselves with, so reading it keeps a standalone
        // label in step with them under every preset rather than pinning the Fluent semibold.
        StringAssert.Contains(ReadStylesheet(), "font-weight: var(--bit-Label-font-weight, #{$tg-field-label-font-weight});");
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
    public void BitLabelShouldKeepTheDisabledStateInForcedColors()
    {
        var block = Block(ReadStylesheet(), "\n@media (forced-colors: active) {");

        StringAssert.Contains(block, ".bit-lbl.bit-dis {\n        color: GrayText;");
    }

    [TestMethod]
    public void BitLabelShouldDrawTheFocusRingOfTheLibrary()
    {
        var block = Block(ReadStylesheet(), "\n    &:focus-visible {");

        // The mixin draws the themed ring and brings a Highlight outline back in a forced-colors mode.
        StringAssert.Contains(block, "@include focus-ring(var(--bit-Label-focus-color, #{$clr-pri-focus}));");
    }

    [TestMethod]
    public void BitLabelShouldBringAVisuallyHiddenLabelBackWhileTheFocusIsInsideIt()
    {
        var stylesheet = ReadStylesheet();

        // The clipping is only lifted while a control inside the label has the focus, so a control it wraps is never
        // focused out of sight, while a label focused itself stays hidden. The condition is inside :where(), so the
        // rule keeps the weight of its one class and a Class of the page still overrides it.
        StringAssert.Contains(stylesheet, ".bit-lbl-vhd:where(:not(:focus-within), :focus) {");
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"\.bit-lbl-vhd\s*\{"), "The visually hidden label is clipped regardless of the focus.");
        Assert.IsFalse(stylesheet.Contains(".bit-lbl-vhd:not("), "The focus condition raises the specificity of the rule.");
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

    [TestMethod]
    public void BitLabelShouldTruncateTheContentAndKeepTheIndicatorWhole()
    {
        var stylesheet = ReadStylesheet();

        // The label keeps its own display, so its text-align still positions the content and a display set through
        // the style still truncates; it clips whatever overflows it.
        var noWrap = Block(stylesheet, "\n.bit-lbl-nwr {");
        Assert.IsFalse(noWrap.Contains("display:"), "The label is turned into a flex container, which ignores its text-align.");
        StringAssert.Contains(noWrap, "overflow: hidden;");
        StringAssert.Contains(noWrap, "white-space: nowrap;");
        StringAssert.Contains(noWrap, ".bit-lbl-rqi,\n    .bit-lbl-opi {\n        flex-shrink: 0;");
        Assert.IsFalse(noWrap.Contains("text-overflow"), "The ellipsis is put on the whole label, indicator included.");

        // The content and its indicator are an inline row no wider than the label, lined up on their text baseline
        // as the wrapping label lines them up.
        var row = Block(stylesheet, "\n.bit-lbl-row {");
        StringAssert.Contains(row, "display: inline-flex;");
        StringAssert.Contains(row, "max-width: 100%;");
        StringAssert.Contains(row, "align-items: baseline;");

        var text = Block(stylesheet, "\n.bit-lbl-txt {");
        StringAssert.Contains(text, "min-width: 0;");
        StringAssert.Contains(text, "overflow: hidden;");
        StringAssert.Contains(text, "text-overflow: ellipsis;");
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

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Utilities", "Label", "BitLabel.scss");

    [GeneratedRegex(@"^//\s+(--bit-Label-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Label-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Label-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
