using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Checkbox;

/// <summary>
/// Pins the order the checkbox's stylesheet ranks its values in, which a bUnit render cannot see: an explicit Color or
/// Size outranks the public --bit-Checkbox-* variable restyling what it sets, which in turn outranks the default an
/// unset parameter stands for.
/// </summary>
[TestClass]
public class BitCheckboxStylesheetTests
{
    [TestMethod]
    public void BitCheckboxShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the default an
        // unset one stands for.
        StringAssert.Contains(stylesheet, "--bit-chb-box-side: var(--bit-chb-box-size, var(--bit-Checkbox-box-size, #{$siz-sel-md}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-chb-lbl-fontsize, var(--bit-Checkbox-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-chb-des-fontsize, var(--bit-Checkbox-description-font-size, #{$tg-fs-xs}));");

        // So does an explicit Color, for every color it paints - including the disabled ones, which the disabled
        // state still picks.
        StringAssert.Contains(stylesheet, "--bit-chb-box-clr-bg: var(--bit-chb-clr, var(--bit-Checkbox-checked-background, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "--bit-chb-ico-clr: var(--bit-chb-clr-txt, var(--bit-Checkbox-check-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "--bit-chb-ico-clr-bg: var(--bit-chb-clr, var(--bit-Checkbox-indeterminate-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "--bit-chb-box-clr-bg: var(--bit-chb-clr-hover, var(--bit-Checkbox-checked-hover-background, #{$clr-pri-hover}));");
        StringAssert.Contains(stylesheet, "var(--bit-chb-clr-focus, var(--bit-Checkbox-focus-color, #{$clr-pri-focus}))");
        StringAssert.Contains(stylesheet, "--bit-chb-box-clr-brd: var(--bit-chb-clr-dis, var(--bit-Checkbox-disabled-color, #{$clr-pri-dis}));");
        StringAssert.Contains(stylesheet, "color: var(--bit-chb-clr-dis-text, var(--bit-Checkbox-disabled-text-color, #{$clr-pri-dis-text}));");

        // A size class publishes the theme's size alone, never a public variable ahead of it.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-chb-sm {"), "--bit-chb-box-size: #{$siz-sel-sm};");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Checkbox-[a-z-]+, var\(--bit-chb-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitCheckboxShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-chb {");

        // A checkbox can sit in the label template or the custom face of another one, which must not inherit the outer
        // checkbox's Color or Size: each root starts the values those classes publish out unset, and the classes -
        // declared further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-chb-clr", "--bit-chb-clr-hover", "--bit-chb-clr-txt", "--bit-chb-clr-txt-sec", "--bit-chb-clr-txt-hover",
                                         "--bit-chb-clr-dis", "--bit-chb-clr-dis-text", "--bit-chb-clr-focus",
                                         "--bit-chb-box-size", "--bit-chb-lbl-fontsize", "--bit-chb-des-fontsize" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Inputs", "Checkbox", "BitCheckbox.scss");
}
