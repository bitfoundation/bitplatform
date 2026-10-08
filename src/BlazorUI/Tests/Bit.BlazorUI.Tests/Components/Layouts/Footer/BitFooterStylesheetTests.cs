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
        StringAssert.Contains(stylesheet, "$ftr-bg: var(--bit-ftr-bg, var(--bit-Footer-background, #{$clr-bg-pri}));");
        StringAssert.Contains(stylesheet, "$ftr-on: var(--bit-ftr-on, var(--bit-Footer-color, #{$clr-fg-pri}));");
        StringAssert.Contains(stylesheet, "$ftr-fg: var(--bit-ftr-fg, var(--bit-Footer-color, #{$clr-fg-pri}));");
        StringAssert.Contains(stylesheet, "$ftr-brd: var(--bit-ftr-brd, var(--bit-Footer-border-color, #{$clr-brd-pri}));");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-ftr-gut {"), "padding: var(--bit-ftr-padding, var(--bit-Footer-padding, #{spacing(1)} #{spacing(3)}));");

        // Gap and MaxWidth were already written inline under the private names, read first.
        StringAssert.Contains(stylesheet, "max-width: var(--bit-ftr-max-width, var(--bit-Footer-max-width, none));");
        StringAssert.Contains(stylesheet, "gap: var(--bit-ftr-gap, var(--bit-Footer-gap, 0));");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Footer-[a-z-]+, var\(--bit-ftr-"), "A public variable is read before the parameter it restyles the default of.");
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

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Layouts", "Footer", "BitFooter.scss");
}
