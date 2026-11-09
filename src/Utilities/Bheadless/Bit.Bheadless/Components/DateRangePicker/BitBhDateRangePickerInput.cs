using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>Which date of a <see cref="BitBhDateRangePicker"/> a <see cref="BitBhDateRangePickerInput"/> edits.</summary>
public enum BitBhDateRangePart
{
    /// <summary>The first date of the range.</summary>
    Start,

    /// <summary>The last date of the range.</summary>
    End,
}

/// <summary>
/// The start or the end of a <see cref="BitBhDateRangePicker"/>: a date field of its own — the month, the day and the year
/// as <c>role="spinbutton"</c> segments in the culture's order, typed and stepped like <see cref="BitBhDateField"/> — inside
/// a <c>role="group"</c> named "Start date" / "End date" (<see cref="BitBhStrings.StartDate"/>,
/// <see cref="BitBhStrings.EndDate"/>) and the field's label: "Start date Stay". Your <c>aria-label</c> wins.
///
/// <para>The focus runs on from the last segment of the start into the first of the end, and back (← or Backspace on an
/// empty first segment). Shows the segments unless given content; render them yourself (to style them) with
/// <see cref="BitBhDateFieldSegments"/>:</para>
/// <code>
/// &lt;BitBhDateRangePickerInput Part="BitBhDateRangePart.End" class="range-input"&gt;
///     &lt;BitBhDateFieldSegments&gt;
///         &lt;BitBhDateFieldSegment Segment="context" class="segment" /&gt;
///     &lt;/BitBhDateFieldSegments&gt;
/// &lt;/BitBhDateRangePickerInput&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-part="start|end"</c> and the hooks of <see cref="BitBhDateField"/>:
/// <c>data-disabled</c>, <c>data-readonly</c>, <c>data-invalid</c> (the whole range's), <c>data-required</c>,
/// <c>data-focused</c>, <c>data-empty</c>; see <see cref="BitBhDateFieldSegment"/> for the segments.</para>
/// </summary>
public class BitBhDateRangePickerInput : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhDatePickerContext? Context { get; set; }

    /// <summary>Which date the input edits: <see cref="BitBhDateRangePart.Start"/> (the default) or <see cref="BitBhDateRangePart.End"/>.</summary>
    [Parameter]
    public BitBhDateRangePart Part { get; set; }

    /// <summary>The segments (<see cref="BitBhDateFieldSegments"/>) and anything around them. Default: the segments.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context?.Owner is not BitBhDateRangePicker picker) return;

        var part = Part;
        var id = Id ?? picker.InputId(part);
        var attrs = new Dictionary<string, object>(AdditionalAttributes ?? [], StringComparer.OrdinalIgnoreCase);
        if (!attrs.ContainsKey("aria-label") && !attrs.ContainsKey("aria-labelledby"))
        {
            attrs["aria-label"] = part == BitBhDateRangePart.Start ? Strings.StartDate : Strings.EndDate;
            // With a label, the group says which field it belongs to: "Start date Stay".
            if (picker.LabelledBy is { } label) attrs["aria-labelledby"] = $"{id} {label}";
        }
        attrs.TryAdd("data-part", part == BitBhDateRangePart.Start ? "start" : "end");

        builder.OpenComponent<BitBhDateRangeField>(0);
        builder.AddComponentParameter(1, nameof(BitBhDateRangeField.Picker), picker);
        builder.AddComponentParameter(2, nameof(BitBhDateRangeField.Part), part);
        builder.AddComponentParameter(3, nameof(BitBhDateRangeField.Value), picker.PartValue(part));
        builder.AddComponentParameter(4, nameof(BitBhDateRangeField.ValueChanged),
            EventCallback.Factory.Create<DateOnly?>(picker, value => picker.SetPartAsync(part, value)));
        builder.AddComponentParameter(5, nameof(BitBhDateRangeField.Min), picker.Min);
        builder.AddComponentParameter(6, nameof(BitBhDateRangeField.Max), picker.Max);
        builder.AddComponentParameter(7, nameof(BitBhDateRangeField.PlaceholderValue), picker.PlaceholderValue);
        builder.AddComponentParameter(8, nameof(BitBhDateRangeField.Culture), picker.Culture);
        builder.AddComponentParameter(9, nameof(BitBhDateRangeField.Disabled), picker.Disabled);
        builder.AddComponentParameter(10, nameof(BitBhDateRangeField.ReadOnly), picker.ReadOnly);
        // The range is valid or not as a whole: both inputs say so.
        builder.AddComponentParameter(11, nameof(BitBhDateRangeField.Invalid), picker.IsInvalid);
        builder.AddComponentParameter(12, nameof(BitBhDateRangeField.Required), picker.Required);
        builder.AddComponentParameter(13, nameof(BitBhDateRangeField.Dir), picker.Dir);
        builder.AddComponentParameter(14, nameof(BitBhDateRangeField.AutoFocus), picker.AutoFocus && part == BitBhDateRangePart.Start);
        builder.AddComponentParameter(15, nameof(As), As);
        builder.AddComponentParameter(16, nameof(Id), id);
        builder.AddComponentParameter(17, nameof(Ref), Ref);
        builder.AddComponentParameter(18, nameof(BitBhDateRangeField.ChildContent), ChildContent);
        builder.AddMultipleAttributes(19, attrs);
        builder.CloseComponent();
    }
}

/// <summary>
/// The date field a <see cref="BitBhDateRangePickerInput"/> renders: the picker holds its value, and it hands the focus
/// on to the other input past its first or last segment.
/// </summary>
internal sealed class BitBhDateRangeField : BitBhDateField
{
    private bool _wasFocused;
    private bool _wasEmpty;

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

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Picker.RegisterField(Part, this, true);
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
        Picker.RegisterField(Part, this, false);
        base.Dispose();
    }
}
