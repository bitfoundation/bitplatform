namespace Bit.Bink;

/// <summary>Moves a layer to a new index in the layer stack.</summary>
/// <param name="LayerId">The layer.</param>
/// <param name="Index">New index (0 = bottom).</param>
public sealed record BinkReorderLayerOperation(Guid LayerId, int Index) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Reorder layer";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.TryGetLayer(LayerId, out _);

    /// <summary>The order key the layer moves to, which places it when given; otherwise <see cref="Index"/> does.</summary>
    public string? OrderKey { get; init; }

    /// <summary>The Lamport clock of the move, or 0 for a new move; concurrent moves resolve as <see cref="BinkReorderElementOperation.OrderClock"/> does.</summary>
    public long OrderClock { get; init; }

    /// <summary>Set for a snapshot's moves, which always apply.</summary>
    internal bool Authoritative { get; init; }

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        var moved = document.ReorderLayerCore(LayerId, Index, OrderKey, OrderClock, Authoritative);
        applied = moved.Key == OrderKey && moved.Clock == OrderClock ? this : this with { OrderKey = moved.Key, OrderClock = moved.Clock };
        return new BinkReorderLayerOperation(LayerId, moved.PreviousIndex) { OrderKey = moved.PreviousKey, OrderClock = moved.PreviousClock };
    }
}
