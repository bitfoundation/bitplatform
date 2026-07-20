namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Specifies the scale boundary strategy.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html#scale-bounds">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BitChartLegacyScaleBound : BitChartLegacyStringEnum
{
    /// <summary>
    /// Makes sure data are fully visible, labels outside are removed.
    /// </summary>
    public static BitChartLegacyScaleBound Data => new BitChartLegacyScaleBound("data");

    /// <summary>
    /// Makes sure ticks are fully visible, data outside are truncated.
    /// </summary>
    public static BitChartLegacyScaleBound Ticks => new BitChartLegacyScaleBound("ticks");

    private BitChartLegacyScaleBound(string stringRep) : base(stringRep) { }
}
