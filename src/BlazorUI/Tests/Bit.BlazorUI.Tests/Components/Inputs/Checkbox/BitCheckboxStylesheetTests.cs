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

        // Only a card that does not read as checked takes the hover surface - one that is not checked, or one whose box
        // shows the mixed state - so a hovered checked card keeps the tint that says it is checked.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(hover, "&:is(:not(.bit-chb-ckd), .bit-chb-ind) {"), "--bit-chb-crd-sbg: var(--bit-chb-crd-bg-hover);");
        Assert.IsFalse(SourceFiles.GetScssDeclarations(stylesheet, ":is(.bit-chb-otl, .bit-chb-fil):hover:not(.bit-chb-rdl):not(.bit-chb-ldg):not(.bit-inv) {").Contains("--bit-chb-crd-sbg"),
                       "The hover moves the surface of a checked card to the neutral hover surface too.");

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-chb {");

        StringAssert.Contains(root, "--bit-chb-crd-sbg: initial;");
        StringAssert.Contains(root, "--bit-chb-crd-sbrd: initial;");
    }

    [TestMethod]
    public void BitCheckboxCardShouldPaintTheStateItsBoxShows()
    {
        var stylesheet = ReadStylesheet();

        // A box carrying the mixed state as well as the value renders the mixed state, so the card does not read as
        // checked - in the forced colors mode either.
        StringAssert.Contains(stylesheet, ".bit-chb-otl.bit-chb-ckd:not(.bit-chb-ind),\n.bit-chb-fil.bit-chb-ckd:not(.bit-chb-ind) {");
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"\.bit-chb-(otl|fil)\.bit-chb-ckd\s*[,{]"), "A checked card rule leaves the mixed state in.");

        // A checked card that is invalid or disabled is tinted with the fill its box takes in that state, which each
        // variant mixes at its own alpha.
        var state = SourceFiles.GetScssBlock(stylesheet, ".bit-chb-otl.bit-chb-ckd:not(.bit-chb-ind):is(.bit-inv, .bit-dis),");

        StringAssert.Contains(state, "background-color: var(--bit-chb-crd-box-tint);");
        StringAssert.Contains(VariantDeclarations(stylesheet, ".bit-chb-otl"), "--bit-chb-crd-box-tint: #{translucent(var(--bit-chb-box-clr-bg), 8%)};");
        StringAssert.Contains(VariantDeclarations(stylesheet, ".bit-chb-fil"), "--bit-chb-crd-box-tint: #{translucent(var(--bit-chb-box-clr-bg), 14%)};");
    }

    [TestMethod]
    public void BitCheckboxCardHitAreaShouldLieUnderTheContent()
    {
        var stylesheet = ReadStylesheet();

        // The hit area lies under the content, above the surface of the card the root isolates a stacking context for,
        // so it never covers the label - which keeps its Title and its press to itself.
        var hit = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-chb-hit {");

        StringAssert.Contains(hit, "z-index: -1;");
        StringAssert.Contains(hit, "position: absolute;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, ".bit-chb-otl,\n.bit-chb-fil {"), "isolation: isolate;");
        StringAssert.Contains(stylesheet, ".bit-chb-inp:focus-visible ~ .bit-chb-hit {");
        Assert.IsFalse(stylesheet.Contains(".bit-chb-lbl::after"), "The label is still stretched over the card, Title and all.");

        // The lines under the box let the pointer through to it, and whatever in them answers the pointer takes it
        // back - without being positioned or stacked, which would move what an app lays out inside it.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, ":where(.bit-chb-otl, .bit-chb-fil) :where(.bit-chb-des, .bit-chb-erm) {"), "pointer-events: none;");

        var handBack = SourceFiles.GetScssBlock(stylesheet, ":where(.bit-chb-otl:not(.bit-dis), .bit-chb-fil:not(.bit-dis)) :where(.bit-chb-des, .bit-chb-erm) :where(");

        foreach (var interactive in new[] { "a", "button", "input", "label", "[contenteditable]", "[role]", "[tabindex]" })
        {
            StringAssert.Contains(handBack, interactive);
        }

        StringAssert.Contains(handBack, "pointer-events: auto;");
        Assert.IsFalse(Regex.IsMatch(handBack, @"(z-index|position)\s*:"), "The card positions what an app puts in its lines.");

        // A press anywhere on the card dips the box, as a press on the label does.
        StringAssert.Contains(stylesheet, ".bit-chb-hit:active ~ .bit-chb-lbl .bit-chb-box");
    }

    // The own rule of a variant, not the card rule the Outline and Fill variants share.
    private static string VariantDeclarations(string stylesheet, string selector)
    {
        var own = SourceFiles.GetScssRules(stylesheet).Single(r => r.Header == selector && r.Ancestors.Count == 0);

        return SourceFiles.GetScssDeclarations(stylesheet[own.Index..], $"{selector} {{");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "Checkbox", "BitCheckbox.scss");
}
