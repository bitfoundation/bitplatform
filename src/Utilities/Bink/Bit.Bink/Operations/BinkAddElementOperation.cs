namespace Bit.Bink;

/// <summary>Adds an element to a layer.</summary>
/// <param name="LayerId">Target layer.</param>
/// <param name="Element">The element to add. Its id must not already exist in the document.</param>
/// <param name="Index">Insertion index within the layer, or null to append on top.</param>
public sealed record BinkAddElementOperation(Guid LayerId, BinkElement Element, int? Index = null) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Add " + Element.TypeName.ToLowerInvariant();

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.TryGetLayer(LayerId, out _) && !document.ContainsElement(Element.Id);

    /// <summary>
    /// The element's order key, which places it in the layer when given; otherwise <see cref="Index"/> does and the
    /// document makes the key. See <see cref="BinkOrderKey"/>.
    /// </summary>
    public string? OrderKey { get; init; }

    /// <summary>The move clock the element was placed with, or 0 for a new move; see <see cref="BinkReorderElementOperation.OrderClock"/>.</summary>
    public long OrderClock { get; init; }

    /// <summary>Set for a snapshot's additions, which state the key and clock to store, as they are.</summary>
    internal bool Authoritative { get; init; }

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        (string key, long clock) = document.InsertElementCore(LayerId, Element, Index, OrderKey, OrderClock, Authoritative);
        applied = key == OrderKey && clock == OrderClock ? this : this with { OrderKey = key, OrderClock = clock };
        return new BinkRemoveElementOperation(Element.Id);
    }
}
