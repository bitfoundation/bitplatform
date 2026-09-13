namespace Bit.BlazorUI;

/// <summary>
/// Supplies a region of the items of a <see cref="BitBasicList{TItem}"/> on demand, which is what a list
/// whose items come from a server (or from anywhere else too large to hold at once) is rendered from.
/// </summary>
/// <typeparam name="TItem">The type of the items of the list.</typeparam>
/// <param name="request">The region being asked for, and the token that says it is no longer needed.</param>
public delegate ValueTask<BitBasicListItemsProviderResult<TItem>> BitBasicListItemsProvider<TItem>(
    BitBasicListItemsProviderRequest<TItem> request);
