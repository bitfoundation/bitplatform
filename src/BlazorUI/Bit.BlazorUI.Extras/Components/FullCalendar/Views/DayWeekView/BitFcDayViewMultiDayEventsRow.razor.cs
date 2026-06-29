namespace Bit.BlazorUI;

public partial class BitFcDayViewMultiDayEventsRow
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter(Name = "OnEventClick")] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }
    [Parameter] public List<BitFullCalendarEvent> MultiDayEvents { get; set; } = [];
    [Parameter] public DateTime Date { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    private List<BitFullCalendarEvent> _multiDayForDay = [];
    private BitFullCalendarEvent? _selectedEvent;

    protected override void OnParametersSet()
    {
        _multiDayForDay = BitFullCalendarHelpers.GetEventsForDay(MultiDayEvents, Date, true);
    }

    private string GetPosition(BitFullCalendarEvent ev)
    {
        if (ev.StartDate.Date == Date.Date) return "first";
        // Treat a 00:00 end as ending the previous day (exclusive midnight), consistent with
        // GetEventsForDay/GroupEventsByDayRange, so an event ending at midnight is still marked
        // "last" on its real final visible day rather than "middle".
        var endInclusive = BitFullCalendarHelpers.GetInclusiveEndDate(ev);
        if (endInclusive == Date.Date) return "last";
        return "middle";
    }

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
}
