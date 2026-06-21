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

    private bool _showAddDialog;
    private DateTime _addDate;
    private int _addHour;

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

    private async Task OnHourClickAsync(DateTime day, int hour)
    {
        State.SetSelectedDate(day);

        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(day, hour);
            await OnAddClick.InvokeAsync(draft);
            return;
        }

        _addDate = day;
        _addHour = hour;
        _showAddDialog = true;
    }

    private async Task OnHourKeyDownAsync(KeyboardEventArgs e, DateTime day, int hour)
    {
        if (e.Key is "Enter" or " " or "Spacebar")
            await OnHourClickAsync(day, hour);
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
                "bit-bfc-week-timegrid-scroll",
                State.StartOfDayHour))
            _timeGridScrollSignature = sig;
    }
}
