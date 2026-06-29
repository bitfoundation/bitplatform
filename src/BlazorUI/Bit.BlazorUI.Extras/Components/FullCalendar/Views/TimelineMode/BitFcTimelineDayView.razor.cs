namespace Bit.BlazorUI;

public partial class BitFcTimelineDayView
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
    private readonly string _scrollContainerId = "bit-bfc-tl-day-scroll-" + Guid.NewGuid().ToString("N");

    private string? _scrollSignature;

    private BitFullCalendarEvent? _selectedEvent;
    private bool _showAddDialog;
    private DateTime _addStartDate;
    private int _addStartHour;
    private int _addStartMinute;
    private string? _addResourceId;

    private string? _dragResourceId;
    private int? _dragHour;
    private int? _dragMinute;

    private RenderFragment RenderLanes(List<List<BitFullCalendarEvent>> lanes) => builder =>
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        const int hourWidth = BitFullCalendarHelpers.TimelineHourWidthPx;
        for (var li = 0; li < lanes.Count; li++)
        {
            var laneTop = _rowPadding + (li * (_laneHeight + _laneGap));
            foreach (var ev in lanes[li])
            {
                var pos = BitFullCalendarHelpers.GetTimelineBlockPosition(ev, State.SelectedDate, hourWidth);
                if (pos is not { } p)
                    continue;

                var style = $"left:{p.LeftPx.ToString("F2", inv)}px;width:{Math.Max(p.WidthPx, 12).ToString("F2", inv)}px;top:{laneTop}px;height:{_laneHeight}px;";
                // Key the per-event block by the event's stable identity so Blazor preserves the
                // correct BitFcTimelineEventBlock instance (and its in-flight drag/resize state) when
                // lane ordering is recomputed, instead of reusing a sibling's component by position -
                // matching the week/month timeline views.
                builder.OpenElement(0, "div");
                builder.SetKey(ev.Id);
                builder.AddAttribute(1, "class", "bit-bfc-tl-event-anchor");
                builder.AddAttribute(2, "style", style);
                builder.OpenComponent<BitFcTimelineEventBlock>(3);
                builder.AddAttribute(4, "Event", ev);
                builder.AddAttribute(5, "OnSelected", EventCallback.Factory.Create<BitFullCalendarEvent>(this, SelectEvent));
                builder.AddAttribute(6, "EventTemplate", EventTemplate);
                builder.AddAttribute(7, "PixelsPerMinute", hourWidth / 60.0);
                builder.AddAttribute(8, "SnapMinutes", 30);
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

    private async Task OnSlotClickAsync(string resourceId, int hour, int minute)
    {
        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(State.SelectedDate, hour, minute);
            draft.Resource = resourceId == _unassignedKey ? null : resourceId;
            await OnAddClick.InvokeAsync(draft);
            return;
        }

        _addStartDate = State.SelectedDate;
        _addStartHour = hour;
        _addStartMinute = minute;
        _addResourceId = resourceId == _unassignedKey ? null : resourceId;
        _showAddDialog = true;
    }

    private async Task OnSlotKeyDownAsync(KeyboardEventArgs e, string resourceId, int hour, int minute)
    {
        // Ignore auto-repeat keydowns so a held Enter/Space only creates a single draft event,
        // matching the day/week view behavior.
        if (e.Key is "Enter" or " " or "Spacebar" && !e.Repeat)
            await OnSlotClickAsync(resourceId, hour, minute);
    }

    private string SlotAriaLabel(string rowLabel, int hour, int minute)
    {
        var start = State.SelectedDate.Date.AddHours(hour).AddMinutes(minute);
        return $"{Texts.AddEventHoverHint}, {rowLabel}, {BitFullCalendarHelpers.FormatTime(start, State.Use24HourFormat, State.Culture)}";
    }

    private void OnDragEnter(string resourceId, int hour, int minute)
    {
        if (!State.IsDragging) return;
        _dragResourceId = resourceId;
        _dragHour = hour;
        _dragMinute = minute;
    }

    private async Task OnDrop(string resourceId, int hour, int minute)
    {
        if (!State.IsDragging) return;

        _dragResourceId = null;
        _dragHour = null;
        _dragMinute = null;
        var newResourceId = resourceId == _unassignedKey ? null : resourceId;
        await Notifier.HandleResourceDropAsync(State.SelectedDate, hour, minute, newResourceId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var sig = $"{State.SelectedDate:yyyy-MM-dd}|{State.StartOfDayHour}";
        if (sig == _scrollSignature) return;

        if (await BitFcTimelineScrollInterop.TryScrollToTargetAsync(JS, _scrollContainerId))
            _scrollSignature = sig;
    }
}
