namespace Bit.Bheadless;

/// <summary>Customizable screen-reader announcements for <see cref="BitBhSortable{TItem}"/>.</summary>
public sealed class BitBhSortableAnnouncements
{
    /// <summary>Instructions referenced by every drag handle (<c>aria-describedby</c>).</summary>
    public string Instructions { get; set; } =
        "To pick up an item, press Space or Enter. While dragging, use the arrow keys to move it. Press Space or Enter again to drop it, or Escape to cancel.";

    /// <summary>(label, position, count) → message.</summary>
    public Func<string, int, int, string> PickedUp { get; set; } =
        (label, position, count) => $"Picked up {label}. Position {position} of {count}.";

    /// <summary>(label, position, count) → message.</summary>
    public Func<string, int, int, string> Moved { get; set; } =
        (label, position, count) => $"{label} moved to position {position} of {count}.";

    /// <summary>(label, position, count) → message.</summary>
    public Func<string, int, int, string> Dropped { get; set; } =
        (label, position, count) => $"Dropped {label} at position {position} of {count}.";

    /// <summary>(label, position) → message.</summary>
    public Func<string, int, string> Cancelled { get; set; } =
        (label, position) => $"Reordering cancelled. {label} returned to position {position}.";

    /// <summary>
    /// Instructions referenced by every drag handle of a list in a <see cref="BitBhSortable{TItem}.Group"/>, used
    /// instead of <see cref="Instructions"/>: they also say how to move an item to another list. Default:
    /// <see cref="BitBhStrings.SortableGroupInstructions"/>.
    /// </summary>
    public string? GroupInstructions { get; set; }

    /// <summary>
    /// (label, list, position, count) → message when a picked-up item moves to another list. Default:
    /// <see cref="BitBhStrings.SortableMovedToList"/>.
    /// </summary>
    public Func<string, string, int, int, string>? MovedToList { get; set; }

    /// <summary>
    /// (label, list, position, count) → message when an item is dropped in another list. Default:
    /// <see cref="BitBhStrings.SortableDroppedInList"/>.
    /// </summary>
    public Func<string, string, int, int, string>? DroppedInList { get; set; }

    /// <summary>
    /// (label, list, position) → message when a move that had taken an item to another list is cancelled. Default:
    /// <see cref="BitBhStrings.SortableReturnedToList"/>.
    /// </summary>
    public Func<string, string, int, string>? ReturnedToList { get; set; }
}
