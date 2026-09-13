namespace Bit.BlazorUI;

/// <summary>
/// The unit a <see cref="BitFullCalendarRecurrence"/> repeats in.
/// </summary>
public enum BitFullCalendarRecurrenceFrequency
{
    /// <summary>
    /// Repeats every <see cref="BitFullCalendarRecurrence.Interval"/> days.
    /// </summary>
    Daily,

    /// <summary>
    /// Repeats on the <see cref="BitFullCalendarRecurrence.DaysOfWeek"/> of every
    /// <see cref="BitFullCalendarRecurrence.Interval"/> weeks.
    /// </summary>
    Weekly,

    /// <summary>
    /// Repeats every <see cref="BitFullCalendarRecurrence.Interval"/> months, on the day of the month the
    /// series starts on or on the weekday picked with <see cref="BitFullCalendarRecurrence.WeekOfMonth"/>.
    /// </summary>
    Monthly,

    /// <summary>
    /// Repeats every <see cref="BitFullCalendarRecurrence.Interval"/> years in the month the series starts
    /// in, on its day of the month or on the weekday picked with <see cref="BitFullCalendarRecurrence.WeekOfMonth"/>.
    /// </summary>
    Yearly
}
