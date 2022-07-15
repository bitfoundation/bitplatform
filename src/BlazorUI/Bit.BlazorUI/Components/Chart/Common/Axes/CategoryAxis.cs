namespace Bit.BlazorUI;

/// <summary>
/// This axis is to be used when you want to display <see cref="string"/> values for an axis.
/// <para>This axis has to be used when using/defining <see cref="ChartData.Labels"/>, <see cref="ChartData.XLabels"/> and/or <see cref="ChartData.YLabels"/>.</para>
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/category.html">here (Chart.js)</a>.</para>
/// </summary>
public class CategoryAxis : CartesianAxis<CategoryTicks>
{
    /// <inheritdoc/>
    public override AxisType Type => AxisType.Category;
}
