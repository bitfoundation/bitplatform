namespace Bit.Bmotion;

/// <summary>
/// One keyboard reorder step, for <see cref="BmotionReorderGroup{TItem}.Announce"/> to put into words for screen
/// readers. <paramref name="Position"/> is 1-based, out of <paramref name="Count"/> items.
/// </summary>
public sealed record BmotionReorderAnnouncement<TItem>(BmotionReorderEvent Event, TItem Item, int Position, int Count)
{
    /// <summary>The library's own English wording, which <see cref="BmotionReorderGroup{TItem}.Announce"/> replaces.</summary>
    public string DefaultText => Event switch
    {
        BmotionReorderEvent.PickedUp => $"Picked up, position {Position} of {Count}. Arrow keys move it, Space drops it, Escape cancels.",
        BmotionReorderEvent.Moved => $"Position {Position} of {Count}.",
        BmotionReorderEvent.Dropped => $"Dropped at position {Position} of {Count}.",
        _ => $"Cancelled, back at position {Position} of {Count}.",
    };
}
