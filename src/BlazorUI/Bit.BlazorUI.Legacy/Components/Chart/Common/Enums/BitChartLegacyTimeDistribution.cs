namespace Bit.BlazorUI.Legacy;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html#scale-distribution">here (Chart.js)</a>.
/// </summary>
public sealed class BitChartLegacyTimeDistribution : BitChartLegacyStringEnum
{
    /// <summary>
    /// Data are spread according to their time (distances can vary)
    /// </summary>
    public static BitChartLegacyTimeDistribution Linear => new BitChartLegacyTimeDistribution("linear");

    /// <summary>
    /// Data are spread at the same distance from each other
    /// </summary>
    public static BitChartLegacyTimeDistribution Series => new BitChartLegacyTimeDistribution("series");


    private BitChartLegacyTimeDistribution(string stringRep) : base(stringRep) { }
}
