namespace Bit.BlazorUI;

public partial class BitFcMiniCalendar
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;

    private DateTime _displayMonth;
    private DateTime _lastSyncedSelectedDate;
    private string? _lastSyncedCultureName;

    protected override void OnInitialized()
    {
        _lastSyncedSelectedDate = State.SelectedDate;
        _lastSyncedCultureName = State.Culture.Name;
        _displayMonth = StartOfDisplayMonth(State.SelectedDate);
    }

    protected override void OnParametersSet()
    {
        // Keep _displayMonth aligned with external SelectedDate changes without clobbering the
        // user's in-component month browsing (PrevMonth/NextMonth leave SelectedDate untouched).
        // A culture/calendar switch also requires re-normalizing the display month so the header
        // and grid reflect the new calendar system.
        if (_lastSyncedSelectedDate != State.SelectedDate
            || !string.Equals(_lastSyncedCultureName, State.Culture.Name, StringComparison.Ordinal))
        {
            _lastSyncedSelectedDate = State.SelectedDate;
            _lastSyncedCultureName = State.Culture.Name;
            _displayMonth = StartOfDisplayMonth(State.SelectedDate);
        }
    }

    private DateTime StartOfDisplayMonth(DateTime date)
    {
        var cal = State.Culture.Calendar;
        return cal.ToDateTime(cal.GetYear(date), cal.GetMonth(date), 1, 0, 0, 0, 0);
    }

    private void PrevMonth() => _displayMonth = State.Culture.Calendar.AddMonths(_displayMonth, -1);
    private void NextMonth() => _displayMonth = State.Culture.Calendar.AddMonths(_displayMonth, 1);

    private void SelectDate(DateTime date)
    {
        State.SetSelectedDate(date);
        _lastSyncedSelectedDate = State.SelectedDate;
        _displayMonth = StartOfDisplayMonth(date);
    }
}
