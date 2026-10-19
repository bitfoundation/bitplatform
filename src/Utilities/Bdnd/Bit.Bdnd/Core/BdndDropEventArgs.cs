namespace Bit.Bdnd;

/// <summary>
/// Passed to <see cref="BdndDropTarget{TItem}.OnDrop"/> and <see cref="BdndDropTarget{TItem}.OnHover"/>,
/// mirroring React DnD's <c>drop(item, monitor)</c> / <c>hover(item, monitor)</c> callbacks.
/// </summary>
/// <typeparam name="TItem">The expected dragged item type.</typeparam>
public sealed class BdndDropEventArgs<TItem>
{
    /// <summary>The monitor of the provider the drag runs in.</summary>
    public required IBdndMonitor Monitor { get; init; }

    /// <summary>The item currently being dragged.</summary>
    public TItem? Item { get; init; }

    /// <summary>
    /// Set this inside an <c>OnDrop</c> handler to publish a drop result. It becomes available
    /// to the drag source via <c>monitor.GetDropResult()</c> in its <c>OnEnd</c> callback. An async
    /// handler may set it after an <c>await</c>: the drag ends - and <c>OnEnd</c> runs - once the async
    /// <c>OnDrop</c> handlers of the drop have completed.
    /// </summary>
    public object? Result { get; set; }
}
