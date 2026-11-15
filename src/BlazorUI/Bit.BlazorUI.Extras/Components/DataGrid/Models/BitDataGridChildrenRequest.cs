namespace Bit.BlazorUI;

/// <summary>
/// What a lazily-loaded tree grid asks of its <c>OnLoadChildren</c> callback: the children of one node, the first time
/// that node is expanded.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class BitDataGridChildrenRequest<TItem>
{
    /// <summary>The node whose direct children are requested.</summary>
    public required TItem Parent { get; init; }

    /// <summary>
    /// Cancelled once the grid no longer needs these children: when a new <c>Items</c> reference replaces the hierarchy
    /// the node belongs to, or when the grid is disposed. Pass it on to the call that fetches them so that call stops
    /// early. Collapsing the node does not cancel it: the children still load and are kept for the next expand.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}
