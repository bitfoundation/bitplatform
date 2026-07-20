namespace Bit.BlazorUI.Legacy;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/#common-configuration">here (Chart.js)</a>.
/// </summary>
public sealed class BitChartLegacyAxisDisplay : BitChartLegacyObjectEnum
{
    /// <summary>
    /// Hidden
    /// </summary>
    public static BitChartLegacyAxisDisplay False => new BitChartLegacyAxisDisplay(false);

    /// <summary>
    /// Visible
    /// </summary>
    public static BitChartLegacyAxisDisplay True => new BitChartLegacyAxisDisplay(true);

    /// <summary>
    /// Visible only if at least one associated dataset is visible
    /// </summary>
    public static BitChartLegacyAxisDisplay Auto => new BitChartLegacyAxisDisplay("auto");


    private BitChartLegacyAxisDisplay(string stringValue) : base(stringValue) { }
    private BitChartLegacyAxisDisplay(bool boolValue) : base(boolValue) { }
}
