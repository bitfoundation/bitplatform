namespace Bit.BlazorUI;

public delegate ValueTask<BitSearchBoxSuggestedItemsProviderResult<TItem>> BitSearchBoxSuggestedItemsProvider<TItem>(BitSearchBoxSuggestedItemsProviderRequest<TItem> request);
