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
    private BitFullCalendarEvent? _editTarget;
    private ScopeAction _pendingScopeAction;
    private bool _isDeleting;
    private bool _deleteCommitted;
    private ElementReference _dialogRef;
    private readonly string _dialogTitleId = $"bfc-details-title-{Guid.NewGuid():N}";

    /// <summary>The action waiting for the user to say whether it means one occurrence or the series.</summary>
    private enum ScopeAction { None, Edit, Delete }

    /// <summary>
    /// The master a generated occurrence was expanded from, or <c>null</c> when the event is not an
    /// occurrence (or its series has since been removed).
    /// </summary>
    private BitFullCalendarEvent? SeriesMaster => Event.SeriesId is { Length: > 0 } seriesId
        ? State.AllEvents.FirstOrDefault(e => string.Equals(e.Id, seriesId, StringComparison.Ordinal))
        : null;

    /// <summary>
    /// True while the edit and delete actions are offered: the calendar has to be editable AND the
    /// event itself must not be locked with <see cref="BitFullCalendarEvent.IsReadOnly"/>. An
    /// occurrence is always read-only (so it cannot be dragged), so for one the lock that counts is
    /// its series master's.
    /// </summary>
    private bool CanEdit => State.ReadOnly is false
        && (Event.IsOccurrence ? SeriesMaster is { IsReadOnly: false } : Event.IsReadOnly is false);

    /// <summary>
    /// One-line description of the repeat rule behind this event, or <c>null</c> for a one-off.
    /// <para>
    /// A generated occurrence carries no rule of its own - it is a projection of its master - so the
    /// rule is looked up through <see cref="BitFullCalendarEvent.SeriesId"/> when the clicked event
    /// is one. Without it an occurrence would give no sign that it belongs to a series.
    /// </para>
    /// </summary>
    private string? RecurrenceSummary
    {
        get
        {
            var master = Event.Recurrence is null ? SeriesMaster : Event;
            return master?.Recurrence is { } rule ? Texts.GetRecurrenceSummary(rule, State.Culture, master.StartDate) : null;
        }
    }

    /// <summary>Display name of the assigned resource, or the "unassigned" label when there is none.</summary>
    private string ResourceTitle
    {
        get
        {
            if (string.IsNullOrEmpty(Event.Resource))
                return Texts.NoResourceLabel;

            var match = State.Resources.FirstOrDefault(r => string.Equals(r.Id, Event.Resource, StringComparison.Ordinal));
            // An id with no matching resource is still worth showing verbatim - it is what the event
            // actually carries, and hiding it would make a stale assignment invisible.
            return match is null || string.IsNullOrWhiteSpace(match.Title) ? Event.Resource! : match.Title;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Move focus into the dialog and trap Tab navigation once it has rendered; teardown in
        // DisposeAsync restores focus to the element that was focused before it opened.
        if (firstRender)
            await BitFcDialogInterop.SetupAsync(JS, _dialogRef);
    }

    private async Task OnDialogKeyDown(KeyboardEventArgs e)
    {
        // Escape is the standard way out of a modal. While the edit overlay or the scope prompt is
        // open it owns the key, and a delete in flight is left alone so the dialog can't close
        // mid-commit.
        if (e.Key is "Escape" or "Esc" && _showEdit is false && _pendingScopeAction is ScopeAction.None && _isDeleting is false)
            await OnClose.InvokeAsync();
    }

    private void Edit()
    {
        if (CanEdit is false)
            return;

        // An occurrence first asks whether the edit means it alone or the whole series.
        if (Event.IsOccurrence)
        {
            _pendingScopeAction = ScopeAction.Edit;
            return;
        }

        _editTarget = Event;
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

    private Task Delete()
    {
        if (CanEdit is false)
            return Task.CompletedTask;

        if (Event.IsOccurrence)
        {
            _pendingScopeAction = ScopeAction.Delete;
            return Task.CompletedTask;
        }

        return DeleteEventAsync(Event);
    }

    private void OnScopeCancelled() => _pendingScopeAction = ScopeAction.None;

    /// <summary>
    /// Carries out the action the scope prompt was opened for. "This event" edits the occurrence
    /// itself (the add/edit dialog detaches it from the series) or skips its date; "all events" edits
    /// or removes the series master.
    /// </summary>
    private async Task OnScopeConfirmed(bool allOccurrences)
    {
        var action = _pendingScopeAction;
        _pendingScopeAction = ScopeAction.None;

        // Read-only may have been switched on, or the series removed, while the prompt was open.
        if (CanEdit is false || SeriesMaster is not { } master)
            return;

        if (action is ScopeAction.Edit)
        {
            _editTarget = allOccurrences ? master : Event;
            _showEdit = true;
        }
        else if (action is ScopeAction.Delete)
        {
            if (allOccurrences)
                await DeleteEventAsync(master);
            else
                await SkipOccurrenceAsync(master);
        }
    }

    private async Task DeleteEventAsync(BitFullCalendarEvent target)
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
                var snapshot = BitFullCalendarChangeNotifier.CloneEvent(target);
                State.RemoveEvent(target.Id);
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

    /// <summary>
    /// Deletes this occurrence alone: the series skips its date, reported as an Edit of the master,
    /// and every other occurrence stays where it was.
    /// </summary>
    private async Task SkipOccurrenceAsync(BitFullCalendarEvent master)
    {
        if (_isDeleting || Event.OccurrenceDate is not { } occurrenceDate)
            return;
        _isDeleting = true;

        try
        {
            if (!_deleteCommitted)
            {
                var snapshot = BitFullCalendarChangeNotifier.CloneEvent(master);
                var updated = BitFullCalendarHelpers.SkipOccurrence(master, occurrenceDate);
                State.UpdateEvent(updated);
                try
                {
                    await Notifier.NotifyAsync(new BitFullCalendarChangeEventArgs
                    {
                        Event = BitFullCalendarChangeNotifier.CloneEvent(updated),
                        OldEvent = snapshot,
                        Kind = BitFullCalendarChangeKind.Edit,
                        Source = BitFullCalendarChangeSource.Dialog
                    });
                }
                catch
                {
                    State.UpdateEvent(master);
                    throw;
                }
                _deleteCommitted = true;
            }

            await OnClose.InvokeAsync();
        }
        finally
        {
            _isDeleting = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await BitFcDialogInterop.TeardownAsync(JS, _dialogRef);
    }
}
