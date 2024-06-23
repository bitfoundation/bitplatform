namespace Bit.BlazorUI;

/// <summary>
/// The hover configuration contains general settings regarding hovering over a chart.
/// </summary>
public class BitChartHover
{
    /// <summary>
    /// Gets or sets which elements appear in the tooltip.
    /// </summary>
    public BitChartInteractionMode Mode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the hover mode only applies
    /// when the mouse position intersects an item on the chart.
    /// </summary>
    public bool? Intersect { get; set; }

    /// <summary>
    /// Gets or sets which directions are used in calculating distances.
    /// Defaults to <see cref="BitChartAxisDirection.X"/> for <see cref="Mode"/> == <see cref="BitChartInteractionMode.Index"/>
    /// and <see cref="BitChartAxisDirection.XY"/> for <see cref="Mode"/> == <see cref="BitChartInteractionMode.Dataset"/> or <see cref="BitChartInteractionMode.Nearest"/>.
    /// </summary>
    public BitChartAxisDirection Axis { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds it takes to animate hover style changes.
    /// </summary>
    public long? AnimationDuration { get; set; }
}
