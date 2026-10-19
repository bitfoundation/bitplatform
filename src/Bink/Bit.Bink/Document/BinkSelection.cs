using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// The set of selected element ids of a document. Ordered by selection time. Raises <see cref="Changed"/> when the
/// set changes; automatically drops elements that are removed from the document.
/// </summary>
public sealed class BinkSelection : IDisposable
{
    private readonly BinkDocument _document;
    private readonly List<Guid> _ids = new();
    private readonly HashSet<Guid> _set = new();
    private bool _disposed;

    /// <summary>Creates a selection bound to a document.</summary>
    public BinkSelection(BinkDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;
        document.Changed += OnDocumentChanged;
    }

    /// <summary>
    /// Selected ids in selection order, as a snapshot. A copy is returned because the obvious use is to iterate
    /// while deleting, and the selection updates itself as the document changes.
    /// </summary>
    public IReadOnlyList<Guid> Ids => _ids.ToArray();

    /// <summary>Number of selected elements.</summary>
    public int Count => _ids.Count;

    /// <summary>True when nothing is selected.</summary>
    public bool IsEmpty => _ids.Count == 0;

    /// <summary>Raised after the selection changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The selected elements that still exist, in selection order.</summary>
    public IEnumerable<BinkElement> Elements
    {
        get
        {
            // Iterating a copy, for the same reason as Ids: callers routinely edit the document while iterating.
            foreach (Guid id in _ids.ToArray())
            {
                if (_document.TryGetElement(id, out BinkElement? e)) yield return e;
            }
        }
    }

    /// <summary>Union of the selected elements' bounds.</summary>
    public BinkRect Bounds
    {
        get
        {
            BinkRect bounds = BinkRect.Empty;
            foreach (BinkElement e in Elements) bounds = bounds.Union(e.Bounds);
            return bounds;
        }
    }

    /// <summary>True when the element is selected.</summary>
    public bool Contains(Guid id) => _set.Contains(id);

    /// <summary>Replaces the selection.</summary>
    public void Set(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var next = new List<Guid>();
        var nextSet = new HashSet<Guid>();
        foreach (Guid id in ids) if (_document.TryGetElement(id, out BinkElement? e) && e.IsSelectable && nextSet.Add(id)) next.Add(id);
        if (next.SequenceEqual(_ids)) return;
        _ids.Clear();
        _set.Clear();
        _ids.AddRange(next);
        _set.UnionWith(nextSet);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Selects exactly one element.</summary>
    public void Set(Guid id) => Set([id]);

    /// <summary>Adds an element.</summary>
    public void Add(Guid id)
    {
        if (_set.Contains(id) || !_document.TryGetElement(id, out BinkElement? e) || !e.IsSelectable) return;
        _set.Add(id);
        _ids.Add(id);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes an element.</summary>
    public void Remove(Guid id)
    {
        if (!_set.Remove(id)) return;
        _ids.Remove(id);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Adds or removes an element.</summary>
    public void Toggle(Guid id)
    {
        if (_set.Contains(id)) Remove(id);
        else Add(id);
    }

    /// <summary>Clears the selection.</summary>
    public void Clear()
    {
        if (_ids.Count == 0) return;
        _ids.Clear();
        _set.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Selects every selectable element on unlocked, visible layers.</summary>
    public void SelectAll()
    {
        var ids = new List<Guid>();
        foreach (BinkLayer layer in _document.Layers)
        {
            if (layer.IsLocked || !layer.IsVisible) continue;
            foreach (BinkElement e in layer.Elements) if (e.IsSelectable) ids.Add(e.Id);
        }
        Set(ids);
    }

    private void OnDocumentChanged(object? sender, BinkDocumentChangedEventArgs e)
    {
        if (_ids.Count == 0) return;
        bool changed = false;
        for (int i = _ids.Count - 1; i >= 0; i--)
        {
            if (!_document.ContainsElement(_ids[i]))
            {
                _set.Remove(_ids[i]);
                _ids.RemoveAt(i);
                changed = true;
            }
        }
        if (changed) Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _document.Changed -= OnDocumentChanged;
    }
}

/// <summary>How a rectangle query matches elements.</summary>
public enum BinkRectSelectionMode : byte
{
    /// <summary>Elements whose geometry intersects the rectangle.</summary>
    Intersect = 0,
    /// <summary>Elements entirely inside the rectangle.</summary>
    Contain = 1,
}

/// <summary>Spatial queries over a document: hit testing and rectangle selection.</summary>
public static class BinkDocumentQuery
{
    /// <summary>
    /// Finds the top-most element under a point. Locked or hidden layers and non-selectable elements are skipped.
    /// </summary>
    /// <param name="document">Document.</param>
    /// <param name="point">Point in document coordinates.</param>
    /// <param name="tolerance">Extra hit distance in document units (use a few units at low zoom so thin strokes are easy to grab).</param>
    /// <param name="includeLocked">Whether elements on locked layers are candidates.</param>
    public static BinkElement? HitTest(BinkDocument document, Vector2 point, float tolerance = 4f, bool includeLocked = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        for (int li = document.Layers.Count - 1; li >= 0; li--)
        {
            BinkLayer layer = document.Layers[li];
            if (!layer.IsVisible || (layer.IsLocked && !includeLocked)) continue;
            IReadOnlyList<BinkElement> elements = layer.Elements;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                BinkElement e = elements[i];
                if (e.IsSelectable && e.HitTest(point, tolerance)) return e;
            }
        }
        return null;
    }

    /// <summary>Finds all elements hit by a point, top-most first.</summary>
    public static List<BinkElement> HitTestAll(BinkDocument document, Vector2 point, float tolerance = 4f, bool includeLocked = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        var result = new List<BinkElement>();
        for (int li = document.Layers.Count - 1; li >= 0; li--)
        {
            BinkLayer layer = document.Layers[li];
            if (!layer.IsVisible || (layer.IsLocked && !includeLocked)) continue;
            IReadOnlyList<BinkElement> elements = layer.Elements;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                BinkElement e = elements[i];
                if (e.IsSelectable && e.HitTest(point, tolerance)) result.Add(e);
            }
        }
        return result;
    }

    /// <summary>Finds selectable elements matching a rectangle, bottom-most first.</summary>
    public static List<BinkElement> FindInRect(BinkDocument document, BinkRect rect, BinkRectSelectionMode mode = BinkRectSelectionMode.Intersect, bool includeLocked = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        var result = new List<BinkElement>();
        foreach (BinkLayer layer in document.Layers)
        {
            if (!layer.IsVisible || (layer.IsLocked && !includeLocked)) continue;
            foreach (BinkElement e in layer.Elements)
            {
                if (!e.IsSelectable) continue;
                bool match = mode == BinkRectSelectionMode.Contain ? rect.Contains(e.Bounds) : e.IntersectsRect(rect);
                if (match) result.Add(e);
            }
        }
        return result;
    }

    /// <summary>Finds elements (including non-selectable ones) whose bounds intersect a rectangle, on visible layers.</summary>
    public static IEnumerable<BinkElement> ElementsInBounds(BinkDocument document, BinkRect rect)
    {
        ArgumentNullException.ThrowIfNull(document);
        foreach (BinkLayer layer in document.Layers)
        {
            if (!layer.IsVisible) continue;
            foreach (BinkElement e in layer.Elements) if (e.Bounds.Intersects(rect)) yield return e;
        }
    }
}
