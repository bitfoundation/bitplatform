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

    /// <summary>
    /// The label a tick gets when the caller supplies no formatter. Month and day names come from the
    /// chart's culture, so a localized chart does not end up printing French numbers next to English
    /// months; the invariant culture is the default, which keeps output stable.
    /// </summary>
    public static string DefaultFormat(DateTime d, BitChartTimeUnit unit, CultureInfo? culture = null)
    {
        var c = culture ?? CultureInfo.InvariantCulture;
        return unit switch
        {
            BitChartTimeUnit.Millisecond => d.ToString("HH:mm:ss.fff", c),
            BitChartTimeUnit.Second => d.ToString("HH:mm:ss", c),
            BitChartTimeUnit.Minute => d.ToString("HH:mm", c),
            BitChartTimeUnit.Hour => d.ToString("HH:mm", c),
            BitChartTimeUnit.Day => d.ToString("MMM d", c),
            BitChartTimeUnit.Week => d.ToString("MMM d", c),
            BitChartTimeUnit.Month => d.ToString("MMM yyyy", c),
            BitChartTimeUnit.Quarter => $"Q{(d.Month - 1) / 3 + 1} {d.Year.ToString(c)}",
            _ => d.Year.ToString(c)
        };
    }

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

    /// <summary>
    /// Advances a tick by one step, saturating at <see cref="DateTime.MaxValue"/>. Returns false when it
    /// could not move, which is what stops the tick loops at the end of the calendar instead of throwing
    /// or spinning.
    /// </summary>
    private static bool TryNext(DateTime d, BitChartTimeUnit unit, int step, out DateTime next)
    {
        try
        {
            next = Next(d, unit, step);
        }
        catch (ArgumentOutOfRangeException)
        {
            next = DateTime.MaxValue;
        }
        return next > d;
    }

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

    /// <summary>The OLE Automation date range <see cref="DateTime.FromOADate"/> accepts.</summary>
    private const double MinOaDate = -657434.0;
    private const double MaxOaDate = 2958465.99999999;

    /// <summary>
    /// Converts an axis value to a date. Values outside the OLE Automation range - a time scale pointed
    /// at data that is not dates - are clamped instead of throwing out of the render.
    /// </summary>
    private static DateTime ToDate(double oa)
        => DateTime.FromOADate(Math.Clamp(double.IsFinite(oa) ? oa : 0, MinOaDate, MaxOaDate));

    /// <summary>Generates (oaDateValue, label) ticks between min and max.</summary>
    public static List<(double Value, string Label)> Ticks(double minOa, double maxOa, BitChartTimeUnit unit,
        Func<DateTime, string>? format, int maxTicks = 11, CultureInfo? culture = null)
    {
        var min = ToDate(minOa);
        var max = ToDate(maxOa);
        if (max < min) (min, max) = (max, min);
        if (unit == BitChartTimeUnit.Auto) unit = ChooseUnit(min, max);

        // Choose a step so we don't exceed maxTicks.
        int step = 1;
        int count = 0;
        for (var t = Floor(min, unit); t <= max; count++)
        {
            if (count > 5000 || !TryNext(t, unit, 1, out t)) break;
        }
        if (count > maxTicks) step = (int)Math.Ceiling((double)count / maxTicks);

        var ticks = new List<(double, string)>();
        var cur = Floor(min, unit);
        while (cur <= max)
        {
            if (cur >= min)
                ticks.Add((cur.ToOADate(), (format ?? (d => DefaultFormat(d, unit, culture)))(cur)));
            if (ticks.Count > maxTicks * 3) break;
            if (!TryNext(cur, unit, step, out cur)) break;
        }
        if (ticks.Count == 0)
            ticks.Add((minOa, (format ?? (d => DefaultFormat(d, unit, culture)))(min)));
        return ticks;
    }
}
