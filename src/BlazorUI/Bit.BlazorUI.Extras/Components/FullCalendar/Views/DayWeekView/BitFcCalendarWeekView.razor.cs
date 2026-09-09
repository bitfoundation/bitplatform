namespace Bit.BlazorUI;

public partial class BitFcCalendarWeekView
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [CascadingParameter(Name = "OnAddClick")] public EventCallback<BitFullCalendarEvent?> OnAddClick { get; set; }
    [CascadingParameter(Name = "OnEventClick")] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }
    [Parameter] public List<BitFullCalendarEvent> SingleDayEvents { get; set; } = [];
    [Parameter] public List<BitFullCalendarEvent> MultiDayEvents { get; set; } = [];
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    private string? _timeGridScrollSignature;
    private readonly string _timeGridScrollElementId = "bit-bfc-week-timegrid-scroll-" + Guid.NewGuid().ToString("N");

    private bool _showAddDialog;
    private DateTime _addDate;
    private int _addHour;
    private int _addMinute;
    private int? _addDurationMinutes;

    private BitFullCalendarEvent? _selectedEvent;
    private DateTime? _dragDate;
    private int? _dragHour;
    private int? _dragMinute;

    // The hour slots exist only as add/drop targets, so a read-only grid must not expose hundreds
    // of focusable no-op buttons to keyboard and assistive-technology users. A null attribute value
    // is omitted from the rendered markup.
    private string? _slotRole => State.ReadOnly ? null : "button";

    // Roving tabindex: the grid is a single tab stop, and the arrow keys move both the tabbable slot
    // and the focus. Without it a week would put hundreds of stops in the tab order - seven days
    // times a slot per hour - and a keyboard user could not tab past the grid.
    private (int DayIndex, int Hour, int Minute)? _focusedSlot;
    private bool _pendingSlotFocus;

    private string SlotElementId(int dayIndex, int hour, int minute)
        => $"{_timeGridScrollElementId}-slot-{dayIndex}-{hour}-{minute}";

    private (int DayIndex, int Hour, int Minute) RovingSlot =>
        _focusedSlot is { } slot ? slot : (0, State.VisibleStartHour, State.SlotMinutes[0]);

    private string? SlotTabIndex(int dayIndex, int hour, int minute)
    {
        if (State.ReadOnly)
            return null;

        return RovingSlot == (dayIndex, hour, minute) ? "0" : "-1";
    }

    /// <summary>
    /// Moves the roving slot by <paramref name="dayDelta"/> columns and <paramref name="slotDelta"/>
    /// slots along the time axis, clamped to the grid, and remembers that the focus has to follow on
    /// the next render.
    /// </summary>
    private void MoveRovingSlot(int dayDelta, int slotDelta)
    {
        var slots = State.SlotMinutes;
        var (dayIndex, hour, minute) = RovingSlot;

        var index = Array.IndexOf(slots, minute);
        if (index < 0) index = 0;

        var absolute = ((hour - State.VisibleStartHour) * slots.Length) + index + slotDelta;
        var total = State.VisibleHourCount * slots.Length;
        absolute = Math.Clamp(absolute, 0, total - 1);

        var columns = Math.Max(1, State.VisibleWeekDayCount);
        // The chevrons follow the reading direction, so a right-to-left grid walks the columns the
        // other way round for the same key.
        var effectiveDayDelta = State.IsRtl ? -dayDelta : dayDelta;

        _focusedSlot = (
            Math.Clamp(dayIndex + effectiveDayDelta, 0, columns - 1),
            State.VisibleStartHour + (absolute / slots.Length),
            slots[absolute % slots.Length]);
        _pendingSlotFocus = true;
    }

    private void SetRovingSlot(int dayIndex, int hour, int minute)
    {
        _focusedSlot = (dayIndex, hour, minute);
        _pendingSlotFocus = true;
    }

    // Range selection: pressing on a slot and dragging down its column picks the span the new event
    // should cover. The selection is confined to the column it started in - an event belongs to one
    // day - and a press and release on the SAME slot is left to the click handler, which is the path
    // a plain click has always taken.
    private int? _selectionDayIndex;
    private int? _selectionAnchor;
    private int? _selectionFocus;

    private int SlotIndex(int hour, int minute)
    {
        var slots = State.SlotMinutes;
        var minuteIndex = Array.IndexOf(slots, minute);
        if (minuteIndex < 0) minuteIndex = 0;
        return ((hour - State.VisibleStartHour) * slots.Length) + minuteIndex;
    }

    private (int Hour, int Minute) SlotAt(int index)
    {
        var slots = State.SlotMinutes;
        var total = State.VisibleHourCount * slots.Length;
        index = Math.Clamp(index, 0, Math.Max(0, total - 1));
        return (State.VisibleStartHour + (index / slots.Length), slots[index % slots.Length]);
    }

    private bool CanSelectRange => State.ReadOnly is false && State.AllowRangeSelection;

    private bool IsSlotSelected(int dayIndex, int hour, int minute)
    {
        if (_selectionDayIndex != dayIndex || _selectionAnchor is not { } anchor || _selectionFocus is not { } focus)
            return false;

        var index = SlotIndex(hour, minute);
        return index >= Math.Min(anchor, focus) && index <= Math.Max(anchor, focus);
    }

    private void OnSlotMouseDown(int dayIndex, int hour, int minute)
    {
        if (CanSelectRange is false)
            return;

        _selectionDayIndex = dayIndex;
        _selectionAnchor = _selectionFocus = SlotIndex(hour, minute);
    }

    private void OnSlotMouseEnter(int dayIndex, int hour, int minute)
    {
        // Dragging into another day's column extends nothing: the range stays in its own column.
        if (_selectionAnchor is null || _selectionDayIndex != dayIndex)
            return;

        _selectionFocus = SlotIndex(hour, minute);
    }

    private async Task OnSlotMouseUpAsync(int dayIndex, DateTime day, int hour, int minute)
    {
        if (_selectionAnchor is not { } anchor || _selectionDayIndex != dayIndex)
        {
            CancelRangeSelection();
            return;
        }

        var focus = SlotIndex(hour, minute);
        CancelRangeSelection();

        // One slot means a plain click, which the click handler already covers.
        if (focus == anchor)
            return;

        var first = Math.Min(anchor, focus);
        var last = Math.Max(anchor, focus);
        var (startHour, startMinute) = SlotAt(first);
        await OpenAddForRangeAsync(day, startHour, startMinute, (last - first + 1) * State.SlotDurationMinutes);
    }

    private void CancelRangeSelection()
    {
        _selectionDayIndex = null;
        _selectionAnchor = null;
        _selectionFocus = null;
    }

    private async Task OpenAddForRangeAsync(DateTime day, int hour, int minute, int durationMinutes)
    {
        if (State.ReadOnly || State.IsDateInAllowedRange(day) is false)
            return;

        State.SetSelectedDate(day);

        if (OnAddClick.HasDelegate)
        {
            await OnAddClick.InvokeAsync(
                BitFullCalendarHelpers.CreateDraftEventForTimeSlot(day, hour, minute, durationMinutes));
            return;
        }

        _addDate = day;
        _addHour = hour;
        _addMinute = minute;
        _addDurationMinutes = durationMinutes;
        _showAddDialog = true;
    }

    private async Task SelectEvent(BitFullCalendarEvent ev)
    {
        if (OnEventClick.HasDelegate)
        {
            await OnEventClick.InvokeAsync(ev);
            return;
        }
        _selectedEvent = ev;
    }
    private void CloseEventDetails() => _selectedEvent = null;

    private async Task OnHourClickAsync(DateTime day, int hour, int minute = 0)
    {
        // The slot is purely an add affordance, so a read-only grid leaves it inert - including the
        // date selection, which the user can still perform from the header and the mini calendar.
        // A day outside the allowed window is inert for the same reason.
        if (State.ReadOnly || State.IsDateInAllowedRange(day) is false)
            return;

        State.SetSelectedDate(day);

        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(day, hour, minute, State.SlotDurationMinutes);
            await OnAddClick.InvokeAsync(draft);
            return;
        }

        _addDate = day;
        _addHour = hour;
        _addMinute = minute;
        // A plain click covers one slot; only a dragged range carries its own length.
        _addDurationMinutes = null;
        _showAddDialog = true;
    }

    private async Task OnHourKeyDownAsync(KeyboardEventArgs e, int dayIndex, DateTime day, int hour, int minute = 0)
    {
        // The slot the key came from is the one the roving tabindex should sit on, whatever the
        // previous arrow keys had selected.
        switch (e.Key)
        {
            case "Enter" or " " or "Spacebar":
                // Ignore auto-repeat so a held key only creates a single draft event.
                if (e.Repeat is false)
                    await OnHourClickAsync(day, hour, minute);
                return;

            case "ArrowDown":
                SetRovingSlot(dayIndex, hour, minute);
                MoveRovingSlot(0, 1);
                return;

            case "ArrowUp":
                SetRovingSlot(dayIndex, hour, minute);
                MoveRovingSlot(0, -1);
                return;

            case "ArrowRight":
                SetRovingSlot(dayIndex, hour, minute);
                MoveRovingSlot(1, 0);
                return;

            case "ArrowLeft":
                SetRovingSlot(dayIndex, hour, minute);
                MoveRovingSlot(-1, 0);
                return;

            case "PageDown":
                SetRovingSlot(dayIndex, hour, minute);
                MoveRovingSlot(0, State.SlotMinutes.Length);
                return;

            case "PageUp":
                SetRovingSlot(dayIndex, hour, minute);
                MoveRovingSlot(0, -State.SlotMinutes.Length);
                return;

            case "Home":
                SetRovingSlot(dayIndex, State.VisibleStartHour, State.SlotMinutes[0]);
                return;

            case "End":
                SetRovingSlot(dayIndex, State.VisibleEndHour - 1, State.SlotMinutes[^1]);
                return;
        }
    }

    private string? HourSlotAriaLabel(DateTime day, int hour, int minute = 0)
    {
        // The slot is inert in read-only mode, so it carries no label to announce.
        if (State.ReadOnly)
            return null;

        var start = day.Date.AddHours(hour).AddMinutes(minute);
        return $"{Texts.AddEventHoverHint}, {day.ToString("ddd", State.Culture)} {BitFullCalendarHelpers.FormatTime(start, State.Use24HourFormat, State.Culture)}";
    }

    private async Task OnDrop(DateTime day, int hour, int minute)
    {
        _dragDate = null;
        _dragHour = null;
        _dragMinute = null;

        if (State.IsDateInAllowedRange(day) is false)
        {
            State.EndDrag();
            Notifier.ReportRefusal(BitFullCalendarChangeRefusal.OutOfRange);
            return;
        }

        await Notifier.HandleDropAsync(day, hour, minute);
    }

    private void OnDragEnterSlot(DateTime day, int hour, int minute)
    {
        if (!State.IsDragging)
            return;

        _dragDate = day.Date;
        _dragHour = hour;
        _dragMinute = minute;
    }

    private string GetWeekDropClass(DateTime day, int hour, int minute)
    {
        if (!State.IsDragging)
            return string.Empty;

        // The first slot of an hour keeps the "hour" highlight; every later slot uses the plain one.
        return _dragDate == day.Date && _dragHour == hour && _dragMinute == minute
            ? (minute == 0 ? "bit-bfc-drop-preview-hour" : "bit-bfc-drop-preview-half")
            : string.Empty;
    }

    private string BuildTimeGridScrollSignature() =>
        $"{State.SelectedDate:yyyy-MM-dd}|{State.StartOfDayHour}|{State.VisibleStartHour}|{State.VisibleEndHour}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The arrow keys only moved the tabbable slot; the focus has to follow it here, once the
        // new tabindex has actually been rendered.
        if (_pendingSlotFocus)
        {
            _pendingSlotFocus = false;
            var (dayIndex, hour, minute) = RovingSlot;
            await BitFcFocusInterop.TryFocusAsync(JS, SlotElementId(dayIndex, hour, minute));
        }

        var sig = BuildTimeGridScrollSignature();
        if (sig == _timeGridScrollSignature)
            return;

        // The grid's first row is VisibleStartHour, so the scroll offset is measured from there.
        if (await BitFcTimeGridScrollInterop.TryScrollToStartOfDayAsync(
                JS,
                _timeGridScrollElementId,
                State.StartOfDayHour - State.VisibleStartHour))
            _timeGridScrollSignature = sig;
    }
}
