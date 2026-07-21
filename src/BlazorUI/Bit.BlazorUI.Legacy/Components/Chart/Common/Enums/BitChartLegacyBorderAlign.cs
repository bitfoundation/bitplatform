namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Specifies the border alignment of a pie chart and a polar area chart.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/doughnut.html#border-alignment">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BitChartLegacyBorderAlign : BitChartLegacyStringEnum
{
    /// <summary>
    /// When <see cref="BitChartLegacyBorderAlign.Center" /> is set, the borders of arcs next to each other will overlap. The default value.
    /// </summary>
    public static BitChartLegacyBorderAlign Center => new BitChartLegacyBorderAlign("center");

    /// <summary>
    /// When <see cref="BitChartLegacyBorderAlign.Inner" /> is set, it is guaranteed that all the borders will not overlap.
    /// </summary>
    public static BitChartLegacyBorderAlign Inner => new BitChartLegacyBorderAlign("inner");

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartLegacyBorderAlign"/> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string"/> value to set.</param>
    private BitChartLegacyBorderAlign(string stringValue) : base(stringValue) { }
}
