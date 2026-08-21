namespace Bit.BlazorUI;

public class BitThemeSpacings
{
    /// <summary>
    /// Base spacing unit as a CSS length (default <c>0.5rem</c>); every component spacing measurement is a
    /// multiple of it, further scaled by the unitless <see cref="BitThemeLayout.DensityScale"/>.
    /// Maps to <c>--bit-spa-scaling-factor</c>.
    /// </summary>
    public string? ScalingFactor { get; set; }

    /// <summary>
    /// The base inset of a dialog surface - the padding a design system decides for dialog headers,
    /// bodies and footers (Fluent/Material 24px, Cupertino alerts 20px). Maps to <c>--bit-spa-dialog</c>.
    /// </summary>
    public string? Dialog { get; set; }
}
