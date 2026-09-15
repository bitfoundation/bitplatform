namespace Bit.BlazorUI;

/// <summary>
/// Which occurrence of a weekday within its month a <see cref="BitFullCalendarRecurrence"/> lands on -
/// the "third" in "the third Tuesday of every month". Assigned to
/// <see cref="BitFullCalendarRecurrence.WeekOfMonth"/>.
/// </summary>
public enum BitFullCalendarWeekOfMonth
{
    /// <summary>The first such weekday of the month (days 1 to 7).</summary>
    First,

    /// <summary>The second such weekday of the month (days 8 to 14).</summary>
    Second,

    /// <summary>The third such weekday of the month (days 15 to 21).</summary>
    Third,

    /// <summary>The fourth such weekday of the month (days 22 to 28).</summary>
    Fourth,

    /// <summary>The last such weekday of the month, whether that is its fourth or its fifth.</summary>
    Last
}
