using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitFullCalendar : IDisposable
{
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
    /// The currently selected (anchor) date of the calendar that determines the visible date range. (two-way bound)
    /// <para>
    /// When set, the calendar navigates to the supplied date. User interactions (prev/next/today navigation,
    /// selecting a day or a month) update this value through the generated <c>DateChanged</c> callback. Use
    /// <see cref="DefaultDate"/> to provide an initial value without taking over control of the date.
    /// </para>
    /// </summary>
    [Parameter, TwoWayBound] public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>
    /// Optional template for customizing event rendering in the day view.
    /// When provided, replaces the default event card content inside the time-grid blocks.
    /// </summary>
    [Parameter] public RenderFragment<BitFullCalendarEvent>? DayEventTemplate { get; set; }

    /// <summary>
    /// The default selected date to be initially used when the <see cref="Date"/> parameter is not set.
    /// Determines the date range the calendar shows on first render. Applied once during initialization;
    /// afterwards the active date is driven by user interaction or by the two-way bound <see cref="Date"/> parameter.
    /// </summary>
    [Parameter] public DateTime? DefaultDate { get; set; }

    /// <summary>
    /// The default layout mode to be initially used when the <see cref="Mode"/> parameter is not set.
    /// <see cref="BitFullCalendarMode.Event"/> shows the standard day/week/month/year/agenda views.
    /// <see cref="BitFullCalendarMode.Timeline"/> switches to the resource × time layout (day, week, month)
    /// and requires <see cref="Resources"/> to contain at least one entry; otherwise the Timeline tab
    /// and mode have no effect. Applied once during initialization; afterwards the active mode is driven
    /// by user interaction or by the two-way bound <see cref="Mode"/> parameter.
    /// </summary>
    [Parameter] public BitFullCalendarMode? DefaultMode { get; set; }

    /// <summary>
    /// The default view to be initially used when the <see cref="View"/> parameter is not set.
    /// Controls how the date range and events are laid out (day, week, month, year, or agenda).
    /// Applied once during initialization; afterwards the active view is driven by user
    /// interaction or by the two-way bound <see cref="View"/> parameter.
    /// </summary>
    [Parameter] public BitFullCalendarView? DefaultView { get; set; }

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
    /// Events displayed in the calendar. Assign a list from parent state; updates are synced on each
    /// render when the reference or contents change. User-driven add, edit, and delete actions are
    /// reported through <see cref="OnChange"/> - update this list (or your backing store) in the handler
    /// to keep the UI in sync.
    /// </summary>
    [Parameter] public List<BitFullCalendarEvent>? Events { get; set; }

    /// <summary>
    /// When <c>true</c>, the built-in color and attendee filter dropdowns are hidden from the calendar header.
    /// Consumers can provide their own external filter UI and pass pre-filtered events to the calendar.
    /// </summary>
    [Parameter] public bool HideFilters { get; set; }

    /// <summary>
    /// When <c>true</c>, the built-in settings gear button is hidden from the calendar header.
    /// Consumers can still drive settings programmatically through the <see cref="Settings"/> object.
    /// </summary>
    [Parameter] public bool HideSettings { get; set; }

    /// <summary>
    /// The currently active layout mode of the calendar (<see cref="BitFullCalendarMode.Event"/> or
    /// <see cref="BitFullCalendarMode.Timeline"/>). (two-way bound)
    /// <para>
    /// When set, the calendar reflects the supplied mode. Timeline mode requires <see cref="Resources"/>
    /// to contain at least one entry; otherwise it falls back to <see cref="BitFullCalendarMode.Event"/>.
    /// User interactions (mode tabs) update this value through the generated <c>ModeChanged</c> callback.
    /// Use <see cref="DefaultMode"/> to provide an initial value without taking over control of the mode.
    /// </para>
    /// </summary>
    [Parameter, TwoWayBound] public BitFullCalendarMode Mode { get; set; } = BitFullCalendarMode.Event;

    /// <summary>
    /// Optional template for customizing event rendering in the month view.
    /// When provided, replaces the default event badge content inside month grid cells.
    /// </summary>
    [Parameter] public RenderFragment<BitFullCalendarEvent>? MonthEventTemplate { get; set; }

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
    /// Raised when a user adds, edits, or deletes an event in the calendar UI.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarChangeEventArgs> OnChange { get; set; }

    /// <summary>
    /// Raised when the visible date range changes - for example when the user navigates
    /// with prev/next/today buttons or switches views. The callback receives the inclusive
    /// start and end dates of the new range together with the active view.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarDateChangeEventArgs> OnDateChange { get; set; }

    /// <summary>
    /// When assigned, the built-in event details dialog is suppressed when an event is clicked.
    /// The callback receives the clicked <see cref="BitFullCalendarEvent"/>. Consumers should
    /// show their own event details UI. This applies to all views (day, week, month, agenda) and
    /// to multi-day event rows and event list dialogs.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }

    /// <summary>
    /// Raised when the active layout mode changes - for example when the user switches between the
    /// Event and Timeline tabs. The callback receives the new <see cref="BitFullCalendarMode"/>.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarMode> OnModeChange { get; set; }

    /// <summary>
    /// Raised when the active view changes - for example when the user selects a view tab or
    /// navigates from the year overview into a month. The callback receives the new <see cref="BitFullCalendarView"/>.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarView> OnViewChange { get; set; }

    /// <summary>
    /// Resources displayed as rows in the resource timeline view. When <c>null</c> or empty,
    /// the resource timeline tab is hidden from the header. Each event's
    /// <see cref="BitFullCalendarEvent.Resource"/> is matched against the resource <c>Id</c>.
    /// </summary>
    [Parameter] public IReadOnlyList<BitFullCalendarResource>? Resources { get; set; }

    /// <summary>
    /// Configuration settings controlling initial calendar preferences
    /// such as dark mode, time format, badge variant, day start hour, and agenda grouping.
    /// Values are applied when the component initializes or when a new instance is assigned.
    /// </summary>
    [Parameter] public BitFullCalendarSettings Settings { get; set; } = new();

    /// <summary>
    /// Localized strings for calendar UI labels, buttons, dialogs, filters, and accessibility text.
    /// Defaults to English; override individual properties on a <see cref="BitFullCalendarTexts"/>
    /// instance to localize the component without replacing built-in dialogs.
    /// </summary>
    [Parameter] public BitFullCalendarTexts Texts { get; set; } = new();

    /// <summary>
    /// Optional template for customizing event rendering in the resource timeline view.
    /// When provided, replaces the default event card content inside the timeline blocks.
    /// </summary>
    [Parameter] public RenderFragment<BitFullCalendarEvent>? TimelineEventTemplate { get; set; }

    /// <summary>
    /// The currently active view of the calendar (day, week, month, year, or agenda). (two-way bound)
    /// <para>
    /// When set, the calendar reflects the supplied view. User interactions (view tabs, year navigation)
    /// update this value through the generated <c>ViewChanged</c> callback. Use <see cref="DefaultView"/>
    /// to provide an initial value without taking over control of the view.
    /// </para>
    /// </summary>
    [Parameter, TwoWayBound] public BitFullCalendarView View { get; set; } = BitFullCalendarView.Month;

    /// <summary>
    /// Optional template for customizing event rendering in the week view.
    /// When provided, replaces the default event card content inside the time-grid blocks.
    /// </summary>
    [Parameter] public RenderFragment<BitFullCalendarEvent>? WeekEventTemplate { get; set; }

    public BitFullCalendarState State { get; set; } = new();
    private BitFullCalendarChangeNotifier _changeNotifier = default!;
    private BitFullCalendarColorScheme _colorScheme = new(null);
    private BitFullCalendarSettings? _appliedSettings;
    private bool _defaultViewApplied;
    private bool _defaultModeApplied;
    private bool _defaultDateApplied;
    // The last view/mode/date the component reconciled with the bound parameters. Used to detect
    // genuine (user-driven) state changes so OnViewChange/OnModeChange are not raised for
    // parameter- or default-driven updates and the bound parameters stay in sync.
    private BitFullCalendarView _lastView;
    private BitFullCalendarMode _lastMode;
    private DateTime _lastDate;
    // True while OnParametersSet pushes parameter/default values into the state. Suppresses the
    // OnViewChange/OnModeChange callbacks for those echoes while still keeping the bound
    // View/Mode/Date parameters in sync with the resulting state.
    private bool _applyingParameters;
    // While applying parameters, several state setters (SetCulture, SetMode/SetView, SetSelectedDate)
    // can each emit a date-range change in one pass. We coalesce them here and raise only the final
    // resolved range to consumers once ApplyBoundState has finished, instead of forwarding every
    // intermediate range.
    private BitFullCalendarDateChangeEventArgs? _pendingDateChange;

    private BitCascadingValueList BuildCascadingValues() => new()
    {
        { State },
        { Texts },
        { _changeNotifier },
        { _colorScheme },
        { Settings },
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
        // Settings/Texts have default instances but can be set to null when bound externally.
        // Normalize before any downstream use (ApplySettings, cascaded Texts) to avoid NREs.
        Settings ??= new();
        Texts ??= new();

        State.Initialize(Events ?? [], ResolveCulture());
        ApplySettings();
        _changeNotifier = new BitFullCalendarChangeNotifier(State, args => OnChange.InvokeAsync(args));
        State.OnStateChanged += HandleStateChanged;
        State.OnDateRangeChanged += HandleDateRangeChanged;

        // Seed the reconciliation baseline so the first genuine (user-driven) view/mode/date change is
        // detected correctly and parameter/default-driven initialization does not raise callbacks.
        _lastView = State.View;
        _lastMode = State.Mode;
        _lastDate = State.SelectedDate;
    }

    protected override void OnParametersSet()
    {
        // A null Settings/Texts can arrive from external binding, overriding the default instances;
        // restore valid defaults before ApplySettings and the cascaded Texts are consumed downstream.
        Settings ??= new();
        Texts ??= new();

        // Mark the parameter-application window so state changes triggered below (events, resources,
        // view, mode, date) keep the bound View/Mode/Date parameters in sync without raising
        // OnViewChange/OnModeChange.
        _applyingParameters = true;
        try
        {
            _colorScheme = new BitFullCalendarColorScheme(EventColorOptions);
            var resolved = ResolveCulture();
            // Compare the calendar identity in addition to the culture name: two cultures can share
            // the same Name but resolve to different calendars (for example a culture whose calendar
            // was switched), and a name-only check would skip the required SetCulture when only the
            // calendar changed - leaving the calendar rendering against the previous calendar system.
            if (!string.Equals(resolved.Name, State.Culture.Name, StringComparison.Ordinal)
                || resolved.Calendar.GetType() != State.Culture.Calendar.GetType())
                State.SetCulture(resolved);

            if (Events is not null)
                State.SyncEvents(Events);
            else
                // Events was cleared (set back to null); drop any previously loaded events so the
                // calendar display reflects the empty state instead of keeping stale items.
                State.SyncEvents([]);

            State.SyncResources(Resources);

            // Apply the view, mode, and date after resources are synced: Timeline mode requires
            // Resources to be populated to take effect.
            ApplyBoundState();

            ApplySettings();
        }
        finally
        {
            _applyingParameters = false;
        }

        // Raise the single coalesced date-range change (if any) now that all parameter-driven
        // setters have run, so consumers see only the final resolved range rather than each
        // intermediate one produced while applying parameters.
        if (_pendingDateChange is { } pending)
        {
            _pendingDateChange = null;
            InvokeAsync(() => OnDateChange.InvokeAsync(pending));
        }
    }

    private void ApplyBoundState()
    {
        // Mode is applied before View because entering Timeline mode clamps the available views.
        if (ModeHasBeenSet)
        {
            // Controlled: keep the state aligned with the bound Mode on every parameter change.
            // State.SetMode falls back to Event when Timeline is requested without resources.
            State.SetMode(Mode);
        }
        else if (!_defaultModeApplied && DefaultMode.HasValue)
        {
            // Timeline default needs at least one resource to take effect; defer until resources are
            // available so a later Resources assignment is not permanently ignored.
            var canApplyDefaultMode = DefaultMode.Value != BitFullCalendarMode.Timeline
                || Resources is { Count: > 0 };
            if (canApplyDefaultMode)
            {
                _defaultModeApplied = true;
                State.SetMode(DefaultMode.Value);
            }
        }

        if (ViewHasBeenSet)
        {
            // Controlled: keep the state aligned with the bound View on every parameter change.
            State.SetView(View);
        }
        else if (!_defaultViewApplied && DefaultView.HasValue)
        {
            _defaultViewApplied = true;
            State.SetView(DefaultView.Value);
        }

        if (DateHasBeenSet)
        {
            // Controlled: keep the state aligned with the bound Date. SetSelectedDate does not
            // short-circuit on equal values, so guard against redundant navigation/re-render loops.
            if (State.SelectedDate != Date)
                State.SetSelectedDate(Date);
        }
        else if (!_defaultDateApplied && DefaultDate.HasValue)
        {
            _defaultDateApplied = true;
            if (State.SelectedDate != DefaultDate.Value)
                State.SetSelectedDate(DefaultDate.Value);
        }
    }

    private void ApplySettings()
    {
        // Sync each individual value rather than short-circuiting on a reference comparison: the same
        // BitFullCalendarSettings instance can be mutated in place by the consumer, so comparing the
        // reference would silently ignore those updates. The State.Set* methods each guard against
        // no-op changes, so re-applying unchanged values is cheap and raises no spurious notifications.
        _appliedSettings = Settings;
        State.SetUse24HourFormat(Settings.Use24HourFormat);
        State.SetBadgeVariant(Settings.BadgeVariant);
        State.SetStartOfDayHour(Settings.StartOfDayHour);
        State.SetAgendaModeGroupBy(Settings.AgendaModeGroupBy);
        State.SetEventLayout(Settings.EventLayout);
        State.SetShowDayViewCalendar(Settings.ShowDayViewCalendar);
    }

    private void HandleStateChanged()
    {
        // Capture the flag now: the queued callback may run after OnParametersSet's finally block
        // has reset _applyingParameters to false, which would otherwise wrongly raise events.
        var applyingParameters = _applyingParameters;
        InvokeAsync(async () =>
        {
            await ReconcileBoundState(raiseEvents: !applyingParameters);
            StateHasChanged();
        });
    }

    // Pushes the current state view/mode/date back into the two-way bound parameters when they change.
    // When raiseEvents is true (user-driven change) the OnViewChange/OnModeChange callbacks are
    // invoked; parameter- and default-driven echoes pass false to keep the bindings in sync silently.
    // Date changes are surfaced separately through OnDateChange (via the date-range channel), so no
    // additional event is raised here for the date.
    private async Task ReconcileBoundState(bool raiseEvents)
    {
        if (!EqualityComparer<BitFullCalendarMode>.Default.Equals(_lastMode, State.Mode))
        {
            _lastMode = State.Mode;
            await AssignMode(State.Mode);
            if (raiseEvents)
                await OnModeChange.InvokeAsync(State.Mode);
        }

        if (!EqualityComparer<BitFullCalendarView>.Default.Equals(_lastView, State.View))
        {
            _lastView = State.View;
            await AssignView(State.View);
            if (raiseEvents)
                await OnViewChange.InvokeAsync(State.View);
        }

        if (_lastDate != State.SelectedDate)
        {
            _lastDate = State.SelectedDate;
            await AssignDate(State.SelectedDate);
        }
    }

    private void HandleDateRangeChanged(BitFullCalendarDateChangeEventArgs args)
    {
        // While applying parameters, coalesce: keep only the latest range and let OnParametersSet
        // raise it once after ApplyBoundState finishes. Outside that window (user-driven navigation,
        // view switches) forward each change immediately as before.
        if (_applyingParameters)
        {
            _pendingDateChange = args;
            return;
        }

        InvokeAsync(() => OnDateChange.InvokeAsync(args));
    }



    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        State.OnDateRangeChanged -= HandleDateRangeChanged;
    }
}
