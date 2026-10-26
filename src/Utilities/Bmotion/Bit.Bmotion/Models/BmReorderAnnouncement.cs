namespace Bit.Bmotion;

/// <summary>What a keyboard user just did to an item of a <see cref="BmotionReorderGroup{TItem}"/>.</summary>
public enum BmReorderEvent
{
    /// <summary>Picked the item up (Space or Enter): the arrow keys now move it.</summary>
    PickedUp,

    /// <summary>Moved the held item one place.</summary>
    Moved,

    /// <summary>Dropped the item (Space, Enter, or moving focus away): the new order is committed.</summary>
    Dropped,

    /// <summary>Put the item back where it was picked up from (Escape).</summary>
    Cancelled,
}

/// <summary>
/// One keyboard reorder step, for <see cref="BmotionReorderGroup{TItem}.Announce"/> to put into words for screen
/// readers. <paramref name="Position"/> is 1-based, out of <paramref name="Count"/> items.
/// </summary>
public sealed record BmReorderAnnouncement<TItem>(BmReorderEvent Event, TItem Item, int Position, int Count)
{
    /// <summary>The library's own English wording, which <see cref="BmotionReorderGroup{TItem}.Announce"/> replaces.</summary>
    public string DefaultText => Event switch
    {
        BmReorderEvent.PickedUp => $"Picked up, position {Position} of {Count}. Arrow keys move it, Space drops it, Escape cancels.",
        BmReorderEvent.Moved => $"Position {Position} of {Count}.",
        BmReorderEvent.Dropped => $"Dropped at position {Position} of {Count}.",
        _ => $"Cancelled, back at position {Position} of {Count}.",
    };
}
