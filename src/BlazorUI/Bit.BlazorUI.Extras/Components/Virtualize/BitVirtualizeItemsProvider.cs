namespace Bit.BlazorUI;

/// <summary>
/// The item provider function of the BitVirtualize component that asynchronously supplies a window of items on demand.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
/// <param name="request">The window of items being requested.</param>
/// <returns>A task that resolves to the requested items and the total item count.</returns>
public delegate ValueTask<BitVirtualizeItemsProviderResult<TItem>> BitVirtualizeItemsProvider<TItem>(BitVirtualizeItemsProviderRequest request);
