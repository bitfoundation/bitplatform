namespace Bit.BlazorUI;

/// <summary>
/// The logarithmic scale is use to chart numerical data. It can be placed on either the x or y axis.
/// As the name suggests, logarithmic interpolation is used to determine where a value lies on the axis.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/logarithmic.html">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartLogarithmicAxis : BitChartCartesianAxis<BitChartLogarithmicTicks>
{
    /// <inheritdoc/>
    public override BitChartAxisType Type => BitChartAxisType.Logarithmic;
}
