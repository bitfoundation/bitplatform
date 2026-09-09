using System.Globalization;

namespace Bit.BlazorUI;

public static class BitFullCalendarHelpers
{
    public const int HourHeightPx = 96;

    /// <summary>
    /// Name of the CSS custom property that carries the height of one hour row on the day and week
    /// time grids. The stylesheet sizes every hour row from it, so a consumer may re-scale the grid
    /// by redeclaring it - which is why the placed event blocks and the current-time indicator are
    /// expressed against the same property (see <see cref="HoursToCssLength"/>) rather than against
    /// the <see cref="HourHeightPx"/> constant.
    /// </summary>
    public const string HourHeightVariableName = "--bit-bfc-hour-height";

    /// <summary>
    /// A CSS length spanning <paramref name="hours"/> hour rows, derived from
    /// <see cref="HourHeightVariableName"/> so it tracks whatever height the consumer gave an hour.
    /// </summary>
    public static string HoursToCssLength(double hours)
        => $"calc(var({HourHeightVariableName}, {HourHeightPx}px) * {hours.ToString("F4", CultureInfo.InvariantCulture)})";
    /// <summary>Width of a single hour column on the timeline-mode day/week views.</summary>
    public const int TimelineHourWidthPx = 96;
    /// <summary>Width of a single day column on the timeline-mode month view.</summary>
    public const int TimelineDayWidthPx = 56;
    private const string FormatString = "MMM d, yyyy";

    /// <summary>
    /// Returns the inclusive end <em>date</em> of an event, treating a 00:00 end as ending the
    /// previous day (exclusive midnight). Centralizes the <c>AddTicks(-1)</c> normalization used by
    /// the overlap/placement helpers and the model's <see cref="BitFullCalendarEvent.IsSingleDay"/>
    /// so an event ending at midnight is never counted on the following day.
    /// </summary>
    public static DateTime GetInclusiveEndDate(BitFullCalendarEvent ev)
        => GetInclusiveEndDate(ev.StartDate, ev.EndDate);

    /// <summary>
    /// The same rule applied to a bare range, for a caller holding start/end values that are not an
    /// event yet - the add/edit dialog's draft, for one - so it reads the last covered date exactly
    /// the way the grid does.
    /// </summary>
    public static DateTime GetInclusiveEndDate(DateTime startDate, DateTime endDate)
        => (endDate > startDate ? endDate.AddTicks(-1) : endDate).Date;

    /// <summary>
    /// True when an event overlaps the period <c>[periodStart, periodEndInclusive]</c>. The end is
    /// treated exclusively (<c>EndDate &gt; periodStart</c>) so an event ending exactly at the period
    /// boundary doesn't leak in, but zero-length single-day events (<c>StartDate == EndDate</c>, e.g.
    /// a 00:00 all-day marker) that fall on or after <paramref name="periodStart"/> are still kept -
    /// otherwise they'd be dropped by the strict <c>EndDate &gt; periodStart</c> check even though they
    /// sit inside the visible range. Used by the year/week/month period helpers so they share one rule.
    /// </summary>
    private static bool OverlapsPeriod(BitFullCalendarEvent ev, DateTime periodStart, DateTime periodEndInclusive)
        => ev.StartDate.Date <= periodEndInclusive
           && (ev.EndDate > periodStart || (ev.StartDate == ev.EndDate && ev.StartDate >= periodStart));

    /// <summary>
    /// True when two events occupy the same instant. Zero-length markers never conflict, and an
    /// event that merely starts as another one ends is not an overlap.
    /// </summary>
    public static bool EventsOverlap(BitFullCalendarEvent a, BitFullCalendarEvent b)
        => a.StartDate < b.EndDate && b.StartDate < a.EndDate;

    // -- Recurrence ------------------------------

    /// <summary>
    /// Upper bound on the occurrences one series may produce in a single expansion. A daily series
    /// over a year is 365, so this only stops a pathological range or interval from spinning.
    /// </summary>
    public const int MaxOccurrencesPerSeries = 2000;

    /// <summary>
    /// Replaces every recurring master in <paramref name="events"/> with the occurrences that fall
    /// inside <c>[rangeStart, rangeEndInclusive]</c>, leaving one-off events untouched and in place.
    /// <para>
    /// Only the visible range is expanded, so an open-ended series costs nothing to keep. Each
    /// occurrence carries the master's fields plus <see cref="BitFullCalendarEvent.SeriesId"/>,
    /// <see cref="BitFullCalendarEvent.OccurrenceDate"/>, and an id derived from the master's, and is
    /// marked read-only: the master is what a consumer edits.
    /// </para>
    /// </summary>
    public static List<BitFullCalendarEvent> ExpandRecurrences(
        IReadOnlyList<BitFullCalendarEvent> events,
        DateTime rangeStart,
        DateTime rangeEndInclusive)
    {
        var result = new List<BitFullCalendarEvent>(events.Count);

        foreach (var ev in events)
        {
            if (ev.Recurrence is not { } rule)
            {
                result.Add(ev);
                continue;
            }

            result.AddRange(ExpandSeries(ev, rule, rangeStart.Date, rangeEndInclusive.Date));
        }

        return result;
    }

    private static IEnumerable<BitFullCalendarEvent> ExpandSeries(
        BitFullCalendarEvent master,
        BitFullCalendarRecurrence rule,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var duration = master.EndDate - master.StartDate;
        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;

        var timeOfDay = master.StartDate.TimeOfDay;
        // An occurrence that begins before the range can still reach into it, so the walk starts
        // early enough to catch one that spans the whole visible window.
        var reachBack = rangeStart.AddDays(-Math.Max(1, (int)Math.Ceiling(duration.TotalDays) + 1));

        var exceptions = rule.ExceptionDates is { Count: > 0 }
            ? rule.ExceptionDates.Select(d => d.Date).ToHashSet()
            : null;

        var emitted = 0;
        foreach (var date in EnumerateOccurrenceDates(master.StartDate.Date, rule, reachBack, rangeEnd))
        {
            // A skipped date still consumes its place in the series, which is how a cancelled
            // occurrence behaves in every calendar client - Count is a position, not a total shown.
            if (exceptions?.Contains(date) is true)
                continue;

            if (date < reachBack)
                continue;

            var start = date + timeOfDay;
            if (start > rangeEnd.AddDays(1).AddTicks(-1))
                break;
            if (start + duration < rangeStart)
                continue;

            yield return new BitFullCalendarEvent
            {
                // Derived from the master's id so month positioning and @key stay stable per date.
                Id = $"{master.Id}@{date:yyyyMMdd}",
                SeriesId = master.Id,
                OccurrenceDate = date,
                Title = master.Title,
                Description = master.Description,
                StartDate = start,
                EndDate = start + duration,
                Color = master.Color,
                Resource = master.Resource,
                Data = master.Data,
                Attendees = [.. master.Attendees],
                IsAllDay = master.IsAllDay,
                CssClass = master.CssClass,
                // The series master is the editable thing; an occurrence is a projection of it.
                IsReadOnly = true
            };

            if (++emitted >= MaxOccurrencesPerSeries)
                break;
        }
    }

    /// <summary>
    /// The dates a series occurs on, ascending, from its start until <see cref="BitFullCalendarRecurrence.Count"/>
    /// is exhausted, <see cref="BitFullCalendarRecurrence.Until"/> is passed, or the walk runs past
    /// <paramref name="hardEnd"/>.
    /// <para>
    /// A series with no <c>Count</c> has no position to preserve, so the walk skips straight to
    /// <paramref name="windowStart"/> - a daily series started years ago would otherwise spend its
    /// whole budget on occurrences nobody can see. A counted series is walked from its start, because
    /// there the position is what decides where it ends.
    /// </para>
    /// </summary>
    private static IEnumerable<DateTime> EnumerateOccurrenceDates(
        DateTime seriesStart,
        BitFullCalendarRecurrence rule,
        DateTime windowStart,
        DateTime hardEnd)
    {
        var until = rule.Until?.Date;
        if (rule.Count is { } declared && declared <= 0)
            yield break;

        var remaining = rule.Count is { } count ? Math.Min(count, MaxOccurrencesPerSeries) : MaxOccurrencesPerSeries;
        var interval = Math.Max(1, rule.Interval);
        // Only an open-ended series may skip ahead; a counted one has to be walked from its start.
        var skipAhead = rule.Count is null;
        var emitted = 0;

        bool Accept(DateTime date) => (until is null || date <= until) && date <= hardEnd;

        if (rule.Frequency is BitFullCalendarRecurrenceFrequency.Weekly)
        {
            var weekDays = rule.ResolveWeekDays(seriesStart);
            // Anchor on the week the series starts in so "every other week" counts from there.
            var weekAnchor = seriesStart.AddDays(-((int)seriesStart.DayOfWeek));
            var firstWeek = skipAhead ? StepsToReach(weekAnchor, windowStart, 7L * interval) : 0;

            for (var week = firstWeek; emitted < remaining; week++)
            {
                if (TryAddDays(weekAnchor, (long)week * 7 * interval) is not { } weekStart)
                    yield break;
                if (weekStart > hardEnd.AddDays(7))
                    yield break;

                foreach (var day in weekDays)
                {
                    var date = weekStart.AddDays((int)day);
                    if (date < seriesStart)
                        continue;
                    if (!Accept(date))
                        yield break;

                    yield return date;
                    if (++emitted >= remaining)
                        yield break;
                }
            }

            yield break;
        }

        var firstStep = skipAhead
            ? rule.Frequency switch
            {
                BitFullCalendarRecurrenceFrequency.Daily => StepsToReach(seriesStart, windowStart, interval),
                BitFullCalendarRecurrenceFrequency.Monthly => MonthStepsToReach(seriesStart, windowStart, interval),
                BitFullCalendarRecurrenceFrequency.Yearly => Math.Max(0, (windowStart.Year - seriesStart.Year) / interval),
                _ => 0
            }
            : 0;

        for (var step = firstStep; emitted < remaining; step++)
        {
            var offset = (long)step * interval;
            DateTime? date = rule.Frequency switch
            {
                BitFullCalendarRecurrenceFrequency.Daily => TryAddDays(seriesStart, offset),
                BitFullCalendarRecurrenceFrequency.Monthly => AddMonthsKeepingDay(seriesStart, offset),
                BitFullCalendarRecurrenceFrequency.Yearly => AddYearsKeepingDay(seriesStart, offset),
                _ => null
            };

            if (date is null)
            {
                // Either the walk ran out of representable dates, or a month (or year) too short for
                // the series' day of the month came up - which is skipped rather than shifted onto a
                // neighbouring day, so a "31st" series never lands on the 1st. The walk still has to
                // terminate, hence the same hard-end guard an accepted date gets.
                var probe = rule.Frequency switch
                {
                    BitFullCalendarRecurrenceFrequency.Monthly => TryAddMonths(seriesStart, offset),
                    BitFullCalendarRecurrenceFrequency.Yearly => TryAddMonths(seriesStart, offset * 12),
                    _ => null
                };
                if (probe is null || probe > hardEnd || (until is not null && probe > until))
                    yield break;
                continue;
            }

            if (!Accept(date.Value))
                yield break;

            yield return date.Value;
            emitted++;
        }
    }

    /// <summary>
    /// How many steps of <paramref name="stepDays"/> days it takes to reach <paramref name="target"/>
    /// from <paramref name="start"/>, rounded down and never below zero.
    /// </summary>
    private static int StepsToReach(DateTime start, DateTime target, long stepDays)
    {
        if (target <= start || stepDays <= 0)
            return 0;

        var days = (target.Date - start.Date).TotalDays;
        var steps = Math.Floor(days / stepDays);
        return steps >= int.MaxValue ? int.MaxValue : (int)Math.Max(0, steps);
    }

    /// <summary>Same as <see cref="StepsToReach"/>, counted in whole months.</summary>
    private static int MonthStepsToReach(DateTime start, DateTime target, int intervalMonths)
    {
        if (target <= start || intervalMonths <= 0)
            return 0;

        var months = ((target.Year - start.Year) * 12) + target.Month - start.Month;
        return Math.Max(0, months / intervalMonths);
    }

    /// <summary>
    /// <paramref name="date"/> plus <paramref name="days"/> days, or <c>null</c> when the result
    /// would fall outside the representable range.
    /// </summary>
    private static DateTime? TryAddDays(DateTime date, long days)
    {
        try
        {
            return date.AddDays(days);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// <paramref name="date"/> plus <paramref name="months"/> months, or <c>null</c> when the result
    /// would fall outside the representable range.
    /// </summary>
    private static DateTime? TryAddMonths(DateTime date, long months)
    {
        if (months is > int.MaxValue or < int.MinValue)
            return null;

        try
        {
            return date.AddMonths((int)months);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// <paramref name="date"/> plus <paramref name="months"/> months, keeping the same day of the
    /// month, or <c>null</c> when the target month has no such day (or is out of range).
    /// </summary>
    private static DateTime? AddMonthsKeepingDay(DateTime date, long months)
    {
        // AddMonths clamps to the last day of a short month; the series should skip it instead.
        return TryAddMonths(date, months) is { } shifted && shifted.Day == date.Day ? shifted.Date : null;
    }

    /// <summary>
    /// <paramref name="date"/> plus <paramref name="years"/> years, keeping the same month and day,
    /// or <c>null</c> for a 29 February series in a non-leap year (or when out of range).
    /// </summary>
    private static DateTime? AddYearsKeepingDay(DateTime date, long years)
    {
        return TryAddMonths(date, years * 12) is { } shifted && shifted.Day == date.Day && shifted.Month == date.Month
            ? shifted.Date
            : null;
    }

    // -- Grid shape: hidden days, first day of week, week numbers ------------------------------

    /// <summary>
    /// Canonicalizes the hidden-day set: duplicates and undefined values are dropped, and a set that
    /// would leave no visible day at all is ignored (returns an empty set) rather than rendering an
    /// empty grid.
    /// </summary>
    public static IReadOnlySet<DayOfWeek> NormalizeHiddenDays(IReadOnlyList<DayOfWeek>? hiddenDays)
    {
        if (hiddenDays is null || hiddenDays.Count == 0)
            return _noHiddenDays;

        var set = new HashSet<DayOfWeek>();
        foreach (var day in hiddenDays)
        {
            if (Enum.IsDefined(day))
                set.Add(day);
        }

        // Hiding every day leaves nothing to render, so the whole set is refused.
        return set.Count is 0 or >= 7 ? _noHiddenDays : set;
    }

    private static readonly IReadOnlySet<DayOfWeek> _noHiddenDays = new HashSet<DayOfWeek>();

    /// <summary>The weekdays business hours run on when the consumer named none: Monday to Friday.</summary>
    public static readonly IReadOnlyList<DayOfWeek> DefaultBusinessDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    /// <summary>
    /// Canonicalizes the business-day set: <c>null</c> falls back to <see cref="DefaultBusinessDays"/>,
    /// duplicates and undefined values are dropped, and an empty list is kept as "no business day"
    /// (unlike the hidden days, a week with no business day is a legitimate configuration).
    /// </summary>
    public static IReadOnlySet<DayOfWeek> NormalizeBusinessDays(IReadOnlyList<DayOfWeek>? businessDays)
    {
        var source = businessDays ?? DefaultBusinessDays;
        var set = new HashSet<DayOfWeek>();
        foreach (var day in source)
        {
            if (Enum.IsDefined(day))
                set.Add(day);
        }
        return set;
    }

    /// <summary>
    /// Canonicalizes the business-hour window the same way <see cref="NormalizeVisibleHours"/>
    /// canonicalizes the rendered one, so a start at or past the end still leaves an hour of business.
    /// </summary>
    public static (int Start, int End) NormalizeBusinessHours(int startHour, int endHour)
        => NormalizeVisibleHours(startHour, endHour);

    /// <summary>The day the week starts on: the explicit override when supplied, else the culture's.</summary>
    public static DayOfWeek ResolveFirstDayOfWeek(CultureInfo? culture = null, DayOfWeek? firstDayOfWeek = null)
        => firstDayOfWeek is { } day && Enum.IsDefined(day)
            ? day
            : (culture ?? CultureInfo.CurrentUICulture).DateTimeFormat.FirstDayOfWeek;

    /// <summary>Number of weekday columns a date grid renders once the hidden days are removed.</summary>
    public static int GetVisibleWeekDayCount(IReadOnlyList<DayOfWeek>? hiddenDays)
        => 7 - NormalizeHiddenDays(hiddenDays).Count;

    /// <summary>
    /// ISO-8601 week number of the supplied date. Week numbers are intentionally ISO rather than
    /// culture-derived so the same date always reports the same number regardless of the calendar
    /// the grid renders in.
    /// </summary>
    public static int GetWeekNumber(DateTime date) => ISOWeek.GetWeekOfYear(date.Date);

    /// <summary>
    /// The week number a grid row is labelled with, for any day the row contains. ISO weeks run
    /// Monday to Sunday, so in a Sunday-start culture a row's first day is the last day of the
    /// PREVIOUS ISO week and reading the number off it labels every row a week low. The row is
    /// labelled by its mid-week day instead - the day six of its seven days share a week with -
    /// which is the row's own start in a Monday-start culture, leaving those unchanged.
    /// </summary>
    public static int GetWeekNumberForRow(DateTime dayInRow, CultureInfo? culture = null, DayOfWeek? firstDayOfWeek = null)
        => GetWeekNumber(StartOfWeek(dayInRow, culture, firstDayOfWeek).AddDays(3));

    // -- Culture-aware: era-safe month/year anchors ------------------------------

    /// <summary>
    /// First day of the cultural month containing <paramref name="date"/>, derived with pure date
    /// arithmetic so the result always stays in the source date's own era. Reconstructing it with
    /// <c>Calendar.ToDateTime(year, month, 1, ...)</c> resolves against the current era instead,
    /// which lands on the wrong date (or throws) on era-based calendars such as the Japanese one.
    /// </summary>
    public static DateTime StartOfCulturalMonth(DateTime date, CultureInfo? culture = null)
    {
        var cal = (culture ?? CultureInfo.CurrentUICulture).Calendar;
        return date.Date.AddDays(1 - cal.GetDayOfMonth(date));
    }

    /// <summary>Last day of the cultural month containing <paramref name="date"/> (era-safe).</summary>
    public static DateTime EndOfCulturalMonth(DateTime date, CultureInfo? culture = null)
    {
        var cal = (culture ?? CultureInfo.CurrentUICulture).Calendar;
        return cal.AddMonths(StartOfCulturalMonth(date, culture), 1).AddDays(-1);
    }

    /// <summary>
    /// First day of the cultural year containing <paramref name="date"/>. Walks month by month from
    /// the date's own month so partial era years (where the era begins mid-year) resolve to the
    /// first month that actually exists, instead of assuming month 1 is materializable.
    /// </summary>
    public static DateTime StartOfCulturalYear(DateTime date, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var cal = culture.Calendar;
        var year = cal.GetYear(date);
        var cursor = StartOfCulturalMonth(date, culture);
        // Bounded by the months in a year; the guard stops the walk as soon as stepping back would
        // leave the year (or the era's first month is reached).
        for (var i = 0; i < 24; i++)
        {
            var previous = cal.AddMonths(cursor, -1);
            if (previous >= cursor || cal.GetYear(previous) != year)
                break;
            cursor = previous;
        }
        return cursor;
    }

    /// <summary>Last day of the cultural year containing <paramref name="date"/> (era-safe).</summary>
    public static DateTime EndOfCulturalYear(DateTime date, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var cal = culture.Calendar;
        var year = cal.GetYear(date);
        var cursor = StartOfCulturalMonth(date, culture);
        for (var i = 0; i < 24; i++)
        {
            var next = cal.AddMonths(cursor, 1);
            if (next <= cursor || cal.GetYear(next) != year)
                break;
            cursor = next;
        }
        return cal.AddMonths(cursor, 1).AddDays(-1);
    }

    // -- Culture-aware: Range text ------------------------------

    public static string RangeText(BitFullCalendarView view, DateTime date, CultureInfo? culture = null, DayOfWeek? firstDayOfWeek = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var cal = culture.Calendar;

        switch (view)
        {
            case BitFullCalendarView.Month:
            case BitFullCalendarView.Agenda:
            {
                // Use the culture's YearMonth pattern so the field ordering (month-before-year vs
                // year-before-month) follows the culture's calendar instead of being hard-coded.
                return date.ToString("Y", culture);
            }
            case BitFullCalendarView.Week:
            {
                var start = StartOfWeek(date, culture, firstDayOfWeek);
                var end = start.AddDays(6);
                return $"{FormatCultureDate(start, culture)} - {FormatCultureDate(end, culture)}";
            }
            case BitFullCalendarView.Day:
                return FormatCultureDate(date, culture);
            case BitFullCalendarView.Year:
            {
                int y = cal.GetYear(date);
                return y.ToString(culture);
            }
            default:
                return "Error";
        }
    }

    /// <summary>Formats a date with an abbreviated month using the culture's field ordering and calendar.</summary>
    public static string FormatCultureDate(DateTime date, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var dtf = culture.DateTimeFormat;

        // Honor the culture's field ordering (and calendar) by deriving the date pattern from the
        // culture's long-date pattern instead of hard-coding "Mon d, yyyy": drop the weekday token
        // and prefer the abbreviated month so the label stays compact across cultures (e.g.
        // day-month-year in much of Europe, year-month-day in East Asian calendars). Falls back to
        // the previous manual format if the derived pattern turns out to be unusable.
        try
        {
            var pattern = BuildAbbreviatedDatePattern(dtf.LongDatePattern);
            if (!string.IsNullOrWhiteSpace(pattern))
                return date.ToString(pattern, culture);
        }
        catch (FormatException)
        {
            // Derived pattern was not a valid format string for this culture; fall through.
        }

        var cal = culture.Calendar;
        int y = cal.GetYear(date);
        int m = cal.GetMonth(date);
        int d = cal.GetDayOfMonth(date);
        return $"{dtf.GetAbbreviatedMonthName(m)} {d.ToString(culture)}, {y.ToString(culture)}";
    }

    /// <summary>
    /// Derives a compact date pattern from a culture's long-date pattern by removing the day-of-week
    /// token and using the abbreviated month form, while preserving the culture's field ordering.
    /// </summary>
    private static string BuildAbbreviatedDatePattern(string longDatePattern)
    {
        if (string.IsNullOrWhiteSpace(longDatePattern))
            return string.Empty;

        // Remove the day-of-week token (ddd / dddd), switch to the abbreviated month, then clean up
        // any separators left behind (whitespace and comma variants, including the Arabic comma).
        var p = System.Text.RegularExpressions.Regex.Replace(longDatePattern, "d{3,4}", "");
        p = p.Replace("MMMM", "MMM");
        p = System.Text.RegularExpressions.Regex.Replace(p, "^[\\s,\u060C]+", "");
        p = System.Text.RegularExpressions.Regex.Replace(p, "[\\s,\u060C]+$", "");
        p = System.Text.RegularExpressions.Regex.Replace(p, "\\s{2,}", " ");
        return p.Trim();
    }

    // -- Culture-aware: Navigation ------------------------------

    public static DateTime NavigateDate(
        DateTime date,
        BitFullCalendarView view,
        bool forward,
        CultureInfo? culture = null,
        IReadOnlyList<DayOfWeek>? hiddenDays = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var cal = culture.Calendar;
        int delta = forward ? 1 : -1;
        return view switch
        {
            BitFullCalendarView.Month  => cal.AddMonths(date, delta),
            BitFullCalendarView.Week   => date.AddDays(forward ? 7 : -7),
            // Day-by-day navigation walks over the hidden weekdays instead of landing on a day the
            // grid does not render.
            BitFullCalendarView.Day    => NextVisibleDay(date.AddDays(delta), delta, hiddenDays),
            BitFullCalendarView.Year   => cal.AddYears(date, delta),
            BitFullCalendarView.Agenda => cal.AddMonths(date, delta),
            _                   => date
        };
    }

    private static DateTime NextVisibleDay(DateTime date, int step, IReadOnlyList<DayOfWeek>? hiddenDays)
    {
        var hidden = NormalizeHiddenDays(hiddenDays);
        if (hidden.Count == 0)
            return date;

        // At most six steps are needed: NormalizeHiddenDays never leaves all seven days hidden.
        for (var i = 0; i < 7 && hidden.Contains(date.DayOfWeek); i++)
            date = date.AddDays(step);

        return date;
    }

    // -- Time grid: visible hour window and slots ------------------------------

    /// <summary>
    /// Canonicalizes the visible-hour window of the day/week time grids: the start is clamped to
    /// 0-23, the end to 1-24, and an end at or before the start is corrected to one hour past it so
    /// the grid always renders at least one hour.
    /// </summary>
    public static (int Start, int End) NormalizeVisibleHours(int startHour, int endHour)
    {
        var start = Math.Clamp(startHour, 0, 23);
        var end = Math.Clamp(endHour, 1, 24);
        if (end <= start)
            end = start + 1;
        return (start, end);
    }

    /// <summary>Number of slots one hour is divided into by the supplied slot duration.</summary>
    public static int GetSlotsPerHour(int slotDurationMinutes)
        => Math.Max(1, 60 / Math.Clamp(slotDurationMinutes, 1, 60));

    /// <summary>
    /// The minute offsets of every slot inside one hour, ascending
    /// (for example <c>[0, 30]</c> for a 30-minute slot duration).
    /// </summary>
    public static int[] GetSlotMinutes(int slotDurationMinutes)
    {
        var duration = Math.Clamp(slotDurationMinutes, 1, 60);
        var count = GetSlotsPerHour(duration);
        var result = new int[count];
        for (var i = 0; i < count; i++)
            result[i] = i * duration;
        return result;
    }

    // -- Culture-aware: Week helpers ------------------------------

    public static DateTime StartOfWeek(DateTime date, CultureInfo? culture = null, DayOfWeek? firstDayOfWeek = null)
    {
        return StartOfWeek(date, ResolveFirstDayOfWeek(culture, firstDayOfWeek));
    }

    public static DateTime StartOfWeek(DateTime date, DayOfWeek startDay)
    {
        int diff = (7 + (date.DayOfWeek - startDay)) % 7;
        return date.Date.AddDays(-diff);
    }

    /// <summary>
    /// The dates of the week containing <paramref name="date"/>, in display order and with the
    /// hidden weekdays removed. The full seven dates are returned when nothing is hidden.
    /// </summary>
    public static DateTime[] GetWeekDates(
        DateTime date,
        CultureInfo? culture = null,
        DayOfWeek? firstDayOfWeek = null,
        IReadOnlyList<DayOfWeek>? hiddenDays = null)
    {
        var start = StartOfWeek(date, culture, firstDayOfWeek);
        var hidden = NormalizeHiddenDays(hiddenDays);
        return Enumerable.Range(0, 7)
            .Select(i => start.AddDays(i))
            .Where(d => !hidden.Contains(d.DayOfWeek))
            .ToArray();
    }

    // -- Culture-aware: Weekday header names ------------------------------

    /// <summary>
    /// The shortest day-name strings (1 char) in display order, starting from the resolved first day
    /// of the week and with the hidden weekdays removed.
    /// </summary>
    public static string[] GetWeekDayHeaders(
        CultureInfo? culture = null,
        DayOfWeek? firstDayOfWeek = null,
        IReadOnlyList<DayOfWeek>? hiddenDays = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var dtf = culture.DateTimeFormat;
        var first = (int)ResolveFirstDayOfWeek(culture, firstDayOfWeek);
        var hidden = NormalizeHiddenDays(hiddenDays);
        return Enumerable.Range(0, 7)
            .Select(i => (DayOfWeek)((first + i) % 7))
            .Where(d => !hidden.Contains(d))
            .Select(dtf.GetShortestDayName)
            .ToArray();
    }

    /// <summary>
    /// The abbreviated day-name strings (2-3 chars) in display order, starting from the resolved
    /// first day of the week and with the hidden weekdays removed.
    /// </summary>
    public static string[] GetAbbreviatedWeekDayHeaders(
        CultureInfo? culture = null,
        DayOfWeek? firstDayOfWeek = null,
        IReadOnlyList<DayOfWeek>? hiddenDays = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var dtf = culture.DateTimeFormat;
        var first = (int)ResolveFirstDayOfWeek(culture, firstDayOfWeek);
        var hidden = NormalizeHiddenDays(hiddenDays);
        return Enumerable.Range(0, 7)
            .Select(i => (DayOfWeek)((first + i) % 7))
            .Where(d => !hidden.Contains(d))
            .Select(dtf.GetAbbreviatedDayName)
            .ToArray();
    }

    // -- Culture-aware: Calendar grid cells ------------------------------

    /// <summary>
    /// The month grid cells for the month containing <paramref name="selectedDate"/>, in display
    /// order. Hidden weekdays are dropped from every row, so the result always divides evenly by
    /// <see cref="GetVisibleWeekDayCount"/>.
    /// </summary>
    public static List<BitFullCalendarCell> GetCalendarCells(
        DateTime selectedDate,
        CultureInfo? culture,
        DayOfWeek? firstDayOfWeek,
        IReadOnlyList<DayOfWeek>? hiddenDays,
        bool fixedWeekCount = false)
    {
        var cells = GetCalendarCells(selectedDate, culture, firstDayOfWeek, fixedWeekCount);
        var hidden = NormalizeHiddenDays(hiddenDays);
        if (hidden.Count == 0)
            return cells;

        // Every row spans the same seven weekdays, so removing a weekday removes one column from
        // each row and the grid stays rectangular.
        return cells.Where(c => !hidden.Contains(c.Date.DayOfWeek)).ToList();
    }

    public static List<BitFullCalendarCell> GetCalendarCells(
        DateTime selectedDate,
        CultureInfo? culture = null,
        DayOfWeek? firstDayOfWeek = null,
        bool fixedWeekCount = false)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var cal = culture.Calendar;
        var dtf = culture.DateTimeFormat;

        // Anchor on the first day of the cultural month using pure date arithmetic derived from the
        // selected date itself, then walk adjacent days with AddDays / AddMonths. This keeps every
        // reconstructed date in the selected date's own era (e.g. JapaneseCalendar) instead of
        // re-materializing year/month numbers against CurrentEra, and avoids manual +/-1 month math
        // on GetYear/GetMonth results that can resolve the wrong era or an invalid day.
        int culturalDay = cal.GetDayOfMonth(selectedDate);
        DateTime firstDay = selectedDate.Date.AddDays(1 - culturalDay);

        // Days in this month = distance to the first day of the next month (era-preserving transition).
        DateTime nextMonthFirstDay = cal.AddMonths(firstDay, 1);
        int daysInMonth = (int)(nextMonthFirstDay.Date - firstDay.Date).TotalDays;

        // Leading blank cells (days from the previous cultural month)
        int firstDow      = (int)cal.GetDayOfWeek(firstDay);
        int culturalFirst = (int)ResolveFirstDayOfWeek(culture, firstDayOfWeek);
        int leadingDays   = (firstDow - culturalFirst + 7) % 7;

        var cells = new List<BitFullCalendarCell>();

        for (int i = leadingDays; i > 0; i--)
        {
            DateTime date = firstDay.AddDays(-i);
            cells.Add(new BitFullCalendarCell { Day = cal.GetDayOfMonth(date), CurrentMonth = false, Date = date });
        }

        for (int i = 0; i < daysInMonth; i++)
        {
            DateTime date = firstDay.AddDays(i);
            cells.Add(new BitFullCalendarCell { Day = cal.GetDayOfMonth(date), CurrentMonth = true, Date = date });
        }

        int totalDays = leadingDays + daysInMonth;
        int trailing  = (7 - (totalDays % 7)) % 7;

        // A fixed week count keeps the grid the same height across months, so the trailing run is
        // padded out to the full six rows instead of stopping at the month's own last week.
        if (fixedWeekCount)
            trailing = Math.Max(trailing, 42 - totalDays);

        for (int i = 0; i < trailing; i++)
        {
            DateTime date = nextMonthFirstDay.AddDays(i);
            cells.Add(new BitFullCalendarCell { Day = cal.GetDayOfMonth(date), CurrentMonth = false, Date = date });
        }

        return cells;
    }

    // -- Culture-aware: Day-of-month display ------------------------------

    public static int GetCulturalDayOfMonth(DateTime date, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        return culture.Calendar.GetDayOfMonth(date);
    }

    // -- Culture-aware: Events for year ------------------------------

    public static List<BitFullCalendarEvent> GetEventsForYear(List<BitFullCalendarEvent> events, DateTime date, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        // Era-safe anchors: reconstructing the year from its number resolves against the current era,
        // which lands on the wrong dates (or throws) on era-based calendars.
        var yearStart = StartOfCulturalYear(date, culture);
        var yearEnd = EndOfCulturalYear(date, culture).Date.AddDays(1).AddTicks(-1);
        return events.Where(ev => OverlapsPeriod(ev, yearStart, yearEnd)).ToList();
    }

    public static string FormatTime(DateTime date, bool use24Hour, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        return use24Hour
            ? date.ToString("HH:mm", culture)
            : date.ToString("h:mm tt", culture);
    }

    /// <summary>
    /// Builds a human-friendly tooltip string for an event. Includes the title, the time range,
    /// and (when present and not redundant with the title) the description. Used by event cards
    /// where layout space may hide most of the visual content.
    /// </summary>
    public static string BuildEventTooltip(
        BitFullCalendarEvent ev, bool use24Hour, CultureInfo? culture = null, string? allDayLabel = null)
    {
        if (ev is null)
            return string.Empty;

        var title = string.IsNullOrWhiteSpace(ev.Title) ? string.Empty : ev.Title.Trim();
        var time = BuildEventRangeText(ev, use24Hour, culture, allDayLabel);

        var lines = new List<string>(3);
        if (!string.IsNullOrEmpty(title))
            lines.Add(title);
        lines.Add(time);

        if (!string.IsNullOrWhiteSpace(ev.Description))
        {
            var description = ev.Description.Trim();
            if (!string.Equals(description, title, StringComparison.Ordinal))
                lines.Add(description);
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// The span an event covers, written the way the event is actually scheduled: a clock range for
    /// a timed event inside one day, the dates as well once it crosses one, and the covered dates
    /// plus <paramref name="allDayLabel"/> for an all-day event, which has no clock time to show.
    /// </summary>
    public static string BuildEventRangeText(
        BitFullCalendarEvent ev, bool use24Hour, CultureInfo? culture = null, string? allDayLabel = null)
    {
        culture ??= CultureInfo.CurrentUICulture;

        if (ev.IsAllDay)
        {
            var lastDate = GetInclusiveEndDate(ev);
            var dates = lastDate <= ev.StartDate.Date
                ? FormatCultureDate(ev.StartDate, culture)
                : $"{FormatCultureDate(ev.StartDate, culture)} - {FormatCultureDate(lastDate, culture)}";
            return string.IsNullOrEmpty(allDayLabel) ? dates : $"{dates} · {allDayLabel}";
        }

        var start = FormatTime(ev.StartDate, use24Hour, culture);
        var end = FormatTime(ev.EndDate, use24Hour, culture);
        // A timed event that crosses midnight would otherwise read as "23:00 - 01:00" with no hint
        // of which day either end falls on, so the dates join the clock times once it spans more.
        if (ev.IsMultiDay)
            return $"{FormatCultureDate(ev.StartDate, culture)} {start} - {FormatCultureDate(ev.EndDate, culture)} {end}";

        return $"{start} - {end}";
    }

    public static string FormatHourLabel(int hour, bool use24Hour, CultureInfo? culture = null)
    {
        var dt = DateTime.Today.AddHours(hour);
        culture ??= CultureInfo.CurrentUICulture;
        return use24Hour
            ? dt.ToString("HH:00", culture)
            : dt.ToString("h tt", culture);
    }

    /// <summary>
    /// Computes horizontal pixel position and width for an event placed on a resource timeline row.
    /// The event range is clipped to the visible day so events that span past midnight stay
    /// inside the row. Returns (LeftPx, WidthPx) or null when the event has no overlap with the day.
    /// </summary>
    public static (double LeftPx, double WidthPx)? GetTimelineBlockPosition(
        BitFullCalendarEvent ev,
        DateTime day,
        int hourWidthPx = TimelineHourWidthPx,
        int visibleStartHour = 0,
        int visibleEndHour = 24)
    {
        var (startHour, endHour) = NormalizeVisibleHours(visibleStartHour, visibleEndHour);
        // Clip to the visible hour window, not to the whole calendar day, so a row that renders only
        // the working hours never places a block outside its own columns.
        var dayStart = day.Date.AddHours(startHour);
        var dayEnd = day.Date.AddHours(endHour);

        var clippedStart = ev.StartDate < dayStart ? dayStart : ev.StartDate;
        var clippedEnd = ev.EndDate > dayEnd ? dayEnd : ev.EndDate;
        if (clippedEnd <= clippedStart)
            return null;

        var pxPerMinute = hourWidthPx / 60.0;
        var leftMinutes = (clippedStart - dayStart).TotalMinutes;
        var widthMinutes = (clippedEnd - clippedStart).TotalMinutes;

        return (leftMinutes * pxPerMinute, widthMinutes * pxPerMinute);
    }

    /// <summary>
    /// Groups events for a single day by their <see cref="BitFullCalendarEvent.Resource"/> id.
    /// Within each resource, events are arranged into non-overlapping lanes so overlapping events
    /// stack vertically inside the same row (similar to <see cref="GroupEvents"/> for day/week).
    /// Events with no resource id (or an id not in <paramref name="resourceIds"/>) are placed
    /// under <paramref name="unassignedKey"/>.
    /// </summary>
    public static Dictionary<string, List<List<BitFullCalendarEvent>>> GroupEventsByResourceForDay(
        List<BitFullCalendarEvent> events,
        DateTime day,
        IEnumerable<string> resourceIds,
        string unassignedKey)
    {
        var dayStart = day.Date;
        var dayEnd = dayStart.AddDays(1);

        var keyed = new Dictionary<string, List<BitFullCalendarEvent>>(StringComparer.Ordinal);
        foreach (var id in resourceIds)
        {
            if (!keyed.ContainsKey(id))
                keyed[id] = [];
        }
        keyed[unassignedKey] = [];

        var validIds = new HashSet<string>(keyed.Keys, StringComparer.Ordinal);

        foreach (var ev in events)
        {
            if (ev.StartDate >= dayEnd)
                continue;
            // Keep zero-length markers (StartDate == EndDate, e.g. a 00:00 all-day marker) that fall
            // on/after dayStart, matching OverlapsPeriod/GetEventsForDay and the month grouping; the
            // strict EndDate <= dayStart check would otherwise drop a midnight marker sitting at the
            // day start instead of laning it.
            if (ev.EndDate <= dayStart && !(ev.StartDate == ev.EndDate && ev.StartDate >= dayStart))
                continue;

            var key = ev.Resource is { Length: > 0 } r && validIds.Contains(r) ? r : unassignedKey;
            keyed[key].Add(ev);
        }

        return keyed.ToDictionary(
            kv => kv.Key,
            kv => GroupEvents(kv.Value),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Groups events that overlap a calendar month by their <see cref="BitFullCalendarEvent.Resource"/>
    /// id. Within each resource the events are arranged into non-overlapping lanes so multi-day events
    /// stack vertically inside the resource row. Events with no resource id (or an id not in
    /// <paramref name="resourceIds"/>) are placed under <paramref name="unassignedKey"/>.
    /// </summary>
    public static Dictionary<string, List<List<BitFullCalendarEvent>>> GroupEventsByResourceForMonth(
        List<BitFullCalendarEvent> events,
        DateTime monthStart,
        int daysInMonth,
        IEnumerable<string> resourceIds,
        string unassignedKey)
    {
        var monthEnd = monthStart.AddDays(daysInMonth);

        var keyed = new Dictionary<string, List<BitFullCalendarEvent>>(StringComparer.Ordinal);
        foreach (var id in resourceIds)
        {
            if (!keyed.ContainsKey(id))
                keyed[id] = [];
        }
        keyed[unassignedKey] = [];

        var validIds = new HashSet<string>(keyed.Keys, StringComparer.Ordinal);

        foreach (var ev in events)
        {
            if (ev.StartDate >= monthEnd)
                continue;
            // Keep zero-length markers (StartDate == EndDate, e.g. a 00:00 all-day marker) that fall
            // on/after monthStart, matching OverlapsPeriod/GetInclusiveEndDate; the strict
            // EndDate <= monthStart check would otherwise drop a midnight marker sitting at the month
            // start instead of laning it.
            if (ev.EndDate <= monthStart && !(ev.StartDate == ev.EndDate && ev.StartDate >= monthStart))
                continue;

            var key = ev.Resource is { Length: > 0 } r && validIds.Contains(r) ? r : unassignedKey;
            keyed[key].Add(ev);
        }

        return keyed.ToDictionary(
            kv => kv.Key,
            kv => GroupEventsByDayRange(kv.Value, monthStart, monthEnd),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Day-range variant of <see cref="GroupEvents"/>: events are sorted by start, then placed in the
    /// first lane whose tail event ends on or before the candidate's start day. Used by the timeline
    /// month view where the granularity is one column per day.
    /// </summary>
    private static List<List<BitFullCalendarEvent>> GroupEventsByDayRange(
        List<BitFullCalendarEvent> events, DateTime rangeStart, DateTime rangeEnd)
    {
        var sorted = events.OrderBy(e => e.StartDate).ThenByDescending(e => e.EndDate).ToList();
        var lanes = new List<List<BitFullCalendarEvent>>();

        DateTime ClipStartDate(BitFullCalendarEvent e) => (e.StartDate < rangeStart ? rangeStart : e.StartDate).Date;
        DateTime ClipEndDate(BitFullCalendarEvent e)
        {
            // Zero-length markers (StartDate == EndDate) occupy their single start day - don't shift
            // a 00:00 end back before the start, which would mis-lane the marker (negative span).
            if (e.StartDate == e.EndDate)
                return ClipStartDate(e);
            var end = e.EndDate > rangeEnd ? rangeEnd : e.EndDate;
            // Treat 00:00 boundary as ending the previous day (exclusive end).
            return end.TimeOfDay == TimeSpan.Zero ? end.Date.AddDays(-1) : end.Date;
        }

        foreach (var ev in sorted)
        {
            var s = ClipStartDate(ev);
            var placed = false;
            foreach (var lane in lanes)
            {
                if (s > ClipEndDate(lane[^1]))
                {
                    lane.Add(ev);
                    placed = true;
                    break;
                }
            }
            if (!placed)
                lanes.Add([ev]);
        }

        return lanes;
    }

    public static List<List<BitFullCalendarEvent>> GroupEvents(List<BitFullCalendarEvent> dayEvents)
    {
        var sorted = dayEvents.OrderBy(e => e.StartDate).ToList();
        var groups = new List<List<BitFullCalendarEvent>>();

        foreach (var ev in sorted)
        {
            bool placed = false;
            foreach (var group in groups)
            {
                if (ev.StartDate >= group[^1].EndDate)
                {
                    group.Add(ev);
                    placed = true;
                    break;
                }
            }
            if (!placed)
                groups.Add([ev]);
        }

        return groups;
    }

    /// <summary>
    /// How many hour rows separate the top of the time grid from the start of <paramref name="ev"/>.
    /// Expressed in hours rather than pixels so the caller can render it against
    /// <see cref="HourHeightVariableName"/> and keep the block aligned with the rows it sits on.
    /// </summary>
    public static double GetEventBlockOffsetHours(BitFullCalendarEvent ev, DateTime day, int visibleStartHour = 0)
    {
        // The grid's first rendered row is visibleStartHour, so a block is positioned relative to
        // that hour rather than to midnight.
        var gridStart = day.Date.AddHours(Math.Clamp(visibleStartHour, 0, 23));
        var eventStart = ev.StartDate < gridStart ? gridStart : ev.StartDate;
        return (eventStart - gridStart).TotalMinutes / 60.0;
    }

    /// <summary>
    /// Where a block sits inside the time grid's visible hour window - hours from the first rendered
    /// row - and how many hours tall it is once clipped to that window. Null when the event falls
    /// entirely outside the window, so a grid rendering only the working hours draws nothing for a
    /// 02:00 event instead of pinning it to the top row, and an event that merely reaches past the
    /// last rendered hour stops there. Mirrors <see cref="GetTimelineBlockPosition"/>, which applies
    /// the same rule along the timeline rows' horizontal axis.
    /// </summary>
    public static (double OffsetHours, double DurationHours)? GetEventBlockPlacement(
        BitFullCalendarEvent ev, DateTime day, int visibleStartHour = 0, int visibleEndHour = 24)
    {
        var (startHour, endHour) = NormalizeVisibleHours(visibleStartHour, visibleEndHour);
        var gridStart = day.Date.AddHours(startHour);
        var gridEnd = day.Date.AddHours(endHour);

        var clippedStart = ev.StartDate < gridStart ? gridStart : ev.StartDate;
        var clippedEnd = ev.EndDate > gridEnd ? gridEnd : ev.EndDate;
        if (clippedEnd <= clippedStart)
            return null;

        return ((clippedStart - gridStart).TotalMinutes / 60.0, (clippedEnd - clippedStart).TotalMinutes / 60.0);
    }

    public static (double TopPx, double WidthPercent, double LeftPercent) GetEventBlockStyle(
        BitFullCalendarEvent ev, DateTime day, int groupIndex, int groupSize, int visibleStartHour = 0)
    {
        double topPx = GetEventBlockOffsetHours(ev, day, visibleStartHour) * HourHeightPx;
        double width = 100.0 / groupSize;
        double left = groupIndex * width;
        return (topPx, width, left);
    }

    /// <summary>
    /// Days of the neighbouring months the month grid can render around the month itself: one week
    /// of leading days, and up to two trailing weeks once a fixed six-row grid pads a short month.
    /// </summary>
    private const int MonthGridPaddingDays = 14;

    private static (int Year, int Month, int Day) MonthGridDayKey(DateTime d)
    {
        d = d.Date;
        return (d.Year, d.Month, d.Day);
    }

    public static Dictionary<string, int> CalculateMonthEventPositions(
        List<BitFullCalendarEvent> multiDayEvents,
        List<BitFullCalendarEvent> singleDayEvents,
        DateTime selectedDate,
        CultureInfo? culture = null,
        int maxEventsPerDayCell = 3)
    {
        culture ??= CultureInfo.CurrentUICulture;
        // Era-safe month anchors (see StartOfCulturalMonth).
        // The grid also renders the days it borrows from the neighbouring months - up to a week of
        // them ahead, and up to two weeks behind with a fixed six-row grid - so the occupancy map
        // covers those too. Leaving them out gave a multi-day event no reserved row there, and every
        // such cell then picked its own free row, breaking the continuous bar across the run.
        DateTime monthStart = StartOfCulturalMonth(selectedDate, culture).AddDays(-MonthGridPaddingDays);
        DateTime monthEnd   = EndOfCulturalMonth(selectedDate, culture).AddDays(MonthGridPaddingDays);

        var slots = Math.Clamp(maxEventsPerDayCell, 1, 10);
        var eventPositions = new Dictionary<string, int>();
        var occupiedPositions = new Dictionary<(int Year, int Month, int Day), bool[]>();

        for (var d = monthStart; d <= monthEnd; d = d.AddDays(1))
            occupiedPositions[MonthGridDayKey(d)] = new bool[slots];

        var sorted = multiDayEvents
            .OrderByDescending(e => (e.EndDate - e.StartDate).TotalDays)
            .ThenBy(e => e.StartDate)
            .Concat(singleDayEvents.OrderBy(e => e.StartDate))
            .ToList();

        foreach (var ev in sorted)
        {
            var evStart = ev.StartDate.Date;
            // Treat a 00:00 end as ending the previous day (exclusive midnight), consistent with
            // IsSingleDay and GroupEventsByDayRange.
            var evEnd = GetInclusiveEndDate(ev);
            var rangeStart = evStart < monthStart ? monthStart : evStart;
            var rangeEnd = evEnd > monthEnd ? monthEnd : evEnd;

            var eventDays = new List<DateTime>();
            for (var d = rangeStart; d <= rangeEnd; d = d.AddDays(1))
                eventDays.Add(d);

            int position = -1;
            for (int i = 0; i < slots; i++)
            {
                if (eventDays.All(d =>
                {
                    var key = MonthGridDayKey(d);
                    return occupiedPositions.TryGetValue(key, out var daySlots) && !daySlots[i];
                }))
                {
                    position = i;
                    break;
                }
            }

            if (position != -1)
            {
                foreach (var d in eventDays)
                {
                    var key = MonthGridDayKey(d);
                    if (occupiedPositions.TryGetValue(key, out var daySlots))
                        daySlots[position] = true;
                }
                eventPositions[ev.Id] = position;
            }
        }

        return eventPositions;
    }

    public static List<(BitFullCalendarEvent Event, int Position, bool IsMultiDay)> GetMonthCellEvents(
        DateTime date, List<BitFullCalendarEvent> events, Dictionary<string, int> eventPositions, int maxEventsPerDayCell = 3)
    {
        var dayStart = date.Date;
        var eventsForDate = events.Where(ev =>
        {
            var s = ev.StartDate.Date;
            // Treat a 00:00 end as ending the previous day (exclusive midnight), consistent with
            // IsSingleDay and GroupEventsByDayRange, so an event ending at midnight doesn't show
            // up as a carry-over in the next day's cell.
            var e = GetInclusiveEndDate(ev);
            return (dayStart >= s && dayStart <= e) || s == dayStart || e == dayStart;
        }).ToList();

        var raw = eventsForDate
            .Select(ev => (
                Event: ev,
                Position: eventPositions.GetValueOrDefault(ev.Id, -1),
                IsMultiDay: ev.IsMultiDay
            ))
            .OrderByDescending(x => x.IsMultiDay)
            .ThenBy(x => x.Position < 0 ? 100 : x.Position)
            .ThenBy(x => x.Event.StartDate)
            .ToList();

        return AssignMonthCellDisplayRows(raw, Math.Clamp(maxEventsPerDayCell, 1, 10));
    }

    private static List<(BitFullCalendarEvent Event, int Position, bool IsMultiDay)> AssignMonthCellDisplayRows(
        List<(BitFullCalendarEvent Event, int Position, bool IsMultiDay)> raw, int slots)
    {
        var occupied = new bool[slots];
        var result = new List<(BitFullCalendarEvent Event, int Position, bool IsMultiDay)>();

        foreach (var x in raw)
        {
            var p = x.Position;
            if (p >= 0 && p < slots && !occupied[p])
            {
                occupied[p] = true;
                result.Add((x.Event, p, x.IsMultiDay));
                continue;
            }

            var free = -1;
            for (var i = 0; i < slots; i++)
            {
                if (!occupied[i])
                {
                    free = i;
                    break;
                }
            }

            if (free >= 0)
            {
                occupied[free] = true;
                result.Add((x.Event, free, x.IsMultiDay));
            }
            else
            {
                result.Add((x.Event, -1, x.IsMultiDay));
            }
        }

        return result
            .OrderByDescending(x => x.IsMultiDay)
            .ThenBy(x => x.Position < 0 ? 100 : x.Position)
            .ThenBy(x => x.Event.StartDate)
            .ToList();
    }

    /// <summary>
    /// Events touching the supplied date. With <paramref name="allDayRowOnly"/> the result is
    /// narrowed to what belongs in the all-day row above the time grid - the events that span more
    /// than one date plus the ones explicitly marked <see cref="BitFullCalendarEvent.IsAllDay"/>.
    /// </summary>
    public static List<BitFullCalendarEvent> GetEventsForDay(List<BitFullCalendarEvent> events, DateTime date, bool allDayRowOnly = false)
    {
        var target = date.Date;
        return events.Where(ev =>
        {
            var s = ev.StartDate.Date;
            // Treat a 00:00 end as ending the previous day (exclusive midnight), consistent with
            // IsSingleDay and GroupEventsByDayRange.
            var e = GetInclusiveEndDate(ev);
            if (allDayRowOnly)
                return ev.IsAllDayOrMultiDay && s <= target && e >= target;
            return s <= target && e >= target;
        }).ToList();
    }

    public static List<BitFullCalendarEvent> GetEventsForWeek(List<BitFullCalendarEvent> events, DateTime date, CultureInfo? culture = null, DayOfWeek? firstDayOfWeek = null)
    {
        var weekStart = StartOfWeek(date, culture, firstDayOfWeek);
        var weekEnd = weekStart.AddDays(6);
        return events.Where(ev => OverlapsPeriod(ev, weekStart, weekEnd)).ToList();
    }

    public static List<BitFullCalendarEvent> GetEventsForMonth(List<BitFullCalendarEvent> events, DateTime date, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        // Era-safe month anchors (see StartOfCulturalMonth).
        DateTime monthStart = StartOfCulturalMonth(date, culture);
        DateTime monthEnd   = EndOfCulturalMonth(date, culture);
        return events.Where(ev => OverlapsPeriod(ev, monthStart, monthEnd)).ToList();
    }

    /// <summary>
    /// Events overlapping the date range implied by the current view and selected date
    /// (used for attendee filters and similar "in this view" logic).
    /// </summary>
    public static List<BitFullCalendarEvent> GetEventsForView(
        List<BitFullCalendarEvent> events,
        BitFullCalendarView view,
        DateTime selectedDate,
        CultureInfo? culture = null,
        DayOfWeek? firstDayOfWeek = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        return view switch
        {
            BitFullCalendarView.Day => GetEventsForDay(events, selectedDate),
            BitFullCalendarView.Week => GetEventsForWeek(events, selectedDate, culture, firstDayOfWeek),
            BitFullCalendarView.Month => GetEventsForMonth(events, selectedDate, culture),
            BitFullCalendarView.Year => GetEventsForYear(events, selectedDate, culture),
            BitFullCalendarView.Agenda => GetEventsForMonth(events, selectedDate, culture),
            _ => events.ToList()
        };
    }

    /// <summary>
    /// Smallest time t' &gt;= <paramref name="dt"/> where (t' - t'.Date) is a whole multiple of
    /// <paramref name="intervalMinutes"/>. If <paramref name="dt"/> is already on such a boundary,
    /// returns <paramref name="dt"/> unchanged. Note: when the ceiling crosses midnight (for example
    /// 23:59 with a 30-minute interval), the result rolls over to 00:00 of the next calendar day.
    /// </summary>
    public static DateTime CeilToMinuteInterval(DateTime dt, int intervalMinutes)
    {
        if (intervalMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(intervalMinutes));

        var dayStart = dt.Date;
        var minutesSinceDay = (dt - dayStart).TotalMinutes;
        var slots = Math.Ceiling(minutesSinceDay / intervalMinutes);
        return dayStart.AddMinutes(slots * intervalMinutes);
    }

    /// <summary>
    /// Largest time t' &lt;= <paramref name="dt"/> on the same calendar day where
    /// (t' - t'.Date) is a whole multiple of <paramref name="intervalMinutes"/>.
    /// </summary>
    public static DateTime FloorToMinuteInterval(DateTime dt, int intervalMinutes)
    {
        if (intervalMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(intervalMinutes));

        var dayStart = dt.Date;
        var minutesSinceDay = (dt - dayStart).TotalMinutes;
        var slots = Math.Floor(minutesSinceDay / intervalMinutes);
        return dayStart.AddMinutes(slots * intervalMinutes);
    }

    /// <summary>Stable key for filtering events by attendee (Id preferred, else full name).</summary>
    public static string AttendeeFilterKey(BitFullCalendarAttendee a)
    {
        if (!string.IsNullOrWhiteSpace(a.Id))
            return "id:" + a.Id.Trim();
        if (!string.IsNullOrWhiteSpace(a.FullName))
            return "name:" + a.FullName.Trim().ToLowerInvariant();
        return "";
    }

    /// <summary>
    /// Offset of the "current time" indicator from the top of the time grid, measured from the
    /// grid's first rendered hour rather than from midnight.
    /// </summary>
    public static double GetCurrentTimeLineTopPx(int visibleStartHour = 0)
        => GetCurrentTimeLineOffsetHours(visibleStartHour) * HourHeightPx;

    /// <summary>
    /// Offset of the "current time" indicator from the top of the time grid, counted in hour rows
    /// from the grid's first rendered hour, so it can be rendered against
    /// <see cref="HourHeightVariableName"/> like the event blocks are.
    /// </summary>
    public static double GetCurrentTimeLineOffsetHours(int visibleStartHour = 0)
    {
        double minutes = DateTime.Now.TimeOfDay.TotalMinutes - (Math.Clamp(visibleStartHour, 0, 23) * 60);
        return minutes / 60.0;
    }

    /// <summary>
    /// True when the current clock time falls inside the grid's visible hour window, so the
    /// indicator has a row to sit on.
    /// </summary>
    public static bool IsNowInVisibleHours(int visibleStartHour, int visibleEndHour)
    {
        var (start, end) = NormalizeVisibleHours(visibleStartHour, visibleEndHour);
        var hours = DateTime.Now.TimeOfDay.TotalHours;
        return hours >= start && hours < end;
    }

    /// <summary>
    /// New event with only <see cref="BitFullCalendarEvent.StartDate"/> and <see cref="BitFullCalendarEvent.EndDate"/>
    /// set (same default duration as the built-in add dialog: 30 minutes from the slot start).
    /// </summary>
    public static BitFullCalendarEvent CreateDraftEventForTimeSlot(
        DateTime day,
        int hour,
        int startMinute = 0,
        int durationMinutes = 30)
    {
        if (hour is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(hour), hour, "Hour must be between 0 and 23.");
        if (startMinute is < 0 or > 59)
            throw new ArgumentOutOfRangeException(nameof(startMinute), startMinute, "Start minute must be between 0 and 59.");
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), durationMinutes, "Duration must be greater than zero.");

        var start = day.Date.AddHours(hour).AddMinutes(startMinute);
        return new BitFullCalendarEvent
        {
            StartDate = start,
            EndDate = start.AddMinutes(durationMinutes)
        };
    }

    /// <summary>
    /// Computes the inclusive start/end dates for the visible range of the given view.
    /// </summary>
    public static (DateTime Start, DateTime End) GetDateRange(
        BitFullCalendarView view, DateTime selectedDate, CultureInfo? culture = null, DayOfWeek? firstDayOfWeek = null)
    {
        culture ??= CultureInfo.CurrentUICulture;

        // Every anchor below is derived with era-safe arithmetic (StartOfCulturalMonth/Year) rather
        // than rebuilt from year/month numbers, which resolve against the current era.
        return view switch
        {
            BitFullCalendarView.Day => (selectedDate.Date, selectedDate.Date),
            BitFullCalendarView.Week =>
            (
                StartOfWeek(selectedDate, culture, firstDayOfWeek),
                StartOfWeek(selectedDate, culture, firstDayOfWeek).AddDays(6)
            ),
            BitFullCalendarView.Month or BitFullCalendarView.Agenda =>
            (
                StartOfCulturalMonth(selectedDate, culture),
                EndOfCulturalMonth(selectedDate, culture)
            ),
            BitFullCalendarView.Year =>
            (
                StartOfCulturalYear(selectedDate, culture),
                EndOfCulturalYear(selectedDate, culture)
            ),
            _ => (selectedDate.Date, selectedDate.Date)
        };
    }

    public static string Capitalize(string str, CultureInfo? culture = null)
    {
        if (string.IsNullOrEmpty(str)) return "";
        // Use culture-aware casing (e.g. Turkish dotted/dotless I) so the first character is
        // capitalized consistently with the other culture-sensitive formatting helpers.
        var textInfo = (culture ?? CultureInfo.CurrentCulture).TextInfo;
        return textInfo.ToUpper(str[0]) + str[1..];
    }
}

