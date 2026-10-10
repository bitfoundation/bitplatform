using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TimePicker;

/// <summary>
/// Pins the order the TimePicker's stylesheet reads its colors and sizes in, which a bUnit render cannot see:
/// what an explicit Color or Size publishes is read before the public --bit-TimePicker-* variable restyling it,
/// and the default an unset one stands for comes last - on the root and on the callout rendered outside it alike.
/// </summary>
[TestClass]
public class BitTimePickerStylesheetTests
{
    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Inputs", "TimePicker", "BitTimePicker.scss");

    [TestMethod]
    public void BitTimePickerShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Color publishes the role slots, which the resolution reads before the variables, then the primary
        // role an unset one stands for. The role classes themselves name no variable.
        var resolution = SourceFiles.GetScssBlock(stylesheet, "@mixin tpc-color-resolution");
        StringAssert.Contains(resolution, "--bit-tpc-clr: var(--bit-tpc-rl-clr, var(--bit-TimePicker-color, #{$clr-pri}));");
        StringAssert.Contains(resolution, "--bit-tpc-clr-txt: var(--bit-tpc-rl-txt, var(--bit-TimePicker-on-color, #{$clr-pri-text}));");
        StringAssert.Contains(resolution, "--bit-tpc-clr-hover: var(--bit-tpc-rl-hover, var(--bit-TimePicker-hover-color, #{$clr-pri-hover}));");
        StringAssert.Contains(resolution, "--bit-tpc-clr-active: var(--bit-tpc-rl-active, var(--bit-TimePicker-active-color, #{$clr-pri-active}));");
        StringAssert.Contains(resolution, "--bit-tpc-clr-focus: var(--bit-tpc-rl-focus, var(--bit-TimePicker-focus-color));");
        Assert.IsFalse(SourceFiles.GetScssBlock(stylesheet, "\n    .bit-tpc-#{$role} {").Contains("--bit-TimePicker-"), "A role class reads a public variable ahead of its own color.");

        // So does an explicit Size, for every size a variable restyles; the size classes name no variable either.
        StringAssert.Contains(stylesheet, "var(--bit-tpc-inp-h, var(--bit-TimePicker-height, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-tpc-inp-fs, var(--bit-TimePicker-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-tpc-lbl-fs, var(--bit-TimePicker-label-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-tpc-des-fs, var(--bit-TimePicker-description-font-size, #{$tg-fs-xs}))");
        StringAssert.Contains(stylesheet, "var(--bit-tpc-fld-ico-fs, var(--bit-TimePicker-icon-size, #{$siz-icon-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-tpc-cell-size, var(--bit-TimePicker-cell-size, #{spacing(3.75)}))");
        StringAssert.Contains(stylesheet, "var(--bit-tpc-cell-fs, var(--bit-TimePicker-cell-font-size, #{$tg-fs-lg}))");
        StringAssert.Contains(stylesheet, "var(--bit-tpc-ico-fs, var(--bit-TimePicker-spin-font-size, #{$tg-fs-xs}))");
        StringAssert.Contains(stylesheet, "var(--bit-tpc-sep-fs, var(--bit-TimePicker-separator-font-size, #{$tg-fs-xl}))");
        StringAssert.Contains(stylesheet, "var(--bit-tpc-ampm-fs, var(--bit-TimePicker-meridiem-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-tpc-act-fs, var(--bit-TimePicker-action-font-size, #{$tg-fs-sm}))");
        foreach (var size in new[] { "\n.bit-tpc-sm {", "\n.bit-tpc-md {", "\n.bit-tpc-lg {" })
        {
            Assert.IsFalse(SourceFiles.GetScssBlock(stylesheet, size).Contains("--bit-TimePicker-"), "A size class reads a public variable ahead of its own size.");
        }

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-TimePicker-[a-z-]+, var\(--bit-tpc-"), "A public variable is read before the parameter it restyles the default of.");
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-tpc-(inp-h|inp-fs|lbl-fs|des-fs|fld-ico-fs|cell-size|cell-fs|ico-fs|sep-fs|ampm-fs|act-fs)\)"), "A size the Size publishes is read with no default behind it.");
    }

    [TestMethod]
    public void BitTimePickerShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var resets = SourceFiles.GetScssBlock(stylesheet, "@mixin tpc-parameter-resets");

        // A TimePicker can sit in a template of another one, which must not inherit the outer one's Color or Size: the
        // root and the callout - which inherits nothing of the root - each start the values those classes publish out
        // unset, and the classes, declared further down at the same weight, still win on the element carrying them.
        foreach (var property in new[] { "--bit-tpc-rl-clr", "--bit-tpc-rl-txt", "--bit-tpc-rl-hover", "--bit-tpc-rl-active", "--bit-tpc-rl-focus",
                                         "--bit-tpc-inp-h", "--bit-tpc-inp-fs", "--bit-tpc-lbl-fs", "--bit-tpc-des-fs", "--bit-tpc-fld-ico-fs",
                                         "--bit-tpc-cell-size", "--bit-tpc-cell-fs", "--bit-tpc-ico-fs", "--bit-tpc-sep-fs", "--bit-tpc-ampm-fs",
                                         "--bit-tpc-act-fs" })
        {
            StringAssert.Contains(resets, $"{property}: initial;");
        }

        foreach (var element in new[] { "\n.bit-tpc {", "\n.bit-tpc-cal {" })
        {
            var declarations = SourceFiles.GetScssDeclarations(stylesheet, element);
            StringAssert.Contains(declarations, "@include tpc-parameter-resets;");
            StringAssert.Contains(declarations, "@include tpc-color-resolution;");
        }

        var calloutIndex = stylesheet.IndexOf("\n.bit-tpc-cal {", System.StringComparison.Ordinal);
        Assert.IsTrue(calloutIndex < stylesheet.IndexOf("\n    .bit-tpc-#{$role} {", System.StringComparison.Ordinal), "The role classes come before the callout that resets what they publish.");
        Assert.IsTrue(calloutIndex < stylesheet.IndexOf("\n.bit-tpc-sm {", System.StringComparison.Ordinal), "The size classes come before the callout that resets what they publish.");
    }
}
