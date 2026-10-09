using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PhoneInput;

/// <summary>
/// Pins the order the phone input reads its values in, which a bUnit render cannot see: a value an explicit Size,
/// Color, Background or Border publishes is read before the public --bit-PhoneInput-* variable that restyles the same
/// thing - on the root and on the callout it renders outside of it - and both start those values out unset, so a
/// nested field never inherits an outer one's choice.
/// </summary>
[TestClass]
public partial class BitPhoneInputStylesheetTests
{
    [TestMethod]
    public void BitPhoneInputShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // field an unset one stands for.
        StringAssert.Contains(stylesheet, "--bit-phi-fs: var(--bit-phi-sz-fs, var(--bit-PhoneInput-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "--bit-phi-lbl-fs: var(--bit-phi-sz-fs, var(--bit-PhoneInput-label-font-size, var(--bit-PhoneInput-font-size, #{$tg-fs-sm})));");
        StringAssert.Contains(stylesheet, "--bit-phi-h: var(--bit-phi-sz-h, var(--bit-PhoneInput-min-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "--bit-phi-inp-pad: var(--bit-phi-sz-pad, var(--bit-PhoneInput-padding, 0 #{$siz-ctrl-pad-x-md}));");
        StringAssert.Contains(stylesheet, "--bit-phi-drp-pad: var(--bit-phi-sz-pad, var(--bit-PhoneInput-dropdown-padding, 0 #{$siz-ctrl-pad-x-md}));");
        StringAssert.Contains(stylesheet, "--bit-phi-flg: var(--bit-phi-sz-icon, var(--bit-PhoneInput-flag-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "--bit-phi-itm-h: var(--bit-phi-sz-itm-h, var(--bit-PhoneInput-item-height, #{$siz-item-md}));");

        // So do an explicit Background, Border and Color - the Border kind before the main color of the Color role.
        StringAssert.Contains(stylesheet, "--bit-phi-bg: var(--bit-phi-kind-bg, var(--bit-PhoneInput-background, #{$clr-bg-pri}));");
        StringAssert.Contains(stylesheet, "--bit-phi-brd: var(--bit-phi-kind-brd, var(--bit-phi-role-main, var(--bit-PhoneInput-border-color, #{$clr-brd-pri})));");
        StringAssert.Contains(stylesheet, "--bit-phi-brd-hover: var(--bit-phi-kind-brd-hover, var(--bit-phi-role-main, var(--bit-PhoneInput-hover-border-color, #{$clr-brd-pri-hover})));");
        StringAssert.Contains(stylesheet, "--bit-phi-focus: var(--bit-phi-role-focus, var(--bit-PhoneInput-focus-color, #{$clr-pri-focus}));");

        Assert.IsFalse(PublicBeforePrivate().IsMatch(stylesheet), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitPhoneInputShouldPublishItsSizeColorBackgroundAndBorderOnlyWhereTheyAreSet()
    {
        var resets = SourceFiles.GetScssBlock(ReadStylesheet(), "@mixin phi-resets {");

        // The root and the callout both start the values the Size, Color, Background and Border classes publish out
        // unset, and those classes - declared further down at the same weight - still win where they are carried.
        foreach (var property in new[] { "--bit-phi-sz-fs", "--bit-phi-sz-h", "--bit-phi-sz-pad", "--bit-phi-sz-icon", "--bit-phi-sz-itm-h",
                                         "--bit-phi-role-main", "--bit-phi-role-focus", "--bit-phi-kind-bg", "--bit-phi-kind-brd", "--bit-phi-kind-brd-hover" })
        {
            StringAssert.Contains(resets, $"{property}: initial;");
        }

        StringAssert.Contains(SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-phi {"), "@include phi-resets;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-phi-cal {"), "@include phi-resets;");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "PhoneInput", "BitPhoneInput.scss");

    [GeneratedRegex(@"var\(--bit-PhoneInput-[a-z-]+, (0 )?var\(--bit-phi-(sz-[a-z-]+|role-[a-z]+|kind-[a-z-]+|fs|h|pad-x|icon)\b")]
    private static partial Regex PublicBeforePrivate();
}
