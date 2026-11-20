using System.Linq;
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
                                         "--bit-tgl-lbl-fontsize", "--bit-tgl-stx-fontsize", "--bit-tgl-des-fontsize", "--bit-tgl-card-padding" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    [TestMethod]
    public void BitToggleCardShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes the padding of the card, an explicit Color the checked border, the hover border and
        // the tint of the checked card - each read ahead of the public variable restyling the default.
        StringAssert.Contains(stylesheet, "padding: var(--bit-tgl-card-padding, var(--bit-Toggle-card-padding, #{spacing(1.5)}));");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-tgl-md {"), "--bit-tgl-card-padding: #{spacing(1.5)};");
        StringAssert.Contains(stylesheet, "border-color: var(--bit-tgl-crd-sbrd, var(--bit-tgl-clr, var(--bit-Toggle-card-checked-border-color, var(--bit-Toggle-checked-background, #{$clr-pri}))));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-tgl-crd-sbg, var(--bit-tgl-crd-tint, var(--bit-tgl-crd-bg-checked)));");
        StringAssert.Contains(stylesheet, "--bit-tgl-crd-sbrd: var(--bit-tgl-clr-hover, var(--bit-Toggle-card-hover-border-color, ");

        // The accent of the checked card is the checked fill of the track, resolved on the root no variant qualifies.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-tgl {"), "--bit-tgl-acc: var(--bit-tgl-clr, var(--bit-Toggle-checked-background, #{$clr-pri}));");
    }

    [TestMethod,
        DataRow("fill", ".bit-tgl-fil", "card-background"),
        DataRow("fill", ".bit-tgl-fil", "card-hover-background"),
        DataRow("fill", ".bit-tgl-fil", "card-checked-background"),
        DataRow("fill", ".bit-tgl-fil", "card-border-color"),
        DataRow("outline", ".bit-tgl-otl", "card-background"),
        DataRow("outline", ".bit-tgl-otl", "card-hover-background"),
        DataRow("outline", ".bit-tgl-otl", "card-checked-background"),
        DataRow("outline", ".bit-tgl-otl", "card-border-color")]
    public void BitToggleShouldReadTheCardVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector, string name)
    {
        // The variant's own rule, not the card rule the Outline and Fill variants share (".bit-tgl-otl,\n.bit-tgl-fil {").
        var stylesheet = ReadStylesheet();
        var own = SourceFiles.GetScssRules(stylesheet).Single(r => r.Header == selector && r.Ancestors.Count == 0);
        var block = SourceFiles.GetScssBlock(stylesheet[own.Index..], $"{selector} {{");

        // Each variant reads a tier of its own ahead of the shared card variables, so a theme can set outline and filled
        // cards apart on :root - and the card rules both variants share read only what each variant resolved.
        StringAssert.Contains(block, $"var(--bit-Toggle-{variant}-{name}, var(--bit-Toggle-{name}, ",
                              $"{selector} does not read --bit-Toggle-{variant}-{name} ahead of --bit-Toggle-{name}.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-Toggle-(?!{variant}-)(?:fill|outline)-"),
                       $"{selector} reads the variables of another variant.");
    }

    [TestMethod]
    public void BitToggleCardHoverShouldMoveAVariableRatherThanPaint()
    {
        var stylesheet = ReadStylesheet();
        var hover = SourceFiles.GetScssBlock(stylesheet, ":is(.bit-tgl-otl, .bit-tgl-fil):hover:not(.bit-dis):not(.bit-inv):not(.bit-tgl-rdl):not(.bit-tgl-ldg) {");

        // A hover that painted the border itself would outrank a class the app hands the root, so it moves the variables
        // the card paints from - which the root starts out unset, so a card nested in another never inherits its hover.
        StringAssert.Contains(hover, "--bit-tgl-crd-sbrd:");
        StringAssert.Contains(hover, "--bit-tgl-crd-sbg: var(--bit-tgl-crd-bg-hover);");
        Assert.IsFalse(Regex.IsMatch(hover, @"(^|[\s;{])(border-color|background-color)\s*:"), "The hover of a card paints instead of moving a variable.");

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-tgl {");

        StringAssert.Contains(root, "--bit-tgl-crd-sbg: initial;");
        StringAssert.Contains(root, "--bit-tgl-crd-sbrd: initial;");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "Toggle", "BitToggle.scss");
}
