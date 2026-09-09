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

    /// <summary>
    /// True while this cell owns the month grid's single tab stop. The month view moves it with the
    /// arrow keys so the grid never puts one stop in the tab order per day.
    /// </summary>
    [Parameter] public bool IsRovingCell { get; set; }

    /// <summary>DOM id of the cell's add button, so the month view can move focus onto it.</summary>
    [Parameter] public string? AddButtonId { get; set; }

    /// <summary>
    /// True for the cell in the grid's trailing column, which drops its column separator. The view
    /// marks it because the grid is not always seven wide: a work week narrows it and the
    /// week-number rail adds a leading child to every row.
    /// </summary>
    [Parameter] public bool IsLastColumn { get; set; }

    /// <summary>Raised for every key pressed on the add button, together with this cell's date.</summary>
    [Parameter] public EventCallback<(DateTime Date, KeyboardEventArgs Args)> OnCellKeyDown { get; set; }

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

    /// <summary>
    /// Opens this single day from its number. The date moves in every case; the view only follows
    /// when the consumer left the day view in the allowed set.
    /// </summary>
    private void GoToDayView()
    {
        if (State.IsDateInAllowedRange(Cell.Date) is false)
            return;

        State.SetSelectedDate(Cell.Date);
        if (State.IsViewAvailable(BitFullCalendarView.Day))
            State.SetView(BitFullCalendarView.Day);
    }

    private async Task OnCellClick()
    {
        // A date outside the allowed window is not navigable, so the whole click is inert there.
        if (State.IsDateInAllowedRange(Cell.Date) is false)
            return;

        State.SetSelectedDate(Cell.Date);

        // Selecting the date above is navigation and stays available in read-only mode; only the
        // add affordance behind the same click is suppressed.
        if (State.ReadOnly)
            return;

        // Build the draft once and use it for both the external add handler and the built-in dialog
        // fallback so they always agree on the start date/time. Seed from the calendar's start-of-day
        // hour (matching the other month-view add entry points) instead of DateTime.Now.Hour.
        var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(
            Cell.Date, State.StartOfDayHour, 0, State.SlotDurationMinutes);

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
        // Dropping onto a date the calendar cannot navigate to would move the event out of sight,
        // so the gesture is dropped (and reported) instead of committed.
        if (State.IsDateInAllowedRange(Cell.Date) is false)
        {
            State.EndDrag();
            Notifier.ReportRefusal(BitFullCalendarChangeRefusal.OutOfRange);
            return;
        }

        await Notifier.HandleDropAsync(Cell.Date);
    }
}
