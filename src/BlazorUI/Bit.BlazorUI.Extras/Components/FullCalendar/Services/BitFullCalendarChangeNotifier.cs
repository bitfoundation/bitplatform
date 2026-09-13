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
    /// Dispatches a change payload to the component's <c>OnChange</c> callback.
    /// </summary>
    public Task NotifyAsync(BitFullCalendarChangeEventArgs args) => _dispatch(args);

    /// <summary>
    /// Applies changes built by <see cref="BitFullCalendarState.BuildEditChanges"/> or
    /// <see cref="BitFullCalendarState.BuildDeleteChanges"/> to the state, then raises <c>OnChange</c> for
    /// each of them in order. A notification that throws rolls every change back, so the calendar never
    /// shows a change its consumer was not told about; nothing is applied while the calendar is read-only.
    /// </summary>
    public async Task CommitAsync(IReadOnlyList<BitFullCalendarChangeEventArgs> changes)
    {
        if (changes.Count == 0 || _state.ReadOnly)
            return;

        foreach (var change in changes)
        {
            _state.ApplyChange(change);
        }

        try
        {
            foreach (var change in changes)
            {
                await NotifyAsync(change);
            }
        }
        catch
        {
            for (var i = changes.Count - 1; i >= 0; i--)
            {
                _state.RevertChange(changes[i]);
            }
            throw;
        }
    }

    /// <summary>
    /// Applies drop logic through <see cref="BitFullCalendarState.HandleDrop"/> and emits
    /// an Edit change when the event date-time has actually changed.
    /// </summary>
    public Task HandleDropAsync(DateTime targetDate, int? hour = null, int? minute = null)
        => HandleDropCoreAsync(targetDate, hour, minute, resourceId: null, applyResource: false);

    /// <summary>
    /// Drops the dragged event on the supplied date/time and (optionally) reassigns its resource,
    /// emitting an Edit change when anything actually changed.
    /// </summary>
    public Task HandleResourceDropAsync(DateTime targetDate, int? hour, int? minute, string? resourceId)
        => HandleDropCoreAsync(targetDate, hour, minute, resourceId, applyResource: true);

    private Task HandleDropCoreAsync(DateTime targetDate, int? hour, int? minute, string? resourceId, bool applyResource)
    {
        var dragged = _state.DraggedEvent;
        if (dragged is null)
            return Task.CompletedTask;

        // Moving an occurrence moves only that occurrence: its series skips the date and the moved copy
        // becomes an event of its own, which takes two changes to report rather than one.
        if (dragged.IsOccurrence)
        {
            var changes = _state.BuildDropChanges(targetDate, hour, minute, resourceId, applyResource);
            _state.EndDrag();
            return CommitAsync(changes);
        }

        var oldSnapshot = CloneEvent(dragged);
        var eventId = dragged.Id;

        _state.HandleDrop(targetDate, hour, minute, resourceId, applyResource);

        var after = _state.AllEvents.FirstOrDefault(e => e.Id == eventId);
        if (after is null)
            return Task.CompletedTask;

        var sameTime = after.StartDate == oldSnapshot.StartDate && after.EndDate == oldSnapshot.EndDate;
        var sameResource = string.Equals(after.Resource ?? "", oldSnapshot.Resource ?? "", StringComparison.Ordinal);
        if (sameTime && sameResource)
            return Task.CompletedTask;

        return NotifyAsync(new BitFullCalendarChangeEventArgs
        {
            Event = CloneEvent(after),
            OldEvent = oldSnapshot,
            Kind = BitFullCalendarChangeKind.Edit,
            Source = BitFullCalendarChangeSource.Drag
        });
    }

    /// <summary>
    /// Creates a snapshot of a calendar event payload suitable for change args. Value-type fields, the
    /// <see cref="BitFullCalendarEvent.Attendees"/> collection, and the
    /// <see cref="BitFullCalendarEvent.Recurrence"/> rule are copied into fresh instances, but the
    /// consumer-defined <see cref="BitFullCalendarEvent.Data"/> payload is shared by reference (it is an
    /// opaque <c>object?</c> that cannot be generically cloned).
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
            Attendees = source.Attendees
                .Select(a => new BitFullCalendarAttendee
                {
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Id = a.Id
                })
                .ToList(),
            Recurrence = source.Recurrence?.Clone(),
            RecurringEventId = source.RecurringEventId,
            OccurrenceDate = source.OccurrenceDate
        };
}
