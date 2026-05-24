namespace Bit.BlazorUI;

public class BitThemeLayout
{
    /// <summary>CSS <c>direction</c> for the document or region, e.g. <c>ltr</c> or <c>rtl</c>. Maps to <c>--bit-layout-dir</c>.</summary>
    public string? Direction { get; set; }

    /// <summary>Multiplier for compact UI (e.g. <c>0.9</c>); combine with <see cref="BitTheme.Spacing"/> and <see cref="BitThemeSpacings.ScalingFactor"/>. Maps to <c>--bit-layout-density-scale</c>.</summary>
    public string? DensityScale { get; set; }

    public BitThemeBreakpoints Breakpoints { get; set; } = new();
}
