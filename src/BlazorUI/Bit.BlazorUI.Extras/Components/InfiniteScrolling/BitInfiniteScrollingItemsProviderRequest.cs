namespace Bit.BlazorUI;

/// <summary>
/// A request to a <see cref="BitInfiniteScrollingItemsProvider{T}"/> for the next page of items.
/// </summary>
public sealed class BitInfiniteScrollingItemsProviderRequest
{
    /// <summary>
    /// Creates a new <see cref="BitInfiniteScrollingItemsProviderRequest"/>.
    /// </summary>
    /// <param name="skip">The number of items already loaded, which is the index of the first item requested.</param>
    /// <param name="cancellationToken">A token that is cancelled when this request is no longer needed.</param>
    public BitInfiniteScrollingItemsProviderRequest(int skip, CancellationToken cancellationToken)
        : this(skip, 0, cancellationToken)
    {
    }

    /// <summary>
    /// Creates a new <see cref="BitInfiniteScrollingItemsProviderRequest"/>.
    /// </summary>
    /// <param name="skip">The number of items already loaded, which is the index of the first item requested.</param>
    /// <param name="count">The maximum number of items requested, or 0 when the page size is left to the provider.</param>
    /// <param name="cancellationToken">A token that is cancelled when this request is no longer needed.</param>
    public BitInfiniteScrollingItemsProviderRequest(int skip, int count, CancellationToken cancellationToken)
    {
        Skip = skip;
        Count = count;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// The number of items already loaded, which is the index of the first item requested.
    /// </summary>
    public int Skip { get; }

    /// <summary>
    /// The maximum number of items requested, which is the PageSize parameter of the component.
    /// <br />
    /// It is 0 when no page size is configured, in which case the page size is left to the provider.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// A token that is cancelled when this request is no longer needed, for example when the data gets
    /// refreshed or the component gets disposed while the request is still in flight.
    /// </summary>
    public CancellationToken CancellationToken { get; }
}
