using System.Globalization;

namespace Bit.Bheadless;

/// <summary>
/// The state a <see cref="BitBhDatePicker"/> shares with its parts (<see cref="BitBhDatePickerGroup"/>,
/// <see cref="BitBhDatePickerTrigger"/>, <see cref="BitBhDatePickerContent"/>) and with the <see cref="BitBhCalendar"/>
/// in its popover, which takes its value, limits and culture from it.
/// </summary>
public sealed class BitBhDatePickerContext : IBitBhCloseContext
{
    internal BitBhDatePickerContext(BitBhDatePicker owner) => Owner = owner;

    internal BitBhDatePicker Owner { get; }

    /// <summary>Whether the calendar popover is open.</summary>
    public bool IsOpen => Owner.IsOpen;

    /// <summary>The selected date.</summary>
    public DateOnly? Value => Owner.CurrentValue;

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
}
