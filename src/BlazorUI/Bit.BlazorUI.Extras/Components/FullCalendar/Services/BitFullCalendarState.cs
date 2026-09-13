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
    // _allEvents with every recurring event replaced by its occurrences around the visible range; the
    // filters narrow this down to _filteredEvents.
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
        var viewEvents = BitFullCalendarHelpers.GetEventsForView(_expandedEvents, View, SelectedDate, Culture);
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
        _filteredEvents = [.. _expandedEvents];
        NotifyStateChanged();
    }

    private void ApplyFilters()
    {
        _expandedEvents = ExpandRecurringEvents();

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
            .GetEventsForView(_expandedEvents, View, SelectedDate, Culture)
            .SelectMany(e => e.Attendees)
            .Select(BitFullCalendarHelpers.AttendeeFilterKey)
            .Where(k => k.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        if (!validKeys.Contains(SelectedAttendeeKey))
            SelectedAttendeeKey = null;
    }

    /// <summary>
    /// The events as the views see them: every recurring event is replaced by its occurrences around the
    /// visible range - with a week of slack either side for the month grid's leading and trailing days - and
    /// around today, which the day view's "happening now" panel reads whatever day is shown.
    /// </summary>
    private List<BitFullCalendarEvent> ExpandRecurringEvents()
    {
        if (_allEvents.Exists(e => e.IsRecurring) is false)
            return _allEvents;

        var (start, end) = BitFullCalendarHelpers.GetDateRange(View, SelectedDate, Culture);
        var today = DateTime.Today;
        (DateTime Start, DateTime End)[] ranges =
        [
            (AddDaysClamped(start, -7), AddDaysClamped(end, 8)),
            (AddDaysClamped(today, -1), AddDaysClamped(today, 2))
        ];

        var result = new List<BitFullCalendarEvent>(_allEvents.Count);
        foreach (var ev in _allEvents)
        {
            if (ev.IsRecurring is false)
            {
                result.Add(ev);
                continue;
            }

            var starts = new SortedSet<DateTime>();
            foreach (var (rangeStart, rangeEnd) in ranges)
            {
                starts.UnionWith(BitFullCalendarHelpers.GetOccurrenceStarts(ev, rangeStart, rangeEnd, Culture));
            }

            foreach (var occurrenceStart in starts)
            {
                result.Add(BitFullCalendarHelpers.CreateOccurrence(ev, occurrenceStart));
            }
        }

        return result;
    }

    private static DateTime AddDaysClamped(DateTime date, int days)
    {
        var ticks = Math.Clamp(date.Ticks + days * TimeSpan.TicksPerDay, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks);
        return new DateTime(ticks, date.Kind);
    }

    // Drag-and-drop helpers
    public void StartDrag(BitFullCalendarEvent ev)
    {
        // Single choke point for every drag entry point: a read-only calendar never enters the
        // dragging state, so the drop handlers downstream have nothing to commit.
        if (ReadOnly)
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

        foreach (var change in BuildDropChanges(targetDate, hour, minute, resourceId, applyResource))
        {
            ApplyChange(change);
        }

        EndDrag();
    }

    /// <summary>
    /// The changes <see cref="HandleDrop(DateTime, int?, int?, string?, bool)"/> makes, without applying
    /// them - empty when nothing is dragged or the drop changes nothing. A dragged occurrence is moved on
    /// its own (<see cref="BitFullCalendarRecurrenceEditScope.ThisEvent"/>).
    /// </summary>
    public List<BitFullCalendarChangeEventArgs> BuildDropChanges(DateTime targetDate, int? hour, int? minute, string? resourceId, bool applyResource)
    {
        if (DraggedEvent == null) return [];

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
            return [];

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

        return BuildEditChanges(DraggedEvent, updated, BitFullCalendarRecurrenceEditScope.ThisEvent, BitFullCalendarChangeSource.Drag);
    }

    /// <summary>
    /// The recurring event <paramref name="ev"/> was generated from, or <c>null</c> when it is not an
    /// occurrence or its series is no longer among the events.
    /// </summary>
    public BitFullCalendarEvent? GetRecurringEvent(BitFullCalendarEvent ev)
        => ev.RecurringEventId is { } id ? _allEvents.Find(e => e.Id == id && e.IsRecurring) : null;

    /// <summary>
    /// The changes that turn <paramref name="original"/> into <paramref name="updated"/>, ready for
    /// <see cref="ApplyChange"/> and <c>OnChange</c> (<see cref="BitFullCalendarChangeNotifier.CommitAsync"/>
    /// does both). An event outside a series gives a single Edit. An occurrence is resolved against its
    /// series according to <paramref name="scope"/>:
    /// <list type="bullet">
    /// <item><see cref="BitFullCalendarRecurrenceEditScope.ThisEvent"/> - an Edit of the series skipping the
    /// occurrence's date, then an Add of the edited occurrence as an event of its own.</item>
    /// <item><see cref="BitFullCalendarRecurrenceEditScope.ThisAndFollowing"/> - an Edit of the series ending
    /// the day before the occurrence, then an Add of a new series starting at the edited occurrence with
    /// <paramref name="updated"/>'s rule. On the first occurrence it is the same as AllEvents.</item>
    /// <item><see cref="BitFullCalendarRecurrenceEditScope.AllEvents"/> - an Edit of the series taking
    /// <paramref name="updated"/>'s details and rule, moved by as much as the occurrence was moved.</item>
    /// </list>
    /// Empty when the occurrence's series is no longer among the events.
    /// </summary>
    public List<BitFullCalendarChangeEventArgs> BuildEditChanges(BitFullCalendarEvent original,
                                                                BitFullCalendarEvent updated,
                                                                BitFullCalendarRecurrenceEditScope scope,
                                                                BitFullCalendarChangeSource source)
    {
        if (original.IsOccurrence is false)
            return [CreateChange(BitFullCalendarChangeKind.Edit, CloneEvent(updated), CloneEvent(original), source)];

        if (GetRecurringEvent(original) is not { } series)
            return [];

        var occurrenceStart = original.OccurrenceDate ?? original.StartDate;
        if (scope == BitFullCalendarRecurrenceEditScope.ThisAndFollowing && occurrenceStart <= series.StartDate)
            scope = BitFullCalendarRecurrenceEditScope.AllEvents;

        switch (scope)
        {
            case BitFullCalendarRecurrenceEditScope.ThisEvent:
            {
                var edited = CloneEvent(series);
                SkipOccurrence(edited.Recurrence!, occurrenceStart);

                var detached = CopyAsNewEvent(updated);
                detached.Recurrence = null;

                return
                [
                    CreateChange(BitFullCalendarChangeKind.Edit, edited, CloneEvent(series), source),
                    CreateChange(BitFullCalendarChangeKind.Add, detached, null, source)
                ];
            }

            case BitFullCalendarRecurrenceEditScope.ThisAndFollowing:
            {
                var edited = CloneEvent(series);
                EndSeriesBefore(edited.Recurrence!, occurrenceStart);

                var following = CopyAsNewEvent(updated);
                if (following.Recurrence is { } rule)
                {
                    // A count left as it was still describes the whole original series, so the new series
                    // only gets what the part before the occurrence has not used up.
                    if (rule.Count is { } count && count == series.Recurrence!.Count)
                    {
                        rule.Count = count - series.Recurrence.CountRuleOccurrencesBefore(series.StartDate, occurrenceStart, Culture);
                    }

                    var firstDate = following.StartDate.Date;
                    rule.ExceptionDates.RemoveAll(d => d.Date < firstDate);
                    rule.AdditionalDates.RemoveAll(d => d.Date < firstDate);
                }

                return
                [
                    CreateChange(BitFullCalendarChangeKind.Edit, edited, CloneEvent(series), source),
                    CreateChange(BitFullCalendarChangeKind.Add, following, null, source)
                ];
            }

            default:
            {
                var edited = CloneEvent(updated);
                edited.Id = series.Id;
                edited.RecurringEventId = null;
                edited.OccurrenceDate = null;
                edited.StartDate = series.StartDate + (updated.StartDate - original.StartDate);
                edited.EndDate = edited.StartDate + updated.Duration;

                return [CreateChange(BitFullCalendarChangeKind.Edit, edited, CloneEvent(series), source)];
            }
        }
    }

    /// <summary>
    /// The changes deleting <paramref name="original"/> makes, ready for <see cref="ApplyChange"/> and
    /// <c>OnChange</c>. An event outside a series gives a single Delete. An occurrence is resolved against its
    /// series according to <paramref name="scope"/>: <see cref="BitFullCalendarRecurrenceEditScope.ThisEvent"/>
    /// gives an Edit of the series skipping the occurrence's date,
    /// <see cref="BitFullCalendarRecurrenceEditScope.ThisAndFollowing"/> an Edit of the series ending the day
    /// before it (a Delete of the series on its first occurrence), and
    /// <see cref="BitFullCalendarRecurrenceEditScope.AllEvents"/> a Delete of the series. Empty when the
    /// occurrence's series is no longer among the events.
    /// </summary>
    public List<BitFullCalendarChangeEventArgs> BuildDeleteChanges(BitFullCalendarEvent original,
                                                                  BitFullCalendarRecurrenceEditScope scope,
                                                                  BitFullCalendarChangeSource source)
    {
        if (original.IsOccurrence is false)
        {
            var snapshot = CloneEvent(original);
            return [CreateChange(BitFullCalendarChangeKind.Delete, snapshot, snapshot, source)];
        }

        if (GetRecurringEvent(original) is not { } series)
            return [];

        var occurrenceStart = original.OccurrenceDate ?? original.StartDate;
        if (scope == BitFullCalendarRecurrenceEditScope.AllEvents
            || (scope == BitFullCalendarRecurrenceEditScope.ThisAndFollowing && occurrenceStart <= series.StartDate))
        {
            var snapshot = CloneEvent(series);
            return [CreateChange(BitFullCalendarChangeKind.Delete, snapshot, snapshot, source)];
        }

        var edited = CloneEvent(series);
        if (scope == BitFullCalendarRecurrenceEditScope.ThisEvent)
            SkipOccurrence(edited.Recurrence!, occurrenceStart);
        else
            EndSeriesBefore(edited.Recurrence!, occurrenceStart);

        return [CreateChange(BitFullCalendarChangeKind.Edit, edited, CloneEvent(series), source)];
    }

    /// <summary>Applies a change built by <see cref="BuildEditChanges"/> or <see cref="BuildDeleteChanges"/> to the events.</summary>
    public void ApplyChange(BitFullCalendarChangeEventArgs change)
    {
        switch (change.Kind)
        {
            case BitFullCalendarChangeKind.Add:
                AddEvent(CloneEvent(change.Event));
                break;
            case BitFullCalendarChangeKind.Edit:
                UpdateEvent(CloneEvent(change.Event));
                break;
            case BitFullCalendarChangeKind.Delete:
                RemoveEvent(change.Event.Id);
                break;
        }
    }

    /// <summary>Undoes <see cref="ApplyChange"/>.</summary>
    public void RevertChange(BitFullCalendarChangeEventArgs change)
    {
        switch (change.Kind)
        {
            case BitFullCalendarChangeKind.Add:
                RemoveEvent(change.Event.Id);
                break;
            case BitFullCalendarChangeKind.Edit when change.OldEvent is not null:
                UpdateEvent(CloneEvent(change.OldEvent));
                break;
            case BitFullCalendarChangeKind.Delete:
                AddEvent(CloneEvent(change.OldEvent ?? change.Event));
                break;
        }
    }

    private static BitFullCalendarChangeEventArgs CreateChange(BitFullCalendarChangeKind kind,
                                                               BitFullCalendarEvent ev,
                                                               BitFullCalendarEvent? oldEvent,
                                                               BitFullCalendarChangeSource source)
        => new() { Event = ev, OldEvent = oldEvent, Kind = kind, Source = source };

    private static BitFullCalendarEvent CloneEvent(BitFullCalendarEvent source) => BitFullCalendarChangeNotifier.CloneEvent(source);

    private static BitFullCalendarEvent CopyAsNewEvent(BitFullCalendarEvent source)
    {
        var copy = CloneEvent(source);
        copy.Id = Guid.NewGuid().ToString("N");
        copy.RecurringEventId = null;
        copy.OccurrenceDate = null;
        return copy;
    }

    private static void SkipOccurrence(BitFullCalendarRecurrence rule, DateTime occurrenceStart)
    {
        var date = occurrenceStart.Date;
        rule.AdditionalDates.RemoveAll(d => d.Date == date);
        if (rule.ExceptionDates.Exists(d => d.Date == date) is false)
        {
            rule.ExceptionDates.Add(date);
        }
    }

    private static void EndSeriesBefore(BitFullCalendarRecurrence rule, DateTime occurrenceStart)
    {
        var lastDate = occurrenceStart.Date.AddDays(-1);
        if (rule.Until is not { } until || until.Date > lastDate)
        {
            rule.Until = lastDate;
        }

        rule.ExceptionDates.RemoveAll(d => d.Date > lastDate);
        rule.AdditionalDates.RemoveAll(d => d.Date > lastDate);
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

