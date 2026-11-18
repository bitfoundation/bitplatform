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

    /// <summary>
    /// Whether an element can be in the selection: it exists, tools may edit it, and its layer is visible and not
    /// locked. Selecting anything else would hand it to the next Delete, arrow key or handle drag, which a locked
    /// element or layer is documented to refuse and which nobody would expect for something they cannot see.
    /// </summary>
    private bool CanSelect(Guid id)
        => _document.TryGetElement(id, out BinkElement? element, out BinkLayer? layer) && element.CanBeEdited && !layer.IsLocked && layer.IsVisible;

    /// <summary>
    /// Replaces the selection. Ids of elements that are missing, not selectable, locked, or on a locked or hidden
    /// layer are skipped.
    /// </summary>
    public void Set(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var next = new List<Guid>();
        var nextSet = new HashSet<Guid>();
        foreach (Guid id in ids) if (CanSelect(id) && nextSet.Add(id)) next.Add(id);
        if (next.SequenceEqual(_ids)) return;
        _ids.Clear();
        _set.Clear();
        _ids.AddRange(next);
        _set.UnionWith(nextSet);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Selects exactly one element.</summary>
    public void Set(Guid id) => Set([id]);

    /// <summary>Adds an element, unless it cannot be selected (see <see cref="Set(IEnumerable{Guid})"/>).</summary>
    public void Add(Guid id)
    {
        if (_set.Contains(id) || !CanSelect(id)) return;
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

    /// <summary>Selects every selectable, unlocked element on unlocked, visible layers.</summary>
    public void SelectAll()
    {
        var ids = new List<Guid>();
        foreach (BinkLayer layer in _document.Layers)
        {
            if (layer.IsLocked || !layer.IsVisible) continue;
            foreach (BinkElement e in layer.Elements) if (e.CanBeEdited) ids.Add(e.Id);
        }
        Set(ids);
    }

    private void OnDocumentChanged(object? sender, BinkDocumentChangedEventArgs e)
    {
        if (_ids.Count == 0) return;
        bool changed = false;
        for (int i = _ids.Count - 1; i >= 0; i--)
        {
            // What was removed goes, and so does what became locked or hidden, itself or through its layer: a
            // collaborator, or the application through the document, locking a selected element must not leave it
            // selected for the next arrow key to move.
            if (!CanSelect(_ids[i]))
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
