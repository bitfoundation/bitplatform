namespace Bit.Bheadless;

/// <summary>
/// Information about an item moved from one <see cref="BitBhSortable{TItem}"/> to another list of its
/// <see cref="BitBhSortable{TItem}.Group"/> - enough to move it in a model the lists are not bound to.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
/// <param name="Item">The item.</param>
/// <param name="Key">The item's key: <see cref="BitBhSortable{TItem}.ItemKey"/>, else the item itself.</param>
/// <param name="SourceList">The list it came from: its <see cref="BitBhSortable{TItem}.ListKey"/>, else its id.</param>
/// <param name="OldIndex">Its position in the source list.</param>
/// <param name="TargetList">The list it moved to: its <see cref="BitBhSortable{TItem}.ListKey"/>, else its id.</param>
/// <param name="NewIndex">Its position in the target list.</param>
public sealed record BitBhTransferEventArgs<TItem>(TItem Item, object? Key, object SourceList, int OldIndex, object TargetList, int NewIndex);
