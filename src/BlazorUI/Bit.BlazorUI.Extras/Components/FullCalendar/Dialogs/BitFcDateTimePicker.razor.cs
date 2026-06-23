using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitFcDateTimePicker : IDisposable
{
    [Parameter] public DateTime Value { get; set; }
    [Parameter] public EventCallback<DateTime> ValueChanged { get; set; }
    [Parameter] public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
    [Parameter] public string PreviousMonthAriaLabel { get; set; } = "Previous month";
    [Parameter] public string NextMonthAriaLabel { get; set; } = "Next month";

    private DateTime _visibleMonthAnchor;
    private int _hour;
    private int _minute;
    private bool _isOpen;
    private DateTime _lastSyncedDate = DateTime.MinValue;
    private CultureInfo? _lastSyncedCulture;
    private string[] _weekdayHeaders = [];
    private CancellationTokenSource? _closeCts;

    protected override void OnParametersSet()
    {
        // Re-anchor the visible month when the value changes, and also when the culture/calendar
        // system changes - the same DateTime maps to a different month label across calendars.
        // Compare by culture name (not reference) so a change carried on a different CultureInfo
        // instance is still detected, and also by calendar system so reusing the same culture name
        // with a different calendar is detected too.
        var calendarChanged = _lastSyncedCulture is null
            || _lastSyncedCulture.DateTimeFormat.Calendar.GetType() != Culture.DateTimeFormat.Calendar.GetType();
        var cultureChanged = calendarChanged
            || !string.Equals(_lastSyncedCulture?.Name, Culture.Name, StringComparison.Ordinal);

        if (_lastSyncedDate != Value || cultureChanged)
        {
            _hour = Value.Hour;
            _minute = Value.Minute;
            _visibleMonthAnchor = GetFirstDayOfMonth(Value);
            _lastSyncedDate = Value;
        }

        _lastSyncedCulture = Culture;
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

    private void OnFocusOut(FocusEventArgs args)
    {
        // Blazor's FocusEventArgs doesn't expose relatedTarget, so we can't tell from the event
        // alone whether focus moved to a child (day button, time select) or left the picker.
        // Defer the close briefly: if focus lands back inside the picker, OnFocusIn cancels it.
        _closeCts?.Cancel();
        _closeCts?.Dispose();
        _closeCts = new CancellationTokenSource();
        var token = _closeCts.Token;
        _ = CloseAfterDelay(token);
    }

    private void OnFocusIn(FocusEventArgs _)
    {
        // Focus returned to (or moved within) the picker - keep it open.
        _closeCts?.Cancel();
    }

    private async Task CloseAfterDelay(CancellationToken token)
    {
        try
        {
            await Task.Delay(120, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (token.IsCancellationRequested || !_isOpen)
            return;

        _isOpen = false;
        await InvokeAsync(StateHasChanged);
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

    public void Dispose()
    {
        _closeCts?.Cancel();
        _closeCts?.Dispose();
    }
}
