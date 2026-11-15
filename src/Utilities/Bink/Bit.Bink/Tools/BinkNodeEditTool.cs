using System.Numerics;

namespace Bit.Bink;

/// <summary>What a node of an element edited by <see cref="BinkNodeEditTool"/> is.</summary>
public enum BinkNodeKind : byte
{
    /// <summary>A point the outline passes through: a polygon's corner, a line's end, a path's anchor.</summary>
    Anchor = 0,

    /// <summary>A Bézier control point of a path, which the curve bends toward without passing through.</summary>
    Control = 1,
}

/// <summary>A node of an element, in the element's local coordinates.</summary>
/// <param name="Position">Where the node is, in local coordinates; the element's transform maps it into the document.</param>
/// <param name="Kind">An anchor or a control point.</param>
public readonly record struct BinkNode(Vector2 Position, BinkNodeKind Kind);

/// <summary>
/// Edits the points of an outline: the corners of a polygon or polyline, the ends of a line, and the anchors and
/// Bézier control points of a path. Click an element to edit it, drag a node to move it; moving a path's anchor
/// carries the control points beside it, so the curves keep their shape.
/// </summary>
/// <remarks>
/// <para>
/// Every drag is one undo step, and every edit is an ordinary replacement of the element, so it saves, synchronizes
/// and exports like any other. An element selected when the tool is activated is edited straight away.
/// </para>
/// <para>
/// From the keyboard: Tab and Shift+Tab move between nodes, the arrow keys nudge the current one a unit (ten with
/// Shift), Delete or Backspace removes a polygon's corner while it keeps enough to stay a polygon, and Escape stops
/// editing. Locked elements and elements on locked or hidden layers are not edited.
/// </para>
/// </remarks>
public sealed class BinkNodeEditTool : BinkToolBase
{
    private Guid? _targetId;
    private int _active = -1;
    private int _pointerId = -1;
    private IDisposable? _group;
    private BinkElement? _original;
    private Vector2 _grab;

    /// <inheritdoc />
    public override string Id => "node-edit";

    /// <inheritdoc />
    public override string DisplayName => "Edit points";

    /// <inheritdoc />
    public override string Cursor => "default";

    /// <summary>Size of the node handles in view (CSS) pixels.</summary>
    public float HandleSize { get; set; } = 9f;

    /// <summary>How much larger handles are to a finger than to a mouse or a pen.</summary>
    public float TouchTargetScale { get; set; } = 2f;

    /// <summary>The element being edited, or null.</summary>
    public Guid? TargetId => _targetId;

    /// <summary>The index of the current node among <see cref="GetNodes"/> of the target, or -1.</summary>
    public int ActiveNode => _active;

    /// <summary>True while a node is being dragged.</summary>
    public bool IsDragging => _group is not null;

    /// <summary>Whether the tool can edit the points of an element: lines, polygons, polylines and paths.</summary>
    public static bool CanEdit(BinkElement element) => element is BinkLineShape or BinkPolygonShape or BinkPathElement && !element.IsLocked;

    /// <summary>The nodes of an element, in its local coordinates, or none when the tool cannot edit it.</summary>
    public static IReadOnlyList<BinkNode> GetNodes(BinkElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        switch (element)
        {
            case BinkLineShape line:
                return [new BinkNode(line.Start, BinkNodeKind.Anchor), new BinkNode(line.End, BinkNodeKind.Anchor)];
            case BinkPolygonShape polygon:
            {
                var nodes = new BinkNode[polygon.Points.Count];
                for (int i = 0; i < nodes.Length; i++) nodes[i] = new BinkNode(polygon.Points[i], BinkNodeKind.Anchor);
                return nodes;
            }
            case BinkPathElement path:
            {
                var nodes = new List<BinkNode>(path.Path.Coordinates.Length / 2);
                ReadOnlySpan<float> c = path.Path.Coordinates;
                int k = 0;
                foreach (BinkPathVerb verb in path.Path.Verbs)
                {
                    int pairs = BinkPathGeometry.CoordinateCount(verb) / 2;
                    for (int p = 0; p < pairs; p++, k += 2)
                    {
                        // The last pair of a verb is where the outline goes; any before it bend the curve.
                        nodes.Add(new BinkNode(new Vector2(c[k], c[k + 1]), p == pairs - 1 ? BinkNodeKind.Anchor : BinkNodeKind.Control));
                    }
                }
                return nodes;
            }
            default:
                return [];
        }
    }

    /// <summary>
    /// Returns a copy of an element with one node moved to <paramref name="position"/>, in local coordinates. Moving a
    /// path's anchor moves the cubic control points beside it by as much. Null when the element or node cannot be edited.
    /// </summary>
    public static BinkElement? MoveNode(BinkElement element, int index, Vector2 position)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) return null;
        switch (element)
        {
            case BinkLineShape line when index is 0 or 1:
                return index == 0 ? line with { Start = position } : line with { End = position };
            case BinkPolygonShape polygon when index >= 0 && index < polygon.Points.Count:
            {
                Vector2[] points = [.. polygon.Points];
                points[index] = position;
                return polygon with { Points = points };
            }
            case BinkPathElement path when index >= 0 && index < path.Path.Coordinates.Length / 2:
            {
                ReadOnlySpan<BinkPathVerb> verbs = path.Path.Verbs;
                float[] c = path.Path.Coordinates.ToArray();
                Vector2 delta = position - new Vector2(c[index * 2], c[index * 2 + 1]);
                var moved = new HashSet<int>();
                if (IsAnchor(verbs, index, out int verb))
                {
                    // A closed sub-path that ends where it began has two anchors on that spot; they move together, so
                    // the curve returning to it keeps its shape too. Found before anything moves.
                    int twin = Twin(verbs, c, index);
                    MoveAnchor(verbs, index, verb);
                    if (twin >= 0 && IsAnchor(verbs, twin, out int twinVerb)) MoveAnchor(verbs, twin, twinVerb);
                }
                else
                {
                    Shift(index);
                }
                return path with { Path = Rebuild(path.Path.Verbs, c) };

                void MoveAnchor(ReadOnlySpan<BinkPathVerb> verbs, int pair, int owner)
                {
                    Shift(pair);
                    // The control point arriving at the anchor, and the one leaving it, move with it.
                    if (verbs[owner] == BinkPathVerb.CubicTo) Shift(pair - 1);
                    if (owner + 1 < verbs.Length && verbs[owner + 1] == BinkPathVerb.CubicTo) Shift(pair + 1);
                }

                void Shift(int pair)
                {
                    if (!moved.Add(pair)) return;
                    c[pair * 2] += delta.X;
                    c[pair * 2 + 1] += delta.Y;
                }
            }
            default:
                return null;
        }
    }

    /// <summary>
    /// Returns a copy of a polygon or polyline without one of its points, or null when it would be left with too few
    /// (three for a closed polygon, two for an open one) or the element is not one.
    /// </summary>
    public static BinkElement? RemoveNode(BinkElement element, int index)
    {
        if (element is not BinkPolygonShape polygon || index < 0 || index >= polygon.Points.Count) return null;
        if (polygon.Points.Count <= (polygon.IsClosed ? 3 : 2)) return null;
        var points = new List<Vector2>(polygon.Points);
        points.RemoveAt(index);
        return polygon with { Points = points };
    }

    /// <inheritdoc />
    public override void Activate(IBinkToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Selection.Count == 1 && context.Document.TryGetElement(context.Selection.Ids[0], out BinkElement? selected) && CanEdit(selected)) Target(context, selected);
    }

    /// <inheritdoc />
    public override void Deactivate(IBinkToolContext context)
    {
        CancelDrag(context);
        _targetId = null;
        _active = -1;
        context?.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_group is not null) return false;
        Vector2 point = context.Viewport.ToDocument(sample.Position);
        float zoom = context.Viewport.Zoom > 0 ? context.Viewport.Zoom : 1f;
        float reach = HandleSize * (sample.Kind == BinkPointerKind.Touch ? TouchTargetScale : 1f) / zoom;

        if (Current(context) is { } target && NearestNode(target, point, reach) is int node and >= 0)
        {
            // The node keeps its offset from the pointer, so grabbing it a few pixels off does not make it jump.
            _grab = Vector2.Transform(GetNodes(target)[node].Position, target.Transform) - point;
            _active = node;
            _pointerId = sample.PointerId;
            _original = target;
            _group = context.History.BeginGroup("Edit points");
            context.CapturePointer(sample.PointerId);
            context.InvalidateOverlay();
            return true;
        }

        BinkElement? hit = BinkDocumentQuery.HitTest(context.Document, point, reach / 2f);
        if (hit is not null && CanEdit(hit))
        {
            Target(context, hit);
            return true;
        }
        if (_targetId is not null)
        {
            _targetId = null;
            _active = -1;
            context.Selection.Clear();
            context.InvalidateOverlay();
        }
        if (hit is not null) context.Announce($"The points of {hit.TypeName.ToLowerInvariant()} elements cannot be edited.");
        return hit is not null;
    }

    /// <inheritdoc />
    public override void OnPointerMove(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_group is null || sample.PointerId != _pointerId || sample.IsPredicted) return;
        if (Current(context) is not { } target || !Matrix3x2.Invert(target.Transform, out Matrix3x2 inverse)) return;
        Vector2 local = Vector2.Transform(context.Viewport.ToDocument(sample.Position) + _grab, inverse);
        IReadOnlyList<BinkNode> nodes = GetNodes(target);
        // A click, or a sample where the pointer has not moved, changes nothing and so records nothing.
        if (_active < nodes.Count && Vector2.DistanceSquared(nodes[_active].Position, local) < 1e-8f) return;
        Replace(context, target, MoveNode(target, _active, local));
    }

    /// <inheritdoc />
    public override void OnPointerUp(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_group is null || sample.PointerId != _pointerId) return;
        OnPointerMove(context, sample);
        context.ReleasePointer(sample.PointerId);
        _group.Dispose();
        _group = null;
        _original = null;
        _pointerId = -1;
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (sample.PointerId == _pointerId) CancelDrag(context);
    }

    /// <inheritdoc />
    public override bool OnKeyDown(IBinkToolContext context, BinkToolKeyEvent key)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_group is not null)
        {
            // While a point is dragged the keys are the drag's: Escape puts it back, and nothing else may reach the
            // canvas, where Delete would remove the element mid-drag inside the drag's own undo step.
            if (key.Key == "Escape")
            {
                CancelDrag(context);
                context.Announce("Move cancelled.");
            }
            return true;
        }
        if (Current(context) is not { } target) return false;
        IReadOnlyList<BinkNode> nodes = GetNodes(target);
        switch (key.Key)
        {
            case "Escape":
                // Leaving the element leaves its selection too, so the keys it claimed go back to the page.
                _targetId = null;
                _active = -1;
                context.Selection.Clear();
                context.InvalidateOverlay();
                context.Announce("Stopped editing points.");
                return true;
            case "Tab" when nodes.Count > 0:
                _active = _active < 0 ? (key.Shift ? nodes.Count - 1 : 0) : (_active + (key.Shift ? nodes.Count - 1 : 1)) % nodes.Count;
                context.InvalidateOverlay();
                context.Announce($"{(nodes[_active].Kind == BinkNodeKind.Anchor ? "Point" : "Control point")} {_active + 1} of {nodes.Count}.");
                return true;
            case "ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown" when _active >= 0 && _active < nodes.Count:
            {
                float step = key.Shift ? 10f : 1f;
                Vector2 delta = key.Key switch { "ArrowLeft" => new(-step, 0), "ArrowRight" => new(step, 0), "ArrowUp" => new(0, -step), _ => new(0, step) };
                // The nudge is a document distance; the node lives in local coordinates.
                if (!Matrix3x2.Invert(target.Transform, out Matrix3x2 inverse)) return false;
                Vector2 document = Vector2.Transform(nodes[_active].Position, target.Transform) + delta;
                using (context.History.BeginGroup("Move point")) Replace(context, target, MoveNode(target, _active, Vector2.Transform(document, inverse)));
                return true;
            }
            case "Delete" or "Backspace" when _active >= 0:
            {
                BinkElement? removed = RemoveNode(target, _active);
                if (removed is null)
                {
                    context.Announce("That point cannot be removed.");
                    return true;
                }
                using (context.History.BeginGroup("Remove point")) Replace(context, target, removed);
                _active = Math.Min(_active, GetNodes(removed).Count - 1);
                context.Announce("Point removed.");
                return true;
            }
            default:
                return false;
        }
    }

    /// <inheritdoc />
    public override void DrawOverlay(IBinkToolContext context, IBinkRenderContext render)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(render);
        if (Current(context) is not { } target) return;
        IReadOnlyList<BinkNode> nodes = GetNodes(target);
        float zoom = context.Viewport.Zoom > 0 ? context.Viewport.Zoom : 1f;
        float size = HandleSize / zoom;
        var accent = new BinkColor(37, 99, 235);
        var outline = new BinkStrokePaint(accent, 1f / zoom, 1f, BinkBlendMode.Normal, BinkLineCap.Butt, BinkLineJoin.Miter);

        // A line from each control point to the anchor beside it: a cubic's first control belongs to the anchor the
        // curve leaves, its second to the one it reaches, and a quadratic's one control to both.
        var levers = new BinkPathBuilder(8);
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Kind != BinkNodeKind.Control) continue;
            Vector2 control = Vector2.Transform(nodes[i].Position, target.Transform);
            if (i > 0 && nodes[i - 1].Kind == BinkNodeKind.Anchor) levers.MoveTo(control).LineTo(Vector2.Transform(nodes[i - 1].Position, target.Transform));
            if (i + 1 < nodes.Count && nodes[i + 1].Kind == BinkNodeKind.Anchor) levers.MoveTo(control).LineTo(Vector2.Transform(nodes[i + 1].Position, target.Transform));
        }
        if (!levers.IsEmpty) render.StrokePath(levers.ToGeometry(), outline);

        for (int i = 0; i < nodes.Count; i++)
        {
            Vector2 p = Vector2.Transform(nodes[i].Position, target.Transform);
            BinkRect box = BinkRect.FromCenter(p, size, size);
            BinkPathGeometry handle = nodes[i].Kind == BinkNodeKind.Anchor ? new BinkPathBuilder(6).AddRect(box).ToGeometry() : new BinkPathBuilder(8).AddEllipse(box).ToGeometry();
            render.FillPath(handle, new BinkFillPaint(i == _active ? accent : BinkColor.White));
            render.StrokePath(handle, outline);
        }
    }

    private BinkElement? Current(IBinkToolContext context)
    {
        if (_targetId is not { } id) return null;
        if (context.Document.TryGetElement(id, out BinkElement? element, out BinkLayer? layer) && CanEdit(element) && layer.IsVisible && !layer.IsLocked) return element;
        _targetId = null;
        _active = -1;
        return null;
    }

    private void Target(IBinkToolContext context, BinkElement element)
    {
        _targetId = element.Id;
        _active = -1;
        context.Selection.Set(element.Id);
        context.InvalidateOverlay();
        context.Announce($"Editing the points of a {element.TypeName.ToLowerInvariant()}: {GetNodes(element).Count} points. Tab moves between them.");
    }

    private static int NearestNode(BinkElement element, Vector2 point, float reach)
    {
        IReadOnlyList<BinkNode> nodes = GetNodes(element);
        int best = -1;
        float bestDistance = reach * reach;
        // Later nodes are drawn on top, so they win a tie.
        for (int i = 0; i < nodes.Count; i++)
        {
            float d = Vector2.DistanceSquared(Vector2.Transform(nodes[i].Position, element.Transform), point);
            if (d <= bestDistance) { bestDistance = d; best = i; }
        }
        return best;
    }

    private void Replace(IBinkToolContext context, BinkElement before, BinkElement? after)
    {
        if (after is null) return;
        try
        {
            context.Document.ReplaceElement(after);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            context.Announce("That change could not be applied: " + ex.Message);
            return;
        }
        context.InvalidateDocument(before.Bounds.Union(after.Bounds));
        context.InvalidateOverlay();
    }

    private void CancelDrag(IBinkToolContext? context)
    {
        if (_group is null) return;
        if (context is not null)
        {
            // The drag is put back, then left out of the history, so the cancelled gesture leaves no undo step.
            if (_original is not null && context.Document.ContainsElement(_original.Id))
            {
                try { context.Document.ReplaceElement(_original); } catch (InvalidOperationException) { }
                context.InvalidateDocument(_original.Bounds);
            }
            context.History.CancelGroup();
            if (_pointerId >= 0) context.ReleasePointer(_pointerId);
            context.InvalidateOverlay();
        }
        _group.Dispose();
        _group = null;
        _original = null;
        _pointerId = -1;
    }

    /// <summary>Whether a coordinate pair is an anchor (the last pair of its verb), and which verb it belongs to.</summary>
    private static bool IsAnchor(ReadOnlySpan<BinkPathVerb> verbs, int pair, out int verb)
        => Owner(verbs, pair, out verb, out int first) && pair == first + BinkPathGeometry.CoordinateCount(verbs[verb]) / 2 - 1;

    /// <summary>
    /// The other anchor on the same spot as <paramref name="pair"/> in a closed sub-path that ends where it began: its
    /// start for its last anchor, and the reverse. -1 when there is none.
    /// </summary>
    private static int Twin(ReadOnlySpan<BinkPathVerb> verbs, float[] c, int pair)
    {
        int k = 0, start = -1, last = -1;
        for (int v = 0; v <= verbs.Length; v++)
        {
            bool ends = v == verbs.Length || verbs[v] is BinkPathVerb.MoveTo or BinkPathVerb.Close;
            if (ends && start >= 0)
            {
                // The sub-path from start to last is over; it is closed when a Close ends it.
                bool closed = v < verbs.Length && verbs[v] == BinkPathVerb.Close;
                if (closed && last > start && MathF.Abs(c[start * 2] - c[last * 2]) < 1e-4f && MathF.Abs(c[start * 2 + 1] - c[last * 2 + 1]) < 1e-4f)
                {
                    if (pair == start) return last;
                    if (pair == last) return start;
                }
                start = -1;
            }
            if (v == verbs.Length) break;
            int pairs = BinkPathGeometry.CoordinateCount(verbs[v]) / 2;
            if (verbs[v] == BinkPathVerb.MoveTo) start = k;
            if (pairs > 0) last = k + pairs - 1;
            k += pairs;
        }
        return -1;
    }

    /// <summary>Which verb a coordinate pair belongs to and where that verb's pairs begin.</summary>
    private static bool Owner(ReadOnlySpan<BinkPathVerb> verbs, int pair, out int verb, out int first)
    {
        int k = 0;
        for (verb = 0; verb < verbs.Length; verb++)
        {
            int pairs = BinkPathGeometry.CoordinateCount(verbs[verb]) / 2;
            if (pair < k + pairs)
            {
                first = k;
                return true;
            }
            k += pairs;
        }
        first = -1;
        return false;
    }

    private static BinkPathGeometry Rebuild(ReadOnlySpan<BinkPathVerb> verbs, float[] c)
    {
        var builder = new BinkPathBuilder(verbs.Length);
        int k = 0;
        foreach (BinkPathVerb verb in verbs)
        {
            switch (verb)
            {
                case BinkPathVerb.MoveTo: builder.MoveTo(c[k], c[k + 1]); break;
                case BinkPathVerb.LineTo: builder.LineTo(c[k], c[k + 1]); break;
                case BinkPathVerb.QuadTo: builder.QuadTo(c[k], c[k + 1], c[k + 2], c[k + 3]); break;
                case BinkPathVerb.CubicTo: builder.CubicTo(c[k], c[k + 1], c[k + 2], c[k + 3], c[k + 4], c[k + 5]); break;
                case BinkPathVerb.Close: builder.Close(); break;
            }
            k += BinkPathGeometry.CoordinateCount(verb);
        }
        return builder.ToGeometry();
    }
}
