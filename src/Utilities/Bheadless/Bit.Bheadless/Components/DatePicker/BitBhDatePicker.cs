using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// A headless date picker (React Aria's DatePicker): a segmented date input with a button that opens a calendar in a
/// popover. It is a <see cref="BitBhDateField"/> — typing, keys, value, limits, forms — plus the popover.
///
/// <para><b>Parts:</b> <see cref="BitBhDatePickerGroup"/> (the <c>role="group"</c> around the segments and the button,
/// labelled by the field's label; Alt+↓ in it opens the calendar), <see cref="BitBhDateFieldSegments"/>,
/// <see cref="BitBhDatePickerTrigger"/> ("Choose date") and <see cref="BitBhDatePickerContent"/> — a non-modal
/// <c>role="dialog"</c> popover anchored to the group, holding a <see cref="BitBhCalendar"/>. The calendar takes the
/// picker's value, <c>Min</c> / <c>Max</c>, <see cref="IsDateUnavailable"/>, <c>Culture</c> and
/// <see cref="FirstDayOfWeek"/> unless it is given its own.</para>
///
/// <para><b>Focus:</b> opening moves the focus to the calendar's selected date (else today); Escape, a press outside
/// or the focus leaving closes it and the focus returns to the button; picking a date closes it
/// (<see cref="CloseOnSelect"/>).</para>
///
/// <code>
/// &lt;BitBhField&gt;
///     &lt;BitBhLabel&gt;Departure&lt;/BitBhLabel&gt;
///     &lt;BitBhDatePicker @bind-Value="departure" Min="today"&gt;
///         &lt;BitBhDatePickerGroup class="picker-group"&gt;
///             &lt;BitBhDateFieldSegments /&gt;
///             &lt;BitBhDatePickerTrigger class="picker-trigger"&gt;📅&lt;/BitBhDatePickerTrigger&gt;
///         &lt;/BitBhDatePickerGroup&gt;
///         &lt;BitBhDatePickerContent class="picker-popover"&gt;
///             &lt;BitBhCalendar&gt; … &lt;/BitBhCalendar&gt;
///         &lt;/BitBhDatePickerContent&gt;
///     &lt;/BitBhDatePicker&gt;
/// &lt;/BitBhField&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c>, <c>data-disabled</c>, <c>data-readonly</c>,
/// <c>data-invalid</c>, <c>data-required</c>, <c>data-focused</c> on the root and the group.</para>
/// </summary>
public class BitBhDatePicker : BitBhDateField, IBitBhDatePickerOwner
{
    private BitBhDatePickerContext? _picker;

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
    /// Marks dates that can't be picked: the calendar shows them unavailable, and a typed one marks the field invalid.
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
    public bool IsOpen => PickerContext.IsOpen;

    private protected override bool IsGroupRoot => false;

    private protected override bool IsValueUnavailable(DateOnly value) => IsDateUnavailable?.Invoke(value) ?? false;

    internal BitBhDatePickerContext PickerContext => _picker ??= new BitBhDatePickerContext(this);

    protected override void OnInitialized()
    {
        // Created once the parameters are in: the popover starts from DefaultOpen.
        _picker = new BitBhDatePickerContext(this);
        base.OnInitialized();
    }

    private protected override RenderFragment RootContent => PickerContext.Cascade(ChildContent);

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    /// <summary>Opens the calendar popover.</summary>
    public Task OpenAsync() => PickerContext.OpenAsync();

    /// <summary>Closes the calendar popover.</summary>
    public Task CloseAsync() => PickerContext.CloseAsync();

    /// <summary>A date picked in the calendar.</summary>
    private async Task SelectDateAsync(DateOnly date)
    {
        if (IsDisabled || IsReadOnly) return;
        await SetValueAsync(date);
        await PickerContext.PickedAsync();
    }

    // ── What the popover and the calendar read ───────────────────────────────

    bool IBitBhDatePickerOwner.IsOpenControlled => IsProvided(nameof(Open));
    string IBitBhDatePickerOwner.RootId => ComponentId;
    void IBitBhDatePickerOwner.Render() => StateHasChanged();
    bool IBitBhDatePickerOwner.IsRange => false;
    string? IBitBhDatePickerOwner.LabelledBy => Field?.LabelledBy;
    DateOnly? IBitBhDatePickerOwner.SelectedDate => CurrentValue;
    BitBhDateRange? IBitBhDatePickerOwner.SelectedRange => null;
    DateOnly? IBitBhDatePickerOwner.MinDate => Min;
    DateOnly? IBitBhDatePickerOwner.MaxDate => Max;
    bool IBitBhDatePickerOwner.AllowsNonContiguousRanges => false;
    BitBhDirection IBitBhDatePickerOwner.EffectiveDir => EffectiveDir;
    Task IBitBhDatePickerOwner.SelectDateAsync(DateOnly date) => SelectDateAsync(date);
    Task IBitBhDatePickerOwner.SelectRangeAsync(BitBhDateRange range) => Task.CompletedTask;

    void IBitBhDatePickerOwner.AddGroupState(Dictionary<string, object> attrs, Dictionary<string, object>? own)
    {
        AddGroupAttributes(attrs, own);
        AddStateAttributes(attrs);
    }
}
