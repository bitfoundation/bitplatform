using System.Globalization;

namespace Bit.BlazorUI;

public class BitFullCalendarState
{
    /// <summary>Every view the calendar offers, in the order the tab strip renders them.</summary>
    private static readonly BitFullCalendarView[] _allViews =
    [
        BitFullCalendarView.Day,
        BitFullCalendarView.Week,
        BitFullCalendarView.Month,
        BitFullCalendarView.Year,
        BitFullCalendarView.Agenda
    ];

    /// <summary>The subset of views the timeline layout can render.</summary>
    private static readonly BitFullCalendarView[] _timelineViews =
    [
        BitFullCalendarView.Day,
        BitFullCalendarView.Week,
        BitFullCalendarView.Month
    ];

    private List<BitFullCalendarEvent> _allEvents = [];
    // The events as the grids see them: every recurring master replaced by the occurrences that fall
    // inside the visible range, before the colour/attendee filters narrow them further.
    private List<BitFullCalendarEvent> _expandedEvents = [];
    private List<BitFullCalendarEvent> _filteredEvents = [];
    private List<BitFullCalendarResource> _resources = [];
    private List<BitFullCalendarView> _views = [.. _allViews];
    private readonly List<string> _selectedColors = [];

    public DateTime SelectedDate { get; private set; } = DateTime.Today;
    public BitFullCalendarView View { get; private set; } = BitFullCalendarView.Month;
    public BitFullCalendarMode Mode { get; private set; } = BitFullCalendarMode.Event;
    public IReadOnlyList<string> SelectedColors => _selectedColors;

    /// <summary>
    /// When <c>true</c> the calendar is presentation-only: the add affordances, drag-and-drop,
    /// resizing, and the edit/delete actions are suppressed while navigation, view switching,
    /// and filtering keep working.
    /// </summary>
    public bool ReadOnly { get; private set; }

    /// <summary>The views the consumer allowed, in display order.</summary>
    public IReadOnlyList<BitFullCalendarView> Views => _views;

    /// <summary>The allowed views that the active <see cref="Mode"/> can render, in display order.</summary>
    public IReadOnlyList<BitFullCalendarView> AvailableViews => GetViewsForMode(Mode);

    /// <summary>
    /// True when Timeline mode can be entered: it needs at least one resource to lay out rows and
    /// at least one allowed view the timeline supports.
    /// </summary>
    public bool IsTimelineModeAvailable => _resources.Count > 0 && _views.Any(_timelineViews.Contains);

    /// <summary>When set, only events that include this attendee (by <see cref="BitFullCalendarHelpers.AttendeeFilterKey"/>) are shown.</summary>
    public string? SelectedAttendeeKey { get; private set; }
    public bool Use24HourFormat { get; private set; } = true;
    public BitFullCalendarBadgeVariant BadgeVariant { get; private set; } = BitFullCalendarBadgeVariant.Colored;
    public int StartOfDayHour { get; private set; } = 8;
    public BitFullCalendarAgendaGroupBy AgendaModeGroupBy { get; private set; } = BitFullCalendarAgendaGroupBy.Date;
    public BitFullCalendarEventLayout EventLayout { get; private set; } = BitFullCalendarEventLayout.Overlap;
    /// <summary>Whether the mini calendar is rendered in the day view sidebar.</summary>
    public bool ShowDayViewCalendar { get; private set; } = true;
    /// <summary>Incremented when <see cref="GoToToday"/> is invoked in agenda view so the list can scroll to today.</summary>
    public ulong AgendaScrollToTodayNonce { get; private set; }

    /// <summary>First hour rendered by the day/week time grids (inclusive).</summary>
    public int VisibleStartHour { get; private set; }

    /// <summary>Last hour rendered by the day/week time grids (exclusive).</summary>
    public int VisibleEndHour { get; private set; } = 24;

    /// <summary>Number of hour rows the day/week time grids render.</summary>
    public int VisibleHourCount => VisibleEndHour - VisibleStartHour;

    /// <summary>The hours the day/week time grids render, ascending.</summary>
    public IEnumerable<int> VisibleHours => Enumerable.Range(VisibleStartHour, VisibleHourCount);

    /// <summary>Length in minutes of one slot inside an hour; also the drag/resize snap granularity.</summary>
    public int SlotDurationMinutes { get; private set; } = 30;

    /// <summary>The minute offsets of the slots inside one hour, ascending.</summary>
    public int[] SlotMinutes { get; private set; } = BitFullCalendarHelpers.GetSlotMinutes(30);

    /// <summary>Weekdays removed from the week, month, year, and timeline week grids.</summary>
    public IReadOnlyList<DayOfWeek> HiddenDays { get; private set; } = [];

    /// <summary>Number of weekday columns the date grids render.</summary>
    public int VisibleWeekDayCount => 7 - HiddenDays.Count;

    /// <summary>Explicit first-day-of-week override; <c>null</c> means "follow the culture".</summary>
    public DayOfWeek? FirstDayOfWeekOverride { get; private set; }

    /// <summary>The day the week starts on: the override when set, otherwise the culture's.</summary>
    public DayOfWeek FirstDayOfWeek => BitFullCalendarHelpers.ResolveFirstDayOfWeek(Culture, FirstDayOfWeekOverride);

    /// <summary>Whether ISO week numbers are rendered in the month grid and the week view gutter.</summary>
    public bool ShowWeekNumbers { get; private set; }

    /// <summary>Whether the "current time" indicator line is rendered on the day/week time grids.</summary>
    public bool ShowCurrentTimeIndicator { get; private set; } = true;

    /// <summary>Event badges a month cell renders before the rest collapse behind "+N more".</summary>
    public int MaxEventsPerDayCell { get; private set; } = 3;

    /// <summary>Whether the built-in add/edit dialog requires a description.</summary>
    public bool RequireEventDescription { get; private set; }

    /// <summary>Whether two events on the same resource may occupy the same time range.</summary>
    public bool AllowEventOverlap { get; private set; } = true;

    /// <summary>Whether dragging across the day/week time grid selects a range to create an event in.</summary>
    public bool AllowRangeSelection { get; private set; } = true;

    /// <summary>Earliest date the calendar can navigate to, or <c>null</c> for no lower bound.</summary>
    public DateTime? MinDate { get; private set; }

    /// <summary>Latest date the calendar can navigate to, or <c>null</c> for no upper bound.</summary>
    public DateTime? MaxDate { get; private set; }

    public CultureInfo Culture { get; private set; } = CultureInfo.CurrentUICulture;
    public bool IsRtl => Culture.TextInfo.IsRightToLeft;

    // Drag state. The setter is private so all drag mutations go through StartDrag/EndDrag,
    // which keeps the OnStateChanged notification consistent.
    public BitFullCalendarEvent? DraggedEvent { get; private set; }
    public bool IsDragging => DraggedEvent != null;

    /// <summary>The events the views render: expanded for recurrence, then filtered.</summary>
    public IReadOnlyList<BitFullCalendarEvent> Events => _filteredEvents;

    /// <summary>
    /// The events as supplied, with every recurring series still represented by its master. This is
    /// the store the add/edit/delete and drop paths mutate.
    /// </summary>
    public IReadOnlyList<BitFullCalendarEvent> AllEvents => _allEvents;

    /// <summary>
    /// The events occupying the visible range - recurring masters expanded into occurrences - before
    /// the colour and attendee filters are applied. This is what the booking rules measure against,
    /// so a filtered-out event still blocks its slot.
    /// </summary>
    public IReadOnlyList<BitFullCalendarEvent> ExpandedEvents => _expandedEvents;
    public IReadOnlyList<BitFullCalendarResource> Resources => _resources;

    public event Action? OnStateChanged;
    public event Action<BitFullCalendarDateChangeEventArgs>? OnDateRangeChanged;

    public void Initialize(List<BitFullCalendarEvent> events, CultureInfo? culture = null)
    {
        _allEvents = [.. events];
        NormalizeEventIds();
        if (culture != null)
            Culture = culture;
        UpdateUI();
    }

    public void SetCulture(CultureInfo culture)
    {
        Culture = culture;
        UpdateUI();
        NotifyDateRangeChanged();
    }

    public void SetSelectedDate(DateTime date)
    {
        SelectedDate = ClampToAllowedRange(date);
        UpdateUI();
        NotifyDateRangeChanged();
    }

    /// <summary>
    /// Pins a date inside the <see cref="MinDate"/>/<see cref="MaxDate"/> window. The time of day is
    /// preserved so a bound Date carrying a time is not silently truncated.
    /// </summary>
    public DateTime ClampToAllowedRange(DateTime date)
    {
        if (MinDate is { } min && date.Date < min.Date)
            return min.Date + date.TimeOfDay;
        if (MaxDate is { } max && date.Date > max.Date)
            return max.Date + date.TimeOfDay;
        return date;
    }

    /// <summary>True when the supplied date sits inside the allowed navigation window.</summary>
    public bool IsDateInAllowedRange(DateTime date)
        => (MinDate is not { } min || date.Date >= min.Date)
           && (MaxDate is not { } max || date.Date <= max.Date);

    /// <summary>
    /// Restricts the dates the calendar can navigate to. Either bound may be <c>null</c>. A window
    /// whose bounds are inverted is ignored, and the active date is pulled into the new window.
    /// </summary>
    public void SetDateBounds(DateTime? minDate, DateTime? maxDate)
    {
        // An inverted window would make every date invalid, so it is refused outright rather than
        // leaving the calendar unable to render anything.
        if (minDate is { } lo && maxDate is { } hi && lo.Date > hi.Date)
        {
            minDate = null;
            maxDate = null;
        }

        if (MinDate == minDate && MaxDate == maxDate)
            return;

        MinDate = minDate;
        MaxDate = maxDate;

        var clamped = ClampToAllowedRange(SelectedDate);
        if (clamped != SelectedDate)
        {
            SelectedDate = clamped;
            UpdateUI();
            NotifyDateRangeChanged();
            return;
        }

        NotifyStateChanged();
    }

    /// <summary>True when <see cref="NavigatePrevious"/> would land inside the allowed window.</summary>
    public bool CanNavigatePrevious => CanNavigate(false);

    /// <summary>True when <see cref="NavigateNext"/> would land inside the allowed window.</summary>
    public bool CanNavigateNext => CanNavigate(true);

    private bool CanNavigate(bool forward)
    {
        if (MinDate is null && MaxDate is null)
            return true;

        // The step is allowed while any part of the range it would land on is still inside the
        // window - navigating a month whose first days are out of bounds is legitimate as long as
        // the month itself is reachable.
        var target = BitFullCalendarHelpers.NavigateDate(SelectedDate, View, forward, Culture, HiddenDays);
        var (start, end) = BitFullCalendarHelpers.GetDateRange(View, target, Culture, FirstDayOfWeekOverride);
        return (MinDate is not { } min || end.Date >= min.Date)
               && (MaxDate is not { } max || start.Date <= max.Date);
    }

    public void SetView(BitFullCalendarView view)
    {
        var clamped = ClampView(view, Mode);
        if (clamped == View)
            return;

        View = clamped;
        UpdateUI();
        NotifyDateRangeChanged();
    }

    /// <summary>
    /// Switches between Event and Timeline modes. When entering Timeline mode the active view
    /// is clamped to Day / Week / Month (Year and Agenda are not supported in timeline mode).
    /// </summary>
    public void SetMode(BitFullCalendarMode mode)
    {
        // Timeline mode requires at least one resource and one allowed view it can lay out. Refuse
        // to enter it otherwise so the state never lands in an unsupported configuration.
        if (mode == BitFullCalendarMode.Timeline && !IsTimelineModeAvailable)
            mode = BitFullCalendarMode.Event;

        if (Mode == mode)
            return;

        Mode = mode;
        var clamped = ClampView(View, mode);
        var viewChanged = clamped != View;
        if (viewChanged)
            View = clamped;

        UpdateUI();
        // The visible date range is a function of View + SelectedDate (+ Culture); none of those
        // change here unless the View was clamped. Only surface OnDateChange when the range really
        // changed, matching the BitFullCalendar.razor.cs visible-range contract.
        if (viewChanged)
            NotifyDateRangeChanged();
    }

    /// <summary>The allowed views the supplied mode can render, in display order.</summary>
    public IReadOnlyList<BitFullCalendarView> GetViewsForMode(BitFullCalendarMode mode)
        => mode == BitFullCalendarMode.Timeline
            ? [.. _views.Where(_timelineViews.Contains)]
            : _views;

    /// <summary>True when the supplied view is reachable in the active mode.</summary>
    public bool IsViewAvailable(BitFullCalendarView view) => AvailableViews.Contains(view);

    private BitFullCalendarView ClampView(BitFullCalendarView view, BitFullCalendarMode mode)
    {
        var available = GetViewsForMode(mode);
        if (available.Contains(view))
            return view;

        // Timeline mode has always fallen back to the week layout for the views it cannot render;
        // keep that whenever Week is still allowed, and otherwise land on the first allowed view so
        // the calendar never renders a view the consumer excluded.
        if (mode == BitFullCalendarMode.Timeline && available.Contains(BitFullCalendarView.Week))
            return BitFullCalendarView.Week;

        return available.Count > 0 ? available[0] : view;
    }

    /// <summary>
    /// Turns the presentation-only mode on or off. A drag that is still in flight is dropped so a
    /// pending gesture cannot commit a change after the calendar has become read-only.
    /// </summary>
    public void SetReadOnly(bool value)
    {
        if (ReadOnly == value)
            return;

        ReadOnly = value;
        if (ReadOnly)
            DraggedEvent = null;

        NotifyStateChanged();
    }

    /// <summary>
    /// Replaces the set of views the calendar offers. Safe to call from <c>OnParametersSet</c> - it
    /// short-circuits when the supplied list matches the current one. A <c>null</c> or empty list
    /// restores every built-in view; unknown and repeated entries are dropped.
    /// </summary>
    public void SyncViews(IReadOnlyList<BitFullCalendarView>? views)
    {
        var next = NormalizeViews(views);
        if (ViewsMatch(next))
            return;

        _views = next;

        // Timeline mode needs at least one view it can lay out, so a set that removes them all has
        // to fall back to Event mode before the active view is re-clamped into the new set.
        if (Mode == BitFullCalendarMode.Timeline && !IsTimelineModeAvailable)
            Mode = BitFullCalendarMode.Event;

        var clamped = ClampView(View, Mode);
        var viewChanged = clamped != View;
        if (viewChanged)
            View = clamped;

        UpdateUI();

        if (viewChanged)
            NotifyDateRangeChanged();
    }

    private static List<BitFullCalendarView> NormalizeViews(IReadOnlyList<BitFullCalendarView>? views)
    {
        if (views is null || views.Count == 0)
            return [.. _allViews];

        var result = new List<BitFullCalendarView>(views.Count);
        foreach (var view in views)
        {
            // Values outside the enum would render a blank tab and never match the active view, and
            // a repeated entry would render the same tab twice; skip both instead.
            if (!Enum.IsDefined(view) || result.Contains(view))
                continue;

            result.Add(view);
        }

        return result.Count > 0 ? result : [.. _allViews];
    }

    private bool ViewsMatch(List<BitFullCalendarView> views)
    {
        if (_views.Count != views.Count)
            return false;

        for (var i = 0; i < _views.Count; i++)
        {
            if (_views[i] != views[i])
                return false;
        }

        return true;
    }

    public void SetUse24HourFormat(bool value)
    {
        if (Use24HourFormat == value)
            return;
        Use24HourFormat = value;
        NotifyStateChanged();
    }

    public void ToggleTimeFormat()
    {
        Use24HourFormat = !Use24HourFormat;
        NotifyStateChanged();
    }

    public void SetBadgeVariant(BitFullCalendarBadgeVariant variant)
    {
        if (BadgeVariant == variant)
            return;
        BadgeVariant = variant;
        NotifyStateChanged();
    }

    public void SetStartOfDayHour(int hour)
    {
        // The scroll anchor only means something inside the rendered window, so it is pinned to it
        // rather than to a fixed 0-16 band that a narrowed window may not even contain.
        var clamped = Math.Clamp(hour, VisibleStartHour, Math.Max(VisibleStartHour, VisibleEndHour - 1));
        if (StartOfDayHour == clamped)
            return;
        StartOfDayHour = clamped;
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the hour window the day/week time grids render. The window is normalized (see
    /// <see cref="BitFullCalendarHelpers.NormalizeVisibleHours"/>) and the scroll anchor is pulled
    /// back inside it.
    /// </summary>
    public void SetVisibleHours(int startHour, int endHour)
    {
        var (start, end) = BitFullCalendarHelpers.NormalizeVisibleHours(startHour, endHour);
        if (VisibleStartHour == start && VisibleEndHour == end)
            return;

        VisibleStartHour = start;
        VisibleEndHour = end;
        StartOfDayHour = Math.Clamp(StartOfDayHour, start, Math.Max(start, end - 1));
        NotifyStateChanged();
    }

    public void SetSlotDurationMinutes(int minutes)
    {
        var normalized = new BitFullCalendarSettings { SlotDurationMinutes = minutes }.SlotDurationMinutes;
        if (SlotDurationMinutes == normalized)
            return;

        SlotDurationMinutes = normalized;
        SlotMinutes = BitFullCalendarHelpers.GetSlotMinutes(normalized);
        NotifyStateChanged();
    }

    public void SetHiddenDays(IReadOnlyList<DayOfWeek>? hiddenDays)
    {
        var next = BitFullCalendarHelpers.NormalizeHiddenDays(hiddenDays).OrderBy(d => (int)d).ToList();
        if (HiddenDays.Count == next.Count && HiddenDays.All(next.Contains))
            return;

        HiddenDays = next;
        UpdateUI();
        NotifyDateRangeChanged();
    }

    public void SetFirstDayOfWeek(DayOfWeek? firstDayOfWeek)
    {
        var next = firstDayOfWeek is { } day && Enum.IsDefined(day) ? day : (DayOfWeek?)null;
        if (FirstDayOfWeekOverride == next)
            return;

        FirstDayOfWeekOverride = next;
        UpdateUI();
        // The week view's visible range is anchored on the first day of the week, so changing it
        // moves the range even though the selected date stays put.
        NotifyDateRangeChanged();
    }

    public void SetShowWeekNumbers(bool value)
    {
        if (ShowWeekNumbers == value)
            return;
        ShowWeekNumbers = value;
        NotifyStateChanged();
    }

    public void ToggleShowWeekNumbers()
    {
        ShowWeekNumbers = !ShowWeekNumbers;
        NotifyStateChanged();
    }

    public void SetShowCurrentTimeIndicator(bool value)
    {
        if (ShowCurrentTimeIndicator == value)
            return;
        ShowCurrentTimeIndicator = value;
        NotifyStateChanged();
    }

    public void ToggleShowCurrentTimeIndicator()
    {
        ShowCurrentTimeIndicator = !ShowCurrentTimeIndicator;
        NotifyStateChanged();
    }

    public void SetMaxEventsPerDayCell(int value)
    {
        var clamped = Math.Clamp(value, 1, 10);
        if (MaxEventsPerDayCell == clamped)
            return;
        MaxEventsPerDayCell = clamped;
        NotifyStateChanged();
    }

    public void SetRequireEventDescription(bool value)
    {
        if (RequireEventDescription == value)
            return;
        RequireEventDescription = value;
        NotifyStateChanged();
    }

    public void SetAllowEventOverlap(bool value)
    {
        if (AllowEventOverlap == value)
            return;
        AllowEventOverlap = value;
        NotifyStateChanged();
    }

    public void SetAllowRangeSelection(bool value)
    {
        if (AllowRangeSelection == value)
            return;
        AllowRangeSelection = value;
        NotifyStateChanged();
    }

    /// <summary>
    /// True when the supplied range may be committed for <paramref name="eventId"/>: either overlaps
    /// are allowed, or no other event on the same resource occupies any part of that range.
    /// </summary>
    public bool IsRangeAvailable(string eventId, DateTime start, DateTime end, string? resourceId)
    {
        if (AllowEventOverlap)
            return true;

        var candidate = new BitFullCalendarEvent { StartDate = start, EndDate = end };
        // Measured against what actually occupies the visible range, so a recurring occurrence blocks
        // its slot just like a one-off event does.
        foreach (var other in _expandedEvents)
        {
            // An occurrence belongs to its master, so a series never collides with itself.
            if (string.Equals(other.SeriesId ?? other.Id, eventId, StringComparison.Ordinal))
                continue;
            // Only events sharing the resource lane can collide; two unassigned events do share one.
            if (!string.Equals(other.Resource ?? "", resourceId ?? "", StringComparison.Ordinal))
                continue;
            if (BitFullCalendarHelpers.EventsOverlap(candidate, other))
                return false;
        }

        return true;
    }

    public void SetAgendaModeGroupBy(BitFullCalendarAgendaGroupBy groupBy)
    {
        if (AgendaModeGroupBy == groupBy)
            return;
        AgendaModeGroupBy = groupBy;
        NotifyStateChanged();
    }
    public void SetEventLayout(BitFullCalendarEventLayout layout)
    {
        if (EventLayout == layout)
            return;
        EventLayout = layout;
        NotifyStateChanged();
    }

    public void SetShowDayViewCalendar(bool value)
    {
        if (ShowDayViewCalendar == value)
            return;
        ShowDayViewCalendar = value;
        NotifyStateChanged();
    }

    public void ToggleShowDayViewCalendar()
    {
        ShowDayViewCalendar = !ShowDayViewCalendar;
        NotifyStateChanged();
    }

    public void ToggleEventLayout()
    {
        EventLayout = EventLayout == BitFullCalendarEventLayout.Overlap
            ? BitFullCalendarEventLayout.Stack
            : BitFullCalendarEventLayout.Overlap;
        NotifyStateChanged();
    }
    public void NavigatePrevious()
    {
        if (CanNavigatePrevious is false)
            return;

        SelectedDate = ClampToAllowedRange(BitFullCalendarHelpers.NavigateDate(SelectedDate, View, false, Culture, HiddenDays));
        UpdateUI();
        NotifyDateRangeChanged();
    }

    public void NavigateNext()
    {
        if (CanNavigateNext is false)
            return;

        SelectedDate = ClampToAllowedRange(BitFullCalendarHelpers.NavigateDate(SelectedDate, View, true, Culture, HiddenDays));
        UpdateUI();
        NotifyDateRangeChanged();
    }

    public void GoToToday()
    {
        SelectedDate = ClampToAllowedRange(DateTime.Today);
        if (View == BitFullCalendarView.Agenda)
            AgendaScrollToTodayNonce++;
        UpdateUI();
        NotifyDateRangeChanged();
    }

    /// <summary>
    /// Replaces the internal event list with the supplied collection when the contents differ.
    /// Safe to call from <c>OnParametersSet</c> - it short-circuits when the list hasn't changed,
    /// preventing infinite re-render loops.
    /// </summary>
    public void SyncEvents(List<BitFullCalendarEvent> events)
    {
        if (EventsMatch(events))
        {
            // References are unchanged, but event properties (color, attendees, id, ...) may have
            // been mutated in place. Re-normalize ids first so a blanked or now-duplicate Id can't
            // leave the month-view slot dictionaries keyed by colliding ids, then recompute the
            // filtered projection so filter-dependent state stays accurate. Skip the change
            // notification to avoid a re-render loop from OnParametersSet.
            NormalizeEventIds();
            ApplyFilters();
            return;
        }

        _allEvents = [.. events];
        NormalizeEventIds();
        ApplyFilters();
        NotifyStateChanged();
    }

    /// <summary>
    /// Replaces the resource list shown by the resource timeline view. Safe to call from
    /// <c>OnParametersSet</c> - it short-circuits when the supplied list matches the current one.
    /// </summary>
    public void SyncResources(IReadOnlyList<BitFullCalendarResource>? resources)
    {
        var next = resources is null ? new List<BitFullCalendarResource>() : [.. resources];
        if (ResourcesMatch(next))
            return;

        // Resource ids key the timeline row grouping and rendering, so two resources sharing an id
        // would collapse or mis-render rows. The Id setter already rejects blank ids; enforce
        // uniqueness here (the resource-building path that populates State.Resources) before the
        // resources reach the FullCalendar models.
        EnsureUniqueResourceIds(next);

        _resources = next;

        // If resources were emptied while Timeline mode is active, fall back to Event mode so the
        // calendar never stays in the unsupported timeline-without-resources state. Event mode
        // offers every allowed view and the timeline views are a subset of them, so the active view
        // is already valid here and no clamp is needed.
        if (Mode == BitFullCalendarMode.Timeline && !IsTimelineModeAvailable)
        {
            Mode = BitFullCalendarMode.Event;
        }

        NotifyStateChanged();
    }

    private static void EnsureUniqueResourceIds(List<BitFullCalendarResource> resources)
    {
        if (resources.Count < 2)
            return;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var resource in resources)
        {
            if (!seen.Add(resource.Id))
                throw new ArgumentException(
                    $"Duplicate resource Id '{resource.Id}'. Resource ids must be unique.",
                    nameof(resources));
        }
    }

    private bool ResourcesMatch(List<BitFullCalendarResource> resources)    {
        if (_resources.Count != resources.Count)
            return false;

        for (var i = 0; i < _resources.Count; i++)
        {
            if (!ReferenceEquals(_resources[i], resources[i]))
                return false;
        }

        return true;
    }

    private bool EventsMatch(List<BitFullCalendarEvent> events)
    {
        if (_allEvents.Count != events.Count)
            return false;

        for (var i = 0; i < _allEvents.Count; i++)
        {
            if (!ReferenceEquals(_allEvents[i], events[i]))
                return false;
        }

        return true;
    }

    public void AddEvent(BitFullCalendarEvent ev)
    {
        _allEvents.Add(ev);
        NormalizeEventIds();
        UpdateUI();
    }

    public void UpdateEvent(BitFullCalendarEvent ev)
    {
        var idx = _allEvents.FindIndex(e => e.Id == ev.Id);
        if (idx >= 0) _allEvents[idx] = ev;
        UpdateUI();
    }

    public void RemoveEvent(string eventId)
    {
        _allEvents.RemoveAll(e => e.Id == eventId);
        UpdateUI();
    }

    public void FilterByColor(string colorId)
    {
        if (string.IsNullOrWhiteSpace(colorId))
            return;

        var trimmed = colorId.Trim();
        var existing = _selectedColors.FindIndex(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
            _selectedColors.RemoveAt(existing);
        else
            _selectedColors.Add(trimmed);
        UpdateUI();
    }

    public void SetColorFilter(string? colorId)
    {
        _selectedColors.Clear();
        if (!string.IsNullOrWhiteSpace(colorId))
            _selectedColors.Add(colorId.Trim());

        UpdateUI();
    }

    public void SetAttendeeFilter(string? attendeeKey)
    {
        SelectedAttendeeKey = string.IsNullOrWhiteSpace(attendeeKey) ? null : attendeeKey.Trim();
        UpdateUI();
    }

    public void UpdateUI()
    {
        ApplyFilters();
        NotifyStateChanged();
    }

    /// <summary>Distinct attendees on events visible in the current view/date range.</summary>
    public IReadOnlyList<(string Key, string DisplayName)> GetAttendeesInCurrentView(string unnamedAttendeeText = "(Unnamed)")
    {
        var viewEvents = BitFullCalendarHelpers.GetEventsForView(_expandedEvents, View, SelectedDate, Culture, FirstDayOfWeekOverride);
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var ev in viewEvents)
        {
            foreach (var a in ev.Attendees)
            {
                var key = BitFullCalendarHelpers.AttendeeFilterKey(a);
                if (key.Length == 0)
                    continue;
                if (map.ContainsKey(key))
                    continue;
                var label = string.IsNullOrWhiteSpace(a.FullName)
                    ? (string.IsNullOrWhiteSpace(a.Id) ? unnamedAttendeeText : a.Id.Trim())
                    : a.FullName.Trim();
                map[key] = label;
            }
        }

        return map
            .OrderBy(kv => kv.Value, StringComparer.Create(Culture, ignoreCase: true))
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    public void ClearFilter()
    {
        _selectedColors.Clear();
        SelectedAttendeeKey = null;
        _expandedEvents = ExpandForCurrentRange();
        _filteredEvents = [.. _expandedEvents];
        NotifyStateChanged();
    }

    /// <summary>
    /// The window recurrences are expanded over: the visible range, widened by the days a grid shows
    /// from the neighbouring periods (a month grid leads and trails with them) so an occurrence in
    /// one of those cells is not missing.
    /// </summary>
    private (DateTime Start, DateTime End) GetExpansionRange()
    {
        var (start, end) = BitFullCalendarHelpers.GetDateRange(View, SelectedDate, Culture, FirstDayOfWeekOverride);
        var padding = View is BitFullCalendarView.Year ? 0 : 7;
        return (start.AddDays(-padding), end.AddDays(padding));
    }

    private List<BitFullCalendarEvent> ExpandForCurrentRange()
    {
        // Nothing to expand is the common case, so the walk is skipped entirely then.
        if (_allEvents.All(e => e.Recurrence is null))
            return _allEvents;

        var (start, end) = GetExpansionRange();
        return BitFullCalendarHelpers.ExpandRecurrences(_allEvents, start, end);
    }

    private void ApplyFilters()
    {
        _expandedEvents = ExpandForCurrentRange();

        PruneInvalidAttendeeFilter();

        var result = _expandedEvents.AsEnumerable();

        if (_selectedColors.Count > 0)
            result = result.Where(e => _selectedColors.Any(c => string.Equals(c, e.Color, StringComparison.OrdinalIgnoreCase)));

        if (SelectedAttendeeKey is not null)
            result = result.Where(e => e.Attendees.Any(a => BitFullCalendarHelpers.AttendeeFilterKey(a) == SelectedAttendeeKey));

        _filteredEvents = result.ToList();
    }

    private void PruneInvalidAttendeeFilter()
    {
        if (SelectedAttendeeKey is null)
            return;

        var validKeys = BitFullCalendarHelpers
            .GetEventsForView(_expandedEvents, View, SelectedDate, Culture, FirstDayOfWeekOverride)
            .SelectMany(e => e.Attendees)
            .Select(BitFullCalendarHelpers.AttendeeFilterKey)
            .Where(k => k.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        if (!validKeys.Contains(SelectedAttendeeKey))
            SelectedAttendeeKey = null;
    }

    // Drag-and-drop helpers
    public void StartDrag(BitFullCalendarEvent ev)
    {
        // Single choke point for every drag entry point: a read-only calendar - or a single event
        // locked with BitFullCalendarEvent.IsReadOnly - never enters the dragging state, so the drop
        // handlers downstream have nothing to commit.
        if (ReadOnly || ev is null || ev.IsReadOnly)
            return;

        DraggedEvent = ev;
        NotifyStateChanged();
    }

    public void EndDrag()
    {
        if (DraggedEvent == null)
            return;

        DraggedEvent = null;
        NotifyStateChanged();
    }

    public BitFullCalendarChangeRefusal HandleDrop(DateTime targetDate, int? hour = null, int? minute = null)
        => HandleDrop(targetDate, hour, minute, resourceId: null, applyResource: false);

    /// <summary>
    /// Drops the currently dragged event onto a date/time and optionally re-assigns its
    /// <see cref="BitFullCalendarEvent.Resource"/>. When <paramref name="applyResource"/> is
    /// <c>false</c> the event keeps its existing resource. Pass <paramref name="resourceId"/> as
    /// <c>null</c> together with <paramref name="applyResource"/> = <c>true</c> to clear the
    /// resource (drop on the unassigned row).
    /// </summary>
    public BitFullCalendarChangeRefusal HandleDrop(DateTime targetDate, int? hour, int? minute, string? resourceId, bool applyResource)
    {
        if (DraggedEvent == null) return BitFullCalendarChangeRefusal.None;

        if (ReadOnly || DraggedEvent.IsReadOnly)
        {
            EndDrag();
            return BitFullCalendarChangeRefusal.ReadOnly;
        }

        var originalStart = DraggedEvent.StartDate;
        var originalResource = DraggedEvent.Resource;
        var duration = DraggedEvent.Duration;

        var newStart = targetDate.Date;
        if (hour.HasValue)
            newStart = newStart.AddHours(hour.Value).AddMinutes(minute ?? 0);
        else
            newStart = newStart.AddHours(originalStart.Hour).AddMinutes(originalStart.Minute);

        var newResource = applyResource ? resourceId : originalResource;

        var resourceChanged = applyResource && !string.Equals(originalResource ?? "", newResource ?? "", StringComparison.Ordinal);

        if (newStart == originalStart && !resourceChanged)
        {
            EndDrag();
            return BitFullCalendarChangeRefusal.None;
        }

        var newEnd = newStart + duration;

        // The drop target has to stay inside the window the calendar is allowed to show; otherwise
        // the event would land on a date the user can never navigate back to.
        if (!IsDateInAllowedRange(newStart) || !IsDateInAllowedRange(BitFullCalendarHelpers.GetInclusiveEndDate(
                new BitFullCalendarEvent { StartDate = newStart, EndDate = newEnd })))
        {
            EndDrag();
            return BitFullCalendarChangeRefusal.OutOfRange;
        }

        if (!IsRangeAvailable(DraggedEvent.Id, newStart, newEnd, newResource))
        {
            EndDrag();
            return BitFullCalendarChangeRefusal.Overlap;
        }

        var updated = new BitFullCalendarEvent
        {
            Id = DraggedEvent.Id,
            Title = DraggedEvent.Title,
            Description = DraggedEvent.Description,
            StartDate = newStart,
            EndDate = newEnd,
            Color = DraggedEvent.Color,
            Resource = newResource,
            Data = DraggedEvent.Data,
            Attendees = [.. DraggedEvent.Attendees],
            IsAllDay = DraggedEvent.IsAllDay,
            Recurrence = DraggedEvent.Recurrence,
            IsReadOnly = DraggedEvent.IsReadOnly,
            CssClass = DraggedEvent.CssClass
        };

        UpdateEvent(updated);
        EndDrag();
        return BitFullCalendarChangeRefusal.None;
    }

    private void NormalizeEventIds() => NormalizeEventIds(_allEvents);

    /// <summary>
    /// Ensures every event carries a non-blank, unique <see cref="BitFullCalendarEvent.Id"/> before
    /// layout state is built. Month positioning keys its slot dictionaries by event id
    /// (see <see cref="BitFullCalendarHelpers.CalculateMonthEventPositions"/> /
    /// <see cref="BitFullCalendarHelpers.GetMonthCellEvents"/>), so blank or duplicate ids would
    /// overwrite each other and drop events from the grid. Blank or colliding ids are remapped to a
    /// generated stable key; already-unique ids are left untouched.
    /// </summary>
    private static void NormalizeEventIds(List<BitFullCalendarEvent> events)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ev in events)
        {
            if (!string.IsNullOrWhiteSpace(ev.Id) && seen.Add(ev.Id))
                continue;

            string generated;
            do
            {
                generated = Guid.NewGuid().ToString("n");
            }
            while (!seen.Add(generated));
            ev.Id = generated;
        }
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    private void NotifyDateRangeChanged()
    {
        if (OnDateRangeChanged is null) return;
        var (start, end) = BitFullCalendarHelpers.GetDateRange(View, SelectedDate, Culture, FirstDayOfWeekOverride);
        OnDateRangeChanged.Invoke(new BitFullCalendarDateChangeEventArgs
        {
            Start = start,
            End = end,
            View = View
        });
    }
}

