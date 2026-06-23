namespace Bit.BlazorUI;

public partial class BitFcEventListDialog
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter(Name = "OnEventClick")] public EventCallback<BitFullCalendarEvent> OnEventClick { get; set; }
    [Parameter] public DateTime Date { get; set; }
    [Parameter] public List<BitFullCalendarEvent> Events { get; set; } = [];
    [Parameter] public EventCallback OnClose { get; set; }

    private bool _showDetails;
    private BitFullCalendarEvent? _selectedEvent;
    // Drives @onkeydown:preventDefault so Space/Enter activation doesn't also scroll the dialog,
    // while leaving Tab/arrow keys with their native behaviour (no keyboard trap).
    private bool _preventKeyDefault;

    private async Task SelectEvent(BitFullCalendarEvent ev)
    {
        if (OnEventClick.HasDelegate)
        {
            await OnEventClick.InvokeAsync(ev);
            return;
        }
        _selectedEvent = ev;
        _showDetails = true;
    }

    private async Task OnEventKeyDown(KeyboardEventArgs e, BitFullCalendarEvent ev)
    {
        _preventKeyDefault = e.Key is "Enter" or " " or "Spacebar";
        if (_preventKeyDefault)
        {
            await SelectEvent(ev);
        }
    }
}
