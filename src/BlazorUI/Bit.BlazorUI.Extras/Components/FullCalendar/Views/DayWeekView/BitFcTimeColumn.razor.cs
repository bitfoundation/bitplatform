namespace Bit.BlazorUI;

public partial class BitFcTimeColumn
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
}
