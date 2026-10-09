using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ToggleButton;

/// <summary>
/// Pins the order in which the toggle button reads its public --bit-ToggleButton-* variables against the values its
/// Color and Size parameters publish, which a bUnit render cannot see: an explicit parameter wins over the variable
/// that restyles what it sets, and the variable only restyles the default an unset parameter stands for.
/// </summary>
[TestClass]
public partial class BitToggleButtonStylesheetTests
{
    [TestMethod]
    public void BitToggleButtonShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // toggle button an unset one stands for.
        StringAssert.Contains(stylesheet, "var(--bit-tgb-fontsize, var(--bit-ToggleButton-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-tgb-min-height, var(--bit-ToggleButton-min-height, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-tgb-padding, var(--bit-ToggleButton-padding, #{$siz-ctrl-pad-y-md} #{$siz-ctrl-pad-x-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-tgb-ntx-pad, var(--bit-ToggleButton-padding, #{$siz-ctrl-pad-y-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-tgb-ntx-icn-size, var(--bit-ToggleButton-icon-size, #{spacing(2.35)}))");
        StringAssert.Contains(stylesheet, "var(--bit-tgb-spn-size, var(--bit-ToggleButton-spinner-size, #{spacing(2.35)}))");
        StringAssert.Contains(stylesheet, "var(--bit-tgb-lbl-fontsize, var(--bit-ToggleButton-loading-label-font-size, #{$tg-fs-xs}))");

        // So does an explicit Color, for every color the role paints - the checked state's included, since the state
        // is the rule's but the colors it shows are still the role's.
        StringAssert.Contains(stylesheet, "--bit-tgb-fg: var(--bit-tgb-clr-txt, var(--bit-ToggleButton-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgb-clr, var(--bit-ToggleButton-background, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgb-clr-hover, var(--bit-ToggleButton-hover-background, #{$clr-pri-hover}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgb-clr-active, var(--bit-ToggleButton-active-background, #{$clr-pri-active}));");
        StringAssert.Contains(stylesheet, "--bit-tgb-fg: var(--bit-tgb-clr-txt, var(--bit-ToggleButton-checked-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgb-clr-dark, var(--bit-ToggleButton-checked-background, #{$clr-pri-dark}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgb-clr-dis, var(--bit-ToggleButton-disabled-background, #{$clr-pri-dis}));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-tgb-clr-focus, var(--bit-ToggleButton-focus-color)))");

        // What a Color does not paint stays the variables' alone: the transparent background of Outline and Text.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-ToggleButton-background, transparent);");

        // Two reads keep the variable in front on purpose, neither of them in front of a value a parameter publishes
        // on its own: the gap, which only the absence of a label zeroes, and the minimum width, which Size does not
        // set - an icon-only toggle button only falls back to its minimum height when no minimum width is given.
        var body = SourceFiles.StripScssComments(stylesheet);
        var publicFirst = PublicBeforePrivate().Matches(body);

        Assert.HasCount(2, publicFirst, "A public variable is read before the parameter it restyles the default of.");
        StringAssert.Contains(body, "gap: var(--bit-ToggleButton-gap, var(--bit-tgb-icn-margin));");
        StringAssert.Contains(body, "min-width: var(--bit-ToggleButton-min-width, var(--bit-tgb-min-height, var(--bit-ToggleButton-min-height, #{$siz-ctrl-md})));");
    }

    [TestMethod]
    public void BitToggleButtonShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-tgb {");

        // A toggle button can sit in the content of another one, which must not inherit the outer one's Color or
        // Size: each root starts the values those classes publish out unset, and the classes - declared further
        // down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-tgb-clr-txt", "--bit-tgb-clr", "--bit-tgb-clr-hover", "--bit-tgb-clr-active", "--bit-tgb-clr-focus",
                                         "--bit-tgb-clr-dark", "--bit-tgb-clr-dark-hover", "--bit-tgb-clr-dark-active", "--bit-tgb-clr-dis",
                                         "--bit-tgb-clr-dis-text", "--bit-tgb-min-height", "--bit-tgb-padding", "--bit-tgb-fontsize",
                                         "--bit-tgb-ntx-pad", "--bit-tgb-ntx-icn-size", "--bit-tgb-spn-size", "--bit-tgb-lbl-fontsize" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Buttons", "ToggleButton", "BitToggleButton.scss");

    [GeneratedRegex(@"var\(--bit-ToggleButton-[a-z-]+, var\(--bit-tgb-")]
    private static partial Regex PublicBeforePrivate();
}
