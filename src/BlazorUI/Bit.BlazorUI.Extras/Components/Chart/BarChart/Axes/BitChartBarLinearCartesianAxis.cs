namespace Bit.BlazorUI;

/// <summary>
/// Extended version of <see cref="BitChartLinearCartesianAxis"/> for use in a bar chart.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/bar.html#scale-configuration">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartBarLinearCartesianAxis : BitChartLinearCartesianAxis
{
    /// <summary>
    /// Gets or sets the percentage (0-1) of the available width each bar should be within the category width.
    /// 1.0 will take the whole category width and put the bars right next to each other.
    /// </summary>
    public double? BarPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage (0-1) of the available width each category should be within the sample width.
    /// </summary>
    public double? CategoryPercentage { get; set; }

    /// <summary>
    /// Gets or sets the width of each bar in pixels.
    /// If set to <see cref="BitChartBarThickness.Flex"/>, it computes "optimal" sample widths that globally arrange bars side by side. If not set (default), bars are equally sized based on the smallest interval.
    /// </summary>
    public BitChartBarThickness BarThickness { get; set; } = default!;

    /// <summary>
    /// Gets or sets the maximum bar thickness.
    /// Set this to ensure that bars are not sized thicker than this.
    /// </summary>
    public double? MaxBarThickness { get; set; }

    /// <summary>
    /// Gets or sets the minimum bar length.
    /// Set this to ensure that bars have a minimum length in pixels.
    /// </summary>
    public double? MinBarLength { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether grid lines will be shifted to be between labels or not.
    /// If <see langword="true"/>, the bars for a particular data point fall between the grid lines.
    /// The grid line will move to the left by one half of the tick interval.
    /// If <see langword="false"/>, the grid line will go right down the middle of the bars.
    /// <para>Changing this value will directly affect <see cref="BitChartGridLines.OffsetGridLines"/> of the property <see cref="BitChartCartesianAxis.GridLines"/> in this instance.</para>
    /// </summary>
    public bool? OffsetGridLines
    {
        get => GridLines?.OffsetGridLines;
        set
        {
            if (GridLines == null)
            {
                if (value == null)
                {
                    return;
                }
                else
                {
                    GridLines = new BitChartGridLines();
                }
            }

            GridLines.OffsetGridLines = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the bar chart is stacked or not.
    /// Bar charts can be configured into stacked bar charts by changing the settings on the X and Y axes to enable stacking.
    /// Stacked bar charts can be used to show how one data series is made up of a number of smaller pieces.
    /// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/bar.html#stacked-bar-chart">here (Chart.js)</a>.</para>
    /// </summary>
    public bool? Stacked { get; set; }
}

