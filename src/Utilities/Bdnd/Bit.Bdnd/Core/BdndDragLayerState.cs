namespace Bit.Bdnd;

/// <summary>
/// State surfaced to a <see cref="BdndDragLayer{TItem}"/>'s child content, equivalent to the
/// values exposed by React DnD's <c>useDragLayer</c> / <c>DragLayerMonitor</c>.
/// </summary>
/// <typeparam name="TItem">The type of the dragged item payload.</typeparam>
public sealed class BdndDragLayerState<TItem>
{
    /// <summary>The monitor of the provider the layer is in.</summary>
    public required IBdndMonitor Monitor { get; init; }

    /// <summary>
    /// True while a drag of the provider is in progress - one of the types the layer draws, when it has
    /// <c>Accept</c> or <c>AcceptTypes</c>.
    /// </summary>
    public bool IsDragging { get; init; }

    /// <summary>The type of the item being dragged, or null.</summary>
    public string? ItemType { get; init; }

    /// <summary>The item being dragged (if assignable to <typeparamref name="TItem"/>).</summary>
    public TItem? Item { get; init; }

    /// <summary>The pointer client offset at the time the drag started.</summary>
    public BdndXYCoord? InitialClientOffset { get; init; }

    /// <summary>The source element's top-left client offset at the time the drag started.</summary>
    public BdndXYCoord? InitialSourceClientOffset { get; init; }

    /// <summary>The last recorded pointer client offset.</summary>
    public BdndXYCoord? ClientOffset { get; init; }

    /// <summary>The delta between the current and initial pointer client offsets.</summary>
    public BdndXYCoord? DifferenceFromInitialOffset { get; init; }

    /// <summary>The projected top-left client offset of the source element: where to draw the preview.</summary>
    public BdndXYCoord? SourceClientOffset { get; init; }

    /// <summary>
    /// <see cref="SourceClientOffset"/> kept to <paramref name="axis"/>: a preview drawn there moves only up and down
    /// (<see cref="BdndAxis.Vertical"/>) or sideways, from where the source was. Null while it is.
    /// </summary>
    public BdndXYCoord? SourceClientOffsetOn(BdndAxis axis) =>
        SourceClientOffset is { } offset && InitialSourceClientOffset is { } origin ? offset.KeepTo(axis, origin) : SourceClientOffset;
}
