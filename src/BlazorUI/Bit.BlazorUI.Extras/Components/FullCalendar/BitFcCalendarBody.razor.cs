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
        _singleDayEvents = State.Events.Where(e => e.IsSingleDay).ToList();
        _multiDayEvents = State.Events.Where(e => e.IsMultiDay).ToList();
    }

    public void Dispose() => State.OnStateChanged -= Refresh;
}
