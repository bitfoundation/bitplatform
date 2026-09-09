namespace Bit.BlazorUI;

/// <summary>
/// The function a <see cref="BitInfiniteScrolling{TItem}"/> calls for each page of items.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="context">
/// The request, carrying the number of the items already loaded, the size of the page that is asked for, and
/// the token that is cancelled when the request is no longer needed.
/// </param>
/// <returns>
/// The items of the requested page. An empty result (or one shorter than the requested count) marks the end of
/// the data, after which no further page is requested.
/// </returns>
public delegate ValueTask<IEnumerable<T>> BitInfiniteScrollingItemsProvider<T>(BitInfiniteScrollingItemsProviderRequest context);
