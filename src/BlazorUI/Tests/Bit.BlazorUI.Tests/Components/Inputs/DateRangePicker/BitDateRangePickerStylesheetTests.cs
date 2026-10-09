using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.DateRangePicker;

/// <summary>
/// Pins the order the DateRangePicker's stylesheet reads its colors and sizes in, which a bUnit render cannot see:
/// what an explicit Color or Size publishes is read before the public --bit-DateRangePicker-* variable restyling it,
/// and the default an unset one stands for comes last - on the root and on the callout rendered outside it alike.
/// </summary>
[TestClass]
public class BitDateRangePickerStylesheetTests
{
    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Inputs", "DateRangePicker", "BitDateRangePicker.scss");

    [TestMethod]
    public void BitDateRangePickerShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Color publishes the role slots, which the resolution reads before the variables, then the primary
        // role an unset one stands for. The role classes themselves name no variable.
        var resolution = SourceFiles.GetScssBlock(stylesheet, "@mixin dtrp-color-resolution");
        StringAssert.Contains(resolution, "--bit-dtrp-clr: var(--bit-dtrp-rl-clr, var(--bit-DateRangePicker-color, #{$clr-pri}));");
        StringAssert.Contains(resolution, "--bit-dtrp-clr-txt: var(--bit-dtrp-rl-txt, var(--bit-DateRangePicker-text-color, #{$clr-pri-text}));");
        StringAssert.Contains(resolution, "--bit-dtrp-clr-hover: var(--bit-dtrp-rl-hover, var(--bit-DateRangePicker-hover-color, #{$clr-pri-hover}));");
        StringAssert.Contains(resolution, "--bit-dtrp-clr-active: var(--bit-dtrp-rl-active, var(--bit-DateRangePicker-active-color, #{$clr-pri-active}));");
        StringAssert.Contains(resolution, "--bit-dtrp-clr-body: var(--bit-dtrp-rl-body, var(--bit-DateRangePicker-range-background, #{$clr-pri-light}));");
        StringAssert.Contains(resolution, "--bit-dtrp-clr-focus: var(--bit-dtrp-rl-focus, var(--bit-DateRangePicker-focus-color));");
        Assert.IsFalse(SourceFiles.GetScssBlock(stylesheet, "\n    .bit-dtrp-#{$role} {").Contains("--bit-DateRangePicker-"), "A role class reads a public variable ahead of its own color.");

        // So does an explicit Size, for the five sizes a variable restyles.
        StringAssert.Contains(stylesheet, "var(--bit-dtrp-inp-h, var(--bit-DateRangePicker-input-height, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-dtrp-inp-fs, var(--bit-DateRangePicker-input-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-dtrp-lbl-fs, var(--bit-DateRangePicker-label-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-dtrp-cell-size, var(--bit-DateRangePicker-day-size, #{spacing(3.5)}))");
        StringAssert.Contains(stylesheet, "var(--bit-dtrp-cell-fs, var(--bit-DateRangePicker-day-font-size, #{$tg-fs-xs}))");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-DateRangePicker-[a-z-]+, var\(--bit-dtrp-"), "A public variable is read before the parameter it restyles the default of.");
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-dtrp-(inp-h|inp-fs|lbl-fs|cell-size|cell-fs)\)"), "A size the Size publishes is read with no default behind it.");
    }

    [TestMethod]
    public void BitDateRangePickerShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var resets = SourceFiles.GetScssBlock(stylesheet, "@mixin dtrp-parameter-resets");

        // A DateRangePicker can sit in a template of another one, which must not inherit the outer one's Color or Size:
        // the root and the callout - which inherits nothing of the root - each start the values those classes publish
        // out unset, and the classes, declared further down at the same weight, still win on the element carrying them.
        foreach (var property in new[] { "--bit-dtrp-rl-clr", "--bit-dtrp-rl-txt", "--bit-dtrp-rl-hover", "--bit-dtrp-rl-active", "--bit-dtrp-rl-body",
                                         "--bit-dtrp-rl-focus", "--bit-dtrp-inp-h", "--bit-dtrp-inp-fs", "--bit-dtrp-cell-size", "--bit-dtrp-cell-fs",
                                         "--bit-dtrp-lbl-fs" })
        {
            StringAssert.Contains(resets, $"{property}: initial;");
        }

        foreach (var element in new[] { "\n.bit-dtrp {", "\n.bit-dtrp-cal {" })
        {
            var declarations = SourceFiles.GetScssDeclarations(stylesheet, element);
            StringAssert.Contains(declarations, "@include dtrp-parameter-resets;");
            StringAssert.Contains(declarations, "@include dtrp-color-resolution;");
        }

        var calloutIndex = stylesheet.IndexOf("\n.bit-dtrp-cal {", System.StringComparison.Ordinal);
        Assert.IsTrue(calloutIndex < stylesheet.IndexOf("\n    .bit-dtrp-#{$role} {", System.StringComparison.Ordinal), "The role classes come before the callout that resets what they publish.");
        Assert.IsTrue(calloutIndex < stylesheet.IndexOf("\n.bit-dtrp-sm {", System.StringComparison.Ordinal), "The size classes come before the callout that resets what they publish.");
    }
}
