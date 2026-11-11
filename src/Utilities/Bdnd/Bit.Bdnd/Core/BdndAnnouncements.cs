using System.Collections;

namespace Bit.Bdnd;

/// <summary>
/// The texts Bdnd gives screen readers about a keyboard drag: the instructions every keyboard-draggable source is
/// described by, and the announcements a live region reads out as the drag is picked up, moved, dropped or
/// cancelled. Every text is replaceable - set the ones to translate or reword and keep the rest - and an
/// announcement that answers <c>null</c> or an empty string is not read out. Hand an instance to
/// <see cref="BdndProvider.Announcements"/> (or <see cref="BdndDragDropManager.Announcements"/>).
/// </summary>
public sealed class BdndAnnouncements
{
    /// <summary>The English texts every provider uses unless it is given others.</summary>
    public static BdndAnnouncements Default { get; } = new();

    /// <summary>
    /// How to drag with the keyboard. Every keyboard-draggable source (or its drag handle) references this text
    /// through <c>aria-describedby</c>, so a screen reader reads it when the source is focused.
    /// </summary>
    public string Instructions { get; init; } =
        "To pick up, press Space or Enter. While dragging, use the arrow keys to move between drop targets, " +
        "Home and End to go to the first or last one, Space or Enter to drop, and Escape to cancel.";

    /// <summary>The name of a source that has no <c>Label</c> and no text of its own.</summary>
    public string UnnamedSource { get; init; } = "item";

    /// <summary>The name of a drop target that has no <c>Label</c>: its place among the targets the item can be dropped on.</summary>
    public Func<int, int, string> UnnamedTarget { get; init; } =
        static (position, count) => $"drop target {position} of {count}";

    /// <summary>
    /// How many things a dragged item stands for (<see cref="BdndAnnouncement.ItemCount"/>): the number of elements of
    /// an item that is a collection (an <see cref="ICollection"/> - a list or an array of the selected cards, say), and
    /// one for any other item. Replace it for an item that counts otherwise - a selection object, or a collection that
    /// is one thing (<c>_ =&gt; 1</c>).
    /// </summary>
    public Func<object?, int> CountItems { get; init; } = static item => item is ICollection collection ? collection.Count : 1;

    /// <summary>Read when a keyboard drag begins.</summary>
    public Func<BdndAnnouncement, string?> DragStarted { get; init; } = static a =>
    {
        var picked = a.ItemCount > 1
            ? $"Picked up {a.SourceLabel} and {a.ItemCount - 1} other {(a.ItemCount == 2 ? "item" : "items")}."
            : $"Picked up {a.SourceLabel}.";
        return a.TargetCount switch
        {
            0 => $"{picked} There is no drop target for {(a.ItemCount > 1 ? "them" : "it")}; press Escape to cancel.",
            1 => $"{picked} 1 drop target is available.",
            var count => $"{picked} {count} drop targets are available.",
        };
    };

    /// <summary>Read when the keyboard moves the item onto another drop target, or to another edge of one.</summary>
    public Func<BdndAnnouncement, string?> DraggedOver { get; init; } = static a => a switch
    {
        { TargetLabel: null } => $"{Subject(a)} {Is(a)} not over a drop target.",
        { Before: true } => $"{Subject(a)} {Is(a)} before {a.TargetLabel}.",
        { Before: false } => $"{Subject(a)} {Is(a)} after {a.TargetLabel}.",
        _ => $"{Subject(a)} {Is(a)} over {a.TargetLabel}.",
    };

    /// <summary>Read when a keyboard move finds no further place in that direction; the drag stays where it is.</summary>
    public Func<BdndAnnouncement, string?> NoFurtherTarget { get; init; } = static a => a switch
    {
        { TargetLabel: null } => "There is no drop target in that direction.",
        { Before: true } => $"There is nothing further in that direction: {Subject(a)} {Stays(a)} before {a.TargetLabel}.",
        { Before: false } => $"There is nothing further in that direction: {Subject(a)} {Stays(a)} after {a.TargetLabel}.",
        _ => $"{a.TargetLabel} is the last drop target in that direction.",
    };

    /// <summary>Read when a keyboard drag ends with a drop on a target.</summary>
    public Func<BdndAnnouncement, string?> Dropped { get; init; } = static a => a.Before switch
    {
        true => $"{Subject(a)} {Was(a)} dropped before {a.TargetLabel}.",
        false => $"{Subject(a)} {Was(a)} dropped after {a.TargetLabel}.",
        _ => $"{Subject(a)} {Was(a)} dropped on {a.TargetLabel}.",
    };

    /// <summary>Read when a keyboard drag is cancelled, or dropped while it was over no target.</summary>
    public Func<BdndAnnouncement, string?> Cancelled { get; init; } = static a => $"Dragging {Subject(a)} was cancelled.";

    // what the English texts call the dragged item: its source's name, or how many things it carries
    private static string Subject(BdndAnnouncement a) => a.ItemCount > 1 ? $"{a.ItemCount} items" : a.SourceLabel;

    private static string Is(BdndAnnouncement a) => a.ItemCount > 1 ? "are" : "is";

    private static string Stays(BdndAnnouncement a) => a.ItemCount > 1 ? "stay" : "stays";

    private static string Was(BdndAnnouncement a) => a.ItemCount > 1 ? "were" : "was";
}

/// <summary>What an announcement of <see cref="BdndAnnouncements"/> is about.</summary>
public sealed class BdndAnnouncement
{
    /// <summary>
    /// The dragged source's name: its <c>Label</c>, else its <c>aria-label</c> or text, else
    /// <see cref="BdndAnnouncements.UnnamedSource"/>.
    /// </summary>
    public required string SourceLabel { get; init; }

    /// <summary>The name of the target the item is over (or was dropped on), or null when it is over none.</summary>
    public string? TargetLabel { get; init; }

    /// <summary>
    /// The edge of that target the item is placed against - <see cref="BdndEdge.Top"/> or <see cref="BdndEdge.Left"/>
    /// before it, <see cref="BdndEdge.Bottom"/> or <see cref="BdndEdge.Right"/> after it - or null when the target is
    /// one place (it lists no <see cref="BdndDropTarget{TItem}.Edges"/>).
    /// </summary>
    public BdndEdge? Edge { get; init; }

    /// <summary>
    /// Whether the item is placed before (true) or after (false) the target in its list's order - in a right-to-left
    /// list the right edge is before - or null when it is not placed against an edge.
    /// </summary>
    public bool? Before { get; init; }

    /// <summary>The 1-based place of that target among the targets the item can be dropped on; 0 when it is over none.</summary>
    public int TargetPosition { get; init; }

    /// <summary>How many targets the item can be dropped on.</summary>
    public int TargetCount { get; init; }

    /// <summary>The type of the dragged item.</summary>
    public required string ItemType { get; init; }

    /// <summary>The dragged item.</summary>
    public object? Item { get; init; }

    /// <summary>
    /// How many things the dragged item stands for, as <see cref="BdndAnnouncements.CountItems"/> counts them: more
    /// than one for a drag of several selected items, whose texts say "3 items" rather than the grabbed source's name.
    /// </summary>
    public int ItemCount { get; init; } = 1;
}
