namespace Bit.BlazorUI;

/// <summary>
/// This axis is to be used when you want to display <see cref="string"/> values for an axis.
/// <para>This axis has to be used when using/defining <see cref="BitChartChartData.Labels"/>, <see cref="BitChartChartData.XLabels"/> and/or <see cref="BitChartChartData.YLabels"/>.</para>
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/category.html">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartCategoryAxis : BitChartCartesianAxis<BitChartCategoryTicks>
{
    /// <inheritdoc/>
    public override BitChartAxisType Type => BitChartAxisType.Category;
}
