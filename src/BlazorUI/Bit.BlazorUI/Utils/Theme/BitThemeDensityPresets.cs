namespace Bit.BlazorUI;

/// <summary>
/// Pre-built density tweaks; merge with a baseline <see cref="BitTheme"/> from your app.
/// </summary>
public static class BitThemeDensityPresets
{
    /// <summary>Returns a theme overlay that tightens vertical rhythm via <see cref="BitThemeLayout.DensityScale"/> and spacing scale.</summary>
    public static BitTheme CreateCompactOverlay()
    {
        return new BitTheme
        {
            Layout = { DensityScale = "0.9" },
            Spacing = { ScalingFactor = "0.95" },
        };
    }
}
