using System.Globalization;

namespace Bit.BlazorUI;

public class BitFullCalendarState
{
    private List<BitFullCalendarEvent> _allEvents = [];
    private List<BitFullCalendarEvent> _filteredEvents = [];
    private List<BitFullCalendarResource> _resources = [];
    private readonly List<string> _selectedColors = [];

    public DateTime SelectedDate { get; private set; } = DateTime.Today;
    public BitFullCalendarView View { get; private set; } = BitFullCalendarView.Month;
    public BitFullCalendarMode Mode { get; private set; } = BitFullCalendarMode.Event;
    public IReadOnlyList<string> SelectedColors => _selectedColors;

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

    public CultureInfo Culture { get; private set; } = CultureInfo.CurrentUICulture;
    public bool IsRtl => Culture.TextInfo.IsRightToLeft;

    // Drag state. The setter is private so all drag mutations go through StartDrag/EndDrag,
    // which keeps the OnStateChanged notification consistent.
    public BitFullCalendarEvent? DraggedEvent { get; private set; }
    public bool IsDragging => DraggedEvent != null;

    public IReadOnlyList<BitFullCalendarEvent> Events => _filteredEvents;
    public IReadOnlyList<BitFullCalendarEvent> AllEvents => _allEvents;
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
        SelectedDate = date;
        UpdateUI();
        NotifyDateRangeChanged();
    }

    public void SetView(BitFullCalendarView view)
    {
        var clamped = ClampViewForMode(view, Mode);
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
        // Timeline mode requires at least one resource. Refuse to enter it when there are none
        // so the state never lands in an unsupported (timeline-without-resources) configuration.
        if (mode == BitFullCalendarMode.Timeline && _resources.Count == 0)
            mode = BitFullCalendarMode.Event;

        if (Mode == mode)
            return;

        Mode = mode;
        var clamped = ClampViewForMode(View, mode);
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

    private static BitFullCalendarView ClampViewForMode(BitFullCalendarView view, BitFullCalendarMode mode)
    {
        if (mode != BitFullCalendarMode.Timeline)
            return view;

        return view switch
        {
            BitFullCalendarView.Day or BitFullCalendarView.Week or BitFullCalendarView.Month => view,
            _ => BitFullCalendarView.Week
        };
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
        var clamped = Math.Clamp(hour, 0, 16);
        if (StartOfDayHour == clamped)
            return;
        StartOfDayHour = clamped;
        NotifyStateChanged();
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
        SelectedDate = BitFullCalendarHelpers.NavigateDate(SelectedDate, View, false, Culture);
        UpdateUI();
        NotifyDateRangeChanged();
    }

    public void NavigateNext()
    {
        SelectedDate = BitFullCalendarHelpers.NavigateDate(SelectedDate, View, true, Culture);
        UpdateUI();
        NotifyDateRangeChanged();
    }

    public void GoToToday()
    {
        SelectedDate = DateTime.Today;
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
        // supports every view, so no view clamp is needed here (ClampViewForMode is a no-op).
        if (Mode == BitFullCalendarMode.Timeline && _resources.Count == 0)
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
        var viewEvents = BitFullCalendarHelpers.GetEventsForView(_allEvents.ToList(), View, SelectedDate, Culture);
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
        _filteredEvents = [.. _allEvents];
        NotifyStateChanged();
    }

    private void ApplyFilters()
    {
        PruneInvalidAttendeeFilter();

        var result = _allEvents.AsEnumerable();

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
            .GetEventsForView(_allEvents.ToList(), View, SelectedDate, Culture)
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

    public void HandleDrop(DateTime targetDate, int? hour = null, int? minute = null)
        => HandleDrop(targetDate, hour, minute, resourceId: null, applyResource: false);

    /// <summary>
    /// Drops the currently dragged event onto a date/time and optionally re-assigns its
    /// <see cref="BitFullCalendarEvent.Resource"/>. When <paramref name="applyResource"/> is
    /// <c>false</c> the event keeps its existing resource. Pass <paramref name="resourceId"/> as
    /// <c>null</c> together with <paramref name="applyResource"/> = <c>true</c> to clear the
    /// resource (drop on the unassigned row).
    /// </summary>
    public void HandleDrop(DateTime targetDate, int? hour, int? minute, string? resourceId, bool applyResource)
    {
        if (DraggedEvent == null) return;

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
            return;
        }

        var updated = new BitFullCalendarEvent
        {
            Id = DraggedEvent.Id,
            Title = DraggedEvent.Title,
            Description = DraggedEvent.Description,
            StartDate = newStart,
            EndDate = newStart + duration,
            Color = DraggedEvent.Color,
            Resource = newResource,
            Data = DraggedEvent.Data,
            Attendees = [.. DraggedEvent.Attendees]
        };

        UpdateEvent(updated);
        EndDrag();
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
        var (start, end) = BitFullCalendarHelpers.GetDateRange(View, SelectedDate, Culture);
        OnDateRangeChanged.Invoke(new BitFullCalendarDateChangeEventArgs
        {
            Start = start,
            End = end,
            View = View
        });
    }
}

