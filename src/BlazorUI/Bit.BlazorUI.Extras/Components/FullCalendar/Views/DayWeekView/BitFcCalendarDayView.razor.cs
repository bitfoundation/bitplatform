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

    private BitFullCalendarEvent? _selectedEvent;
    private int? _dragHour;
    private int? _dragMinute;

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
        if (OnAddClick.HasDelegate)
        {
            var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(State.SelectedDate, hour, minute);
            await OnAddClick.InvokeAsync(draft);
            return;
        }

        _addStartDate = State.SelectedDate;
        _addStartHour = hour;
        _addStartMinute = minute;
        _showAddDialog = true;
    }

    private async Task OnHourKeyDownAsync(KeyboardEventArgs e, int hour, int minute = 0)
    {
        // Ignore auto-repeat keydown events so a held Enter/Space only creates a single draft event.
        if (e.Key is "Enter" or " " or "Spacebar" && !e.Repeat)
            await OnHourClickAsync(hour, minute);
    }

    private string HourSlotAriaLabel(int hour, int minute = 0)
    {
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
                _scrollContainerId,
                State.StartOfDayHour))
            _timeGridScrollSignature = sig;
    }

    public void Dispose()
    {
        _isDisposed = true;
        _nowTimer?.Dispose();
    }
}
