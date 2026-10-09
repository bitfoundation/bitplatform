namespace Bit.BlazorUI;

public partial class BitFcDayBackgroundLabels
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter] internal BitFcParts Parts { get; set; } = default!;

    /// <summary>
    /// The calendar's background events, passed so a change to them re-renders the labels even when the day did not.
    /// </summary>
    [Parameter] public IReadOnlyList<BitFullCalendarEvent> Events { get; set; } = [];

    /// <summary>The day whose header the labels sit in.</summary>
    [Parameter] public DateTime Day { get; set; }

    private List<BitFullCalendarEvent> _events = [];

    protected override void OnParametersSet()
        => _events = Events.Count == 0 ? [] : State.GetWholeDayBackground(Day).Where(e => string.IsNullOrWhiteSpace(e.Title) is false).ToList();
}
