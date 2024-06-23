namespace Bit.BlazorUI;

/// <summary>
/// Specifies the border alignment of a pie chart and a polar area chart.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/doughnut.html#border-alignment">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BitChartBorderAlign : BitChartStringEnum
{
    /// <summary>
    /// When <see cref="BitChartBorderAlign.Center" /> is set, the borders of arcs next to each other will overlap. The default value.
    /// </summary>
    public static BitChartBorderAlign Center => new BitChartBorderAlign("center");

    /// <summary>
    /// When <see cref="BitChartBorderAlign.Inner" /> is set, it is guaranteed that all the borders will not overlap.
    /// </summary>
    public static BitChartBorderAlign Inner => new BitChartBorderAlign("inner");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartBorderAlign"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartBorderAlign(string stringValue) : base(stringValue) { }
}
