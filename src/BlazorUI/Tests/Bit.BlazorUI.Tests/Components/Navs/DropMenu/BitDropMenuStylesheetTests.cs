using System.IO;
using System.Runtime.CompilerServices;
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
        // The track and the arc of the spinner would otherwise be forced to the same color.
        DataRow(".bit-drm-spn {", "border-top-color: CanvasText;")]
    public void BitDropMenuShouldReestablishItsStatesInForcedColors(string selector, string declaration)
    {
        var forcedColors = GetBlock(ReadStylesheet(), "\n@media (forced-colors: active) {\n    .bit-drm {");

        var start = forcedColors.IndexOf(selector, System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector} was not found in the forced-colors block.");

        var rule = forcedColors[start..forcedColors.IndexOf('}', start)];

        StringAssert.Contains(rule, declaration);
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

    private static string GetBlock(string stylesheet, string selector)
    {
        var start = stylesheet.IndexOf(selector, System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector.Trim()} was not found in the stylesheet.");

        var end = stylesheet.IndexOf("\n}", start + 1, System.StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Navs", "DropMenu", "BitDropMenu.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
