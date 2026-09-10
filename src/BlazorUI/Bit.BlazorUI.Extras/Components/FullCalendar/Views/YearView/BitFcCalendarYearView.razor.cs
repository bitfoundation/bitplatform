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
        // A month the date bounds only clip - Min/MaxDate landing mid-month - is still navigable: the
        // target moves to the nearest day inside the window rather than leaving the month title inert.
        // A month they put entirely out of reach clamps into a neighbouring one, and that is where the
        // navigation stops.
        var target = State.ClampToAllowedRange(month);
        var calendar = State.Culture.Calendar;
        if (calendar.GetYear(target) != calendar.GetYear(month) || calendar.GetMonth(target) != calendar.GetMonth(month))
            return;

        State.SetSelectedDate(target);

        // Drilling into a month is an indirect route to the month view; when the consumer excluded
        // it, navigating the date is all this does rather than landing on some other view.
        if (State.IsViewAvailable(BitFullCalendarView.Month))
            State.SetView(BitFullCalendarView.Month);
    }

    private void ShowEventsForDay(DateTime date, List<BitFullCalendarEvent> events)
    {
        _eventListDate = date;
        _eventListEvents = events;
        _showEventList = true;
    }
}
