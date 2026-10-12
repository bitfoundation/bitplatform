using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ChoiceGroup;

/// <summary>
/// Pins the order the choice group's stylesheet ranks its values in, which a bUnit render cannot see: an explicit Color,
/// Size or Gap outranks the public --bit-ChoiceGroup-* variable restyling what it sets, which in turn outranks the
/// default an unset parameter stands for.
/// </summary>
[TestClass]
public class BitChoiceGroupStylesheetTests
{
    [TestMethod]
    public void BitChoiceGroupShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these (and Gap, written on the style of the root, the gap over it), so they are
        // read before the variable, which only restyles the default an unset one stands for.
        StringAssert.Contains(stylesheet, "--bit-chg-cnt-gap: var(--bit-chg-gap, var(--bit-ChoiceGroup-gap, #{spacing(1)}));");
        StringAssert.Contains(stylesheet, "--bit-chg-cnt-gap: var(--bit-chg-gap, var(--bit-ChoiceGroup-gap, #{spacing(2)}));");
        StringAssert.Contains(stylesheet, "--bit-chg-circle-side: var(--bit-chg-circle-size, var(--bit-ChoiceGroup-circle-size, #{$siz-sel-md}));");
        StringAssert.Contains(stylesheet, "--bit-chg-ico-side: var(--bit-chg-ico-size, var(--bit-ChoiceGroup-icon-size, #{spacing(4)}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-chg-fontsize, var(--bit-ChoiceGroup-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "var(--bit-chg-dot-size, var(--bit-ChoiceGroup-dot-size, #{spacing(1.25)}))");
        StringAssert.Contains(stylesheet, "var(--bit-chg-card-padding, var(--bit-ChoiceGroup-item-padding, #{spacing(1.5)}))");

        // So does an explicit Color, for every color it paints - including the disabled ones, which the disabled
        // state still picks, and the tint of a checked card, which is left unset while the Color is.
        StringAssert.Contains(stylesheet, "var(--bit-chg-clr, var(--bit-ChoiceGroup-color, #{$clr-pri}))");
        StringAssert.Contains(stylesheet, "var(--bit-chg-clr-hover, var(--bit-ChoiceGroup-hover-color, #{$clr-pri-hover}))");
        StringAssert.Contains(stylesheet, "var(--bit-chg-clr-dis, var(--bit-ChoiceGroup-disabled-color, #{$clr-pri-dis}))");
        StringAssert.Contains(stylesheet, "var(--bit-chg-clr-dis-text, var(--bit-ChoiceGroup-disabled-text-color, #{$clr-pri-dis-text}))");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-chg-clr-focus, var(--bit-ChoiceGroup-focus-color)))");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-chg-clr-bg, var(--bit-ChoiceGroup-text-item-background, var(--bit-ChoiceGroup-item-background, #{$clr-bg-sec})));");
        // The checked card at rest reads the variable its hover moves first, then the Color as everywhere else.
        StringAssert.Contains(stylesheet, "border-color: var(--bit-chg-itl-brd, var(--bit-chg-clr, var(--bit-ChoiceGroup-item-checked-border-color, var(--bit-ChoiceGroup-color, #{$clr-pri}))));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-chg-itl-bg, var(--bit-chg-card-tint, var(--bit-chg-card-bg-checked)));");
        StringAssert.Contains(stylesheet, "--bit-chg-card-bg-checked: var(--bit-ChoiceGroup-outline-item-checked-background, var(--bit-ChoiceGroup-item-checked-background, #{translucent(var(--bit-chg-acc), 8%)}));");

        // A role class publishes the role's tokens alone, never a public variable ahead of them.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-chg-err {"), "--bit-chg-clr: #{$clr-err};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-chg-sm {"), "--bit-chg-circle-size: #{$siz-sel-sm};");

        // The values the parameters publish are never read behind a public variable. The card surface and border
        // (--bit-chg-card-*) are the Variant's recipe rather than a Color's or a Size's, so they stay the variables'.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-ChoiceGroup-[a-z-]+, var\(--bit-chg-(clr|gap|ico-size|fontsize|circle-size|dot-size|desc-fontsize|inline-ico-size|card-padding|card-tint)\b"),
                       "A public variable is read before the parameter it restyles the default of.");
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"\n    --bit-chg-[a-z-]+: var\(--bit-ChoiceGroup-[a-z-]+, #\{\$[a-z-]+\}\);"),
                       "A Color or Size class reads a public variable ahead of the value it publishes.");
    }

    [TestMethod,
        DataRow("fill", ".bit-chg-fil .bit-chg-itl", "item-background"),
        DataRow("fill", ".bit-chg-fil .bit-chg-itl", "item-hover-background"),
        DataRow("fill", ".bit-chg-fil .bit-chg-itl", "item-checked-background"),
        DataRow("fill", ".bit-chg-fil .bit-chg-itl", "item-border-color"),
        DataRow("outline", ".bit-chg-otl .bit-chg-itl", "item-background"),
        DataRow("outline", ".bit-chg-otl .bit-chg-itl", "item-hover-background"),
        DataRow("outline", ".bit-chg-otl .bit-chg-itl", "item-checked-background"),
        DataRow("outline", ".bit-chg-otl .bit-chg-itl", "item-border-color"),
        DataRow("text", ":where(.bit-chg-txt) .bit-chg-ihi", "item-background")]
    public void BitChoiceGroupShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector, string name)
    {
        // The variant's own rule, not the card rule the Outline and Fill variants share (".bit-chg-otl,\n.bit-chg-fil {").
        var stylesheet = ReadStylesheet();
        var own = SourceFiles.GetScssRules(stylesheet).Single(r => r.Header == selector && r.Ancestors.Count == 0);
        var block = SourceFiles.GetScssBlock(stylesheet[own.Index..], $"{selector} {{");

        // Each variant reads a tier of its own ahead of the shared item variables, so a theme can set outline and
        // filled cards apart on :root - and the card rules both variants share read only what each variant resolved.
        StringAssert.Contains(block, $"var(--bit-ChoiceGroup-{variant}-{name}, var(--bit-ChoiceGroup-{name}, ",
                              $"{selector} does not read --bit-ChoiceGroup-{variant}-{name} ahead of --bit-ChoiceGroup-{name}.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-ChoiceGroup-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");
    }

    [TestMethod]
    public void BitChoiceGroupShouldResolveItsCardSurfaceOnTheCard()
    {
        // An item's Style and Styles.ItemContainer land on the item container around the card, so a public item
        // variable set there reaches the card only while the card resolves it itself - a value worked out on the
        // root would already be fixed by the time it inherits down to the card.
        var stylesheet = ReadStylesheet();

        foreach (var rule in SourceFiles.GetScssRules(stylesheet).Where(r => r.Header is ".bit-chg-otl" or ".bit-chg-fil" && r.Ancestors.Count == 0))
        {
            var block = SourceFiles.GetScssDeclarations(stylesheet[rule.Index..], $"{rule.Header} {{");

            StringAssert.DoesNotMatch(block, new Regex(@"--bit-chg-card-[a-z-]+\s*:"),
                                      $"{rule.Header} resolves the card surface on the root, out of reach of an item's Style.");
        }
    }

    [TestMethod]
    public void BitChoiceGroupShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-chg {");

        // A choice group can sit in an item template of another one, which must not inherit the outer group's Color or
        // Size: each root starts the values those classes publish out unset, and the classes - declared further down
        // at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-chg-clr", "--bit-chg-clr-bg", "--bit-chg-clr-hover", "--bit-chg-clr-dis", "--bit-chg-clr-dis-text",
                                         "--bit-chg-clr-focus", "--bit-chg-gap", "--bit-chg-ico-size", "--bit-chg-fontsize", "--bit-chg-circle-size",
                                         "--bit-chg-dot-size", "--bit-chg-desc-fontsize", "--bit-chg-inline-ico-size", "--bit-chg-card-padding" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Inputs", "ChoiceGroup", "BitChoiceGroup.scss");
}
