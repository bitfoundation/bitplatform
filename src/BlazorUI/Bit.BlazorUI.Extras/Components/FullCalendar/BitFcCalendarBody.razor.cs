namespace Bit.BlazorUI;

public partial class BitFcCalendarBody
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;

    [Parameter] public RenderFragment<BitFullCalendarEvent>? MonthEventTemplate { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? WeekEventTemplate { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? DayEventTemplate { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? TimelineEventTemplate { get; set; }

    private List<BitFullCalendarEvent> _singleDayEvents = [];
    private List<BitFullCalendarEvent> _multiDayEvents = [];
    private List<BitFullCalendarEvent> _timelineEvents = [];

    protected override void OnInitialized()
    {
        State.OnStateChanged += Refresh;
        ComputeEvents();
    }

    private void Refresh()
    {
        ComputeEvents();
        InvokeAsync(StateHasChanged);
    }

    private void ComputeEvents()
    {
        // The split decides which surface renders an event: the hour grid, or the all-day row above
        // it. An event explicitly marked all-day joins the multi-day bucket even when it covers a
        // single date, so it is never placed on the time axis.
        _singleDayEvents = State.Events.Where(e => e.IsAllDayOrMultiDay is false).ToList();
        _multiDayEvents = State.Events.Where(e => e.IsAllDayOrMultiDay).ToList();
        _timelineEvents = State.Events.ToList();
    }

    public void Dispose() => State.OnStateChanged -= Refresh;
}
