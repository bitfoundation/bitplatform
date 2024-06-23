namespace Bit.BlazorUI;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/#common-configuration">here (Chart.js)</a>.
/// </summary>
public sealed class BitChartAxisDisplay : BitChartObjectEnum
{
    /// <summary>
    /// Hidden
    /// </summary>
    public static BitChartAxisDisplay False => new BitChartAxisDisplay(false);

    /// <summary>
    /// Visible
    /// </summary>
    public static BitChartAxisDisplay True => new BitChartAxisDisplay(true);

    /// <summary>
    /// Visible only if at least one associated dataset is visible
    /// </summary>
    public static BitChartAxisDisplay Auto => new BitChartAxisDisplay("auto");


    private BitChartAxisDisplay(string stringValue) : base(stringValue) { }
    private BitChartAxisDisplay(bool boolValue) : base(boolValue) { }
}
