using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.CountryPicker;

/// <summary>
/// Pins the order the country picker reads its values in, which a bUnit render cannot see: a value an explicit Size,
/// Color, Background or Border publishes is read before the public --bit-CountryPicker-* variable that restyles the
/// same thing - on the root and on the callout it renders outside of it - and both start those values out unset, so
/// a nested picker never inherits an outer one's choice.
/// </summary>
[TestClass]
public partial class BitCountryPickerStylesheetTests
{
    [TestMethod]
    public void BitCountryPickerShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // field an unset one stands for.
        StringAssert.Contains(stylesheet, "--bit-cnp-fs: var(--bit-cnp-sz-fs, var(--bit-CountryPicker-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "--bit-cnp-lbl-fs: var(--bit-cnp-sz-fs, var(--bit-CountryPicker-label-font-size, var(--bit-CountryPicker-font-size, #{$tg-fs-sm})));");
        StringAssert.Contains(stylesheet, "--bit-cnp-h: var(--bit-cnp-sz-h, var(--bit-CountryPicker-min-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "--bit-cnp-pad: var(--bit-cnp-sz-pad, var(--bit-CountryPicker-padding, 0 #{$siz-ctrl-pad-x-md}));");
        StringAssert.Contains(stylesheet, "--bit-cnp-flg: var(--bit-cnp-sz-icon, var(--bit-CountryPicker-flag-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "--bit-cnp-itm-h: var(--bit-cnp-sz-itm-h, var(--bit-CountryPicker-item-height, #{$siz-item-md}));");

        // So do an explicit Background, Border and Color - the Border kind before the main color of the Color role.
        StringAssert.Contains(stylesheet, "--bit-cnp-bg: var(--bit-cnp-kind-bg, var(--bit-CountryPicker-background, #{$clr-bg-pri}));");
        StringAssert.Contains(stylesheet, "--bit-cnp-brd: var(--bit-cnp-kind-brd, var(--bit-cnp-role-main, var(--bit-CountryPicker-border-color, #{$clr-brd-pri})));");
        StringAssert.Contains(stylesheet, "--bit-cnp-brd-hover: var(--bit-cnp-kind-brd-hover, var(--bit-cnp-role-main, var(--bit-CountryPicker-hover-border-color, #{$clr-brd-pri-hover})));");
        StringAssert.Contains(stylesheet, "--bit-cnp-focus: var(--bit-cnp-role-focus, var(--bit-CountryPicker-focus-color));");

        Assert.IsFalse(PublicBeforePrivate().IsMatch(stylesheet), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitCountryPickerShouldPublishItsSizeColorBackgroundAndBorderOnlyWhereTheyAreSet()
    {
        var resets = SourceFiles.GetScssBlock(ReadStylesheet(), "@mixin cnp-resets {");

        // The root and the callout both start the values the Size, Color, Background and Border classes publish out
        // unset, and those classes - declared further down at the same weight - still win where they are carried.
        foreach (var property in new[] { "--bit-cnp-sz-fs", "--bit-cnp-sz-h", "--bit-cnp-sz-pad", "--bit-cnp-sz-icon", "--bit-cnp-sz-itm-h",
                                         "--bit-cnp-role-main", "--bit-cnp-role-focus", "--bit-cnp-kind-bg", "--bit-cnp-kind-brd", "--bit-cnp-kind-brd-hover" })
        {
            StringAssert.Contains(resets, $"{property}: initial;");
        }

        StringAssert.Contains(SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-cnp {"), "@include cnp-resets;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-cnp-cal {"), "@include cnp-resets;");
    }

    [TestMethod]
    public void BitCountryPickerShouldDrawBothLabelsAtTheSameSize()
    {
        var stylesheet = ReadStylesheet();

        // The label above the field and the one heading the responsive panel are the same Label, so both read the size
        // cnp-vars resolves - an explicit Size, then the label and font variables - which the callout includes too.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnp-lbl {"), "font-size: var(--bit-cnp-lbl-fs);");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnp-rlb {"), "font-size: var(--bit-cnp-lbl-fs);");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnp-cal {"), "@include cnp-vars;");
    }

    [TestMethod]
    public void BitCountryPickerShouldMoveAVariableUnderThePointerRatherThanPaint()
    {
        var stylesheet = ReadStylesheet();

        // The frame of the field and the fill of a row are painted from a variable of their own that the pointer
        // moves, so a field or a row an app paints through Classes keeps that paint while it is pointed at.
        var field = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnp-fld {");
        StringAssert.Contains(field, "--bit-cnp-fld-brd-st: initial;");
        StringAssert.Contains(field, "var(--bit-cnp-fld-brd-st, var(--bit-cnp-brd))");

        var item = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnp-itm {");
        StringAssert.Contains(item, "--bit-cnp-itm-st-bg: initial;");
        StringAssert.Contains(item, "background-color: var(--bit-cnp-itm-st-bg, transparent);");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "CountryPicker", "BitCountryPicker.scss");

    [GeneratedRegex(@"var\(--bit-CountryPicker-[a-z-]+, (0 )?var\(--bit-cnp-(sz-[a-z-]+|role-[a-z]+|kind-[a-z-]+|fs|h|pad)\b")]
    private static partial Regex PublicBeforePrivate();
}
