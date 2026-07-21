namespace Bit.BlazorUI.Legacy;

/// <summary>
/// The time scale is used to display times and dates.
/// When building its ticks, it will automatically calculate the most comfortable unit base on the size of the scale.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartLegacyTimeAxis : BitChartLegacyCartesianAxis<BitChartLegacyTimeTicks>
{
    /// <inheritdoc/>
    public override BitChartLegacyAxisType Type => BitChartLegacyAxisType.Time;

    /// <summary>
    /// Gets or sets the distribution which controls the data distribution along the scale.
    /// </summary>
    public BitChartLegacyTimeDistribution? Distribution { get; set; }

    /// <summary>
    /// Gets or sets the bounds which control the scale boundary strategy (bypassed by min/max time options).
    /// </summary>
    public BitChartLegacyScaleBound? Bounds { get; set; }

    /// <summary>
    /// Gets or sets the configuration for time related options.
    /// </summary>
    public BitChartLegacyTimeOptions? Time { get; set; }
}
