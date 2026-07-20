namespace Bit.BlazorUI.Legacy;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html#ticks-source">here (Chart.js)</a>..
/// </summary>
public sealed class BitChartLegacyTickSource : BitChartLegacyStringEnum
{
    /// <summary>
    /// Generates "optimal" ticks based on scale size and time options.
    /// </summary>
    public static BitChartLegacyTickSource Auto => new BitChartLegacyTickSource("auto");

    /// <summary>
    /// Generates ticks from data (including labels from data {t|x|y} objects).
    /// </summary>
    public static BitChartLegacyTickSource Data => new BitChartLegacyTickSource("data");

    /// <summary>
    /// Generates ticks from user given <see cref="BitChartLegacyChartData.Labels"/> values ONLY.
    /// </summary>
    public static BitChartLegacyTickSource Labels => new BitChartLegacyTickSource("labels");


    private BitChartLegacyTickSource(string stringRep) : base(stringRep) { }
}
