using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

public partial class BitFcEventDetailsDialog : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarColorScheme ColorScheme { get; set; } = default!;
    [CascadingParameter] public BitFullCalendarChangeNotifier Notifier { get; set; } = default!;
    [Parameter] public BitFullCalendarEvent Event { get; set; } = default!;
    [Parameter] public EventCallback OnClose { get; set; }

    private bool _showEdit;
    private bool _isDeleting;
    private bool _deleteCommitted;
    private ElementReference _dialogRef;
    private readonly string _dialogTitleId = $"bfc-details-title-{Guid.NewGuid():N}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Move focus into the dialog and trap Tab navigation once it has rendered; teardown in
        // DisposeAsync restores focus to the element that was focused before it opened.
        if (firstRender)
            await BitFcDialogInterop.SetupAsync(JS, _dialogRef);
    }

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

        try
        {
            // Once the local removal AND the Delete notification have both succeeded, never send it
            // again: _deleteCommitted is set only after NotifyAsync returns. If NotifyAsync throws,
            // the local removal is rolled back so State stays in sync with consumers (mirroring the
            // add/edit save compensation), and the flag stays unset so a retry re-runs both steps.
            if (!_deleteCommitted)
            {
                var snapshot = BitFullCalendarChangeNotifier.CloneEvent(Event);
                State.RemoveEvent(Event.Id);
                try
                {
                    await Notifier.NotifyAsync(new BitFullCalendarChangeEventArgs
                    {
                        Event = snapshot,
                        OldEvent = snapshot,
                        Kind = BitFullCalendarChangeKind.Delete,
                        Source = BitFullCalendarChangeSource.Dialog
                    });
                }
                catch
                {
                    // Restore the removed event so the calendar doesn't show it gone while consumers
                    // were never notified; the next attempt will remove and notify again.
                    State.AddEvent(snapshot);
                    throw;
                }
                // Mark committed only after the notification has succeeded.
                _deleteCommitted = true;
            }

            await OnClose.InvokeAsync();
        }
        finally
        {
            // The event has already been removed from state, so a throwing notifier/close must not
            // leave the dialog wedged with _isDeleting stuck true - reset it so the user can retry
            // (e.g. close) instead of the delete button staying permanently inert.
            _isDeleting = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await BitFcDialogInterop.TeardownAsync(JS, _dialogRef);
    }
}
