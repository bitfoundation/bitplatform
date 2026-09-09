using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitFullCalendar
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
    /// A date outside the <see cref="MinDate"/>/<see cref="MaxDate"/> window is pulled back into it and the
    /// corrected value is pushed back through the binding.
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
    /// When <c>true</c>, the whole toolbar is removed - the today/prev/next navigation, the mode and
    /// view tabs, the filters, the "Add Event" button, and the settings gear. Use it to drive the
    /// calendar entirely from your own chrome through the two-way bound <see cref="View"/>,
    /// <see cref="Mode"/>, and <see cref="Date"/> parameters or the navigation methods.
    /// </summary>
    [Parameter] public bool HideHeader { get; set; }

    /// <summary>
    /// When <c>true</c>, the built-in settings gear button is hidden from the calendar header.
    /// Consumers can still drive settings programmatically through the <see cref="Settings"/> object.
    /// </summary>
    [Parameter] public bool HideSettings { get; set; }

    /// <summary>
    /// The latest date the calendar can navigate to and display. Navigation past it is refused, the
    /// "next" button is disabled, and a bound <see cref="Date"/> beyond it is pulled back.
    /// <c>null</c> (the default) leaves the calendar unbounded.
    /// </summary>
    [Parameter] public DateTime? MaxDate { get; set; }

    /// <summary>
    /// The earliest date the calendar can navigate to and display. Navigation before it is refused,
    /// the "previous" button is disabled, and a bound <see cref="Date"/> before it is pulled forward.
    /// <c>null</c> (the default) leaves the calendar unbounded. A window whose bounds are inverted is
    /// ignored altogether.
    /// </summary>
    [Parameter] public DateTime? MinDate { get; set; }

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
    /// Raised when the visible date range changes - on first render, and afterwards whenever the user
    /// navigates with the prev/next/today buttons, switches views, or the grid shape changes. The
    /// callback receives the inclusive start and end dates of the new range together with the active view.
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
    /// Raised when the calendar refuses a user-driven change: a drop or resize that would overlap
    /// another event while <see cref="BitFullCalendarSettings.AllowEventOverlap"/> is <c>false</c>,
    /// one that would land outside the <see cref="MinDate"/>/<see cref="MaxDate"/> window, or one
    /// targeting an event marked <see cref="BitFullCalendarEvent.IsReadOnly"/>.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarChangeRefusal> OnRefused { get; set; }

    /// <summary>
    /// Raised when the active view changes - for example when the user selects a view tab or
    /// navigates from the year overview into a month. The callback receives the new <see cref="BitFullCalendarView"/>.
    /// </summary>
    [Parameter] public EventCallback<BitFullCalendarView> OnViewChange { get; set; }

    /// <summary>
    /// When <c>true</c>, the calendar becomes presentation-only: the "Add Event" button and the
    /// per-cell add affordances are hidden, events can no longer be dragged or resized, and the
    /// edit/delete actions are removed from the event details dialog.
    /// <para>
    /// Everything that does not modify events keeps working - date navigation, view and mode
    /// switching, filtering, the settings panel, and opening an event to read its details.
    /// Setting <c>IsEnabled</c> to <c>false</c> has the same effect on the event-editing surface.
    /// </para>
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// Resources displayed as rows in the resource timeline view. When <c>null</c> or empty,
    /// the resource timeline tab is hidden from the header. Each event's
    /// <see cref="BitFullCalendarEvent.Resource"/> is matched against the resource <c>Id</c>.
    /// </summary>
    [Parameter] public IReadOnlyList<BitFullCalendarResource>? Resources { get; set; }

    /// <summary>
    /// Configuration settings controlling calendar preferences such as the time format, the visible
    /// hour window, the slot duration, hidden weekdays, week numbers, badge variant, and agenda
    /// grouping. Values are applied when the component initializes and whenever the consumer changes
    /// one of them; preferences the user changes from the built-in settings panel are written back
    /// onto this instance so a parent re-render never reverts them.
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
    /// The views the calendar offers, in the order the view tabs render them. When <c>null</c> or
    /// empty, every view (day, week, month, year, agenda) is offered in that order. Unknown and
    /// repeated entries are ignored.
    /// <para>
    /// Excluded views are unreachable: the view tabs omit them, and <see cref="View"/>,
    /// <see cref="DefaultView"/>, and the indirect navigation paths (for example selecting a month
    /// from the year overview) are clamped into the supplied set. The tab strip is hidden entirely
    /// when a single view is left. Timeline mode still renders only the day, week, and month
    /// layouts, so it is unavailable when none of them is listed here.
    /// </para>
    /// </summary>
    [Parameter] public IReadOnlyList<BitFullCalendarView>? Views { get; set; }

    /// <summary>
    /// Optional template for customizing event rendering in the week view.
    /// When provided, replaces the default event card content inside the time-grid blocks.
    /// </summary>
    [Parameter] public RenderFragment<BitFullCalendarEvent>? WeekEventTemplate { get; set; }

    /// <summary>
    /// The calendar's live state: the active view, mode, selected date, resolved settings, and the
    /// event/resource projections every inner view renders from. Exposed so consumers can read what
    /// the calendar is currently showing; drive it through the parameters and the navigation methods
    /// rather than mutating it directly.
    /// </summary>
    public BitFullCalendarState State { get; } = new();

    private BitFullCalendarChangeNotifier _changeNotifier = default!;
    private BitFullCalendarColorScheme _colorScheme = new(null);
    private BitFcCalendarToast? _toast;
    private SettingsSnapshot? _appliedSettings;
    private bool _defaultViewApplied;
    private bool _defaultModeApplied;
    private bool _defaultDateApplied;
    private bool _initialDateChangeRaised;
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

    protected override string RootElementClass => "bit-bfc";

    /// <summary>
    /// The direction the calendar renders in: the explicit <c>Dir</c> parameter when supplied,
    /// otherwise the direction of the active culture, so a right-to-left culture flips the layout
    /// without the consumer having to say so twice.
    /// </summary>
    private BitDir ResolvedDir => Dir ?? (State.IsRtl ? BitDir.Rtl : BitDir.Ltr);

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
        State.SetDateBounds(MinDate, MaxDate);
        ApplySettings();
        _changeNotifier = new BitFullCalendarChangeNotifier(State, args => OnChange.InvokeAsync(args))
        {
            RefusalReporter = ReportRefusal
        };
        State.OnStateChanged += HandleStateChanged;
        State.OnDateRangeChanged += HandleDateRangeChanged;

        // Seed the reconciliation baseline so the first genuine (user-driven) view/mode/date change is
        // detected correctly and parameter/default-driven initialization does not raise callbacks.
        _lastView = State.View;
        _lastMode = State.Mode;
        _lastDate = State.SelectedDate;

        base.OnInitialized();
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
            State.SyncViews(Views);
            // A disabled calendar cannot be edited either, so it takes the same presentation-only path.
            State.SetReadOnly(ReadOnly || IsEnabled is false);
            // The bounds are applied before the date so a bound Date outside them is clamped once,
            // by the same rule the navigation buttons obey.
            State.SetDateBounds(MinDate, MaxDate);

            // Apply the view, mode, and date after resources and views are synced: Timeline mode
            // requires Resources to be populated to take effect, and both the mode and the view are
            // clamped into the allowed view set.
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
            _initialDateChangeRaised = true;
            InvokeAsync(() => OnDateChange.InvokeAsync(pending));
        }

        // A bound View/Mode the state refuses can resolve to the value that is already active, which
        // emits no state change and so reaches no reconciliation through HandleStateChanged. Reconcile
        // explicitly here so the corrected value is always pushed back into the binding. This is an
        // echo of the parameters just applied, so it stays silent (raiseEvents: false) like the
        // reconciliations queued during the parameter-application window.
        InvokeAsync(() => ReconcileBoundState(raiseEvents: false));

        base.OnParametersSet();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // Consumers commonly fetch the events for the range the calendar is about to show, so the
        // initial range is reported like every later one instead of only after the first navigation.
        if (firstRender && _initialDateChangeRaised is false && OnDateChange.HasDelegate)
        {
            _initialDateChangeRaised = true;
            var (start, end) = BitFullCalendarHelpers.GetDateRange(
                State.View, State.SelectedDate, State.Culture, State.FirstDayOfWeekOverride);
            // This range never travelled through the state's own channel, so tell it the range has
            // been reported - otherwise a first navigation that lands right back on it (pressing
            // "Today" while today is already showing) would report the same range a second time.
            State.MarkCurrentRangeReported();
            InvokeAsync(() => OnDateChange.InvokeAsync(new BitFullCalendarDateChangeEventArgs
            {
                Start = start,
                End = end,
                View = State.View
            }));
        }

        base.OnAfterRender(firstRender);
    }

    private void ApplyBoundState()
    {
        // Mode is applied before View because entering Timeline mode clamps the available views.
        if (ModeHasBeenSet)
        {
            // Controlled: keep the state aligned with the bound Mode on every parameter change.
            // State.SetMode falls back to Event when Timeline is requested without the resources or
            // the timeline-capable views it needs.
            State.SetMode(Mode);
        }
        else if (!_defaultModeApplied && DefaultMode.HasValue)
        {
            // Timeline default needs at least one resource and one timeline-capable view to take
            // effect; defer until they are available so a later Resources/Views assignment is not
            // permanently ignored.
            var canApplyDefaultMode = DefaultMode.Value != BitFullCalendarMode.Timeline
                || State.IsTimelineModeAvailable;
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
            // The comparison uses the clamped target so a Date outside the allowed window doesn't
            // re-trigger navigation on every parameter pass.
            var target = State.ClampToAllowedRange(Date);
            if (State.SelectedDate != target)
                State.SetSelectedDate(target);
        }
        else if (!_defaultDateApplied && DefaultDate.HasValue)
        {
            _defaultDateApplied = true;
            var target = State.ClampToAllowedRange(DefaultDate.Value);
            if (State.SelectedDate != target)
                State.SetSelectedDate(target);
        }
    }

    /// <summary>
    /// Pushes only the settings the CONSUMER changed into the state.
    /// <para>
    /// The previous pass's values are snapshotted, so a setting the consumer left alone is never
    /// re-pushed. Blindly re-applying every value on each parameter pass would revert whatever the
    /// user had just picked in the built-in settings panel as soon as anything re-rendered the parent.
    /// User-driven changes travel the other way, through <see cref="SyncSettingsFromState"/>.
    /// </para>
    /// </summary>
    private void ApplySettings()
    {
        var current = SettingsSnapshot.From(Settings);
        var previous = _appliedSettings;
        _appliedSettings = current;

        // First pass: nothing has been applied yet, so every value is the consumer's.
        if (previous is null)
        {
            PushAllSettings();
            return;
        }

        if (previous.Use24HourFormat != current.Use24HourFormat)
            State.SetUse24HourFormat(current.Use24HourFormat);
        if (previous.BadgeVariant != current.BadgeVariant)
            State.SetBadgeVariant(current.BadgeVariant);
        if (previous.VisibleStartHour != current.VisibleStartHour || previous.VisibleEndHour != current.VisibleEndHour)
            State.SetVisibleHours(current.VisibleStartHour, current.VisibleEndHour);
        if (previous.StartOfDayHour != current.StartOfDayHour)
            State.SetStartOfDayHour(current.StartOfDayHour);
        if (previous.SlotDurationMinutes != current.SlotDurationMinutes)
            State.SetSlotDurationMinutes(current.SlotDurationMinutes);
        if (previous.AgendaModeGroupBy != current.AgendaModeGroupBy)
            State.SetAgendaModeGroupBy(current.AgendaModeGroupBy);
        if (previous.EventLayout != current.EventLayout)
            State.SetEventLayout(current.EventLayout);
        if (previous.ShowDayViewCalendar != current.ShowDayViewCalendar)
            State.SetShowDayViewCalendar(current.ShowDayViewCalendar);
        if (previous.HiddenDays != current.HiddenDays)
            State.SetHiddenDays(Settings.HiddenDays);
        if (previous.FirstDayOfWeek != current.FirstDayOfWeek)
            State.SetFirstDayOfWeek(current.FirstDayOfWeek);
        if (previous.ShowWeekNumbers != current.ShowWeekNumbers)
            State.SetShowWeekNumbers(current.ShowWeekNumbers);
        if (previous.ShowCurrentTimeIndicator != current.ShowCurrentTimeIndicator)
            State.SetShowCurrentTimeIndicator(current.ShowCurrentTimeIndicator);
        if (previous.MaxEventsPerDayCell != current.MaxEventsPerDayCell)
            State.SetMaxEventsPerDayCell(current.MaxEventsPerDayCell);
        if (previous.RequireEventDescription != current.RequireEventDescription)
            State.SetRequireEventDescription(current.RequireEventDescription);
        if (previous.AllowEventOverlap != current.AllowEventOverlap)
            State.SetAllowEventOverlap(current.AllowEventOverlap);
        if (previous.AllowRangeSelection != current.AllowRangeSelection)
            State.SetAllowRangeSelection(current.AllowRangeSelection);
        if (previous.BusinessDays != current.BusinessDays)
            State.SetBusinessDays(Settings.BusinessDays);
        if (previous.BusinessStartHour != current.BusinessStartHour || previous.BusinessEndHour != current.BusinessEndHour)
            State.SetBusinessHours(current.BusinessStartHour, current.BusinessEndHour);
        if (previous.HighlightBusinessHours != current.HighlightBusinessHours)
            State.SetHighlightBusinessHours(current.HighlightBusinessHours);
        if (previous.RestrictToBusinessHours != current.RestrictToBusinessHours)
            State.SetRestrictToBusinessHours(current.RestrictToBusinessHours);
        if (previous.FixedWeekCount != current.FixedWeekCount)
            State.SetFixedWeekCount(current.FixedWeekCount);
        if (previous.ShowNonCurrentDates != current.ShowNonCurrentDates)
            State.SetShowNonCurrentDates(current.ShowNonCurrentDates);
        if (previous.NavLinks != current.NavLinks)
            State.SetNavLinks(current.NavLinks);
    }

    private void PushAllSettings()
    {
        State.SetUse24HourFormat(Settings.Use24HourFormat);
        State.SetBadgeVariant(Settings.BadgeVariant);
        // The window comes first: it decides the band the scroll anchor is clamped into.
        State.SetVisibleHours(Settings.VisibleStartHour, Settings.VisibleEndHour);
        State.SetStartOfDayHour(Settings.StartOfDayHour);
        State.SetSlotDurationMinutes(Settings.SlotDurationMinutes);
        State.SetAgendaModeGroupBy(Settings.AgendaModeGroupBy);
        State.SetEventLayout(Settings.EventLayout);
        State.SetShowDayViewCalendar(Settings.ShowDayViewCalendar);
        State.SetHiddenDays(Settings.HiddenDays);
        State.SetFirstDayOfWeek(Settings.FirstDayOfWeek);
        State.SetShowWeekNumbers(Settings.ShowWeekNumbers);
        State.SetShowCurrentTimeIndicator(Settings.ShowCurrentTimeIndicator);
        State.SetMaxEventsPerDayCell(Settings.MaxEventsPerDayCell);
        State.SetRequireEventDescription(Settings.RequireEventDescription);
        State.SetAllowEventOverlap(Settings.AllowEventOverlap);
        State.SetAllowRangeSelection(Settings.AllowRangeSelection);
        State.SetBusinessDays(Settings.BusinessDays);
        State.SetBusinessHours(Settings.BusinessStartHour, Settings.BusinessEndHour);
        State.SetHighlightBusinessHours(Settings.HighlightBusinessHours);
        State.SetRestrictToBusinessHours(Settings.RestrictToBusinessHours);
        State.SetFixedWeekCount(Settings.FixedWeekCount);
        State.SetShowNonCurrentDates(Settings.ShowNonCurrentDates);
        State.SetNavLinks(Settings.NavLinks);
    }

    /// <summary>
    /// Copies the state's resolved preferences back onto the <see cref="Settings"/> instance after a
    /// user-driven change, and re-snapshots it. Without this write-back the next parameter pass would
    /// see the consumer's untouched object as "different" and revert what the user just picked.
    /// <para>
    /// Only the values the STATE actually changed are written back. A state change can be observed
    /// after the consumer has already mutated <see cref="Settings"/> (for example inside an
    /// <c>OnChange</c> handler) but before the next parameter pass reads it, and writing every value
    /// would overwrite that pending edit with the value the calendar still holds.
    /// </para>
    /// </summary>
    private void SyncSettingsFromState()
    {
        if (Settings is null) return;

        var previous = _appliedSettings;

        if (previous is null || previous.Use24HourFormat != State.Use24HourFormat)
            Settings.Use24HourFormat = State.Use24HourFormat;
        if (previous is null || previous.BadgeVariant != State.BadgeVariant)
            Settings.BadgeVariant = State.BadgeVariant;
        if (previous is null || previous.VisibleStartHour != State.VisibleStartHour)
            Settings.VisibleStartHour = State.VisibleStartHour;
        if (previous is null || previous.VisibleEndHour != State.VisibleEndHour)
            Settings.VisibleEndHour = State.VisibleEndHour;
        if (previous is null || previous.StartOfDayHour != State.StartOfDayHour)
            Settings.StartOfDayHour = State.StartOfDayHour;
        if (previous is null || previous.SlotDurationMinutes != State.SlotDurationMinutes)
            Settings.SlotDurationMinutes = State.SlotDurationMinutes;
        if (previous is null || previous.AgendaModeGroupBy != State.AgendaModeGroupBy)
            Settings.AgendaModeGroupBy = State.AgendaModeGroupBy;
        if (previous is null || previous.EventLayout != State.EventLayout)
            Settings.EventLayout = State.EventLayout;
        if (previous is null || previous.ShowDayViewCalendar != State.ShowDayViewCalendar)
            Settings.ShowDayViewCalendar = State.ShowDayViewCalendar;
        if (previous is null || previous.ShowWeekNumbers != State.ShowWeekNumbers)
            Settings.ShowWeekNumbers = State.ShowWeekNumbers;
        if (previous is null || previous.ShowCurrentTimeIndicator != State.ShowCurrentTimeIndicator)
            Settings.ShowCurrentTimeIndicator = State.ShowCurrentTimeIndicator;
        if (previous is null || previous.MaxEventsPerDayCell != State.MaxEventsPerDayCell)
            Settings.MaxEventsPerDayCell = State.MaxEventsPerDayCell;
        if (previous is null || previous.RequireEventDescription != State.RequireEventDescription)
            Settings.RequireEventDescription = State.RequireEventDescription;
        if (previous is null || previous.AllowEventOverlap != State.AllowEventOverlap)
            Settings.AllowEventOverlap = State.AllowEventOverlap;
        if (previous is null || previous.AllowRangeSelection != State.AllowRangeSelection)
            Settings.AllowRangeSelection = State.AllowRangeSelection;
        if (previous is null || previous.BusinessStartHour != State.BusinessStartHour)
            Settings.BusinessStartHour = State.BusinessStartHour;
        if (previous is null || previous.BusinessEndHour != State.BusinessEndHour)
            Settings.BusinessEndHour = State.BusinessEndHour;
        if (previous is null || previous.HighlightBusinessHours != State.HighlightBusinessHours)
            Settings.HighlightBusinessHours = State.HighlightBusinessHours;
        if (previous is null || previous.RestrictToBusinessHours != State.RestrictToBusinessHours)
            Settings.RestrictToBusinessHours = State.RestrictToBusinessHours;
        if (previous is null || previous.FixedWeekCount != State.FixedWeekCount)
            Settings.FixedWeekCount = State.FixedWeekCount;
        if (previous is null || previous.ShowNonCurrentDates != State.ShowNonCurrentDates)
            Settings.ShowNonCurrentDates = State.ShowNonCurrentDates;
        if (previous is null || previous.NavLinks != State.NavLinks)
            Settings.NavLinks = State.NavLinks;

        // The new baseline is what the STATE now holds, not what Settings holds: a pending consumer
        // edit has to stay "different" so the next ApplySettings still pushes it.
        _appliedSettings = SettingsSnapshot.FromState(State, previous);
    }

    private void HandleStateChanged()
    {
        // Capture the flag now: the queued callback may run after OnParametersSet's finally block
        // has reset _applyingParameters to false, which would otherwise wrongly raise events.
        var applyingParameters = _applyingParameters;
        InvokeAsync(async () =>
        {
            if (applyingParameters is false)
                SyncSettingsFromState();

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
        var modeChanged = !EqualityComparer<BitFullCalendarMode>.Default.Equals(_lastMode, State.Mode);
        // A refused bound Mode (Timeline without the resources or timeline-capable views it needs) can
        // resolve to the mode that is already active. The state then reports no change at all, so the
        // divergence has to be detected against the parameter itself - otherwise the binding would keep
        // reporting a mode the calendar never entered. Only a genuine state change raises OnModeChange.
        if (modeChanged || (ModeHasBeenSet && !EqualityComparer<BitFullCalendarMode>.Default.Equals(Mode, State.Mode)))
        {
            _lastMode = State.Mode;
            await AssignMode(State.Mode);
            if (modeChanged && raiseEvents)
                await OnModeChange.InvokeAsync(State.Mode);
        }

        var viewChanged = !EqualityComparer<BitFullCalendarView>.Default.Equals(_lastView, State.View);
        // Same for a bound View excluded by Views: when the clamp lands on the active view there is no
        // state change to reconcile against, only a parameter that no longer matches what is rendered.
        if (viewChanged || (ViewHasBeenSet && !EqualityComparer<BitFullCalendarView>.Default.Equals(View, State.View)))
        {
            _lastView = State.View;
            await AssignView(State.View);
            if (viewChanged && raiseEvents)
                await OnViewChange.InvokeAsync(State.View);
        }

        // A bound Date the bounds clamped resolves to a value the state already holds, so the
        // divergence has to be detected against the parameter too - otherwise the binding would keep
        // reporting a date the calendar never navigated to.
        if (_lastDate != State.SelectedDate || (DateHasBeenSet && Date != State.SelectedDate))
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

        _initialDateChangeRaised = true;
        InvokeAsync(() => OnDateChange.InvokeAsync(args));
    }

    private void ReportRefusal(BitFullCalendarChangeRefusal refusal)
    {
        var message = refusal switch
        {
            BitFullCalendarChangeRefusal.Overlap => Texts.EventOverlapMessage,
            BitFullCalendarChangeRefusal.OutOfRange => Texts.OutOfRangeMessage,
            BitFullCalendarChangeRefusal.OutsideBusinessHours => Texts.OutsideBusinessHoursMessage,
            _ => null
        };

        if (message is { Length: > 0 })
            _toast?.Show(message, isError: true);

        if (OnRefused.HasDelegate)
            InvokeAsync(() => OnRefused.InvokeAsync(refusal));
    }



    /// <summary>Moves the calendar to the supplied date, clamped into the allowed date window.</summary>
    public void GoToDate(DateTime date) => State.SetSelectedDate(date);

    /// <summary>Moves the calendar to today, clamped into the allowed date window.</summary>
    public void GoToToday() => State.GoToToday();

    /// <summary>
    /// Steps the calendar one period forward (a day, week, month, or year depending on the active
    /// view). Does nothing when the step would leave the allowed date window.
    /// </summary>
    public void NavigateNext() => State.NavigateNext();

    /// <summary>
    /// Steps the calendar one period back. Does nothing when the step would leave the allowed date window.
    /// </summary>
    public void NavigatePrevious() => State.NavigatePrevious();

    /// <summary>
    /// Switches the active view. The value is clamped into the allowed <see cref="Views"/> set and,
    /// in Timeline mode, into the layouts the timeline can render.
    /// </summary>
    public void ChangeView(BitFullCalendarView view) => State.SetView(view);

    /// <summary>
    /// Switches the active layout mode. Timeline is refused (and falls back to Event) while there is
    /// no resource or no timeline-capable view to render.
    /// </summary>
    public void ChangeMode(BitFullCalendarMode mode) => State.SetMode(mode);

    /// <summary>The inclusive start and end dates the calendar is currently showing.</summary>
    public (DateTime Start, DateTime End) GetVisibleRange()
        => BitFullCalendarHelpers.GetDateRange(State.View, State.SelectedDate, State.Culture, State.FirstDayOfWeekOverride);



    protected override ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            State.OnStateChanged -= HandleStateChanged;
            State.OnDateRangeChanged -= HandleDateRangeChanged;
        }

        return base.DisposeAsync(disposing);
    }

    /// <summary>
    /// The settings values last pushed into the state, so the next parameter pass can tell a
    /// consumer-driven change apart from an unchanged value the user has since overridden.
    /// <see cref="HiddenDays"/> is captured as a canonical string because the list itself is a
    /// mutable reference the consumer may reuse.
    /// </summary>
    private sealed record SettingsSnapshot(
        bool Use24HourFormat,
        BitFullCalendarBadgeVariant BadgeVariant,
        int StartOfDayHour,
        int VisibleStartHour,
        int VisibleEndHour,
        int SlotDurationMinutes,
        BitFullCalendarAgendaGroupBy AgendaModeGroupBy,
        BitFullCalendarEventLayout EventLayout,
        bool ShowDayViewCalendar,
        string HiddenDays,
        DayOfWeek? FirstDayOfWeek,
        bool ShowWeekNumbers,
        bool ShowCurrentTimeIndicator,
        int MaxEventsPerDayCell,
        bool RequireEventDescription,
        bool AllowEventOverlap,
        bool AllowRangeSelection,
        string BusinessDays,
        int BusinessStartHour,
        int BusinessEndHour,
        bool HighlightBusinessHours,
        bool RestrictToBusinessHours,
        bool FixedWeekCount,
        bool ShowNonCurrentDates,
        bool NavLinks)
    {
        /// <summary>
        /// The baseline after a user-driven change: the values the state now holds, keeping the two
        /// the panel cannot change (<paramref name="previous"/>'s hidden days and first day of week)
        /// so a consumer edit to either is still detected on the next pass.
        /// </summary>
        public static SettingsSnapshot FromState(BitFullCalendarState state, SettingsSnapshot? previous) => new(
            state.Use24HourFormat,
            state.BadgeVariant,
            state.StartOfDayHour,
            state.VisibleStartHour,
            state.VisibleEndHour,
            state.SlotDurationMinutes,
            state.AgendaModeGroupBy,
            state.EventLayout,
            state.ShowDayViewCalendar,
            previous?.HiddenDays ?? string.Join(',', state.HiddenDays.Select(d => (int)d).Order()),
            previous?.FirstDayOfWeek ?? state.FirstDayOfWeekOverride,
            state.ShowWeekNumbers,
            state.ShowCurrentTimeIndicator,
            state.MaxEventsPerDayCell,
            state.RequireEventDescription,
            state.AllowEventOverlap,
            state.AllowRangeSelection,
            // The panel cannot change the business days either, so the consumer's value stays the
            // baseline and an edit to it is still detected on the next pass.
            previous?.BusinessDays ?? string.Join(',', state.BusinessDays.Select(d => (int)d).Order()),
            state.BusinessStartHour,
            state.BusinessEndHour,
            state.HighlightBusinessHours,
            state.RestrictToBusinessHours,
            state.FixedWeekCount,
            state.ShowNonCurrentDates,
            state.NavLinks);

        public static SettingsSnapshot From(BitFullCalendarSettings settings) => new(
            settings.Use24HourFormat,
            settings.BadgeVariant,
            settings.StartOfDayHour,
            settings.VisibleStartHour,
            settings.VisibleEndHour,
            settings.SlotDurationMinutes,
            settings.AgendaModeGroupBy,
            settings.EventLayout,
            settings.ShowDayViewCalendar,
            string.Join(',', BitFullCalendarHelpers.NormalizeHiddenDays(settings.HiddenDays).Select(d => (int)d).Order()),
            settings.FirstDayOfWeek,
            settings.ShowWeekNumbers,
            settings.ShowCurrentTimeIndicator,
            settings.MaxEventsPerDayCell,
            settings.RequireEventDescription,
            settings.AllowEventOverlap,
            settings.AllowRangeSelection,
            string.Join(',', BitFullCalendarHelpers.NormalizeBusinessDays(settings.BusinessDays).Select(d => (int)d).Order()),
            settings.BusinessStartHour,
            settings.BusinessEndHour,
            settings.HighlightBusinessHours,
            settings.RestrictToBusinessHours,
            settings.FixedWeekCount,
            settings.ShowNonCurrentDates,
            settings.NavLinks);
    }
}
