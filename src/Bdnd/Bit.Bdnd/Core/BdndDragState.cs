namespace Bit.Bdnd;

/// <summary>
/// State surfaced to a <see cref="BdndDraggable{TItem}"/>'s child content, equivalent to the
/// "collected props" returned by React DnD's <c>useDrag</c>.
/// </summary>
public sealed class BdndDragState
{
    /// <summary>The monitor of the provider the draggable is in.</summary>
    public required IBdndMonitor Monitor { get; init; }

    /// <summary>True while this source is being dragged.</summary>
    public bool IsDragging { get; init; }

    /// <summary>True if dragging is currently allowed for this source.</summary>
    public bool CanDrag { get; init; }
}
