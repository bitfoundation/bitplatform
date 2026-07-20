namespace Bit.BlazorUI;

/// <summary>
/// The result returned from a <see cref="BitVirtualizeItemsProvider{TItem}"/>.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
public readonly struct BitVirtualizeItemsProviderResult<TItem>
{
    /// <summary>
    /// Creates a new <see cref="BitVirtualizeItemsProviderResult{TItem}"/>.
    /// </summary>
    /// <param name="items">The items for the requested window.</param>
    /// <param name="totalItemCount">The total number of items in the underlying data source.</param>
    public BitVirtualizeItemsProviderResult(IReadOnlyList<TItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }

    /// <summary>
    /// The items that were loaded for the requested window.
    /// </summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>
    /// The total number of items in the underlying data source.
    /// </summary>
    public int TotalItemCount { get; }
}
