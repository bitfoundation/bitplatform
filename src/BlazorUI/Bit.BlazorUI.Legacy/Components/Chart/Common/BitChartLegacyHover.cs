namespace Bit.BlazorUI.Legacy;

/// <summary>
/// The hover configuration contains general settings regarding hovering over a chart.
/// </summary>
public class BitChartLegacyHover
{
    /// <summary>
    /// Gets or sets which elements appear in the tooltip.
    /// </summary>
    public BitChartLegacyInteractionMode? Mode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the hover mode only applies
    /// when the mouse position intersects an item on the chart.
    /// </summary>
    public bool? Intersect { get; set; }

    /// <summary>
    /// Gets or sets which directions are used in calculating distances.
    /// Defaults to <see cref="BitChartLegacyAxisDirection.X"/> for <see cref="Mode"/> == <see cref="BitChartLegacyInteractionMode.Index"/>
    /// and <see cref="BitChartLegacyAxisDirection.XY"/> for <see cref="Mode"/> == <see cref="BitChartLegacyInteractionMode.Dataset"/> or <see cref="BitChartLegacyInteractionMode.Nearest"/>.
    /// </summary>
    public BitChartLegacyAxisDirection? Axis { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds it takes to animate hover style changes.
    /// </summary>
    public long? AnimationDuration { get; set; }
}
