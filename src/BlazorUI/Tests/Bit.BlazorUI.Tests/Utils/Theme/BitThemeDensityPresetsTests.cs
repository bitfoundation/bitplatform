using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeDensityPresetsTests
{
    [TestMethod]
    public void CompactOverlaySetsUnitlessDensityScaleOnly()
    {
        var overlay = BitThemeDensityPresets.CreateCompactOverlay();

        Assert.IsTrue(double.TryParse(overlay.Layout.DensityScale, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale),
            $"DensityScale '{overlay.Layout.DensityScale}' must be a plain unitless number; spacing() multiplies it with the --bit-spa-scaling-factor length.");
        Assert.IsTrue(scale > 0 && scale < 1, $"A compact overlay must shrink spacing (0 < scale < 1), got {scale}.");

        // The base spacing unit is a CSS length; a unitless value here would make every spacing()
        // calc resolve to an invalid length and collapse padding/gaps library-wide.
        Assert.IsNull(overlay.Spacing.ScalingFactor, "The compact overlay must not override the spacing base unit.");
    }

    [TestMethod]
    public void CompactOverlayEmitsDensityScaleCssVariable()
    {
        var mapped = BitThemeUtilities.ToCssVariables(BitThemeDensityPresets.CreateCompactOverlay());

        Assert.IsTrue(mapped.TryGetValue("--bit-layout-density-scale", out var value) && value == "0.9");
        Assert.IsFalse(mapped.ContainsKey("--bit-spa-scaling-factor"));
    }
}
