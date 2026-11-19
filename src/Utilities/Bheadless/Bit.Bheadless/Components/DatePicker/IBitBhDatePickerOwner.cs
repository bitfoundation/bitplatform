using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// What a picker (<see cref="BitBhDatePicker"/>, <see cref="BitBhDateTimePicker"/>, <see cref="BitBhDateRangePicker"/>)
/// hands to the <see cref="BitBhDatePickerContext"/>, which runs the popover the three share, and to the
/// <see cref="BitBhCalendar"/> in it.
/// </summary>
internal interface IBitBhDatePickerOwner
{
    // The popover's parameters. A component's parameters are its own properties: each picker declares them.
    bool Open { get; }
    EventCallback<bool> OpenChanged { get; }
    bool DefaultOpen { get; }
    EventCallback<bool> OnOpenChange { get; }
    bool CloseOnSelect { get; }

    /// <summary>Whether <see cref="Open"/> is supplied: the open state is controlled.</summary>
    bool IsOpenControlled { get; }

    /// <summary>The id of the picker's root, which the ids of the parts start with.</summary>
    string RootId { get; }

    /// <summary>Renders the picker (its open state or its group changed).</summary>
    void Render();

    bool IsDisabled { get; }
    bool IsReadOnly { get; }
    bool IsInvalid { get; }

    /// <summary>Whether the picker picks a range (its calendar selects ranges).</summary>
    bool IsRange { get; }

    string? LabelledBy { get; }
    CultureInfo? Culture { get; }

    /// <summary>The date the calendar shows selected (a date-time picker's date).</summary>
    DateOnly? SelectedDate { get; }

    /// <summary>The range the calendar shows selected.</summary>
    BitBhDateRange? SelectedRange { get; }

    DateOnly? MinDate { get; }
    DateOnly? MaxDate { get; }
    Func<DateOnly, bool>? IsDateUnavailable { get; }
    bool AllowsNonContiguousRanges { get; }
    DayOfWeek? FirstDayOfWeek { get; }
    BitBhDirection EffectiveDir { get; }

    /// <summary>The group's role, name, description and state (<see cref="BitBhDatePickerGroup"/>).</summary>
    void AddGroupState(Dictionary<string, object> attrs, Dictionary<string, object>? own);

    /// <summary>A date picked in the calendar.</summary>
    Task SelectDateAsync(DateOnly date);

    /// <summary>A range picked in the calendar.</summary>
    Task SelectRangeAsync(BitBhDateRange range);
}
