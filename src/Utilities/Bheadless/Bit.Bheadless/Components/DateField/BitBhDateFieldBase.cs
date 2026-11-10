using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The shared behaviour of the segmented fields (<see cref="BitBhDateField"/>, <see cref="BitBhTimeField"/>,
/// <see cref="BitBhDateTimeField"/>, <see cref="BitBhDatePicker"/>, <see cref="BitBhDateTimePicker"/> and the inputs of a
/// <see cref="BitBhDateRangePicker"/>): the value, its limits, the segments the culture's pattern makes, their keyboard
/// model, <c>EditForm</c> / <see cref="BitBhField"/> wiring and the hidden input.
/// </summary>
/// <typeparam name="TValue"><see cref="DateOnly"/>, <see cref="TimeOnly"/> or <see cref="DateTime"/>.</typeparam>
public abstract class BitBhDateFieldBase<TValue> : BitBhComponentBase, IBitBhDateFieldOwner, IDisposable
    where TValue : struct, IComparable<TValue>
{
    private readonly BitBhDateFieldContext _context;
    private BitBhFieldBinding? _binding;
    private bool _initialized;
    private TValue? _value;
    private TValue? _lastValue;
    private Type? _lastCalendar;
    private BitBhDateSegmentType? _focusedSegment;
    // Auto-advance asks the browser to move the focus. Over a slow connection (Blazor Server) the next keys can still
    // come from the segment left behind - or one asked for but not reached yet - before the focus lands: they are
    // meant for the segment the focus is on its way to (_focusIntent), so they go there.
    private BitBhDateSegmentType? _focusIntent;
    // The same across the start and end fields of a range picker: the field the focus left this one for.
    private BitBhDateFieldBase<TValue>? _focusIntentField;
    private readonly BitBhFocusMoves<BitBhDateSegmentType> _focusMoves = new();
    private IReadOnlyList<BitBhDateSegment> _segments = [];

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>Creates the component.</summary>
    protected BitBhDateFieldBase() => _context = new BitBhDateFieldContext(this);

    /// <summary>The value. Supplying it makes the field controlled (use <c>@bind-Value</c>). Null while a segment is empty.</summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Value</c>).</summary>
    [Parameter]
    public Expression<Func<TValue?>>? ValueExpression { get; set; }

    /// <summary>Initial value when uncontrolled.</summary>
    [Parameter]
    public TValue? DefaultValue { get; set; }

    /// <summary>Fires whenever the value changes: once every segment is filled, and when a filled field is emptied.</summary>
    [Parameter]
    public EventCallback<TValue?> OnValueChange { get; set; }

    /// <summary>The earliest valid value. An earlier one is kept, and marks the field invalid (<c>data-invalid</c>, <c>aria-invalid</c>).</summary>
    [Parameter]
    public TValue? Min { get; set; }

    /// <summary>The latest valid value. A later one is kept, and marks the field invalid.</summary>
    [Parameter]
    public TValue? Max { get; set; }

    /// <summary>
    /// The value an empty segment starts from when ↑ / ↓ are pressed on it. Default: today for a date, midnight for a time.
    /// </summary>
    [Parameter]
    public TValue? PlaceholderValue { get; set; }

    /// <summary>
    /// The culture whose pattern orders the segments and names the separators (<c>M/d/yyyy</c>, <c>dd.MM.yyyy</c> …), and
    /// whose calendar the date parts are in. Default: <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>Disables the field.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes the field read-only: focusable and announced, but its value cannot change.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Marks the field invalid (in addition to EditContext validation and <see cref="Min"/> / <see cref="Max"/>).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the field required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Form field name: a hidden input carries the value in ISO 8601 (<c>yyyy-MM-dd</c>, <c>HH:mm:ss</c>).</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Reading direction (mirrors ← / →). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>Focuses the first segment when the field first renders.</summary>
    [Parameter]
    public bool AutoFocus { get; set; }

    /// <summary>The segments (<see cref="BitBhDateFieldSegments"/>) and anything around them. Default: the segments.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The current value.</summary>
    public TValue? CurrentValue => IsProvided(nameof(Value)) ? Value : _value;

    private protected BitBhDateSegmentState State { get; } = new();

    private protected BitBhCalendarSystem System => BitBhCalendarSystem.For(EffectiveCulture);

    private protected virtual CultureInfo? EffectiveCulture => Culture;

    private protected BitBhFieldContext? Field => FieldContext;

    internal BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    /// <summary>Whether the field is disabled (own flag, field or fieldset).</summary>
    public bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);

    /// <summary>Whether the field is read-only.</summary>
    public bool IsReadOnly => ReadOnly;

    /// <summary>Whether the field is invalid: marked so, by validation, or a value out of range (or unavailable).</summary>
    public bool IsInvalid => Invalid || IsValueInvalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);

    /// <summary>Whether the field is required.</summary>
    public bool IsRequired => Required || (FieldContext?.Required ?? false);

    // Checked as the segments show it: a controlled value's units below the granularity (seconds at minute granularity)
    // don't make a time the field shows as 17:00 later than a Max of 17:00.
    private bool IsValueInvalid => CurrentValue is { } current && Normalize(current) is var value
        && ((Min is { } min && value.CompareTo(min) < 0) || (Max is { } max && value.CompareTo(max) > 0) || IsValueUnavailable(value));

    // ── What a date field and a time field differ in ─────────────────────────

    /// <summary>The segments and whether the clock is a 12-hour one.</summary>
    private protected abstract List<BitBhDateSegmentState.Spec> BuildLayout(BitBhCalendarSystem system, out bool is12Hour);

    private protected abstract void Load(TValue value);

    private protected abstract TValue? Compose();

    private protected abstract string ToIso(TValue value);

    /// <summary>
    /// <paramref name="value"/> without the units the segments don't edit (a time field's seconds at minute granularity):
    /// what the field stores and posts, so a unit no segment shows never comes along unseen.
    /// </summary>
    private protected virtual TValue Normalize(TValue value) => value;

    /// <summary>The value of a segment in <see cref="PlaceholderValue"/> (or its default).</summary>
    private protected abstract int PlaceholderPart(BitBhDateSegmentType type);

    /// <summary>The year an empty year segment is taken to be (for the days of February).</summary>
    private protected virtual int PlaceholderYear => System.Year(DateOnly.FromDateTime(DateTime.Today));

    private protected virtual bool IsValueUnavailable(TValue value) => false;

    /// <summary>Whether the root is the field's group (<c>role="group"</c>, labelled by the field's label).</summary>
    private protected virtual bool IsGroupRoot => true;

    /// <summary>Whether the field is its <see cref="BitBhField"/>'s control (its label focuses it): not a range picker's end.</summary>
    private protected virtual bool IsFieldControl => true;

    /// <summary>The field the focus moves on to past the last segment (a range picker's end field, from its start).</summary>
    private protected virtual BitBhDateFieldBase<TValue>? NextField => null;

    /// <summary>The field the focus moves back to before the first segment (a range picker's start field, from its end).</summary>
    private protected virtual BitBhDateFieldBase<TValue>? PreviousField => null;

    /// <summary>The root's content: the child content, or the segments.</summary>
    private protected virtual RenderFragment RootContent => ChildContent ?? (b =>
    {
        b.OpenComponent<BitBhDateFieldSegments>(0);
        b.CloseComponent();
    });

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        _value = DefaultValue is { } initial ? Normalize(initial) : null;
        if (IsFieldControl) FieldContext?.SetControlFocus(this, FocusAsync);
    }

    protected override void OnParametersSet()
    {
        var system = System;
        var layout = BuildLayout(system, out var is12Hour);
        State.Configure(system, layout, is12Hour);

        var current = CurrentValue;
        var calendarChanged = _lastCalendar is not null && _lastCalendar != system.Calendar.GetType();
        _lastCalendar = system.Calendar.GetType();
        if (!_initialized || calendarChanged)
        {
            _initialized = true;
            _lastValue = current;
            if (current is { } value) Load(value);
        }
        else if (!Nullable.Equals(current, _lastValue))
        {
            // Changed from outside: the segments show the new value (an emptied value empties them).
            _lastValue = current;
            if (current is { } value) Load(value);
            else State.Clear();
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && AutoFocus) await FocusAsync();
    }

    /// <summary>Focuses the first segment.</summary>
    public Task FocusAsync()
    {
        if (IsDisabled || !State.Editable.Any()) return Task.CompletedTask;
        return Interop.FocusAsync(SegmentId(State.Editable.First())).AsTask();
    }

    /// <summary>Empties every segment (and the value).</summary>
    public async Task ClearAsync()
    {
        State.Clear();
        await CommitAsync();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _segments = BuildSegments();
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhDateFieldContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        // Not fixed: the segments render the values, which change without them getting new parameters.
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", RootContent);
        builder.CloseComponent();
        builder.CloseElement();

        // Like a native disabled control, a disabled field posts nothing.
        if (!string.IsNullOrEmpty(Name) && !IsDisabled)
        {
            builder.OpenElement(60, "input");
            builder.AddAttribute(61, "type", "hidden");
            builder.AddAttribute(62, "name", Name);
            builder.AddAttribute(63, "value", CurrentValue is { } value ? ToIso(value) : string.Empty);
            builder.CloseElement();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (IsGroupRoot) AddGroupAttributes(attrs);
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        AddStateAttributes(attrs);
        return attrs;
    }

    /// <summary>The group's role, name and description (on the root, or on a picker's group part).</summary>
    internal void AddGroupAttributes(Dictionary<string, object> attrs, Dictionary<string, object>? own = null)
    {
        attrs["role"] = "group";
        var hasName = own is null
            ? GetAdditionalAttribute("aria-label") is not null || GetAdditionalAttribute("aria-labelledby") is not null
            : own.ContainsKey("aria-label") || own.ContainsKey("aria-labelledby");
        if (FieldContext?.LabelledBy is { } label && !hasName) attrs["aria-labelledby"] = label;
        if (FieldContext?.DescribedBy is { } description) attrs["aria-describedby"] = description;
    }

    internal void AddStateAttributes(Dictionary<string, object> attrs)
    {
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "readonly", IsReadOnly);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "required", IsRequired);
        SetDataFlag(attrs, "focused", _focusedSegment is not null);
        SetDataFlag(attrs, "empty", State.IsEmpty);
    }

    private IReadOnlyList<BitBhDateSegment> BuildSegments()
    {
        var list = new BitBhDateSegment[State.Layout.Count];
        for (var i = 0; i < list.Length; i++)
        {
            var spec = State.Layout[i];
            var (min, max) = State.Bounds(spec.Type, PlaceholderYear);
            list[i] = new BitBhDateSegment
            {
                Type = spec.Type,
                Text = State.Text(spec, Strings),
                IsPlaceholder = spec.Type != BitBhDateSegmentType.Literal && State[spec.Type] is null
                                && !(State.TypingSegment == spec.Type && State.Buffer.Length > 0),
                Value = spec.Type == BitBhDateSegmentType.Literal ? null : State[spec.Type],
                MinValue = min,
                MaxValue = max,
            };
        }
        return list;
    }

    // ── Segments ─────────────────────────────────────────────────────────────

    IReadOnlyList<BitBhDateSegment> IBitBhDateFieldOwner.Segments => _segments;

    bool IBitBhDateFieldOwner.IsComplete => State.IsComplete;

    BitBhDateSegmentType? IBitBhDateFieldOwner.FocusedSegment => _focusedSegment;

    string? IBitBhDateFieldOwner.DescribedBy => FieldContext?.DescribedBy;

    internal string SegmentId(BitBhDateSegmentType type) => $"{ComponentId}-{type.ToString().ToLowerInvariant()}";

    string IBitBhDateFieldOwner.SegmentId(BitBhDateSegmentType type) => SegmentId(type);

    string IBitBhDateFieldOwner.SegmentName(BitBhDateSegmentType type) => Strings.DateSegmentName(type);

    string IBitBhDateFieldOwner.ValueText(BitBhDateSegmentType type) => State.ValueText(type, Strings);

    void IBitBhDateFieldOwner.HandleFocus(BitBhDateSegmentType type)
    {
        if (((_focusIntent is { } intent && type != intent) || _focusIntentField is not null) && _focusMoves.IsEcho(type))
        {
            // The echo of a focus move already overtaken by the next one: typing goes on in the segment ahead. A focus
            // event on any other segment - the one typed into included - is the user's (a click).
            return;
        }
        ForgetFocusIntent();
        // The focus has arrived from the other field of a range: the keys still on their way are all in.
        if (PreviousField is { } previous && previous._focusIntentField == this) previous.ForgetFocusIntent();
        if (NextField is { } next && next._focusIntentField == this) next.ForgetFocusIntent();
        if (State.TypingSegment is { } typing && typing != type) State.EndTyping();
        _focusedSegment = type;
        StateHasChanged();
    }

    async Task IBitBhDateFieldOwner.HandleBlurAsync(BitBhDateSegmentType type)
    {
        if (_focusedSegment == type) _focusedSegment = null;
        // Leaving a segment ends what was typed into it: "26" in the year is the year 26.
        if (State.TypingSegment == type) State.EndTyping();
        await CommitAsync();
    }

    async Task IBitBhDateFieldOwner.HandleKeyDownAsync(BitBhDateSegmentType type, KeyboardEventArgs e)
    {
        if (IsDisabled || e.CtrlKey || e.MetaKey || e.AltKey) return;
        if (_focusIntentField is { } field && _focusMoves.IsLeftBehind(type))
        {
            // Meant for the other field of the range, which the focus is on its way to.
            if ((field._focusIntent ?? field._focusedSegment) is { } target)
                await ((IBitBhDateFieldOwner)field).HandleKeyDownAsync(target, e);
            return;
        }
        if (_focusIntent is { } intent && intent != type && _focusMoves.IsLeftBehind(type)) type = intent;
        var rtl = EffectiveDir == BitBhDirection.Rtl;
        var editable = !IsReadOnly;
        switch (e.Key)
        {
            case "ArrowLeft":
                await MoveFocusAsync(type, rtl ? 1 : -1);
                return;
            case "ArrowRight":
                await MoveFocusAsync(type, rtl ? -1 : 1);
                return;
            case "ArrowUp" or "ArrowDown" when editable:
                State.Step(type, e.Key == "ArrowUp" ? 1 : -1, PlaceholderPart(type), PlaceholderYear);
                break;
            case "PageUp" or "PageDown" when editable:
                State.Step(type, (e.Key == "PageUp" ? 1 : -1) * PageStep(type), PlaceholderPart(type), PlaceholderYear);
                break;
            case "Home" or "End" when editable:
                State.SetToBound(type, max: e.Key == "End", PlaceholderYear);
                break;
            case "Backspace" or "Delete" when editable:
                var wasEmpty = State.Backspace(type);
                await CommitAsync();
                if (wasEmpty && e.Key == "Backspace") await MoveFocusAsync(type, -1);
                return;
            default:
                if (!editable || e.Key.Length != 1) return;
                await TypeAsync(type, e.Key[0]);
                return;
        }
        await CommitAsync();
    }

    private async Task TypeAsync(BitBhDateSegmentType type, char key)
    {
        if (type == BitBhDateSegmentType.DayPeriod)
        {
            var lower = char.ToLowerInvariant(key);
            var am = State.AmDesignator;
            var pm = State.PmDesignator;
            if (lower == 'a' || (am.Length > 0 && char.ToLowerInvariant(am[0]) == lower)) State.SetDayPeriod(false);
            else if (lower == 'p' || (pm.Length > 0 && char.ToLowerInvariant(pm[0]) == lower)) State.SetDayPeriod(true);
            else return;
            await CommitAsync();
            return;
        }

        if (char.IsDigit(key))
        {
            var digit = (int)char.GetNumericValue(key);
            if (digit is < 0 or > 9) return;
            var full = State.TypeDigit(type, digit, PlaceholderYear);
            if (full)
            {
                // Full: the next segment takes the next digit (the last one keeps the focus).
                await CommitAsync();
                await MoveFocusAsync(type, 1);
                return;
            }
            await CommitAsync();
            return;
        }

        // A separator ("/", ".", ":", " ") ends the segment and moves on, as typing the whole date in one go does.
        if ((char.IsPunctuation(key) || char.IsSeparator(key) || char.IsSymbol(key)) && (State[type] is not null || State.TypingSegment == type))
        {
            State.EndTyping();
            await CommitAsync();
            await MoveFocusAsync(type, 1);
        }
    }

    private static int PageStep(BitBhDateSegmentType type) => type switch
    {
        BitBhDateSegmentType.Year => 5,
        BitBhDateSegmentType.Month => 2,
        BitBhDateSegmentType.Day => 7,
        BitBhDateSegmentType.Hour => 2,
        BitBhDateSegmentType.Minute or BitBhDateSegmentType.Second => 15,
        _ => 1,
    };

    /// <summary>
    /// Moves the focus to the editable segment <paramref name="step"/> places away, in reading order - past the ends, to
    /// the other field of a range picker.
    /// </summary>
    private async Task MoveFocusAsync(BitBhDateSegmentType from, int step)
    {
        var editable = State.Editable.ToList();
        var index = editable.IndexOf(from);
        var target = index + step;
        var field = target < 0 ? PreviousField : target >= editable.Count ? NextField : this;
        if (index < 0 || field is null) return;
        if (State.TypingSegment == from)
        {
            State.EndTyping();
            await CommitAsync();
        }
        _focusMoves.Leaving(from);
        if (field != this)
        {
            _focusIntent = null;
            _focusIntentField = field;
            await field.FocusEdgeAsync(last: step < 0);
            return;
        }
        _focusIntent = editable[target];
        _focusMoves.Requesting(editable[target]);
        await Interop.FocusAsync(SegmentId(editable[target]));
    }

    /// <summary>Focuses the first (or the last) segment, the focus coming from the other field of a range picker.</summary>
    private async Task FocusEdgeAsync(bool last)
    {
        var editable = State.Editable.ToList();
        if (IsDisabled || editable.Count == 0) return;
        ForgetFocusIntent();
        _focusIntent = last ? editable[^1] : editable[0];
        _focusMoves.Requesting(_focusIntent.Value);
        await Interop.FocusAsync(SegmentId(_focusIntent.Value));
    }

    private void ForgetFocusIntent()
    {
        _focusIntent = null;
        _focusIntentField = null;
        _focusMoves.Clear();
    }

    /// <summary>
    /// Takes the value the segments make once they are all filled (and nothing is being typed), or null when one was
    /// emptied, then renders.
    /// </summary>
    private async Task CommitAsync()
    {
        TValue? next;
        if (State.IsComplete)
        {
            if (State.TypingSegment is not null)
            {
                StateHasChanged();
                return;
            }
            next = Compose();
            // The day kept within its month: show what was taken.
            if (next is { } composed) Load(composed);
        }
        else
        {
            next = null;
        }

        if (!Nullable.Equals(next, CurrentValue))
        {
            _lastValue = next;
            if (!IsProvided(nameof(Value))) _value = next;
            await ValueChanged.InvokeAsync(next);
            await OnValueChange.InvokeAsync(next);
            _binding?.NotifyChanged();
            _binding?.ReportTo(FieldContext);
            await OnCommittedAsync(next);
        }
        StateHasChanged();
    }

    /// <summary>Called after the field took a new value from its segments.</summary>
    private protected virtual Task OnCommittedAsync(TValue? value) => Task.CompletedTask;

    /// <summary>Sets the value from code other than the segments (a picker's calendar), firing the change callbacks.</summary>
    private protected async Task SetValueAsync(TValue? value)
    {
        if (value is { } given) value = Normalize(given);
        if (value is { } v) Load(v);
        else State.Clear();
        if (Nullable.Equals(value, CurrentValue))
        {
            StateHasChanged();
            return;
        }
        _lastValue = value;
        if (!IsProvided(nameof(Value))) _value = value;
        await ValueChanged.InvokeAsync(value);
        await OnValueChange.InvokeAsync(value);
        _binding?.NotifyChanged();
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    public virtual void Dispose()
    {
        _binding?.Dispose();
        if (IsFieldControl) FieldContext?.ReleaseControlFocus(this);
    }
}
