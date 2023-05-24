namespace Bit.BlazorUI;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html#scale-distribution">here (Chart.js)</a>.
/// </summary>
public sealed class BitChartTimeDistribution : BitChartStringEnum
{
    /// <summary>
    /// Data are spread according to their time (distances can vary)
    /// </summary>
    public static BitChartTimeDistribution Linear => new BitChartTimeDistribution("linear");

    /// <summary>
    /// Data are spread at the same distance from each other
    /// </summary>
    public static BitChartTimeDistribution Series => new BitChartTimeDistribution("series");


    private BitChartTimeDistribution(string stringRep) : base(stringRep) { }
}
