using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// The date field a <see cref="BitBhDateRangePickerInput"/> renders: the picker holds its value, and it hands the focus
/// on to the other input past its first or last segment.
/// </summary>
internal sealed class BitBhDateRangeField : BitBhDateField
{
    private bool _wasFocused;
    private bool _wasEmpty;
    // Where the field is registered: Picker and Part can change.
    private BitBhDateRangePicker? _registeredPicker;
    private BitBhDateRangePart _registeredPart;

    [Parameter]
    public BitBhDateRangePicker Picker { get; set; } = default!;

    [Parameter]
    public BitBhDateRangePart Part { get; set; }

    internal bool HasFocus => ((IBitBhDateFieldOwner)this).FocusedSegment is not null;

    internal bool IsEmpty => State.IsEmpty;

    // The picker is the field's control: its label focuses the start.
    private protected override bool IsFieldControl => false;

    private protected override BitBhDateFieldBase<DateOnly>? NextField => Part == BitBhDateRangePart.Start ? Picker.EndField : null;

    private protected override BitBhDateFieldBase<DateOnly>? PreviousField => Part == BitBhDateRangePart.End ? Picker.StartField : null;

    private protected override bool IsValueUnavailable(DateOnly value) => Picker.IsDateUnavailable?.Invoke(value) ?? false;

    protected override void OnParametersSet()
    {
        if (_registeredPicker != Picker || _registeredPart != Part)
        {
            _registeredPicker?.RegisterField(_registeredPart, this, false);
            (_registeredPicker, _registeredPart) = (Picker, Part);
            Picker.RegisterField(Part, this, true);
        }
        base.OnParametersSet();
    }

    /// <summary>Empties the segments - a date typed partway included - and leaves the value to the picker.</summary>
    internal void ClearSegments()
    {
        State.Clear();
        StateHasChanged();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // The picker's root and group show whether either input has the focus, and whether both are empty.
        var focused = HasFocus;
        var empty = IsEmpty;
        if (firstRender)
        {
            (_wasFocused, _wasEmpty) = (focused, empty);
            return;
        }
        if (focused == _wasFocused && empty == _wasEmpty) return;
        (_wasFocused, _wasEmpty) = (focused, empty);
        Picker.FieldStateChanged();
    }

    public override void Dispose()
    {
        _registeredPicker?.RegisterField(_registeredPart, this, false);
        _registeredPicker = null;
        base.Dispose();
    }
}
