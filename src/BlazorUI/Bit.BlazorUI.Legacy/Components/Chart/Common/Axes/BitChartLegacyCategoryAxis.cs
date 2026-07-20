namespace Bit.BlazorUI.Legacy;

/// <summary>
/// This axis is to be used when you want to display <see cref="string"/> values for an axis.
/// <para>This axis has to be used when using/defining <see cref="BitChartLegacyChartData.Labels"/>, <see cref="BitChartLegacyChartData.XLabels"/> and/or <see cref="BitChartLegacyChartData.YLabels"/>.</para>
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/category.html">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartLegacyCategoryAxis : BitChartLegacyCartesianAxis<BitChartLegacyCategoryTicks>
{
    /// <inheritdoc/>
    public override BitChartLegacyAxisType Type => BitChartLegacyAxisType.Category;
}
