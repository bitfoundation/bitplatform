using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.DropMenu;

/// <summary>
/// Pins what a bUnit render cannot see: the forced-colors (Windows High Contrast) rules of the drop menu, where the
/// palette is reduced to the system pair and every state carried by a background alone would disappear.
/// </summary>
[TestClass]
public class BitDropMenuStylesheetTests
{
    [TestMethod,
        // The default look and the Text variant leave the root border transparent, and it is the button's only edge.
        DataRow(".bit-drm {", "border-color: ButtonBorder;"),
        // The open state is otherwise a pressed background, which forced colors strip.
        DataRow(".bit-drm.bit-drm-omn {", "border-color: Highlight;"),
        // A loading drop menu is only aria-disabled, so nothing else would tell it from an enabled one.
        DataRow(".bit-drm.bit-drm-ldg {", "color: GrayText;"),
        DataRow(".bit-drm.bit-drm-ldg {", "border-color: GrayText;"),
        // The disabled or loading drop menu goes GrayText, and its spinner with it rather than turning in CanvasText (the
        // track and the arc of an enabled one are kept apart by the spinner-ring mixin, see BitSpinnerRingStylesheetTests).
        DataRow(".bit-drm.bit-dis .bit-drm-spn,", "@include spinner-ring-forced-disabled;"),
        DataRow(".bit-drm.bit-drm-ldg .bit-drm-spn {", "@include spinner-ring-forced-disabled;")]
    public void BitDropMenuShouldReestablishItsStatesInForcedColors(string selector, string declaration)
    {
        var forcedColors = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {\n    .bit-drm {");

        StringAssert.Contains(SourceFiles.GetScssDeclarations(forcedColors, selector), declaration);
    }

    [TestMethod]
    public void BitDropMenuShouldDeclareItsForcedColorsRulesAfterTheStateRules()
    {
        // The forced-colors rules share the specificity of the variant and state rules they override, so they only
        // win by coming after them.
        var stylesheet = ReadStylesheet();

        var forcedColors = stylesheet.IndexOf("\n@media (forced-colors: active) {\n    .bit-drm {", System.StringComparison.Ordinal);

        Assert.IsTrue(forcedColors > stylesheet.IndexOf("\n.bit-drm-trn {", System.StringComparison.Ordinal));
        Assert.IsTrue(forcedColors > stylesheet.IndexOf("\n.bit-drm-lg {", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitDropMenuShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-drm {");

        // Each state ranks what an explicit Color paints there first, then the public variable, then the default look
        // (the -d slots) the variable restyles.
        StringAssert.Contains(root, "--bit-drm-cur-txt: var(--bit-drm-rst-txt, var(--bit-DropMenu-color, var(--bit-drm-rst-txt-d)));");
        StringAssert.Contains(root, "--bit-drm-cur-bg: var(--bit-drm-rst-bg, var(--bit-DropMenu-background, var(--bit-drm-rst-bg-d)));");
        StringAssert.Contains(root, "--bit-drm-cur-bg: var(--bit-drm-hov-bg, var(--bit-DropMenu-hover-background, var(--bit-drm-hov-bg-d)));");
        StringAssert.Contains(root, "--bit-drm-cur-bg: var(--bit-drm-act-bg, var(--bit-DropMenu-active-background, var(--bit-drm-act-bg-d)));");
        StringAssert.Contains(root, "@include focus-ring(var(--bit-drm-clr-focus, var(--bit-DropMenu-focus-color, #{$clr-pri-focus})));");

        // A look that paints no Color in a slot leaves the bare slot unset, so the variable keeps it.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-drm-otl {"), "--bit-drm-rst-bg: initial;");

        // An explicit Size, Background and Border win over their variables as well.
        StringAssert.Contains(stylesheet, "min-height: var(--bit-drm-min-height, var(--bit-DropMenu-min-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-drm-font-size, var(--bit-DropMenu-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-drm-icon-size, var(--bit-DropMenu-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-drm-cal-bg, var(--bit-DropMenu-callout-background, #{$clr-bg-pri}));");
        StringAssert.Contains(stylesheet, "border-color: var(--bit-drm-cal-brd, var(--bit-DropMenu-callout-border-color, transparent));");

        // Only a -d default slot may sit behind a public variable.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-DropMenu-[a-z-]+, var\(--bit-drm-(?![a-z-]+-d\))"), "A public variable is read before the parameter it restyles the default of.");
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-DropMenu-[a-z-]+, #\{role"), "A role class reads a public variable ahead of the role.");
    }

    [TestMethod]
    public void BitDropMenuShouldPublishItsColorAndSizeOnlyWhereTheyAreSet()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-drm {");

        // A drop menu never inherits another one's Color or Size from an ancestor carrying those classes (one placed in
        // the callout of another): each root starts the values they publish out unset, and the classes - declared
        // further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-drm-clr", "--bit-drm-clr-txt", "--bit-drm-clr-flt", "--bit-drm-clr-hover", "--bit-drm-clr-active",
                                         "--bit-drm-clr-focus", "--bit-drm-clr-dis", "--bit-drm-clr-dis-text", "--bit-drm-font-size",
                                         "--bit-drm-min-height", "--bit-drm-icon-size", "--bit-drm-pad-y", "--bit-drm-padding" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }

        // The callout starts what Background and Border publish out unset too, ahead of the classes that set them.
        var callout = SourceFiles.GetScssBlock(stylesheet, "\n.bit-drm-cal {");
        StringAssert.Contains(callout, "--bit-drm-cal-bg: initial;");
        StringAssert.Contains(callout, "--bit-drm-cal-brd: initial;");

        var rootAt = stylesheet.IndexOf("\n.bit-drm {", System.StringComparison.Ordinal);
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n    .bit-drm-#{$role} {", System.StringComparison.Ordinal), "The color classes are declared ahead of the root that resets them.");
        Assert.IsTrue(rootAt < stylesheet.IndexOf("\n.bit-drm-md {", System.StringComparison.Ordinal), "The size classes are declared ahead of the root that resets them.");
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Navs", "DropMenu", "BitDropMenu.scss");
}
