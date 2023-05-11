namespace Bit.BlazorUI;

public delegate ValueTask<BitDropdownItemsProviderResult<TDropdownItem>> BitDropdownItemsProvider<TDropdownItem>(
    BitDropdownItemsProviderRequest<TDropdownItem> request);
