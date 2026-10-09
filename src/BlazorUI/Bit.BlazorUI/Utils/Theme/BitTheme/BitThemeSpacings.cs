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

    /// <summary>
    /// The inset of a <c>BitCard</c> and of each of its parts, per size class (Fluent 2 8/12/16px, Material 16dp).
    /// Maps to <c>--bit-spa-card-{sm,md,lg}</c>.
    /// </summary>
    public BitThemeSizeScale Card { get; set; } = new();

    /// <summary>
    /// The unitless multiples of the spacing unit the density-aware insets are computed from (<c>--bit-spa-*-steps</c>).
    /// </summary>
    /// <remarks>
    /// <see cref="Dialog"/> and <see cref="Card"/> are computed where a component uses them, as
    /// <see cref="ScalingFactor"/> times <see cref="BitThemeLayout.DensityScale"/> times these steps, so they
    /// follow a density or a spacing unit re-valued anywhere in the tree; a value set for an inset itself wins.
    /// </remarks>
    public BitThemeSpacingSteps Steps { get; set; } = new();
}

/// <summary>
/// How many steps of the spacing unit each density-aware inset is (<c>--bit-spa-*-steps</c>): a unitless number,
/// such as <c>3</c> for a 24px dialog inset at the default 8px unit and density of 1.
/// </summary>
public class BitThemeSpacingSteps
{
    /// <summary>The steps of the base inset of a dialog surface (<c>--bit-spa-dialog-steps</c>).</summary>
    public string? Dialog { get; set; }

    /// <summary>The steps of the inset of a <c>BitCard</c> per size class (<c>--bit-spa-card-{sm,md,lg}-steps</c>).</summary>
    public BitThemeSizeScale Card { get; set; } = new();
}
