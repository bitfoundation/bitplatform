namespace Bit.BlazorUI;

/// <summary>
/// A request to a <see cref="BitVirtualizeItemsProvider{TItem}"/> for a window of items.
/// </summary>
public readonly struct BitVirtualizeItemsProviderRequest
{
    /// <summary>
    /// Creates a new <see cref="BitVirtualizeItemsProviderRequest"/>.
    /// </summary>
    /// <param name="startIndex">The (inclusive) zero-based index of the first item requested.</param>
    /// <param name="count">The maximum number of items requested.</param>
    /// <param name="cancellationToken">A token that is cancelled when the request is superseded.</param>
    public BitVirtualizeItemsProviderRequest(int startIndex, int count, CancellationToken cancellationToken)
    {
        StartIndex = startIndex;
        Count = count;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// The zero-based index of the first item requested.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// The maximum number of items requested.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// A token that is cancelled when this request is no longer needed.
    /// </summary>
    public CancellationToken CancellationToken { get; }
}
