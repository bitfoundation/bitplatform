namespace Bit.BlazorUI;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html#ticks-source">here (Chart.js)</a>..
/// </summary>
public sealed class BitChartTickSource : BitChartStringEnum
{
    /// <summary>
    /// Generates "optimal" ticks based on scale size and time options.
    /// </summary>
    public static BitChartTickSource Auto => new BitChartTickSource("auto");

    /// <summary>
    /// Generates ticks from data (including labels from data {t|x|y} objects).
    /// </summary>
    public static BitChartTickSource Data => new BitChartTickSource("data");

    /// <summary>
    /// Generates ticks from user given <see cref="BitChartChartData.Labels"/> values ONLY.
    /// </summary>
    public static BitChartTickSource Labels => new BitChartTickSource("labels");


    private BitChartTickSource(string stringRep) : base(stringRep) { }
}
