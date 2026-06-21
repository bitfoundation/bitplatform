namespace Bit.BlazorUI;

public partial class BitFcEventDetailsDialog
{
    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [Parameter] public BitFullCalendarEvent Event { get; set; } = default!;
    [Parameter] public EventCallback OnClose { get; set; }

    private bool _showEdit;

    private void EditAsync()
    {
        _showEdit = true;
    }

    private async Task OnEditClose()
    {
        _showEdit = false;
        await OnClose.InvokeAsync();
    }

    private async Task Delete()
    {
        var snapshot = BitFullCalendarChangeNotifier.CloneEvent(Event);
        State.RemoveEvent(Event.Id);
        await Notifier.NotifyAsync(new BitFullCalendarChangeEventArgs
        {
            Event = snapshot,
            OldEvent = snapshot,
            Kind = BitFullCalendarChangeKind.Delete,
            Source = BitFullCalendarChangeSource.Delete
        });
        await OnClose.InvokeAsync();
    }
}
