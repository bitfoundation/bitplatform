namespace Bit.BlazorUI;

/// <summary>
/// Specifies the scale boundary strategy.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html#scale-bounds">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BitChartScaleBound : BitChartStringEnum
{
    /// <summary>
    /// Makes sure data are fully visible, labels outside are removed.
    /// </summary>
    public static BitChartScaleBound Data => new BitChartScaleBound("data");

    /// <summary>
    /// Makes sure ticks are fully visible, data outside are truncated.
    /// </summary>
    public static BitChartScaleBound Ticks => new BitChartScaleBound("ticks");

    private BitChartScaleBound(string stringRep) : base(stringRep) { }
}
