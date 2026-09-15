namespace Bit.BlazorUI;

/// <summary>
/// The optional result of a <see cref="BitInfiniteScrollingItemsProvider{T}"/>, which lets a provider state
/// explicitly whether another page still exists and how many items the data source holds in total, instead of
/// letting the component infer the end from the size of the page it received.
/// </summary>
/// <remarks>
/// It is itself an <see cref="IEnumerable{T}"/> over the items of the page, so a provider can return one
/// wherever a plain sequence is expected and nothing else has to change.
/// </remarks>
/// <typeparam name="T">The item type.</typeparam>
public sealed class BitInfiniteScrollingItemsProviderResult<T> : IEnumerable<T>
{
    /// <summary>
    /// Creates a new <see cref="BitInfiniteScrollingItemsProviderResult{T}"/>.
    /// </summary>
    /// <param name="items">The items of the requested page.</param>
    /// <param name="hasMore">
    /// Whether another page can still be fetched. Leaving it null keeps the default behavior, where a page
    /// shorter than the requested count marks the end of the data.
    /// </param>
    /// <param name="totalCount">The total number of the items of the data source, when it is known.</param>
    public BitInfiniteScrollingItemsProviderResult(IEnumerable<T>? items, bool? hasMore = null, int? totalCount = null)
    {
        Items = items ?? [];
        HasMore = hasMore;
        TotalCount = totalCount;
    }

    /// <summary>
    /// The items of the requested page.
    /// </summary>
    public IEnumerable<T> Items { get; }

    /// <summary>
    /// Whether another page can still be fetched, or null to let the component infer it from the size of
    /// the page: a page shorter than the requested count is the last one.
    /// </summary>
    public bool? HasMore { get; }

    /// <summary>
    /// The total number of the items of the data source, when the provider knows it. The component exposes
    /// the last reported value through its TotalCount property.
    /// </summary>
    public int? TotalCount { get; }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
