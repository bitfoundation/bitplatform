using System.Numerics;

namespace Bit.Bink;

/// <summary>The shape a <see cref="BinkShapeTool"/> draws.</summary>
public enum BinkShapeKind : byte
{
    /// <summary>A rectangle.</summary>
    Rectangle = 0,
    /// <summary>An ellipse inscribed in the dragged rectangle.</summary>
    Ellipse = 1,
    /// <summary>A straight line.</summary>
    Line = 2,
    /// <summary>A line with an arrow head at the end.</summary>
    Arrow = 3,
    /// <summary>A translucent highlight rectangle.</summary>
    Highlight = 4,
}

/// <summary>
/// Draws shape annotations by dragging: rectangle, ellipse, line, arrow and highlight. Holding Shift constrains
/// to a square/circle or to 15-degree angles for lines.
/// </summary>
public sealed class BinkShapeTool : BinkToolBase
{
    private bool _dragging;
    private int _pointerId = -1;
    private Vector2 _start;
    private Vector2 _current;
    private bool _constrain;
    private Guid _targetLayerId;

    /// <summary>Creates a shape tool.</summary>
    /// <param name="kind">Which shape to draw.</param>
    public BinkShapeTool(BinkShapeKind kind = BinkShapeKind.Rectangle) { Kind = kind; }

    /// <summary>Which shape is drawn.</summary>
    public BinkShapeKind Kind { get; set; }

    /// <summary>Corner radius for rectangles, in document units.</summary>
    public float CornerRadius { get; set; }

    /// <summary>Whether the tool stays active after drawing a shape (otherwise the host usually switches to select).</summary>
    public bool Sticky { get; set; } = true;

    /// <inheritdoc />
    public override string Id => "shape-" + Kind.ToString().ToLowerInvariant();

    /// <inheritdoc />
    public override string DisplayName => Kind.ToString();

    /// <summary>True while a shape is being dragged.</summary>
    public bool IsDrawing => _dragging;

    /// <inheritdoc />
    public override bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_dragging) return false;
        if (context.Document.TryGetLayer(context.ActiveLayerId, out BinkLayer? layer) && (layer.IsLocked || !layer.IsVisible)) return false;
        _dragging = true;
        _pointerId = sample.PointerId;
        // The shape belongs to the layer the gesture started on, even if the active layer changes meanwhile.
        _targetLayerId = context.ActiveLayerId;
        _start = context.Viewport.ToDocument(sample.Position);
        _current = _start;
        _constrain = sample.Shift;
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
        _constrain = sample.Shift;
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerUp(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_dragging || sample.PointerId != _pointerId) return;
        context.ReleasePointer(sample.PointerId);
        _current = context.Viewport.ToDocument(sample.Position);
        _constrain = sample.Shift;
        _dragging = false;
        _pointerId = -1;

        BinkElement? element = Build(context);
        if (element is not null)
        {
            try
            {
                context.Document.AddElement(element, _targetLayerId);
                context.InvalidateDocument(element.Bounds);
                context.Announce($"{element.TypeName} added.");
            }
            catch (InvalidOperationException ex)
            {
                context.Announce("That shape could not be added: " + ex.Message);
            }
        }
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample)
    {
        ArgumentNullException.ThrowIfNull(context);
        // Only the contact that started the drag can end it; see DrawTool.OnPointerCancel.
        if (!_dragging || sample.PointerId != _pointerId) return;
        context.ReleasePointer(sample.PointerId);
        _dragging = false;
        _pointerId = -1;
        context.InvalidateOverlay();
    }

    /// <inheritdoc />
    public override void Deactivate(IBinkToolContext context)
    {
        if (!_dragging) return;
        _dragging = false;
        _pointerId = -1;
        context?.InvalidateOverlay();
    }

    private (Vector2 Start, Vector2 End) GetPoints()
    {
        Vector2 end = _current;
        if (_constrain)
        {
            Vector2 d = end - _start;
            if (Kind is BinkShapeKind.Line or BinkShapeKind.Arrow)
            {
                // Snap to 15 degree increments.
                float angle = MathF.Atan2(d.Y, d.X);
                float step = MathF.PI / 12f;
                angle = MathF.Round(angle / step) * step;
                float len = d.Length();
                end = _start + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * len;
            }
            else
            {
                float size = MathF.Max(MathF.Abs(d.X), MathF.Abs(d.Y));
                end = _start + new Vector2(MathF.CopySign(size, d.X == 0 ? 1 : d.X), MathF.CopySign(size, d.Y == 0 ? 1 : d.Y));
            }
        }
        return (_start, end);
    }

    private BinkElement? Build(IBinkToolContext context)
    {
        (Vector2 start, Vector2 end) = GetPoints();
        BinkRect rect = BinkRect.FromPoints(start, end);
        float minimum = 2f / MathF.Max(context.Viewport.Zoom, 0.001f);
        bool tiny = rect.Width < minimum && rect.Height < minimum;
        BinkShapeStyle style = context.ShapeStyle;

        switch (Kind)
        {
            case BinkShapeKind.Rectangle:
                return tiny ? null : new BinkRectangleShape(rect) { Style = style, CornerRadius = CornerRadius };
            case BinkShapeKind.Ellipse:
                return tiny ? null : new BinkEllipseShape(rect) { Style = style };
            case BinkShapeKind.Line:
                return tiny ? null : new BinkLineShape(start, end) { Style = style };
            case BinkShapeKind.Arrow:
                return tiny ? null : new BinkLineShape(start, end) { Style = style, EndArrow = true };
            case BinkShapeKind.Highlight:
                return tiny ? null : new BinkHighlightElement(rect) { Color = style.StrokeColor, Opacity = 0.45f };
            default:
                return null;
        }
    }

    /// <inheritdoc />
    public override void DrawOverlay(IBinkToolContext context, IBinkRenderContext render)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_dragging) return;
        BinkElement? preview = Build(context);
        preview?.Draw(render);
    }

    /// <inheritdoc />
    public override bool OnKeyDown(IBinkToolContext context, BinkToolKeyEvent key)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (key.Key == "Escape" && _dragging)
        {
            _dragging = false;
            _pointerId = -1;
            context.InvalidateOverlay();
            return true;
        }
        return false;
    }
}
