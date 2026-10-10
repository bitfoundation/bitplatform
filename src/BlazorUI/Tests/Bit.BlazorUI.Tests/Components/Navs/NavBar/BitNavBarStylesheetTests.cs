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
        StringAssert.Contains(root, "--bit-nbr-ico-fs: var(--bit-nbr-sz-ico, var(--bit-NavBar-icon-size, #{$tg-fs-md}));");
        StringAssert.Contains(root, "--bit-nbr-txt-fs: var(--bit-nbr-sz-txt, var(--bit-NavBar-text-size, #{$tg-fs-sm}));");
        StringAssert.Contains(root, "--bit-nbr-pad: var(--bit-nbr-sz-pad, var(--bit-NavBar-item-padding, #{spacing(1)}));");
        StringAssert.Contains(root, "--bit-nbr-row-pad: var(--bit-nbr-sz-row-pad, var(--bit-NavBar-item-padding, #{spacing(2)}));");
        StringAssert.Contains(root, "--bit-nbr-min: var(--bit-nbr-sz-min, var(--bit-NavBar-item-min-size, #{$siz-ctrl-md}));");

        // The size classes publish the plain steps and read no public variable of their own.
        foreach (var size in new[] { "sm", "md", "lg" })
        {
            Assert.IsFalse(SourceFiles.GetScssBlock(stylesheet, $"\n.bit-nbr-{size} {{").Contains("--bit-NavBar-"), $"The {size} class reads a public variable ahead of the size.");
        }

        // An explicit Color is read first for every color it paints (its filled recipe ahead of the plain role), then
        // the variable, then the primary role an unset Color stands for - filled or not.
        StringAssert.Contains(stylesheet, "color: var(--bit-nbr-clr-acc-text, var(--bit-nbr-clr, var(--bit-NavBar-selected-color, var(--bit-nbr-fil-text, #{$clr-pri}))));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-nbr-clr-acc-hover, var(--bit-NavBar-item-hover-background, var(--bit-nbr-fil-hover, transparent)));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-nbr-clr-ind, var(--bit-NavBar-indicator-color, var(--bit-nbr-fil-text, #{$clr-pri})));");
        StringAssert.Contains(stylesheet, "@include focus-ring-own(var(--bit-nbr-clr-fcs, var(--bit-NavBar-focus-color)));");
        StringAssert.Contains(stylesheet, "color: var(--bit-nbr-clr-dis, var(--bit-NavBar-disabled-color, #{$clr-pri-dis-text}));");

        // Filled only switches the default (--bit-nbr-fil-*), which the variables are read ahead of; nothing else a
        // parameter publishes may sit behind a public variable.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-NavBar-[a-z-]+, var\(--bit-nbr-(?!fil-)"), "A public variable is read before the parameter it restyles the default of.");
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
            StringAssert.Contains(root, $"{property}: initial;");
        }

        var rootAt = stylesheet.IndexOf("\n.bit-nbr {", System.StringComparison.Ordinal);
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n.bit-nbr-md {", System.StringComparison.Ordinal), "The size classes are declared ahead of the root that resets them.");
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n    .bit-nbr-#{$role} {", System.StringComparison.Ordinal), "The color classes are declared ahead of the root that resets them.");
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n.bit-nbr-fil {", System.StringComparison.Ordinal), "The filled class is declared ahead of the root that resets it.");
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Navs", "NavBar", "BitNavBar.scss");
}
