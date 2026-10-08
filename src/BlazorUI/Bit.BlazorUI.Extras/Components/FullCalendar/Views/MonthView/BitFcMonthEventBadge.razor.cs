namespace Bit.BlazorUI;

public partial class BitFcMonthEventBadge
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] internal BitFcParts Parts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [Parameter] public BitFullCalendarEvent Event { get; set; } = default!;
    [Parameter] public DateTime CellDate { get; set; }
    [Parameter] public string Position { get; set; } = "none";
    [Parameter] public EventCallback<BitFullCalendarEvent> OnSelected { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    /// <summary>
    /// True for the segment that stands for the event in the tab order and to assistive technology: a multi-day
    /// event is drawn once per day it covers, and only the first segment of each run (a week row) is one stop -
    /// the rest stay clickable but are skipped, instead of repeating the same event once per day.
    /// </summary>
    [Parameter] public bool IsLead { get; set; } = true;

    private string MarginStyle
    {
        get
        {
            // Logical margins, so a bar joined across cells overlaps its neighbour on the side it continues
            // to in either direction - whether the direction comes from the culture or from Dir.
            return Position switch
            {
                "first"  => "margin-inline:2px -4px;",
                "middle" => "margin-inline:-4px;",
                "last"   => "margin-inline:-4px 2px;",
                _        => "margin-inline:2px;"
            };
        }
    }

    private void OnDragStart() => State.StartDrag(Event);
    private void OnDragEnd() => State.EndDrag();

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Enter" or " " or "Spacebar" && !e.Repeat)
        {
            await OnSelected.InvokeAsync(Event);
            return;
        }

        // Keyboard parity for dragging a badge across the month grid: Alt+Arrow moves it a day,
        // following the reading direction, under the same rules the drop path obeys.
        if (e.Key is not ("ArrowLeft" or "ArrowRight") || e.AltKey is false) return;
        if (State.CanDrag(Event) is false) return;

        var forward = (e.Key == "ArrowRight") != State.IsRtl;
        var step = TimeSpan.FromDays(forward ? 1 : -1);
        var start = Event.StartDate + step;
        var end = Event.EndDate + step;

        // The same gate a drop passes through: allowed date window, business hours, booking rule.
        var refusal = State.ValidateRange(Event.Id, start, end, Event.Resource);
        if (refusal is not BitFullCalendarChangeRefusal.None)
        {
            Notifier.ReportRefusal(refusal);
            return;
        }

        // An occurrence of a series moves on its own; the notifier detaches it and reports both halves.
        await Notifier.CommitEditAsync(Event, start, end, Event.Resource, BitFullCalendarChangeSource.Drag);
    }
}
