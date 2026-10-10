using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TagsInput;

/// <summary>
/// Pins the order the tags input's stylesheet ranks its values in, which a bUnit render cannot see: an explicit Color
/// or Size outranks the public --bit-TagsInput-* variable restyling what it sets, which in turn outranks the default an
/// unset parameter stands for.
/// </summary>
[TestClass]
public class BitTagsInputStylesheetTests
{
    [TestMethod]
    public void BitTagsInputShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the default an
        // unset one stands for.
        StringAssert.Contains(stylesheet, "font-size: var(--bit-tgi-fontsize, var(--bit-TagsInput-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "gap: var(--bit-tgi-gap, var(--bit-TagsInput-gap, #{spacing(0.375)}));");
        StringAssert.Contains(stylesheet, "min-height: var(--bit-tgi-minheight, var(--bit-TagsInput-min-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "var(--bit-tgi-tagminheight, var(--bit-TagsInput-tag-min-height, #{$siz-chip-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-tgi-tagfontsize, var(--bit-TagsInput-tag-font-size, #{$tg-fs-xs}))");
        StringAssert.Contains(stylesheet, "var(--bit-tgi-iconsize, var(--bit-TagsInput-icon-size, #{$siz-icon-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-tgi-helperfontsize, var(--bit-TagsInput-description-font-size, #{$tg-fs-xs}))");

        // So does an explicit Color: the chip colors of the tag variant are the Color's (unset while it is), then the
        // variable's, then the primary chip's; the field's focus ring and rule and the spinner rank the same way. The
        // chip reads them behind the value its own hover moves, which is unset while the chip is not hovered.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgi-tag-sbg, var(--bit-tgi-tag-bg, var(--bit-TagsInput-tag-background, var(--bit-tgi-tag-bg-dft))));");
        StringAssert.Contains(stylesheet, "color: var(--bit-tgi-tag-sfg, var(--bit-tgi-tag-clr, var(--bit-TagsInput-tag-color, var(--bit-tgi-tag-clr-dft))));");
        StringAssert.Contains(stylesheet, "var(--bit-tgi-tag-ring, var(--bit-TagsInput-tag-focus-color, var(--bit-tgi-tag-ring-dft)))");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-tgi-clr-focus, var(--bit-TagsInput-focus-color)))");
        StringAssert.Contains(stylesheet, "var(--bit-tgi-clr, var(--bit-TagsInput-focus-border-color, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-tgi-clr, var(--bit-TagsInput-spinner-color, #{$clr-pri}))");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-tgi-tgf {"), "--bit-tgi-tag-bg: var(--bit-tgi-clr);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-tgi-tgf {"), "--bit-tgi-tag-bg-dft: #{$clr-pri};");

        // The values the parameters publish are never read behind a public variable. The -dft values are the
        // Variant's recipe for the primary chip an unset Color stands for, so the variables stay ahead of them.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-TagsInput-[a-z-]+, var\(--bit-tgi-(clr|clr-txt|clr-hover|clr-focus|gap|fontsize|iconsize|helperfontsize|tagfontsize|minheight|padding|tagminheight|tagpadding|tag-clr|tag-brd|tag-bg|tag-ring|tag-clr-hover|tag-brd-hover|tag-bg-hover)\)"),
                       "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitTagsInputShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-tgi {");

        // A tags input can sit in a template of another one, which must not inherit the outer field's Color or Size:
        // each root starts the values those classes publish out unset, and the classes - declared further down at
        // the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-tgi-clr", "--bit-tgi-clr-txt", "--bit-tgi-clr-hover", "--bit-tgi-clr-focus", "--bit-tgi-gap",
                                         "--bit-tgi-fontsize", "--bit-tgi-iconsize", "--bit-tgi-helperfontsize", "--bit-tgi-tagfontsize",
                                         "--bit-tgi-minheight", "--bit-tgi-padding", "--bit-tgi-tagminheight", "--bit-tgi-tagpadding" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Inputs", "TagsInput", "BitTagsInput.scss");
}
