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
    private const string _scrollContainerId = "bit-bfc-tl-week-scroll";

    private string? _scrollSignature;

    private BitFullCalendarEvent? _selectedEvent;
    private bool _showAddDialog;
    private DateTime _addStartDate;
    private int _addStartHour;

    private string? _dragResourceId;
    private DateTime? _dragDay;
    private int? _dragHour;
    private int? _dragMinute;

    private RenderFragment RenderLanes(List<List<BitFullCalendarEvent>> lanes, DateTime day, int dayOffsetPx) => builder =>
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        const int hourWidth = BitFullCalendarHelpers.TimelineHourWidthPx;

        for (var li = 0; li < lanes.Count; li++)
        {
            var laneTop = _rowPadding + (li * (_laneHeight + _laneGap));
            foreach (var ev in lanes[li])
            {
                var pos = BitFullCalendarHelpers.GetTimelineBlockPosition(ev, day, hourWidth);
                if (pos is not { } p)
                    continue;

                var style = $"left:{(dayOffsetPx + p.LeftPx).ToString("F2", inv)}px;width:{Math.Max(p.WidthPx, 12).ToString("F2", inv)}px;top:{laneTop}px;height:{_laneHeight}px;";
                builder.OpenElement(0, "div");
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

    private async Task OnSlotClickAsync(string resourceId, DateTime day, int hour, int minute)
    {
        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(day, hour, minute);
            draft.Resource = resourceId == _unassignedKey ? null : resourceId;
            await OnAddClick.InvokeAsync(draft);
            return;
        }

        _addStartDate = day;
        _addStartHour = hour;
        _showAddDialog = true;
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
        _dragResourceId = null;
        _dragDay = null;
        _dragHour = null;
        _dragMinute = null;
        var newResourceId = resourceId == _unassignedKey ? null : resourceId;
        await Notifier.HandleResourceDropAsync(day, hour, minute, newResourceId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var weekStart = BitFullCalendarHelpers.StartOfWeek(State.SelectedDate, State.Culture);
        var sig = $"{weekStart:yyyy-MM-dd}|{State.StartOfDayHour}|{DateTime.Today:yyyy-MM-dd}";
        if (sig == _scrollSignature) return;

        if (await BitFcTimelineScrollInterop.TryScrollToTargetAsync(JS, _scrollContainerId))
            _scrollSignature = sig;
    }
}
