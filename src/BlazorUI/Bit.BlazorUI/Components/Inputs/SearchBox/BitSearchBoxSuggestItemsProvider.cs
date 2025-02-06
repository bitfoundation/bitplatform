namespace Bit.BlazorUI;

public delegate ValueTask<IEnumerable<string>> BitSearchBoxSuggestItemsProvider(BitSearchBoxSuggestItemsProviderRequest context);
