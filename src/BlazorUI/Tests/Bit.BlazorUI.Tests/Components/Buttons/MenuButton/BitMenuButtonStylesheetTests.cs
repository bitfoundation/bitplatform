using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.MenuButton;

/// <summary>
/// Pins the order in which the menu button - and the callout it renders outside its root - reads its public
/// --bit-MenuButton-* variables against the values its Color, Size and Background parameters publish, which a bUnit
/// render cannot see: an explicit parameter wins over the variable that restyles what it sets, and the variable only
/// restyles the default an unset parameter stands for.
/// </summary>
[TestClass]
public partial class BitMenuButtonStylesheetTests
{
    [TestMethod]
    public void BitMenuButtonShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, on the root and on the callout, so they are read before the variable,
        // which only restyles the medium menu button an unset one stands for.
        StringAssert.Contains(stylesheet, "var(--bit-mnb-font-size, var(--bit-MenuButton-font-size, #{$tg-fs-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-mnb-min-height, var(--bit-MenuButton-min-height, #{$siz-ctrl-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-mnb-min-height, var(--bit-MenuButton-chevron-width, var(--bit-MenuButton-min-height, #{$siz-ctrl-md})))");
        StringAssert.Contains(stylesheet, "var(--bit-mnb-padding, var(--bit-MenuButton-padding, #{$siz-ctrl-pad-y-md} #{$siz-ctrl-pad-x-sm}))");
        StringAssert.Contains(stylesheet, "var(--bit-mnb-pad-y, var(--bit-MenuButton-padding, #{$siz-ctrl-pad-y-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-mnb-icon-size, var(--bit-MenuButton-icon-size, #{$siz-icon-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-mnb-item-height, var(--bit-MenuButton-item-min-height, #{$siz-item-md}))");
        StringAssert.Contains(stylesheet, "var(--bit-mnb-item-padding, var(--bit-MenuButton-item-padding, #{$siz-ctrl-pad-y-md} #{$siz-ctrl-pad-x-sm}))");

        // So does an explicit Color, for every color the role paints - the toggled header, the divider and the
        // focus ring and check mark of the items included.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-mnb-clr, var(--bit-MenuButton-fill-background, var(--bit-MenuButton-background, #{$clr-pri})));");
        StringAssert.Contains(stylesheet, "color: var(--bit-mnb-clr-txt, var(--bit-MenuButton-fill-color, var(--bit-MenuButton-color, #{$clr-pri-text})));");
        // The hover and press states of the two halves set the variables the rules at rest paint from, never the
        // colors themselves, so the state's value is read ahead of the rest value it stands in for. The colors of the states
        // are resolved per variant on the root the halves inherit them from, the role ahead of the variant's own variable.
        StringAssert.Contains(stylesheet, "--bit-mnb-hlf-sbg: var(--bit-mnb-hov-bg);");
        StringAssert.Contains(stylesheet, "--bit-mnb-hov-bg: var(--bit-mnb-clr-hover, var(--bit-MenuButton-fill-hover-background, var(--bit-MenuButton-hover-background, #{$clr-pri-hover})));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-mnb-hlf-sbg, var(--bit-mnb-clr-dark, var(--bit-MenuButton-toggled-background, #{$clr-pri-dark})));");
        StringAssert.Contains(stylesheet, "--bit-mnb-sbr: var(--bit-mnb-hov-bg);");
        StringAssert.Contains(stylesheet, "--bit-mnb-dis-clr: var(--bit-mnb-clr-dis-text, var(--bit-MenuButton-disabled-color, #{$clr-pri-dis-text}));");
        StringAssert.Contains(stylesheet, "--bit-mnb-clr-spb: var(--bit-mnb-clr-txt, var(--bit-MenuButton-fill-divider-color, var(--bit-MenuButton-divider-color, var(--bit-MenuButton-fill-color, var(--bit-MenuButton-color, #{$clr-pri-text})))));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-mnb-clr-focus, var(--bit-MenuButton-focus-color)))");
        StringAssert.Contains(stylesheet, "var(--bit-mnb-clr-focus, var(--bit-MenuButton-item-focus-color, #{$clr-pri-focus}))");
        StringAssert.Contains(stylesheet, "color: var(--bit-mnb-clr, var(--bit-MenuButton-item-checked-color, #{$clr-pri}));");

        // And an explicit Background, over the callout's own background.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-mnb-cal-bg, var(--bit-MenuButton-callout-background, #{$clr-bg-pri}));");

        // What a Color does not paint stays the variables' alone: the transparent background of Outline and Text.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-MenuButton-outline-background, var(--bit-MenuButton-background, transparent));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-MenuButton-text-background, var(--bit-MenuButton-background, transparent));");

        Assert.IsFalse(PublicBeforePrivate().IsMatch(SourceFiles.StripScssComments(stylesheet)), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod,
        DataRow("fill", ".bit-mnb-fil"),
        DataRow("outline", ".bit-mnb-otl"),
        DataRow("text", ".bit-mnb-txt")]
    public void BitMenuButtonShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector)
    {
        var stylesheet = ReadStylesheet();
        // The hovered and pressed colors are resolved by a rule of the variant's own that also reaches each half, so a
        // public variable set on Styles.OperatorButton or Styles.ChevronDownButton reaches the half it was set on.
        var states = SourceFiles.GetScssBlock(stylesheet, $"\n{selector},\n{selector} > .bit-mnb-opb,\n{selector} > .bit-mnb-chb {{");
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{") + states;

        foreach (var name in new[] { "hov-fg", "hov-bg", "act-fg", "act-bg" })
        {
            StringAssert.Contains(states, $"--bit-mnb-{name}: ", $"{selector} does not resolve --bit-mnb-{name} on the halves.");
        }

        // Each variant reads a tier of its own ahead of every shared color variable of the button, so a theme can set
        // one look apart from the other two on :root - an outline menu button's neutral hairline without a ring around
        // every filled one.
        foreach (var name in new[] { "color", "background", "border-color",
                                     "hover-color", "hover-background", "active-color", "active-background",
                                     "disabled-background", "disabled-border-color", "divider-color" })
        {
            StringAssert.Contains(block, $"var(--bit-MenuButton-{variant}-{name}, var(--bit-MenuButton-{name}, ",
                                  $"{selector} does not read --bit-MenuButton-{variant}-{name} ahead of --bit-MenuButton-{name}.");
        }

        // A variant reads a shared color variable only right behind its own twin, and never the tier of another variant.
        Assert.IsFalse(Regex.IsMatch(block, $@"(?<!var\(--bit-MenuButton-{variant}-[a-z-]+, )var\(--bit-MenuButton-(?:hover-|active-|disabled-|divider-)?(?:color|background|border-color)\b"),
                       $"{selector} reads a shared color variable without its own tier ahead of it.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-MenuButton-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");

        // The halves are painted in the hovered and pressed colors their variant resolves on them, so no rule shared by
        // the three variants reads those variables itself.
        var halves = SourceFiles.StripScssComments(SourceFiles.GetScssBlock(stylesheet, "\n.bit-mnb-opb,\n.bit-mnb-chb {"));
        Assert.IsFalse(Regex.IsMatch(halves, @"var\(--bit-MenuButton-(?:hover|active)-"), "The halves read the shared hover or active variables past their variant's tier.");
    }

    [TestMethod]
    public void BitMenuButtonShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();

        var unset = SourceFiles.GetScssBlock(stylesheet, "@mixin bit-mnb-unset-published {");

        // A menu button can sit in the header or the menu of another one, which must not inherit the outer one's
        // Color or Size: every element those classes land on starts what they publish out unset, and the classes -
        // declared further down at the same weight - still win on the element that carries them.
        foreach (var property in new[] { "--bit-mnb-clr", "--bit-mnb-clr-txt", "--bit-mnb-clr-hover", "--bit-mnb-clr-active", "--bit-mnb-clr-dark",
                                         "--bit-mnb-clr-dark-hover", "--bit-mnb-clr-dark-active", "--bit-mnb-clr-focus", "--bit-mnb-clr-dis",
                                         "--bit-mnb-clr-dis-text", "--bit-mnb-font-size", "--bit-mnb-min-height", "--bit-mnb-icon-size",
                                         "--bit-mnb-item-height", "--bit-mnb-pad-y", "--bit-mnb-padding", "--bit-mnb-item-padding" })
        {
            StringAssert.Contains(unset, $"{property}: initial;");
        }

        // The callout is a sibling of the root rather than a descendant of it and carries the classes again, so it
        // resets them too - and the Background class it alone carries.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-mnb {"), "@include bit-mnb-unset-published;");

        // The colors the hover and press states move start out unset on the element they are painted on - the root's
        // border, each half of the button, each item - so a nested menu button never inherits the outer one's state.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-mnb {"), "--bit-mnb-sbr: initial;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-mnb-opb,\n.bit-mnb-chb {"), "--bit-mnb-hlf-sfg: initial;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-mnb-opb,\n.bit-mnb-chb {"), "--bit-mnb-hlf-sbg: initial;");
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-mnb-itm {"), "--bit-mnb-itm-sbg: initial;");

        var callout = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-mnb-cal {");
        StringAssert.Contains(callout, "@include bit-mnb-unset-published;");
        StringAssert.Contains(callout, "--bit-mnb-cal-bg: initial;");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Buttons", "MenuButton", "BitMenuButton.scss");

    [GeneratedRegex(@"var\(--bit-MenuButton-[a-z-]+, var\(--bit-mnb-")]
    private static partial Regex PublicBeforePrivate();
}
