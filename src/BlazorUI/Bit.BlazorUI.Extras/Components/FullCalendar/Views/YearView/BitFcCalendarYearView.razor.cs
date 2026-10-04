namespace Bit.BlazorUI;

public partial class BitFcCalendarYearView
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [Parameter] public List<BitFullCalendarEvent> SingleDayEvents { get; set; } = [];
    [Parameter] public List<BitFullCalendarEvent> MultiDayEvents { get; set; } = [];

    private bool _showEventList;
    private DateTime _eventListDate;

    // Roving tabindex over the days that have events: the year is one tab stop, not one per busy day.
    private readonly string _gridId = $"bit-bfc-year-{Guid.NewGuid():N}";
    private List<DateTime> _eventDays = [];
    private DateTime? _focusedDate;
    private bool _pendingFocus;

    private string DayId(DateTime date) => $"{_gridId}-{date:yyyyMMdd}";

    /// <summary>The busy day that owns the tab stop: the one the arrow keys landed on, else today, else the first.</summary>
    private DateTime? RovingDate
    {
        get
        {
            if (_focusedDate is { } focused && _eventDays.Contains(focused))
                return focused;
            if (_eventDays.Contains(State.Today))
                return State.Today;
            return _eventDays.Count > 0 ? _eventDays[0] : null;
        }
    }

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

    private void ShowEventsForDay(DateTime date)
    {
        _eventListDate = date;
        _focusedDate = date.Date;
        _showEventList = true;
    }

    private void OnDayKeyDown(DateTime date, KeyboardEventArgs e)
    {
        var index = _eventDays.IndexOf(date);
        if (index < 0)
            return;

        var calendar = State.Culture.Calendar;
        // Left and right step to the previous and the next busy day in reading order, up and down to the first one a
        // week away, PageUp and PageDown to the first one of the neighbouring month, Home and End to the year's ends.
        DateTime? target = e.Key switch
        {
            "ArrowRight" => Step(index, State.IsRtl ? -1 : 1),
            "ArrowLeft" => Step(index, State.IsRtl ? 1 : -1),
            "ArrowDown" => _eventDays.FirstOrDefault(d => d >= date.AddDays(7)) is var down && down != default ? down : null,
            "ArrowUp" => _eventDays.LastOrDefault(d => d <= date.AddDays(-7)) is var up && up != default ? up : null,
            "PageDown" => _eventDays.FirstOrDefault(d => d > date && (calendar.GetMonth(d) != calendar.GetMonth(date) || calendar.GetYear(d) != calendar.GetYear(date))) is var next && next != default ? next : null,
            "PageUp" => FirstOfPreviousMonth(date, calendar),
            "Home" => _eventDays[0],
            "End" => _eventDays[^1],
            _ => null
        };

        if (target is not { } t || t == date)
            return;

        _focusedDate = t;
        _pendingFocus = true;
    }

    private DateTime? Step(int index, int delta)
    {
        var target = index + delta;
        return target >= 0 && target < _eventDays.Count ? _eventDays[target] : null;
    }

    private DateTime? FirstOfPreviousMonth(DateTime date, System.Globalization.Calendar calendar)
    {
        var earlier = _eventDays.LastOrDefault(d => calendar.GetMonth(d) != calendar.GetMonth(date) && d < date);
        if (earlier == default)
            return null;

        return _eventDays.First(d => calendar.GetMonth(d) == calendar.GetMonth(earlier) && calendar.GetYear(d) == calendar.GetYear(earlier));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_pendingFocus is false || RovingDate is not { } date)
            return;

        _pendingFocus = false;
        await BitFcFocusInterop.TryFocusAsync(JS, DayId(date));
    }
}
