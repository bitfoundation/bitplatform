namespace Bit.Bink;

/// <summary>Moves an element to a new z-order index and/or a different layer.</summary>
/// <param name="ElementId">The element.</param>
/// <param name="TargetLayerId">Destination layer.</param>
/// <param name="Index">Destination index within the layer (clamped), or null for the top.</param>
public sealed record BinkReorderElementOperation(Guid ElementId, Guid TargetLayerId, int? Index) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Reorder";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.ContainsElement(ElementId) && document.TryGetLayer(TargetLayerId, out _);

    /// <summary>
    /// The order key the element moves to, which places it when given; otherwise <see cref="Index"/> does and the
    /// document makes the key. See <see cref="BinkOrderKey"/>.
    /// </summary>
    public string? OrderKey { get; init; }

    /// <summary>
    /// The Lamport clock of the move, or 0 for a new move here. When two collaborators move one element at once, the
    /// move with the greater clock (then key, then layer id) wins on every replica, so they end in the same order.
    /// </summary>
    public long OrderClock { get; init; }

    /// <summary>Set for a snapshot's moves, which apply whatever clock the element was last moved with.</summary>
    internal bool Authoritative { get; init; }

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        var moved = document.ReorderElementCore(ElementId, TargetLayerId, Index, OrderKey, OrderClock, Authoritative);
        applied = moved.Key == OrderKey && moved.Clock == OrderClock ? this : this with { OrderKey = moved.Key, OrderClock = moved.Clock };
        return new BinkReorderElementOperation(ElementId, moved.PreviousLayerId, moved.PreviousIndex) { OrderKey = moved.PreviousKey, OrderClock = moved.PreviousClock };
    }
}
