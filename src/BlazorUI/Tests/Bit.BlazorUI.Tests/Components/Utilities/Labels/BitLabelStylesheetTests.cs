using System;
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

        Assert.IsNotEmpty(documented, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            // The focus color alone is read without a fallback, on purpose: its absence is what hands the ring over
            // to the global --bit-shd-focus-ring (see BitFocusRingStylesheetTests).
            var read = name.EndsWith("-focus-color", StringComparison.Ordinal) ? $"var({name})" : $"var({name}, ";

            Assert.Contains(read, stylesheet, $"{name} is documented but never read as it should be.");
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
            Assert.Contains(name, documented, $"{name} is read but not documented in the header.");
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
        Assert.Contains("color: var(--bit-Label-color, inherit);", ReadStylesheet());
    }

    [TestMethod]
    public void BitLabelShouldTakeItsWeightFromTheFieldLabelToken()
    {
        // The theme's field-label weight is what the inputs caption themselves with, so reading it keeps a standalone
        // label in step with them under every preset rather than pinning the Fluent semibold.
        Assert.Contains("font-weight: var(--bit-Label-font-weight, #{$tg-field-label-font-weight});", ReadStylesheet());
    }

    [TestMethod,
        DataRow("sm"),
        DataRow("md"),
        DataRow("lg")]
    public void BitLabelShouldLetTheSizeWinOverTheFontSizeVariable(string size)
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), $"\n.bit-lbl-{size} {{");

        Assert.Contains("font-size: $tg-fs-", block);
        Assert.DoesNotContain("--bit-Label-", block, $"The {size} size reads a public variable.");
    }

    [TestMethod]
    public void BitLabelShouldPaintTheColorsInTheReadableForegroundOfTheirRole()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    .bit-lbl-#{$role} {");

        Assert.Contains("color: role($tokens, fg);", block);
        Assert.DoesNotContain("--bit-Label-", block, "The Color classes read a public variable.");
    }

    [TestMethod]
    public void BitLabelShouldKeepADisabledLabelInTheDisabledColor()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    &.bit-dis {");

        Assert.Contains("color: $clr-fg-dis;", block);
        Assert.DoesNotContain("--bit-Label-", block);
    }

    [TestMethod]
    public void BitLabelShouldDimTheIndicatorsOfADisabledLabel()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    &.bit-dis {");

        // Inheriting rather than naming the disabled color is what also carries the GrayText of a forced-colors mode.
        Assert.Contains(".bit-lbl-rqi,\n        .bit-lbl-opi {\n            color: inherit;", block);
    }

    [TestMethod]
    public void BitLabelShouldKeepTheDisabledStateInForcedColors()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        Assert.Contains(".bit-lbl.bit-dis {\n        color: GrayText;", block);
    }

    [TestMethod]
    public void BitLabelShouldDrawTheFocusRingOfTheLibrary()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    &:focus-visible {");

        // The mixin draws the themed ring and brings a Highlight outline back in a forced-colors mode.
        Assert.Contains("@include focus-ring-own(var(--bit-Label-focus-color));", block);
    }

    [TestMethod]
    public void BitLabelShouldBringAVisuallyHiddenLabelBackWhileTheFocusIsInsideIt()
    {
        var stylesheet = ReadStylesheet();

        // The clipping is only lifted while a control inside the label has the focus, so a control it wraps is never
        // focused out of sight, while a label focused itself stays hidden. The condition is inside :where(), so the
        // rule keeps the weight of its one class and a Class of the page still overrides it.
        Assert.Contains(".bit-lbl-vhd:where(:not(:focus-within), :focus) {", stylesheet);
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"\.bit-lbl-vhd\s*\{"), "The visually hidden label is clipped regardless of the focus.");
        Assert.DoesNotContain(".bit-lbl-vhd:not(", stylesheet, "The focus condition raises the specificity of the rule.");
    }

    [TestMethod]
    public void BitLabelShouldKeepTheIndicatorGapLogical()
    {
        var stylesheet = ReadStylesheet();

        foreach (var indicator in new[] { "rqi", "opi" })
        {
            var block = SourceFiles.GetScssBlock(stylesheet, $"\n.bit-lbl-{indicator} {{");

            Assert.Contains("margin-inline-start: var(--bit-Label-indicator-gap, ", block);
        }
    }

    [TestMethod]
    public void BitLabelShouldTruncateTheContentAndKeepTheIndicatorWhole()
    {
        var stylesheet = ReadStylesheet();

        // The label keeps its own display, so its text-align still positions the content and a display set through
        // the style still truncates; it clips whatever overflows it.
        var noWrap = SourceFiles.GetScssBlock(stylesheet, "\n.bit-lbl-nwr {");
        Assert.DoesNotContain("display:", noWrap, "The label is turned into a flex container, which ignores its text-align.");
        Assert.Contains("overflow: hidden;", noWrap);
        Assert.Contains("white-space: nowrap;", noWrap);
        Assert.Contains(".bit-lbl-rqi,\n    .bit-lbl-opi {\n        flex-shrink: 0;", noWrap);
        Assert.DoesNotContain("text-overflow", noWrap, "The ellipsis is put on the whole label, indicator included.");

        // The content and its indicator are an inline row no wider than the label, lined up on their text baseline
        // as the wrapping label lines them up.
        var row = SourceFiles.GetScssBlock(stylesheet, "\n.bit-lbl-row {");
        Assert.Contains("display: inline-flex;", row);
        Assert.Contains("max-width: 100%;", row);
        Assert.Contains("align-items: baseline;", row);

        var text = SourceFiles.GetScssBlock(stylesheet, "\n.bit-lbl-txt {");
        Assert.Contains("min-width: 0;", text);
        Assert.Contains("overflow: hidden;", text);
        Assert.Contains("text-overflow: ellipsis;", text);
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
