namespace Bit.BlazorUI;

public partial class BitFcTimelineMonthView
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [CascadingParameter(Name = "OnAddClick")] public EventCallback<BitFullCalendarEvent?> OnAddClick { get; set; }
    [CascadingParameter(Name = "OnEventClick")] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }

    [Parameter] public List<BitFullCalendarEvent> Events { get; set; } = [];
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    private const string _unassignedKey = "__bfc_unassigned__";
    private const int _laneHeight = 40;
    private const int _laneGap = 4;
    private const int _rowPadding = 4;
    private readonly string _scrollContainerId = "bit-bfc-tl-month-scroll-" + Guid.NewGuid().ToString("N");

    private string? _scrollSignature;

    private BitFullCalendarEvent? _selectedEvent;
    private bool _showAddDialog;
    private DateTime _addStartDate;
    private int _addStartHour;
    private string? _addResourceId;

    private string? _dragResourceId;
    private DateTime? _dragDay;

    // The day cells exist only as add/drop targets, so a read-only timeline must not expose a
    // focusable no-op button per day and resource. A null attribute value is omitted from the markup.
    private string? _slotRole => State.ReadOnly ? null : "button";

    // Roving tabindex: the whole grid is a single tab stop and the arrow keys move both the tabbable
    // cell and the focus - a resource row per day would otherwise flood the tab order.
    private List<string> _rowKeys = [];
    private DateTime[] _monthDays = [];
    private (string RowKey, DateTime Day)? _focusedCell;
    private bool _pendingCellFocus;

    private string SlotElementId(string rowKey, DateTime day)
        => $"{_scrollContainerId}-cell-{Math.Max(0, _rowKeys.IndexOf(rowKey))}-{day:yyyyMMdd}";

    private (string RowKey, DateTime Day) RovingCell
    {
        get
        {
            if (_focusedCell is { } cell
                && _rowKeys.Contains(cell.RowKey)
                && _monthDays.Any(d => d.Date == cell.Day.Date))
                return cell;

            return (
                _rowKeys.Count > 0 ? _rowKeys[0] : _unassignedKey,
                _monthDays.Length > 0 ? _monthDays[0] : State.SelectedDate.Date);
        }
    }

    private string? SlotTabIndex(string rowKey, DateTime day)
    {
        if (State.ReadOnly)
            return null;

        var roving = RovingCell;
        return roving.RowKey == rowKey && roving.Day.Date == day.Date ? "0" : "-1";
    }

    /// <summary>
    /// Moves the roving cell by <paramref name="rowDelta"/> resource rows and
    /// <paramref name="dayDelta"/> days, clamped to the rendered month.
    /// </summary>
    private void MoveRovingCell(int rowDelta, int dayDelta)
    {
        var (rowKey, day) = RovingCell;

        var dayIndex = Math.Max(0, Array.FindIndex(_monthDays, d => d.Date == day.Date));
        dayIndex = Math.Clamp(dayIndex + dayDelta, 0, Math.Max(0, _monthDays.Length - 1));

        var rowIndex = Math.Max(0, _rowKeys.IndexOf(rowKey));
        rowIndex = Math.Clamp(rowIndex + rowDelta, 0, Math.Max(0, _rowKeys.Count - 1));

        _focusedCell = (
            _rowKeys.Count > 0 ? _rowKeys[rowIndex] : rowKey,
            _monthDays.Length > 0 ? _monthDays[dayIndex] : day);
        _pendingCellFocus = true;
    }

    private void SetRovingCell(string rowKey, DateTime day)
    {
        _focusedCell = (rowKey, day);
        _pendingCellFocus = true;
    }

    private RenderFragment RenderLanes(List<List<BitFullCalendarEvent>> lanes, DateTime monthStart, int daysInMonth) => builder =>
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        const int dayWidth = BitFullCalendarHelpers.TimelineDayWidthPx;
        var monthEnd = monthStart.AddDays(daysInMonth);

        for (var li = 0; li < lanes.Count; li++)
        {
            var laneTop = _rowPadding + (li * (_laneHeight + _laneGap));
            foreach (var ev in lanes[li])
            {
                var clippedStart = ev.StartDate < monthStart ? monthStart : ev.StartDate;
                var clippedEnd = ev.EndDate > monthEnd ? monthEnd : ev.EndDate;
                // Allow zero-length markers (clippedEnd == clippedStart) through so a 00:00 all-day
                // marker is still rendered (as a single-day block below) instead of being hidden.
                if (clippedEnd < clippedStart) continue;

                // Use full-day boundaries for span (start of start day → start of next-after-end day).
                var startDayIdx = (int)(clippedStart.Date - monthStart.Date).TotalDays;
                var endExclusiveIdx = (int)(clippedEnd.Date - monthStart.Date).TotalDays + (clippedEnd.TimeOfDay > TimeSpan.Zero ? 1 : 0);
                if (endExclusiveIdx <= startDayIdx) endExclusiveIdx = startDayIdx + 1;

                var leftPx = startDayIdx * dayWidth;
                var widthPx = (endExclusiveIdx - startDayIdx) * dayWidth - 2; // -2 for visual gap

                // Anchor with inset-inline-start (not left) so blocks and the row's day cells share
                // the same axis in a right-to-left layout.
                var style = $"inset-inline-start:{leftPx.ToString("F2", inv)}px;width:{Math.Max(widthPx, 12).ToString("F2", inv)}px;top:{laneTop}px;height:{_laneHeight}px;";
                // Key the per-event block by the event's stable identity so Blazor preserves the
                // correct BitFcTimelineEventBlock instance (and its in-flight state) when lane ordering
                // is recomputed, instead of reusing a sibling's component by position - matching the
                // week timeline view.
                builder.OpenElement(0, "div");
                builder.SetKey(ev.Id);
                builder.AddAttribute(1, "class", "bit-bfc-tl-event-anchor");
                builder.AddAttribute(2, "style", style);
                builder.OpenComponent<BitFcTimelineEventBlock>(3);
                builder.AddAttribute(4, "Event", ev);
                builder.AddAttribute(5, "OnSelected", EventCallback.Factory.Create<BitFullCalendarEvent>(this, SelectEvent));
                builder.AddAttribute(6, "EventTemplate", EventTemplate);
                builder.AddAttribute(7, "PixelsPerMinute", dayWidth / 1440.0);
                builder.AddAttribute(8, "SnapMinutes", 1440);
                builder.AddAttribute(9, "MinDurationMinutes", 1440);
                builder.AddAttribute(10, "PreviewAsDate", true);
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

    private async Task OnSlotClickAsync(string resourceId, DateTime day)
    {
        if (State.ReadOnly)
            return;

        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(
                day, State.StartOfDayHour, 0, State.SlotDurationMinutes);
            draft.Resource = resourceId == _unassignedKey ? null : resourceId;
            await OnAddClick.InvokeAsync(draft);
            return;
        }

        _addStartDate = day;
        _addStartHour = State.StartOfDayHour;
        _addResourceId = resourceId == _unassignedKey ? null : resourceId;
        _showAddDialog = true;
    }

    private async Task OnSlotKeyDownAsync(KeyboardEventArgs e, string resourceId, DateTime day)
    {
        // The cell the key came from is the one the roving tabindex should sit on. The date axis runs
        // along the reading direction, so left/right move by a day and up/down between resources.
        switch (e.Key)
        {
            case "Enter" or " " or "Spacebar":
                // Ignore auto-repeat so a held key only creates a single draft event.
                if (e.Repeat is false)
                    await OnSlotClickAsync(resourceId, day);
                return;

            case "ArrowRight":
                SetRovingCell(resourceId, day);
                MoveRovingCell(0, State.IsRtl ? -1 : 1);
                return;

            case "ArrowLeft":
                SetRovingCell(resourceId, day);
                MoveRovingCell(0, State.IsRtl ? 1 : -1);
                return;

            case "ArrowDown":
                SetRovingCell(resourceId, day);
                MoveRovingCell(1, 0);
                return;

            case "ArrowUp":
                SetRovingCell(resourceId, day);
                MoveRovingCell(-1, 0);
                return;

            case "Home":
                SetRovingCell(resourceId, _monthDays.Length > 0 ? _monthDays[0] : day);
                return;

            case "End":
                SetRovingCell(resourceId, _monthDays.Length > 0 ? _monthDays[^1] : day);
                return;
        }
    }

    private string? SlotAriaLabel(DateTime day, string rowLabel)
    {
        // The cell is inert in read-only mode, so it carries no label to announce.
        if (State.ReadOnly)
            return null;

        return $"{Texts.AddEventHoverHint}, {rowLabel}, {day.ToString("D", State.Culture)}";
    }

    private void OnDragEnter(string resourceId, DateTime day)
    {
        if (!State.IsDragging) return;
        _dragResourceId = resourceId;
        _dragDay = day.Date;
    }

    private async Task OnDrop(string resourceId, DateTime day)
    {
        if (!State.IsDragging) return;

        _dragResourceId = null;
        _dragDay = null;
        var newResourceId = resourceId == _unassignedKey ? null : resourceId;
        // Day-precision drop: keep the original time of day (passing null hour/minute makes
        // HandleResourceDropAsync preserve the dragged event's time).
        await Notifier.HandleResourceDropAsync(day, hour: null, minute: null, newResourceId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The arrow keys only moved the tabbable cell; the focus has to follow it here, once the
        // new tabindex has actually been rendered.
        if (_pendingCellFocus)
        {
            _pendingCellFocus = false;
            var (rowKey, day) = RovingCell;
            await BitFcFocusInterop.TryFocusAsync(JS, SlotElementId(rowKey, day));
        }

        // Only the current month has a "today" scroll target; skip the interop entirely otherwise
        // so we don't make a JS round-trip on every render of a non-current month.
        // Compare using the active culture's calendar since the rendered month follows that
        // calendar system, not the Gregorian one.
        var cal = State.Culture.Calendar;
        var today = DateTime.Today;
        if (cal.GetYear(State.SelectedDate) != cal.GetYear(today) ||
            cal.GetMonth(State.SelectedDate) != cal.GetMonth(today))
        {
            // Reset the signature so that navigating back to the current month later re-triggers
            // the scroll-to-today interop instead of being skipped by a stale matching signature.
            _scrollSignature = "";
            return;
        }

        var sig = $"{cal.GetYear(State.SelectedDate)}-{cal.GetMonth(State.SelectedDate)}|{cal.GetYear(today)}-{cal.GetMonth(today)}-{cal.GetDayOfMonth(today)}";
        if (sig == _scrollSignature) return;

        if (await BitFcTimelineScrollInterop.TryScrollToTargetAsync(JS, _scrollContainerId))
            _scrollSignature = sig;
    }
}
