using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Generates nice tick boundaries and labels for a time scale. Axis values are stored as
/// OLE Automation dates (<see cref="DateTime.ToOADate"/>), i.e. days since 1899-12-30.
/// </summary>
public static class BitChartTimeAxis
{
    public static BitChartTimeUnit ChooseUnit(DateTime min, DateTime max)
    {
        var span = max - min;
        if (span <= TimeSpan.FromSeconds(2)) return BitChartTimeUnit.Millisecond;
        if (span <= TimeSpan.FromMinutes(2)) return BitChartTimeUnit.Second;
        if (span <= TimeSpan.FromHours(2)) return BitChartTimeUnit.Minute;
        if (span <= TimeSpan.FromDays(2)) return BitChartTimeUnit.Hour;
        if (span <= TimeSpan.FromDays(14)) return BitChartTimeUnit.Day;
        if (span <= TimeSpan.FromDays(60)) return BitChartTimeUnit.Week;
        if (span <= TimeSpan.FromDays(365 * 2)) return BitChartTimeUnit.Month;
        return BitChartTimeUnit.Year;
    }

    public static string DefaultFormat(DateTime d, BitChartTimeUnit unit) => unit switch
    {
        BitChartTimeUnit.Millisecond => d.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
        BitChartTimeUnit.Second => d.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        BitChartTimeUnit.Minute => d.ToString("HH:mm", CultureInfo.InvariantCulture),
        BitChartTimeUnit.Hour => d.ToString("HH:mm", CultureInfo.InvariantCulture),
        BitChartTimeUnit.Day => d.ToString("MMM d", CultureInfo.InvariantCulture),
        BitChartTimeUnit.Week => d.ToString("MMM d", CultureInfo.InvariantCulture),
        BitChartTimeUnit.Month => d.ToString("MMM yyyy", CultureInfo.InvariantCulture),
        BitChartTimeUnit.Quarter => $"Q{(d.Month - 1) / 3 + 1} {d.Year}",
        _ => d.Year.ToString(CultureInfo.InvariantCulture)
    };

    private static DateTime Floor(DateTime d, BitChartTimeUnit unit) => unit switch
    {
        BitChartTimeUnit.Millisecond => d,
        BitChartTimeUnit.Second => new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, d.Second),
        BitChartTimeUnit.Minute => new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0),
        BitChartTimeUnit.Hour => new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0),
        BitChartTimeUnit.Day or BitChartTimeUnit.Week => d.Date,
        BitChartTimeUnit.Month => new DateTime(d.Year, d.Month, 1),
        BitChartTimeUnit.Quarter => new DateTime(d.Year, (d.Month - 1) / 3 * 3 + 1, 1),
        _ => new DateTime(d.Year, 1, 1)
    };

    private static DateTime Next(DateTime d, BitChartTimeUnit unit, int step) => unit switch
    {
        BitChartTimeUnit.Millisecond => d.AddMilliseconds(step),
        BitChartTimeUnit.Second => d.AddSeconds(step),
        BitChartTimeUnit.Minute => d.AddMinutes(step),
        BitChartTimeUnit.Hour => d.AddHours(step),
        BitChartTimeUnit.Day => d.AddDays(step),
        BitChartTimeUnit.Week => d.AddDays(7 * step),
        BitChartTimeUnit.Month => d.AddMonths(step),
        BitChartTimeUnit.Quarter => d.AddMonths(3 * step),
        _ => d.AddYears(step)
    };

    /// <summary>Generates (oaDateValue, label) ticks between min and max.</summary>
    public static List<(double Value, string Label)> Ticks(double minOa, double maxOa, BitChartTimeUnit unit,
        Func<DateTime, string>? format, int maxTicks = 11)
    {
        var min = DateTime.FromOADate(minOa);
        var max = DateTime.FromOADate(maxOa);
        if (unit == BitChartTimeUnit.Auto) unit = ChooseUnit(min, max);

        // Choose a step so we don't exceed maxTicks.
        int step = 1;
        var probe = Floor(min, unit);
        int count = 0;
        for (var t = probe; t <= max; t = Next(t, unit, 1)) { count++; if (count > 5000) break; }
        if (count > maxTicks) step = (int)Math.Ceiling((double)count / maxTicks);

        var ticks = new List<(double, string)>();
        var cur = Floor(min, unit);
        while (cur <= max)
        {
            if (cur >= min)
                ticks.Add((cur.ToOADate(), (format ?? (d => DefaultFormat(d, unit)))(cur)));
            cur = Next(cur, unit, step);
            if (ticks.Count > maxTicks * 3) break;
        }
        if (ticks.Count == 0)
            ticks.Add((minOa, (format ?? (d => DefaultFormat(d, unit)))(min)));
        return ticks;
    }
}
