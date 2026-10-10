using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

public partial class BitFcEventDetailsDialog : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [CascadingParameter] public BitFullCalendarState State { get; set; } = default!;
    [CascadingParameter] internal BitFcParts Parts { get; set; } = default!;
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
    // Which Escapes something inside the dialog claimed first (see _OnEscapeVerdict), for OnDialogKeyDown. Made on
    // first use, so a dialog disposed before it rendered never starts watching.
    private BitEscapeClaimWatch<BitFcEventDetailsDialog>? _escapeWatch;
    private BitEscapeClaimWatch<BitFcEventDetailsDialog> EscapeWatch => _escapeWatch ??= new(JS, this);

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
    /// The event whose lock decides the actions: for an occurrence, its series master - the record the lock lives on,
    /// and the one an edit or a delete of the whole series changes.
    /// </summary>
    private BitFullCalendarEvent? LockHolder => Event.IsOccurrence ? SeriesMaster : Event;

    /// <summary>
    /// True while the Edit action is offered: the calendar is editable, editing is allowed, and the event is not
    /// locked with <see cref="BitFullCalendarEvent.IsReadOnly"/>.
    /// </summary>
    private bool CanEdit => LockHolder is { } holder && State.CanEdit(holder);

    /// <summary>True while the Delete action is offered - the same rule with deleting allowed instead.</summary>
    private bool CanDelete => LockHolder is { } holder && State.CanDelete(holder);

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
        {
            await BitFcDialogInterop.SetupAsync(JS, _dialogRef);
            await EscapeWatch.WatchAsync(_dialogRef);
        }
    }

    // Whether Escape closes the dialog, the one rule OnDialogKeyDown and _EscapeClaim both go by. While the edit
    // overlay or the scope prompt is open it owns the key (and claims it on its own dialog), and a delete in flight
    // is left alone so the dialog can't close mid-commit.
    private bool _ClosesOnEscape => _showEdit is false && _pendingScopeAction is ScopeAction.None && _isDeleting is false;

    // The Escape that closes the dialog, claimed on the dialog (see Utils.claimEscape) so a surface the calendar sits
    // in does not close on the same press. It stays claimed while a delete is in flight too, when the press does
    // nothing: a surface closing on it would take the dialog away mid-commit all the same.
    private string? _EscapeClaim => _ClosesOnEscape || _isDeleting ? "claim" : null;

    private async Task OnDialogKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not "Escape") return;

        // One Escape does one thing: a press something inside the dialog claimed first (a field of an
        // EventDetailsTemplate clearing itself) is that field's, which Blazor still bubbles up to here. The verdict
        // is read once, so it never outlives the press it was given for.
        if (EscapeWatch.TakeForeign()) return;

        // Only the plain key, the one the dialog claims: an Escape with a modifier is left to the surface the
        // calendar sits in.
        if (e.IsPlainEscape() is false) return;

        // Escape is the standard way out of a modal.
        if (_ClosesOnEscape)
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
        if (CanDelete is false)
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
        if ((action is ScopeAction.Edit ? CanEdit : CanDelete) is false || SeriesMaster is not { } master)
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
                    // Refused by OnChanging: the event is already back, and the dialog stays open on it.
                    if (await Notifier.TryNotifyAsync(new BitFullCalendarChangeEventArgs
                    {
                        Event = snapshot,
                        OldEvent = snapshot,
                        Kind = BitFullCalendarChangeKind.Delete,
                        Source = BitFullCalendarChangeSource.Dialog
                    }) is false)
                        return;
                }
                catch
                {
                    // Restore the removed event so the calendar doesn't show it gone while consumers
                    // were never notified; the next attempt will remove and notify again.
                    State.RestoreEvent(snapshot);
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
                    if (await Notifier.TryNotifyAsync(new BitFullCalendarChangeEventArgs
                    {
                        Event = BitFullCalendarChangeNotifier.CloneEvent(updated),
                        OldEvent = snapshot,
                        Kind = BitFullCalendarChangeKind.Edit,
                        Source = BitFullCalendarChangeSource.Dialog
                    }) is false)
                        return;
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

    /// <summary>
    /// Sent by the browser as each Escape goes down inside the dialog while it claims the key, ahead of the keydown
    /// Blazor then dispatches, with whether something inside the dialog claimed it first - a field of an EventDetailsTemplate
    /// clearing itself - so the key handler that runs next leaves that press alone.
    /// <br />
    /// <strong>This method is intended for internal use and should not be called directly.</strong>
    /// </summary>
    [JSInvokable("OnEscapeVerdict")]
    public void _OnEscapeVerdict(bool foreign)
    {
        EscapeWatch.SetVerdict(foreign);
    }

    public async ValueTask DisposeAsync()
    {
        await EscapeWatch.DisposeAsync();
        await BitFcDialogInterop.TeardownAsync(JS, _dialogRef);
    }
}
