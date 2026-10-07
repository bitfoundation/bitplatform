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
        // The disabled drop menu goes GrayText, and its spinner with it rather than turning in CanvasText (the track
        // and the arc of an enabled one are kept apart by the spinner-ring mixin, see BitSpinnerRingStylesheetTests).
        DataRow(".bit-drm.bit-dis .bit-drm-spn {", "@include spinner-ring-forced-disabled;")]
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

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Navs", "DropMenu", "BitDropMenu.scss");
}
