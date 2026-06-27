namespace Bit.BlazorUI;

public partial class BitFcTodayButton
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
}
