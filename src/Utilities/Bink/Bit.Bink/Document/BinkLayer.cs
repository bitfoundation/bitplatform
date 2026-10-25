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

/// <summary>
/// An ordered collection of elements within a document. Layers are only mutated through <see cref="BinkDocument"/>
/// so that every change is an operation that can be undone and synchronized.
/// </summary>
public sealed class BinkLayer
{
    private readonly List<BinkElement> _elements;

    // Where each element sits, by id. Every lookup of an element by id (moving a selection, replacing, removing,
    // the selection's own bounds) needs its index, and a linear scan made dragging a large selection quadratic:
    // ten thousand selected strokes cost hundreds of milliseconds a frame. Positions below _indexedUpTo are
    // exact; an insertion or removal only invalidates the positions from where it happened, and the next lookup
    // that needs one of those re-reads the tail. A drag only replaces elements in place, so it never invalidates.
    private readonly Dictionary<Guid, int> _positions = new();
    private int _indexedUpTo;

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

    /// <summary>How the layer combines with the layers below it.</summary>
    public BinkBlendMode BlendMode => Properties.BlendMode;

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
        if (_positions.TryGetValue(elementId, out int index) && index < _indexedUpTo) return index;
        if (_indexedUpTo == _elements.Count) return -1;
        for (int i = _indexedUpTo; i < _elements.Count; i++) _positions[_elements[i].Id] = i;
        _indexedUpTo = _elements.Count;
        return _positions.TryGetValue(elementId, out index) ? index : -1;
    }

    /// <summary>Inserts an element at an index (clamped by the caller).</summary>
    internal void InsertAt(int index, BinkElement element)
    {
        if (index == _elements.Count && _indexedUpTo == index)
        {
            // Appending, the common case of drawing, keeps the whole index exact.
            _elements.Add(element);
            _positions[element.Id] = index;
            _indexedUpTo = index + 1;
            return;
        }
        _elements.Insert(index, element);
        _indexedUpTo = Math.Min(_indexedUpTo, index);
    }

    /// <summary>Removes the element at an index.</summary>
    internal void RemoveAt(int index)
    {
        _positions.Remove(_elements[index].Id);
        _elements.RemoveAt(index);
        _indexedUpTo = Math.Min(_indexedUpTo, index);
    }

    /// <summary>Replaces the element at an index with another version of the same element.</summary>
    internal void SetAt(int index, BinkElement element)
    {
        System.Diagnostics.Debug.Assert(_elements[index].Id == element.Id, "A replacement keeps the element's id.");
        _elements[index] = element;
    }

    /// <inheritdoc />
    public override string ToString() => $"Layer \"{Name}\" ({_elements.Count} elements)";
}
