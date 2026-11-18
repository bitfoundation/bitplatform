namespace Bit.BlazorUI;

/// <summary>
/// Identifies the kind of change applied to a calendar event.
/// </summary>
public enum BitFullCalendarChangeKind
{
    /// <summary>A new event: one saved in the add dialog, or an occurrence of a recurring event that became an event of
    /// its own.</summary>
    Add,

    /// <summary>An existing event changed: edited in its dialog, moved or resized.</summary>
    Edit,

    /// <summary>An event was deleted.</summary>
    Delete
}

/// <summary>
/// Identifies where a calendar event change originated from in the UI.
/// This describes the interaction origin only; the action itself (add/edit/delete)
/// is carried by <see cref="BitFullCalendarChangeKind"/>.
/// </summary>
public enum BitFullCalendarChangeSource
{
    /// <summary>The add/edit dialog or the details dialog.</summary>
    Dialog,

    /// <summary>The event was moved: dragged, or moved from the keyboard.</summary>
    Drag,

    /// <summary>The event was resized: by its edge, or from the keyboard.</summary>
    Resize
}

/// <summary>
/// Provides details about a user-applied calendar event change.
/// </summary>
public sealed class BitFullCalendarChangeEventArgs
{
    /// <summary>
    /// The current event snapshot after the change for Add/Edit,
    /// or the removed event snapshot for Delete.
    /// </summary>
    public required BitFullCalendarEvent Event { get; init; }

    /// <summary>
    /// The change type that occurred.
    /// </summary>
    public required BitFullCalendarChangeKind Kind { get; init; }

    /// <summary>
    /// The event snapshot before the change for Edit/Delete.
    /// Null for Add.
    /// </summary>
    public BitFullCalendarEvent? OldEvent { get; init; }

    /// <summary>
    /// The UI source that triggered this change.
    /// </summary>
    public required BitFullCalendarChangeSource Source { get; init; }
}

