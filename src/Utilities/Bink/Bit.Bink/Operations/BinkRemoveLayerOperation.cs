namespace Bit.Bink;

/// <summary>Removes a layer and everything in it.</summary>
/// <param name="LayerId">The layer.</param>
public sealed record BinkRemoveLayerOperation(Guid LayerId) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Remove layer";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.TryGetLayer(LayerId, out _) && document.Layers.Count > 1;

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        applied = this;
        (int index, BinkLayer removed, string key, long clock) = document.RemoveLayerCore(LayerId);
        return new BinkAddLayerOperation(removed.Id, removed.Properties, index, removed.Elements.ToArray())
        {
            OrderKey = key,
            OrderClock = clock,
            ElementOrderKeys = removed.Keys.ToArray(),
            ElementOrderClocks = removed.Clocks.ToArray(),
        };
    }
}
