namespace Bit.BlazorUI;

/// <summary>
/// The linear scale is use to chart numerical data. It can be placed on either the x or y axis.
/// As the name suggests, linear interpolation is used to determine where a value lies on the axis.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/linear.html">here (Chart.js)</a>.</para>
/// </summary>
public class LinearCartesianAxis : CartesianAxis<LinearCartesianTicks>
{
    /// <inheritdoc/>
    public override AxisType Type => AxisType.Linear;
}
