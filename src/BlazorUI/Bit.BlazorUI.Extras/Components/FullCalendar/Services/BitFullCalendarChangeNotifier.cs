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
        _state = state;
        _dispatch = dispatch;
    }

    /// <summary>
    /// Dispatches a change payload to the component's <c>OnChange</c> callback.
    /// </summary>
    public Task NotifyAsync(BitFullCalendarChangeEventArgs args) => _dispatch(args);

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
    /// Creates a deep snapshot of a calendar event payload suitable for change args.
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
                .ToList()
        };
}

