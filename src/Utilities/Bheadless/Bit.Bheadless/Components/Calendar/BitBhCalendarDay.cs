namespace Bit.Bheadless;

/// <summary>
/// One date of a <see cref="BitBhCalendarGrid"/>: what a <see cref="BitBhCalendarCell"/> renders and the context of
/// its <c>Template</c>. A snapshot taken when the grid rendered.
/// </summary>
public sealed class BitBhCalendarDay
{
    internal BitBhCalendarDay() { }

    /// <summary>The date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>The first day of the month of the grid the date is shown in.</summary>
    public required DateOnly Month { get; init; }

    /// <summary>The number of the day in its month, in the calendar's culture: "8".</summary>
    public required string Text { get; init; }

    /// <summary>The full date, the cell's accessible name: "Sunday, November 8, 2026".</summary>
    public required string Label { get; init; }

    /// <summary>Whether the date is today.</summary>
    public bool IsToday { get; init; }

    /// <summary>Whether the date belongs to the month before or after the grid's (a filler of the first or last week).</summary>
    public bool IsOutsideMonth { get; init; }

    /// <summary>Whether the date is selected (in a range: inside the selected range, or the one being picked).</summary>
    public bool IsSelected { get; init; }

    /// <summary>Whether the date starts the selected (or highlighted) range.</summary>
    public bool IsSelectionStart { get; init; }

    /// <summary>Whether the date ends the selected (or highlighted) range.</summary>
    public bool IsSelectionEnd { get; init; }

    /// <summary>Whether the date is inside the selected (or highlighted) range, neither its start nor its end.</summary>
    public bool IsRangeMiddle { get; init; }

    /// <summary>Whether the date is in the range being picked (between the first press and the date under the pointer or focus).</summary>
    public bool IsHighlighted { get; init; }

    /// <summary>Whether <c>IsDateUnavailable</c> marks the date: focusable, but not selectable.</summary>
    public bool IsUnavailable { get; init; }

    /// <summary>Whether the date can't be focused or selected: outside <c>Min</c> / <c>Max</c>, or the calendar is disabled.</summary>
    public bool IsDisabled { get; init; }

    /// <summary>Whether the date holds the calendar's tab stop.</summary>
    public bool IsFocused { get; init; }

    /// <summary>Whether the selected value is invalid and the date is part of it.</summary>
    public bool IsInvalid { get; init; }

    /// <summary>The day of the week.</summary>
    public DayOfWeek DayOfWeek => Date.DayOfWeek;
}
