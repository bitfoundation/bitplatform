namespace Bit.BlazorUI;

public class BitThemeLayout
{
    /// <summary>
    /// Unitless multiplier applied to every component spacing measurement (e.g. <c>0.9</c> for a compact UI);
    /// the effective spacing unit is <see cref="BitThemeSpacings.ScalingFactor"/> × this value.
    /// Maps to <c>--bit-layout-density-scale</c>.
    /// </summary>
    public string? DensityScale { get; set; }

    /// <summary>Responsive breakpoint tokens driving the predefined <see cref="BitScreenQuery"/> values. Map to the <c>--bit-bp-*</c> custom properties.</summary>
    public BitThemeBreakpoints Breakpoints { get; set; } = new();
}
