namespace Bit.BlazorUI;

/// <summary>
/// How often a recurring event repeats. The step between two occurrences is this unit multiplied by
/// <see cref="BitFullCalendarRecurrence.Interval"/>.
/// </summary>
public enum BitFullCalendarRecurrenceFrequency
{
    /// <summary>Repeats every <c>Interval</c> days.</summary>
    Daily,

    /// <summary>
    /// Repeats every <c>Interval</c> weeks, on the weekdays listed in
    /// <see cref="BitFullCalendarRecurrence.DaysOfWeek"/> (the start date's own weekday when none are listed).
    /// </summary>
    Weekly,

    /// <summary>
    /// Repeats every <c>Interval</c> months on the start date's day of the month. A month that is too
    /// short for that day (the 31st of a 30-day month) is skipped rather than shifted.
    /// </summary>
    Monthly,

    /// <summary>
    /// Repeats every <c>Interval</c> years on the start date's month and day. A 29 February series
    /// only occurs in leap years.
    /// </summary>
    Yearly
}
