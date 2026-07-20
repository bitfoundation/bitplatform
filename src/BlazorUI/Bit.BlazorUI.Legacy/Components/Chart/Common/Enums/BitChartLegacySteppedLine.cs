namespace Bit.BlazorUI.Legacy;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/line.html#stepped-line">here (Chart.js)</a>.
/// </summary>
public sealed class BitChartLegacySteppedLine : BitChartLegacyObjectEnum
{
    /// <summary>
    /// No Step Interpolation (default)
    /// </summary>
    public static BitChartLegacySteppedLine False => new BitChartLegacySteppedLine(false);

    /// <summary>
    /// Step-before Interpolation (same as <see cref="Before"/>)
    /// </summary>
    public static BitChartLegacySteppedLine True => new BitChartLegacySteppedLine(true);

    /// <summary>
    /// Step-before Interpolation
    /// </summary>
    public static BitChartLegacySteppedLine Before => new BitChartLegacySteppedLine("before");

    /// <summary>
    /// Step-after Interpolation
    /// </summary>
    public static BitChartLegacySteppedLine After => new BitChartLegacySteppedLine("after");

    /// <summary>
    /// Step-middle Interpolation
    /// </summary>
    public static BitChartLegacySteppedLine Middle => new BitChartLegacySteppedLine("middle");

    private BitChartLegacySteppedLine(string stringValue) : base(stringValue) { }
    private BitChartLegacySteppedLine(bool boolValue) : base(boolValue) { }
}
