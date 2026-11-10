using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.CircularTimePicker;

/// <summary>
/// Pins the order the CircularTimePicker's stylesheet reads its colors and sizes in, which a bUnit render cannot see:
/// what an explicit Color or Size publishes is read before the public --bit-CircularTimePicker-* variable restyling
/// it, and the default an unset one stands for comes last - on the root and on the callout rendered beside it alike.
/// </summary>
[TestClass]
public class BitCircularTimePickerStylesheetTests
{
    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "CircularTimePicker", "BitCircularTimePicker.scss");

    [TestMethod]
    public void BitCircularTimePickerShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Color publishes the role slots, which are read before the variables, then the primary role an
        // unset one stands for - for the accent itself and for every color that defaults to it.
        StringAssert.Contains(stylesheet, "--bit-ctp-clr: var(--bit-ctp-rl-clr, var(--bit-CircularTimePicker-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "--bit-ctp-clr-text: var(--bit-ctp-rl-text, var(--bit-CircularTimePicker-text-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "--bit-ctp-clr-focus: var(--bit-ctp-rl-focus, var(--bit-CircularTimePicker-focus-color));");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-rl-clr, var(--bit-CircularTimePicker-pointer-color, ");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-rl-clr, var(--bit-CircularTimePicker-toolbar-background, ");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-rl-text, var(--bit-CircularTimePicker-toolbar-color, ");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-rl-clr, var(--bit-CircularTimePicker-selected-number-background, ");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-rl-text, var(--bit-CircularTimePicker-selected-number-color, ");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-rl-clr, var(--bit-CircularTimePicker-selected-ampm-background, ");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-rl-text, var(--bit-CircularTimePicker-selected-ampm-color, ");
        // The label of an action button is read on the callout rather than over the role's fill, so it takes the role's
        // text color - a secondary or a warning main is picked to fill a surface and is under 3:1 as text on white.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-ctp-abn {"), "color: var(--bit-ctp-rl-fg, var(--bit-CircularTimePicker-action-color, var(--bit-ctp-clr)));");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n    .bit-ctp-#{$role} {"), "--bit-ctp-rl-fg: #{role-text($role, $tokens)};");
        Assert.IsFalse(SourceFiles.GetScssBlock(stylesheet, "\n    .bit-ctp-#{$role} {").Contains("--bit-CircularTimePicker-"), "A role class reads a public variable ahead of its own color.");

        // So does an explicit Size, for every size a variable restyles.
        StringAssert.Contains(stylesheet, "--bit-ctp-clk: var(--bit-ctp-clk-size, var(--bit-CircularTimePicker-clock-size, #{spacing(32.5)}));");
        StringAssert.Contains(stylesheet, "--bit-ctp-num: var(--bit-ctp-num-size, var(--bit-CircularTimePicker-number-size, #{spacing(4)}));");
        StringAssert.Contains(stylesheet, "--bit-ctp-thb: var(--bit-ctp-thb-size, var(--bit-CircularTimePicker-thumb-size, #{spacing(3.5)}));");
        StringAssert.Contains(stylesheet, "--bit-ctp-thb-min: var(--bit-ctp-thb-size-min, var(--bit-CircularTimePicker-minor-thumb-size, #{spacing(1.75)}));");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-lbl-fs, var(--bit-CircularTimePicker-label-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-inp-h, var(--bit-CircularTimePicker-input-height, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-inp-fs, var(--bit-CircularTimePicker-input-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-txt-fontsize, var(--bit-CircularTimePicker-time-font-size, calc(#{$tg-fs-4xl} * 1.375)))");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-apb-fontsize, var(--bit-CircularTimePicker-ampm-font-size, #{$tg-fs-xl}))");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-apk-fontsize, var(--bit-CircularTimePicker-clock-ampm-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-ctp-num-fontsize, var(--bit-CircularTimePicker-number-font-size, 1em))");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-CircularTimePicker-[a-z-]+,\s*var\(--bit-ctp-(rl-|inp-h|inp-fs|lbl-fs|clk-size|num-size|txt-fontsize|apb-fontsize|apk-fontsize|num-fontsize|thb-size)"),
                       "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitCircularTimePickerShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-ctp,\n.bit-ctp-cal {");

        // A picker can sit in a template of another one, which must not inherit the outer one's Color or Size: the root
        // and the callout - a sibling of the root, so it carries the classes itself - each start the values those
        // classes publish out unset, and the classes, declared further down at the same weight, still win on the
        // element carrying them.
        foreach (var property in new[] { "--bit-ctp-rl-clr", "--bit-ctp-rl-fg", "--bit-ctp-rl-text", "--bit-ctp-rl-focus", "--bit-ctp-inp-h", "--bit-ctp-inp-fs",
                                         "--bit-ctp-lbl-fs", "--bit-ctp-clk-size", "--bit-ctp-num-size", "--bit-ctp-txt-fontsize",
                                         "--bit-ctp-apb-fontsize", "--bit-ctp-apk-fontsize", "--bit-ctp-num-fontsize", "--bit-ctp-thb-size",
                                         "--bit-ctp-thb-size-min" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }

        var rootIndex = stylesheet.IndexOf("\n.bit-ctp,\n.bit-ctp-cal {", System.StringComparison.Ordinal);
        Assert.IsTrue(rootIndex < stylesheet.IndexOf("\n    .bit-ctp-#{$role} {", System.StringComparison.Ordinal), "The role classes come before the root that resets what they publish.");
        Assert.IsTrue(rootIndex < stylesheet.IndexOf("\n.bit-ctp-sm {", System.StringComparison.Ordinal), "The size classes come before the root that resets what they publish.");
    }

    [TestMethod]
    public void BitCircularTimePickerShouldLetThePointerThroughItsHand()
    {
        // The hand is drawn over the numbers - the hand of an outer-ring hour across the inner-ring hour at its angle -
        // so it takes no pointer of its own: a press, the hover and the cursor are those of the number underneath. Its
        // thumb inherits that.
        StringAssert.Contains(SourceFiles.StripScssComments(SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-ctp-ptr {")), "pointer-events: none;");
    }

    [TestMethod]
    public void BitCircularTimePickerShouldReachOutUnderAThumbLargerThanTheDial()
    {
        // A thumb larger than the gap a number leaves inside the rim reaches past it, and it takes no pointer of its own:
        // the face reaches out under it by as much, so a press on the part outside the dial is still the face's - and
        // reaches nowhere while the thumb fits.
        var reach = SourceFiles.StripScssComments(SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-ctp-clf::before {"));

        StringAssert.Contains(reach, "position: absolute;");
        StringAssert.Contains(reach, "inset: min(0px, calc(var(--bit-ctp-num) / 2 + var(--bit-ctp-num-gap) - max(var(--bit-ctp-thb), var(--bit-ctp-thb-min)) / 2));");
    }
}
