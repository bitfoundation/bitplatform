namespace Bit.BlazorUI;

/// <summary>
/// The major ticks sub-config of the tick-configuration (see <see cref="BitChartTicks"/>).
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/styling.html#major-tick-configuration">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartMajorTicks : BitChartSubTicks
{
    /// <summary>
    /// Gets or sets the value indicating whether these options are used to show major ticks.
    /// </summary>
    public bool? Enabled { get; set; }
}
