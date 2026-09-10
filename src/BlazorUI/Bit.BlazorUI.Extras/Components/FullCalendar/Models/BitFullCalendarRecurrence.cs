namespace Bit.BlazorUI;

/// <summary>
/// The repeat rule of a recurring event, assigned to <see cref="BitFullCalendarEvent.Recurrence"/>.
/// The event it sits on is the series master: it defines the first occurrence's date, time, and
/// length, and every later occurrence repeats that same length.
/// <para>
/// Occurrences are expanded for the visible range only, so an open-ended series costs nothing to
/// keep. They are rendered read-only: the master is what a consumer edits, and an occurrence carries
/// <see cref="BitFullCalendarEvent.SeriesId"/> and <see cref="BitFullCalendarEvent.OccurrenceDate"/>
/// so a click handler knows which one was picked.
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
    /// other week. Values below 1 are treated as 1.
    /// </summary>
    public int Interval
    {
        get => _interval;
        set => _interval = Math.Max(1, value);
    }
    private int _interval = 1;

    /// <summary>
    /// Weekdays a <see cref="BitFullCalendarRecurrenceFrequency.Weekly"/> series occurs on. When
    /// <c>null</c> or empty the series follows the start date's own weekday. Ignored by the other
    /// frequencies.
    /// </summary>
    public IReadOnlyList<DayOfWeek>? DaysOfWeek { get; set; }

    /// <summary>
    /// Total number of occurrences, counting the first. <c>null</c> leaves the series open-ended
    /// unless <see cref="Until"/> closes it. When both are set, whichever ends the series first wins.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// Last date the series may occur on (inclusive). <c>null</c> leaves the series open-ended unless
    /// <see cref="Count"/> closes it.
    /// </summary>
    public DateTime? Until { get; set; }

    /// <summary>
    /// Dates the series skips - a cancelled occurrence, a holiday. Only the date part is compared, so
    /// the time of day of an entry does not matter. A skipped date still counts against
    /// <see cref="Count"/>, matching how calendar clients treat a cancelled occurrence.
    /// </summary>
    public IReadOnlyList<DateTime>? ExceptionDates { get; set; }

    /// <summary>The weekdays a weekly series occurs on, resolved against the series start date.</summary>
    internal IReadOnlyList<DayOfWeek> ResolveWeekDays(DateTime seriesStart)
    {
        if (DaysOfWeek is not { Count: > 0 })
            return [seriesStart.DayOfWeek];

        var days = DaysOfWeek.Where(Enum.IsDefined).Distinct().OrderBy(d => (int)d).ToList();
        return days.Count > 0 ? days : [seriesStart.DayOfWeek];
    }
}
