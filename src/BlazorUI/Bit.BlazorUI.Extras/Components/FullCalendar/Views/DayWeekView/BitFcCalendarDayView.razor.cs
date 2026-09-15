namespace Bit.BlazorUI;

public partial class BitFcCalendarDayView : IDisposable
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
    private readonly string _scrollContainerId = "bit-bfc-day-timegrid-scroll-" + Guid.NewGuid().ToString("N");
    private Timer? _nowTimer;
    private bool _isDisposed;

    private bool _showAddDialog;
    private DateTime _addStartDate;
    private int _addStartHour;
    private int _addStartMinute;
    private int? _addDurationMinutes;

    private BitFullCalendarEvent? _selectedEvent;
    private int? _dragHour;
    private int? _dragMinute;

    // The hour slots exist only as add/drop targets, so a read-only grid must not expose a focusable
    // no-op button per slot to keyboard and assistive-technology users. A null attribute value is
    // omitted from the rendered markup.
    private string? _slotRole => State.ReadOnly ? null : "button";

    // Roving tabindex: the grid is a single tab stop, and the arrow keys move both the tabbable slot
    // and the focus. Without it a day would put one stop in the tab order per slot - 48 at the
    // default half-hour grid, 96 at a quarter-hour one - and a keyboard user could not tab past it.
    private (int Hour, int Minute)? _focusedSlot;
    private bool _pendingSlotFocus;

    private string SlotElementId(int hour, int minute) => $"{_scrollContainerId}-slot-{hour}-{minute}";

    /// <summary>
    /// Shades a slot that falls outside the business hours, so the schedulable part of the day reads
    /// at a glance. Off unless the consumer asked for it.
    /// </summary>
    private string? OffHoursClass(int hour, int minute)
        => State.HighlightBusinessHours
           && State.IsBusinessTime(State.SelectedDate.Date.AddHours(hour).AddMinutes(minute)) is false
            ? "bit-bfc-slot-off"
            : null;

    /// <summary>
    /// True when a remembered slot still exists in the grid being rendered. The visible hour window
    /// and the slot duration are settings the user can change while the view is open, which would
    /// otherwise leave the tab stop on an hour or a minute the grid no longer draws - and the grid
    /// with no tab stop at all.
    /// </summary>
    private bool IsSlotInGrid((int Hour, int Minute) slot)
        => slot.Hour >= State.VisibleStartHour
           && slot.Hour < State.VisibleEndHour
           && Array.IndexOf(State.SlotMinutes, slot.Minute) >= 0;

    private (int Hour, int Minute) RovingSlot =>
        _focusedSlot is { } slot && IsSlotInGrid(slot)
            ? slot
            : (State.VisibleStartHour, State.SlotMinutes[0]);

    private string? SlotTabIndex(int hour, int minute)
    {
        if (State.ReadOnly)
            return null;

        return RovingSlot == (hour, minute) ? "0" : "-1";
    }

    /// <summary>
    /// Moves the roving slot by <paramref name="delta"/> slots along the time axis, clamped to the
    /// grid, and remembers that the focus has to follow on the next render.
    /// </summary>
    private void MoveRovingSlot(int delta)
    {
        var slots = State.SlotMinutes;
        var (hour, minute) = RovingSlot;
        var index = Array.IndexOf(slots, minute);
        if (index < 0) index = 0;

        var absolute = ((hour - State.VisibleStartHour) * slots.Length) + index + delta;
        var total = State.VisibleHourCount * slots.Length;
        absolute = Math.Clamp(absolute, 0, total - 1);

        SetRovingSlot(State.VisibleStartHour + (absolute / slots.Length), slots[absolute % slots.Length]);
    }

    private void SetRovingSlot(int hour, int minute)
    {
        _focusedSlot = (hour, minute);
        _pendingSlotFocus = true;
    }

    // Range selection: pressing on a slot and dragging over its neighbours picks the span the new
    // event should cover. A press and release on the SAME slot is left to the click handler, which
    // is the path a plain click has always taken, so a single slot is never handled twice.
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

    private bool IsSlotSelected(int hour, int minute)
    {
        if (_selectionAnchor is not { } anchor || _selectionFocus is not { } focus)
            return false;

        var index = SlotIndex(hour, minute);
        return index >= Math.Min(anchor, focus) && index <= Math.Max(anchor, focus);
    }

    private void OnSlotMouseDown(int hour, int minute)
    {
        if (CanSelectRange is false)
            return;

        _selectionAnchor = _selectionFocus = SlotIndex(hour, minute);
    }

    private void OnSlotMouseEnter(int hour, int minute)
    {
        if (_selectionAnchor is null)
            return;

        _selectionFocus = SlotIndex(hour, minute);
    }

    private async Task OnSlotMouseUpAsync(int hour, int minute)
    {
        if (_selectionAnchor is not { } anchor)
            return;

        var focus = SlotIndex(hour, minute);
        _selectionAnchor = null;
        _selectionFocus = null;

        // One slot means a plain click, which the click handler already covers.
        if (focus == anchor)
            return;

        var first = Math.Min(anchor, focus);
        var last = Math.Max(anchor, focus);
        var (startHour, startMinute) = SlotAt(first);
        await OpenAddForRangeAsync(startHour, startMinute, (last - first + 1) * State.SlotDurationMinutes);
    }

    private void CancelRangeSelection()
    {
        _selectionAnchor = null;
        _selectionFocus = null;
    }

    private async Task OpenAddForRangeAsync(int hour, int minute, int durationMinutes)
    {
        if (State.ReadOnly)
            return;

        if (OnAddClick.HasDelegate)
        {
            await OnAddClick.InvokeAsync(
                BitFullCalendarHelpers.CreateDraftEventForTimeSlot(State.SelectedDate, hour, minute, durationMinutes));
            return;
        }

        _addStartDate = State.SelectedDate;
        _addStartHour = hour;
        _addStartMinute = minute;
        _addDurationMinutes = durationMinutes;
        _showAddDialog = true;
    }

    protected override void OnInitialized()
    {
        // The "Happening now" panel is derived from DateTime.Now; refresh once a minute so it
        // doesn't go stale during long sessions. The callback can fire after disposal, so guard
        // against re-rendering a disposed component.
        _nowTimer = new Timer(_ =>
        {
            if (_isDisposed)
                return;
            InvokeAsync(() =>
            {
                if (_isDisposed)
                    return;
                StateHasChanged();
            });
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
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

    private async Task OnHourClickAsync(int hour, int minute = 0)
    {
        if (State.ReadOnly)
            return;

        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(
                State.SelectedDate, hour, minute, State.SlotDurationMinutes);
            await OnAddClick.InvokeAsync(draft);
            return;
        }

        _addStartDate = State.SelectedDate;
        _addStartHour = hour;
        _addStartMinute = minute;
        // A plain click covers one slot; only a dragged range carries its own length.
        _addDurationMinutes = null;
        _showAddDialog = true;
    }

    private async Task OnHourKeyDownAsync(KeyboardEventArgs e, int hour, int minute = 0)
    {
        // The slot the key came from is the one the roving tabindex should sit on, whatever the
        // previous arrow keys had selected.
        switch (e.Key)
        {
            case "Enter" or " " or "Spacebar":
                // Ignore auto-repeat so a held key only creates a single draft event.
                if (e.Repeat is false)
                    await OnHourClickAsync(hour, minute);
                return;

            case "ArrowDown":
                SetRovingSlot(hour, minute);
                MoveRovingSlot(1);
                return;

            case "ArrowUp":
                SetRovingSlot(hour, minute);
                MoveRovingSlot(-1);
                return;

            case "PageDown":
                SetRovingSlot(hour, minute);
                MoveRovingSlot(State.SlotMinutes.Length);
                return;

            case "PageUp":
                SetRovingSlot(hour, minute);
                MoveRovingSlot(-State.SlotMinutes.Length);
                return;

            case "Home":
                SetRovingSlot(State.VisibleStartHour, State.SlotMinutes[0]);
                return;

            case "End":
                SetRovingSlot(State.VisibleEndHour - 1, State.SlotMinutes[^1]);
                return;
        }
    }

    private string? HourSlotAriaLabel(int hour, int minute = 0)
    {
        // The slot is inert in read-only mode, so it carries no label to announce.
        if (State.ReadOnly)
            return null;

        var start = State.SelectedDate.Date.AddHours(hour).AddMinutes(minute);
        return $"{Texts.AddEventHoverHint}, {BitFullCalendarHelpers.FormatTime(start, State.Use24HourFormat, State.Culture)}";
    }

    private async Task OnDropHour(int hour, int minute)
    {
        _dragHour = null;
        _dragMinute = null;
        await Notifier.HandleDropAsync(State.SelectedDate, hour, minute);
    }

    private void OnDragEnterHour(int hour, int minute)
    {
        if (!State.IsDragging)
            return;

        _dragHour = hour;
        _dragMinute = minute;
    }

    private string GetHourDropClass(int hour, int minute)
    {
        if (!State.IsDragging)
            return string.Empty;

        // The first slot of an hour keeps the "hour" highlight (it shades the top of the row); every
        // later slot in the same hour uses the plain block highlight.
        return _dragHour == hour && _dragMinute == minute
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
            var (hour, minute) = RovingSlot;
            await BitFcFocusInterop.TryFocusAsync(JS, SlotElementId(hour, minute));
        }

        var sig = BuildTimeGridScrollSignature();
        if (sig == _timeGridScrollSignature)
            return;

        // The grid's first row is VisibleStartHour, so the scroll offset is measured from there
        // rather than from midnight.
        if (await BitFcTimeGridScrollInterop.TryScrollToStartOfDayAsync(
                JS,
                _scrollContainerId,
                State.StartOfDayHour - State.VisibleStartHour))
            _timeGridScrollSignature = sig;
    }

    public void Dispose()
    {
        _isDisposed = true;
        _nowTimer?.Dispose();
    }
}
