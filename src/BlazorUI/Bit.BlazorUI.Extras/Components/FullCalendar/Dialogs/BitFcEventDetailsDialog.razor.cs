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
    private bool _isDeleting;
    private readonly string _dialogTitleId = $"bfc-details-title-{Guid.NewGuid():N}";

    private void Edit()
    {
        _showEdit = true;
    }

    private void OnEditClose()
    {
        // Cancelling the edit overlay must only dismiss the edit dialog, not the parent
        // details dialog. The details dialog is closed via OnEditSaved on a real save.
        _showEdit = false;
    }

    private async Task OnEditSaved()
    {
        _showEdit = false;
        await OnClose.InvokeAsync();
    }

    private async Task Delete()
    {
        // Guard against double invocation (rapid clicks / Enter while the async work is in flight):
        // keep the flag set through the notifier and OnClose so the delete only runs once.
        if (_isDeleting)
            return;
        _isDeleting = true;

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
