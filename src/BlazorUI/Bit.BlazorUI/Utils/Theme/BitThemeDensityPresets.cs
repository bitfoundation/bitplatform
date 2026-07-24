namespace Bit.BlazorUI;

/// <summary>
/// Pre-built density tweaks; merge with a baseline <see cref="BitTheme"/> from your app.
/// </summary>
public static class BitThemeDensityPresets
{
    /// <summary>
    /// Returns a theme overlay that tightens component rhythm via <see cref="BitThemeLayout.DensityScale"/>,
    /// the unitless multiplier every spacing measurement is scaled by. <see cref="BitThemeSpacings.ScalingFactor"/>
    /// (the base spacing unit, a CSS length) is intentionally left untouched.
    /// </summary>
    /// <example>
    /// Merge the overlay's density-affecting properties into your baseline <see cref="BitTheme"/>:
    /// <code>
    /// var bitThemeManager = ...; // inject BitThemeManager
    /// var theme = new BitTheme(); // your app baseline
    /// var overlay = BitThemeDensityPresets.CreateCompactOverlay();
    /// theme.Layout.DensityScale = overlay.Layout.DensityScale;
    /// await bitThemeManager.ApplyBitThemeAsync(theme);
    /// </code>
    /// </example>
    public static BitTheme CreateCompactOverlay()
    {
        return new BitTheme
        {
            Layout = { DensityScale = "0.9" },
        };
    }
}
