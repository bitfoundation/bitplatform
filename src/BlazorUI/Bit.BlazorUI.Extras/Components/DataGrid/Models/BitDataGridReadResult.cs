namespace Bit.BlazorUI;

/// <summary>
/// Result returned from a grid's <c>OnRead</c> (server mode) or <c>OnLoadMore</c>
/// (infinite-scrolling mode) callback.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class BitDataGridReadResult<TItem>
{
    public BitDataGridReadResult(IReadOnlyList<TItem> items, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCount), totalCount, "Total count must be greater than or equal to zero.");
        // A totalCount of 0 is the infinite-scrolling (OnLoadMore) sentinel: those batches intentionally
        // don't report a grand total, so a non-empty batch paired with totalCount 0 is valid and must be
        // accepted. For any finite (positive) total, a single page can never hold more items than the
        // reported grand total. Rejecting that here keeps an inconsistent OnRead provider from feeding
        // BitDataGrid a _pageItems/_totalCount pair where the page is larger than the total (which would
        // break paging math and the displayed counts).
        if (totalCount > 0 && items.Count > totalCount)
            throw new ArgumentOutOfRangeException(nameof(totalCount), totalCount,
                $"Total count ({totalCount}) must be greater than or equal to the number of items in the result ({items.Count}).");

        Items = items;
        TotalCount = totalCount;
    }

    /// <summary>The items for the current page/window.</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>
    /// The total number of items matching the current filters (across all pages) in <c>OnRead</c> mode.
    /// In <c>OnLoadMore</c> (infinite-scrolling) mode the grand total is typically unknown, so <c>0</c> is
    /// used as the "unknown total" sentinel: a non-empty batch paired with a <see cref="TotalCount"/> of
    /// <c>0</c> is valid and simply means "keep loading until a batch comes back short".
    /// </summary>
    public int TotalCount { get; }
}
