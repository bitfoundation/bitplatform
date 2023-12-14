namespace Bit.BlazorUI;

public delegate ValueTask<BitSearchBoxItemsProviderResult<TItem>> BitSearchBoxItemsProvider<TItem>(BitSearchBoxItemsProviderRequest<TItem> request);
