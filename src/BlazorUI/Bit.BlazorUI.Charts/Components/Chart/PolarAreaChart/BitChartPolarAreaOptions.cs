namespace Bit.BlazorUI;

/// <summary>
/// The options-subconfig of a <see cref="BitChartPolarAreaConfig"/>.
/// </summary>
public class BitChartPolarAreaOptions : BitChartBaseConfigOptions
{
    /// <summary>
    /// Gets or sets the starting angle to draw arcs for the first item in a dataset.
    /// </summary>
    public double? StartAngle { get; set; }

    /// <summary>
    /// Gets or sets the animation-configuration for this chart.
    /// </summary>
    public new BitChartArcAnimation Animation { get; set; }

    /// <summary>
    /// The scale (axis) for this chart.
    /// </summary>
    public BitChartLinearRadialAxis Scale { get; set; }
}
