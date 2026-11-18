namespace Bit.Bink;

/// <summary>Mutable-by-document properties of a layer, grouped so a layer update is a single operation.</summary>
public sealed record BinkLayerProperties
{
    /// <summary>Display name.</summary>
    public string Name { get; init; } = "Layer";

    /// <summary>Whether the layer is drawn.</summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>Whether the layer rejects edits from tools.</summary>
    public bool IsLocked { get; init; }

    /// <summary>Opacity 0..1 applied to the composited layer.</summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>
    /// How the finished layer combines with the layers below it: <see cref="BinkBlendMode.Multiply"/> darkens like ink
    /// on paper, which is what a layer of markup over a scanned page wants, <see cref="BinkBlendMode.Screen"/> lightens.
    /// <see cref="BinkBlendMode.DestinationOut"/> is an element's eraser, not a layer's, and reads as Normal.
    /// </summary>
    public BinkBlendMode BlendMode { get; init; } = BinkBlendMode.Normal;

    /// <summary>Optional application metadata.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
