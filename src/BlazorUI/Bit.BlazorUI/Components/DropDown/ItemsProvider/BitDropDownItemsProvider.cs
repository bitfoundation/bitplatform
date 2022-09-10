namespace Bit.BlazorUI;

public delegate ValueTask<BitDropDownItemsProviderResult<TDropDownItem>> BitDropDownItemsProvider<TDropDownItem>(
    BitDropDownItemsProviderRequest<TDropDownItem> request);
