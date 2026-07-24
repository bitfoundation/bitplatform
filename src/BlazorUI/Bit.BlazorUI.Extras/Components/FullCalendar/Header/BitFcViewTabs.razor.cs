namespace Bit.BlazorUI;

public partial class BitFcViewTabs
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;

    private static readonly BitFullCalendarView[] _views = [
        BitFullCalendarView.Day, BitFullCalendarView.Week, BitFullCalendarView.Month,
        BitFullCalendarView.Year, BitFullCalendarView.Agenda
    ];

    private static readonly HashSet<BitFullCalendarView> _timelineViews =
    [
        BitFullCalendarView.Day,
        BitFullCalendarView.Week,
        BitFullCalendarView.Month
    ];
}
