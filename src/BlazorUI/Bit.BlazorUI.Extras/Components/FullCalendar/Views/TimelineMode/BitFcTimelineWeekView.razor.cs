namespace Bit.BlazorUI;

public partial class BitFcTimelineWeekView
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [CascadingParameter(Name = "OnAddClick")] public EventCallback<BitFullCalendarEvent?> OnAddClick { get; set; }
    [CascadingParameter(Name = "OnEventClick")] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }

    [Parameter] public List<BitFullCalendarEvent> Events { get; set; } = [];
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    private const string _unassignedKey = "__bfc_unassigned__";
    private const int _laneHeight = 44;
    private const int _laneGap = 4;
    private const int _rowPadding = 4;
    private readonly string _scrollContainerId = "bit-bfc-tl-week-scroll-" + Guid.NewGuid().ToString("N");

    private string? _scrollSignature;

    private BitFullCalendarEvent? _selectedEvent;
    private bool _showAddDialog;
    private DateTime _addStartDate;
    private int _addStartHour;
    private int _addStartMinute;
    private string? _addResourceId;

    private string? _dragResourceId;
    private DateTime? _dragDay;
    private int? _dragHour;
    private int? _dragMinute;

    // The slots exist only as add/drop targets, so a read-only timeline must not expose a focusable
    // no-op button per slot and resource. A null attribute value is omitted from the markup.
    private string? _slotRole => State.ReadOnly ? null : "button";

    // Roving tabindex: the whole grid is a single tab stop and the arrow keys move both the tabbable
    // slot and the focus. A resource row per day per slot would otherwise put a four-figure number of
    // stops in the tab order.
    private List<string> _rowKeys = [];
    private DateTime[] _weekDays = [];
    private (string RowKey, DateTime Day, int Hour, int Minute)? _focusedSlot;
    private bool _pendingSlotFocus;

    private string SlotElementId(string rowKey, DateTime day, int hour, int minute)
        => $"{_scrollContainerId}-slot-{Math.Max(0, _rowKeys.IndexOf(rowKey))}-{day:yyyyMMdd}-{hour}-{minute}";

    private (string RowKey, DateTime Day, int Hour, int Minute) RovingSlot
    {
        get
        {
            if (_focusedSlot is { } slot
                && _rowKeys.Contains(slot.RowKey)
                && _weekDays.Any(d => d.Date == slot.Day.Date))
                return slot;

            return (
                _rowKeys.Count > 0 ? _rowKeys[0] : _unassignedKey,
                _weekDays.Length > 0 ? _weekDays[0] : State.SelectedDate.Date,
                State.VisibleStartHour,
                State.SlotMinutes[0]);
        }
    }

    private string? SlotTabIndex(string rowKey, DateTime day, int hour, int minute)
    {
        if (State.ReadOnly)
            return null;

        var roving = RovingSlot;
        return roving.RowKey == rowKey && roving.Day.Date == day.Date && roving.Hour == hour && roving.Minute == minute
            ? "0"
            : "-1";
    }

    /// <summary>
    /// Moves the roving slot by <paramref name="rowDelta"/> resource rows and
    /// <paramref name="slotDelta"/> slots along the time axis. The time axis spans the whole week,
    /// so a step past the end of a day rolls into the next one.
    /// </summary>
    private void MoveRovingSlot(int rowDelta, int slotDelta)
    {
        var slots = State.SlotMinutes;
        var (rowKey, day, hour, minute) = RovingSlot;

        var minuteIndex = Array.IndexOf(slots, minute);
        if (minuteIndex < 0) minuteIndex = 0;

        var slotsPerDay = State.VisibleHourCount * slots.Length;
        var dayIndex = Math.Max(0, Array.FindIndex(_weekDays, d => d.Date == day.Date));

        var absolute = (dayIndex * slotsPerDay)
            + ((hour - State.VisibleStartHour) * slots.Length)
            + minuteIndex
            + slotDelta;
        var total = Math.Max(1, _weekDays.Length) * slotsPerDay;
        absolute = Math.Clamp(absolute, 0, Math.Max(0, total - 1));

        var rowIndex = Math.Max(0, _rowKeys.IndexOf(rowKey));
        rowIndex = Math.Clamp(rowIndex + rowDelta, 0, Math.Max(0, _rowKeys.Count - 1));

        var newDayIndex = slotsPerDay > 0 ? absolute / slotsPerDay : 0;
        var withinDay = slotsPerDay > 0 ? absolute % slotsPerDay : 0;

        _focusedSlot = (
            _rowKeys.Count > 0 ? _rowKeys[rowIndex] : rowKey,
            _weekDays.Length > 0 ? _weekDays[Math.Clamp(newDayIndex, 0, _weekDays.Length - 1)] : day,
            State.VisibleStartHour + (withinDay / slots.Length),
            slots[withinDay % slots.Length]);
        _pendingSlotFocus = true;
    }

    private void SetRovingSlot(string rowKey, DateTime day, int hour, int minute)
    {
        _focusedSlot = (rowKey, day, hour, minute);
        _pendingSlotFocus = true;
    }

    private RenderFragment RenderLanes(List<List<BitFullCalendarEvent>> lanes, DateTime day, int dayOffsetPx) => builder =>
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        const int hourWidth = BitFullCalendarHelpers.TimelineHourWidthPx;

        for (var li = 0; li < lanes.Count; li++)
        {
            var laneTop = _rowPadding + (li * (_laneHeight + _laneGap));
            foreach (var ev in lanes[li])
            {
                var pos = BitFullCalendarHelpers.GetTimelineBlockPosition(
                    ev, day, hourWidth, State.VisibleStartHour, State.VisibleEndHour);
                if (pos is not { } p)
                    continue;

                // Anchor with inset-inline-start (not left) so blocks and the row's drop cells share
                // the same axis in a right-to-left layout.
                var style = $"inset-inline-start:{(dayOffsetPx + p.LeftPx).ToString("F2", inv)}px;width:{Math.Max(p.WidthPx, 12).ToString("F2", inv)}px;top:{laneTop}px;height:{_laneHeight}px;";
                // Key the per-event block by the event's stable identity so Blazor preserves the
                // correct BitFcTimelineEventBlock instance (and its in-flight drag/resize state) when
                // lane ordering is recomputed, instead of reusing a sibling's component by position.
                builder.OpenElement(0, "div");
                builder.SetKey(ev.Id);
                builder.AddAttribute(1, "class", "bit-bfc-tl-event-anchor");
                builder.AddAttribute(2, "style", style);
                builder.OpenComponent<BitFcTimelineEventBlock>(3);
                builder.AddAttribute(4, "Event", ev);
                builder.AddAttribute(5, "OnSelected", EventCallback.Factory.Create<BitFullCalendarEvent>(this, SelectEvent));
                builder.AddAttribute(6, "EventTemplate", EventTemplate);
                builder.AddAttribute(7, "PixelsPerMinute", hourWidth / 60.0);
                builder.AddAttribute(8, "SnapMinutes", State.SlotDurationMinutes);
                builder.CloseComponent();
                builder.CloseElement();
            }
        }
    };

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

    private async Task OnSlotClickAsync(string resourceId, DateTime day, int hour, int minute)
    {
        if (State.ReadOnly)
            return;

        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(day, hour, minute, State.SlotDurationMinutes);
            draft.Resource = resourceId == _unassignedKey ? null : resourceId;
            await OnAddClick.InvokeAsync(draft);
            return;
        }

        _addStartDate = day;
        _addStartHour = hour;
        _addStartMinute = minute;
        _addResourceId = resourceId == _unassignedKey ? null : resourceId;
        _showAddDialog = true;
    }

    private async Task OnSlotKeyDownAsync(KeyboardEventArgs e, string resourceId, DateTime day, int hour, int minute)
    {
        // The slot the key came from is the one the roving tabindex should sit on. The time axis runs
        // along the reading direction here, so left/right move in time and up/down between resources.
        switch (e.Key)
        {
            case "Enter" or " " or "Spacebar":
                // Ignore auto-repeat so a held key only creates a single draft event.
                if (e.Repeat is false)
                    await OnSlotClickAsync(resourceId, day, hour, minute);
                return;

            case "ArrowRight":
                SetRovingSlot(resourceId, day, hour, minute);
                MoveRovingSlot(0, State.IsRtl ? -1 : 1);
                return;

            case "ArrowLeft":
                SetRovingSlot(resourceId, day, hour, minute);
                MoveRovingSlot(0, State.IsRtl ? 1 : -1);
                return;

            case "ArrowDown":
                SetRovingSlot(resourceId, day, hour, minute);
                MoveRovingSlot(1, 0);
                return;

            case "ArrowUp":
                SetRovingSlot(resourceId, day, hour, minute);
                MoveRovingSlot(-1, 0);
                return;

            case "PageDown":
                SetRovingSlot(resourceId, day, hour, minute);
                MoveRovingSlot(0, State.VisibleHourCount * State.SlotMinutes.Length);
                return;

            case "PageUp":
                SetRovingSlot(resourceId, day, hour, minute);
                MoveRovingSlot(0, -State.VisibleHourCount * State.SlotMinutes.Length);
                return;

            case "Home":
                SetRovingSlot(resourceId, day, State.VisibleStartHour, State.SlotMinutes[0]);
                return;

            case "End":
                SetRovingSlot(resourceId, day, State.VisibleEndHour - 1, State.SlotMinutes[^1]);
                return;
        }
    }

    private string? SlotAriaLabel(string rowLabel, DateTime day, int hour, int minute)
    {
        // The slot is inert in read-only mode, so it carries no label to announce.
        if (State.ReadOnly)
            return null;

        var start = day.Date.AddHours(hour).AddMinutes(minute);
        return $"{Texts.AddEventHoverHint}, {rowLabel}, {day.ToString("ddd", State.Culture)} {BitFullCalendarHelpers.FormatTime(start, State.Use24HourFormat, State.Culture)}";
    }

    private void OnDragEnter(string resourceId, DateTime day, int hour, int minute)
    {
        if (!State.IsDragging) return;
        _dragResourceId = resourceId;
        _dragDay = day.Date;
        _dragHour = hour;
        _dragMinute = minute;
    }

    private async Task OnDrop(string resourceId, DateTime day, int hour, int minute)
    {
        if (!State.IsDragging) return;

        _dragResourceId = null;
        _dragDay = null;
        _dragHour = null;
        _dragMinute = null;
        var newResourceId = resourceId == _unassignedKey ? null : resourceId;
        await Notifier.HandleResourceDropAsync(day, hour, minute, newResourceId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The arrow keys only moved the tabbable slot; the focus has to follow it here, once the
        // new tabindex has actually been rendered.
        if (_pendingSlotFocus)
        {
            _pendingSlotFocus = false;
            var (rowKey, day, hour, minute) = RovingSlot;
            await BitFcFocusInterop.TryFocusAsync(JS, SlotElementId(rowKey, day, hour, minute));
        }

        var weekStart = BitFullCalendarHelpers.StartOfWeek(State.SelectedDate, State.Culture, State.FirstDayOfWeekOverride);
        var sig = $"{weekStart:yyyy-MM-dd}|{State.StartOfDayHour}|{State.VisibleStartHour}|{State.VisibleEndHour}|{DateTime.Today:yyyy-MM-dd}";
        if (sig == _scrollSignature) return;

        if (await BitFcTimelineScrollInterop.TryScrollToTargetAsync(JS, _scrollContainerId))
            _scrollSignature = sig;
    }
}
