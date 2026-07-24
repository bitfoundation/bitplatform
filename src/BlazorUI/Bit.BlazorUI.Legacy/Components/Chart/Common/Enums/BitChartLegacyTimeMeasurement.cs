namespace Bit.BlazorUI.Legacy;

/// <summary>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/time.html#time-units">here (Chart.js)</a>.
/// </summary>
public sealed class BitChartLegacyTimeMeasurement : BitChartLegacyStringEnum
{
    public static BitChartLegacyTimeMeasurement Millisecond => new BitChartLegacyTimeMeasurement("millisecond");
    public static BitChartLegacyTimeMeasurement Second => new BitChartLegacyTimeMeasurement("second");
    public static BitChartLegacyTimeMeasurement Minute => new BitChartLegacyTimeMeasurement("minute");
    public static BitChartLegacyTimeMeasurement Hour => new BitChartLegacyTimeMeasurement("hour");
    public static BitChartLegacyTimeMeasurement Day => new BitChartLegacyTimeMeasurement("day");
    public static BitChartLegacyTimeMeasurement Week => new BitChartLegacyTimeMeasurement("week");
    public static BitChartLegacyTimeMeasurement Month => new BitChartLegacyTimeMeasurement("month");
    public static BitChartLegacyTimeMeasurement Quarter => new BitChartLegacyTimeMeasurement("quarter");
    public static BitChartLegacyTimeMeasurement Year => new BitChartLegacyTimeMeasurement("year");

    private BitChartLegacyTimeMeasurement(string stringRep) : base(stringRep) { }
}
