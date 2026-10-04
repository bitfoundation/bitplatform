using Bit.BlazorUI;

namespace Bit.BlazorUI;

/// <summary>
/// Dispatches calendar event change notifications to the component consumer.
/// Also provides wrappers for mutation paths that need pre/post snapshots.
/// </summary>
public sealed class BitFullCalendarChangeNotifier
{
    private readonly BitFullCalendarState _state;
    private readonly Func<BitFullCalendarChangeEventArgs, Task> _dispatch;

    public BitFullCalendarChangeNotifier(BitFullCalendarState state, Func<BitFullCalendarChangeEventArgs, Task> dispatch)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(dispatch);

        _state = state;
        _dispatch = dispatch;
    }

    /// <summary>
    /// Invoked when the calendar refuses a user-driven change (an overlap while overlaps are
    /// disallowed, a target outside the allowed date window, a locked event). The component wires
    /// this to the in-calendar notice so every refusal path reports itself the same way.
    /// </summary>
    public Action<BitFullCalendarChangeRefusal>? RefusalReporter { get; set; }

    /// <summary>Reports a refusal through <see cref="RefusalReporter"/> when one was set.</summary>
    public void ReportRefusal(BitFullCalendarChangeRefusal refusal)
    {
        if (refusal is not BitFullCalendarChangeRefusal.None)
            RefusalReporter?.Invoke(refusal);
    }

    /// <summary>
    /// Asked before a change is reported, with the change the state already holds. Returning <c>false</c> refuses
    /// it: the notifier puts the state back the way it was and the change is never dispatched. The component wires
    /// this to its <c>OnChanging</c> callback.
    /// </summary>
    public Func<BitFullCalendarChangeEventArgs, Task<bool>>? ApprovalHandler { get; set; }

    /// <summary>
    /// Dispatches a change payload to the component's <c>OnChange</c> callback, unless the
    /// <see cref="ApprovalHandler"/> refuses it.
    /// </summary>
    public Task NotifyAsync(BitFullCalendarChangeEventArgs args) => TryNotifyAsync(args);

    /// <summary>
    /// Dispatches a change payload to the component's <c>OnChange</c> callback, unless the
    /// <see cref="ApprovalHandler"/> refuses it - in which case the state is put back the way it was before the
    /// change and <c>false</c> is returned, so a caller (a dialog) can stay open instead of acting as if it saved.
    /// </summary>
    public async Task<bool> TryNotifyAsync(BitFullCalendarChangeEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (ApprovalHandler is { } approve && await approve(args) is false)
        {
            Revert(args);
            return false;
        }

        await _dispatch(args);
        return true;
    }

    /// <summary>
    /// Asks the <see cref="ApprovalHandler"/> about a change without dispatching it or undoing anything, for a caller
    /// that reports several changes as one and has to know all of them are allowed before reporting any.
    /// </summary>
    public async Task<bool> ApproveAsync(BitFullCalendarChangeEventArgs args)
        => ApprovalHandler is not { } approve || await approve(args);

    /// <summary>
    /// Dispatches a change that was already approved (see <see cref="ApproveAsync"/>) to the <c>OnChange</c> callback.
    /// </summary>
    public Task DispatchAsync(BitFullCalendarChangeEventArgs args) => _dispatch(args);

    // Every mutation path commits to the state before it reports, so a refused change is undone from what the report
    // carries: an add is removed again, an edit gets its previous snapshot back, and a delete is put back in place -
    // unless it is back already, from a consumer list re-synced while an asynchronous OnChanging was awaited.
    private void Revert(BitFullCalendarChangeEventArgs args)
    {
        switch (args.Kind)
        {
            case BitFullCalendarChangeKind.Add:
                _state.RemoveEvent(args.Event.Id);
                break;
            case BitFullCalendarChangeKind.Edit when args.OldEvent is not null:
                _state.UpdateEvent(CloneEvent(args.OldEvent));
                break;
            case BitFullCalendarChangeKind.Delete:
                _state.RestoreEvent(CloneEvent(args.OldEvent ?? args.Event));
                break;
        }
    }

    /// <summary>
    /// Drops the dragged event on the supplied date/time under every rule a drop obeys and commits it through
    /// <see cref="CommitEditAsync"/>, when the event date-time has actually changed.
    /// </summary>
    public Task HandleDropAsync(DateTime targetDate, int? hour = null, int? minute = null)
        => HandleDropCoreAsync(targetDate, hour, minute, resourceId: null, applyResource: false);

    /// <summary>
    /// Drops the dragged event on the supplied date/time and (optionally) reassigns its resource,
    /// emitting an Edit change when anything actually changed.
    /// </summary>
    public Task HandleResourceDropAsync(DateTime targetDate, int? hour, int? minute, string? resourceId)
        => HandleDropCoreAsync(targetDate, hour, minute, resourceId, applyResource: true);

    private async Task HandleDropCoreAsync(DateTime targetDate, int? hour, int? minute, string? resourceId, bool applyResource)
    {
        var dragged = _state.DraggedEvent;
        if (dragged is null)
            return;

        var (refusal, target) = _state.ResolveDrop(targetDate, hour, minute, resourceId, applyResource);
        _state.EndDrag();

        if (refusal is not BitFullCalendarChangeRefusal.None)
        {
            // The drop was rejected (overlap, out of range, locked event): tell the user why instead
            // of silently snapping the block back to where it started.
            ReportRefusal(refusal);
            return;
        }

        if (target is { } t)
            await CommitEditAsync(dragged, t.Start, t.End, t.Resource, BitFullCalendarChangeSource.Drag);
    }

    /// <summary>
    /// Commits a move or a resize of <paramref name="original"/> - from a drag, a resize handle or their keyboard
    /// equivalents - to the supplied range and resource, and reports it. The caller has already passed the change
    /// through <see cref="BitFullCalendarState.ValidateRange"/>.
    /// <para>
    /// An occurrence of a series changes on its own, the way one meeting of a series is moved in a calendar app: its
    /// date is skipped on the series master and a one-off takes its place. That is reported as an edit of the master
    /// and an add, which stand or fall together.
    /// </para>
    /// Returns <c>false</c> when <c>OnChanging</c> refused it; the state is then back where it was.
    /// </summary>
    public async Task<bool> CommitEditAsync(BitFullCalendarEvent original, DateTime start, DateTime end, string? resource, BitFullCalendarChangeSource source)
    {
        ArgumentNullException.ThrowIfNull(original);

        if (original.IsOccurrence is false)
        {
            var oldSnapshot = CloneEvent(original);
            var updated = CloneEvent(original);
            updated.StartDate = start;
            updated.EndDate = end;
            updated.Resource = resource;

            _state.UpdateEvent(updated);
            try
            {
                return await TryNotifyAsync(new BitFullCalendarChangeEventArgs
                {
                    Event = CloneEvent(updated),
                    OldEvent = oldSnapshot,
                    Kind = BitFullCalendarChangeKind.Edit,
                    Source = source
                });
            }
            catch
            {
                // The report failed: put the previous times back so the state matches what the consumer believes.
                _state.UpdateEvent(oldSnapshot);
                throw;
            }
        }

        if (_state.DetachOccurrence(original, start, end, resource) is not { } detach)
            return false;

        return await CommitDetachAsync(detach.Master, detach.Skipped, detach.Detached, source);
    }

    /// <summary>
    /// Reports an occurrence the state has already detached from its series - <paramref name="skipped"/> (the series
    /// <paramref name="master"/> with the occurrence's date skipped) in place of the master, and the one-off
    /// <paramref name="detached"/> added - as an edit of the master and an add, which stand or fall together: both are
    /// approved before either is dispatched, and a refusal of either puts the state back and reports nothing.
    /// Returns <c>false</c> when <c>OnChanging</c> refused it.
    /// </summary>
    internal async Task<bool> CommitDetachAsync(BitFullCalendarEvent master, BitFullCalendarEvent skipped, BitFullCalendarEvent detached, BitFullCalendarChangeSource source)
    {
        void Undo()
        {
            if (_state.LastDetach?.To == detached.Id)
                _state.ClearLastDetach();
            _state.RemoveEvent(detached.Id);
            _state.UpdateEvent(master);
        }

        var skip = new BitFullCalendarChangeEventArgs
        {
            Event = CloneEvent(skipped),
            OldEvent = CloneEvent(master),
            Kind = BitFullCalendarChangeKind.Edit,
            Source = source
        };
        var add = new BitFullCalendarChangeEventArgs
        {
            Event = CloneEvent(detached),
            Kind = BitFullCalendarChangeKind.Add,
            Source = source
        };

        var dispatched = false;
        try
        {
            if (await ApproveAsync(skip) is false || await ApproveAsync(add) is false)
            {
                Undo();
                return false;
            }

            dispatched = true;
            await DispatchAsync(skip);
            await DispatchAsync(add);
            return true;
        }
        catch
        {
            // Once the first half has reached the consumer, taking the change back here would leave the calendar and
            // the consumer disagreeing; the state keeps what was reported and the failure surfaces to the caller.
            if (dispatched is false)
                Undo();
            throw;
        }
    }

    /// <summary>
    /// Creates a snapshot of a calendar event payload suitable for change args. Value-type fields
    /// and the <see cref="BitFullCalendarEvent.Attendees"/> collection are copied into fresh
    /// instances, but the consumer-defined <see cref="BitFullCalendarEvent.Data"/> payload is
    /// shared by reference (it is an opaque <c>object?</c> that cannot be generically cloned).
    /// </summary>
    public static BitFullCalendarEvent CloneEvent(BitFullCalendarEvent source) =>
        new()
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Color = source.Color,
            Resource = source.Resource,
            Data = source.Data,
            IsAllDay = source.IsAllDay,
            IsReadOnly = source.IsReadOnly,
            CssClass = source.CssClass,
            IsBackground = source.IsBackground,
            IsBlocking = source.IsBlocking,
            // The repeat rule is a consumer-owned object like Data, so it travels by reference.
            Recurrence = source.Recurrence,
            SeriesId = source.SeriesId,
            OccurrenceDate = source.OccurrenceDate,
            Attendees = source.Attendees
                .Select(a => new BitFullCalendarAttendee
                {
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Id = a.Id
                })
                .ToList()
        };
}

