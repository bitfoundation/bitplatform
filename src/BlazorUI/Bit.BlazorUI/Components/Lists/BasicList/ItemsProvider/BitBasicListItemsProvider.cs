namespace Bit.BlazorUI;

public delegate ValueTask<BitBasicListItemsProviderResult<TItem>> BitBasicListItemsProvider<TItem>(
    BitBasicListItemsProviderRequest<TItem> request);
