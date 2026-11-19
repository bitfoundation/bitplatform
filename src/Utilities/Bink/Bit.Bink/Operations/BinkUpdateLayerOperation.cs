namespace Bit.Bink;

/// <summary>Changes a layer's properties (name, visibility, lock, opacity).</summary>
/// <param name="LayerId">The layer.</param>
/// <param name="Properties">New properties.</param>
public sealed record BinkUpdateLayerOperation(Guid LayerId, BinkLayerProperties Properties) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Change layer";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => document.TryGetLayer(LayerId, out _);

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        applied = this;
        BinkLayerProperties previous = document.UpdateLayerCore(LayerId, Properties);
        return new BinkUpdateLayerOperation(LayerId, previous);
    }
}
