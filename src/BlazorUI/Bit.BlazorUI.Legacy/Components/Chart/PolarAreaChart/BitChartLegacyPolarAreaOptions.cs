namespace Bit.BlazorUI.Legacy;

/// <summary>
/// The options-subconfig of a <see cref="BitChartLegacyPolarAreaConfig"/>.
/// </summary>
public class BitChartLegacyPolarAreaOptions : BitChartLegacyBaseConfigOptions
{
    /// <summary>
    /// Gets or sets the starting angle to draw arcs for the first item in a dataset.
    /// </summary>
    public double? StartAngle { get; set; }

    /// <summary>
    /// Gets or sets the animation-configuration for this chart.
    /// </summary>
    public new BitChartLegacyArcAnimation? Animation { get; set; }

    /// <summary>
    /// The scale (axis) for this chart.
    /// </summary>
    public BitChartLegacyLinearRadialAxis? Scale { get; set; }
}
