namespace Bit.BlazorUI;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/line.html#stepped-line">here (Chart.js)</a>.
/// </summary>
public sealed class BitChartSteppedLine : BitChartObjectEnum
{
    /// <summary>
    /// No Step Interpolation (default)
    /// </summary>
    public static BitChartSteppedLine False => new BitChartSteppedLine(false);

    /// <summary>
    /// Step-before Interpolation (same as <see cref="Before"/>)
    /// </summary>
    public static BitChartSteppedLine True => new BitChartSteppedLine(true);

    /// <summary>
    /// Step-before Interpolation
    /// </summary>
    public static BitChartSteppedLine Before => new BitChartSteppedLine("before");

    /// <summary>
    /// Step-after Interpolation
    /// </summary>
    public static BitChartSteppedLine After => new BitChartSteppedLine("after");

    /// <summary>
    /// Step-middle Interpolation
    /// </summary>
    public static BitChartSteppedLine Middle => new BitChartSteppedLine("middle");

    private BitChartSteppedLine(string stringValue) : base(stringValue) { }
    private BitChartSteppedLine(bool boolValue) : base(boolValue) { }
}
