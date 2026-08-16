namespace Bit.BlazorUI;

/// <summary>
/// The answer of an items provider of a <see cref="BitBasicList{TItem}"/> to a request for a region of its items.
/// </summary>
/// <typeparam name="TItem">The type of the items of the list.</typeparam>
public struct BitBasicListItemsProviderResult<TItem>
{
    /// <summary>
    /// The items of the requested region.
    /// </summary>
    public ICollection<TItem> Items { get; set; }

    /// <summary>
    /// The total number of items of the whole set, which is what a virtualized list sizes its scrollbar by.
    /// </summary>
    /// <remarks>
    /// The LoadMore mode does not read this value: it recognizes the last page by it being shorter than the
    /// page that was asked for, which saves counting the whole set on every page.
    /// </remarks>
    public int TotalItemCount { get; set; }

    public BitBasicListItemsProviderResult(ICollection<TItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}

/// <summary>
/// Creates <see cref="BitBasicListItemsProviderResult{TItem}"/> instances with the item type inferred from the items.
/// </summary>
public static class BitBasicListItemsProviderResult
{
    /// <summary>
    /// Creates a result of the given items and total count.
    /// </summary>
    public static BitBasicListItemsProviderResult<TItem> From<TItem>(ICollection<TItem> items, int totalItemCount)
        => new BitBasicListItemsProviderResult<TItem>(items, totalItemCount);

    /// <summary>
    /// Creates a result of the given items, taking the total count to be the number of items supplied.
    /// </summary>
    /// <remarks>
    /// This suits the LoadMore mode, which never reads the total count, and a provider that has the whole set
    /// on hand. A virtualized list sizes its scroll region by the total count, so it needs the count of the
    /// whole set rather than of the page.
    /// </remarks>
    public static BitBasicListItemsProviderResult<TItem> From<TItem>(ICollection<TItem> items)
        => new BitBasicListItemsProviderResult<TItem>(items, items?.Count ?? 0);

    /// <summary>
    /// Creates an empty result.
    /// </summary>
    public static BitBasicListItemsProviderResult<TItem> Empty<TItem>()
        => new BitBasicListItemsProviderResult<TItem>([], 0);
}
