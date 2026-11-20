using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Checkbox;

/// <summary>
/// Pins the order the checkbox's stylesheet ranks its values in, which a bUnit render cannot see: an explicit Color or
/// Size outranks the public --bit-Checkbox-* variable restyling what it sets, which in turn outranks the default an
/// unset parameter stands for.
/// </summary>
[TestClass]
public class BitCheckboxStylesheetTests
{
    [TestMethod]
    public void BitCheckboxShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the default an
        // unset one stands for.
        StringAssert.Contains(stylesheet, "--bit-chb-box-side: var(--bit-chb-box-size, var(--bit-Checkbox-box-size, #{$siz-sel-md}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-chb-lbl-fontsize, var(--bit-Checkbox-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-chb-des-fontsize, var(--bit-Checkbox-description-font-size, #{$tg-fs-xs}));");

        // So does an explicit Color, for every color it paints - including the disabled ones, which the disabled
        // state still picks.
        StringAssert.Contains(stylesheet, "--bit-chb-box-clr-bg: var(--bit-chb-clr, var(--bit-Checkbox-checked-background, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "--bit-chb-ico-clr: var(--bit-chb-clr-txt, var(--bit-Checkbox-check-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "--bit-chb-ico-clr-bg: var(--bit-chb-clr, var(--bit-Checkbox-indeterminate-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "--bit-chb-box-clr-bg: var(--bit-chb-clr-hover, var(--bit-Checkbox-checked-hover-background, #{$clr-pri-hover}));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-chb-clr-focus, var(--bit-Checkbox-focus-color)))");
        StringAssert.Contains(stylesheet, "--bit-chb-box-clr-brd: var(--bit-chb-clr-dis, var(--bit-Checkbox-disabled-color, #{$clr-pri-dis}));");
        StringAssert.Contains(stylesheet, "color: var(--bit-chb-clr-dis-text, var(--bit-Checkbox-disabled-text-color, #{$clr-pri-dis-text}));");

        // A size class publishes the theme's size alone, never a public variable ahead of it.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-chb-sm {"), "--bit-chb-box-size: #{$siz-sel-sm};");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Checkbox-[a-z-]+, var\(--bit-chb-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitCheckboxShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-chb {");

        // A checkbox can sit in the label template or the custom face of another one, which must not inherit the outer
        // checkbox's Color or Size: each root starts the values those classes publish out unset, and the classes -
        // declared further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-chb-clr", "--bit-chb-clr-hover", "--bit-chb-clr-txt", "--bit-chb-clr-txt-sec", "--bit-chb-clr-txt-hover",
                                         "--bit-chb-clr-dis", "--bit-chb-clr-dis-text", "--bit-chb-clr-focus",
                                         "--bit-chb-box-size", "--bit-chb-lbl-fontsize", "--bit-chb-des-fontsize", "--bit-chb-card-padding" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    [TestMethod]
    public void BitCheckboxCardShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes the padding of the card, an explicit Color the checked border, the hover border and
        // the tint of the checked card - each read ahead of the public variable restyling the default.
        StringAssert.Contains(stylesheet, "padding: var(--bit-chb-card-padding, var(--bit-Checkbox-card-padding, #{spacing(1.5)}));");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-chb-md {"), "--bit-chb-card-padding: #{spacing(1.5)};");
        StringAssert.Contains(stylesheet, "border-color: var(--bit-chb-crd-sbrd, var(--bit-chb-clr, var(--bit-Checkbox-card-checked-border-color, var(--bit-Checkbox-checked-background, #{$clr-pri}))));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-chb-crd-sbg, var(--bit-chb-crd-tint, var(--bit-chb-crd-bg-checked)));");
        StringAssert.Contains(stylesheet, "--bit-chb-crd-sbrd: var(--bit-chb-clr-hover, var(--bit-Checkbox-card-hover-border-color, ");

        // The accent of the checked card is the checked fill of the box, resolved on the root no variant qualifies.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-chb {"), "--bit-chb-acc: var(--bit-chb-clr, var(--bit-Checkbox-checked-background, #{$clr-pri}));");
    }

    [TestMethod,
        DataRow("fill", ".bit-chb-fil", "card-background"),
        DataRow("fill", ".bit-chb-fil", "card-hover-background"),
        DataRow("fill", ".bit-chb-fil", "card-checked-background"),
        DataRow("fill", ".bit-chb-fil", "card-border-color"),
        DataRow("outline", ".bit-chb-otl", "card-background"),
        DataRow("outline", ".bit-chb-otl", "card-hover-background"),
        DataRow("outline", ".bit-chb-otl", "card-checked-background"),
        DataRow("outline", ".bit-chb-otl", "card-border-color")]
    public void BitCheckboxShouldReadTheCardVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector, string name)
    {
        // The variant's own rule, not the card rule the Outline and Fill variants share (".bit-chb-otl,\n.bit-chb-fil {").
        var stylesheet = ReadStylesheet();
        var own = SourceFiles.GetScssRules(stylesheet).Single(r => r.Header == selector && r.Ancestors.Count == 0);
        var block = SourceFiles.GetScssBlock(stylesheet[own.Index..], $"{selector} {{");

        // Each variant reads a tier of its own ahead of the shared card variables, so a theme can set outline and filled
        // cards apart on :root - and the card rules both variants share read only what each variant resolved.
        StringAssert.Contains(block, $"var(--bit-Checkbox-{variant}-{name}, var(--bit-Checkbox-{name}, ",
                              $"{selector} does not read --bit-Checkbox-{variant}-{name} ahead of --bit-Checkbox-{name}.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-Checkbox-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");
    }

    [TestMethod]
    public void BitCheckboxCardHoverShouldMoveAVariableRatherThanPaint()
    {
        var stylesheet = ReadStylesheet();
        var hover = SourceFiles.GetScssBlock(stylesheet, ":is(.bit-chb-otl, .bit-chb-fil):hover:not(.bit-chb-rdl):not(.bit-chb-ldg):not(.bit-inv) {");

        // A hover that painted the border itself would outrank a class the app hands the root, so it moves the variables
        // the card paints from - which the root starts out unset, so a card nested in another never inherits its hover.
        StringAssert.Contains(hover, "--bit-chb-crd-sbrd:");
        StringAssert.Contains(hover, "--bit-chb-crd-sbg: var(--bit-chb-crd-bg-hover);");
        Assert.IsFalse(Regex.IsMatch(hover, @"(^|[\s;{])(border-color|background-color)\s*:"), "The hover of a card paints instead of moving a variable.");

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-chb {");

        StringAssert.Contains(root, "--bit-chb-crd-sbg: initial;");
        StringAssert.Contains(root, "--bit-chb-crd-sbrd: initial;");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "Checkbox", "BitCheckbox.scss");
}
