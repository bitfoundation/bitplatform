namespace Bit.BlazorUI;

public partial class BitFcMiniCalendar
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;

    private DateTime _displayMonth;

    protected override void OnInitialized()
    {
        _displayMonth = State.Culture.Calendar.ToDateTime(
            State.Culture.Calendar.GetYear(State.SelectedDate),
            State.Culture.Calendar.GetMonth(State.SelectedDate),
            1, 0, 0, 0, 0);
    }

    private void PrevMonth() => _displayMonth = State.Culture.Calendar.AddMonths(_displayMonth, -1);
    private void NextMonth() => _displayMonth = State.Culture.Calendar.AddMonths(_displayMonth, 1);

    private void SelectDate(DateTime date)
    {
        State.SetSelectedDate(date);
        var cal = State.Culture.Calendar;
        _displayMonth = cal.ToDateTime(cal.GetYear(date), cal.GetMonth(date), 1, 0, 0, 0, 0);
    }
}
