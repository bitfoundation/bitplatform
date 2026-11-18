namespace Bit.Bortable;

/// <summary>What a list answers the browser layer when a drag of one of its items starts.</summary>
internal sealed class BortableStartResult
{
    /// <summary>
    /// The ids of the lists that take the drag: the list the item is in, and every list that could receive it (or, in
    /// swap mode, trade an item of its own for it). The browser layer routes the pointer to these lists only.
    /// </summary>
    public required string[] Lists { get; init; }

    /// <summary>What to announce to assistive technology.</summary>
    public string? Announce { get; init; }

    /// <summary>The drag copies the item into another list (a clone pull): the pointer shows a copy.</summary>
    public bool Clone { get; init; }

    /// <summary>The drag's id, which the browser layer sends back with every call about it.</summary>
    public int Drag { get; init; }
}
