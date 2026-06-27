using System.Globalization;

namespace Bit.BlazorUI;

public static class BitFullCalendarHelpers
{
    public const int HourHeightPx = 96;
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
        => (ev.EndDate > ev.StartDate ? ev.EndDate.AddTicks(-1) : ev.EndDate).Date;

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

    // -- Culture-aware: Range text ------------------------------

    public static string RangeText(BitFullCalendarView view, DateTime date, CultureInfo? culture = null)
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
                var start = StartOfWeek(date, culture);
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

    public static DateTime NavigateDate(DateTime date, BitFullCalendarView view, bool forward, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var cal = culture.Calendar;
        int delta = forward ? 1 : -1;
        return view switch
        {
            BitFullCalendarView.Month  => cal.AddMonths(date, delta),
            BitFullCalendarView.Week   => date.AddDays(forward ? 7 : -7),
            BitFullCalendarView.Day    => date.AddDays(delta),
            BitFullCalendarView.Year   => cal.AddYears(date, delta),
            BitFullCalendarView.Agenda => cal.AddMonths(date, delta),
            _                   => date
        };
    }

    // -- Culture-aware: Week helpers ------------------------------

    public static DateTime StartOfWeek(DateTime date, CultureInfo? culture = null)
    {
        var startDay = (culture ?? CultureInfo.CurrentUICulture).DateTimeFormat.FirstDayOfWeek;
        return StartOfWeek(date, startDay);
    }

    public static DateTime StartOfWeek(DateTime date, DayOfWeek startDay)
    {
        int diff = (7 + (date.DayOfWeek - startDay)) % 7;
        return date.Date.AddDays(-diff);
    }

    public static DateTime[] GetWeekDates(DateTime date, CultureInfo? culture = null)
    {
        var start = StartOfWeek(date, culture);
        return Enumerable.Range(0, 7).Select(i => start.AddDays(i)).ToArray();
    }

    // -- Culture-aware: Weekday header names ------------------------------

    /// <summary>
    /// Returns 7 shortest day-name strings (1 char) starting from
    /// culture.DateTimeFormat.FirstDayOfWeek.
    /// </summary>
    public static string[] GetWeekDayHeaders(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var dtf = culture.DateTimeFormat;
        var first = (int)dtf.FirstDayOfWeek;
        return Enumerable.Range(0, 7)
            .Select(i => dtf.GetShortestDayName((DayOfWeek)((first + i) % 7)))
            .ToArray();
    }

    /// <summary>
    /// Returns 7 abbreviated day-name strings (2-3 chars) starting from
    /// culture.DateTimeFormat.FirstDayOfWeek.
    /// </summary>
    public static string[] GetAbbreviatedWeekDayHeaders(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var dtf = culture.DateTimeFormat;
        var first = (int)dtf.FirstDayOfWeek;
        return Enumerable.Range(0, 7)
            .Select(i => dtf.GetAbbreviatedDayName((DayOfWeek)((first + i) % 7)))
            .ToArray();
    }

    // -- Culture-aware: Calendar grid cells ------------------------------

    public static List<BitFullCalendarCell> GetCalendarCells(DateTime selectedDate, CultureInfo? culture = null)
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
        int culturalFirst = (int)dtf.FirstDayOfWeek;
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
        var cal = culture.Calendar;
        int culturalYear  = cal.GetYear(date);
        DateTime yearStart = cal.ToDateTime(culturalYear, 1, 1, 0, 0, 0, 0);
        int monthsInYear   = cal.GetMonthsInYear(culturalYear);
        int lastDayOfYear  = cal.GetDaysInMonth(culturalYear, monthsInYear);
        DateTime yearEnd   = cal.ToDateTime(culturalYear, monthsInYear, lastDayOfYear, 23, 59, 59, 0);
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
    public static string BuildEventTooltip(BitFullCalendarEvent ev, bool use24Hour, CultureInfo? culture = null)
    {
        if (ev is null)
            return string.Empty;

        var title = string.IsNullOrWhiteSpace(ev.Title) ? string.Empty : ev.Title.Trim();
        var time = $"{FormatTime(ev.StartDate, use24Hour, culture)} - {FormatTime(ev.EndDate, use24Hour, culture)}";

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
        BitFullCalendarEvent ev, DateTime day, int hourWidthPx = TimelineHourWidthPx)
    {
        var dayStart = day.Date;
        var dayEnd = dayStart.AddDays(1);

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

    public static (double TopPx, double WidthPercent, double LeftPercent) GetEventBlockStyle(
        BitFullCalendarEvent ev, DateTime day, int groupIndex, int groupSize)
    {
        var dayStart = day.Date;
        var eventStart = ev.StartDate < dayStart ? dayStart : ev.StartDate;
        double startMinutes = (eventStart - dayStart).TotalMinutes;
        double topPx = startMinutes / 60.0 * HourHeightPx;
        double width = 100.0 / groupSize;
        double left = groupIndex * width;
        return (topPx, width, left);
    }

    private static (int Year, int Month, int Day) MonthGridDayKey(DateTime d)
    {
        d = d.Date;
        return (d.Year, d.Month, d.Day);
    }

    public static Dictionary<string, int> CalculateMonthEventPositions(
        List<BitFullCalendarEvent> multiDayEvents,
        List<BitFullCalendarEvent> singleDayEvents,
        DateTime selectedDate,
        CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var cal = culture.Calendar;
        int y = cal.GetYear(selectedDate);
        int m = cal.GetMonth(selectedDate);
        DateTime monthStart = cal.ToDateTime(y, m, 1, 0, 0, 0, 0);
        DateTime monthEnd   = cal.AddMonths(monthStart, 1).AddDays(-1);

        var eventPositions = new Dictionary<string, int>();
        var occupiedPositions = new Dictionary<(int Year, int Month, int Day), bool[]>();

        for (var d = monthStart; d <= monthEnd; d = d.AddDays(1))
            occupiedPositions[MonthGridDayKey(d)] = new bool[3];

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
            for (int i = 0; i < 3; i++)
            {
                if (eventDays.All(d =>
                {
                    var key = MonthGridDayKey(d);
                    return occupiedPositions.TryGetValue(key, out var slots) && !slots[i];
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
                    if (occupiedPositions.TryGetValue(key, out var slots))
                        slots[position] = true;
                }
                eventPositions[ev.Id] = position;
            }
        }

        return eventPositions;
    }

    public static List<(BitFullCalendarEvent Event, int Position, bool IsMultiDay)> GetMonthCellEvents(
        DateTime date, List<BitFullCalendarEvent> events, Dictionary<string, int> eventPositions)
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

        return AssignMonthCellDisplayRows(raw);
    }

    private static List<(BitFullCalendarEvent Event, int Position, bool IsMultiDay)> AssignMonthCellDisplayRows(
        List<(BitFullCalendarEvent Event, int Position, bool IsMultiDay)> raw)
    {
        var occupied = new bool[3];
        var result = new List<(BitFullCalendarEvent Event, int Position, bool IsMultiDay)>();

        foreach (var x in raw)
        {
            var p = x.Position;
            if (p is >= 0 and < 3 && !occupied[p])
            {
                occupied[p] = true;
                result.Add((x.Event, p, x.IsMultiDay));
                continue;
            }

            var free = -1;
            for (var i = 0; i < 3; i++)
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

    public static List<BitFullCalendarEvent> GetEventsForDay(List<BitFullCalendarEvent> events, DateTime date, bool weekOnly = false)
    {
        var target = date.Date;
        return events.Where(ev =>
        {
            var s = ev.StartDate.Date;
            // Treat a 00:00 end as ending the previous day (exclusive midnight), consistent with
            // IsSingleDay and GroupEventsByDayRange.
            var e = GetInclusiveEndDate(ev);
            if (weekOnly)
                return ev.IsMultiDay && s <= target && e >= target;
            return s <= target && e >= target;
        }).ToList();
    }

    public static List<BitFullCalendarEvent> GetEventsForWeek(List<BitFullCalendarEvent> events, DateTime date, CultureInfo? culture = null)
    {
        var weekStart = StartOfWeek(date, culture);
        var weekEnd = weekStart.AddDays(6);
        return events.Where(ev => OverlapsPeriod(ev, weekStart, weekEnd)).ToList();
    }

    public static List<BitFullCalendarEvent> GetEventsForMonth(List<BitFullCalendarEvent> events, DateTime date, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var cal = culture.Calendar;
        int y = cal.GetYear(date);
        int m = cal.GetMonth(date);
        DateTime monthStart = cal.ToDateTime(y, m, 1, 0, 0, 0, 0);
        DateTime monthEnd   = cal.AddMonths(monthStart, 1).AddDays(-1);
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
        CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        return view switch
        {
            BitFullCalendarView.Day => GetEventsForDay(events, selectedDate),
            BitFullCalendarView.Week => GetEventsForWeek(events, selectedDate, culture),
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

    public static double GetCurrentTimeLineTopPx()
    {
        double minutes = DateTime.Now.TimeOfDay.TotalMinutes;
        return minutes / 60.0 * HourHeightPx;
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
        BitFullCalendarView view, DateTime selectedDate, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var cal = culture.Calendar;

        return view switch
        {
            BitFullCalendarView.Day => (selectedDate.Date, selectedDate.Date),
            BitFullCalendarView.Week =>
            (
                StartOfWeek(selectedDate, culture),
                StartOfWeek(selectedDate, culture).AddDays(6)
            ),
            BitFullCalendarView.Month or BitFullCalendarView.Agenda =>
            (
                cal.ToDateTime(cal.GetYear(selectedDate), cal.GetMonth(selectedDate), 1, 0, 0, 0, 0),
                cal.AddMonths(
                    cal.ToDateTime(cal.GetYear(selectedDate), cal.GetMonth(selectedDate), 1, 0, 0, 0, 0), 1)
                    .AddDays(-1)
            ),
            BitFullCalendarView.Year =>
            (
                cal.ToDateTime(cal.GetYear(selectedDate), 1, 1, 0, 0, 0, 0),
                cal.ToDateTime(cal.GetYear(selectedDate), cal.GetMonthsInYear(cal.GetYear(selectedDate)),
                    cal.GetDaysInMonth(cal.GetYear(selectedDate), cal.GetMonthsInYear(cal.GetYear(selectedDate))),
                    0, 0, 0, 0)
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

