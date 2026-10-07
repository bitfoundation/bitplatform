using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Swiper;

/// <summary>
/// Pins what a bUnit render cannot see of the swiper: that a parameter written on it ranks above the public
/// --bit-Swiper-* variable restyling the same thing, and that a swiper nested in another one does not inherit its choice.
/// </summary>
[TestClass]
public class BitSwiperStylesheetTests
{
    [TestMethod]
    public void BitSwiperShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Color, Accent or Size publishes these only while it is set, so they are read before the variable,
        // which only restyles the default an unset one stands for.
        StringAssert.Contains(stylesheet, "$swp-clr: var(--bit-swp-clr, var(--bit-Swiper-dot-current-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "$swp-clr-hover: var(--bit-swp-clr-hover, var(--bit-Swiper-dot-current-hover-color, #{$clr-pri-hover}));");
        StringAssert.Contains(stylesheet, "$swp-btn: var(--bit-swp-btn, var(--bit-Swiper-button-color, #{$clr-fg-pri}));");
        StringAssert.Contains(stylesheet, "$swp-btn-hover: var(--bit-swp-btn-hover, var(--bit-Swiper-button-hover-color, #{$clr-fg-pri-hover}));");
        StringAssert.Contains(stylesheet, "--bit-swp-focus: var(--bit-swp-fcs, var(--bit-Swiper-focus-color, #{$clr-pri-focus}));");
        StringAssert.Contains(stylesheet, "--bit-swp-dot-size: var(--bit-swp-dotsz, var(--bit-Swiper-dot-size, #{spacing(1.25)}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-swp-btnsz, var(--bit-Swiper-button-size, #{spacing(3)}));");

        // The one private value read after a public variable is the dot size already resolved above, which is only the
        // default of the current dot's width - a width no parameter sets.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Swiper-[a-z-]+, var\(--bit-swp-(?!dot-size\))"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitSwiperShouldPublishItsColorAccentAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-swp {");

        // A swiper can sit in an item of another one, which must not inherit the outer swiper's Color, Accent or Size:
        // each root starts the values those classes publish out unset, and the classes - declared further down at the
        // same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-swp-dotsz", "--bit-swp-btnsz", "--bit-swp-clr", "--bit-swp-clr-hover", "--bit-swp-btn", "--bit-swp-btn-hover", "--bit-swp-fcs" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Lists", "Swiper", "BitSwiper.scss");
}
