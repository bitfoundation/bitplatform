namespace Bit.Bdnd;

/// <summary>
/// State surfaced to a <see cref="BdndDropTarget{TItem}"/>'s child content, equivalent to the
/// "collected props" returned by React DnD's <c>useDrop</c>.
/// </summary>
/// <typeparam name="TItem">The expected dragged item type.</typeparam>
public sealed class BdndDropState<TItem>
{
    /// <summary>The monitor of the provider the drop target is in.</summary>
    public required IBdndMonitor Monitor { get; init; }

    /// <summary>True if a drag is in progress and the pointer is over this target (or a child).</summary>
    public bool IsOver { get; init; }

    /// <summary>True if the pointer is over this target but not a nested target.</summary>
    public bool IsOverShallow { get; init; }

    /// <summary>True if this target can accept the item currently being dragged.</summary>
    public bool CanDrop { get; init; }

    /// <summary>The type of the item currently being dragged, or null.</summary>
    public string? ItemType { get; init; }

    /// <summary>The item currently being dragged (if assignable to <typeparamref name="TItem"/>).</summary>
    public TItem? Item { get; init; }

    /// <summary>
    /// The edge of the target the drag is closest to while it is over the target - one of the
    /// <see cref="BdndDropTarget{TItem}.Edges"/> the target lists - or null (no edges, or not over it). What a drop
    /// indicator is drawn from: a line above the item for <see cref="BdndEdge.Top"/>, below it for
    /// <see cref="BdndEdge.Bottom"/>.
    /// </summary>
    public BdndEdge? ClosestEdge { get; init; }
}
