using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Toggle;

/// <summary>
/// Pins the order the toggle's stylesheet ranks its values in, which a bUnit render cannot see: an explicit Color or
/// Size outranks the public --bit-Toggle-* variable restyling what it sets, which in turn outranks the default an
/// unset parameter stands for.
/// </summary>
[TestClass]
public class BitToggleStylesheetTests
{
    [TestMethod]
    public void BitToggleShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the default an
        // unset one stands for - including the geometry a glyph in the knob floors, which an explicit Size floors too.
        StringAssert.Contains(stylesheet, "--bit-tgl-trk-w: var(--bit-tgl-trk-w-base, var(--bit-Toggle-track-width, #{$siz-switch-w-md}));");
        StringAssert.Contains(stylesheet, "--bit-tgl-trk-h: var(--bit-tgl-trk-h-base, var(--bit-Toggle-track-height, #{$siz-switch-h-md}));");
        StringAssert.Contains(stylesheet, "--bit-tgl-thb-size: var(--bit-tgl-thb-size-base, var(--bit-Toggle-thumb-size, #{$siz-switch-thumb-md}));");
        StringAssert.Contains(stylesheet, "--bit-tgl-trk-w: var(--bit-tgl-trk-w-tic, var(--bit-Toggle-track-width, ");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-tgl-lbl-fontsize, var(--bit-Toggle-label-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-tgl-des-fontsize, var(--bit-Toggle-description-font-size, #{$tg-fs-xs}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-tgl-stx-fontsize, var(--bit-Toggle-text-font-size, inherit));");
        StringAssert.Contains(stylesheet, "padding-inline: var(--bit-tgl-cnn-pad, var(--bit-Toggle-content-padding, #{spacing(0.375)}));");

        // So does an explicit Color, for the checked colors it paints and the focus ring.
        StringAssert.Contains(stylesheet, "--bit-tgl-trk-clr-bg: var(--bit-tgl-clr, var(--bit-Toggle-checked-background, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "--bit-tgl-thb-clr: var(--bit-tgl-clr-text, var(--bit-Toggle-checked-thumb-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "--bit-tgl-cnn-clr: var(--bit-tgl-clr-text, var(--bit-Toggle-content-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "--bit-tgl-trk-clr-bg: var(--bit-tgl-clr-hover, var(--bit-Toggle-checked-hover-background, #{$clr-pri-hover}));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-tgl-clr-focus, var(--bit-Toggle-focus-color)))");

        // The values the parameters publish are never read behind a public variable. The glyph, the spinner and their
        // sizes follow the knob and the track rather than a parameter, so they stay the variables'.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Toggle-[a-z-]+, (max\()?var\(--bit-tgl-(clr|gap|trk-w-base|trk-h-base|thb-size-base|trk-w-ico|trk-h-ico|thb-size-ico|cnn-fontsize|cnn-pad|lbl-fontsize|stx-fontsize|des-fontsize)\b"),
                       "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitToggleShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-tgl {");

        // A toggle can sit in the label, the track content or the description of another one, which must not inherit
        // the outer toggle's Color or Size: each root starts the values those classes publish out unset, and the
        // classes - declared further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-tgl-clr", "--bit-tgl-clr-hover", "--bit-tgl-clr-text", "--bit-tgl-clr-focus", "--bit-tgl-gap",
                                         "--bit-tgl-trk-w-base", "--bit-tgl-trk-h-base", "--bit-tgl-thb-size-base", "--bit-tgl-trk-w-ico",
                                         "--bit-tgl-trk-h-ico", "--bit-tgl-thb-size-ico", "--bit-tgl-cnn-fontsize", "--bit-tgl-cnn-pad",
                                         "--bit-tgl-lbl-fontsize", "--bit-tgl-stx-fontsize", "--bit-tgl-des-fontsize" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "Toggle", "BitToggle.scss");
}
