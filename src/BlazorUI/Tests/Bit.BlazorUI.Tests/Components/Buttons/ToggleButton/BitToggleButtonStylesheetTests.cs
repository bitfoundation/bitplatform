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
        StringAssert.Contains(stylesheet, "--bit-tgb-fg: var(--bit-tgb-clr-txt, var(--bit-ToggleButton-fill-color, var(--bit-ToggleButton-color, #{$clr-pri-text})));");
        // The hover and press states set the variables the rules at rest paint from (--bit-tgb-sfg / -sbg / -sbr), never
        // the colors themselves, so the state's value is read ahead of the rest value it stands in for.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgb-sbg, var(--bit-tgb-clr, var(--bit-ToggleButton-fill-background, var(--bit-ToggleButton-background, #{$clr-pri}))));");
        StringAssert.Contains(stylesheet, "--bit-tgb-sbg: var(--bit-tgb-clr-hover, var(--bit-ToggleButton-fill-hover-background, var(--bit-ToggleButton-hover-background, #{$clr-pri-hover})));");
        StringAssert.Contains(stylesheet, "--bit-tgb-sbg: var(--bit-tgb-clr-active, var(--bit-ToggleButton-fill-active-background, var(--bit-ToggleButton-active-background, #{$clr-pri-active})));");
        StringAssert.Contains(stylesheet, "--bit-tgb-fg: var(--bit-tgb-clr-txt, var(--bit-ToggleButton-checked-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgb-sbg, var(--bit-tgb-clr-dark, var(--bit-ToggleButton-checked-background, #{$clr-pri-dark})));");
        StringAssert.Contains(stylesheet, "--bit-tgb-sbg: var(--bit-tgb-clr-dark-hover, var(--bit-ToggleButton-checked-hover-background, #{$clr-pri-dark-hover}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgb-clr-dis, var(--bit-ToggleButton-fill-disabled-background, var(--bit-ToggleButton-disabled-background, #{$clr-pri-dis})));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-tgb-clr-focus, var(--bit-ToggleButton-focus-color)))");

        // What a Color does not paint stays the variables' alone: the transparent background of Outline and Text.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgb-sbg, var(--bit-ToggleButton-outline-background, var(--bit-ToggleButton-background, transparent)));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgb-sbg, var(--bit-ToggleButton-text-background, var(--bit-ToggleButton-background, transparent)));");

        // Two reads keep the variable in front on purpose, neither of them in front of a value a parameter publishes
        // on its own: the gap, which only the absence of a label zeroes, and the minimum width, which Size does not
        // set - an icon-only toggle button only falls back to its minimum height when no minimum width is given.
        var body = SourceFiles.StripScssComments(stylesheet);
        var publicFirst = PublicBeforePrivate().Matches(body);

        Assert.HasCount(2, publicFirst, "A public variable is read before the parameter it restyles the default of.");
        StringAssert.Contains(body, "gap: var(--bit-ToggleButton-gap, var(--bit-tgb-icn-margin));");
        StringAssert.Contains(body, "min-width: var(--bit-ToggleButton-min-width, var(--bit-tgb-min-height, var(--bit-ToggleButton-min-height, #{$siz-ctrl-md})));");
    }

    [TestMethod,
        DataRow("fill", ".bit-tgb-fil"),
        DataRow("outline", ".bit-tgb-otl"),
        DataRow("text", ".bit-tgb-txt")]
    public void BitToggleButtonShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // Each variant reads a tier of its own ahead of every shared color variable of the unchecked state, so a theme
        // can set one look apart from the other two on :root - an outline toggle button's neutral hairline without a
        // ring around every filled one.
        foreach (var name in new[] { "color", "background", "border-color",
                                     "hover-color", "hover-background", "hover-border-color",
                                     "active-color", "active-background", "active-border-color",
                                     "disabled-color", "disabled-background", "disabled-border-color" })
        {
            StringAssert.Contains(block, $"var(--bit-ToggleButton-{variant}-{name}, var(--bit-ToggleButton-{name}, ",
                                  $"{selector} does not read --bit-ToggleButton-{variant}-{name} ahead of --bit-ToggleButton-{name}.");
        }

        // The foreground FixedColor pins is the variant's too, set by the variant rather than the root.
        StringAssert.Contains(block, $"--bit-tgb-fg: var(--bit-tgb-clr-txt, var(--bit-ToggleButton-{variant}-color, var(--bit-ToggleButton-color, #{{$clr-pri-text}})));");

        // A variant reads a shared color variable only right behind its own twin, and never the tier of another variant.
        Assert.IsFalse(Regex.IsMatch(block, $@"(?<!var\(--bit-ToggleButton-{variant}-[a-z-]+, )var\(--bit-ToggleButton-(?:hover-|active-|disabled-)?(?:color|background|border-color)\b"),
                       $"{selector} reads a shared color variable without its own tier ahead of it.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-ToggleButton-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");
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
                                         "--bit-tgb-ntx-pad", "--bit-tgb-ntx-icn-size", "--bit-tgb-spn-size", "--bit-tgb-lbl-fontsize",
                                         // The colors the hover and press states move, which a nested toggle button must not inherit either.
                                         "--bit-tgb-sfg", "--bit-tgb-sbg", "--bit-tgb-sbr" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Buttons", "ToggleButton", "BitToggleButton.scss");

    [GeneratedRegex(@"var\(--bit-ToggleButton-[a-z-]+, var\(--bit-tgb-")]
    private static partial Regex PublicBeforePrivate();
}
