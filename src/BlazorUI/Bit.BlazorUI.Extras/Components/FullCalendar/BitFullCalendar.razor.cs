using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitFullCalendar : IDisposable
{
    /// <summary>
    /// Events displayed in the calendar. Assign a list from parent state; updates are synced on each
    /// render when the reference or contents change. User-driven add, edit, and delete actions are
    /// reported through <see cref="OnChange"/> - update this list (or your backing store) in the handler
    /// to keep the UI in sync.
    /// </summary>
    [Parameter] public List<BitFullCalendarEvent>? Events { get; set; }

    /// <summary>
    /// Culture for the calendar. Accepts any CultureInfo, e.g. new CultureInfo("fa-IR").
    /// NOTE: do NOT use this parameter when the component is rendered with
    /// @rendermode="InteractiveServer" - CultureInfo is not JSON-serializable.
    /// Use <see cref="CultureName"/> instead for server-interactive scenarios.
    /// </summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Culture name string (e.g. "fa-IR", "ar-SA", "fr-FR").
    /// Preferred over <see cref="Culture"/> when using @rendermode="InteractiveServer"
    /// because plain strings are safely serialized by Blazor's parameter persistence.
    /// When both are supplied, CultureName takes precedence.
    /// Blazor WebAssembly hosts must set <c>BlazorWebAssemblyLoadAllGlobalizationData</c> to
    /// <c>true</c> (or load a custom ICU shard) for cultures outside the default EFIGS/CJK shards.
    /// </summary>
    [Parameter] public string? CultureName { get; set; }

    /// <summary>
    /// Localized strings for calendar UI labels, buttons, dialogs, filters, and accessibility text.
    /// Defaults to English; override individual properties on a <see cref="BitFullCalendarTexts"/>
    /// instance to localize the component without replacing built-in dialogs.
    /// </summary>
    [Parameter] public BitFullCalendarTexts Texts { get; set; } = new();

    /// <summary>
    /// Ordered list of event colors shown in pickers, filters, agenda headers, badges, and bullets.
    /// Each entry has its own <see cref="BitFullCalendarColorOption.Id"/> (matched against
    /// <see cref="BitFullCalendarEvent.Color"/>), <see cref="BitFullCalendarColorOption.Title"/>
    /// (the display name shown verbatim - for example <c>"SkyBlue"</c>), and
    /// <see cref="BitFullCalendarColorOption.Value"/> (any CSS color value used for swatches and badges).
    /// When <c>null</c> or empty, <see cref="BitFullCalendarColorOption.Defaults"/> is used.
    /// </summary>
    [Parameter] public IReadOnlyList<BitFullCalendarColorOption>? EventColorOptions { get; set; }

    /// <summary>
    /// Resources displayed as rows in the resource timeline view. When <c>null</c> or empty,
    /// the resource timeline tab is hidden from the header. Each event's
    /// <see cref="BitFullCalendarEvent.Resource"/> is matched against the resource <c>Id</c>.
    /// </summary>
    [Parameter] public IReadOnlyList<BitFullCalendarResource>? Resources { get; set; }

    /// <summary>
    /// Raised when a user adds, edits, or deletes an event in the calendar UI.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarChangeEventArgs> OnChange { get; set; }

    /// <summary>
    /// When assigned, the built-in add dialog is suppressed. The callback receives a draft
    /// <see cref="BitFullCalendarEvent"/> with <see cref="BitFullCalendarEvent.StartDate"/> and
    /// <see cref="BitFullCalendarEvent.EndDate"/> set from the interaction (for example the clicked day/week slot);
    /// <see cref="BitFullCalendarEvent.Id"/> is empty and other fields are left at defaults.
    /// Consumers should show their own UI and
    /// raise <see cref="OnChange"/> (or mutate <see cref="Events"/> bound to parent state) after persisting changes.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarEvent?> OnAddClick { get; set; }

    /// <summary>
    /// When assigned, the built-in event details dialog is suppressed when an event is clicked.
    /// The callback receives the clicked <see cref="BitFullCalendarEvent"/>. Consumers should
    /// show their own event details UI. This applies to all views (day, week, month, agenda) and
    /// to multi-day event rows and event list dialogs.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }

    /// <summary>
    /// Raised when the visible date range changes - for example when the user navigates
    /// with prev/next/today buttons or switches views. The callback receives the inclusive
    /// start and end dates of the new range together with the active view.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarDateChangeEventArgs> OnDateChange { get; set; }

    /// <summary>
    /// Optional template for customizing event rendering in the day view.
    /// When provided, replaces the default event card content inside the time-grid blocks.
    /// </summary>
    [Parameter] public RenderFragment<BitFullCalendarEvent>? DayEventTemplate { get; set; }

    /// <summary>
    /// Optional template for customizing event rendering in the week view.
    /// When provided, replaces the default event card content inside the time-grid blocks.
    /// </summary>
    [Parameter] public RenderFragment<BitFullCalendarEvent>? WeekEventTemplate { get; set; }

    /// <summary>
    /// Optional template for customizing event rendering in the month view.
    /// When provided, replaces the default event badge content inside month grid cells.
    /// </summary>
    [Parameter] public RenderFragment<BitFullCalendarEvent>? MonthEventTemplate { get; set; }

    /// <summary>
    /// Optional template for customizing event rendering in the resource timeline view.
    /// When provided, replaces the default event card content inside the timeline blocks.
    /// </summary>
    [Parameter] public RenderFragment<BitFullCalendarEvent>? TimelineEventTemplate { get; set; }

    /// <summary>
    /// When <c>true</c>, the built-in color and attendee filter dropdowns are hidden from the calendar header.
    /// Consumers can provide their own external filter UI and pass pre-filtered events to the calendar.
    /// </summary>
    [Parameter] public bool HideFilters { get; set; }

    /// <summary>
    /// When <c>true</c>, the built-in settings gear button is hidden from the calendar header.
    /// Consumers can still drive settings programmatically through the <see cref="Options"/> object.
    /// </summary>
    [Parameter] public bool HideSettings { get; set; }

    /// <summary>
    /// Configuration options controlling initial calendar preferences
    /// such as dark mode, time format, badge variant, day start hour, and agenda grouping.
    /// Values are applied when the component initializes or when a new instance is assigned.
    /// </summary>
    [Parameter] public BitFullCalendarOptions Options { get; set; } = new();

    /// <summary>
    /// Initial layout mode. <see cref="BitFullCalendarMode.Event"/> shows the standard
    /// day/week/month/year/agenda views. <see cref="BitFullCalendarMode.Timeline"/> switches to
    /// the resource × time layout (day, week, month) and requires <see cref="Resources"/> to contain
    /// at least one entry; otherwise the Timeline tab and mode have no effect.
    /// </summary>
    [Parameter] public BitFullCalendarMode? InitialMode { get; set; }

    public BitFullCalendarState State { get; set; } = new();
    private BitFullCalendarChangeNotifier _changeNotifier = default!;
    private BitFullCalendarColorScheme _colorScheme = new(null);
    private BitFullCalendarOptions? _appliedOptions;

    private BitCascadingValueList BuildCascadingValues() => new()
    {
        { State },
        { Texts },
        { _changeNotifier },
        { _colorScheme },
        { Options },
        { HideFilters, "HideFilters" },
        { HideSettings, "HideSettings" },
        { OnAddClick, "OnAddClick" },
        { OnEventClick, "OnEventClick" },
    };

    private CultureInfo ResolveCulture()
    {
        if (CultureName is { Length: > 0 } name)
        {
            try
            {
                return new CultureInfo(name);
            }
            catch (CultureNotFoundException)
            {
                // Invalid CultureName supplied; fall back to the explicit Culture or the current UI culture.
            }
        }

        return Culture ?? CultureInfo.CurrentUICulture;
    }

    protected override void OnInitialized()
    {
        State.Initialize(Events ?? [], ResolveCulture());
        ApplyOptions();
        if (InitialMode.HasValue)
            State.SetMode(InitialMode.Value);
        _changeNotifier = new BitFullCalendarChangeNotifier(State, args => OnChange.InvokeAsync(args));
        State.OnStateChanged += HandleStateChanged;
        State.OnDateRangeChanged += HandleDateRangeChanged;
    }

    protected override void OnParametersSet()
    {
        _colorScheme = new BitFullCalendarColorScheme(EventColorOptions);
        var resolved = ResolveCulture();
        if (!string.Equals(resolved.Name, State.Culture.Name, StringComparison.Ordinal))
            State.SetCulture(resolved);

        if (Events is not null)
            State.SyncEvents(Events);

        State.SyncResources(Resources);

        ApplyOptions();
    }

    private void ApplyOptions()
    {
        if (ReferenceEquals(Options, _appliedOptions))
            return;

        _appliedOptions = Options;
        State.SetUse24HourFormat(Options.Use24HourFormat);
        State.SetBadgeVariant(Options.BadgeVariant);
        State.SetStartOfDayHour(Options.StartOfDayHour);
        State.SetAgendaModeGroupBy(Options.AgendaModeGroupBy);
        State.SetEventLayout(Options.EventLayout);
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    private void HandleDateRangeChanged(BitFullCalendarDateChangeEventArgs args)
    {
        InvokeAsync(() => OnDateChange.InvokeAsync(args));
    }



    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        State.OnDateRangeChanged -= HandleDateRangeChanged;
    }
}
