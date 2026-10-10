using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitFcDateTimePicker : IDisposable
{
    [Parameter] public DateTime Value { get; set; }
    [Parameter] public EventCallback<DateTime> ValueChanged { get; set; }
    [Parameter] public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
    /// <summary>When true the time selects/display use 24-hour values; otherwise a 12-hour hour list plus an AM/PM select.</summary>
    [Parameter] public bool Use24HourFormat { get; set; } = true;

    /// <summary>
    /// Day the picker's week starts on. <c>null</c> follows the culture. Supplied by the calendar so
    /// the picker's grid starts on the same day its own grids do rather than drifting from them.
    /// </summary>
    [Parameter] public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// When false the picker chooses a date only: the time selects are left out and the trigger shows
    /// the date alone. A picked date keeps the time of day <see cref="Value"/> already had.
    /// </summary>
    [Parameter] public bool ShowTime { get; set; } = true;

    /// <summary>Earliest selectable date; earlier days render disabled. <c>null</c> leaves it open.</summary>
    [Parameter] public DateTime? MinDate { get; set; }

    /// <summary>Latest selectable date; later days render disabled. <c>null</c> leaves it open.</summary>
    [Parameter] public DateTime? MaxDate { get; set; }
    [Parameter] public string PreviousMonthAriaLabel { get; set; } = "Previous month";
    [Parameter] public string NextMonthAriaLabel { get; set; } = "Next month";
    [Parameter] public string HourAriaLabel { get; set; } = "Hour";
    [Parameter] public string MinuteAriaLabel { get; set; } = "Minute";
    [Parameter] public string MeridiemAriaLabel { get; set; } = "AM/PM";
    [Parameter] public string SelectedDayAriaLabel { get; set; } = "selected";

    /// <summary>
    /// Accessible name for the picker's trigger button. When set, assistive tech announces this
    /// field name together with the current value (e.g. "Start date and time: 5/1/2025 09:00").
    /// </summary>
    [Parameter] public string? TriggerAriaLabel { get; set; }

    /// <summary>The id of the trigger button, so a form can move the focus onto a field it found invalid.</summary>
    [Parameter] public string? TriggerId { get; set; }

    /// <summary>Whether the field the picker edits failed validation (aria-invalid on the trigger).</summary>
    [Parameter] public bool Invalid { get; set; }

    /// <summary>The id of the element describing the field, typically its validation message.</summary>
    [Parameter] public string? DescribedBy { get; set; }

    [CascadingParameter] public BitFullCalendarState? State { get; set; }

    private readonly string _id = $"bit-bfc-dtp-{Guid.NewGuid():N}";
    private string _TriggerId => TriggerId ?? $"{_id}-trigger";
    private string _PanelId => $"{_id}-panel";
    private DateTime? _focusDate;
    private string? _pendingFocusId;

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

    private DayOfWeek ResolvedFirstDayOfWeek
        => BitFullCalendarHelpers.ResolveFirstDayOfWeek(Culture, FirstDayOfWeek);

    private string[] BuildWeekdayHeaders()
    {
        var source = Culture.DateTimeFormat.AbbreviatedDayNames;
        var firstDay = (int)ResolvedFirstDayOfWeek;
        return Enumerable.Range(0, 7)
            .Select(i => source[(i + firstDay) % 7])
            .ToArray();
    }

    /// <summary>True when the supplied date sits outside the window the calendar may navigate to.</summary>
    private bool IsOutOfRange(DateTime date)
        => (MinDate is { } min && date.Date < min.Date) || (MaxDate is { } max && date.Date > max.Date);

    private string DayId(DateTime date) => $"{_id}-{date:yyyyMMdd}";

    /// <summary>
    /// The day that owns the grid's tab stop: the one the arrow keys landed on, else the picked date, else the first
    /// enabled day of the month shown - always one of the month shown, so the stop never sits on a muted neighbour, and
    /// never on a disabled day, which cannot take the focus.
    /// </summary>
    private DateTime RovingDate
    {
        get
        {
            if (_focusDate is { } focused && IsSameCalendarMonth(focused, _visibleMonthAnchor) && IsOutOfRange(focused) is false)
                return focused.Date;
            if (IsSameCalendarMonth(Value, _visibleMonthAnchor) && IsOutOfRange(Value) is false)
                return Value.Date;

            for (var day = _visibleMonthAnchor.Date; IsSameCalendarMonth(day, _visibleMonthAnchor); day = day.AddDays(1))
            {
                if (IsOutOfRange(day) is false)
                    return day;
            }

            return _visibleMonthAnchor.Date;
        }
    }

    private void ToggleOpen()
    {
        _isOpen = !_isOpen;
        // Opening moves the focus onto the day grid, the way a date dialog opens; closing leaves it on the trigger.
        _focusDate = null;
        _pendingFocusId = _isOpen ? DayId(RovingDate) : null;
    }

    private void Close()
    {
        _isOpen = false;
        // The day that held the focus is about to leave the DOM, so the focus goes back to the field it belongs to
        // instead of dropping out of the dialog the picker sits in.
        _pendingFocusId = _TriggerId;
    }

    // The Escape that closes the popup, claimed on the picker and its panel (see Utils.claimEscape) - read off the
    // same state OnKeyDown decides on, so a closed picker leaves the key to the dialog it sits in.
    private string? _EscapeClaim => _isOpen ? "claim" : null;

    private void OnKeyDown(KeyboardEventArgs e)
    {
        // Escape closes the popup rather than the surrounding dialog; the markup stops the event
        // there while the popup is open so the two do not close together.
        if (_isOpen && e.Key is "Escape" or "Esc")
            Close();
    }

    private void OnDayKeyDown(DateTime date, KeyboardEventArgs e)
    {
        var isRtl = State?.IsRtl ?? Culture.TextInfo.IsRightToLeft;
        DateTime target;
        try
        {
            target = e.Key switch
            {
                "ArrowRight" => date.AddDays(isRtl ? -1 : 1),
                "ArrowLeft" => date.AddDays(isRtl ? 1 : -1),
                "ArrowDown" => date.AddDays(7),
                "ArrowUp" => date.AddDays(-7),
                "PageDown" => e.ShiftKey ? ActiveCalendar.AddYears(date, 1) : ActiveCalendar.AddMonths(date, 1),
                "PageUp" => e.ShiftKey ? ActiveCalendar.AddYears(date, -1) : ActiveCalendar.AddMonths(date, -1),
                "Home" => date.AddDays(-(((int)date.DayOfWeek - (int)ResolvedFirstDayOfWeek + 7) % 7)),
                "End" => date.AddDays(6 - (((int)date.DayOfWeek - (int)ResolvedFirstDayOfWeek + 7) % 7)),
                _ => date
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        if (target == date)
            return;

        // A disabled day cannot take the focus, so a single step walks on over the run of them; a page that lands on
        // one goes nowhere.
        if (e.Key.StartsWith("Arrow", StringComparison.Ordinal))
        {
            var step = target > date ? 1 : -1;
            for (var i = 0; i < 366 && IsOutOfRange(target); i++)
                target = target.AddDays(step);
        }

        if (IsOutOfRange(target))
            return;

        _focusDate = target.Date;
        if (IsSameCalendarMonth(target, _visibleMonthAnchor) is false)
            _visibleMonthAnchor = GetFirstDayOfMonth(target);

        _pendingFocusId = DayId(target.Date);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_pendingFocusId is not { } id)
            return;

        _pendingFocusId = null;
        await BitFcFocusInterop.TryFocusAsync(JS, id);
    }

    private void ShowPreviousMonth()
    {
        _visibleMonthAnchor = GetFirstDayOfMonth(ActiveCalendar.AddMonths(_visibleMonthAnchor, -1));
        _focusDate = null;
    }

    private void ShowNextMonth()
    {
        _visibleMonthAnchor = GetFirstDayOfMonth(ActiveCalendar.AddMonths(_visibleMonthAnchor, 1));
        _focusDate = null;
    }

    private async Task SelectDate(DateTime date)
    {
        var selected = new DateTime(date.Year, date.Month, date.Day, _hour, _minute, 0, Value.Kind);
        Value = selected;
        _lastSyncedDate = selected;
        // Re-anchor the visible month to the selected date so picking a muted overflow day from a
        // neighboring month doesn't leave the grid on the old month (OnParametersSet won't re-anchor
        // because _lastSyncedDate now matches Value).
        _visibleMonthAnchor = GetFirstDayOfMonth(selected);
        Close();
        await ValueChanged.InvokeAsync(selected);
    }

    private async Task OnTimeChanged()
    {
        var selected = new DateTime(Value.Year, Value.Month, Value.Day, _hour, _minute, 0, Value.Kind);
        Value = selected;
        _lastSyncedDate = selected;
        await ValueChanged.InvokeAsync(selected);
    }

    // 12-hour view helpers. _hour stays canonical (0-23); these project it onto the 12-hour
    // hour list and AM/PM selector and convert edits back to the canonical 24-hour value.
    private int Hour12 => _hour % 12 == 0 ? 12 : _hour % 12;
    private bool IsPm => _hour >= 12;

    private static int ConvertTo24Hour(int hour12, bool pm)
    {
        var h = hour12 % 12; // 12 maps to 0
        return pm ? h + 12 : h;
    }

    private async Task OnHour12Changed(ChangeEventArgs e)
    {
        if (!int.TryParse(e.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var h12))
            return;
        _hour = ConvertTo24Hour(Math.Clamp(h12, 1, 12), IsPm);
        await OnTimeChanged();
    }

    private async Task OnMeridiemChanged(ChangeEventArgs e)
    {
        var pm = string.Equals(e.Value?.ToString(), "PM", StringComparison.Ordinal);
        _hour = ConvertTo24Hour(Hour12, pm);
        await OnTimeChanged();
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
        var firstDayOfWeek = ResolvedFirstDayOfWeek;
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
        // Anchor the first day within the same era as the source date. Era-based calendars (e.g.
        // JapaneseCalendar) repeat year numbers across eras, so resolving without the era would
        // map the anchor to the wrong era's month.
        var era = ActiveCalendar.GetEra(date);
        return ActiveCalendar.ToDateTime(year, month, 1, 0, 0, 0, 0, era);
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

    private string GetDayAriaLabel(CalendarDay day)
    {
        var fullDate = day.Date.ToString("D", Culture);
        return day.IsSelected ? $"{fullDate}, {SelectedDayAriaLabel}" : fullDate;
    }

    private string GetDisplayText()
    {
        var datePart = Value.ToString("d", Culture);
        if (ShowTime is false)
            return datePart;

        // Honor the configured time format: 24-hour ("HH:mm") or 12-hour with the culture's AM/PM
        // designator ("h:mm tt"), matching the rest of the calendar's time rendering.
        var timePart = Value.ToString(Use24HourFormat ? "HH:mm" : "h:mm tt", Culture);
        return $"{datePart} {timePart}";
    }

    private string? GetTriggerAriaLabel()
        => string.IsNullOrEmpty(TriggerAriaLabel) ? null : $"{TriggerAriaLabel}: {GetDisplayText()}";

    private sealed record CalendarDay(DateTime Date, string Label, bool IsCurrentMonth, bool IsSelected);

    public void Dispose()
    {
        _closeCts?.Cancel();
        _closeCts?.Dispose();
    }
}
