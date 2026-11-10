using System.Globalization;

namespace Bit.Bheadless;

/// <summary>What a <see cref="BitBhCalendar"/> selects.</summary>
public enum BitBhCalendarSelectionMode
{
    /// <summary>One date (<c>@bind-Value</c>).</summary>
    Single,

    /// <summary>A range of dates (<c>@bind-Range</c>): the first press sets one end, the second the other.</summary>
    Range,

    /// <summary>Any number of dates (<c>@bind-Values</c>): each press toggles a date.</summary>
    Multiple,
}

/// <summary>How the week-day column headers of a <see cref="BitBhCalendarGrid"/> name the days.</summary>
public enum BitBhCalendarWeekdayFormat
{
    /// <summary>The culture's shortest names: "S", "M" … in English, "So", "Mo" … in German.</summary>
    Narrow,

    /// <summary>The culture's abbreviated names: "Sun", "Mon" …</summary>
    Abbreviated,

    /// <summary>The full names: "Sunday", "Monday" …</summary>
    Full,
}

/// <summary>
/// The state a <see cref="BitBhCalendar"/> shares with its parts (<see cref="BitBhCalendarHeading"/>,
/// <see cref="BitBhCalendarPrevious"/>, <see cref="BitBhCalendarNext"/>, <see cref="BitBhCalendarGrid"/>,
/// <see cref="BitBhCalendarCell"/>), and the context of their templates.
/// </summary>
public sealed class BitBhCalendarContext
{
    internal BitBhCalendarContext(BitBhCalendar owner) => Owner = owner;

    internal BitBhCalendar Owner { get; }

    /// <summary>The first day of the first visible month.</summary>
    public DateOnly VisibleStart => Owner.VisibleStart;

    /// <summary>The last day of the last visible month.</summary>
    public DateOnly VisibleEnd => Owner.VisibleEnd;

    /// <summary>The visible months as the culture writes them: "November 2026", "November – December 2026".</summary>
    public string Title => Owner.Title;

    /// <summary>The date that holds the calendar's tab stop (the one the arrow keys move).</summary>
    public DateOnly FocusedDate => Owner.Focused;

    /// <summary>What the calendar selects.</summary>
    public BitBhCalendarSelectionMode SelectionMode => Owner.EffectiveMode;

    /// <summary>The selected date (<see cref="BitBhCalendarSelectionMode.Single"/>).</summary>
    public DateOnly? Value => Owner.CurrentValue;

    /// <summary>The selected range (<see cref="BitBhCalendarSelectionMode.Range"/>).</summary>
    public BitBhDateRange? Range => Owner.CurrentRange;

    /// <summary>The selected dates (<see cref="BitBhCalendarSelectionMode.Multiple"/>).</summary>
    public IReadOnlyList<DateOnly> Values => Owner.CurrentValues;

    /// <summary>The first end of a range being picked (the second press completes it), or null.</summary>
    public DateOnly? Anchor => Owner.Anchor;

    /// <summary>The culture the calendar is written in (and whose calendar it shows).</summary>
    public CultureInfo Culture => Owner.System.Culture;

    /// <summary>Whether the calendar is disabled.</summary>
    public bool IsDisabled => Owner.IsDisabled;

    /// <summary>Whether the calendar is read-only.</summary>
    public bool IsReadOnly => Owner.IsReadOnly;

    /// <summary>Whether the calendar is invalid (marked so, or its value is out of range or unavailable).</summary>
    public bool IsInvalid => Owner.IsInvalid;

    /// <summary>Whether the previous page of months can be shown.</summary>
    public bool CanGoPrevious => Owner.CanGoPrevious;

    /// <summary>Whether the next page of months can be shown.</summary>
    public bool CanGoNext => Owner.CanGoNext;

    /// <summary>Shows the previous page of months (moving the focused date along).</summary>
    public Task GoPreviousAsync() => Owner.GoPreviousAsync();

    /// <summary>Shows the next page of months (moving the focused date along).</summary>
    public Task GoNextAsync() => Owner.GoNextAsync();
}

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
