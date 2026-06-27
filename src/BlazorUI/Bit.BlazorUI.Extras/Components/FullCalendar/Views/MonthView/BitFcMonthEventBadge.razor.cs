namespace Bit.BlazorUI;

public partial class BitFcMonthEventBadge
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [Parameter] public BitFullCalendarEvent Event { get; set; } = default!;
    [Parameter] public DateTime CellDate { get; set; }
    [Parameter] public string Position { get; set; } = "none";
    [Parameter] public EventCallback<BitFullCalendarEvent> OnSelected { get; set; }
    [Parameter] public RenderFragment<BitFullCalendarEvent>? EventTemplate { get; set; }

    private string MarginStyle
    {
        get
        {
            var isRtl = State.IsRtl;
            return Position switch
            {
                "first"  => isRtl ? "margin-left:-4px; margin-right:2px;" : "margin-left:2px; margin-right:-4px;",
                "middle" => "margin-left:-4px; margin-right:-4px;",
                "last"   => isRtl ? "margin-left:2px; margin-right:-4px;" : "margin-left:-4px; margin-right:2px;",
                _        => "margin:0 2px;"
            };
        }
    }

    private void OnDragStart() => State.StartDrag(Event);
    private void OnDragEnd() => State.EndDrag();

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Enter" or " " or "Spacebar" && !e.Repeat)
            await OnSelected.InvokeAsync(Event);
    }
}
