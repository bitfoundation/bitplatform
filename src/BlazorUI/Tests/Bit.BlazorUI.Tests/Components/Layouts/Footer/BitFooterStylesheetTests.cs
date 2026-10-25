using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Footer;

/// <summary>
/// Pins what a bUnit render cannot see of the footer: that a parameter written on it ranks above the public
/// --bit-Footer-* variable restyling the same thing, and that a footer nested in another one does not inherit its choice.
/// </summary>
[TestClass]
public class BitFooterStylesheetTests
{
    [TestMethod]
    public void BitFooterShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Color or Size publishes these only while it is set, so they are read before the variable, which
        // only restyles the default an unset one stands for: the plain page surface and the medium spacing.
        // The colors are read per variant, the variant's own tier of each public variable ahead of the shared one.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-ftr-bg, var(--bit-Footer-fill-background, var(--bit-Footer-background, #{$clr-bg-pri})));");
        StringAssert.Contains(stylesheet, "color: var(--bit-ftr-on, var(--bit-Footer-fill-color, var(--bit-Footer-color, #{$clr-fg-pri})));");
        StringAssert.Contains(stylesheet, "color: var(--bit-ftr-fg, var(--bit-Footer-outline-color, var(--bit-Footer-color, #{$clr-fg-pri})));");
        StringAssert.Contains(stylesheet, "var(--bit-ftr-brd, var(--bit-Footer-outline-border-color, var(--bit-Footer-border-color, #{$clr-brd-pri})));");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-ftr-gut {"), "padding: var(--bit-ftr-padding, var(--bit-Footer-padding, #{spacing(1)} #{spacing(3)}));");

        // Gap and MaxWidth were already written inline under the private names, read first.
        StringAssert.Contains(stylesheet, "max-width: var(--bit-ftr-max-width, var(--bit-Footer-max-width, none));");
        StringAssert.Contains(stylesheet, "gap: var(--bit-ftr-gap, var(--bit-Footer-gap, 0));");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Footer-[a-z-]+, var\(--bit-ftr-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod,
        DataRow("fill", ".bit-ftr-fil", new[] { "color", "background", "border-color" }),
        DataRow("outline", ".bit-ftr-otl", new[] { "color", "border-color" }),
        DataRow("text", ".bit-ftr-txt", new[] { "color", "border-color" })]
    public void BitFooterShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector, string[] names)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // The divider of a Bordered footer is the variant's too (Outline draws a whole border instead), in a rule whose
        // variant costs no specificity, so a single class an app hands the footer still restyles it.
        if (variant != "outline")
        {
            block += SourceFiles.GetScssBlock(stylesheet, $"\n:where({selector}).bit-ftr-brd {{");
        }

        // Each variant reads a tier of its own ahead of every shared color variable it draws with, so a theme can set
        // one look apart from the other two on :root - a neutral hairline around the outlined footers without a divider
        // over every filled one.
        foreach (var name in names)
        {
            StringAssert.Contains(block, $"var(--bit-Footer-{variant}-{name}, var(--bit-Footer-{name}, ",
                                  $"{selector} does not read --bit-Footer-{variant}-{name} ahead of --bit-Footer-{name}.");
        }

        // A variant reads a shared color variable only right behind its own twin, and never the tier of another variant.
        Assert.IsFalse(Regex.IsMatch(block, $@"(?<!var\(--bit-Footer-{variant}-[a-z-]+, )var\(--bit-Footer-(?:color|background|border-color)\b"),
                       $"{selector} reads a shared color variable without its own tier ahead of it.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-Footer-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");

        // The divider of a Bordered footer is drawn by its variant, never by a rule every variant shares.
        Assert.IsFalse(stylesheet.Contains("\n.bit-ftr-brd {"), "A rule of every variant draws the divider.");
    }

    [TestMethod]
    public void BitFooterShouldPublishItsParametersOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-ftr {");

        // A footer can sit in the content of another one, which must not inherit the outer footer's Color, Size, Gap,
        // MaxWidth or alignment: each root starts the values its parameters publish out unset, and the classes -
        // declared further down at the same weight - and the inline style still win on the root that carries them.
        foreach (var property in new[] { "--bit-ftr-bg", "--bit-ftr-on", "--bit-ftr-fg", "--bit-ftr-brd", "--bit-ftr-bg-dis", "--bit-ftr-fg-dis",
                                         "--bit-ftr-padding", "--bit-ftr-gap", "--bit-ftr-max-width", "--bit-ftr-wrap", "--bit-ftr-align", "--bit-ftr-justify" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Layouts", "Footer", "BitFooter.scss");
}
