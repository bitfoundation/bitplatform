using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Separator;

/// <summary>
/// Pins the public --bit-Separator-* variables and the rules of the stylesheet a bUnit render cannot see: every
/// variable the header documents is read with a fallback, none of them is ever declared (so they keep inheriting from
/// :root and the ancestors), nothing is read that the header does not document, a parameter wins over the variable
/// standing for the same thing, nested separators do not inherit each other's line, and the disabled state holds in a
/// forced-colors mode.
/// </summary>
[TestClass]
public partial class BitSeparatorStylesheetTests
{
    [TestMethod]
    public void BitSeparatorShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.IsNotEmpty(documented, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}, ", stylesheet, $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitSeparatorShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitSeparatorShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Separator-* variable is declared, which stops it inheriting.");
    }

    [TestMethod,
        DataRow("--bit-spr-brd"),
        DataRow("--bit-spr-siz"),
        DataRow("--bit-spr-stl"),
        DataRow("--bit-spr-bg"),
        DataRow("--bit-spr-ofs"),
        DataRow("--bit-spr-ins"),
        DataRow("--bit-spr-dis-brd"),
        DataRow("--bit-spr-dis-fg")]
    public void BitSeparatorShouldStartEveryPrivateVariableUnset(string name)
    {
        // A separator nested in the content of another would otherwise inherit its line.
        Assert.Contains($"{name}: initial;", SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spr {"));
    }

    [TestMethod,
        DataRow("var(--bit-spr-stl, var(--bit-Separator-line-style, solid))"),
        DataRow("var(--bit-spr-brd, var(--bit-Separator-color, #{$clr-brd-sec}))"),
        DataRow("var(--bit-spr-siz, var(--bit-Separator-thickness, #{$siz-divider}))"),
        DataRow("var(--bit-spr-ins, var(--bit-Separator-inset, 0))"),
        DataRow("var(--bit-spr-ofs, var(--bit-Separator-content-offset, 0))"),
        DataRow("var(--bit-spr-bg, var(--bit-Separator-content-background, transparent))")]
    public void BitSeparatorShouldLetTheParameterWinOverItsVariable(string chain)
    {
        // The parameter classes and the inline lengths set the private variable, which is read ahead of the public one.
        Assert.Contains(chain, ReadStylesheet());
    }

    [TestMethod]
    public void BitSeparatorShouldGiveSolidAClassOfItsOwn()
    {
        // An explicit Solid has to beat a dashed line-style variable set on an ancestor, so it is not the unset default.
        Assert.Contains(".bit-spr-sld {\n    --bit-spr-stl: solid;", ReadStylesheet());
    }

    [TestMethod]
    public void BitSeparatorShouldKeepADisabledSeparatorInTheDisabledColors()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-dis {");
        Assert.Contains("--bit-spr-dis-brd: #{$clr-brd-dis};", block);
        Assert.Contains("--bit-spr-dis-fg: #{$clr-fg-dis};", block);

        // The disabled colors come first in each chain, ahead of the parameters and the public variables.
        Assert.Contains("var(--bit-spr-dis-brd, var(--bit-spr-brd, ", stylesheet);
        Assert.Contains("color: var(--bit-spr-dis-fg, var(--bit-Separator-content-color, ", stylesheet);
    }

    [TestMethod]
    public void BitSeparatorShouldKeepTheDisabledStateInForcedColors()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        Assert.Contains("border-color: GrayText;", block);
        Assert.Contains("color: GrayText;", block);
    }

    [TestMethod]
    public void BitSeparatorShouldInsetTheLineLogically()
    {
        var stylesheet = ReadStylesheet();

        // Logical padding takes one length for both ends or two for the start and the end, in the reading direction.
        Assert.Contains("padding-inline: var(--bit-spr-ins, ", SourceFiles.GetScssBlock(stylesheet, "\n.bit-spr-hrz {"));
        Assert.Contains("padding-block: var(--bit-spr-ins, ", SourceFiles.GetScssBlock(stylesheet, "\n.bit-spr-vrt {"));
    }

    [TestMethod]
    public void BitSeparatorShouldLetTheContentShrinkAndWrap()
    {
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spr-cnt {");

        Assert.Contains("min-width: 0;", block);
        Assert.Contains("overflow-wrap: break-word;", block);
    }

    [TestMethod]
    public void BitSeparatorShouldKeepAVerticalSeparatorAtLeastALineOfTextTall()
    {
        var stylesheet = ReadStylesheet();

        // Between runs of text, or sized to the content it does not have, the line would otherwise collapse to nothing.
        // The minimum sits on the line rather than on the border-box root, whose Inset padding would otherwise eat it.
        Assert.Contains("min-height: 1em;", SourceFiles.GetScssBlock(stylesheet, "\n    &:not(:has(> .bit-spr-cnt))::before {"));
        Assert.DoesNotContain("\n    min-height:", SourceFiles.GetScssBlock(stylesheet, "\n.bit-spr-vrt {"), "The minimum is on the root, where an Inset eats it.");
    }

    [TestMethod]
    public void BitSeparatorShouldKeepTheContentGapAtBothEnds()
    {
        var stylesheet = ReadStylesheet();

        // Start- and end-aligned content keeps the same gap from the edge as from the line, however it is painted and
        // whatever its offset, so upgrading leaves an existing aligned label where it was.
        Assert.Contains("padding: 0 var(--bit-Separator-content-gap, #{spacing(1.5)});", SourceFiles.GetScssBlock(stylesheet, "\n.bit-spr-hrz {"));
        Assert.Contains("padding: var(--bit-Separator-content-gap, #{spacing(1.5)}) 0;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-spr-vrt {"));
        Assert.DoesNotContain("padding-inline-start", stylesheet, "The content is held flush with its edge.");
        Assert.DoesNotContain("padding-top", stylesheet, "The content is held flush with its edge.");
    }

    [TestMethod]
    public void BitSeparatorShouldLetTheContentInheritTheFontOfItsSurroundings()
    {
        // The size and the weight inherit, so the family does too: a label inside a heading or a monospace panel
        // matches the text around it.
        Assert.DoesNotContain("font-family", SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spr-cnt {"), "The content forces a font family.");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Utilities", "Separator", "BitSeparator.scss");

    [GeneratedRegex(@"^//\s+(--bit-Separator-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Separator-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Separator-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
