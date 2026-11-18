namespace Bit.Bdnd;

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
