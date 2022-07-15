namespace Bit.BlazorUI;

/// <summary>
/// A linear radial axis.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html">here (Chart.js)</a>.</para>
/// </summary>
public class LinearRadialAxis
{
    /// <summary>
    /// Gets or sets the angle lines configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#angle-line-options">here (Chart.js)</a>.</para>
    /// </summary>
    public AngleLines AngleLines { get; set; }

    /// <summary>
    /// Gets or sets the grid lines configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/styling.html#grid-line-configuration">here (Chart.js)</a>.</para>
    /// </summary>
    public GridLines GridLines { get; set; }

    /// <summary>
    /// Gets or sets the point labels configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#point-label-options">here (Chart.js)</a>.</para>
    /// </summary>
    public PointLabels PointLabels { get; set; }

    /// <summary>
    /// Gets or sets the ticks configuration.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/radial/linear.html#tick-options">here (Chart.js)</a>.</para>
    /// </summary>
    public LinearRadialTicks Ticks { get; set; }
}
