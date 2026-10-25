namespace Bit.Bdnd;

/// <summary>
/// A read-only view over the drag-and-drop state, mirroring the union of React DnD's
/// <c>DragSourceMonitor</c>, <c>DropTargetMonitor</c> and <c>DragLayerMonitor</c>.
/// A single monitor instance is shared through the <see cref="BdndDragDropManager"/> and
/// passed into the collecting/lifecycle callbacks of draggables, drop targets and layers.
/// </summary>
public interface IBdndMonitor
{
    /// <summary>True if a drag operation is currently in progress.</summary>
    bool IsDragging();

    /// <summary>
    /// True if the given source is the one being dragged. When <paramref name="sourceId"/>
    /// is null this is equivalent to <see cref="IsDragging()"/>.
    /// </summary>
    bool IsDragging(string? sourceId);

    /// <summary>
    /// How the drag in progress is being made - the browser's native drag and drop or the keyboard - or null when
    /// nothing is being dragged. A drag layer can, for one, skip its preview for keyboard drags.
    /// </summary>
    BdndDragInput? GetDragInput() => IsDragging() ? BdndDragInput.Native : null;

    /// <summary>Returns the type string of the item currently being dragged, or null.</summary>
    string? GetItemType();

    /// <summary>Returns the plain item object currently being dragged, or null.</summary>
    object? GetItem();

    /// <summary>Strongly-typed convenience accessor for <see cref="GetItem()"/>.</summary>
    T? GetItem<T>();

    /// <summary>Returns the last recorded drop result, or null.</summary>
    object? GetDropResult();

    /// <summary>Strongly-typed convenience accessor for <see cref="GetDropResult()"/>.</summary>
    T? GetDropResult<T>();

    /// <summary>True if a compatible drop target has handled the drop.</summary>
    bool DidDrop();

    /// <summary>True if the given handler can currently accept a drop.</summary>
    bool CanDrop(string handlerId);

    /// <summary>True if a drag is in progress and the pointer is over the given handler.</summary>
    bool IsOver(string handlerId, bool shallow = false);

    /// <summary>The pointer client offset at the time the drag started.</summary>
    BdndXYCoord? GetInitialClientOffset();

    /// <summary>The source element's top-left client offset at the time the drag started.</summary>
    BdndXYCoord? GetInitialSourceClientOffset();

    /// <summary>The last recorded pointer client offset during the drag.</summary>
    BdndXYCoord? GetClientOffset();

    /// <summary>The delta between the current and initial pointer client offsets.</summary>
    BdndXYCoord? GetDifferenceFromInitialOffset();

    /// <summary>The projected top-left client offset of the source element.</summary>
    BdndXYCoord? GetSourceClientOffset();
}
