namespace Bit.BlazorUI;

public partial class BitFcWeekViewMultiDayEventsRow
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter(Name = "OnEventClick")] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }
    [Parameter] public List<BitFullCalendarEvent> MultiDayEvents { get; set; } = [];
    [Parameter] public DateTime Date { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    private DateTime[] _weekDays = [];
    private List<BitFullCalendarEvent> _weekEvents = [];
    private Dictionary<string, int> _eventRows = new();
    private Dictionary<(DateTime Day, int Row), BitFullCalendarEvent> _cellLookup = new();
    private int _rowCount;
    private BitFullCalendarEvent? _selectedEvent;

    protected override void OnParametersSet()
    {
        _weekDays = BitFullCalendarHelpers.GetWeekDates(Date);
        _weekEvents = BitFullCalendarHelpers.GetEventsForWeek(MultiDayEvents, Date)
            .Where(e => e.IsMultiDay)
            .OrderByDescending(e => (e.EndDate - e.StartDate).TotalDays)
            .ThenBy(e => e.StartDate)
            .ToList();

        _eventRows = new Dictionary<string, int>();
        var rowUsageByDay = _weekDays.ToDictionary(d => d.Date, _ => new HashSet<int>());

        foreach (var ev in _weekEvents)
        {
            var evEndInclusive = (ev.EndDate > ev.StartDate ? ev.EndDate.AddTicks(-1) : ev.EndDate).Date;
            var evDays = _weekDays
                .Where(d => ev.StartDate.Date <= d.Date && evEndInclusive >= d.Date)
                .ToList();

            for (int row = 0; ; row++)
            {
                if (evDays.All(d => !rowUsageByDay[d.Date].Contains(row)))
                {
                    _eventRows[ev.Id] = row;
                    foreach (var d in evDays)
                        rowUsageByDay[d.Date].Add(row);
                    break;
                }
            }
        }

        _rowCount = _eventRows.Count > 0 ? _eventRows.Values.Max() + 1 : 0;

        // Precompute (day, row) -> event so the render loop is an O(1) lookup instead of a
        // FirstOrDefault scan per cell.
        _cellLookup = new Dictionary<(DateTime, int), BitFullCalendarEvent>();
        foreach (var ev in _weekEvents)
        {
            var row = _eventRows[ev.Id];
            var evEndInclusive = (ev.EndDate > ev.StartDate ? ev.EndDate.AddTicks(-1) : ev.EndDate).Date;
            foreach (var d in _weekDays)
            {
                if (ev.StartDate.Date <= d.Date && evEndInclusive >= d.Date)
                    _cellLookup[(d.Date, row)] = ev;
            }
        }
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
