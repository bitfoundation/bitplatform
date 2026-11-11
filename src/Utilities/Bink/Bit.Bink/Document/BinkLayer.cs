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

    // Each element's order key and the clock of the move that put it there, parallel to _elements. The elements are
    // always in key order (ties by id), so the stacking order is a function of the keys alone, and two replicas with
    // the same keys draw the same order whatever order the moves reached them in.
    private readonly List<string> _keys;
    private readonly List<long> _clocks;

    internal BinkLayer(Guid id, BinkLayerProperties properties, IEnumerable<BinkElement>? elements, IReadOnlyList<string>? keys = null, IReadOnlyList<long>? clocks = null)
    {
        Id = id;
        Properties = properties;
        _elements = elements is null ? new List<BinkElement>() : new List<BinkElement>(elements);
        // Keys that are missing or out of order (a file that does not store them, or a corrupt one) are made afresh
        // from the order of the elements, which is then what they say.
        bool keyed = keys is not null && keys.Count == _elements.Count && IsOrdered(keys, _elements);
        _keys = keyed ? new List<string>(keys!) : new List<string>(BinkOrderKey.Spread(_elements.Count));
        _clocks = keyed && clocks is not null && clocks.Count == _elements.Count ? new List<long>(clocks) : new List<long>(new long[_elements.Count]);
    }

    /// <summary>Whether keys are valid and increase with the elements' order, ties broken by id.</summary>
    internal static bool IsOrdered(IReadOnlyList<string> keys, IReadOnlyList<BinkElement> elements)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            if (!BinkOrderKey.IsValid(keys[i])) return false;
            if (i > 0 && Precedes(keys[i], elements[i].Id, keys[i - 1], elements[i - 1].Id)) return false;
        }
        return true;
    }

    /// <summary>Whether (key a, id a) sorts before (key b, id b).</summary>
    internal static bool Precedes(string keyA, Guid idA, string keyB, Guid idB)
    {
        int byKey = string.CompareOrdinal(keyA, keyB);
        return byKey < 0 || (byKey == 0 && idA.CompareTo(idB) < 0);
    }

    /// <summary>The order key of the element at an index.</summary>
    internal string KeyAt(int index) => _keys[index];

    /// <summary>The clock of the move that put the element at an index where it is.</summary>
    internal long ClockAt(int index) => _clocks[index];

    /// <summary>The keys in stacking order.</summary>
    internal IReadOnlyList<string> Keys => _keys;

    /// <summary>The clocks in stacking order.</summary>
    internal IReadOnlyList<long> Clocks => _clocks;

    /// <summary>The order key of an element, or null when it is not in the layer.</summary>
    public string? OrderKeyOf(Guid elementId)
    {
        int index = IndexOf(elementId);
        return index < 0 ? null : _keys[index];
    }

    /// <summary>
    /// The key an element inserted at <paramref name="index"/> (of the elements as they are, the element itself not
    /// among them) would need. Elements sharing a key, which concurrent insertions can make, are kept together, so the
    /// new key goes after all of the run below it.
    /// </summary>
    internal string KeyForIndex(int index, Guid excluded = default)
    {
        string? below = null, above = null;
        for (int i = index - 1; i >= 0 && below is null; i--) if (_elements[i].Id != excluded) below = _keys[i];
        for (int i = index; i < _elements.Count && above is null; i++)
        {
            if (_elements[i].Id == excluded) continue;
            if (below is null || string.CompareOrdinal(_keys[i], below) > 0) above = _keys[i];
        }
        return BinkOrderKey.Between(below, above);
    }

    /// <summary>The index an element with a key belongs at.</summary>
    internal int IndexForKey(string key, Guid elementId)
    {
        int low = 0, high = _elements.Count;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (Precedes(_keys[mid], _elements[mid].Id, key, elementId)) low = mid + 1;
            else high = mid;
        }
        return low;
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

    /// <summary>Inserts an element where its key places it, and returns that index.</summary>
    internal int Insert(BinkElement element, string key, long clock)
    {
        int index = IndexForKey(key, element.Id);
        _keys.Insert(index, key);
        _clocks.Insert(index, clock);
        if (index == _elements.Count && _indexedUpTo == index)
        {
            // Appending, the common case of drawing, keeps the whole index exact.
            _elements.Add(element);
            _positions[element.Id] = index;
            _indexedUpTo = index + 1;
            return index;
        }
        _elements.Insert(index, element);
        _indexedUpTo = Math.Min(_indexedUpTo, index);
        return index;
    }

    /// <summary>Removes the element at an index and returns its key and clock.</summary>
    internal (string Key, long Clock) RemoveAt(int index)
    {
        (string key, long clock) = (_keys[index], _clocks[index]);
        _positions.Remove(_elements[index].Id);
        _elements.RemoveAt(index);
        _keys.RemoveAt(index);
        _clocks.RemoveAt(index);
        _indexedUpTo = Math.Min(_indexedUpTo, index);
        return (key, clock);
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
