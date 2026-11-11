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
        Assert.Contains("--bit-cnp-fs: var(--bit-cnp-sz-fs, var(--bit-CountryPicker-font-size, #{$tg-fs-sm}));", stylesheet);
        Assert.Contains("--bit-cnp-lbl-fs: var(--bit-cnp-sz-fs, var(--bit-CountryPicker-label-font-size, var(--bit-CountryPicker-font-size, #{$tg-fs-sm})));", stylesheet);
        Assert.Contains("--bit-cnp-h: var(--bit-cnp-sz-h, var(--bit-CountryPicker-min-height, #{$siz-ctrl-md}));", stylesheet);
        Assert.Contains("--bit-cnp-pad: var(--bit-cnp-sz-pad, var(--bit-CountryPicker-padding, 0 #{$siz-ctrl-pad-x-md}));", stylesheet);
        Assert.Contains("--bit-cnp-flg: var(--bit-cnp-sz-icon, var(--bit-CountryPicker-flag-size, #{$siz-icon-md}));", stylesheet);
        Assert.Contains("--bit-cnp-itm-h: var(--bit-cnp-sz-itm-h, var(--bit-CountryPicker-item-height, #{$siz-item-md}));", stylesheet);

        // So do an explicit Background, Border and Color - the Border kind before the main color of the Color role.
        Assert.Contains("--bit-cnp-bg: var(--bit-cnp-kind-bg, var(--bit-CountryPicker-background, #{$clr-bg-pri}));", stylesheet);
        Assert.Contains("--bit-cnp-brd: var(--bit-cnp-kind-brd, var(--bit-cnp-role-main, var(--bit-CountryPicker-border-color, #{$clr-brd-pri})));", stylesheet);
        Assert.Contains("--bit-cnp-brd-hover: var(--bit-cnp-kind-brd-hover, var(--bit-cnp-role-main, var(--bit-CountryPicker-hover-border-color, #{$clr-brd-pri-hover})));", stylesheet);
        Assert.Contains("--bit-cnp-focus: var(--bit-cnp-role-focus, var(--bit-CountryPicker-focus-color));", stylesheet);

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
            Assert.Contains($"{property}: initial;", resets);
        }

        Assert.Contains("@include cnp-resets;", SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-cnp {"));
        Assert.Contains("@include cnp-resets;", SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-cnp-cal {"));
    }

    [TestMethod]
    public void BitCountryPickerShouldDrawBothLabelsAtTheSameSize()
    {
        var stylesheet = ReadStylesheet();

        // The label above the field and the one heading the responsive panel are the same Label, so both read the size
        // cnp-vars resolves - an explicit Size, then the label and font variables - which the callout includes too.
        Assert.Contains("font-size: var(--bit-cnp-lbl-fs);", SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnp-lbl {"));
        Assert.Contains("font-size: var(--bit-cnp-lbl-fs);", SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnp-rlb {"));
        Assert.Contains("@include cnp-vars;", SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnp-cal {"));
    }

    [TestMethod]
    public void BitCountryPickerShouldMoveAVariableUnderThePointerRatherThanPaint()
    {
        var stylesheet = ReadStylesheet();

        // The frame of the field and the fill of a row are painted from a variable of their own that the pointer
        // moves, so a field or a row an app paints through Classes keeps that paint while it is pointed at.
        var field = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnp-fld {");
        Assert.Contains("--bit-cnp-fld-brd-st: initial;", field);
        Assert.Contains("var(--bit-cnp-fld-brd-st, var(--bit-cnp-brd))", field);

        var item = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnp-itm {");
        Assert.Contains("--bit-cnp-itm-st-bg: initial;", item);
        Assert.Contains("background-color: var(--bit-cnp-itm-st-bg, transparent);", item);
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "CountryPicker", "BitCountryPicker.scss");

    [GeneratedRegex(@"var\(--bit-CountryPicker-[a-z-]+, (0 )?var\(--bit-cnp-(sz-[a-z-]+|role-[a-z]+|kind-[a-z-]+|fs|h|pad)\b")]
    private static partial Regex PublicBeforePrivate();
}
