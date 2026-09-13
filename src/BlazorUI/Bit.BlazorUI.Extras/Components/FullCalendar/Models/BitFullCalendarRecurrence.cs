using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Describes how a <see cref="BitFullCalendarEvent"/> repeats. Assigning one to
/// <see cref="BitFullCalendarEvent.Recurrence"/> turns the event into a series: the calendar keeps the
/// event itself as the series and renders one occurrence for every date the rule produces inside the
/// visible range. The event's own <see cref="BitFullCalendarEvent.StartDate"/> is always the first
/// occurrence, and every occurrence keeps the event's time of day and duration.
/// <para>
/// Monthly and yearly rules follow the calendar's culture, so a monthly rule rendered with
/// <c>CultureName="fa-IR"</c> repeats on the same day of the Persian month.
/// </para>
/// </summary>
public class BitFullCalendarRecurrence
{
    private int _interval = 1;
    private int? _count;
    private List<DayOfWeek> _daysOfWeek = [];
    private List<DateTime> _exceptionDates = [];
    private List<DateTime> _additionalDates = [];

    /// <summary>The unit the rule repeats in: daily, weekly, monthly, or yearly.</summary>
    public BitFullCalendarRecurrenceFrequency Frequency { get; set; } = BitFullCalendarRecurrenceFrequency.Daily;

    /// <summary>
    /// How many <see cref="Frequency"/> units lie between two repetitions: 1 repeats every unit, 2 every
    /// other one, 15 with a daily frequency every 15 days. Values below 1 are stored as 1.
    /// </summary>
    public int Interval
    {
        get => _interval;
        set => _interval = Math.Max(1, value);
    }

    /// <summary>
    /// The weekdays the rule lands on. With a weekly frequency, every listed day of each repeated week
    /// (for example Monday, Wednesday, and Friday). With a monthly or yearly frequency and a
    /// <see cref="WeekOfMonth"/>, the weekdays picked inside the month. Empty means the weekday the series
    /// starts on; daily rules ignore it. Assigning <c>null</c> coalesces to an empty list.
    /// </summary>
    public List<DayOfWeek> DaysOfWeek
    {
        get => _daysOfWeek;
        set => _daysOfWeek = value ?? [];
    }

    /// <summary>
    /// Monthly and yearly rules only. When set, the rule lands on that week's <see cref="DaysOfWeek"/> of
    /// the month (for example the third Tuesday, or the last Friday). When <c>null</c>, it lands on the day
    /// of the month the series starts on, and months too short to have that day are skipped.
    /// </summary>
    public BitFullCalendarRecurrenceWeekOfMonth? WeekOfMonth { get; set; }

    /// <summary>
    /// The number of occurrences the rule produces, the first one included. Values below 1 are stored as 1.
    /// A date skipped through <see cref="ExceptionDates"/> still counts, so skipping one never extends the
    /// series, and <see cref="AdditionalDates"/> never count. <c>null</c> for no limit.
    /// </summary>
    public int? Count
    {
        get => _count;
        set => _count = value is null ? null : Math.Max(1, value.Value);
    }

    /// <summary>
    /// The last date an occurrence may start on, inclusive; its time of day is ignored. <c>null</c> for no end date.
    /// </summary>
    public DateTime? Until { get; set; }

    /// <summary>
    /// Dates the rule produces an occurrence on that are skipped, compared by date only. Skipping a date
    /// leaves the rest of the pattern untouched. Assigning <c>null</c> coalesces to an empty list.
    /// </summary>
    public List<DateTime> ExceptionDates
    {
        get => _exceptionDates;
        set => _exceptionDates = value ?? [];
    }

    /// <summary>
    /// Extra dates that get an occurrence, at the series' time of day, even though the rule does not
    /// produce them. Compared by date only; a date that is also in <see cref="ExceptionDates"/> is skipped.
    /// They are not limited by <see cref="Count"/> or <see cref="Until"/>. Assigning <c>null</c> coalesces
    /// to an empty list.
    /// </summary>
    public List<DateTime> AdditionalDates
    {
        get => _additionalDates;
        set => _additionalDates = value ?? [];
    }

    /// <summary>Creates a copy of the rule whose date and weekday lists are independent of this one.</summary>
    public BitFullCalendarRecurrence Clone() => new()
    {
        Frequency = Frequency,
        Interval = Interval,
        DaysOfWeek = [.. DaysOfWeek],
        WeekOfMonth = WeekOfMonth,
        Count = Count,
        Until = Until,
        ExceptionDates = [.. ExceptionDates],
        AdditionalDates = [.. AdditionalDates],
    };

    /// <summary>
    /// Returns, in chronological order, the start of every occurrence of a series starting at
    /// <paramref name="seriesStart"/> that begins between <paramref name="rangeStart"/> and
    /// <paramref name="rangeEnd"/> (both inclusive). Weeks start on the culture's first day of the week,
    /// and months and years are those of the culture's calendar.
    /// </summary>
    public List<DateTime> GetOccurrences(DateTime seriesStart, DateTime rangeStart, DateTime rangeEnd, CultureInfo? culture = null)
    {
        var result = new List<DateTime>();
        if (rangeEnd < rangeStart)
            return result;

        culture ??= CultureInfo.CurrentUICulture;
        var exceptions = ExceptionDates.Select(d => d.Date).ToHashSet();
        var seen = new HashSet<DateTime>();

        foreach (var start in EnumerateRuleStarts(seriesStart, rangeStart, rangeEnd, culture))
        {
            if (start < rangeStart || exceptions.Contains(start.Date))
                continue;

            if (seen.Add(start))
                result.Add(start);
        }

        foreach (var date in AdditionalDates)
        {
            if (TryCombine(date.Date, seriesStart.TimeOfDay, out var start) is false)
                continue;

            if (start < rangeStart || start > rangeEnd || exceptions.Contains(start.Date))
                continue;

            if (seen.Add(start))
                result.Add(start);
        }

        result.Sort();
        return result;
    }

    /// <summary>
    /// The number of occurrences the rule itself produces before <paramref name="before"/>, skipped dates
    /// included and additional dates not - what a <see cref="Count"/> has already used up at that point.
    /// </summary>
    internal int CountRuleOccurrencesBefore(DateTime seriesStart, DateTime before, CultureInfo culture)
    {
        if (before <= seriesStart)
            return 0;

        return EnumerateRuleStarts(seriesStart, skipTo: null, before.AddTicks(-1), culture).Count();
    }

    private IEnumerable<DateTime> EnumerateRuleStarts(DateTime seriesStart, DateTime? skipTo, DateTime rangeEnd, CultureInfo culture)
    {
        if (seriesStart > rangeEnd)
            yield break;

        var lastDate = rangeEnd.Date;
        if (Until is { } until && until.Date < lastDate)
            lastDate = until.Date;

        if (seriesStart.Date > lastDate)
            yield break;

        // The series start is always the first occurrence, whether or not the rule itself lands on it.
        yield return seriesStart;
        var produced = 1;
        if (Count is { } firstLimit && produced >= firstLimit)
            yield break;

        var calendar = culture.Calendar;
        var firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
        // A counted rule has to be walked from its start to know how many occurrences are used up; an
        // uncounted one can jump straight to the periods around the requested range.
        var period = Count is null && skipTo is { } target ? EstimateFirstPeriod(seriesStart, target) : 0;

        for (; ; period++)
        {
            if (TryGetPeriodDates(seriesStart, period, calendar, firstDayOfWeek, out var periodStart, out var dates) is false)
                yield break;

            if (periodStart > lastDate)
                yield break;

            foreach (var date in dates)
            {
                if (date > lastDate)
                    yield break;

                if (TryCombine(date, seriesStart.TimeOfDay, out var start) is false)
                    yield break;

                if (start <= seriesStart)
                    continue;

                if (start > rangeEnd)
                    yield break;

                yield return start;
                produced++;
                if (Count is { } limit && produced >= limit)
                    yield break;
            }
        }
    }

    /// <summary>
    /// A period index at or before the first one that can reach <paramref name="rangeStart"/>. Each unit is
    /// divided by an upper bound of its length in days, so the estimate never overshoots.
    /// </summary>
    private long EstimateFirstPeriod(DateTime seriesStart, DateTime rangeStart)
    {
        var days = (rangeStart.Date - seriesStart.Date).TotalDays;
        if (days <= 0)
            return 0;

        var daysPerUnit = Frequency switch
        {
            BitFullCalendarRecurrenceFrequency.Weekly => 7.0,
            BitFullCalendarRecurrenceFrequency.Monthly => 31.0,
            // A Hebrew leap year runs to 385 days.
            BitFullCalendarRecurrenceFrequency.Yearly => 386.0,
            _ => 1.0
        };

        return Math.Max(0, (long)(days / (daysPerUnit * Interval)) - 1);
    }

    private bool TryGetPeriodDates(DateTime seriesStart, long period, Calendar calendar, DayOfWeek firstDayOfWeek,
                                   out DateTime periodStart, out List<DateTime> dates)
    {
        periodStart = default;
        dates = [];

        var steps = period * Interval;
        if (steps > int.MaxValue)
            return false;

        try
        {
            switch (Frequency)
            {
                case BitFullCalendarRecurrenceFrequency.Weekly:
                    periodStart = BitFullCalendarHelpers.StartOfWeek(seriesStart, firstDayOfWeek).AddDays(steps * 7.0);
                    foreach (var offset in GetWeekdays(seriesStart).Select(d => ((int)d - (int)firstDayOfWeek + 7) % 7).Order())
                    {
                        dates.Add(periodStart.AddDays(offset));
                    }
                    return true;

                case BitFullCalendarRecurrenceFrequency.Monthly:
                case BitFullCalendarRecurrenceFrequency.Yearly:
                    var monthStart = seriesStart.Date.AddDays(1 - calendar.GetDayOfMonth(seriesStart));
                    periodStart = Frequency == BitFullCalendarRecurrenceFrequency.Monthly
                        ? calendar.AddMonths(monthStart, (int)steps)
                        : calendar.AddYears(monthStart, (int)steps);
                    AddMonthDates(seriesStart, periodStart, calendar, dates);
                    return true;

                default:
                    periodStart = seriesStart.Date.AddDays(steps);
                    dates.Add(periodStart);
                    return true;
            }
        }
        catch (ArgumentException)
        {
            // The period lies past the range DateTime or the calendar supports; the series ends there.
            return false;
        }
    }

    private void AddMonthDates(DateTime seriesStart, DateTime monthStart, Calendar calendar, List<DateTime> dates)
    {
        var daysInMonth = (calendar.AddMonths(monthStart, 1) - monthStart).Days;

        if (WeekOfMonth is not { } week)
        {
            var day = calendar.GetDayOfMonth(seriesStart);
            if (day <= daysInMonth)
            {
                dates.Add(monthStart.AddDays(day - 1));
            }
            return;
        }

        var lastDay = monthStart.AddDays(daysInMonth - 1);
        foreach (var dayOfWeek in GetWeekdays(seriesStart))
        {
            if (week == BitFullCalendarRecurrenceWeekOfMonth.Last)
            {
                dates.Add(lastDay.AddDays(-(((int)lastDay.DayOfWeek - (int)dayOfWeek + 7) % 7)));
                continue;
            }

            var first = monthStart.AddDays(((int)dayOfWeek - (int)monthStart.DayOfWeek + 7) % 7);
            var date = first.AddDays(7 * (int)week);
            if (date <= lastDay)
            {
                dates.Add(date);
            }
        }

        dates.Sort();
    }

    private IEnumerable<DayOfWeek> GetWeekdays(DateTime seriesStart)
    {
        var days = DaysOfWeek.Where(d => Enum.IsDefined(d)).Distinct().ToList();
        return days.Count > 0 ? days : [seriesStart.DayOfWeek];
    }

    private static bool TryCombine(DateTime date, TimeSpan timeOfDay, out DateTime result)
    {
        if (date.Ticks > DateTime.MaxValue.Ticks - timeOfDay.Ticks)
        {
            result = default;
            return false;
        }

        result = date + timeOfDay;
        return true;
    }
}
