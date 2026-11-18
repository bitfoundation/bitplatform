namespace Bit.Bmotion;

/// <summary>What a keyboard user just did to an item of a <see cref="BmotionReorderGroup{TItem}"/>.</summary>
public enum BmotionReorderEvent
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
