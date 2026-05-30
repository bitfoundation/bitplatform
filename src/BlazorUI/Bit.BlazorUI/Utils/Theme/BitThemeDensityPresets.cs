namespace Bit.BlazorUI;

/// <summary>
/// Pre-built density tweaks; merge with a baseline <see cref="BitTheme"/> from your app.
/// </summary>
public static class BitThemeDensityPresets
{
    /// <summary>Returns a theme overlay that tightens vertical rhythm via <see cref="BitThemeLayout.DensityScale"/> and spacing scale.</summary>
    /// <example>
    /// Merge the overlay's density-affecting properties into your baseline <see cref="BitTheme"/>:
    /// <code>
    /// var theme = new BitTheme(); // your app baseline
    /// var overlay = BitThemeDensityPresets.CreateCompactOverlay();
    /// theme.Layout.DensityScale = overlay.Layout.DensityScale;
    /// theme.Spacing.ScalingFactor = overlay.Spacing.ScalingFactor;
    /// await ThemeManager.ApplyBitThemeAsync(theme);
    /// </code>
    /// </example>
    public static BitTheme CreateCompactOverlay()
    {
        return new BitTheme
        {
            Layout = { DensityScale = "0.9" },
            Spacing = { ScalingFactor = "0.95" },
        };
    }
}
