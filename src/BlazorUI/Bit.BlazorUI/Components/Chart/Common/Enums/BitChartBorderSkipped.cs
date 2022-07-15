namespace Bit.BlazorUI;

/// <summary>
///     This setting is used to avoid drawing the bar stroke at the base of the fill.
///     In general, this does not need to be changed except when creating chart types that derive from a bar chart.
///     Note: For negative bars in vertical chart, top and bottom are flipped. Same goes for left and right in horizontal
///     chart.
///     <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/bar.html#borderskipped">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartBorderSkipped : BitChartObjectEnum
{
    /// <summary>
    ///     Creates a new instance of the <see cref="BitChartBorderSkipped" /> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string" /> value to set.</param>
    private BitChartBorderSkipped(string stringValue) : base(stringValue) { }

    /// <summary>
    ///     Creates a new instance of the <see cref="BitChartBorderSkipped" /> class.
    /// </summary>
    /// <param name="boolValue">The <see cref="bool" /> value to set.</param>
    private BitChartBorderSkipped(bool boolValue) : base(boolValue) { }

    /// <summary>
    ///     The bottom border skipped style.
    /// </summary>
    public static BitChartBorderSkipped Bottom => new BitChartBorderSkipped("bottom");

    /// <summary>
    ///     The false border skipped style.
    /// </summary>
    public static BitChartBorderSkipped False => new BitChartBorderSkipped(false);

    /// <summary>
    ///     The left border skipped style.
    /// </summary>
    public static BitChartBorderSkipped Left => new BitChartBorderSkipped("left");

    /// <summary>
    ///     The right border skipped style.
    /// </summary>
    public static BitChartBorderSkipped Right => new BitChartBorderSkipped("right");

    /// <summary>
    ///     The top border skipped style.
    /// </summary>
    public static BitChartBorderSkipped Top => new BitChartBorderSkipped("top");
}
