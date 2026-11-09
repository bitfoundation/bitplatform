using System.Globalization;

namespace Bit.Bheadless;

/// <summary>
/// What a picker (<see cref="BitBhDatePicker"/>, <see cref="BitBhDateTimePicker"/>, <see cref="BitBhDateRangePicker"/>)
/// shares with its parts and with the <see cref="BitBhCalendar"/> in its popover.
/// </summary>
internal interface IBitBhDatePickerOwner
{
    bool IsOpen { get; }
    bool IsDisabled { get; }
    bool IsReadOnly { get; }
    bool IsInvalid { get; }

    /// <summary>Whether the picker picks a range (its calendar selects ranges).</summary>
    bool IsRange { get; }

    string GroupId { get; }
    string TriggerId { get; }
    string ContentId { get; }
    bool HasGroup { get; }
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

    Task OpenAsync();
    Task CloseAsync();
    Task ToggleAsync();
    void RegisterGroup(bool present);
    void AddGroupState(Dictionary<string, object> attrs, Dictionary<string, object>? own);

    /// <summary>A date picked in the calendar.</summary>
    Task SelectDateAsync(DateOnly date);

    /// <summary>A range picked in the calendar.</summary>
    Task SelectRangeAsync(BitBhDateRange range);
}

/// <summary>
/// The state a <see cref="BitBhDatePicker"/>, <see cref="BitBhDateTimePicker"/> or <see cref="BitBhDateRangePicker"/>
/// shares with its parts (<see cref="BitBhDatePickerGroup"/>, <see cref="BitBhDatePickerTrigger"/>,
/// <see cref="BitBhDatePickerContent"/>) and with the <see cref="BitBhCalendar"/> in its popover, which takes its value,
/// limits and culture from it.
/// </summary>
public sealed class BitBhDatePickerContext : IBitBhCloseContext
{
    internal BitBhDatePickerContext(IBitBhDatePickerOwner owner) => Owner = owner;

    internal IBitBhDatePickerOwner Owner { get; }

    /// <summary>Whether the calendar popover is open.</summary>
    public bool IsOpen => Owner.IsOpen;

    /// <summary>The selected date (the date of a <see cref="BitBhDateTimePicker"/>'s value; null in a range picker).</summary>
    public DateOnly? Value => Owner.SelectedDate;

    /// <summary>The selected range of a <see cref="BitBhDateRangePicker"/> (null in the other pickers).</summary>
    public BitBhDateRange? Range => Owner.SelectedRange;

    /// <summary>Whether the picker picks a range (a <see cref="BitBhDateRangePicker"/>).</summary>
    public bool IsRange => Owner.IsRange;

    /// <summary>Whether the picker is disabled.</summary>
    public bool IsDisabled => Owner.IsDisabled;

    /// <summary>Whether the picker is read-only (the calendar can be browsed, not used to pick).</summary>
    public bool IsReadOnly => Owner.IsReadOnly;

    /// <summary>Whether the picker is invalid.</summary>
    public bool IsInvalid => Owner.IsInvalid;

    /// <summary>Opens the calendar popover.</summary>
    public Task OpenAsync() => Owner.OpenAsync();

    /// <summary>Closes the calendar popover.</summary>
    public Task CloseAsync() => Owner.CloseAsync();

    /// <summary>Opens the calendar popover when closed, closes it when open.</summary>
    public Task ToggleAsync() => Owner.ToggleAsync();

    internal CultureInfo? Culture => Owner.Culture;

    /// <summary>The name of the trigger (and of the popover without a label): "Choose date" or "Choose date range".</summary>
    internal string ChooseLabel(BitBhStrings strings) => Owner.IsRange ? strings.ChooseDateRange : strings.ChooseDate;
}
