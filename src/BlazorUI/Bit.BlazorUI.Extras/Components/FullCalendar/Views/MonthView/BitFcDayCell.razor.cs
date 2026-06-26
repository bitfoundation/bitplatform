namespace Bit.BlazorUI;

public partial class BitFcDayCell
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [CascadingParameter(Name = "OnAddClick")] public EventCallback<BitFullCalendarEvent?> OnAddClick { get; set; }
    [CascadingParameter(Name = "OnEventClick")] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }
    [Parameter] public BitFullCalendarCell Cell { get; set; } = default!;
    [Parameter] public List<BitFullCalendarEvent> Events { get; set; } = [];
    [Parameter] public Dictionary<string, int> EventPositions { get; set; } = new();
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    private bool _showEventList;
    private bool _showAddDialog;
    private DateTime _addDraftStart;
    private BitFullCalendarEvent? _selectedEvent;

    private async Task ShowEventDetails(BitFullCalendarEvent ev)
    {
        if (OnEventClick.HasDelegate)
        {
            await OnEventClick.InvokeAsync(ev);
            return;
        }
        _selectedEvent = ev;
    }
    private void CloseEventDetails() => _selectedEvent = null;

    private async Task OnCellClick()    {
        State.SetSelectedDate(Cell.Date);

        // Build the draft once and use it for both the external add handler and the built-in dialog
        // fallback so they always agree on the start date/time. Seed from the calendar's start-of-day
        // hour (matching the other month-view add entry points) instead of DateTime.Now.Hour.
        var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(Cell.Date, State.StartOfDayHour);

        if (OnAddClick.HasDelegate)
        {
            await OnAddClick.InvokeAsync(draft);
        }
        else
        {
            _addDraftStart = draft.StartDate;
            _showAddDialog = true;
        }
    }

    private string GetBadgePosition(BitFullCalendarEvent ev, DateTime cellDate)
    {
        if (ev.IsSingleDay) return "none";
        if (ev.StartDate.Date == cellDate.Date) return "first";
        // Treat a 00:00 end as ending the previous day (exclusive midnight), consistent with
        // GetMonthCellEvents, so the badge on the true last day is marked "last" rather than "middle".
        var lastDate = BitFullCalendarHelpers.GetInclusiveEndDate(ev);
        if (lastDate == cellDate.Date) return "last";
        return "middle";
    }

    private void OnDragOver() { }

    private async Task OnDrop()
    {
        await Notifier.HandleDropAsync(Cell.Date);
    }
}
