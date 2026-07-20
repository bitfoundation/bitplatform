namespace Bit.BlazorUI.Legacy;

/// <summary>
/// A callback that provides data for a <see cref="BitDataGridLegacy{TGridItem}"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
/// <param name="request">Parameters describing the data being requested.</param>
/// <returns>A <see cref="ValueTask{TResult}"/> (specifically <c>ValueTask&lt;BitDataGridLegacyItemsProviderResult&lt;TGridItem&gt;&gt;</c>) whose result is a <see cref="BitDataGridLegacyItemsProviderResult{TGridItem}"/> that gives the data to be displayed.</returns>
public delegate ValueTask<BitDataGridLegacyItemsProviderResult<TGridItem>> BitDataGridItemsProvider<TGridItem>(BitDataGridLegacyItemsProviderRequest<TGridItem> request);
