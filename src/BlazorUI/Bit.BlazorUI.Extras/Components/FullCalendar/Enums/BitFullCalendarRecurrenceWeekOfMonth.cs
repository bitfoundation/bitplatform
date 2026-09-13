namespace Bit.BlazorUI;

/// <summary>
/// The week of the month a monthly or yearly <see cref="BitFullCalendarRecurrence"/> picks its weekday in,
/// as in "the third Tuesday" or "the last Friday".
/// </summary>
public enum BitFullCalendarRecurrenceWeekOfMonth
{
    /// <summary>
    /// The first such weekday of the month.
    /// </summary>
    First,

    /// <summary>
    /// The second such weekday of the month.
    /// </summary>
    Second,

    /// <summary>
    /// The third such weekday of the month.
    /// </summary>
    Third,

    /// <summary>
    /// The fourth such weekday of the month.
    /// </summary>
    Fourth,

    /// <summary>
    /// The last such weekday of the month, whether it is the fourth or the fifth.
    /// </summary>
    Last
}
