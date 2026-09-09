namespace Bit.BlazorUI;

public partial class BitFcMiniCalendar
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;

    private DateTime _displayMonth;
    private DateTime _lastSyncedSelectedDate;
    private string? _lastSyncedCultureName;
    private Type? _lastSyncedCalendarType;

    private readonly string _gridId = "bit-bfc-mini-cal-" + Guid.NewGuid().ToString("N");

    // Roving tabindex: the mini calendar is a single tab stop the arrow keys walk, rather than one
    // stop per rendered day sitting between the day view and whatever follows it.
    private List<BitFullCalendarCell> _cells = [];
    private int _columnCount = 7;
    private DateTime? _focusedDate;
    private bool _pendingFocus;

    private string DayButtonId(DateTime date) => $"{_gridId}-{date:yyyyMMdd}";

    /// <summary>
    /// The day that owns the tab stop: the one the arrow keys landed on, else the selected date when
    /// the displayed month contains it, else the first day of that month.
    /// </summary>
    private DateTime RovingDate
    {
        get
        {
            if (_focusedDate is { } focused && _cells.Any(c => c.Date.Date == focused.Date && IsSelectable(c)))
                return focused.Date;

            if (_cells.Any(c => c.Date.Date == State.SelectedDate.Date && State.IsDateInAllowedRange(State.SelectedDate)))
                return State.SelectedDate.Date;

            var firstSelectable = _cells.FirstOrDefault(c => c.CurrentMonth && IsSelectable(c))
                                  ?? _cells.FirstOrDefault(IsSelectable);
            return (firstSelectable ?? _cells.FirstOrDefault())?.Date.Date ?? DateTime.Today;
        }
    }

    /// <summary>
    /// True when a day is actually pickable. A day the date bounds put out of reach renders a
    /// disabled button, which cannot take focus - so it must never own the grid's single tab stop.
    /// </summary>
    private bool IsSelectable(BitFullCalendarCell cell) => State.IsDateInAllowedRange(cell.Date);

    private void OnDayKeyDown(DateTime date, KeyboardEventArgs args)
    {
        // Enter and Space are the button's own activation keys, so they are left alone.
        var delta = args.Key switch
        {
            // The arrow keys follow the reading direction, which the grid flips in right-to-left.
            "ArrowRight" => State.IsRtl ? -1 : 1,
            "ArrowLeft" => State.IsRtl ? 1 : -1,
            "ArrowDown" => _columnCount,
            "ArrowUp" => -_columnCount,
            _ => 0
        };

        var index = _cells.FindIndex(c => c.Date.Date == date.Date);
        if (index < 0)
            return;

        var target = args.Key switch
        {
            // Home and End walk to the ends of the day's own week row.
            "Home" => index - (index % _columnCount),
            "End" => index - (index % _columnCount) + _columnCount - 1,
            _ => delta == 0 ? -1 : index + delta
        };

        if (target < 0 || target >= _cells.Count)
            return;

        // A disabled day cannot take focus, so the walk steps over it in the direction it was
        // already going rather than stranding the focus on a button that refuses it.
        var step = target - index;
        if (step != 0)
        {
            var direction = Math.Sign(step);
            while (target >= 0 && target < _cells.Count && IsSelectable(_cells[target]) is false)
                target += direction;
        }

        if (target < 0 || target >= _cells.Count || IsSelectable(_cells[target]) is false)
            return;

        _focusedDate = _cells[target].Date.Date;
        _pendingFocus = true;
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The arrow keys only moved the tabbable day; the focus has to follow it here, once the new
        // tabindex has actually been rendered.
        if (_pendingFocus is false)
            return;

        _pendingFocus = false;
        await BitFcFocusInterop.TryFocusAsync(JS, DayButtonId(RovingDate));
    }

    protected override void OnInitialized()
    {
        _lastSyncedSelectedDate = State.SelectedDate;
        _lastSyncedCultureName = State.Culture.Name;
        _lastSyncedCalendarType = State.Culture.Calendar.GetType();
        _displayMonth = StartOfDisplayMonth(State.SelectedDate);
    }

    protected override void OnParametersSet()
    {
        // Keep _displayMonth aligned with external SelectedDate changes without clobbering the
        // user's in-component month browsing (PrevMonth/NextMonth leave SelectedDate untouched).
        // A culture/calendar switch also requires re-normalizing the display month so the header
        // and grid reflect the new calendar system. The calendar type is tracked alongside the
        // culture name because two cultures can share a name while using different calendars.
        if (_lastSyncedSelectedDate != State.SelectedDate
            || !string.Equals(_lastSyncedCultureName, State.Culture.Name, StringComparison.Ordinal)
            || _lastSyncedCalendarType != State.Culture.Calendar.GetType())
        {
            _lastSyncedSelectedDate = State.SelectedDate;
            _lastSyncedCultureName = State.Culture.Name;
            _lastSyncedCalendarType = State.Culture.Calendar.GetType();
            _displayMonth = StartOfDisplayMonth(State.SelectedDate);
        }
    }

    private DateTime StartOfDisplayMonth(DateTime date)
    {
        var cal = State.Culture.Calendar;
        // Preserve the source era so era-based calendars (e.g. Japanese) rebuild the month in the
        // same era; the era-less ToDateTime overload can otherwise select the wrong era or throw.
        return cal.ToDateTime(cal.GetYear(date), cal.GetMonth(date), 1, 0, 0, 0, 0, cal.GetEra(date));
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
