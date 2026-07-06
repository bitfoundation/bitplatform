namespace Bit.BlazorUI;

/// <summary>
/// Per-segment line styling, mirroring Chart.js dataset <c>segment</c>. Each callback receives the
/// segment endpoints and returns an override (or null to use the dataset default).
/// </summary>
public sealed class BitChartLineSegmentStyle
{
    public Func<BitChartSegmentContext, string?>? BorderColor { get; set; }
    public Func<BitChartSegmentContext, double?>? BorderWidth { get; set; }
    public Func<BitChartSegmentContext, IReadOnlyList<double>?>? BorderDash { get; set; }
}
