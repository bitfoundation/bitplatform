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

/// <summary>What a list answers the browser layer for a key pressed on one of its items, or during a keyboard drag.</summary>
internal sealed class BortableKeyResult
{
    /// <summary>Whether a keyboard drag is in progress after the key: picked up and not yet dropped or cancelled.</summary>
    public bool Dragging { get; init; }

    /// <summary>What to announce to assistive technology.</summary>
    public string? Announce { get; init; }

    /// <summary>The id of the list the dragged item is in now, where focus follows it.</summary>
    public string? List { get; init; }

    /// <summary>The key the dragged item is rendered with in that list.</summary>
    public string? Key { get; init; }

    /// <summary>When a drag starts: the ids of the lists that take it.</summary>
    public string[]? Lists { get; init; }

    /// <summary>In swap mode: the key of the item highlighted as the swap target, if any.</summary>
    public string? Target { get; init; }

    /// <summary>When a drag starts: its id, which the browser layer sends back with every key of it.</summary>
    public int Drag { get; init; }
}
