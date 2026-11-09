using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A headless date range picker (React Aria's DateRangePicker, Bits UI's DateRangePicker): a start and an end date, each
/// typed into its own segments, with a button that opens a range <see cref="BitBhCalendar"/> in a popover.
///
/// <para><b>Parts:</b> <see cref="BitBhDatePickerGroup"/> (the <c>role="group"</c> around the inputs and the button,
/// labelled by the field's label; Alt+↓ in it opens the calendar), two <see cref="BitBhDateRangePickerInput"/>s — the start
/// and the end, each a date field of its own (<c>role="group"</c> named "Start date" / "End date") holding
/// <see cref="BitBhDateFieldSegments"/> — the <see cref="BitBhDatePickerTrigger"/> ("Choose date range") and
/// <see cref="BitBhDatePickerContent"/>, the popover, with a <see cref="BitBhCalendar"/> in it. The calendar picks ranges
/// here, and takes the picker's range, <see cref="Min"/> / <see cref="Max"/>, <see cref="IsDateUnavailable"/>,
/// <see cref="AllowsNonContiguousRanges"/>, <see cref="Culture"/> and <see cref="FirstDayOfWeek"/> unless it is given its own.</para>
///
/// <para><b>Typing:</b> the inputs type and step like <see cref="BitBhDateField"/>, and the focus runs on from the last
/// segment of the start into the end (and Backspace or ← back), so a whole range can be typed in one go. Every segment is
/// a tab stop.</para>
///
/// <para><b>Value:</b> <c>@bind-Value</c> a <see cref="BitBhDateRange"/>: set once both dates are filled in, the end on or
/// after the start; null while one is missing. An end before the start, a date outside <see cref="Min"/> /
/// <see cref="Max"/> or an unavailable one (or one a range can't span) is kept in the segments and marks the picker
/// invalid (<c>data-invalid</c>, <c>aria-invalid</c>).</para>
///
/// <para><b>Focus:</b> opening moves the focus to the calendar (the start of the range, else today); the first press sets
/// one end and the second the other, which closes the popover (<see cref="CloseOnSelect"/>) and returns the focus to the
/// button. Escape drops a half-picked range, then closes.</para>
///
/// <code>
/// &lt;BitBhField&gt;
///     &lt;BitBhLabel&gt;Stay&lt;/BitBhLabel&gt;
///     &lt;BitBhDateRangePicker @bind-Value="stay" Min="today"&gt;
///         &lt;BitBhDatePickerGroup class="picker-group"&gt;
///             &lt;BitBhDateRangePickerInput Part="BitBhDateRangePart.Start" /&gt;
///             &lt;span aria-hidden="true"&gt;–&lt;/span&gt;
///             &lt;BitBhDateRangePickerInput Part="BitBhDateRangePart.End" /&gt;
///             &lt;BitBhDatePickerTrigger class="picker-trigger"&gt;📅&lt;/BitBhDatePickerTrigger&gt;
///         &lt;/BitBhDatePickerGroup&gt;
///         &lt;BitBhDatePickerContent class="picker-popover"&gt;
///             &lt;BitBhCalendar VisibleMonths="2"&gt; … &lt;/BitBhCalendar&gt;
///         &lt;/BitBhDatePickerContent&gt;
///     &lt;/BitBhDateRangePicker&gt;
/// &lt;/BitBhField&gt;
/// </code>
///
/// <para><b>Forms:</b> <c>EditForm</c> validation through <c>@bind-Value</c>, <see cref="BitBhField"/> wiring (clicking the
/// label focuses the start) and a hidden input (<see cref="Name"/>) carrying the range as an ISO interval
/// (<c>2026-11-08/2026-11-12</c>), as a range <see cref="BitBhCalendar"/> posts it.</para>
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c>, <c>data-disabled</c>, <c>data-readonly</c>,
/// <c>data-invalid</c>, <c>data-required</c>, <c>data-focused</c> (a segment of either input has the focus),
/// <c>data-empty</c> (both inputs empty) on the root and the group; the inputs carry the date field's hooks and
/// <c>data-part="start|end"</c>.</para>
/// </summary>
public class BitBhDateRangePicker : BitBhComponentBase, IBitBhDatePickerOwner, IDisposable
{
    private BitBhDatePickerContext? _picker;
    private BitBhFieldBinding? _binding;
    private bool _initialized;
    private BitBhDateRange? _value;
    private BitBhDateRange? _lastValue;
    // What the start and the end inputs hold (each once all its segments are filled): kept apart from the value, which
    // only a complete range in order makes.
    private DateOnly? _start;
    private DateOnly? _end;
    private bool _open;
    private bool _hasGroup;
    private BitBhDateRangeField? _startField;
    private BitBhDateRangeField? _endField;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>The selected range. Supplying it makes the picker controlled (use <c>@bind-Value</c>). Null while a date is missing.</summary>
    [Parameter]
    public BitBhDateRange? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<BitBhDateRange?> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Value</c>).</summary>
    [Parameter]
    public Expression<Func<BitBhDateRange?>>? ValueExpression { get; set; }

    /// <summary>Initial range when uncontrolled.</summary>
    [Parameter]
    public BitBhDateRange? DefaultValue { get; set; }

    /// <summary>Fires whenever the range changes: once both dates are filled in (in order), and when a complete range is broken.</summary>
    [Parameter]
    public EventCallback<BitBhDateRange?> OnValueChange { get; set; }

    /// <summary>Whether the calendar popover is open. Supplying it makes it controlled (use <c>@bind-Open</c>).</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Two-way binding callback for <see cref="Open"/>.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Initial open state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Fires whenever the popover opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>The earliest valid date. An earlier one is kept, and marks the picker invalid; the calendar disables earlier dates.</summary>
    [Parameter]
    public DateOnly? Min { get; set; }

    /// <summary>The latest valid date. A later one is kept, and marks the picker invalid; the calendar disables later dates.</summary>
    [Parameter]
    public DateOnly? Max { get; set; }

    /// <summary>
    /// Marks dates that can't be picked (booked nights, holidays): the calendar shows them unavailable, and a typed one -
    /// or, unless <see cref="AllowsNonContiguousRanges"/>, a range across one - marks the picker invalid.
    /// </summary>
    [Parameter]
    public Func<DateOnly, bool>? IsDateUnavailable { get; set; }

    /// <summary>Lets a range span unavailable dates. Default: false.</summary>
    [Parameter]
    public bool AllowsNonContiguousRanges { get; set; }

    /// <summary>The date an empty segment starts from when ↑ / ↓ are pressed on it. Default: today.</summary>
    [Parameter]
    public DateOnly? PlaceholderValue { get; set; }

    /// <summary>The first day of the calendar's week. Default: the culture's.</summary>
    [Parameter]
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// The culture whose short date pattern orders the segments, and whose calendar the dates are in. Default:
    /// <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>Closes the popover once a range is picked in the calendar. Default: true.</summary>
    [Parameter]
    public bool CloseOnSelect { get; set; } = true;

    /// <summary>Disables the picker.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes the picker read-only: focusable and announced, but its range cannot change.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Marks the picker invalid (in addition to EditContext validation and the range's own checks).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the picker required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Form field name: a hidden input carries the range as an ISO 8601 interval (<c>yyyy-MM-dd/yyyy-MM-dd</c>).</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Reading direction (mirrors ← / →). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>Focuses the first segment of the start when the picker first renders.</summary>
    [Parameter]
    public bool AutoFocus { get; set; }

    /// <summary>The group, the inputs, the trigger and the popover.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The selected range.</summary>
    public BitBhDateRange? CurrentValue => IsProvided(nameof(Value)) ? Value : _value;

    /// <summary>Whether the calendar popover is open.</summary>
    public bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    /// <summary>Whether the picker is disabled (own flag, field or fieldset).</summary>
    public bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);

    /// <summary>Whether the picker is read-only.</summary>
    public bool IsReadOnly => ReadOnly;

    /// <summary>
    /// Whether the picker is invalid: marked so, by validation, an end before the start, or a date out of range or
    /// unavailable (or a range across an unavailable date).
    /// </summary>
    public bool IsInvalid => Invalid || IsValueInvalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);

    /// <summary>Whether the picker is required.</summary>
    public bool IsRequired => Required || (FieldContext?.Required ?? false);

    /// <summary>Whether both dates are filled in but the end is before the start.</summary>
    public bool IsReversed => _start is { } start && _end is { } end && end < start;

    internal BitBhDatePickerContext PickerContext => _picker ??= new BitBhDatePickerContext(this);

    internal string GroupId => $"{ComponentId}-group";
    internal string TriggerId => $"{ComponentId}-trigger";
    internal string ContentId => $"{ComponentId}-content";
    internal string InputId(BitBhDateRangePart part) => $"{ComponentId}-{(part == BitBhDateRangePart.Start ? "start" : "end")}";
    internal string? LabelledBy => FieldContext?.LabelledBy;
    internal BitBhDirection EffectiveDir => Dir ?? CascadedDir;
    internal BitBhDateRangeField? StartField => _startField;
    internal BitBhDateRangeField? EndField => _endField;

    private bool HasFocus => (_startField?.HasFocus ?? false) || (_endField?.HasFocus ?? false);

    private bool IsEmpty => (_startField?.IsEmpty ?? _start is null) && (_endField?.IsEmpty ?? _end is null);

    private bool IsValueInvalid
        => (_start is { } start && !IsSelectable(start)) || (_end is { } end && !IsSelectable(end)) || IsReversed
           || (_start is { } s && _end is { } e && s <= e && SpansUnavailable(s, e));

    private bool IsSelectable(DateOnly date)
        => (Min is not { } min || date >= min) && (Max is not { } max || date <= max) && !(IsDateUnavailable?.Invoke(date) ?? false);

    private bool SpansUnavailable(DateOnly start, DateOnly end)
    {
        // As the calendar: long ranges are not walked day by day, only what a person could pick by hand is checked.
        if (AllowsNonContiguousRanges || IsDateUnavailable is not { } unavailable || end.DayNumber - start.DayNumber > 3700) return false;
        for (var d = start; d <= end; d = d.AddDays(1))
            if (unavailable(d)) return true;
        return false;
    }

    /// <summary>What the input of <paramref name="part"/> holds.</summary>
    internal DateOnly? PartValue(BitBhDateRangePart part) => part == BitBhDateRangePart.Start ? _start : _end;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        _value = DefaultValue;
        _open = DefaultOpen;
        FieldContext?.SetControlFocus(FocusAsync);
    }

    protected override void OnParametersSet()
    {
        var current = CurrentValue;
        if (!_initialized || current != _lastValue)
        {
            // First, or changed from outside: the inputs show the new range (an emptied range empties them).
            _initialized = true;
            _lastValue = current;
            _start = current?.Start;
            _end = current?.End;
        }

        if (ValueExpression is not null || _binding is not null)
        {
            _binding ??= new BitBhFieldBinding(() => InvokeAsync(OnValidationStateChanged));
            _binding.Update(EditContext, ValueExpression);
            _binding.ReportTo(FieldContext);
        }
    }

    private void OnValidationStateChanged()
    {
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    /// <summary>Focuses the first segment of the start.</summary>
    public Task FocusAsync() => _startField?.FocusAsync() ?? Task.CompletedTask;

    /// <summary>Empties both inputs (and the range).</summary>
    public async Task ClearAsync()
    {
        _start = _end = null;
        await CommitAsync();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhDatePickerContext>>(40);
        builder.AddComponentParameter(41, "Value", PickerContext);
        // Not fixed: the parts render the open state and the range, which change without new parameters.
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();

        // Like a native disabled control, a disabled picker posts nothing.
        if (!string.IsNullOrEmpty(Name) && !IsDisabled)
        {
            builder.OpenElement(60, "input");
            builder.AddAttribute(61, "type", "hidden");
            builder.AddAttribute(62, "name", Name);
            builder.AddAttribute(63, "value", CurrentValue?.ToString() ?? string.Empty);
            builder.CloseElement();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        AddStateAttributes(attrs);
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    private void AddStateAttributes(Dictionary<string, object> attrs)
    {
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "readonly", IsReadOnly);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "required", IsRequired);
        SetDataFlag(attrs, "focused", HasFocus);
        SetDataFlag(attrs, "empty", IsEmpty);
    }

    // ── The inputs ───────────────────────────────────────────────────────────

    internal void RegisterField(BitBhDateRangePart part, BitBhDateRangeField field, bool present)
    {
        if (part == BitBhDateRangePart.Start)
        {
            if (present) _startField = field;
            else if (_startField == field) _startField = null;
        }
        else
        {
            if (present) _endField = field;
            else if (_endField == field) _endField = null;
        }
    }

    /// <summary>An input's focus or emptiness changed: the root and the group show it.</summary>
    internal void FieldStateChanged() => StateHasChanged();

    /// <summary>An input took a new date from its segments (or was emptied).</summary>
    internal Task SetPartAsync(BitBhDateRangePart part, DateOnly? date)
    {
        if (part == BitBhDateRangePart.Start) _start = date;
        else _end = date;
        return CommitAsync();
    }

    /// <summary>Takes the range the inputs make - null unless both are filled in, in order - then renders.</summary>
    private async Task CommitAsync()
    {
        var next = _start is { } start && _end is { } end && start <= end ? new BitBhDateRange(start, end) : null;
        if (next != CurrentValue)
        {
            _lastValue = next;
            if (!IsProvided(nameof(Value))) _value = next;
            await ValueChanged.InvokeAsync(next);
            await OnValueChange.InvokeAsync(next);
            _binding?.NotifyChanged();
            _binding?.ReportTo(FieldContext);
        }
        StateHasChanged();
    }

    // ── The popover ──────────────────────────────────────────────────────────

    /// <summary>Opens the calendar popover.</summary>
    public Task OpenAsync() => IsOpen || IsDisabled ? Task.CompletedTask : SetOpenAsync(true);

    /// <summary>Closes the calendar popover.</summary>
    public Task CloseAsync() => IsOpen ? SetOpenAsync(false) : Task.CompletedTask;

    private async Task SetOpenAsync(bool open)
    {
        if (!IsProvided(nameof(Open))) _open = open;
        await OpenChanged.InvokeAsync(open);
        await OnOpenChange.InvokeAsync(open);
        StateHasChanged();
    }

    /// <summary>A range picked in the calendar.</summary>
    private async Task SelectRangeAsync(BitBhDateRange range)
    {
        if (IsDisabled || IsReadOnly) return;
        _start = range.Start;
        _end = range.End;
        await CommitAsync();
        if (CloseOnSelect) await CloseAsync();
    }

    // ── What the parts and the calendar read ─────────────────────────────────

    bool IBitBhDatePickerOwner.IsRange => true;
    string IBitBhDatePickerOwner.GroupId => GroupId;
    string IBitBhDatePickerOwner.TriggerId => TriggerId;
    string IBitBhDatePickerOwner.ContentId => ContentId;
    bool IBitBhDatePickerOwner.HasGroup => _hasGroup;
    string? IBitBhDatePickerOwner.LabelledBy => LabelledBy;
    DateOnly? IBitBhDatePickerOwner.SelectedDate => null;
    BitBhDateRange? IBitBhDatePickerOwner.SelectedRange => CurrentValue;
    DateOnly? IBitBhDatePickerOwner.MinDate => Min;
    DateOnly? IBitBhDatePickerOwner.MaxDate => Max;
    BitBhDirection IBitBhDatePickerOwner.EffectiveDir => EffectiveDir;
    Task IBitBhDatePickerOwner.ToggleAsync() => IsOpen ? CloseAsync() : OpenAsync();
    Task IBitBhDatePickerOwner.SelectDateAsync(DateOnly date) => Task.CompletedTask;
    Task IBitBhDatePickerOwner.SelectRangeAsync(BitBhDateRange range) => SelectRangeAsync(range);

    void IBitBhDatePickerOwner.RegisterGroup(bool present)
    {
        if (_hasGroup == present) return;
        _hasGroup = present;
        StateHasChanged();
    }

    void IBitBhDatePickerOwner.AddGroupState(Dictionary<string, object> attrs, Dictionary<string, object>? own)
    {
        attrs["role"] = "group";
        var named = own is not null && (own.ContainsKey("aria-label") || own.ContainsKey("aria-labelledby"));
        if (FieldContext?.LabelledBy is { } label && !named) attrs["aria-labelledby"] = label;
        if (FieldContext?.DescribedBy is { } description) attrs["aria-describedby"] = description;
        AddStateAttributes(attrs);
        SetDataState(attrs, IsOpen);
    }

    public virtual void Dispose()
    {
        _binding?.Dispose();
        FieldContext?.SetControlFocus(null);
    }
}
