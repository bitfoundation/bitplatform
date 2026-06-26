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

    private BitFullCalendarEvent? _selectedEvent;
    private DateTime? _dragDate;
    private int? _dragHour;
    private int? _dragMinute;

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
        State.SetSelectedDate(day);

        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(day, hour, minute);
            await OnAddClick.InvokeAsync(draft);
            return;
        }

        _addDate = day;
        _addHour = hour;
        _addMinute = minute;
        _showAddDialog = true;
    }

    private async Task OnHourKeyDownAsync(KeyboardEventArgs e, DateTime day, int hour, int minute = 0)
    {
        // Ignore auto-repeat keydown events so a held Enter/Space only creates a single draft event.
        if (e.Key is "Enter" or " " or "Spacebar" && !e.Repeat)
            await OnHourClickAsync(day, hour, minute);
    }

    private string HourSlotAriaLabel(DateTime day, int hour, int minute = 0)
    {
        var start = day.Date.AddHours(hour).AddMinutes(minute);
        return $"{Texts.AddEventHoverHint}, {day.ToString("ddd", State.Culture)} {BitFullCalendarHelpers.FormatTime(start, State.Use24HourFormat, State.Culture)}";
    }

    private async Task OnDrop(DateTime day, int hour, int minute)
    {
        _dragDate = null;
        _dragHour = null;
        _dragMinute = null;
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

        return _dragDate == day.Date && _dragHour == hour && _dragMinute == minute
            ? (minute == 30 ? "bit-bfc-drop-preview-half" : "bit-bfc-drop-preview-hour")
            : string.Empty;
    }

    private string BuildTimeGridScrollSignature() =>
        $"{State.SelectedDate:yyyy-MM-dd}|{State.StartOfDayHour}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var sig = BuildTimeGridScrollSignature();
        if (sig == _timeGridScrollSignature)
            return;

        if (await BitFcTimeGridScrollInterop.TryScrollToStartOfDayAsync(
                JS,
                _timeGridScrollElementId,
                State.StartOfDayHour))
            _timeGridScrollSignature = sig;
    }
}
