using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.DatePicker;

/// <summary>
/// Pins the order the DatePicker's stylesheet reads its colors and sizes in, which a bUnit render cannot see: what an
/// explicit Color or Size publishes is read before the public --bit-DatePicker-* variable restyling it, and the
/// default an unset one stands for comes last - on the root and on the callout rendered outside it alike.
/// </summary>
[TestClass]
public class BitDatePickerStylesheetTests
{
    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "DatePicker", "BitDatePicker.scss");

    [TestMethod]
    public void BitDatePickerShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // size an unset one stands for.
        var resolution = SourceFiles.GetScssBlock(stylesheet, "@mixin dtp-size-resolution");
        StringAssert.Contains(resolution, "--bit-dtp-inp-h: var(--bit-dtp-inp-h-sz, var(--bit-DatePicker-input-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(resolution, "--bit-dtp-inp-fs: var(--bit-dtp-inp-fs-sz, var(--bit-DatePicker-input-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(resolution, "--bit-dtp-ico-fs: var(--bit-dtp-ico-fs-sz, var(--bit-DatePicker-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(resolution, "--bit-dtp-cell-size: var(--bit-dtp-cell-size-sz, var(--bit-DatePicker-day-size, #{spacing(3.5)}));");
        StringAssert.Contains(resolution, "--bit-dtp-cell-fs: var(--bit-dtp-cell-fs-sz, var(--bit-DatePicker-day-font-size, #{$tg-fs-xs}));");
        StringAssert.Contains(resolution, "--bit-dtp-lbl-fs: var(--bit-dtp-lbl-fs-sz, var(--bit-DatePicker-label-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(resolution, "--bit-dtp-hdr-fs: var(--bit-dtp-hdr-fs-sz, var(--bit-DatePicker-header-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(resolution, "--bit-dtp-time-fs: var(--bit-dtp-time-fs-sz, var(--bit-DatePicker-time-font-size, #{$tg-fs-lg}));");

        // So does an explicit Color, for every color it paints.
        StringAssert.Contains(stylesheet, "var(--bit-dtp-clr, var(--bit-DatePicker-today-background, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-dtp-clr-txt, var(--bit-DatePicker-today-color, #{$clr-pri-text}))");
        StringAssert.Contains(stylesheet, "var(--bit-dtp-clr-hover, var(--bit-DatePicker-today-hover-background, #{$clr-pri-hover}))");
        StringAssert.Contains(stylesheet, "var(--bit-dtp-clr-active, var(--bit-DatePicker-today-active-background, #{$clr-pri-active}))");
        StringAssert.Contains(stylesheet, "var(--bit-dtp-clr-focus, var(--bit-DatePicker-focus-color, #{$clr-pri-focus}))");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-DatePicker-[a-z-]+, var\(--bit-dtp-"), "A public variable is read before the parameter it restyles the default of.");

        // Every read of what a Color publishes falls back to the primary role an unset one stands for.
        Assert.IsFalse(Regex.IsMatch(SourceFiles.StripScssComments(stylesheet), @"var\(--bit-dtp-clr(-[a-z]+)?\)"), "A color the Color publishes is read with no default behind it.");
    }

    [TestMethod]
    public void BitDatePickerShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var resets = SourceFiles.GetScssBlock(stylesheet, "@mixin dtp-parameter-resets");

        // A DatePicker can sit in a template of another one, which must not inherit the outer one's Color or Size: the
        // root and the callout - which inherits nothing of the root - each start the values those classes publish out
        // unset, and the classes, declared further down at the same weight, still win on the element carrying them.
        foreach (var property in new[] { "--bit-dtp-clr", "--bit-dtp-clr-txt", "--bit-dtp-clr-hover", "--bit-dtp-clr-active", "--bit-dtp-clr-focus",
                                         "--bit-dtp-inp-h-sz", "--bit-dtp-inp-fs-sz", "--bit-dtp-ico-fs-sz", "--bit-dtp-cell-size-sz",
                                         "--bit-dtp-cell-fs-sz", "--bit-dtp-lbl-fs-sz", "--bit-dtp-hdr-fs-sz", "--bit-dtp-time-fs-sz" })
        {
            StringAssert.Contains(resets, $"{property}: initial;");
        }

        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-dtp {"), "@include dtp-parameter-resets;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-dtp-cal {"), "@include dtp-parameter-resets;");

        var calloutIndex = stylesheet.IndexOf("\n.bit-dtp-cal {", System.StringComparison.Ordinal);
        Assert.IsTrue(calloutIndex < stylesheet.IndexOf("\n    .bit-dtp-#{$role} {", System.StringComparison.Ordinal), "The role classes come before the callout that resets what they publish.");
        Assert.IsTrue(calloutIndex < stylesheet.IndexOf("\n.bit-dtp-sm {", System.StringComparison.Ordinal), "The size classes come before the callout that resets what they publish.");
    }
}
