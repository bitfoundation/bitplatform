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

/// <summary>
/// The state a <see cref="BitBhDatePicker"/>, <see cref="BitBhDateTimePicker"/> or <see cref="BitBhDateRangePicker"/>
/// shares with its parts (<see cref="BitBhDatePickerGroup"/>, <see cref="BitBhDatePickerTrigger"/>,
/// <see cref="BitBhDatePickerContent"/>) and with the <see cref="BitBhCalendar"/> in its popover, which takes its value,
/// limits and culture from it. It runs the popover the three pickers share: open or closed, the ids of the parts and
/// whether a group is rendered.
/// </summary>
public sealed class BitBhDatePickerContext : IBitBhCloseContext
{
    private bool _open;
    private bool _hasGroup;

    internal BitBhDatePickerContext(IBitBhDatePickerOwner owner)
    {
        Owner = owner;
        _open = owner.DefaultOpen;
    }

    internal IBitBhDatePickerOwner Owner { get; }

    /// <summary>Whether the calendar popover is open.</summary>
    public bool IsOpen => Owner.IsOpenControlled ? Owner.Open : _open;

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
    public Task OpenAsync() => IsOpen || Owner.IsDisabled ? Task.CompletedTask : SetOpenAsync(true);

    /// <summary>Closes the calendar popover.</summary>
    public Task CloseAsync() => IsOpen ? SetOpenAsync(false) : Task.CompletedTask;

    /// <summary>Opens the calendar popover when closed, closes it when open.</summary>
    public Task ToggleAsync() => IsOpen ? CloseAsync() : OpenAsync();

    internal CultureInfo? Culture => Owner.Culture;

    internal string GroupId => $"{Owner.RootId}-group";
    internal string TriggerId => $"{Owner.RootId}-trigger";
    internal string ContentId => $"{Owner.RootId}-content";

    /// <summary>Whether a <see cref="BitBhDatePickerGroup"/> is rendered (the popover is anchored to it, else to the trigger).</summary>
    internal bool HasGroup => _hasGroup;

    /// <summary>The name of the trigger (and of the popover without a label): "Choose date" or "Choose date range".</summary>
    internal string ChooseLabel(BitBhStrings strings) => Owner.IsRange ? strings.ChooseDateRange : strings.ChooseDate;

    /// <summary>The picker's content, with this context cascaded to it.</summary>
    internal RenderFragment Cascade(RenderFragment? content) => builder =>
    {
        builder.OpenComponent<CascadingValue<BitBhDatePickerContext>>(0);
        builder.AddComponentParameter(1, "Value", this);
        // Not fixed: the parts render the open state and the value, which change without new parameters.
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", content);
        builder.CloseComponent();
    };

    internal void RegisterGroup(bool present)
    {
        if (_hasGroup == present) return;
        _hasGroup = present;
        Owner.Render();
    }

    /// <summary>A date or a range was picked in the calendar: the popover closes, unless the picker keeps it open.</summary>
    internal Task PickedAsync() => Owner.CloseOnSelect ? CloseAsync() : Task.CompletedTask;

    private async Task SetOpenAsync(bool open)
    {
        if (!Owner.IsOpenControlled) _open = open;
        await Owner.OpenChanged.InvokeAsync(open);
        await Owner.OnOpenChange.InvokeAsync(open);
        Owner.Render();
    }
}
