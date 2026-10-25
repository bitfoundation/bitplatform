namespace Bit.BlazorUI;

/// <summary>
/// A user-driven change the calendar is about to commit, passed to the OnChanging callback. Set
/// <see cref="Cancel"/> to refuse it: the calendar puts the event back the way it was and raises no OnChange.
/// </summary>
public sealed class BitFullCalendarChangingEventArgs
{
    /// <summary>
    /// The event as the change would leave it for Add/Edit, or the event about to be removed for Delete.
    /// </summary>
    public required BitFullCalendarEvent Event { get; init; }

    /// <summary>
    /// The kind of change.
    /// </summary>
    public required BitFullCalendarChangeKind Kind { get; init; }

    /// <summary>
    /// The event before the change for Edit/Delete. Null for Add.
    /// </summary>
    public BitFullCalendarEvent? OldEvent { get; init; }

    /// <summary>
    /// The UI source of the change: a dialog, a drag, or a resize (the keyboard move and resize report Drag and Resize).
    /// </summary>
    public required BitFullCalendarChangeSource Source { get; init; }

    /// <summary>
    /// Set to <c>true</c> to refuse the change.
    /// </summary>
    public bool Cancel { get; set; }
}
