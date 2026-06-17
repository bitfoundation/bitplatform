namespace Bit.BlazorUI;

public partial class BitFcModeTabs
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;

    private static readonly BitFullCalendarMode[] _modes =
    [
        BitFullCalendarMode.Event,
        BitFullCalendarMode.Timeline
    ];
}
