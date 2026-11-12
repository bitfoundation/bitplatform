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

        Assert.Contains("--bit-tzp-fs: var(--bit-tzp-sz-fs, var(--bit-TimeZonePicker-font-size, #{$tg-fs-sm}));", stylesheet);
        Assert.Contains("--bit-tzp-lbl-fs: var(--bit-tzp-sz-fs, var(--bit-TimeZonePicker-label-font-size, var(--bit-TimeZonePicker-font-size, #{$tg-fs-sm})));", stylesheet);
        Assert.Contains("--bit-tzp-h: var(--bit-tzp-sz-h, var(--bit-TimeZonePicker-min-height, #{$siz-ctrl-md}));", stylesheet);
        Assert.Contains("--bit-tzp-pad: var(--bit-tzp-sz-pad, var(--bit-TimeZonePicker-padding, 0 #{$siz-ctrl-pad-x-md}));", stylesheet);
        Assert.Contains("--bit-tzp-itm-h: var(--bit-tzp-sz-itm-h, var(--bit-TimeZonePicker-item-height, #{$siz-item-md}));", stylesheet);

        Assert.Contains("--bit-tzp-bg: var(--bit-tzp-kind-bg, var(--bit-TimeZonePicker-background, #{$clr-bg-pri}));", stylesheet);
        Assert.Contains("--bit-tzp-brd: var(--bit-tzp-kind-brd, var(--bit-tzp-role-main, var(--bit-TimeZonePicker-border-color, #{$clr-brd-pri})));", stylesheet);
        Assert.Contains("--bit-tzp-brd-hover: var(--bit-tzp-kind-brd-hover, var(--bit-tzp-role-main, var(--bit-TimeZonePicker-hover-border-color, #{$clr-brd-pri-hover})));", stylesheet);
        Assert.Contains("--bit-tzp-focus: var(--bit-tzp-role-focus, var(--bit-TimeZonePicker-focus-color));", stylesheet);

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
            Assert.Contains($"{property}: initial;", resets);
        }

        Assert.Contains("@include tzp-resets;", SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-tzp {"));
        Assert.Contains("@include tzp-resets;", SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-tzp-cal {"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldDrawBothLabelsAtTheSameSize()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("font-size: var(--bit-tzp-lbl-fs);", SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-lbl {"));
        Assert.Contains("font-size: var(--bit-tzp-lbl-fs);", SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-rlb {"));
        Assert.Contains("@include tzp-vars;", SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-cal {"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldLineTheOffsetsUpInAColumn()
    {
        // The offsets lead every row, so their figures are of one width and never wrap.
        var offset = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-tzp-ofs {");

        Assert.Contains("font-variant-numeric: tabular-nums;", offset);
        Assert.Contains("white-space: nowrap;", offset);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldKeepItsSeparatorsInTheAccessibilityTree()
    {
        // A separator only a screen reader reads is hidden through the visually-hidden mixin, which keeps it in the
        // accessibility tree (BitVisuallyHiddenContractTests pins what the mixin does). What this pins is that the
        // separator is hidden that way, and that no rule of its own - a nested state included - takes it out of the
        // tree after all.
        var stylesheet = ReadStylesheet();
        var separator = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-sep {");
        var block = SourceFiles.GetScssBlock(stylesheet, "\n.bit-tzp-sep {");

        Assert.Contains("@include visually-hidden;", separator);
        Assert.DoesNotContain("display: none", block);
        Assert.DoesNotContain("visibility: hidden", block);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldMoveAVariableUnderThePointerRatherThanPaint()
    {
        var stylesheet = ReadStylesheet();

        var field = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-fld {");
        Assert.Contains("--bit-tzp-fld-brd-st: initial;", field);
        Assert.Contains("var(--bit-tzp-fld-brd-st, var(--bit-tzp-brd))", field);

        var item = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-tzp-itm {");
        Assert.Contains("--bit-tzp-itm-st-bg: initial;", item);
        Assert.Contains("background-color: var(--bit-tzp-itm-st-bg, transparent);", item);
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "TimeZonePicker", "BitTimeZonePicker.scss");

    [GeneratedRegex(@"var\(--bit-TimeZonePicker-[a-z-]+, (0 )?var\(--bit-tzp-(sz-[a-z-]+|role-[a-z]+|kind-[a-z-]+|fs|h|pad)\b")]
    private static partial Regex PublicBeforePrivate();
}
