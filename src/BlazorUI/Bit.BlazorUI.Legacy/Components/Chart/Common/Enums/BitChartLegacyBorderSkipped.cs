namespace Bit.BlazorUI.Legacy;

/// <summary>
///     This setting is used to avoid drawing the bar stroke at the base of the fill.
///     In general, this does not need to be changed except when creating chart types that derive from a bar chart.
///     Note: For negative bars in vertical chart, top and bottom are flipped. Same goes for left and right in horizontal
///     chart.
///     <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/bar.html#borderskipped">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartLegacyBorderSkipped : BitChartLegacyObjectEnum
{
    /// <summary>
    ///     Creates a new instance of the <see cref="BitChartLegacyBorderSkipped" /> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string" /> value to set.</param>
    private BitChartLegacyBorderSkipped(string stringValue) : base(stringValue) { }

    /// <summary>
    ///     Creates a new instance of the <see cref="BitChartLegacyBorderSkipped" /> class.
    /// </summary>
    /// <param name="boolValue">The <see cref="bool" /> value to set.</param>
    private BitChartLegacyBorderSkipped(bool boolValue) : base(boolValue) { }

    /// <summary>
    ///     The bottom border skipped style.
    /// </summary>
    public static BitChartLegacyBorderSkipped Bottom => new BitChartLegacyBorderSkipped("bottom");

    /// <summary>
    ///     The false border skipped style.
    /// </summary>
    public static BitChartLegacyBorderSkipped False => new BitChartLegacyBorderSkipped(false);

    /// <summary>
    ///     The left border skipped style.
    /// </summary>
    public static BitChartLegacyBorderSkipped Left => new BitChartLegacyBorderSkipped("left");

    /// <summary>
    ///     The right border skipped style.
    /// </summary>
    public static BitChartLegacyBorderSkipped Right => new BitChartLegacyBorderSkipped("right");

    /// <summary>
    ///     The top border skipped style.
    /// </summary>
    public static BitChartLegacyBorderSkipped Top => new BitChartLegacyBorderSkipped("top");
}
