namespace Bit.BlazorUI;

public partial class BitFcRenderGroupedEvents
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [Parameter] public List<List<BitFullCalendarEvent>> GroupedEvents { get; set; } = [];
    [Parameter] public DateTime Day { get; set; }
    [Parameter] public EventCallback<BitFullCalendarEvent> OnEventSelected { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }
}
