namespace Bit.Bheadless;

/// <summary>A week-day column of a <see cref="BitBhCalendarGrid"/>: the context of its <c>WeekdayTemplate</c>.</summary>
public sealed class BitBhCalendarWeekday
{
    internal BitBhCalendarWeekday() { }

    /// <summary>The day of the week.</summary>
    public required DayOfWeek DayOfWeek { get; init; }

    /// <summary>The name the grid's <c>WeekdayFormat</c> asks for: "S", "Sun" or "Sunday".</summary>
    public required string Text { get; init; }

    /// <summary>The full name: "Sunday" (the column header's accessible name).</summary>
    public required string Name { get; init; }
}
