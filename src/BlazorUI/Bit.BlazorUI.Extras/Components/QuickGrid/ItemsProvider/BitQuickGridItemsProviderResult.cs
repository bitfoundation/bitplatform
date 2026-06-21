namespace Bit.BlazorUI;

/// <summary>
/// Holds data being supplied to a <see cref="BitQuickGrid{TGridItem}"/>'s <see cref="BitQuickGrid{TGridItem}.ItemsProvider"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
public struct BitQuickGridItemsProviderResult<TGridItem>
{
    /// <summary>
    /// The items being supplied.
    /// </summary>
    public ICollection<TGridItem> Items { get; set; }

    /// <summary>
    /// The total number of items that may be displayed in the grid. This normally means the total number of items in the
    /// underlying data source after applying any filtering that is in effect.
    ///
    /// If the grid is paginated, this should include all pages. If the grid is virtualized, this should include the entire scroll range.
    /// </summary>
    public int TotalItemCount { get; set; }

    /// <summary>
    /// Constructs an instance of <see cref="BitQuickGridItemsProviderResult{TGridItem}"/>.
    /// </summary>
    /// <param name="items">The items being supplied.</param>
    /// <param name="totalItemCount">The total number of items that exist. See <see cref="TotalItemCount"/> for details.</param>
    public BitQuickGridItemsProviderResult(ICollection<TGridItem> items, int totalItemCount)
    {
        Items = items;
        TotalItemCount = totalItemCount;
    }
}

/// <summary>
/// Provides convenience methods for constructing <see cref="BitQuickGridItemsProviderResult{TGridItem}"/> instances.
/// </summary>
public static class BitQuickGridItemsProviderResult
{
    // This is just to provide generic type inference, so you don't have to specify TGridItem yet again.

    /// <summary>
    /// Supplies an instance of <see cref="BitQuickGridItemsProviderResult{TGridItem}"/>.
    /// </summary>
    /// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
    /// <param name="items">The items being supplied.</param>
    /// <param name="totalItemCount">The total number of items that exist. See <see cref="BitQuickGridItemsProviderResult{TGridItem}.TotalItemCount"/> for details.</param>
    /// <returns>An instance of <see cref="BitQuickGridItemsProviderResult{TGridItem}"/>.</returns>
    public static BitQuickGridItemsProviderResult<TGridItem> From<TGridItem>(ICollection<TGridItem> items, int totalItemCount) => new(items, totalItemCount);
}
