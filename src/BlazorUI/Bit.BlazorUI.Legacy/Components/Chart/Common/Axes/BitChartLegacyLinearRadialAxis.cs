namespace Bit.BlazorUI.Legacy;

/// <summary>
/// A linear radial axis.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartLegacyLinearRadialAxis
{
    /// <summary>
    /// Gets or sets the angle lines configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#angle-line-options">here (Chart.js)</a>.</para>
    /// </summary>
    public BitChartLegacyAngleLines? AngleLines { get; set; }

    /// <summary>
    /// Gets or sets the grid lines configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/styling.html#grid-line-configuration">here (Chart.js)</a>.</para>
    /// </summary>
    public BitChartLegacyGridLines? GridLines { get; set; }

    /// <summary>
    /// Gets or sets the point labels configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#point-label-options">here (Chart.js)</a>.</para>
    /// </summary>
    public BitChartLegacyPointLabels? PointLabels { get; set; }

    /// <summary>
    /// Gets or sets the ticks configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#tick-options">here (Chart.js)</a>.</para>
    /// </summary>
    public BitChartLegacyLinearRadialTicks? Ticks { get; set; }
}
