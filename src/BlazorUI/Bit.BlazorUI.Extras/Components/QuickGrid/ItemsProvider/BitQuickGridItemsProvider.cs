namespace Bit.BlazorUI;

/// <summary>
/// A callback that provides data for a <see cref="BitQuickGrid{TGridItem}"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
/// <param name="request">Parameters describing the data being requested.</param>
/// <returns>A <see cref="ValueTask{TResult}"/> (specifically <c>ValueTask&lt;BitQuickGridItemsProviderResult&lt;TGridItem&gt;&gt;</c>) whose result is a <see cref="BitQuickGridItemsProviderResult{TGridItem}"/> that gives the data to be displayed.</returns>
public delegate ValueTask<BitQuickGridItemsProviderResult<TGridItem>> BitQuickGridItemsProvider<TGridItem>(BitQuickGridItemsProviderRequest<TGridItem> request);
