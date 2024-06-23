namespace Bit.BlazorUI;

/// <summary>
/// A callback that provides data for a <see cref="BitDataGrid{TGridItem}"/>.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
/// <param name="request">Parameters describing the data being requested.</param>
/// <returns>A <see cref="ValueTask{BitDataGridItemsProviderResult{TResult}}" /> that gives the data to be displayed.</returns>
public delegate ValueTask<BitDataGridItemsProviderResult<TGridItem>> BitDataGridItemsProvider<TGridItem>(
    BitDataGridItemsProviderRequest<TGridItem> request);
