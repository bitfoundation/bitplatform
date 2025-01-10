namespace Bit.BlazorUI;

public delegate Task<IEnumerable<T>> BitInfiniteScrollingItemsProviderRequestDelegate<T>(BitInfiniteScrollingItemsProviderRequest context);
