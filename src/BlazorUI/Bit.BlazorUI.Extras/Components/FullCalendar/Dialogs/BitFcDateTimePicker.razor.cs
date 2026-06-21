using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitFcDateTimePicker
{
    [Parameter] public DateTime Value { get; set; }
    [Parameter] public EventCallback<DateTime> ValueChanged { get; set; }
    [Parameter] public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    private DateTime _visibleMonthAnchor;
    private int _hour;
    private int _minute;
    private bool _isOpen;
    private DateTime _lastSyncedDate = DateTime.MinValue;
    private string[] _weekdayHeaders = [];

    protected override void OnParametersSet()
    {
        if (_lastSyncedDate != Value)
        {
            _hour = Value.Hour;
            _minute = Value.Minute;
            _visibleMonthAnchor = GetFirstDayOfMonth(Value);
            _lastSyncedDate = Value;
        }

        _weekdayHeaders = BuildWeekdayHeaders();
    }

    private Calendar ActiveCalendar => Culture.DateTimeFormat.Calendar;

    private string[] BuildWeekdayHeaders()
    {
        var source = Culture.DateTimeFormat.AbbreviatedDayNames;
        var firstDay = (int)Culture.DateTimeFormat.FirstDayOfWeek;
        return Enumerable.Range(0, 7)
            .Select(i => source[(i + firstDay) % 7])
            .ToArray();
    }

    private void ToggleOpen() => _isOpen = !_isOpen;

    private void ShowPreviousMonth()
    {
        _visibleMonthAnchor = GetFirstDayOfMonth(ActiveCalendar.AddMonths(_visibleMonthAnchor, -1));
    }

    private void ShowNextMonth()
    {
        _visibleMonthAnchor = GetFirstDayOfMonth(ActiveCalendar.AddMonths(_visibleMonthAnchor, 1));
    }

    private async Task SelectDate(DateTime date)
    {
        var selected = new DateTime(date.Year, date.Month, date.Day, _hour, _minute, 0, Value.Kind);
        Value = selected;
        _lastSyncedDate = selected;
        _isOpen = false;
        await ValueChanged.InvokeAsync(selected);
    }

    private async Task OnTimeChanged()
    {
        var selected = new DateTime(Value.Year, Value.Month, Value.Day, _hour, _minute, 0, Value.Kind);
        Value = selected;
        _lastSyncedDate = selected;
        await ValueChanged.InvokeAsync(selected);
    }

    private void OnFocusOut(FocusEventArgs _)
    {
        _isOpen = false;
    }

    private IEnumerable<CalendarDay> BuildCalendarDays()
    {
        var firstDayOfMonth = GetFirstDayOfMonth(_visibleMonthAnchor);
        var firstDayOfWeek = Culture.DateTimeFormat.FirstDayOfWeek;
        var shift = ((int)firstDayOfMonth.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
        var gridStart = firstDayOfMonth.AddDays(-shift);

        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            yield return new CalendarDay(
                Date: date,
                Label: ActiveCalendar.GetDayOfMonth(date).ToString(Culture),
                IsCurrentMonth: IsSameCalendarMonth(date, _visibleMonthAnchor),
                IsSelected: date.Date == Value.Date);
        }
    }

    private string GetMonthYearLabel(DateTime date)
    {
        var month = ActiveCalendar.GetMonth(date);
        var year = ActiveCalendar.GetYear(date);
        var monthName = Culture.DateTimeFormat.GetMonthName(month);
        return $"{monthName} {year.ToString(Culture)}";
    }

    private DateTime GetFirstDayOfMonth(DateTime date)
    {
        var year = ActiveCalendar.GetYear(date);
        var month = ActiveCalendar.GetMonth(date);
        return ActiveCalendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
    }

    private bool IsSameCalendarMonth(DateTime left, DateTime right) =>
        ActiveCalendar.GetYear(left) == ActiveCalendar.GetYear(right)
        && ActiveCalendar.GetMonth(left) == ActiveCalendar.GetMonth(right);

    private string GetDayCellClass(CalendarDay day)
    {
        var classes = "bit-bfc-dtp-day";
        if (!day.IsCurrentMonth)
            classes += " bit-bfc-dtp-day-muted";
        if (day.IsSelected)
            classes += " bit-bfc-dtp-day-selected";
        return classes;
    }

    private string GetDisplayText()
    {
        var datePart = Value.ToString("d", Culture);
        var timePart = Value.ToString("HH:mm", CultureInfo.InvariantCulture);
        return $"{datePart} {timePart}";
    }

    private sealed record CalendarDay(DateTime Date, string Label, bool IsCurrentMonth, bool IsSelected);
}
