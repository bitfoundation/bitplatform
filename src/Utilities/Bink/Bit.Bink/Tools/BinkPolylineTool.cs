using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// Draws a polyline or polygon point by point: each click (or each lift of a finger or pen dragged to the spot) adds
/// a point, while the next segment follows the pointer. A double click or Enter finishes an open polyline; clicking
/// the first point again closes it into a polygon. Backspace takes the last point back and Escape abandons it.
/// Shift keeps each segment to 15-degree steps. The result is a <see cref="BinkPolygonShape"/>, added as one undo step.
/// </summary>
public sealed class BinkPolylineTool : BinkToolBase
{
    private readonly List<Vector2> _points = new();
    private Vector2 _cursor;
    private Guid _targetLayerId;
    private double _lastUpTime = double.NegativeInfinity;
    private Vector2 _lastUpPosition;
    private BinkSnapper _snapper = BinkSnapper.Disabled;
    private readonly List<BinkSnapGuide> _guides = new();

    /// <inheritdoc />
    public override string Id => "polyline";

    /// <inheritdoc />
    public override string DisplayName => "Polyline";

    /// <inheritdoc />
    /// <remarks>The next segment follows the pointer between clicks, which needs its movement while no button is pressed.</remarks>
    public override bool WantsHover => true;

    /// <summary>Whether an open polyline gets an arrow head at its last point, which is what a diagram connector wants.</summary>
    public bool EndArrow { get; set; }

    /// <summary>Whether the result is a smooth curve through the points rather than straight segments.</summary>
    public bool Smooth { get; set; }

    /// <summary>The longest pause between two clicks that still finishes the polyline as a double click, in milliseconds.</summary>
    public double DoubleClickTime { get; set; } = 500;

    /// <summary>How close to the first point, in view pixels, a click has to be to close the polygon.</summary>
    public float CloseDistance { get; set; } = 8f;

    /// <summary>The points placed so far, in document coordinates; empty when nothing is being drawn.</summary>
    public IReadOnlyList<Vector2> Points => _points;

    /// <summary>True while a polyline is being placed.</summary>
    public bool IsDrawing => _points.Count > 0;

    /// <inheritdoc />
    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        float zoom = MathF.Max(context.Viewport.Zoom, 0.001f);
        if (_points.Count == 0)
        {
            if (context.Document.TryGetLayer(context.ActiveLayerId, out BinkLayer? layer) && (layer.IsLocked || !layer.IsVisible)) return false;
            // The shape belongs to the layer it was started on, even if the active layer changes meanwhile.
            _targetLayerId = context.ActiveLayerId;
            _snapper = context.Snapping is { IsEnabled: true } snapping
                ? BinkSnapper.Create(context.Document, snapping, null, context.Viewport.VisibleRect(context.ViewSize))
                : BinkSnapper.Disabled;
            _points.Add(Place(context, context.Viewport.ToDocument(sample.Position)));
            _cursor = _points[0];
            context.Announce("Polyline started. Click to add points, double-click or press Enter to finish, click the first point to close it.");
            context.InvalidateOverlay();
            return true;
        }

        Vector2 point = context.Viewport.ToDocument(sample.Position);
        if (sample.Timestamp - _lastUpTime <= DoubleClickTime && Vector2.Distance(sample.Position, _lastUpPosition) <= 6f)
        {
            Finish(context, closed: false);
            return true;
        }
        if (_points.Count >= 3 && Vector2.Distance(point, _points[0]) * zoom <= CloseDistance)
        {
            Finish(context, closed: true);
            return true;
        }
        _cursor = Place(context, point, sample.Shift);
        context.InvalidateOverlay();
        return true;
    }

    /// <inheritdoc />
    public override void OnPointerMove(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_points.Count == 0 || sample.IsPredicted) return;
        _guides.Clear();
        _cursor = Place(context, context.Viewport.ToDocument(sample.Position), sample.Shift, _guides);
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerUp(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_points.Count == 0) return;
        // A point is added where the pointer is let go, unless that is where the last one already is: the release of
        // the first click, or a click that did not move.
        Vector2 point = Place(context, context.Viewport.ToDocument(sample.Position), sample.Shift);
        float zoom = MathF.Max(context.Viewport.Zoom, 0.001f);
        if (Vector2.Distance(point, _points[^1]) * zoom > 2f)
        {
            _points.Add(point);
            if (_points.Count > BinkPolygonShape.MaxPoints) _points.RemoveAt(_points.Count - 1);
        }
        _cursor = point;
        _lastUpTime = sample.Timestamp;
        _lastUpPosition = sample.Position;
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        // A cancelled contact adds nothing, but the points already placed stay: they were each a deliberate click.
        if (_points.Count > 0) _cursor = _points[^1];
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void Deactivate(IBinkToolContext context)
    {
        // A tool change or an interruption abandons what was not finished, as every tool's unfinished gesture is.
        Reset();
        context?.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override bool OnKeyDown(IBinkToolContext context, BinkToolKeyEvent key)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_points.Count == 0) return false;
        switch (key.Key)
        {
            case "Enter":
                Finish(context, closed: false);
                return true;
            case "Escape":
                Reset();
                context.Announce("Polyline abandoned.");
                context.InvalidateOverlay();
                return true;
            case "Backspace" or "Delete":
                _points.RemoveAt(_points.Count - 1);
                context.Announce(_points.Count == 0 ? "Polyline abandoned." : $"{_points.Count} points.");
                context.InvalidateOverlay();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Ends the polyline and adds it to the document when it has at least two points (three for a closed one).
    /// </summary>
    /// <param name="context">The tool context.</param>
    /// <param name="closed">Whether to close it into a polygon.</param>
    /// <returns>The element added, or null when there was not enough to add.</returns>
    public BinkPolygonShape? Finish(IBinkToolContext context, bool closed)
    {
        ArgumentNullException.ThrowIfNull(context);
        var points = new List<Vector2>(_points);
        Guid layer = _targetLayerId;
        Reset();
        context.InvalidateOverlay();
        if (points.Count < (closed ? 3 : 2))
        {
            context.Announce("A polyline needs at least two points.");
            return null;
        }
        var shape = new BinkPolygonShape(points, closed)
        {
            Style = context.ShapeStyle,
            Smooth = Smooth,
            EndArrow = !closed && EndArrow,
        };
        try
        {
            context.Document.AddElement(shape, layer);
        }
        catch (InvalidOperationException ex)
        {
            context.Announce("That shape could not be added: " + ex.Message);
            return null;
        }
        context.InvalidateDocument(shape.Bounds);
        context.Announce($"{shape.TypeName} of {points.Count} points added.");
        return shape;
    }

    private void Reset()
    {
        _points.Clear();
        _guides.Clear();
        _snapper = BinkSnapper.Disabled;
        _lastUpTime = double.NegativeInfinity;
    }

    /// <summary>Where a point lands: snapped to the grid and to other elements, or kept to 15-degree steps with Shift.</summary>
    private Vector2 Place(IBinkToolContext context, Vector2 point, bool constrain = false, List<BinkSnapGuide>? guides = null)
    {
        if (constrain && _points.Count > 0)
        {
            Vector2 from = _points[^1];
            Vector2 d = point - from;
            float step = MathF.PI / 12f;
            float angle = MathF.Round(MathF.Atan2(d.Y, d.X) / step) * step;
            return from + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * d.Length();
        }
        if (!_snapper.IsEnabled) return point;
        float tolerance = context.Snapping.Threshold / MathF.Max(context.Viewport.Zoom, 0.001f);
        return _snapper.SnapPoint(point, tolerance, guides: guides);
    }

    /// <inheritdoc />
    public override void DrawOverlay(IBinkToolContext context, IBinkRenderContext render)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(render);
        if (_points.Count == 0) return;
        float zoom = MathF.Max(context.Viewport.Zoom, 0.001f);
        BinkShapeStyle style = context.ShapeStyle;

        // What would be added now, with the segment to the pointer, drawn in the shape's own style.
        var preview = new List<Vector2>(_points);
        if (Vector2.Distance(_cursor, _points[^1]) * zoom > 2f) preview.Add(_cursor);
        if (preview.Count >= 2)
        {
            new BinkPolygonShape(preview, isClosed: false) { Style = style with { FillColor = null }, Smooth = Smooth, EndArrow = EndArrow }.Draw(render);
        }

        // The first point is ringed, so it is clear where to click to close the shape.
        var accent = new BinkColor(37, 99, 235);
        BinkPathGeometry ring = new BinkPathBuilder(8).AddCircle(_points[0], CloseDistance / zoom).ToGeometry();
        render.StrokePath(ring, new BinkStrokePaint(accent, 1.5f / zoom));
        BinkSnapper.DrawGuides(render, _guides, zoom);
    }
}
