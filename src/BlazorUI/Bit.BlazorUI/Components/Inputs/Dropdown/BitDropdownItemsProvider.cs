namespace Bit.BlazorUI;

public delegate ValueTask<BitDropdownItemsProviderResult<TItem>> BitDropdownItemsProvider<TItem>(BitDropdownItemsProviderRequest<TItem> request);
