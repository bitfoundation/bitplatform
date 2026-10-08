using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Slider;

/// <summary>
/// Pins the order the slider's stylesheet ranks its values in, which a bUnit render cannot see: an explicit Color or
/// Size outranks the public --bit-Slider-* variable restyling what it sets, which in turn outranks the default an
/// unset parameter stands for.
/// </summary>
[TestClass]
public class BitSliderStylesheetTests
{
    [TestMethod]
    public void BitSliderShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the default an
        // unset one stands for.
        StringAssert.Contains(stylesheet, "--bit-sld-thumb: var(--bit-sld-sz-thumb, var(--bit-Slider-thumb-size, #{$siz-slider-thumb-md}));");
        StringAssert.Contains(stylesheet, "--bit-sld-rail: var(--bit-sld-sz-rail, var(--bit-Slider-rail-size, #{$siz-track-md}));");
        StringAssert.Contains(stylesheet, "--bit-sld-length: var(--bit-sld-sz-length, var(--bit-Slider-length, #{spacing(24)}));");
        StringAssert.Contains(stylesheet, "--bit-sld-font-size: var(--bit-sld-sz-font-size, var(--bit-Slider-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-sld-sz-font-size-sub, var(--bit-Slider-mark-label-font-size, #{$tg-fs-xs}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-sld-sz-font-size-sub, var(--bit-Slider-thumb-label-font-size, #{$tg-fs-xs}));");

        // So does an explicit Color, for the accent in every state and for what is drawn on top of it.
        StringAssert.Contains(stylesheet, "var(--bit-sld-clr, var(--bit-Slider-color, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-sld-clr-hover, var(--bit-Slider-hover-color, var(--bit-Slider-color, #{$clr-pri-hover})))");
        StringAssert.Contains(stylesheet, "var(--bit-sld-clr-active, var(--bit-Slider-active-color, var(--bit-Slider-color, #{$clr-pri-active})))");
        StringAssert.Contains(stylesheet, "--bit-sld-clr-ring: var(--bit-sld-clr-focus, var(--bit-Slider-focus-color, var(--bit-Slider-color, #{$clr-pri-focus})));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-sld-clr-on, var(--bit-Slider-mark-active-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "color: var(--bit-sld-clr-on, var(--bit-Slider-thumb-label-color, #{$clr-pri-text}));");

        // A role class publishes the role's tokens alone, never a public variable ahead of them.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"--bit-sld-clr(-on|-hover|-active|-focus)?: var\(--bit-Slider-"), "A Color class reads a public variable ahead of the role.");

        // The values the parameters publish are never read behind a public variable. The rail radius follows the
        // rail thickness rather than a parameter, so it stays the variable's.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Slider-[a-z-]+, var\(--bit-sld-(clr|sz-)"),
                       "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitSliderShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-sld {");

        // A slider can sit in the label template of another one, which must not inherit the outer slider's Color or
        // Size: each root starts the values those classes publish out unset, and the classes - declared further down
        // at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-sld-clr", "--bit-sld-clr-on", "--bit-sld-clr-hover", "--bit-sld-clr-active", "--bit-sld-clr-focus",
                                         "--bit-sld-sz-thumb", "--bit-sld-sz-rail", "--bit-sld-sz-length", "--bit-sld-sz-font-size",
                                         "--bit-sld-sz-font-size-sub" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Inputs", "Slider", "BitSlider.scss");
}
