namespace Bit.BlazorUI;

/// <summary>
/// The repeat rule of a recurring event, assigned to <see cref="BitFullCalendarEvent.Recurrence"/>.
/// The event it sits on is the series master: it defines the first occurrence's date, time, and
/// length, and every later occurrence repeats that same length.
/// <para>
/// Occurrences are expanded for the visible range only, so an open-ended series costs nothing to
/// keep. They carry <see cref="BitFullCalendarEvent.SeriesId"/> and
/// <see cref="BitFullCalendarEvent.OccurrenceDate"/> so a click handler knows which one was picked.
/// An occurrence cannot be dragged or resized; the event details dialog edits or deletes either that
/// one occurrence (by skipping its date and, for an edit, adding a one-off event in its place) or the
/// whole series (by changing the master).
/// </para>
/// <para>
/// Recurrence is computed on the proleptic Gregorian calendar (the one <see cref="DateTime"/> itself
/// counts in), independent of the culture the calendar renders in.
/// </para>
/// </summary>
public sealed class BitFullCalendarRecurrence
{
    /// <summary>How often the event repeats.</summary>
    public BitFullCalendarRecurrenceFrequency Frequency { get; set; }

    /// <summary>
    /// Number of frequency units between two occurrences - 2 with a weekly frequency means every
    /// other week, 15 with a daily one every fifteen days. Values below 1 are treated as 1.
    /// </summary>
    public int Interval
    {
        get => _interval;
        set => _interval = Math.Max(1, value);
    }
    private int _interval = 1;

    /// <summary>
    /// Weekdays the series occurs on. A <see cref="BitFullCalendarRecurrenceFrequency.Weekly"/> series
    /// occurs on each of them every week; a <see cref="BitFullCalendarRecurrenceFrequency.Monthly"/> or
    /// <see cref="BitFullCalendarRecurrenceFrequency.Yearly"/> one with a <see cref="WeekOfMonth"/>
    /// occurs on that week's occurrence of each of them. When <c>null</c> or empty the series follows
    /// the start date's own weekday. Ignored by a daily series, and by a monthly or yearly one without
    /// a <see cref="WeekOfMonth"/>.
    /// </summary>
    public IReadOnlyList<DayOfWeek>? DaysOfWeek { get; set; }

    /// <summary>
    /// Places a <see cref="BitFullCalendarRecurrenceFrequency.Monthly"/> or
    /// <see cref="BitFullCalendarRecurrenceFrequency.Yearly"/> series on a weekday of the month instead
    /// of on the start date's day number: <see cref="BitFullCalendarWeekOfMonth.Third"/> with
    /// <see cref="DaysOfWeek"/> set to Tuesday is "the third Tuesday". A yearly series stays in the
    /// start date's month. <c>null</c> (the default) keeps the day number. Ignored by the other
    /// frequencies.
    /// </summary>
    public BitFullCalendarWeekOfMonth? WeekOfMonth { get; set; }

    /// <summary>
    /// Total number of occurrences the pattern produces, counting the first. <c>null</c> leaves the
    /// series open-ended unless <see cref="Until"/> closes it. When both are set, whichever ends the
    /// series first wins.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// Last date the pattern may occur on (inclusive). <c>null</c> leaves the series open-ended unless
    /// <see cref="Count"/> closes it.
    /// </summary>
    public DateTime? Until { get; set; }

    /// <summary>
    /// Dates the series skips - a cancelled occurrence, a holiday. Only the date part is compared, so
    /// the time of day of an entry does not matter. A skipped date still counts against
    /// <see cref="Count"/>, matching how calendar clients treat a cancelled occurrence, and it also
    /// removes an <see cref="AdditionalDates"/> entry on the same date.
    /// </summary>
    public IReadOnlyList<DateTime>? ExceptionDates { get; set; }

    /// <summary>
    /// Dates the series also occurs on, outside its pattern - a make-up session, an extra meeting.
    /// Only the date part is used: each one repeats the master's time of day and length. They are
    /// independent of the pattern, so they neither count against <see cref="Count"/> nor stop at
    /// <see cref="Until"/>, and a date the pattern already produces is not doubled.
    /// </summary>
    public IReadOnlyList<DateTime>? AdditionalDates { get; set; }

    /// <summary>The weekdays a weekly series occurs on, resolved against the series start date.</summary>
    internal IReadOnlyList<DayOfWeek> ResolveWeekDays(DateTime seriesStart)
    {
        if (DaysOfWeek is not { Count: > 0 })
            return [seriesStart.DayOfWeek];

        var days = DaysOfWeek.Where(Enum.IsDefined).Distinct().OrderBy(d => (int)d).ToList();
        return days.Count > 0 ? days : [seriesStart.DayOfWeek];
    }

    /// <summary>
    /// The week of the month a monthly or yearly series is placed on, or <c>null</c> when it keeps the
    /// start date's day number - which is also what an undefined value falls back to.
    /// </summary>
    internal BitFullCalendarWeekOfMonth? ResolveWeekOfMonth()
        => Frequency is BitFullCalendarRecurrenceFrequency.Monthly or BitFullCalendarRecurrenceFrequency.Yearly
           && WeekOfMonth is { } week && Enum.IsDefined(week)
            ? week
            : null;

    /// <summary>
    /// A copy whose lists are fresh instances, so a change built on it never reaches the rule a change
    /// snapshot still points at.
    /// </summary>
    internal BitFullCalendarRecurrence Copy() => new()
    {
        Frequency = Frequency,
        Interval = Interval,
        DaysOfWeek = DaysOfWeek is null ? null : [.. DaysOfWeek],
        WeekOfMonth = WeekOfMonth,
        Count = Count,
        Until = Until,
        ExceptionDates = ExceptionDates is null ? null : [.. ExceptionDates],
        AdditionalDates = AdditionalDates is null ? null : [.. AdditionalDates]
    };
}
