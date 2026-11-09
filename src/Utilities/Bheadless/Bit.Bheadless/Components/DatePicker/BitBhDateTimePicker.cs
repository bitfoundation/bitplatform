using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// A headless date-and-time picker (React Aria's DatePicker with <c>granularity="minute"</c>): a
/// <see cref="BitBhDateTimeField"/> — date and time segments, <c>@bind-Value</c> a <c>DateTime?</c>,
/// <see cref="BitBhDateTimeField.Granularity"/>, <see cref="BitBhDateTimeField.HourCycle"/> — with a button that opens a
/// calendar in a popover. It takes the parts of <see cref="BitBhDatePicker"/>: <see cref="BitBhDatePickerGroup"/>,
/// <see cref="BitBhDateFieldSegments"/>, <see cref="BitBhDatePickerTrigger"/> and <see cref="BitBhDatePickerContent"/>
/// with a <see cref="BitBhCalendar"/> in it.
///
/// <para><b>Picking:</b> the calendar picks the date and the time is kept: the time in the segments, else the time of
/// <c>PlaceholderValue</c> (midnight by default). The calendar takes the date of <c>Min</c> / <c>Max</c>,
/// <see cref="IsDateUnavailable"/>, <c>Culture</c> and <see cref="FirstDayOfWeek"/> unless it is given its own. Focus and
/// dismissal are those of <see cref="BitBhDatePicker"/>.</para>
///
/// <code>
/// &lt;BitBhField&gt;
///     &lt;BitBhLabel&gt;Appointment&lt;/BitBhLabel&gt;
///     &lt;BitBhDateTimePicker @bind-Value="appointment" Granularity="BitBhTimeGranularity.Minute"&gt;
///         &lt;BitBhDatePickerGroup class="picker-group"&gt;
///             &lt;BitBhDateFieldSegments /&gt;
///             &lt;BitBhDatePickerTrigger class="picker-trigger"&gt;📅&lt;/BitBhDatePickerTrigger&gt;
///         &lt;/BitBhDatePickerGroup&gt;
///         &lt;BitBhDatePickerContent class="picker-popover"&gt;
///             &lt;BitBhCalendar&gt; … &lt;/BitBhCalendar&gt;
///         &lt;/BitBhDatePickerContent&gt;
///     &lt;/BitBhDateTimePicker&gt;
/// &lt;/BitBhField&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> as <see cref="BitBhDatePicker"/>.</para>
/// </summary>
public class BitBhDateTimePicker : BitBhDateTimeField, IBitBhDatePickerOwner
{
    private BitBhDatePickerContext? _picker;
    private bool _open;
    private bool _hasGroup;

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

    /// <summary>
    /// Marks dates that can't be picked: the calendar shows them unavailable, and a typed value on one marks the field invalid.
    /// </summary>
    [Parameter]
    public Func<DateOnly, bool>? IsDateUnavailable { get; set; }

    /// <summary>The first day of the calendar's week. Default: the culture's.</summary>
    [Parameter]
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>Closes the popover when a date is picked in the calendar. Default: true.</summary>
    [Parameter]
    public bool CloseOnSelect { get; set; } = true;

    /// <summary>Whether the calendar popover is open.</summary>
    public bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    private protected override bool IsGroupRoot => false;

    private protected override bool IsValueUnavailable(DateTime value) => IsDateUnavailable?.Invoke(DateOnly.FromDateTime(value)) ?? false;

    internal BitBhDatePickerContext PickerContext => _picker ??= new BitBhDatePickerContext(this);

    internal string GroupId => $"{ComponentId}-group";
    internal string TriggerId => $"{ComponentId}-trigger";
    internal string ContentId => $"{ComponentId}-content";

    protected override void OnInitialized()
    {
        _open = DefaultOpen;
        base.OnInitialized();
    }

    private protected override RenderFragment RootContent => builder =>
    {
        builder.OpenComponent<CascadingValue<BitBhDatePickerContext>>(0);
        builder.AddComponentParameter(1, "Value", PickerContext);
        // Not fixed: the parts render the open state and the value, which change without new parameters.
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    };

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        return attrs;
    }

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

    /// <summary>A date picked in the calendar: the time in the segments (else the placeholder's) is kept.</summary>
    private async Task SelectDateAsync(DateOnly date)
    {
        if (IsDisabled || IsReadOnly) return;
        var time = CurrentValue is { } value ? TimeOnly.FromDateTime(value)
            : State.ComposeTime() ?? TimeOnly.FromDateTime(PlaceholderValue ?? DateTime.Today);
        await SetValueAsync(date.ToDateTime(time));
        if (CloseOnSelect) await CloseAsync();
    }

    // ── What the parts and the calendar read ─────────────────────────────────

    bool IBitBhDatePickerOwner.IsRange => false;
    string IBitBhDatePickerOwner.GroupId => GroupId;
    string IBitBhDatePickerOwner.TriggerId => TriggerId;
    string IBitBhDatePickerOwner.ContentId => ContentId;
    bool IBitBhDatePickerOwner.HasGroup => _hasGroup;
    string? IBitBhDatePickerOwner.LabelledBy => Field?.LabelledBy;
    DateOnly? IBitBhDatePickerOwner.SelectedDate => CurrentValue is { } value ? DateOnly.FromDateTime(value) : null;
    BitBhDateRange? IBitBhDatePickerOwner.SelectedRange => null;
    DateOnly? IBitBhDatePickerOwner.MinDate => Min is { } min ? DateOnly.FromDateTime(min) : null;
    DateOnly? IBitBhDatePickerOwner.MaxDate => Max is { } max ? DateOnly.FromDateTime(max) : null;
    bool IBitBhDatePickerOwner.AllowsNonContiguousRanges => false;
    BitBhDirection IBitBhDatePickerOwner.EffectiveDir => EffectiveDir;
    Task IBitBhDatePickerOwner.ToggleAsync() => IsOpen ? CloseAsync() : OpenAsync();
    Task IBitBhDatePickerOwner.SelectDateAsync(DateOnly date) => SelectDateAsync(date);
    Task IBitBhDatePickerOwner.SelectRangeAsync(BitBhDateRange range) => Task.CompletedTask;

    void IBitBhDatePickerOwner.RegisterGroup(bool present)
    {
        if (_hasGroup == present) return;
        _hasGroup = present;
        StateHasChanged();
    }

    void IBitBhDatePickerOwner.AddGroupState(Dictionary<string, object> attrs, Dictionary<string, object>? own)
    {
        AddGroupAttributes(attrs, own);
        AddStateAttributes(attrs);
        SetDataState(attrs, IsOpen);
    }
}
