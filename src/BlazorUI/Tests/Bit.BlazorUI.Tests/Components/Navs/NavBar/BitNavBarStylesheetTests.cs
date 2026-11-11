using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.NavBar;

/// <summary>
/// Pins the order the stylesheet reads the public --bit-NavBar-* variables in, which a bUnit render cannot see: a
/// parameter written on the navbar wins over the variable that restyles what it sets, and the values the Color, Size
/// and Filled classes publish start out unset on every root.
/// </summary>
[TestClass]
public class BitNavBarStylesheetTests
{
    [TestMethod]
    public void BitNavBarShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-nbr {");

        // An explicit Size publishes the steps, which the root reads before the variable; an unset one is the medium
        // navbar the variable restyles.
        Assert.Contains("--bit-nbr-ico-fs: var(--bit-nbr-sz-ico, var(--bit-NavBar-icon-size, #{$tg-fs-md}));", root);
        Assert.Contains("--bit-nbr-txt-fs: var(--bit-nbr-sz-txt, var(--bit-NavBar-text-size, #{$tg-fs-sm}));", root);
        Assert.Contains("--bit-nbr-pad: var(--bit-nbr-sz-pad, var(--bit-NavBar-item-padding, #{spacing(1)}));", root);
        Assert.Contains("--bit-nbr-row-pad: var(--bit-nbr-sz-row-pad, var(--bit-NavBar-item-padding, #{spacing(2)}));", root);
        Assert.Contains("--bit-nbr-min: var(--bit-nbr-sz-min, var(--bit-NavBar-item-min-size, #{$siz-ctrl-md}));", root);

        // The size classes publish the plain steps and read no public variable of their own.
        foreach (var size in new[] { "sm", "md", "lg" })
        {
            Assert.DoesNotContain("--bit-NavBar-", SourceFiles.GetScssBlock(stylesheet, $"\n.bit-nbr-{size} {{"), $"The {size} class reads a public variable ahead of the size.");
        }

        // An explicit Color is read first for every color it paints (its filled recipe ahead of the plain role), then
        // the variable, then the primary role an unset Color stands for - filled or not.
        Assert.Contains("color: var(--bit-nbr-clr-acc-text, var(--bit-nbr-clr, var(--bit-NavBar-selected-color, var(--bit-nbr-fil-text, #{$clr-pri}))));", stylesheet);
        Assert.Contains("background-color: var(--bit-nbr-clr-acc-hover, var(--bit-NavBar-item-hover-background, var(--bit-nbr-fil-hover, transparent)));", stylesheet);
        Assert.Contains("background-color: var(--bit-nbr-clr-ind, var(--bit-NavBar-indicator-color, var(--bit-nbr-fil-text, #{$clr-pri})));", stylesheet);
        Assert.Contains("@include focus-ring-own(var(--bit-nbr-clr-fcs, var(--bit-NavBar-focus-color)));", stylesheet);
        Assert.Contains("color: var(--bit-nbr-clr-dis, var(--bit-NavBar-disabled-color, #{$clr-pri-dis-text}));", stylesheet);

        // Filled only switches the default (--bit-nbr-fil-*), which the variables are read ahead of; nothing else a
        // parameter publishes may sit behind a public variable.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-NavBar-[a-z-]+, var\(--bit-nbr-(?!fil-)"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitNavBarShouldDrawTheLabelsOfAnUnfilledBarInTheRoleTextColor()
    {
        var roles = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    .bit-nbr-#{$role} {");

        // Without the fill the labels sit on the bar's own background, so a Color draws them in the role's text color -
        // a secondary or a warning main is picked to fill a surface and is under 3:1 as text on white - while the
        // indicator, a rule rather than text, keeps the main.
        Assert.Contains("--bit-nbr-clr: #{role-text($role, $tokens)};", roles);
        Assert.Contains("--bit-nbr-clr-ind: #{role($tokens, main)};", roles);
    }

    [TestMethod]
    public void BitNavBarShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-nbr {");

        // A navbar never inherits another one's Color, Size or Filled from an ancestor carrying those classes: each root
        // starts the values they publish out unset, and the classes - declared further down at the same weight - still
        // win on the root that carries them.
        foreach (var property in new[] { "--bit-nbr-clr", "--bit-nbr-clr-ind", "--bit-nbr-clr-dis", "--bit-nbr-clr-fcs", "--bit-nbr-clr-acc-hover",
                                         "--bit-nbr-clr-acc-active", "--bit-nbr-clr-acc-text", "--bit-nbr-fil-hover", "--bit-nbr-fil-active",
                                         "--bit-nbr-fil-text", "--bit-nbr-sz-ico", "--bit-nbr-sz-txt", "--bit-nbr-sz-gap", "--bit-nbr-sz-pad",
                                         "--bit-nbr-sz-sec-pad", "--bit-nbr-sz-row-pad", "--bit-nbr-sz-min" })
        {
            Assert.Contains($"{property}: initial;", root);
        }

        var rootAt = stylesheet.IndexOf("\n.bit-nbr {", System.StringComparison.Ordinal);
        Assert.IsLessThan(stylesheet.IndexOf("\n.bit-nbr-md {", System.StringComparison.Ordinal), rootAt, "The size classes are declared ahead of the root that resets them.");
        Assert.IsLessThan(stylesheet.IndexOf("\n    .bit-nbr-#{$role} {", System.StringComparison.Ordinal), rootAt, "The color classes are declared ahead of the root that resets them.");
        Assert.IsLessThan(stylesheet.IndexOf("\n.bit-nbr-fil {", System.StringComparison.Ordinal), rootAt, "The filled class is declared ahead of the root that resets it.");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Navs", "NavBar", "BitNavBar.scss");
}
