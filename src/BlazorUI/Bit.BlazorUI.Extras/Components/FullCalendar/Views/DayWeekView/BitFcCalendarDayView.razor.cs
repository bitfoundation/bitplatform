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
    private Timer? _nowTimer;

    private bool _showAddDialog;
    private DateTime _addStartDate;
    private int _addStartHour;

    private BitFullCalendarEvent? _selectedEvent;
    private int? _dragHour;
    private int? _dragMinute;

    protected override void OnInitialized()
    {
        // The "Happening now" panel is derived from DateTime.Now; refresh once a minute so it
        // doesn't go stale during long sessions.
        _nowTimer = new Timer(_ => InvokeAsync(StateHasChanged), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
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

    private async Task OnHourClickAsync(int hour)
    {
        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(State.SelectedDate, hour);
            await OnAddClick.InvokeAsync(draft);
            return;
        }

        _addStartDate = State.SelectedDate;
        _addStartHour = hour;
        _showAddDialog = true;
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

        return _dragHour == hour && _dragMinute == minute
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
                "bit-bfc-day-timegrid-scroll",
                State.StartOfDayHour))
            _timeGridScrollSignature = sig;
    }

    public void Dispose() => _nowTimer?.Dispose();
}
