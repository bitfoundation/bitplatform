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
        Assert.Contains("--bit-chb-box-side: var(--bit-chb-box-size, var(--bit-Checkbox-box-size, #{$siz-sel-md}));", stylesheet);
        Assert.Contains("font-size: var(--bit-chb-lbl-fontsize, var(--bit-Checkbox-font-size, #{$tg-fs-sm}));", stylesheet);
        Assert.Contains("font-size: var(--bit-chb-des-fontsize, var(--bit-Checkbox-description-font-size, #{$tg-fs-xs}));", stylesheet);

        // So does an explicit Color, for every color it paints - including the disabled ones, which the disabled
        // state still picks.
        Assert.Contains("--bit-chb-box-clr-bg: var(--bit-chb-clr, var(--bit-Checkbox-checked-background, #{$clr-pri}));", stylesheet);
        Assert.Contains("--bit-chb-ico-clr: var(--bit-chb-clr-txt, var(--bit-Checkbox-check-color, #{$clr-pri-text}));", stylesheet);
        Assert.Contains("--bit-chb-ico-clr-bg: var(--bit-chb-clr, var(--bit-Checkbox-indeterminate-color, #{$clr-pri}));", stylesheet);
        Assert.Contains("--bit-chb-box-clr-bg: var(--bit-chb-clr-hover, var(--bit-Checkbox-checked-hover-background, #{$clr-pri-hover}));", stylesheet);
        Assert.Contains("focus-ring-own(var(--bit-chb-clr-focus, var(--bit-Checkbox-focus-color)))", stylesheet);
        Assert.Contains("--bit-chb-box-clr-brd: var(--bit-chb-clr-dis, var(--bit-Checkbox-disabled-color, #{$clr-pri-dis}));", stylesheet);
        Assert.Contains("color: var(--bit-chb-clr-dis-text, var(--bit-Checkbox-disabled-text-color, #{$clr-pri-dis-text}));", stylesheet);

        // A size class publishes the theme's size alone, never a public variable ahead of it.
        Assert.Contains("--bit-chb-box-size: #{$siz-sel-sm};", SourceFiles.GetScssBlock(stylesheet, "\n.bit-chb-sm {"));

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
            Assert.Contains($"{property}: initial;", root);
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "Checkbox", "BitCheckbox.scss");
}
