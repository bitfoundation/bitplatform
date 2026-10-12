using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TimeZonePicker;

/// <summary>
/// Pins the order the time zone picker reads its values in, which a bUnit render cannot see: a value an explicit Size,
/// Color, Background or Border publishes is read before the public --bit-TimeZonePicker-* variable that restyles the same
/// thing - on the root and on the callout it renders outside of it - and both start those values out unset, so a nested
/// picker never inherits an outer one's choice.
/// </summary>
[TestClass]
public partial class BitTimeZonePickerStylesheetTests
{
    [TestMethod]
    public void BitTimeZonePickerShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        StringAssert.Contains(stylesheet, "--bit-tzp-fs: var(--bit-tzp-sz-fs, var(--bit-TimeZonePicker-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "--bit-tzp-lbl-fs: var(--bit-tzp-sz-fs, var(--bit-TimeZonePicker-label-font-size, var(--bit-TimeZonePicker-font-size, #{$tg-fs-sm})));");
        StringAssert.Contains(stylesheet, "--bit-tzp-h: var(--bit-tzp-sz-h, var(--bit-TimeZonePicker-min-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "--bit-tzp-pad: var(--bit-tzp-sz-pad, var(--bit-TimeZonePicker-padding, 0 #{$siz-ctrl-pad-x-md}));");
        StringAssert.Contains(stylesheet, "--bit-tzp-itm-h: var(--bit-tzp-sz-itm-h, var(--bit-TimeZonePicker-item-height, #{$siz-item-md}));");

        StringAssert.Contains(stylesheet, "--bit-tzp-bg: var(--bit-tzp-kind-bg, var(--bit-TimeZonePicker-background, #{$clr-bg-pri}));");
        StringAssert.Contains(stylesheet, "--bit-tzp-brd: var(--bit-tzp-kind-brd, var(--bit-tzp-role-main, var(--bit-TimeZonePicker-border-color, #{$clr-brd-pri})));");
        StringAssert.Contains(stylesheet, "--bit-tzp-brd-hover: var(--bit-tzp-kind-brd-hover, var(--bit-tzp-role-main, var(--bit-TimeZonePicker-hover-border-color, #{$clr-brd-pri-hover})));");
        StringAssert.Contains(stylesheet, "--bit-tzp-focus: var(--bit-tzp-role-focus, var(--bit-TimeZonePicker-focus-color));");

        Assert.IsFalse(PublicBeforePrivate().IsMatch(stylesheet), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldPublishItsSizeColorBackgroundAndBorderOnlyWhereTheyAreSet()
    {
        var resets = SourceFiles.GetScssBlock(ReadStylesheet(), "@mixin tzp-resets {");

        // The root and the callout both start the values the Size, Color, Background and Border classes publish out
        // unset, and those classes - declared further down at the same weight - still win where they are carried.
        foreach (var property in new[] { "--bit-tzp-sz-fs", "--bit-tzp-sz-h", "--bit-tzp-sz-pad", "--bit-tzp-sz-itm-h",
                                         "--bit-tzp-role-main", "--bit-tzp-role-focus", "--bit-tzp-kind-bg", "--bit-tzp-kind-brd", "--bit-tzp-kind-brd-hover" })
        {
            StringAssert.Contains(resets, $"{property}: initial;");
        }

        StringAssert.Contains(SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-tzp {"), "@include tzp-resets;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-tzp-cal {"), "@include tzp-resets;");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldDrawBothLabelsAtTheSameSize()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-lbl {"), "font-size: var(--bit-tzp-lbl-fs);");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-rlb {"), "font-size: var(--bit-tzp-lbl-fs);");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-cal {"), "@include tzp-vars;");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldLineTheOffsetsUpInAColumn()
    {
        // The offsets lead every row, so their figures are of one width and never wrap.
        var offset = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-tzp-ofs {");

        StringAssert.Contains(offset, "font-variant-numeric: tabular-nums;");
        StringAssert.Contains(offset, "white-space: nowrap;");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldKeepItsSeparatorsInTheAccessibilityTree()
    {
        // A separator only a screen reader reads is clipped out of sight, never display:none or visibility:hidden.
        var separator = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-tzp-sep {");

        StringAssert.Contains(separator, "clip-path: inset(50%);");
        Assert.IsFalse(separator.Contains("display: none"));
        Assert.IsFalse(separator.Contains("visibility: hidden"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldMoveAVariableUnderThePointerRatherThanPaint()
    {
        var stylesheet = ReadStylesheet();

        var field = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-fld {");
        StringAssert.Contains(field, "--bit-tzp-fld-brd-st: initial;");
        StringAssert.Contains(field, "var(--bit-tzp-fld-brd-st, var(--bit-tzp-brd))");

        var item = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-itm {");
        StringAssert.Contains(item, "--bit-tzp-itm-st-bg: initial;");
        StringAssert.Contains(item, "background-color: var(--bit-tzp-itm-st-bg, transparent);");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "TimeZonePicker", "BitTimeZonePicker.scss");

    [GeneratedRegex(@"var\(--bit-TimeZonePicker-[a-z-]+, (0 )?var\(--bit-tzp-(sz-[a-z-]+|role-[a-z]+|kind-[a-z-]+|fs|h|pad)\b")]
    private static partial Regex PublicBeforePrivate();
}
