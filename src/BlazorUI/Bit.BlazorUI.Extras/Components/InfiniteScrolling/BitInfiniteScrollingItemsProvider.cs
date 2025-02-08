namespace Bit.BlazorUI;

public delegate ValueTask<IEnumerable<T>> BitInfiniteScrollingItemsProvider<T>(BitInfiniteScrollingItemsProviderRequest context);
