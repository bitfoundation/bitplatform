using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// What a <see cref="BitFullCalendar"/> is showing (<see cref="BitFullCalendar.State"/>): the active view, mode and
/// date, the settings in force and the events each view draws, and the answers to the calendar's own questions
/// (<see cref="ValidateRange"/>, <see cref="CanEdit"/>, <see cref="IsBusinessTime"/>, ...). An app reads it, and
/// sets its color and attendee filters; the calendar changes everything else as its parameters, its
/// <see cref="BitFullCalendar.Settings"/> and the user change, and raises <see cref="OnStateChanged"/> each time.
/// </summary>
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
    // The window _expandedEvents was expanded over (inclusive dates), or null when nothing recurs and it is every event.
    private (DateTime Start, DateTime End)? _expansionWindow;
    // The expanded events less the background ones: what the filters narrow, and what the attendee filter offers.
    private List<BitFullCalendarEvent> _foregroundEvents = [];
    private List<BitFullCalendarEvent> _filteredEvents = [];
    // The IsBackground events of the visible range: drawn as bands behind the grids, never as cards, and never
    // narrowed by the filters - they are the context the other events are read against.
    private List<BitFullCalendarEvent> _backgroundEvents = [];
    private List<BitFullCalendarResource> _resources = [];
    private List<BitFullCalendarView> _views = [.. _allViews];
    private readonly List<string> _selectedColors = [];

    /// <summary>
    /// The clock the calendar reads "now" and "today" from - the today highlight, the current-time line, the
    /// "Today" button and the "Happening now" panel. <see cref="TimeProvider.System"/> unless the calendar was
    /// handed one of its own.
    /// </summary>
    public TimeProvider TimeProvider { get; private set; } = TimeProvider.System;

    /// <summary>The current local date and time according to <see cref="TimeProvider"/>.</summary>
    public DateTime Now => TimeProvider.GetLocalNow().DateTime;

    /// <summary>The current local date according to <see cref="TimeProvider"/>.</summary>
    public DateTime Today => Now.Date;

    private DateTime _selectedDate;
    // The first day a shorter week (WeekDayCount) shows. It moves only when the selected date leaves the run of days
    // on screen, so adding an event on the third of three days does not slide the view to start there.
    private DateTime _daySpanStart;

    internal BitFullCalendarState()
    {
        // The state opens on the today of its own clock, read once: DateTime.Today is another clock than the one
        // SetTimeProvider compares the selected date with, and two reads can fall on either side of a midnight.
        _selectedDate = _daySpanStart = Today;
    }

    /// <summary>
    /// The selected date: the day the day view shows, and the date the week, month and year views are laid out
    /// around. Always inside <see cref="MinDate"/> and <see cref="MaxDate"/>; set it with
    /// <see cref="BitFullCalendar.GoToDate"/>.
    /// </summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        private set
        {
            _selectedDate = value;
            SyncDaySpan();
        }
    }

    /// <summary>
    /// The number of days the week view shows, or <c>null</c> for the culture's whole week
    /// (<see cref="BitFullCalendarSettings.WeekDayCount"/>).
    /// </summary>
    public int? WeekDayCount { get; private set; }

    /// <summary>
    /// The date the week grid is laid out from: the selected date for a whole week, the first day shown for a shorter
    /// one - whatever view is active, so the run is kept while another view is shown.
    /// </summary>
    internal DateTime WeekSpanStart => WeekDayCount is null ? SelectedDate : _daySpanStart;

    /// <summary>
    /// The date the active view's range is computed from: the first day of a shorter week while the week view is
    /// shown, the selected date otherwise (a day or a month is the one the selected date falls in).
    /// </summary>
    internal DateTime WeekAnchor => View == BitFullCalendarView.Week ? WeekSpanStart : SelectedDate;

    /// <summary>The dates the week view (and the week timeline) shows, in display order.</summary>
    public DateTime[] GetVisibleWeekDates()
        => BitFullCalendarHelpers.GetWeekDates(WeekSpanStart, Culture, FirstDayOfWeekOverride, HiddenDays, WeekDayCount);

    /// <summary>
    /// The inclusive first and last date the active view shows. For the month view that is the whole grid - the days it
    /// borrows from the neighbouring months lead and trail it and show their events too, so an app loading the events of
    /// the reported range has to load theirs as well - unless <see cref="ShowNonCurrentDates"/> leaves those days blank,
    /// in which case it is the month alone. The timeline's month view has a column per day of the month and nothing
    /// more, so its range is always the month.
    /// </summary>
    public (DateTime Start, DateTime End) GetVisibleRange()
    {
        if (View == BitFullCalendarView.Month && Mode == BitFullCalendarMode.Event && ShowNonCurrentDates)
        {
            var cells = BitFullCalendarHelpers.GetCalendarCells(SelectedDate, Culture, FirstDayOfWeekOverride, HiddenDays, FixedWeekCount);

            if (cells.Count > 0)
                return (cells[0].Date.Date, cells[^1].Date.Date);
        }

        return BitFullCalendarHelpers.GetDateRange(View, WeekAnchor, Culture, FirstDayOfWeekOverride, WeekDayCount, HiddenDays);
    }

    private void SyncDaySpan()
    {
        if (WeekDayCount is null)
            return;

        if (Array.IndexOf(GetVisibleWeekDates(), _selectedDate.Date) < 0)
            _daySpanStart = _selectedDate.Date;
    }

    internal void SetWeekDayCount(int? weekDayCount)
    {
        var next = BitFullCalendarHelpers.NormalizeWeekDayCount(weekDayCount);
        if (WeekDayCount == next)
            return;

        WeekDayCount = next;
        _daySpanStart = _selectedDate.Date;
        UpdateUI();
        NotifyDateRangeChanged();
    }

    /// <summary>The active view; changed with <see cref="BitFullCalendar.ChangeView"/>.</summary>
    public BitFullCalendarView View { get; private set; } = BitFullCalendarView.Month;

    /// <summary>The active mode, events or timeline; changed with <see cref="BitFullCalendar.ChangeMode"/>.</summary>
    public BitFullCalendarMode Mode { get; private set; } = BitFullCalendarMode.Event;

    /// <summary>
    /// The color ids (<see cref="BitFullCalendarColorOption"/> ids) the color filter keeps, or none when events of
    /// every color are shown. Set with <see cref="SetColorFilter"/> and <see cref="FilterByColor"/>.
    /// </summary>
    public IReadOnlyList<string> SelectedColors => _selectedColors;

    /// <summary>
    /// Resolves an event's color to the palette id the color filter offers, so a default-colored event is filtered with
    /// the swatch it is drawn in. Set by the calendar from its color scheme.
    /// </summary>
    internal Func<string?, string>? CanonicalColorId { get; set; }

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

    /// <summary>
    /// When set, only events that include this attendee are shown. The key is <c>"id:"</c> and the attendee's
    /// <see cref="BitFullCalendarAttendee.Id"/>, or, for an attendee without one, <c>"name:"</c> and its
    /// <see cref="BitFullCalendarAttendee.FullName"/> in lower case.
    /// </summary>
    public string? SelectedAttendeeKey { get; private set; }

    /// <summary>Whether times are written in the 24-hour format (<see cref="BitFullCalendarSettings.Use24HourFormat"/>).</summary>
    public bool Use24HourFormat { get; private set; } = true;

    /// <summary>How an event's color is drawn (<see cref="BitFullCalendarSettings.BadgeVariant"/>).</summary>
    public BitFullCalendarBadgeVariant BadgeVariant { get; private set; } = BitFullCalendarBadgeVariant.Colored;

    /// <summary>
    /// The hour the day and week time grids scroll to (<see cref="BitFullCalendarSettings.StartOfDayHour"/>), within
    /// <see cref="VisibleStartHour"/> and <see cref="VisibleEndHour"/>.
    /// </summary>
    public int StartOfDayHour { get; private set; } = 8;

    /// <summary>How the agenda view groups its events (<see cref="BitFullCalendarSettings.AgendaModeGroupBy"/>).</summary>
    public BitFullCalendarAgendaGroupBy AgendaModeGroupBy { get; private set; } = BitFullCalendarAgendaGroupBy.Date;

    /// <summary>
    /// How overlapping events are laid out in the day and week views (<see cref="BitFullCalendarSettings.EventLayout"/>).
    /// </summary>
    public BitFullCalendarEventLayout EventLayout { get; private set; } = BitFullCalendarEventLayout.Overlap;

    /// <summary>Whether the mini calendar is rendered in the day view sidebar.</summary>
    public bool ShowDayViewCalendar { get; private set; } = true;
    /// <summary>Incremented when <see cref="GoToToday"/> is invoked in agenda view so the list can scroll to today.</summary>
    internal ulong AgendaScrollToTodayNonce { get; private set; }

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

    /// <summary>The weekdays business hours run on.</summary>
    public IReadOnlySet<DayOfWeek> BusinessDays { get; private set; } =
        BitFullCalendarHelpers.NormalizeBusinessDays(null);

    /// <summary>First hour of the business day (inclusive).</summary>
    public int BusinessStartHour { get; private set; } = 9;

    /// <summary>Last hour of the business day (exclusive).</summary>
    public int BusinessEndHour { get; private set; } = 17;

    /// <summary>Whether the grids shade everything outside the business hours.</summary>
    public bool HighlightBusinessHours { get; private set; }

    /// <summary>Whether a drag, resize, or save outside the business hours is refused.</summary>
    public bool RestrictToBusinessHours { get; private set; }

    /// <summary>Whether the month grid always renders six week rows.</summary>
    public bool FixedWeekCount { get; private set; }

    /// <summary>Whether the month grid fills in the days it borrows from the neighbouring months.</summary>
    public bool ShowNonCurrentDates { get; private set; } = true;

    /// <summary>Whether day numbers, weekday headers, and week numbers navigate when clicked.</summary>
    public bool NavLinks { get; private set; }

    /// <summary>Earliest date the calendar can navigate to, or <c>null</c> for no lower bound.</summary>
    public DateTime? MinDate { get; private set; }

    /// <summary>Latest date the calendar can navigate to, or <c>null</c> for no upper bound.</summary>
    public DateTime? MaxDate { get; private set; }

    /// <summary>
    /// The culture the calendar writes its dates and times in, and takes its calendar system and first day of the week
    /// from: the one <see cref="BitFullCalendar.CultureName"/> names, else <see cref="BitFullCalendar.Culture"/>, else
    /// <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    public CultureInfo Culture { get; private set; } = CultureInfo.CurrentUICulture;

    /// <summary>
    /// Whether the calendar is laid out right-to-left: the explicit direction of the component when one was given,
    /// the one the browser resolved for <see cref="BitDir.Auto"/> once it has rendered, otherwise the writing direction
    /// of the active culture. Everything that maps a physical key or pointer movement onto earlier/later reads this,
    /// so it agrees with what is actually rendered.
    /// </summary>
    public bool IsRtl => _dir switch
    {
        BitDir.Rtl => true,
        BitDir.Ltr => false,
        BitDir.Auto when _autoRtl is { } autoRtl => autoRtl,
        _ => Culture.TextInfo.IsRightToLeft
    };

    private BitDir? _dir;
    private bool? _autoRtl;

    /// <summary>
    /// Sets the explicit layout direction of the calendar; <c>null</c> follows the writing direction of the culture,
    /// and <see cref="BitDir.Auto"/> the content, as the browser resolves it (see <see cref="SetAutoDirection"/>).
    /// </summary>
    internal void SetDirection(BitDir? dir)
    {
        if (_dir == dir)
            return;

        _dir = dir;
        NotifyStateChanged();
    }

    /// <summary>
    /// Records the direction the browser resolved for a calendar whose direction is <see cref="BitDir.Auto"/>: its
    /// content decides it, which .NET cannot read, so until it is known the culture's stands in for it.
    /// </summary>
    internal void SetAutoDirection(bool isRtl)
    {
        if (_autoRtl == isRtl)
            return;

        var before = IsRtl;

        _autoRtl = isRtl;

        if (IsRtl != before)
        {
            NotifyStateChanged();
        }
    }

    // Drag state. The setter is private so all drag mutations go through StartDrag/EndDrag,
    // which keeps the OnStateChanged notification consistent.

    /// <summary>
    /// The event being dragged with the pointer, from the moment its drag starts until it is dropped or the drag ends;
    /// <c>null</c> otherwise, and while an event is moved from the keyboard.
    /// </summary>
    public BitFullCalendarEvent? DraggedEvent { get; private set; }

    /// <summary>Whether an event is being dragged with the pointer (<see cref="DraggedEvent"/> is set).</summary>
    public bool IsDragging => DraggedEvent != null;

    /// <summary>The events the views render: expanded for recurrence, then filtered.</summary>
    public IReadOnlyList<BitFullCalendarEvent> Events => _filteredEvents;

    /// <summary>
    /// The <see cref="BitFullCalendarEvent.IsBackground"/> events of the visible range, recurrences expanded. They
    /// are not in <see cref="Events"/>, so nothing that lists, counts or opens events ever sees them.
    /// </summary>
    public IReadOnlyList<BitFullCalendarEvent> BackgroundEvents => _backgroundEvents;

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

    /// <summary>The resources the calendar was given (<see cref="BitFullCalendar.Resources"/>): the rows of the timeline.</summary>
    public IReadOnlyList<BitFullCalendarResource> Resources => _resources;

    /// <summary>
    /// Raised after anything in the state changes: the view, the mode, the date, a setting, the events, the
    /// resources, a filter or a drag. The changes the calendar makes together, such as applying its parameters, raise
    /// it once. An app that subscribes unsubscribes when it is done, since the calendar keeps the handler.
    /// </summary>
    public event Action? OnStateChanged;

    /// <summary>
    /// Raised when the range of dates the active view covers changes, or the view does, with that range and view. A
    /// new date inside the range already shown raises nothing, and nor does the first range: the calendar reports that
    /// one itself, through <see cref="BitFullCalendar.OnDateChange"/>, which also reports every later one.
    /// </summary>
    public event Action<BitFullCalendarDateChangeEventArgs>? OnDateRangeChanged;

    internal void Initialize(List<BitFullCalendarEvent> events, CultureInfo? culture = null)
    {
        _allEvents = [.. events];
        NormalizeEventIds();
        if (culture != null)
            Culture = culture;
        UpdateUI();
    }

    /// <summary>
    /// Replaces the clock. A calendar still showing the old clock's today (the one it opened on) follows the new
    /// one, so a user in another time zone opens on their own today rather than the server's.
    /// </summary>
    internal void SetTimeProvider(TimeProvider? timeProvider)
    {
        timeProvider ??= TimeProvider.System;
        if (ReferenceEquals(timeProvider, TimeProvider)) return;

        var oldNow = Now;
        var wasOnToday = SelectedDate == oldNow.Date;
        TimeProvider = timeProvider;
        // A provider created anew on every render (an inline `new`) reads the same clock as the one it replaces: nothing
        // on screen changes, so nothing is recomputed or re-rendered for it.
        if (Math.Abs((Now - oldNow).TotalMinutes) < 1) return;

        var moved = wasOnToday && SelectedDate != Today;
        if (moved)
            SelectedDate = ClampToAllowedRange(Today);
        UpdateUI();
        // A calendar that moved to another today shows another range, which a consumer loading by range has to hear.
        if (moved)
            NotifyDateRangeChanged();
    }

    internal void SetCulture(CultureInfo culture)
    {
        Culture = culture;
        UpdateUI();
        NotifyDateRangeChanged();
    }

    internal void SetSelectedDate(DateTime date)
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
    internal void SetDateBounds(DateTime? minDate, DateTime? maxDate)
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
        var target = BitFullCalendarHelpers.NavigateDate(WeekAnchor, View, forward, Culture, HiddenDays, WeekDayCount);
        var (start, end) = BitFullCalendarHelpers.GetDateRange(View, target, Culture, FirstDayOfWeekOverride, WeekDayCount, HiddenDays);
        return (MinDate is not { } min || end.Date >= min.Date)
               && (MaxDate is not { } max || start.Date <= max.Date);
    }

    internal void SetView(BitFullCalendarView view)
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
    internal void SetMode(BitFullCalendarMode mode)
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
        // The range moves when the View is clamped, and in the month view the timeline shows the
        // month alone where the event grid also shows the neighbouring months' days. Reported only
        // when it really changed (NotifyDateRangeChanged skips an identical range), matching the
        // BitFullCalendar.razor.cs visible-range contract.
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
    internal void SetReadOnly(bool value)
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
    internal void SyncViews(IReadOnlyList<BitFullCalendarView>? views)
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

    internal void SetUse24HourFormat(bool value)
    {
        if (Use24HourFormat == value)
            return;
        Use24HourFormat = value;
        NotifyStateChanged();
    }

    internal void ToggleTimeFormat()
    {
        Use24HourFormat = !Use24HourFormat;
        NotifyStateChanged();
    }

    internal void SetBadgeVariant(BitFullCalendarBadgeVariant variant)
    {
        if (BadgeVariant == variant)
            return;
        BadgeVariant = variant;
        NotifyStateChanged();
    }

    internal void SetStartOfDayHour(int hour)
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
    /// Sets the hour window the day/week time grids render. The start is clamped to 0-23 and the end
    /// to 1-24, an end not after the start becomes the hour after it, and the scroll anchor is pulled
    /// back inside the window.
    /// </summary>
    internal void SetVisibleHours(int startHour, int endHour)
    {
        var (start, end) = BitFullCalendarHelpers.NormalizeVisibleHours(startHour, endHour);
        if (VisibleStartHour == start && VisibleEndHour == end)
            return;

        VisibleStartHour = start;
        VisibleEndHour = end;
        StartOfDayHour = Math.Clamp(StartOfDayHour, start, Math.Max(start, end - 1));
        NotifyStateChanged();
    }

    internal void SetSlotDurationMinutes(int minutes)
    {
        var normalized = new BitFullCalendarSettings { SlotDurationMinutes = minutes }.SlotDurationMinutes;
        if (SlotDurationMinutes == normalized)
            return;

        SlotDurationMinutes = normalized;
        SlotMinutes = BitFullCalendarHelpers.GetSlotMinutes(normalized);
        NotifyStateChanged();
    }

    internal void SetHiddenDays(IReadOnlyList<DayOfWeek>? hiddenDays)
    {
        var next = BitFullCalendarHelpers.NormalizeHiddenDays(hiddenDays).OrderBy(d => (int)d).ToList();
        if (HiddenDays.Count == next.Count && HiddenDays.All(next.Contains))
            return;

        HiddenDays = next;
        SyncDaySpan();
        UpdateUI();
        NotifyDateRangeChanged();
    }

    internal void SetFirstDayOfWeek(DayOfWeek? firstDayOfWeek)
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

    internal void SetShowWeekNumbers(bool value)
    {
        if (ShowWeekNumbers == value)
            return;
        ShowWeekNumbers = value;
        NotifyStateChanged();
    }

    internal void ToggleShowWeekNumbers()
    {
        ShowWeekNumbers = !ShowWeekNumbers;
        NotifyStateChanged();
    }

    internal void SetShowCurrentTimeIndicator(bool value)
    {
        if (ShowCurrentTimeIndicator == value)
            return;
        ShowCurrentTimeIndicator = value;
        NotifyStateChanged();
    }

    internal void ToggleShowCurrentTimeIndicator()
    {
        ShowCurrentTimeIndicator = !ShowCurrentTimeIndicator;
        NotifyStateChanged();
    }

    internal void SetMaxEventsPerDayCell(int value)
    {
        var clamped = Math.Clamp(value, 1, 10);
        if (MaxEventsPerDayCell == clamped)
            return;
        MaxEventsPerDayCell = clamped;
        NotifyStateChanged();
    }

    internal void SetRequireEventDescription(bool value)
    {
        if (RequireEventDescription == value)
            return;
        RequireEventDescription = value;
        NotifyStateChanged();
    }

    internal void SetAllowEventOverlap(bool value)
    {
        if (AllowEventOverlap == value)
            return;
        AllowEventOverlap = value;
        NotifyStateChanged();
    }

    /// <summary>Whether the user may create events (<see cref="BitFullCalendarSettings.AllowAdd"/>).</summary>
    public bool AllowAdd { get; private set; } = true;

    /// <summary>Whether the user may change existing events (<see cref="BitFullCalendarSettings.AllowEdit"/>).</summary>
    public bool AllowEdit { get; private set; } = true;

    /// <summary>Whether the user may delete events (<see cref="BitFullCalendarSettings.AllowDelete"/>).</summary>
    public bool AllowDelete { get; private set; } = true;

    /// <summary>Whether the user may move events (<see cref="BitFullCalendarSettings.AllowDrag"/>).</summary>
    public bool AllowDrag { get; private set; } = true;

    /// <summary>Whether the user may resize events (<see cref="BitFullCalendarSettings.AllowResize"/>).</summary>
    public bool AllowResize { get; private set; } = true;

    /// <summary>True when the user may create an event: the calendar is editable and adding is allowed.</summary>
    public bool CanAdd => ReadOnly is false && AllowAdd;

    /// <summary>True when the user may change <paramref name="ev"/> through the details dialog.</summary>
    public bool CanEdit(BitFullCalendarEvent ev) => ReadOnly is false && AllowEdit && ev.IsReadOnly is false;

    /// <summary>True when the user may delete <paramref name="ev"/>.</summary>
    public bool CanDelete(BitFullCalendarEvent ev) => ReadOnly is false && AllowDelete && ev.IsReadOnly is false;

    /// <summary>True when the user may move <paramref name="ev"/>, by pointer or keyboard.</summary>
    public bool CanDrag(BitFullCalendarEvent ev) => CanEdit(ev) && AllowDrag;

    /// <summary>True when the user may resize <paramref name="ev"/>, by pointer or keyboard.</summary>
    public bool CanResize(BitFullCalendarEvent ev) => CanEdit(ev) && AllowResize;

    /// <summary>Applies the five edit permissions of <see cref="BitFullCalendarSettings"/> at once.</summary>
    internal void SetEditPermissions(bool add, bool edit, bool delete, bool drag, bool resize)
    {
        if ((AllowAdd, AllowEdit, AllowDelete, AllowDrag, AllowResize) == (add, edit, delete, drag, resize))
            return;

        (AllowAdd, AllowEdit, AllowDelete, AllowDrag, AllowResize) = (add, edit, delete, drag, resize);
        // A gesture under way when moving is taken away must not commit on release.
        if (DraggedEvent is { } dragged && CanDrag(dragged) is false)
            DraggedEvent = null;
        NotifyStateChanged();
    }

    internal void SetAllowRangeSelection(bool value)
    {
        if (AllowRangeSelection == value)
            return;
        AllowRangeSelection = value;
        NotifyStateChanged();
    }

    internal void SetBusinessDays(IReadOnlyList<DayOfWeek>? businessDays)
    {
        var next = BitFullCalendarHelpers.NormalizeBusinessDays(businessDays);
        if (BusinessDays.Count == next.Count && BusinessDays.All(next.Contains))
            return;

        BusinessDays = next;
        NotifyStateChanged();
    }

    internal void SetBusinessHours(int startHour, int endHour)
    {
        var (start, end) = BitFullCalendarHelpers.NormalizeBusinessHours(startHour, endHour);
        if (BusinessStartHour == start && BusinessEndHour == end)
            return;

        BusinessStartHour = start;
        BusinessEndHour = end;
        NotifyStateChanged();
    }

    internal void SetHighlightBusinessHours(bool value)
    {
        if (HighlightBusinessHours == value)
            return;
        HighlightBusinessHours = value;
        NotifyStateChanged();
    }

    internal void ToggleHighlightBusinessHours()
    {
        HighlightBusinessHours = !HighlightBusinessHours;
        NotifyStateChanged();
    }

    internal void SetRestrictToBusinessHours(bool value)
    {
        if (RestrictToBusinessHours == value)
            return;
        RestrictToBusinessHours = value;
        NotifyStateChanged();
    }

    internal void SetFixedWeekCount(bool value)
    {
        if (FixedWeekCount == value)
            return;
        FixedWeekCount = value;
        // A sixth row moves the end of the month view's range, and the recurrences are expanded over the range.
        UpdateUI();
        NotifyDateRangeChanged();
    }

    internal void SetShowNonCurrentDates(bool value)
    {
        if (ShowNonCurrentDates == value)
            return;
        ShowNonCurrentDates = value;
        // The borrowed days are in the month view's range only while they are shown, and the recurrences are
        // expanded over the range.
        UpdateUI();
        NotifyDateRangeChanged();
    }

    internal void SetNavLinks(bool value)
    {
        if (NavLinks == value)
            return;
        NavLinks = value;
        NotifyStateChanged();
    }

    /// <summary>True when the supplied weekday is one the business hours run on.</summary>
    public bool IsBusinessDay(DayOfWeek day) => BusinessDays.Contains(day);

    /// <summary>
    /// True when the supplied instant falls inside the business hours: on a business day, at or
    /// after <see cref="BusinessStartHour"/> and before <see cref="BusinessEndHour"/>.
    /// </summary>
    public bool IsBusinessTime(DateTime moment)
        => IsBusinessDay(moment.DayOfWeek)
           && moment.TimeOfDay >= TimeSpan.FromHours(BusinessStartHour)
           && moment.TimeOfDay < TimeSpan.FromHours(BusinessEndHour);

    /// <summary>
    /// True when the whole <c>[start, end)</c> range sits inside the business hours of the days it
    /// covers.
    /// <para>
    /// A range that covers whole days end to end - an all-day event, in other words - has no clock
    /// time to judge, so only the days it falls on are checked. That matches how the other calendar
    /// libraries apply a business-hours constraint to an all-day event.
    /// </para>
    /// </summary>
    public bool IsWithinBusinessHours(DateTime start, DateTime end)
    {
        if (end <= start)
            return IsBusinessTime(start);

        if (start.TimeOfDay == TimeSpan.Zero && end.TimeOfDay == TimeSpan.Zero)
        {
            for (var wholeDay = start.Date; wholeDay < end.Date; wholeDay = wholeDay.AddDays(1))
            {
                if (!IsBusinessDay(wholeDay.DayOfWeek))
                    return false;
            }
            return true;
        }

        var businessStart = TimeSpan.FromHours(BusinessStartHour);
        var businessEnd = TimeSpan.FromHours(BusinessEndHour);

        // The end is exclusive, so a range ending exactly at midnight ends on the previous day.
        var lastDate = end.AddTicks(-1).Date;
        for (var day = start.Date; day <= lastDate; day = day.AddDays(1))
        {
            if (!IsBusinessDay(day.DayOfWeek))
                return false;

            var dayStart = day > start.Date ? day : start;
            var dayEnd = day < lastDate ? day.AddDays(1) : end;
            if (dayStart.TimeOfDay < businessStart)
                return false;
            // A part that runs to midnight only fits when the business day itself does.
            var endOfDay = dayEnd.Date > day ? TimeSpan.FromHours(24) : dayEnd.TimeOfDay;
            if (endOfDay > businessEnd)
                return false;
        }

        return true;
    }

    /// <summary>
    /// The single gate every user-driven move, resize, and save passes through: the resulting range
    /// has to stay inside the allowed date window, inside the business hours when they are enforced,
    /// and free of a conflicting event when double booking is disallowed. Returns
    /// <see cref="BitFullCalendarChangeRefusal.None"/> when the change may be committed.
    /// </summary>
    public BitFullCalendarChangeRefusal ValidateRange(string eventId, DateTime start, DateTime end, string? resourceId)
    {
        // The inclusive end is what the bounds are measured against, so a range ending at midnight
        // is not judged against the day it never actually reaches.
        var inclusiveEnd = end > start ? end.AddTicks(-1) : end;
        if (IsDateInAllowedRange(start) is false || IsDateInAllowedRange(inclusiveEnd) is false)
            return BitFullCalendarChangeRefusal.OutOfRange;

        if (RestrictToBusinessHours && IsWithinBusinessHours(start, end) is false)
            return BitFullCalendarChangeRefusal.OutsideBusinessHours;

        if (FindBlockingEvent(eventId, start, end, resourceId) is not null)
            return BitFullCalendarChangeRefusal.Blocked;

        if (IsRangeAvailable(eventId, start, end, resourceId) is false)
            return BitFullCalendarChangeRefusal.Overlap;

        return BitFullCalendarChangeRefusal.None;
    }

    /// <summary>
    /// The first <see cref="BitFullCalendarEvent.IsBlocking"/> event the supplied range would overlap on
    /// <paramref name="resourceId"/> - one blocking every resource (no <see cref="BitFullCalendarEvent.Resource"/>) or
    /// that resource itself - or <c>null</c> when the range is free of them. An all-day blocker covers its whole days.
    /// </summary>
    public BitFullCalendarEvent? FindBlockingEvent(string eventId, DateTime start, DateTime end, string? resourceId)
    {
        // A zero-length range still occupies its instant, so it is measured as one tick long.
        var rangeEnd = end > start ? end : start.AddTicks(1);
        foreach (var other in GetEventsOccupying(start, rangeEnd))
        {
            if (other.IsBlocking is false)
                continue;
            if (IsSelfOrSeries(other, eventId))
                continue;
            if (other.Resource is not null && string.Equals(other.Resource, resourceId ?? "", StringComparison.Ordinal) is false)
                continue;

            var (blockStart, blockEnd) = BitFullCalendarHelpers.GetOccupiedRange(other);
            if (start < blockEnd && blockStart < rangeEnd)
                return other;
        }

        return null;
    }

    /// <summary>
    /// The background events whose time overlaps the supplied range on <paramref name="resourceId"/> (one with no
    /// resource lies on every row), in the order they were supplied. Pass <paramref name="anyResource"/> for the
    /// event-mode grids, which have no resource rows.
    /// </summary>
    public IEnumerable<BitFullCalendarEvent> GetBackgroundEventsAt(DateTime start, DateTime end, string? resourceId = null, bool anyResource = true)
        => BitFullCalendarHelpers.GetBackgroundEventsAt(_backgroundEvents, start, end, resourceId, anyResource);

    /// <summary>
    /// The background events covering the whole of <paramref name="day"/>: an all-day one, or a timed one running from
    /// before its start to after its end. These are what a day's header (or a month day) names, since a band that
    /// fills the column has no edge of its own to carry a title where it can be seen.
    /// </summary>
    public List<BitFullCalendarEvent> GetWholeDayBackground(DateTime day)
    {
        if (_backgroundEvents.Count == 0) return [];

        var dayStart = day.Date;
        var dayEnd = dayStart.AddDays(1);
        return GetBackgroundEventsAt(dayStart, dayEnd)
            .Where(e =>
            {
                var (start, end) = BitFullCalendarHelpers.GetOccupiedRange(e);
                return start <= dayStart && end >= dayEnd;
            })
            .ToList();
    }

    /// <summary>
    /// The titles of the background events covering the supplied range, each led by a comma, for the accessible name
    /// of the slot or day they cover - the band itself is hidden from assistive technology while the grid is
    /// editable, so this is where its meaning is spoken. Empty when nothing covers the range.
    /// </summary>
    internal string DescribeBackground(DateTime start, DateTime end, string? resourceId = null, bool anyResource = true)
    {
        if (_backgroundEvents.Count == 0) return string.Empty;

        return string.Concat(GetBackgroundEventsAt(start, end, resourceId, anyResource)
            .Where(e => string.IsNullOrWhiteSpace(e.Title) is false)
            .Select(e => $", {e.Title}"));
    }

    /// <summary>
    /// True when the supplied range may be committed for <paramref name="eventId"/>: either overlaps
    /// are allowed, or no other event on the same resource occupies any part of that range.
    /// </summary>
    // The event a change is measured for never collides with itself: neither with its own projection (an occurrence
    // being moved on its own, by its occurrence id) nor, when the id is a series master's, with its occurrences.
    private static bool IsSelfOrSeries(BitFullCalendarEvent other, string eventId)
        => string.Equals(other.Id, eventId, StringComparison.Ordinal)
           || string.Equals(other.SeriesId, eventId, StringComparison.Ordinal);

    /// <summary>
    /// The events occupying any part of <paramref name="start"/>..<paramref name="end"/>, occurrences included: the
    /// expanded events when the range lies inside the window they were expanded over, else every series expanded over
    /// the range itself - an add or a move can target a date the visible range never reaches, and a recurring
    /// blocker or booking there still counts.
    /// </summary>
    private List<BitFullCalendarEvent> GetEventsOccupying(DateTime start, DateTime end)
    {
        if (_expansionWindow is not { } window)
            return _expandedEvents;

        var lastDay = (end > start ? end.AddTicks(-1) : end).Date;
        if (start.Date >= window.Start.Date && lastDay <= window.End.Date)
            return _expandedEvents;

        return BitFullCalendarHelpers.ExpandRecurrences(_allEvents, start, lastDay);
    }

    /// <summary>
    /// Whether an event may take a time range without overlapping another event on the same resource. Always
    /// <c>true</c> while <see cref="AllowEventOverlap"/> is on. Background events, the event itself and the other
    /// occurrences of its series are not counted. <see cref="ValidateRange"/> asks this along with the other rules.
    /// </summary>
    /// <param name="eventId">The id of the event being placed, so its own place is not counted against it.</param>
    /// <param name="start">The start of the range.</param>
    /// <param name="end">The end of the range.</param>
    /// <param name="resourceId">The resource the event would be on, or <c>null</c> for none; two events with no
    /// resource share one.</param>
    /// <returns><c>true</c> when nothing on that resource overlaps the range.</returns>
    public bool IsRangeAvailable(string eventId, DateTime start, DateTime end, string? resourceId)
    {
        if (AllowEventOverlap)
            return true;

        var candidate = new BitFullCalendarEvent { StartDate = start, EndDate = end };
        // Measured against what actually occupies the range, so a recurring occurrence blocks its slot just like a
        // one-off event does.
        foreach (var other in GetEventsOccupying(start, end))
        {
            // A background event is context, not a booking: it only keeps others out when it is blocking.
            if (other.IsBackground)
                continue;
            // An occurrence belongs to its master, so a series never collides with itself.
            if (IsSelfOrSeries(other, eventId))
                continue;
            // Only events sharing the resource lane can collide; two unassigned events do share one.
            if (!string.Equals(other.Resource ?? "", resourceId ?? "", StringComparison.Ordinal))
                continue;
            if (BitFullCalendarHelpers.EventsOverlap(candidate, other))
                return false;
        }

        return true;
    }

    internal void SetAgendaModeGroupBy(BitFullCalendarAgendaGroupBy groupBy)
    {
        if (AgendaModeGroupBy == groupBy)
            return;
        AgendaModeGroupBy = groupBy;
        NotifyStateChanged();
    }
    internal void SetEventLayout(BitFullCalendarEventLayout layout)
    {
        if (EventLayout == layout)
            return;
        EventLayout = layout;
        NotifyStateChanged();
    }

    internal void SetShowDayViewCalendar(bool value)
    {
        if (ShowDayViewCalendar == value)
            return;
        ShowDayViewCalendar = value;
        NotifyStateChanged();
    }

    internal void ToggleShowDayViewCalendar()
    {
        ShowDayViewCalendar = !ShowDayViewCalendar;
        NotifyStateChanged();
    }

    internal void ToggleEventLayout()
    {
        EventLayout = EventLayout == BitFullCalendarEventLayout.Overlap
            ? BitFullCalendarEventLayout.Stack
            : BitFullCalendarEventLayout.Overlap;
        NotifyStateChanged();
    }
    internal void NavigatePrevious()
    {
        if (CanNavigatePrevious is false)
            return;

        Step(forward: false);
        UpdateUI();
        NotifyDateRangeChanged();
    }

    internal void NavigateNext()
    {
        if (CanNavigateNext is false)
            return;

        Step(forward: true);
        UpdateUI();
        NotifyDateRangeChanged();
    }

    private void Step(bool forward)
    {
        var target = BitFullCalendarHelpers.NavigateDate(WeekAnchor, View, forward, Culture, HiddenDays, WeekDayCount);
        // A shorter week turns to the next run of days, which starts on the date the step lands on.
        if (View == BitFullCalendarView.Week && WeekDayCount is not null)
            _daySpanStart = target.Date;
        SelectedDate = ClampToAllowedRange(target);
    }

    internal void GoToToday()
    {
        SelectedDate = ClampToAllowedRange(Today);
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
    internal void SyncEvents(List<BitFullCalendarEvent> events)
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
    internal void SyncResources(IReadOnlyList<BitFullCalendarResource>? resources)
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
        // calendar never stays in the unsupported timeline-without-resources state. Through SetMode,
        // like any other change of mode: the projections are rebuilt for Event mode, and the visible
        // range it moves to (a month view shows the neighbouring months' days again) is reported.
        if (Mode == BitFullCalendarMode.Timeline && !IsTimelineModeAvailable)
        {
            SetMode(BitFullCalendarMode.Event);
            return;
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

    internal void AddEvent(BitFullCalendarEvent ev)
    {
        _allEvents.Add(ev);
        NormalizeEventIds();
        UpdateUI();
    }

    internal void UpdateEvent(BitFullCalendarEvent ev)
    {
        var idx = _allEvents.FindIndex(e => e.Id == ev.Id);
        if (idx >= 0) _allEvents[idx] = ev;
        UpdateUI();
    }

    /// <summary>
    /// Puts back an event a refused or failed change had removed, unless one with its id is already there - a consumer
    /// list re-synced while the change was awaited brings it back on its own, and adding it again would show it twice.
    /// </summary>
    internal void RestoreEvent(BitFullCalendarEvent ev)
    {
        if (_allEvents.Exists(e => e.Id == ev.Id))
        {
            UpdateUI();
            return;
        }

        AddEvent(ev);
    }

    internal void RemoveEvent(string eventId)
    {
        _allEvents.RemoveAll(e => e.Id == eventId);
        UpdateUI();
    }

    /// <summary>
    /// Adds a color to the colors the color filter keeps, or takes it out when it is already there (ignoring case). A
    /// blank id does nothing.
    /// </summary>
    /// <param name="colorId">The id of a <see cref="BitFullCalendarColorOption"/>.</param>
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

    /// <summary>
    /// Shows only the events of one color, as the header's color filter does, or the events of every color for a
    /// <c>null</c> or blank id. An event matches by the color option it is drawn in, so one left at the default color
    /// matches the palette's fallback color.
    /// </summary>
    /// <param name="colorId">The id of a <see cref="BitFullCalendarColorOption"/>, or <c>null</c>.</param>
    public void SetColorFilter(string? colorId)
    {
        _selectedColors.Clear();
        if (!string.IsNullOrWhiteSpace(colorId))
            _selectedColors.Add(colorId.Trim());

        UpdateUI();
    }

    /// <summary>
    /// Shows only the events an attendee takes part in, as the header's attendee filter does, or every event for a
    /// <c>null</c> or blank key. The filter is dropped whenever no event in view has that attendee, including at once
    /// for a key no event there has.
    /// </summary>
    /// <param name="attendeeKey">A key of <see cref="GetAttendeesInCurrentView"/>, in the form
    /// <see cref="SelectedAttendeeKey"/> describes, or <c>null</c>.</param>
    public void SetAttendeeFilter(string? attendeeKey)
    {
        SelectedAttendeeKey = string.IsNullOrWhiteSpace(attendeeKey) ? null : attendeeKey.Trim();
        UpdateUI();
    }

    internal void UpdateUI()
    {
        ApplyFilters();
        NotifyStateChanged();
    }

    /// <summary>Distinct attendees on events visible in the current view/date range.</summary>
    public IReadOnlyList<(string Key, string DisplayName)> GetAttendeesInCurrentView(string unnamedAttendeeText = "(Unnamed)")
    {
        var viewEvents = GetForegroundEventsInView();
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

    /// <summary>Clears the color and the attendee filter, so every event is shown.</summary>
    public void ClearFilter()
    {
        _selectedColors.Clear();
        SelectedAttendeeKey = null;
        SplitExpandedEvents();
        _filteredEvents = _foregroundEvents;
        NotifyStateChanged();
    }

    /// <summary>
    /// The window recurrences are expanded over: the visible range, widened by the days a grid shows
    /// from the neighbouring periods (a month grid leads and trails with them) so an occurrence in
    /// one of those cells is not missing.
    /// </summary>
    private (DateTime Start, DateTime End) GetExpansionRange()
    {
        var (start, end) = GetVisibleRange();
        var padding = View is BitFullCalendarView.Year ? 0 : 7;
        return (start.AddDays(-padding), end.AddDays(padding));
    }

    private List<BitFullCalendarEvent> ExpandForCurrentRange()
    {
        // Nothing to expand is the common case, so the walk is skipped entirely then.
        if (_allEvents.All(e => e.Recurrence is null))
        {
            _expansionWindow = null;
            return _allEvents;
        }

        var (start, end) = GetExpansionRange();
        _expansionWindow = (start, end);
        return BitFullCalendarHelpers.ExpandRecurrences(_allEvents, start, end);
    }

    // Expands the current range and splits it into the background bands and the foreground events, in one pass.
    private void SplitExpandedEvents()
    {
        _expandedEvents = ExpandForCurrentRange();
        _backgroundEvents = [];
        _foregroundEvents = new(_expandedEvents.Count);
        foreach (var ev in _expandedEvents)
            (ev.IsBackground ? _backgroundEvents : _foregroundEvents).Add(ev);
    }

    private void ApplyFilters()
    {
        SplitExpandedEvents();

        PruneInvalidAttendeeFilter();

        if (_selectedColors.Count == 0 && SelectedAttendeeKey is null)
        {
            _filteredEvents = _foregroundEvents;
            return;
        }

        IEnumerable<BitFullCalendarEvent> result = _foregroundEvents;

        if (_selectedColors.Count > 0)
            result = result.Where(e =>
            {
                var colorId = CanonicalColorId?.Invoke(e.Color) ?? e.Color;
                return _selectedColors.Any(c => string.Equals(c, colorId, StringComparison.OrdinalIgnoreCase));
            });

        if (SelectedAttendeeKey is not null)
            result = result.Where(e => e.Attendees.Any(a => BitFullCalendarHelpers.AttendeeFilterKey(a) == SelectedAttendeeKey));

        _filteredEvents = result.ToList();
    }

    private void PruneInvalidAttendeeFilter()
    {
        if (SelectedAttendeeKey is null)
            return;

        var validKeys = GetForegroundEventsInView()
            .SelectMany(e => e.Attendees)
            .Select(BitFullCalendarHelpers.AttendeeFilterKey)
            .Where(k => k.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        if (!validKeys.Contains(SelectedAttendeeKey))
            SelectedAttendeeKey = null;
    }

    // The foreground events the active view shows, which the attendee filter lists and keeps its selection by. The month
    // view shows the days its grid borrows from the neighbouring months and their events too (GetVisibleRange), so it
    // reads its own range rather than the month alone.
    private List<BitFullCalendarEvent> GetForegroundEventsInView()
    {
        if (View == BitFullCalendarView.Month)
        {
            var (start, end) = GetVisibleRange();
            return BitFullCalendarHelpers.GetEventsInRange(_foregroundEvents, start, end);
        }

        return BitFullCalendarHelpers.GetEventsForView(_foregroundEvents, View, WeekAnchor, Culture, FirstDayOfWeekOverride, WeekDayCount, HiddenDays);
    }

    // Drag-and-drop helpers
    internal void StartDrag(BitFullCalendarEvent ev)
    {
        // Single choke point for every drag entry point: a read-only calendar - or a single event
        // locked with BitFullCalendarEvent.IsReadOnly - never enters the dragging state, so the drop
        // handlers downstream have nothing to commit.
        if (ev is null || CanDrag(ev) is false)
            return;

        DraggedEvent = ev;
        NotifyStateChanged();
    }

    internal void EndDrag()
    {
        if (DraggedEvent == null)
            return;

        DraggedEvent = null;
        NotifyStateChanged();
    }

    internal BitFullCalendarChangeRefusal HandleDrop(DateTime targetDate, int? hour = null, int? minute = null)
        => HandleDrop(targetDate, hour, minute, resourceId: null, applyResource: false);

    /// <summary>
    /// Drops the currently dragged event onto a date/time and optionally re-assigns its
    /// <see cref="BitFullCalendarEvent.Resource"/>. When <paramref name="applyResource"/> is
    /// <c>false</c> the event keeps its existing resource. Pass <paramref name="resourceId"/> as
    /// <c>null</c> together with <paramref name="applyResource"/> = <c>true</c> to clear the
    /// resource (drop on the unassigned row).
    /// </summary>
    internal BitFullCalendarChangeRefusal HandleDrop(DateTime targetDate, int? hour, int? minute, string? resourceId, bool applyResource)
    {
        if (DraggedEvent is not { } dragged) return BitFullCalendarChangeRefusal.None;

        var (refusal, target) = ResolveDrop(targetDate, hour, minute, resourceId, applyResource);
        EndDrag();
        if (refusal is not BitFullCalendarChangeRefusal.None || target is not { } t)
            return refusal;

        // An occurrence moves on its own, as one meeting of a series does in a calendar app.
        if (dragged.IsOccurrence)
        {
            DetachOccurrence(dragged, t.Start, t.End, t.Resource);
            return BitFullCalendarChangeRefusal.None;
        }

        var updated = BitFullCalendarChangeNotifier.CloneEvent(dragged);
        updated.StartDate = t.Start;
        updated.EndDate = t.End;
        updated.Resource = t.Resource;
        UpdateEvent(updated);
        return BitFullCalendarChangeRefusal.None;
    }

    /// <summary>
    /// Works out where dropping the dragged event on the supplied date/time (and resource) puts it, under every rule a
    /// drop obeys, without changing anything. The target is <c>null</c> when the drop is refused or changes nothing.
    /// </summary>
    internal (BitFullCalendarChangeRefusal Refusal, (DateTime Start, DateTime End, string? Resource)? Target) ResolveDrop(
        DateTime targetDate, int? hour, int? minute, string? resourceId, bool applyResource)
    {
        if (DraggedEvent is not { } dragged) return (BitFullCalendarChangeRefusal.None, null);

        if (CanDrag(dragged) is false)
            return (BitFullCalendarChangeRefusal.ReadOnly, null);

        var originalStart = dragged.StartDate;
        var originalResource = dragged.Resource;

        var newStart = targetDate.Date;
        if (hour.HasValue)
            newStart = newStart.AddHours(hour.Value).AddMinutes(minute ?? 0);
        else
            newStart = newStart.AddHours(originalStart.Hour).AddMinutes(originalStart.Minute);

        var newResource = applyResource ? resourceId : originalResource;
        var resourceChanged = applyResource && !string.Equals(originalResource ?? "", newResource ?? "", StringComparison.Ordinal);

        if (newStart == originalStart && !resourceChanged)
            return (BitFullCalendarChangeRefusal.None, null);

        var newEnd = newStart + dragged.Duration;

        // One gate for every rule a drop has to obey: the allowed date window (otherwise the event
        // would land on a date the user can never navigate back to), the business hours when they
        // are enforced, and the booking rule.
        var refusal = ValidateRange(dragged.Id, newStart, newEnd, newResource);
        return refusal is BitFullCalendarChangeRefusal.None
            ? (refusal, (newStart, newEnd, newResource))
            : (refusal, null);
    }

    /// <summary>
    /// Moves one occurrence of a series on its own: its date is skipped on the series master and a one-off with the
    /// new range takes its place. Returns the master as it was, the master as it is now, and the one-off - or
    /// <c>null</c> when the series is gone.
    /// </summary>
    internal (BitFullCalendarEvent Master, BitFullCalendarEvent Skipped, BitFullCalendarEvent Detached)? DetachOccurrence(
        BitFullCalendarEvent occurrence, DateTime start, DateTime end, string? resource)
    {
        var master = _allEvents.FirstOrDefault(e => string.Equals(e.Id, occurrence.SeriesId, StringComparison.Ordinal));
        if (master is null || occurrence.OccurrenceDate is not { } date)
            return null;

        var skipped = BitFullCalendarHelpers.SkipOccurrence(master, date);
        var detached = BitFullCalendarChangeNotifier.CloneEvent(occurrence);
        detached.Id = Guid.NewGuid().ToString("N");
        detached.SeriesId = null;
        detached.OccurrenceDate = null;
        detached.Recurrence = null;
        detached.StartDate = start;
        detached.EndDate = end;
        detached.Resource = resource;

        LastDetach = (occurrence.Id, detached.Id);
        UpdateEvent(skipped);
        AddEvent(detached);
        return (master, skipped, detached);
    }

    /// <summary>
    /// The occurrence last moved out of its series and the one-off that replaced it, so the focus a keyboard move had
    /// on the occurrence can follow it to an element with a new key.
    /// </summary>
    internal (string From, string To)? LastDetach { get; private set; }

    /// <summary>Forgets <see cref="LastDetach"/>, once the move it describes has been taken back.</summary>
    internal void ClearLastDetach() => LastDetach = null;

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

    // While an update is open (BeginUpdate / EndUpdate), the changes it makes are announced once, when
    // it ends, rather than one notification per setter: a subscriber never sees the intermediate
    // states of a pass that applies many values at once (the component's parameters).
    private int _updateDepth;
    private bool _stateChangedDuringUpdate;

    /// <summary>
    /// Opens an update: every <see cref="OnStateChanged"/> it would raise is held back until the
    /// matching <see cref="EndUpdate"/>, which raises one for all of them. Updates nest; only the
    /// outermost one announces.
    /// </summary>
    internal void BeginUpdate() => _updateDepth++;

    /// <summary>
    /// Closes an update opened by <see cref="BeginUpdate"/>, raising one <see cref="OnStateChanged"/>
    /// if anything changed while it was open.
    /// </summary>
    internal void EndUpdate()
    {
        if (_updateDepth is 0 || --_updateDepth > 0)
            return;

        if (_stateChangedDuringUpdate is false)
            return;

        _stateChangedDuringUpdate = false;
        OnStateChanged?.Invoke();
    }

    private void NotifyStateChanged()
    {
        if (_updateDepth > 0)
        {
            _stateChangedDuringUpdate = true;
            return;
        }

        OnStateChanged?.Invoke();
    }

    // The last range actually reported. Several interactions move the selected date without moving
    // the range it sits in - clicking another day of the week that is already on screen, for one -
    // and re-reporting the identical range would have consumers re-fetch the same events.
    private (DateTime Start, DateTime End, BitFullCalendarView View)? _lastReportedRange;

    /// <summary>
    /// Records the range the calendar is showing as already reported, so the next navigation that
    /// lands on the very same range stays silent. The component calls this after raising the initial
    /// range itself on first render, which does not travel through <see cref="OnDateRangeChanged"/>.
    /// </summary>
    internal void MarkCurrentRangeReported()
    {
        var (start, end) = GetVisibleRange();
        _lastReportedRange = (start, end, View);
    }

    private void NotifyDateRangeChanged()
    {
        var (start, end) = GetVisibleRange();
        var range = (start, end, View);
        if (_lastReportedRange == range)
            return;

        _lastReportedRange = range;

        if (OnDateRangeChanged is null) return;
        OnDateRangeChanged.Invoke(new BitFullCalendarDateChangeEventArgs
        {
            Start = start,
            End = end,
            View = View
        });
    }
}

