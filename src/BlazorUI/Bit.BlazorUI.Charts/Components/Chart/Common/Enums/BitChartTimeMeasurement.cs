namespace Bit.BlazorUI;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html#time-units">here (Chart.js)</a>.
/// </summary>
public sealed class BitChartTimeMeasurement : BitChartStringEnum
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static BitChartTimeMeasurement Millisecond => new BitChartTimeMeasurement("millisecond");
    public static BitChartTimeMeasurement Second => new BitChartTimeMeasurement("second");
    public static BitChartTimeMeasurement Minute => new BitChartTimeMeasurement("minute");
    public static BitChartTimeMeasurement Hour => new BitChartTimeMeasurement("hour");
    public static BitChartTimeMeasurement Day => new BitChartTimeMeasurement("day");
    public static BitChartTimeMeasurement Week => new BitChartTimeMeasurement("week");
    public static BitChartTimeMeasurement Month => new BitChartTimeMeasurement("month");
    public static BitChartTimeMeasurement Quarter => new BitChartTimeMeasurement("quarter");
    public static BitChartTimeMeasurement Year => new BitChartTimeMeasurement("year");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private BitChartTimeMeasurement(string stringRep) : base(stringRep) { }
}
