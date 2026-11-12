using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Carousel;

/// <summary>
/// Pins what a bUnit render cannot see of the carousel: that a parameter written on it ranks above the public
/// --bit-Carousel-* variable restyling the same thing, and that a carousel nested in another one does not inherit its
/// choice.
/// </summary>
[TestClass]
public class BitCarouselStylesheetTests
{
    [TestMethod]
    public void BitCarouselShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Color, Accent or Size publishes these only while it is set, so they are read before the variable,
        // which only restyles the default an unset one stands for.
        Assert.Contains("$csl-clr: var(--bit-csl-clr, var(--bit-Carousel-dot-current-color, #{$clr-pri}));", stylesheet);
        Assert.Contains("$csl-clr-hover: var(--bit-csl-clr-hover, var(--bit-Carousel-dot-current-hover-color, #{$clr-pri-hover}));", stylesheet);
        Assert.Contains("$csl-btn: var(--bit-csl-btn, var(--bit-Carousel-button-color, #{$clr-fg-pri}));", stylesheet);
        Assert.Contains("$csl-btn-hover: var(--bit-csl-btn-hover, var(--bit-Carousel-button-hover-color, #{$clr-fg-pri-hover}));", stylesheet);
        Assert.Contains("--bit-csl-focus: var(--bit-csl-fcs, var(--bit-Carousel-focus-color));", stylesheet);
        Assert.Contains("--bit-csl-dotsz: var(--bit-csl-dot-size, var(--bit-Carousel-dot-size, #{spacing(1.25)}));", stylesheet);
        Assert.Contains("font-size: var(--bit-csl-btn-size, var(--bit-Carousel-button-size, calc(#{$siz-icon-md} * 1.5)));", stylesheet);
        Assert.Contains("font-size: var(--bit-csl-ppb-size, #{$siz-icon-md});", stylesheet);

        // The role classes publish the focus color bare, so it wins over the variable.
        Assert.Contains("--bit-csl-fcs: #{role($tokens, focus)};", stylesheet);

        // The one private value read after a public variable is the spacing the dots row works out from the resolved
        // dot size, which is only the default of a gap no parameter sets.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Carousel-[a-z-]+, var\(--bit-csl-(?!dots-gap\))"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitCarouselShouldPublishItsColorAccentAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-csl {");

        // A carousel can sit in a slide of another one, which must not inherit the outer carousel's Color, Accent or
        // Size: each root starts the values those classes publish out unset, and the classes - declared further down at
        // the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-csl-clr", "--bit-csl-clr-hover", "--bit-csl-btn", "--bit-csl-btn-hover", "--bit-csl-fcs", "--bit-csl-dot-size", "--bit-csl-btn-size", "--bit-csl-ppb-size" })
        {
            Assert.Contains($"{property}: initial;", root);
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Lists", "Carousel", "BitCarousel.scss");
}
