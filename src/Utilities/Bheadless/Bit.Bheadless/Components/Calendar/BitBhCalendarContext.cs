using System.Globalization;

namespace Bit.Bheadless;

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
