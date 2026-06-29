namespace Bit.BlazorUI;

public partial class BitFcCalendarMonthView
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [Parameter] public List<BitFullCalendarEvent> SingleDayEvents { get; set; } = [];
    [Parameter] public List<BitFullCalendarEvent> MultiDayEvents { get; set; } = [];
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }
}
