namespace Bit.BlazorUI;

public partial class BitFcCalendarYearView
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [Parameter] public List<BitFullCalendarEvent> SingleDayEvents { get; set; } = [];
    [Parameter] public List<BitFullCalendarEvent> MultiDayEvents { get; set; } = [];

    private bool _showEventList;
    private DateTime _eventListDate;
    private List<BitFullCalendarEvent> _eventListEvents = [];

    private void GoToMonth(DateTime month)
    {
        State.SetSelectedDate(month);
        State.SetView(BitFullCalendarView.Month);
    }

    private void ShowEventsForDay(DateTime date, List<BitFullCalendarEvent> events)
    {
        _eventListDate = date;
        _eventListEvents = events;
        _showEventList = true;
    }
}
