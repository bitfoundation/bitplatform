namespace Bit.BlazorUI;

/// <summary>
/// The context passed to the IndexedItemTemplate of the BitVirtualize component: an item along with its position in the whole list.
/// </summary>
public readonly struct BitVirtualizeItemContext<TItem>
{
    /// <summary>
    /// Creates a new <see cref="BitVirtualizeItemContext{TItem}"/>.
    /// </summary>
    public BitVirtualizeItemContext(TItem item, int index)
    {
        Item = item;
        Index = index;
    }

    /// <summary>
    /// The item to render.
    /// </summary>
    public TItem Item { get; }

    /// <summary>
    /// The zero-based index of the item in the whole list (not in the rendered window).
    /// </summary>
    public int Index { get; }
}
