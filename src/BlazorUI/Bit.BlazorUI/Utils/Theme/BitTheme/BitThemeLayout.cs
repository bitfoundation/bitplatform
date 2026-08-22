namespace Bit.BlazorUI;

public class BitThemeLayout
{
    /// <summary>
    /// Unitless multiplier applied to every component spacing measurement (e.g. <c>0.9</c> for a compact UI);
    /// the effective spacing unit is <see cref="BitThemeSpacings.ScalingFactor"/> × this value.
    /// Maps to <c>--bit-layout-density-scale</c>.
    /// </summary>
    public string? DensityScale { get; set; }

    /// <summary>
    /// The <c>flex-direction</c> of a dialog/message-box footer's action buttons (<c>row</c> under
    /// Fluent and Material, <c>column</c> for Cupertino's stacked alert buttons).
    /// Maps to <c>--bit-layout-dialog-actions-direction</c>.
    /// </summary>
    public string? DialogActionsDirection { get; set; }

    /// <summary>
    /// The <c>justify-content</c> of a dialog/message-box footer's action buttons (<c>flex-end</c>
    /// under Fluent and Material, <c>center</c> for Cupertino).
    /// Maps to <c>--bit-layout-dialog-actions-justify</c>.
    /// </summary>
    public string? DialogActionsJustify { get; set; }

    /// <summary>
    /// The <c>align-items</c> of a dialog/message-box footer's action buttons (<c>center</c> under
    /// Fluent and Material, <c>stretch</c> for Cupertino - which is what runs its stacked alert
    /// actions the full width of the alert).
    /// Maps to <c>--bit-layout-dialog-actions-align</c>.
    /// </summary>
    public string? DialogActionsAlign { get; set; }

    /// <summary>Responsive breakpoint tokens driving the predefined <see cref="BitScreenQuery"/> values. Map to the <c>--bit-bp-*</c> custom properties.</summary>
    public BitThemeBreakpoints Breakpoints { get; set; } = new();
}
