namespace Bit.BlazorUI;

/// <summary>
///     This setting is used to avoid drawing the bar stroke at the base of the fill.
///     In general, this does not need to be changed except when creating chart types that derive from a bar chart.
///     Note: For negative bars in vertical chart, top and bottom are flipped. Same goes for left and right in horizontal
///     chart.
///     <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/bar.html#borderskipped">here (Chart.js)</a>.</para>
/// </summary>
public class BorderSkipped : ObjectEnum
{
    /// <summary>
    ///     Creates a new instance of the <see cref="BorderSkipped" /> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string" /> value to set.</param>
    private BorderSkipped(string stringValue) : base(stringValue) { }

    /// <summary>
    ///     Creates a new instance of the <see cref="BorderSkipped" /> class.
    /// </summary>
    /// <param name="boolValue">The <see cref="bool" /> value to set.</param>
    private BorderSkipped(bool boolValue) : base(boolValue) { }

    /// <summary>
    ///     The bottom border skipped style.
    /// </summary>
    public static BorderSkipped Bottom => new BorderSkipped("bottom");

    /// <summary>
    ///     The false border skipped style.
    /// </summary>
    public static BorderSkipped False => new BorderSkipped(false);

    /// <summary>
    ///     The left border skipped style.
    /// </summary>
    public static BorderSkipped Left => new BorderSkipped("left");

    /// <summary>
    ///     The right border skipped style.
    /// </summary>
    public static BorderSkipped Right => new BorderSkipped("right");

    /// <summary>
    ///     The top border skipped style.
    /// </summary>
    public static BorderSkipped Top => new BorderSkipped("top");
}
