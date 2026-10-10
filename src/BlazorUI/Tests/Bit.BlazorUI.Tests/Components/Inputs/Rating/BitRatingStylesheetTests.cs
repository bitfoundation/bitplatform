using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Rating;

/// <summary>
/// Pins the order the rating's stylesheet ranks its values in, which a bUnit render cannot see: an explicit Color or
/// Size outranks the public --bit-Rating-* variable restyling what it sets, which in turn outranks the default an
/// unset parameter stands for.
/// </summary>
[TestClass]
public class BitRatingStylesheetTests
{
    [TestMethod]
    public void BitRatingShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the default an
        // unset one stands for.
        StringAssert.Contains(stylesheet, "font-size: var(--bit-rtg-size, var(--bit-Rating-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-rtg-lbl-size, var(--bit-Rating-label-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-rtg-dsc-size, var(--bit-Rating-description-font-size, #{$tg-fs-xs}));");

        // So does an explicit Color, for the fill in every state and for the focus ring.
        StringAssert.Contains(stylesheet, "color: var(--bit-rtg-clr, var(--bit-Rating-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "color: var(--bit-rtg-clr-hover, var(--bit-Rating-hover-color, #{$clr-pri-hover}));");
        StringAssert.Contains(stylesheet, "color: var(--bit-rtg-clr-active, var(--bit-Rating-active-color, #{$clr-pri-active}));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-rtg-clr-focus, var(--bit-Rating-focus-color)))");

        // The values the parameters publish are never read behind a public variable. The unfilled part is not the
        // Color's to paint (--bit-rtg-clr-uns), so it stays the variable's.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Rating-[a-z-]+, var\(--bit-rtg-(clr|clr-hover|clr-active|clr-focus|size|lbl-size|dsc-size)\)"),
                       "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitRatingShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-rtg {");

        // A rating can sit in the label or an item template of another one, which must not inherit the outer rating's
        // Color or Size: each root starts the values those classes publish out unset, and the classes - declared
        // further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-rtg-clr", "--bit-rtg-clr-hover", "--bit-rtg-clr-active", "--bit-rtg-clr-focus",
                                         "--bit-rtg-size", "--bit-rtg-lbl-size", "--bit-rtg-dsc-size" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "Rating", "BitRating.scss");
}
