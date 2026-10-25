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
    /// <param name="includeLocked">Whether locked elements, and elements on locked layers, are candidates.</param>
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
                if (e.IsSelectable && (includeLocked || !e.IsLocked) && e.HitTest(point, tolerance)) return e;
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
                if (e.IsSelectable && (includeLocked || !e.IsLocked) && e.HitTest(point, tolerance)) result.Add(e);
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
                if (!e.IsSelectable || (e.IsLocked && !includeLocked)) continue;
                bool match = mode == BinkRectSelectionMode.Contain ? rect.Contains(e.Bounds) : e.IntersectsRect(rect);
                if (match) result.Add(e);
            }
        }
        return result;
    }

    /// <summary>
    /// Finds selectable elements enclosed by a freehand lasso, bottom-most first. A stroke counts when at least
    /// <paramref name="coverage"/> of its points lie inside, which is what lets a loose loop around a handwritten
    /// word pick up its strokes without the ones it merely brushes; other elements are judged by their corners and
    /// centre the same way.
    /// </summary>
    /// <param name="document">Document.</param>
    /// <param name="lasso">The lasso, in document coordinates; it is closed implicitly.</param>
    /// <param name="coverage">The fraction of an element that must be inside, from just touching (0) to all of it (1).</param>
    /// <param name="includeLocked">Whether locked elements, and elements on locked layers, are candidates.</param>
    public static List<BinkElement> FindInPolygon(BinkDocument document, ReadOnlySpan<Vector2> lasso, float coverage = 0.5f, bool includeLocked = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        var result = new List<BinkElement>();
        if (lasso.Length < 3) return result;
        BinkRect area = BinkGeometryMath.Bounds(lasso);
        coverage = float.IsFinite(coverage) ? Math.Clamp(coverage, 0f, 1f) : 0.5f;
        foreach (BinkLayer layer in document.Layers)
        {
            if (!layer.IsVisible || (layer.IsLocked && !includeLocked)) continue;
            foreach (BinkElement e in layer.Elements)
            {
                if (!e.IsSelectable || (e.IsLocked && !includeLocked) || !e.Bounds.Intersects(area)) continue;
                if (Covered(e, lasso, area, coverage)) result.Add(e);
            }
        }
        return result;
    }

    private static bool Covered(BinkElement element, ReadOnlySpan<Vector2> lasso, BinkRect area, float coverage)
    {
        int inside = 0;
        int total;
        Matrix3x2 transform = element.Transform;
        bool identity = transform.IsIdentity;
        if (element is BinkStroke { Points.Count: > 0 } stroke)
        {
            ReadOnlySpan<float> xs = stroke.Points.X;
            ReadOnlySpan<float> ys = stroke.Points.Y;
            total = xs.Length;
            for (int i = 0; i < xs.Length; i++)
            {
                var p = new Vector2(xs[i], ys[i]);
                if (!identity) p = Vector2.Transform(p, transform);
                if (area.Contains(p) && BinkGeometryMath.PointInPolygon(p, lasso)) inside++;
            }
        }
        else
        {
            BinkRect local = element.LocalBounds;
            Span<Vector2> probes = [local.TopLeft, new(local.Right, local.Top), new(local.Right, local.Bottom), new(local.Left, local.Bottom), local.Center];
            total = probes.Length;
            foreach (Vector2 probe in probes)
            {
                Vector2 p = identity ? probe : Vector2.Transform(probe, transform);
                if (area.Contains(p) && BinkGeometryMath.PointInPolygon(p, lasso)) inside++;
            }
        }
        return coverage <= 0f ? inside > 0 : inside >= MathF.Ceiling(total * coverage - 1e-4f);
    }

    /// <summary>
    /// The group that picking <paramref name="element"/> selects as a whole: its outermost group at the top level,
    /// or, while working inside a group, the group nested directly inside that one.
    /// </summary>
    /// <param name="element">The element picked.</param>
    /// <param name="within">The group being worked inside, or null for the top level.</param>
    /// <returns>The group id, or null when the element is picked on its own.</returns>
    public static Guid? GetSelectionGroup(BinkElement element, Guid? within = null)
    {
        ArgumentNullException.ThrowIfNull(element);
        IReadOnlyList<Guid> chain = element.GroupIds;
        if (chain.Count == 0) return null;
        if (within is { } group)
        {
            for (int i = 0; i < chain.Count; i++)
            {
                if (chain[i] != group) continue;
                // Inside the innermost group an element is picked on its own; deeper in, by the next group inward.
                return i == 0 ? null : chain[i - 1];
            }
        }
        return chain[^1];
    }

    /// <summary>The members of a group, including those of groups nested inside it, bottom-most first.</summary>
    /// <param name="document">Document.</param>
    /// <param name="groupId">The group.</param>
    public static List<BinkElement> GetGroupMembers(BinkDocument document, Guid groupId)
    {
        ArgumentNullException.ThrowIfNull(document);
        var result = new List<BinkElement>();
        foreach (BinkLayer layer in document.Layers)
        {
            foreach (BinkElement e in layer.Elements) if (e.IsInGroup(groupId)) result.Add(e);
        }
        return result;
    }

    /// <summary>
    /// Expands picked elements to the groups they belong to, the way clicking an element or drawing a marquee over
    /// part of a group selects the whole group.
    /// </summary>
    /// <param name="document">Document.</param>
    /// <param name="elementIds">The elements picked, for example by a click or a marquee.</param>
    /// <param name="within">
    /// The group being worked inside, or null for the top level. See <see cref="GetSelectionGroup"/>.
    /// </param>
    /// <returns>
    /// The picked elements followed by the other members of their groups, without duplicates. Members a tool may not
    /// touch (locked, or on a locked or hidden layer) are left out.
    /// </returns>
    public static IReadOnlyList<Guid> ExpandToGroups(BinkDocument document, IEnumerable<Guid> elementIds, Guid? within = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(elementIds);
        var result = new List<Guid>();
        var seen = new HashSet<Guid>();
        HashSet<Guid>? groups = null;
        foreach (Guid id in elementIds)
        {
            if (!document.TryGetElement(id, out BinkElement? element)) continue;
            if (seen.Add(id)) result.Add(id);
            if (GetSelectionGroup(element, within) is { } group) (groups ??= []).Add(group);
        }
        if (groups is null) return result;

        foreach (BinkLayer layer in document.Layers)
        {
            if (layer.IsLocked || !layer.IsVisible) continue;
            foreach (BinkElement e in layer.Elements)
            {
                if (e.GroupIds.Count == 0 || !e.CanBeEdited || seen.Contains(e.Id)) continue;
                foreach (Guid g in e.GroupIds)
                {
                    if (!groups.Contains(g)) continue;
                    seen.Add(e.Id);
                    result.Add(e.Id);
                    break;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Measures the ink in a document: how many strokes, how long they are and how long they took to draw. This is
    /// what a signature field should validate, rather than whether anything at all was drawn: a dot or a stray
    /// line is not a signature, and a rule on length and duration says so.
    /// </summary>
    /// <param name="document">Document.</param>
    /// <param name="includeHiddenLayers">Whether strokes on hidden layers count.</param>
    public static BinkInkMetrics MeasureInk(BinkDocument document, bool includeHiddenLayers = false)
    {
        ArgumentNullException.ThrowIfNull(document);
        int strokes = 0;
        long points = 0;
        double length = 0;
        double milliseconds = 0;
        BinkRect bounds = BinkRect.Empty;
        foreach (BinkLayer layer in document.Layers)
        {
            if (!layer.IsVisible && !includeHiddenLayers) continue;
            foreach (BinkElement element in layer.Elements)
            {
                if (element is not BinkStroke { IsEraser: false, Points.Count: > 0 } stroke) continue;
                strokes++;
                points += stroke.Points.Count;
                bounds = bounds.Union(stroke.Bounds);
                ReadOnlySpan<float> xs = stroke.Points.X;
                ReadOnlySpan<float> ys = stroke.Points.Y;
                Matrix3x2 transform = stroke.Transform;
                bool identity = transform.IsIdentity;
                Vector2 previous = identity ? new Vector2(xs[0], ys[0]) : Vector2.Transform(new Vector2(xs[0], ys[0]), transform);
                for (int i = 1; i < xs.Length; i++)
                {
                    Vector2 next = identity ? new Vector2(xs[i], ys[i]) : Vector2.Transform(new Vector2(xs[i], ys[i]), transform);
                    length += Vector2.Distance(previous, next);
                    previous = next;
                }
                ReadOnlySpan<float> times = stroke.Points.Time;
                if (times.Length > 1 && float.IsFinite(times[^1] - times[0])) milliseconds += Math.Max(times[^1] - times[0], 0f);
            }
        }
        return new BinkInkMetrics(strokes, points, (float)length, TimeSpan.FromMilliseconds(milliseconds), bounds);
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

/// <summary>What <see cref="BinkDocumentQuery.MeasureInk"/> found.</summary>
/// <param name="StrokeCount">Strokes drawn, eraser strokes not counted.</param>
/// <param name="PointCount">Points across those strokes.</param>
/// <param name="Length">The total length of the ink in document units, as drawn (transforms applied).</param>
/// <param name="Duration">The time the pen was down, summed over strokes; zero for strokes without timing.</param>
/// <param name="Bounds">The area the ink covers.</param>
public readonly record struct BinkInkMetrics(int StrokeCount, long PointCount, float Length, TimeSpan Duration, BinkRect Bounds)
{
    /// <summary>True when there is no ink at all.</summary>
    public bool IsEmpty => StrokeCount == 0;
}
