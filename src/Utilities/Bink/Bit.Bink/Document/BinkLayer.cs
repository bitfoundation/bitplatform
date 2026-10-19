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

    /// <summary>Optional application metadata.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// An ordered collection of elements within a document. Layers are only mutated through <see cref="BinkDocument"/>
/// so that every change is an operation that can be undone and synchronized.
/// </summary>
public sealed class BinkLayer
{
    private readonly List<BinkElement> _elements;

    internal BinkLayer(Guid id, BinkLayerProperties properties, IEnumerable<BinkElement>? elements)
    {
        Id = id;
        Properties = properties;
        _elements = elements is null ? new List<BinkElement>() : new List<BinkElement>(elements);
    }

    /// <summary>Unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>Current properties.</summary>
    public BinkLayerProperties Properties { get; internal set; }

    /// <summary>Display name.</summary>
    public string Name => Properties.Name;

    /// <summary>Whether the layer is drawn.</summary>
    public bool IsVisible => Properties.IsVisible;

    /// <summary>Whether the layer rejects edits.</summary>
    public bool IsLocked => Properties.IsLocked;

    /// <summary>Opacity 0..1.</summary>
    public float Opacity => Properties.Opacity;

    /// <summary>Elements in z-order (first is bottom-most).</summary>
    public IReadOnlyList<BinkElement> Elements => _elements;

    /// <summary>Number of elements.</summary>
    public int Count => _elements.Count;

    /// <summary>Union of element bounds.</summary>
    public BinkRect ContentBounds
    {
        get
        {
            BinkRect bounds = BinkRect.Empty;
            foreach (BinkElement e in _elements) bounds = bounds.Union(e.Bounds);
            return bounds;
        }
    }

    /// <summary>Index of the element with the given id, or -1.</summary>
    public int IndexOf(Guid elementId)
    {
        for (int i = _elements.Count - 1; i >= 0; i--) if (_elements[i].Id == elementId) return i;
        return -1;
    }

    internal List<BinkElement> MutableElements => _elements;

    /// <inheritdoc />
    public override string ToString() => $"Layer \"{Name}\" ({_elements.Count} elements)";
}
