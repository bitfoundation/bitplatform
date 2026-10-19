using System.Numerics;

namespace Bit.Bink.Demo.Client.Diagramming;

/// <summary>
/// Draws a diagram node by dragging out its box.
/// </summary>
/// <remarks>
/// A custom tool needs nothing from the library beyond <see cref="BinkToolBase"/>: it reads pointer samples, draws
/// its own preview on the overlay, and adds an ordinary element when the gesture ends. Everything else - undo,
/// selection, hit testing, serialization, export - follows because the result is a normal document element.
/// </remarks>
public sealed class NodeTool : BinkToolBase
{
    private bool _dragging;
    private int _pointerId = -1;
    private Vector2 _start;
    private Vector2 _current;

    /// <inheritdoc />
    public override string Id => "diagram-node";

    /// <inheritdoc />
    public override string DisplayName => "Node";

    /// <inheritdoc />
    public override string Cursor => "crosshair";

    /// <summary>Smallest box a drag can produce, so a stray click still makes a usable node.</summary>
    public Vector2 MinimumSize { get; set; } = new(90, 48);

    /// <summary>Supplies the label of each new node, or null for unlabelled nodes.</summary>
    public Func<string?>? NextLabel { get; set; }

    /// <inheritdoc />
    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_dragging) return false;
        _dragging = true;
        _pointerId = sample.PointerId;
        _start = context.Viewport.ToDocument(sample.Position);
        _current = _start;
        context.CapturePointer(sample.PointerId);
        context.InvalidateOverlay();
        return true;
    }

    /// <inheritdoc />
    public override void OnPointerMove(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_dragging || sample.PointerId != _pointerId) return;
        _current = context.Viewport.ToDocument(sample.Position);
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerUp(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_dragging || sample.PointerId != _pointerId) return;
        _current = context.Viewport.ToDocument(sample.Position);
        context.ReleasePointer(sample.PointerId);
        _dragging = false;
        _pointerId = -1;

        BinkRect box = BinkRect.FromPoints(_start, _current);
        if (box.Width < MinimumSize.X || box.Height < MinimumSize.Y)
        {
            // A click, not a drag: place a default-sized node centred where the pointer went down.
            box = BinkRect.FromCenter(_start, MinimumSize.X, MinimumSize.Y);
        }

        try
        {
            context.Document.AddElement(DiagramGraph.CreateNode(box, context.ShapeStyle, NextLabel?.Invoke()), context.ActiveLayerId);
            context.Announce("Node added.");
        }
        catch (InvalidOperationException ex)
        {
            context.Announce("The node could not be added: " + ex.Message);
        }
        context.InvalidateDocument();
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_dragging || sample.PointerId != _pointerId) return;
        context.ReleasePointer(sample.PointerId);
        _dragging = false;
        _pointerId = -1;
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void Deactivate(IBinkToolContext context)
    {
        _dragging = false;
        _pointerId = -1;
        context?.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void DrawOverlay(IBinkToolContext context, IBinkRenderContext render)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(render);
        if (!_dragging) return;
        BinkRect box = BinkRect.FromPoints(_start, _current);
        if (box.IsEmpty) return;
        var path = new BinkPathBuilder(8).AddRoundedRect(box, 8f).ToGeometry();
        render.FillPath(path, new BinkFillPaint(new BinkColor(37, 99, 235, 30)));
        render.StrokePath(path, new BinkStrokePaint(new BinkColor(37, 99, 235), 1.5f, 0.9f));
    }
}

/// <summary>
/// Joins two nodes with a connector by dragging from one to the other.
/// </summary>
/// <remarks>
/// The connector stores the ids of the nodes it joins in element metadata, which the file format preserves.
/// <see cref="DiagramGraph"/> reads that metadata to reroute the connector whenever a node moves.
/// </remarks>
public sealed class ConnectorTool : BinkToolBase
{
    private BinkElement? _from;
    private int _pointerId = -1;
    private Vector2 _current;

    /// <inheritdoc />
    public override string Id => "diagram-connector";

    /// <inheritdoc />
    public override string DisplayName => "Connector";

    /// <inheritdoc />
    public override string Cursor => "alias";

    /// <summary>The node the drag started on, or null when no connector is being drawn.</summary>
    public BinkElement? From => _from;

    /// <inheritdoc />
    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_from is not null) return false;
        Vector2 point = context.Viewport.ToDocument(sample.Position);
        BinkElement? node = NodeAt(context.Document, point);
        if (node is null)
        {
            context.Announce("Start a connector on a node.");
            return false;
        }
        _from = node;
        _pointerId = sample.PointerId;
        _current = point;
        context.CapturePointer(sample.PointerId);
        context.InvalidateOverlay();
        return true;
    }

    /// <inheritdoc />
    public override void OnPointerMove(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_from is null || sample.PointerId != _pointerId) return;
        _current = context.Viewport.ToDocument(sample.Position);
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerUp(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_from is null || sample.PointerId != _pointerId) return;
        context.ReleasePointer(sample.PointerId);

        Vector2 point = context.Viewport.ToDocument(sample.Position);
        BinkElement? target = NodeAt(context.Document, point);
        BinkElement from = _from;
        _from = null;
        _pointerId = -1;

        if (target is null || target.Id == from.Id)
        {
            context.Announce("A connector needs a different node to end on.");
            context.InvalidateOverlay();
            return;
        }

        try
        {
            // The connector goes under the nodes so the boxes stay readable where a line meets them.
            BinkLineShape connector = DiagramGraph.CreateConnector(from, target, context.ShapeStyle);
            using (context.Document.BeginBatch("Connect nodes"))
            {
                context.Document.AddElement(connector, context.ActiveLayerId);
                if (context.Document.GetLayerOf(connector.Id) is { } layer)
                {
                    context.Document.Apply(new BinkReorderElementOperation(connector.Id, layer.Id, 0));
                }
            }
            context.Announce("Nodes connected.");
        }
        catch (InvalidOperationException ex)
        {
            context.Announce("The connector could not be added: " + ex.Message);
        }
        context.InvalidateDocument();
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_from is null || sample.PointerId != _pointerId) return;
        context.ReleasePointer(sample.PointerId);
        _from = null;
        _pointerId = -1;
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void Deactivate(IBinkToolContext context)
    {
        _from = null;
        _pointerId = -1;
        context?.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void DrawOverlay(IBinkToolContext context, IBinkRenderContext render)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(render);
        if (_from is null) return;
        (Vector2 start, _) = DiagramGraph.Route(_from.Bounds, BinkRect.FromCenter(_current, 1, 1));
        var path = new BinkPathBuilder(4);
        path.MoveTo(start);
        path.LineTo(_current);
        render.StrokePath(path.ToGeometry(), new BinkStrokePaint(new BinkColor(37, 99, 235), 2f, 0.8f, BinkBlendMode.Normal, BinkLineCap.Round, BinkLineJoin.Round, [6f, 4f]));
    }

    /// <summary>The top-most node under a point, ignoring connectors and anything else on the surface.</summary>
    public static BinkElement? NodeAt(BinkDocument document, Vector2 point)
    {
        ArgumentNullException.ThrowIfNull(document);
        BinkElement? found = null;
        foreach (BinkElement element in document.AllElements)
        {
            if (!DiagramGraph.HasRole(element, DiagramGraph.NodeRole)) continue;
            if (element.HitTest(point, 2f)) found = element;
        }
        return found;
    }
}
