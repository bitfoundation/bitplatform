namespace Bit.Bdnd;

/// <summary>
/// A drag of a provider beginning or ending, as <see cref="BdndProvider.OnDragStart"/>,
/// <see cref="BdndProvider.OnDragEnd"/> and the manager's <see cref="BdndDragDropManager.DragStarted"/> and
/// <see cref="BdndDragDropManager.DragEnded"/> report it - for what watches every drag of a context rather than one
/// source: analytics, an undo history, a page-wide "dragging" state.
/// </summary>
public sealed class BdndDragEventArgs
{
    /// <summary>The dragged item.</summary>
    public object? Item { get; init; }

    /// <summary>The dragged item's type.</summary>
    public required string ItemType { get; init; }

    /// <summary>How the drag is (or was) made.</summary>
    public BdndDragInput Input { get; init; }

    /// <summary>On the end: whether a target took the drop. Always false on the start.</summary>
    public bool DidDrop { get; init; }

    /// <summary>On the end: the result the targets that took the drop published, if any.</summary>
    public object? DropResult { get; init; }

    /// <summary>The provider's monitor. Live: on the end it no longer reports the drag that ended.</summary>
    public required IBdndMonitor Monitor { get; init; }
}
