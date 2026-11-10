using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A headless calendar (WAI-ARIA date grid, the behaviour of React Aria's Calendar and RangeCalendar): one or more
/// month grids of dates to pick one date, a range or several dates from.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Selection</b> — one date (<c>@bind-Value</c>), a range (<c>@bind-Range</c>: the first press sets one end,
///     the range follows the pointer and the focus until the second press, Escape cancels) or several dates
///     (<c>@bind-Values</c>). See <see cref="SelectionMode"/>.</item>
///   <item><b>Keyboard</b> — the grid is one tab stop on the focused date. ← / → move a day (mirrored right-to-left),
///     ↑ / ↓ a week, PageUp / PageDown a month (Shift: a year), Home / End to the start / end of the week, Enter or
///     Space select. Moving onto a date of another month shows that month.</item>
///   <item><b>Limits</b> — dates outside <see cref="Min"/> / <see cref="Max"/> are disabled and can't be focused; dates
///     <see cref="IsDateUnavailable"/> marks can be focused but not selected (<c>aria-disabled</c>).</item>
///   <item><b>Culture</b> — month and day names, the first day of the week and the calendar itself come from
///     <see cref="Culture"/>: a Persian (<c>fa-IR</c>), Umm al-Qura (<c>ar-SA</c>) or Thai Buddhist (<c>th-TH</c>)
///     culture shows its own months. Values are always <see cref="DateOnly"/>.</item>
///   <item><b>Several months</b> — <see cref="VisibleMonths"/> with one <see cref="BitBhCalendarGrid"/> per month
///     (<see cref="BitBhCalendarGrid.Offset"/>).</item>
///   <item><b>Forms</b> — <c>EditForm</c> validation, <see cref="BitBhField"/> label / description / error wiring and
///     hidden inputs (<see cref="Name"/>) carrying ISO dates (<c>yyyy-MM-dd</c>, a range as <c>start/end</c>).</item>
/// </list>
///
/// <code>
/// &lt;BitBhCalendar @bind-Value="date" class="calendar"&gt;
///     &lt;BitBhCalendarPrevious&gt;‹&lt;/BitBhCalendarPrevious&gt;
///     &lt;BitBhCalendarHeading /&gt;
///     &lt;BitBhCalendarNext&gt;›&lt;/BitBhCalendarNext&gt;
///     &lt;BitBhCalendarGrid class="calendar-grid"&gt;
///         &lt;BitBhCalendarCell Day="context" class="calendar-cell" /&gt;
///     &lt;/BitBhCalendarGrid&gt;
/// &lt;/BitBhCalendar&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-disabled</c>, <c>data-readonly</c>, <c>data-invalid</c>, <c>data-selection-mode</c>
/// on the root; the cells carry <c>data-today</c>, <c>data-outside-month</c>, <c>data-unavailable</c>, <c>data-disabled</c>,
/// <c>data-selected</c>, <c>data-focused</c>, <c>data-highlighted</c>, <c>data-selection-start</c>,
/// <c>data-selection-end</c> and <c>data-range-middle</c>.</para>
///
/// <para><b>Today</b> is read from the <see cref="TimeProvider"/> registered in the services (the system clock otherwise),
/// in its local time zone; on Blazor Server register one with the reader's time zone.</para>
/// </summary>
public class BitBhCalendar : BitBhComponentBase, IDisposable
{
    private readonly BitBhCalendarContext _context;
    private BitBhFieldBinding? _binding;
    private bool _initialized;
    private DateOnly? _value;
    private BitBhDateRange? _range;
    private IReadOnlyList<DateOnly> _values = [];
    private DateOnly _focused;
    private DateOnly _visibleStart;
    private DateOnly? _anchor;
    private DateOnly? _previewEnd;
    private (DateOnly? Low, DateOnly? High)? _anchorBounds;
    private DateOnly? _lastSelection;
    private DateOnly? _lastFocusedParameter;
    private Calendar? _lastCalendar;
    private bool _focusPending;
    private bool _hasHeading;
    // Read once per render: every cell asks.
    private DateOnly _today;
    private bool _valueInvalid;

    [CascadingParameter]
    private BitBhFieldContext? CascadedField { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    [CascadingParameter]
    private BitBhDatePickerContext? Picker { get; set; }

    // In a date picker the calendar is not the field's control: the picker's segments are.
    private BitBhFieldContext? FieldContext => Picker is null ? CascadedField : null;

    /// <summary>Creates the component.</summary>
    public BitBhCalendar() => _context = new BitBhCalendarContext(this);

    /// <summary>
    /// What the calendar selects. Default: <see cref="BitBhCalendarSelectionMode.Single"/>, or
    /// <see cref="BitBhCalendarSelectionMode.Range"/> / <see cref="BitBhCalendarSelectionMode.Multiple"/> when a
    /// <c>Range…</c> / <c>Values…</c> parameter is supplied.
    /// </summary>
    [Parameter]
    public BitBhCalendarSelectionMode? SelectionMode { get; set; }

    /// <summary>The selected date. Supplying it makes the calendar controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public DateOnly? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<DateOnly?> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Value</c>).</summary>
    [Parameter]
    public Expression<Func<DateOnly?>>? ValueExpression { get; set; }

    /// <summary>Initial date when uncontrolled.</summary>
    [Parameter]
    public DateOnly? DefaultValue { get; set; }

    /// <summary>Fires whenever the selected date changes.</summary>
    [Parameter]
    public EventCallback<DateOnly?> OnValueChange { get; set; }

    /// <summary>The selected range. Supplying it makes the calendar a controlled range calendar (use <c>@bind-Range</c>).</summary>
    [Parameter]
    public BitBhDateRange? Range { get; set; }

    /// <summary>Two-way binding callback for <see cref="Range"/>.</summary>
    [Parameter]
    public EventCallback<BitBhDateRange?> RangeChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Range</c>).</summary>
    [Parameter]
    public Expression<Func<BitBhDateRange?>>? RangeExpression { get; set; }

    /// <summary>Initial range when uncontrolled.</summary>
    [Parameter]
    public BitBhDateRange? DefaultRange { get; set; }

    /// <summary>Fires whenever a range is completed.</summary>
    [Parameter]
    public EventCallback<BitBhDateRange?> OnRangeChange { get; set; }

    /// <summary>The selected dates. Supplying it makes the calendar a controlled multiple-date calendar (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IReadOnlyList<DateOnly>? Values { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<DateOnly>> ValuesChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Values</c>).</summary>
    [Parameter]
    public Expression<Func<IReadOnlyList<DateOnly>?>>? ValuesExpression { get; set; }

    /// <summary>Initial dates when uncontrolled.</summary>
    [Parameter]
    public IReadOnlyList<DateOnly>? DefaultValues { get; set; }

    /// <summary>Fires whenever a date is added to or removed from the selected dates.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<DateOnly>> OnValuesChange { get; set; }

    /// <summary>
    /// The date that holds the tab stop (and decides the visible months). Supplying it makes the focus controlled
    /// (use <c>@bind-FocusedDate</c>). Default: the selected date, else today.
    /// </summary>
    [Parameter]
    public DateOnly? FocusedDate { get; set; }

    /// <summary>Two-way binding callback for <see cref="FocusedDate"/>.</summary>
    [Parameter]
    public EventCallback<DateOnly?> FocusedDateChanged { get; set; }

    /// <summary>The date that first holds the tab stop when the focus is uncontrolled. Default: the selected date, else today.</summary>
    [Parameter]
    public DateOnly? DefaultFocusedDate { get; set; }

    /// <summary>The earliest date that can be focused and selected. Earlier dates are disabled.</summary>
    [Parameter]
    public DateOnly? Min { get; set; }

    /// <summary>The latest date that can be focused and selected. Later dates are disabled.</summary>
    [Parameter]
    public DateOnly? Max { get; set; }

    /// <summary>
    /// Marks dates that can't be selected (booked days, holidays): they stay focusable and announced, with
    /// <c>aria-disabled</c> and <c>data-unavailable</c>.
    /// </summary>
    [Parameter]
    public Func<DateOnly, bool>? IsDateUnavailable { get; set; }

    /// <summary>
    /// Lets a range span unavailable dates. Default: false — while a range is being picked, the dates beyond the first
    /// unavailable date on either side of its first end are disabled.
    /// </summary>
    [Parameter]
    public bool AllowsNonContiguousRanges { get; set; }

    /// <summary>
    /// How many months are shown side by side (render one <see cref="BitBhCalendarGrid"/> per month, with its
    /// <see cref="BitBhCalendarGrid.Offset"/>). The previous / next buttons page by this many months. Default: 1.
    /// </summary>
    [Parameter]
    public int VisibleMonths { get; set; } = 1;

    /// <summary>The first day of the week. Default: the culture's (<see cref="DateTimeFormatInfo.FirstDayOfWeek"/>).</summary>
    [Parameter]
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// The culture the calendar is written in: month and day names, the first day of the week, and the calendar itself
    /// (<see cref="DateTimeFormatInfo.Calendar"/> — Gregorian, Persian, Umm al-Qura, Thai Buddhist …). Default:
    /// <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>Always shows six weeks per month, so grids side by side (or month after month) keep their height. Default: false.</summary>
    [Parameter]
    public bool FixedWeeks { get; set; }

    /// <summary>Renders the dates of the neighbouring months that fill the first and last weeks (not focusable, hidden from assistive technology). Default: false.</summary>
    [Parameter]
    public bool ShowOutsideDays { get; set; }

    /// <summary>Focuses the focused date when the calendar first renders.</summary>
    [Parameter]
    public bool AutoFocus { get; set; }

    /// <summary>Disables the calendar: nothing can be focused or selected.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes the calendar read-only: dates can be focused and browsed, not selected.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Marks the calendar invalid (in addition to EditContext validation and an out-of-range value).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the calendar required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Form field name: hidden inputs carry the selection as ISO dates.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Reading direction (mirrors ← / →). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>The heading, the previous / next buttons, the grids and anything else.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── Effective state ──────────────────────────────────────────────────────

    internal BitBhCalendarSystem System => BitBhCalendarSystem.For(Culture ?? Picker?.Culture);

    internal BitBhCalendarSelectionMode EffectiveMode
    {
        get
        {
            if (SelectionMode is { } mode) return mode;
            if (IsProvided(nameof(Range)) || DefaultRange is not null || RangeChanged.HasDelegate || OnRangeChange.HasDelegate || RangeExpression is not null)
                return BitBhCalendarSelectionMode.Range;
            if (IsProvided(nameof(Values)) || DefaultValues is not null || ValuesChanged.HasDelegate || OnValuesChange.HasDelegate || ValuesExpression is not null)
                return BitBhCalendarSelectionMode.Multiple;
            return BitBhCalendarSelectionMode.Single;
        }
    }

    // A calendar inside a date picker takes the picker's value unless it is given one of its own.
    private bool PickerValue => Picker is not null && !IsProvided(nameof(Value)) && !IsProvided(nameof(Range)) && !IsProvided(nameof(Values));

    /// <summary>The selected date (single selection).</summary>
    public DateOnly? CurrentValue => IsProvided(nameof(Value)) ? Value : PickerValue ? Picker!.Owner.CurrentValue : _value;

    /// <summary>The selected range (range selection).</summary>
    public BitBhDateRange? CurrentRange => IsProvided(nameof(Range)) ? Range : _range;

    /// <summary>The selected dates (multiple selection).</summary>
    public IReadOnlyList<DateOnly> CurrentValues => IsProvided(nameof(Values)) ? Values ?? [] : _values;

    internal DateOnly? EffectiveMin => Min ?? Picker?.Owner.Min;
    internal DateOnly? EffectiveMax => Max ?? Picker?.Owner.Max;
    private Func<DateOnly, bool>? EffectiveUnavailable => IsDateUnavailable ?? Picker?.Owner.IsDateUnavailable;
    internal DayOfWeek EffectiveFirstDayOfWeek => FirstDayOfWeek ?? Picker?.Owner.FirstDayOfWeek ?? System.Format.FirstDayOfWeek;
    internal BitBhDirection EffectiveDir => Dir ?? Picker?.Owner.EffectiveDir ?? CascadedDir;
    internal int EffectiveVisibleMonths => Math.Clamp(VisibleMonths, 1, 12);
    internal bool IsDisabled => Disabled || (Picker?.Owner.IsDisabled ?? false) || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    internal bool IsReadOnly => ReadOnly || (Picker?.Owner.IsReadOnly ?? false);
    internal bool IsRequired => Required || (FieldContext?.Required ?? false);
    internal bool IsInvalid => Invalid || IsValueInvalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);
    internal bool InPicker => Picker is not null;
    internal DateOnly Focused => _focused;
    internal DateOnly? Anchor => _anchor;
    internal DateOnly VisibleStart => _visibleStart;
    internal DateOnly VisibleEnd => System.LastOfMonth(System.AddMonths(_visibleStart, EffectiveVisibleMonths - 1));
    internal string Title => System.MonthRange(_visibleStart, System.AddMonths(_visibleStart, EffectiveVisibleMonths - 1));
    internal string HeadingId => $"{ComponentId}-heading";
    internal bool HasHeading => _hasHeading;
    internal BitBhCalendarContext Context => _context;

    // The dates that can be focused at all: the calendar's own range, narrowed by Min / Max.
    private DateOnly LowerBound => EffectiveMin is { } min && min > System.MinSupported ? min : System.MinSupported;
    private DateOnly UpperBound => EffectiveMax is { } max && max < System.MaxSupported ? max : System.MaxSupported;

    internal bool CanGoPrevious => !IsDisabled && _visibleStart > System.FirstOfMonth(LowerBound);
    internal bool CanGoNext => !IsDisabled && System.AddMonths(_visibleStart, EffectiveVisibleMonths) <= System.FirstOfMonth(UpperBound)
                               && System.LastOfMonth(System.AddMonths(_visibleStart, EffectiveVisibleMonths - 1)) < UpperBound;

    private DateOnly ReadToday()
    {
        var clock = GetService<TimeProvider>() ?? TimeProvider.System;
        return DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
    }

    private bool IsValueInvalid => _valueInvalid;

    private bool ComputeValueInvalid() => EffectiveMode switch
    {
        BitBhCalendarSelectionMode.Single => CurrentValue is { } v && !IsSelectableValue(v),
        BitBhCalendarSelectionMode.Range => CurrentRange is { } r && (!IsSelectableValue(r.Start) || !IsSelectableValue(r.End)
                                            || (!AllowsNonContiguousRanges && EffectiveUnavailable is not null && SpansUnavailable(r))),
        _ => CurrentValues.Any(v => !IsSelectableValue(v)),
    };

    private bool SpansUnavailable(BitBhDateRange range)
    {
        // Long ranges are not walked day by day: only what a person could pick by hand is checked.
        if (range.Days > 3700 || EffectiveUnavailable is not { } unavailable) return false;
        for (var d = range.Start; d <= range.End; d = d.AddDays(1))
            if (unavailable(d)) return true;
        return false;
    }

    private bool IsInBounds(DateOnly date) => date >= LowerBound && date <= UpperBound;

    private bool IsUnavailable(DateOnly date) => EffectiveUnavailable?.Invoke(date) ?? false;

    private bool IsSelectableValue(DateOnly date) => IsInBounds(date) && !IsUnavailable(date);

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        _value = DefaultValue;
        _range = DefaultRange;
        _values = DefaultValues ?? [];
        FieldContext?.SetControlFocus(FocusAsync);
    }

    protected override void OnParametersSet()
    {
        var system = System;
        if (!_initialized)
        {
            _initialized = true;
            _lastCalendar = system.Calendar;
            _lastSelection = SelectionAnchor;
            _lastFocusedParameter = FocusedDate;
            _today = ReadToday();
            var initial = (IsProvided(nameof(FocusedDate)) ? FocusedDate : null) ?? DefaultFocusedDate ?? SelectionAnchor ?? _today;
            _focused = Constrain(initial);
            _visibleStart = system.FirstOfMonth(_focused);
            ConstrainVisible();
        }
        else
        {
            // A new calendar (another culture) has other month boundaries: realign the visible months on the focus.
            if (!ReferenceEquals(_lastCalendar, system.Calendar) && _lastCalendar?.GetType() != system.Calendar.GetType())
            {
                _visibleStart = system.FirstOfMonth(_focused);
            }
            _lastCalendar = system.Calendar;

            // A focus set from outside, or a value changed from outside, brings its date into view.
            if (IsProvided(nameof(FocusedDate)) && FocusedDate != _lastFocusedParameter)
            {
                _lastFocusedParameter = FocusedDate;
                if (FocusedDate is { } date) Reveal(Constrain(date));
            }
            var selection = SelectionAnchor;
            if (selection != _lastSelection)
            {
                _lastSelection = selection;
                if (selection is { } date) Reveal(Constrain(date));
            }

            _focused = Constrain(_focused);
            ConstrainVisible();
        }

        if (ValueExpression is not null || RangeExpression is not null || ValuesExpression is not null || _binding is not null)
        {
            _binding ??= new BitBhFieldBinding(() => InvokeAsync(OnValidationStateChanged));
            _binding.Update(EditContext, (LambdaExpression?)ValueExpression ?? (LambdaExpression?)RangeExpression ?? ValuesExpression);
            _binding.ReportTo(FieldContext);
        }
    }

    private void OnValidationStateChanged()
    {
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    private DateOnly? SelectionAnchor => EffectiveMode switch
    {
        BitBhCalendarSelectionMode.Single => CurrentValue,
        BitBhCalendarSelectionMode.Range => CurrentRange?.Start,
        _ => null,
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if ((firstRender && AutoFocus) || _focusPending)
        {
            _focusPending = false;
            if (!IsDisabled) await Interop.FocusAsync(CellId(_focused));
        }
    }

    /// <summary>Focuses the focused date.</summary>
    public Task FocusAsync() => IsDisabled ? Task.CompletedTask : Interop.FocusAsync(CellId(_focused)).AsTask();

    /// <summary>Shows the month of <paramref name="date"/> and moves the focused date to it (clamped to <see cref="Min"/> / <see cref="Max"/>).</summary>
    public Task GoToDateAsync(DateOnly date)
    {
        Reveal(Constrain(date));
        StateHasChanged();
        return FocusedDateChanged.InvokeAsync(_focused);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _today = ReadToday();
        _valueInvalid = ComputeValueInvalid();

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhCalendarContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        // Not fixed: the parts render the visible months and the selection, which change without new parameters.
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();

        // Like a native disabled control, a disabled calendar posts nothing.
        if (!string.IsNullOrEmpty(Name) && !IsDisabled)
        {
            var mode = EffectiveMode;
            if (mode == BitBhCalendarSelectionMode.Multiple)
            {
                foreach (var date in CurrentValues.Order())
                {
                    builder.OpenElement(60, "input");
                    builder.AddAttribute(61, "type", "hidden");
                    builder.AddAttribute(62, "name", Name);
                    builder.AddAttribute(63, "value", Iso(date));
                    builder.CloseElement();
                }
            }
            else
            {
                builder.OpenElement(70, "input");
                builder.AddAttribute(71, "type", "hidden");
                builder.AddAttribute(72, "name", Name);
                builder.AddAttribute(73, "value", mode == BitBhCalendarSelectionMode.Range
                    ? CurrentRange?.ToString() ?? string.Empty
                    : CurrentValue is { } value ? Iso(value) : string.Empty);
                builder.CloseElement();
            }
        }
    }

    internal static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "group";
        if (FieldContext?.LabelledBy is { } label && GetAdditionalAttribute("aria-label") is null) attrs["aria-labelledby"] = label;
        if (FieldContext?.DescribedBy is { } description) attrs["aria-describedby"] = description;
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataValue(attrs, "selection-mode", EffectiveMode switch
        {
            BitBhCalendarSelectionMode.Range => "range",
            BitBhCalendarSelectionMode.Multiple => "multiple",
            _ => "single",
        });
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "readonly", IsReadOnly);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "required", IsRequired);
        SetDataFlag(attrs, "picking", _anchor is not null);
        return attrs;
    }

    // ── Parts ────────────────────────────────────────────────────────────────

    internal void RegisterHeading(bool present)
    {
        if (_hasHeading == present) return;
        _hasHeading = present;
        StateHasChanged();
    }

    internal string CellId(DateOnly date) => $"{ComponentId}-{Iso(date)}";

    /// <summary>The first day of the month the grid at <paramref name="offset"/> shows.</summary>
    internal DateOnly MonthAt(int offset) => System.AddMonths(_visibleStart, offset);

    internal IReadOnlyList<BitBhCalendarWeekday> Weekdays(BitBhCalendarWeekdayFormat format)
    {
        var dtf = System.Format;
        var first = (int)EffectiveFirstDayOfWeek;
        var days = new BitBhCalendarWeekday[7];
        for (var i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)((first + i) % 7);
            days[i] = new BitBhCalendarWeekday
            {
                DayOfWeek = day,
                Name = dtf.GetDayName(day),
                Text = format switch
                {
                    BitBhCalendarWeekdayFormat.Full => dtf.GetDayName(day),
                    BitBhCalendarWeekdayFormat.Abbreviated => dtf.GetAbbreviatedDayName(day),
                    _ => dtf.GetShortestDayName(day),
                },
            };
        }
        return days;
    }

    /// <summary>
    /// The weeks of the month starting at <paramref name="month"/>: seven dates each (null past the calendar's range),
    /// from the week of its first day to the week of its last (six weeks with <see cref="FixedWeeks"/>).
    /// </summary>
    internal List<DateOnly?[]> WeeksOf(DateOnly month)
    {
        var system = System;
        var first = system.FirstOfMonth(month);
        var leading = ((int)first.DayOfWeek - (int)EffectiveFirstDayOfWeek + 7) % 7;
        var days = system.DaysInMonth(first);
        var count = FixedWeeks ? 6 : (leading + days + 6) / 7;
        var weeks = new List<DateOnly?[]>(count);
        var start = first.DayNumber - leading;
        for (var w = 0; w < count; w++)
        {
            var week = new DateOnly?[7];
            for (var d = 0; d < 7; d++)
            {
                var number = start + (w * 7) + d;
                week[d] = number < DateOnly.MinValue.DayNumber || number > DateOnly.MaxValue.DayNumber ? null : DateOnly.FromDayNumber(number);
            }
            weeks.Add(week);
        }
        return weeks;
    }

    /// <summary>The state of <paramref name="date"/> in the grid of the month starting at <paramref name="month"/>.</summary>
    internal BitBhCalendarDay DayOf(DateOnly date, DateOnly month)
    {
        var system = System;
        var outside = system.FirstOfMonth(date) != system.FirstOfMonth(month);
        var disabled = IsDisabled || !IsInBounds(date) || !IsWithinAnchorBounds(date);
        var unavailable = IsUnavailable(date);

        bool selected = false, start = false, end = false, middle = false, highlighted = false;
        switch (EffectiveMode)
        {
            case BitBhCalendarSelectionMode.Single:
                selected = start = end = CurrentValue == date;
                break;
            case BitBhCalendarSelectionMode.Range:
                var preview = PreviewRange;
                var shown = preview ?? CurrentRange;
                if (shown is not null && shown.Contains(date))
                {
                    selected = true;
                    start = date == shown.Start;
                    end = date == shown.End;
                    middle = !start && !end;
                    highlighted = preview is not null;
                }
                break;
            default:
                selected = start = end = CurrentValues.Contains(date);
                break;
        }

        return new BitBhCalendarDay
        {
            Date = date,
            Month = system.FirstOfMonth(month),
            Text = system.DayNumber(date),
            Label = system.LongDate(date),
            IsToday = date == _today,
            IsOutsideMonth = outside,
            IsSelected = selected,
            IsSelectionStart = start,
            IsSelectionEnd = end,
            IsRangeMiddle = middle,
            IsHighlighted = highlighted,
            IsUnavailable = unavailable,
            IsDisabled = disabled,
            IsFocused = !outside && date == _focused,
            IsInvalid = selected && IsValueInvalid,
        };
    }

    /// <summary>The range between the first press and the date under the pointer or focus, while a range is being picked.</summary>
    private BitBhDateRange? PreviewRange
    {
        get
        {
            if (_anchor is not { } anchor) return null;
            var end = _previewEnd ?? anchor;
            if (_anchorBounds is { } bounds)
            {
                if (bounds.Low is { } low && end <= low) end = low.AddDays(1);
                if (bounds.High is { } high && end >= high) end = high.AddDays(-1);
            }
            return new BitBhDateRange(anchor, end);
        }
    }

    private bool IsWithinAnchorBounds(DateOnly date)
        => _anchorBounds is not { } bounds || ((bounds.Low is not { } low || date > low) && (bounds.High is not { } high || date < high));

    /// <summary>The nearest unavailable dates before and after <paramref name="anchor"/> (a range can't span them).</summary>
    private (DateOnly? Low, DateOnly? High)? BoundsAround(DateOnly anchor)
    {
        if (AllowsNonContiguousRanges || EffectiveUnavailable is not { } unavailable) return null;
        const int Limit = 3700;
        DateOnly? low = null, high = null;
        for (var (d, i) = (anchor, 0); d > LowerBound && i < Limit; i++)
        {
            d = d.AddDays(-1);
            if (unavailable(d)) { low = d; break; }
        }
        for (var (d, i) = (anchor, 0); d < UpperBound && i < Limit; i++)
        {
            d = d.AddDays(1);
            if (unavailable(d)) { high = d; break; }
        }
        return (low, high);
    }

    // ── Focus and visible months ─────────────────────────────────────────────

    private DateOnly Constrain(DateOnly date)
    {
        date = System.ClampSupported(date);
        if (date < LowerBound) return LowerBound;
        if (date > UpperBound) return UpperBound;
        return date;
    }

    /// <summary>Moves the focused date, paging the visible months so it is in view.</summary>
    private void Reveal(DateOnly date)
    {
        var system = System;
        _focused = date;
        var month = system.FirstOfMonth(date);
        var last = system.AddMonths(_visibleStart, EffectiveVisibleMonths - 1);
        // Back past the first visible month: the date's month becomes the last one shown; forward past the last: the first.
        if (month < _visibleStart) _visibleStart = system.AddMonths(month, -(EffectiveVisibleMonths - 1));
        else if (month > last) _visibleStart = month;
        ConstrainVisible();
    }

    /// <summary>Keeps the visible months inside <see cref="Min"/> / <see cref="Max"/> when they fit.</summary>
    private void ConstrainVisible()
    {
        var system = System;
        _visibleStart = system.FirstOfMonth(_visibleStart);
        var lastAllowed = system.FirstOfMonth(UpperBound);
        var lastShown = system.AddMonths(_visibleStart, EffectiveVisibleMonths - 1);
        if (lastShown > lastAllowed) _visibleStart = system.FirstOfMonth(system.AddMonths(lastAllowed, -(EffectiveVisibleMonths - 1)));
        var firstAllowed = system.FirstOfMonth(LowerBound);
        if (_visibleStart < firstAllowed) _visibleStart = firstAllowed;

        // The focused date stays on a visible month.
        var visibleLast = system.AddMonths(_visibleStart, EffectiveVisibleMonths - 1);
        var focusedMonth = system.FirstOfMonth(_focused);
        if (focusedMonth < _visibleStart || focusedMonth > visibleLast)
            _focused = Constrain(focusedMonth < _visibleStart ? _visibleStart : system.LastOfMonth(visibleLast));
    }

    private async Task MoveFocusAsync(DateOnly target)
    {
        target = Constrain(target);
        // Pinned at Min / Max (or already there): the focus stays where it is.
        if (target == _focused) return;
        Reveal(target);
        if (_anchor is not null) _previewEnd = _focused;
        _focusPending = true;
        StateHasChanged();
        await FocusedDateChanged.InvokeAsync(_focused);
    }

    private async Task PageAsync(int direction)
    {
        if (IsDisabled) return;
        var system = System;
        var months = EffectiveVisibleMonths * direction;
        _visibleStart = system.AddMonths(_visibleStart, months);
        _focused = Constrain(system.AddMonths(_focused, months));
        ConstrainVisible();
        StateHasChanged();
        await FocusedDateChanged.InvokeAsync(_focused);
        // The focus stays on the button: say which months are shown now.
        if (GetService<IBitBhAnnouncer>() is { } announcer) await announcer.AnnounceAsync(Title);
    }

    internal Task GoPreviousAsync() => CanGoPrevious ? PageAsync(-1) : Task.CompletedTask;

    internal Task GoNextAsync() => CanGoNext ? PageAsync(1) : Task.CompletedTask;

    // ── Cell interaction ─────────────────────────────────────────────────────

    internal async Task HandleKeyDownAsync(DateOnly date, KeyboardEventArgs e)
    {
        if (IsDisabled || e.CtrlKey || e.MetaKey || e.AltKey) return;
        var system = System;
        var rtl = EffectiveDir == BitBhDirection.Rtl;
        var weekStart = ((int)date.DayOfWeek - (int)EffectiveFirstDayOfWeek + 7) % 7;
        switch (e.Key)
        {
            case "ArrowLeft": await MoveFocusAsync(system.AddDays(date, rtl ? 1 : -1)); break;
            case "ArrowRight": await MoveFocusAsync(system.AddDays(date, rtl ? -1 : 1)); break;
            case "ArrowUp": await MoveFocusAsync(system.AddDays(date, -7)); break;
            case "ArrowDown": await MoveFocusAsync(system.AddDays(date, 7)); break;
            case "PageUp": await MoveFocusAsync(e.ShiftKey ? system.AddYears(date, -1) : system.AddMonths(date, -1)); break;
            case "PageDown": await MoveFocusAsync(e.ShiftKey ? system.AddYears(date, 1) : system.AddMonths(date, 1)); break;
            case "Home": await MoveFocusAsync(system.AddDays(date, -weekStart)); break;
            case "End": await MoveFocusAsync(system.AddDays(date, 6 - weekStart)); break;
            case "Enter" or " ": await SelectAsync(date); break;
            case "Escape" when _anchor is not null:
                _anchor = null;
                _previewEnd = null;
                _anchorBounds = null;
                StateHasChanged();
                break;
        }
    }

    internal async Task HandleFocusAsync(DateOnly date)
    {
        if (date == _focused || IsDisabled || !IsInBounds(date)) return;
        _focused = date;
        if (_anchor is not null) _previewEnd = date;
        StateHasChanged();
        await FocusedDateChanged.InvokeAsync(_focused);
    }

    internal void HandlePointerEnter(DateOnly date)
    {
        if (_anchor is null || !IsInBounds(date) || _previewEnd == date) return;
        _previewEnd = date;
        StateHasChanged();
    }

    /// <summary>Whether pressing <paramref name="date"/> selects it.</summary>
    internal bool IsSelectable(DateOnly date)
        => !IsDisabled && !IsReadOnly && IsInBounds(date) && !IsUnavailable(date) && IsWithinAnchorBounds(date);

    internal async Task SelectAsync(DateOnly date)
    {
        if (!IsSelectable(date)) return;
        if (date != _focused)
        {
            _focused = date;
            await FocusedDateChanged.InvokeAsync(_focused);
        }

        switch (EffectiveMode)
        {
            case BitBhCalendarSelectionMode.Single:
                await SetValueAsync(date);
                break;

            case BitBhCalendarSelectionMode.Range:
                if (_anchor is not { } anchor)
                {
                    _anchor = date;
                    _previewEnd = date;
                    _anchorBounds = BoundsAround(date);
                    StateHasChanged();
                    return;
                }
                _anchor = null;
                _previewEnd = null;
                _anchorBounds = null;
                await SetRangeAsync(new BitBhDateRange(anchor, date));
                break;

            default:
                var values = CurrentValues.Contains(date)
                    ? CurrentValues.Where(v => v != date).ToArray()
                    : CurrentValues.Append(date).Order().ToArray();
                await SetValuesAsync(values);
                break;
        }
    }

    private async Task SetValueAsync(DateOnly date)
    {
        var changed = CurrentValue != date;
        _lastSelection = date;
        if (PickerValue)
        {
            StateHasChanged();
            await AnnounceAsync(Strings.SelectedDate(System.LongDate(date)));
            await Picker!.Owner.SelectDateAsync(date);
            return;
        }
        if (!IsProvided(nameof(Value))) _value = date;
        if (changed)
        {
            await ValueChanged.InvokeAsync(date);
            await OnValueChange.InvokeAsync(date);
            NotifyFieldChanged();
        }
        StateHasChanged();
        if (changed) await AnnounceAsync(Strings.SelectedDate(System.LongDate(date)));
    }

    private async Task SetRangeAsync(BitBhDateRange range)
    {
        var changed = CurrentRange != range;
        _lastSelection = range.Start;
        var message = Strings.SelectedRange(System.LongDate(range.Start), System.LongDate(range.End));
        if (!IsProvided(nameof(Range))) _range = range;
        if (changed)
        {
            await RangeChanged.InvokeAsync(range);
            await OnRangeChange.InvokeAsync(range);
            NotifyFieldChanged();
        }
        StateHasChanged();
        if (changed) await AnnounceAsync(message);
    }

    private async Task SetValuesAsync(IReadOnlyList<DateOnly> values)
    {
        if (!IsProvided(nameof(Values))) _values = values;
        await ValuesChanged.InvokeAsync(values);
        await OnValuesChange.InvokeAsync(values);
        NotifyFieldChanged();
        StateHasChanged();
    }

    private void NotifyFieldChanged()
    {
        _binding?.NotifyChanged();
        _binding?.ReportTo(FieldContext);
    }

    private async Task AnnounceAsync(string message)
    {
        if (GetService<IBitBhAnnouncer>() is { } announcer) await announcer.AnnounceAsync(message);
    }

    /// <summary>Cancels a range whose first end has been picked (as Escape does).</summary>
    internal void CancelPicking()
    {
        if (_anchor is null) return;
        _anchor = null;
        _previewEnd = null;
        _anchorBounds = null;
        StateHasChanged();
    }

    public virtual void Dispose()
    {
        _binding?.Dispose();
        FieldContext?.SetControlFocus(null);
    }
}
