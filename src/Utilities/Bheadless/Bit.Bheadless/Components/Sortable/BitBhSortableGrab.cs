namespace Bit.Bheadless;

/// <summary>An item picked up with the keyboard: where it came from and where it is now.</summary>
/// <typeparam name="TItem">The item type.</typeparam>
internal sealed class BitBhSortableGrab<TItem>(BitBhSortable<TItem> origin, int originIndex, TItem item)
{
    /// <summary>The list the item was picked up from.</summary>
    public BitBhSortable<TItem> Origin { get; } = origin;

    /// <summary>The item's position in <see cref="Origin"/> when it was picked up.</summary>
    public int OriginIndex { get; } = originIndex;

    /// <summary>The item.</summary>
    public TItem Item { get; } = item;

    /// <summary>The list that shows the item now.</summary>
    public BitBhSortable<TItem> Current { get; set; } = origin;

    /// <summary>The item's position in <see cref="Current"/>'s preview.</summary>
    public int Index { get; set; } = originIndex;
}
