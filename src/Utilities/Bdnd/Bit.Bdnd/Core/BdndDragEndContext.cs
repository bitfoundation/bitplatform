namespace Bit.Bdnd;

/// <summary>
/// Passed to <see cref="BdndDraggable{TItem}.OnEnd"/> when a drag operation finishes,
/// mirroring the arguments of React DnD's <c>end(item, monitor)</c> callback.
/// </summary>
/// <typeparam name="TItem">The type of the dragged item payload.</typeparam>
public sealed class BdndDragEndContext<TItem>
{
    /// <summary>
    /// The monitor of the provider the drag ran in. It is live: the drag state is cleared right after the
    /// synchronous part of <c>OnEnd</c>, so after an <c>await</c> it reports no drag; <see cref="Item"/>,
    /// <see cref="DidDrop"/> and <see cref="DropResult"/> keep the values of the drag that ended.
    /// </summary>
    public required IBdndMonitor Monitor { get; init; }

    /// <summary>The item that was being dragged.</summary>
    public TItem? Item { get; init; }

    /// <summary>True if a compatible drop target handled the drop.</summary>
    public bool DidDrop { get; init; }

    /// <summary>The drop result returned by the handling target, if any.</summary>
    public object? DropResult { get; init; }
}
