namespace Bit.BlazorUI;

/// <summary>
/// A linear radial axis.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartLinearRadialAxis
{
    /// <summary>
    /// Gets or sets the angle lines configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#angle-line-options">here (Chart.js)</a>.</para>
    /// </summary>
    public BitChartAngleLines AngleLines { get; set; }

    /// <summary>
    /// Gets or sets the grid lines configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/styling.html#grid-line-configuration">here (Chart.js)</a>.</para>
    /// </summary>
    public BitChartGridLines GridLines { get; set; }

    /// <summary>
    /// Gets or sets the point labels configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#point-label-options">here (Chart.js)</a>.</para>
    /// </summary>
    public BitChartPointLabels PointLabels { get; set; }

    /// <summary>
    /// Gets or sets the ticks configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#tick-options">here (Chart.js)</a>.</para>
    /// </summary>
    public BitChartLinearRadialTicks Ticks { get; set; }
}
