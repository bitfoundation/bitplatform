namespace Bit.BlazorUI;

public partial class BitFcRenderGroupedEvents
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [Parameter] public List<List<BitFullCalendarEvent>> GroupedEvents { get; set; } = [];
    [Parameter] public DateTime Day { get; set; }
    [Parameter] public EventCallback<BitFullCalendarEvent> OnEventSelected { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    /// <summary>
    /// The events of one lane group that the grid's visible hour window actually covers, each with
    /// the offset it is drawn at and the height it is drawn with once clipped to that window. An
    /// event lying entirely outside the window is left out rather than pinned to the first row.
    /// </summary>
    private List<(BitFullCalendarEvent Event, double OffsetHours, double DurationHours)> VisibleBlocks(List<BitFullCalendarEvent> group)
    {
        var blocks = new List<(BitFullCalendarEvent, double, double)>(group.Count);

        foreach (var ev in group)
        {
            if (BitFullCalendarHelpers.GetEventBlockPlacement(ev, Day, State.VisibleStartHour, State.VisibleEndHour) is { } placement)
                blocks.Add((ev, placement.OffsetHours, placement.DurationHours));
        }

        return blocks;
    }
}
