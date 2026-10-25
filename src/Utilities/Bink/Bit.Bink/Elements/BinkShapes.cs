using System.Numerics;

namespace Bit.Bink;

/// <summary>Visual style shared by shape annotations.</summary>
public sealed record BinkShapeStyle
{
    /// <summary>A 2-unit black outline with no fill.</summary>
    public static readonly BinkShapeStyle Default = new();

    /// <summary>Outline color.</summary>
    public BinkColor StrokeColor { get; init; } = BinkColor.Black;

    /// <summary>Outline width in local units; 0 disables the outline.</summary>
    public float StrokeWidth { get; init; } = 2f;

    /// <summary>Fill color, or null for no fill.</summary>
    public BinkColor? FillColor { get; init; }

    /// <summary>Opacity 0..1 applied to the whole shape.</summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>Dash pattern (on, off, ...) in local units, or null for a solid line.</summary>
    public float[]? Dash { get; init; }

    /// <summary>Blend mode.</summary>
    public BinkBlendMode BlendMode { get; init; } = BinkBlendMode.Normal;

    /// <summary>How the ends of open outlines are drawn. Round by default, which is what hand-drawn annotation looks like.</summary>
    public BinkLineCap LineCap { get; init; } = BinkLineCap.Round;

    /// <summary>How corners are drawn. Round by default; <see cref="BinkLineJoin.Miter"/> keeps a rectangle's corners sharp.</summary>
    public BinkLineJoin LineJoin { get; init; } = BinkLineJoin.Round;

    /// <summary>Paint for the outline.</summary>
    public BinkStrokePaint ToStrokePaint() => new(StrokeColor, StrokeWidth, Opacity, BlendMode, LineCap, LineJoin, Dash);

    /// <summary>Paint for the fill.</summary>
    public BinkFillPaint ToFillPaint() => new(FillColor ?? BinkColor.Transparent, Opacity, BlendMode);

    /// <inheritdoc />
    public bool Equals(BinkShapeStyle? other)
        => other is not null && StrokeColor == other.StrokeColor && StrokeWidth == other.StrokeWidth && FillColor == other.FillColor
           && Opacity == other.Opacity && BlendMode == other.BlendMode && LineCap == other.LineCap && LineJoin == other.LineJoin
           && (Dash ?? []).AsSpan().SequenceEqual(other.Dash ?? []);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(StrokeColor, StrokeWidth, FillColor, Opacity, BlendMode, LineCap, LineJoin);
}

/// <summary>Base for shape annotations that share a <see cref="BinkShapeStyle"/>.</summary>
public abstract record BinkShapeElement : BinkElement
{
    /// <summary>Visual style.</summary>
    public BinkShapeStyle Style { get; init; } = BinkShapeStyle.Default;

    /// <summary>Builds the shape's path in local coordinates.</summary>
    public abstract BinkPathGeometry GetPath();

    /// <summary>Bounds of the geometry without stroke width.</summary>
    protected abstract BinkRect GeometryBounds { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Round joins and caps stay within half the outline's width; square caps reach the corner of a half-width square,
    /// and a miter join can reach the miter limit (4, the SVG and canvas default) at a sharp corner.
    /// </remarks>
    public override BinkRect LocalBounds
    {
        get
        {
            float reach = Style.LineJoin == BinkLineJoin.Miter ? 4f : Style.LineCap == BinkLineCap.Square ? 1.4143f : 1f;
            return GeometryBounds.Inflate(Style.StrokeWidth / 2f * reach + 1f);
        }
    }

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context)
    {
        BinkPathGeometry path = GetPath();
        if (path.IsEmpty) return;
        if (Style.FillColor is { } fill && !fill.IsTransparent) context.FillPath(path, Style.ToFillPaint());
        if (Style.StrokeWidth > 0) context.StrokePath(path, Style.ToStrokePaint());
    }

    /// <summary>Distance-to-outline hit test shared by shapes; fill hit counts when the shape is filled.</summary>
    protected bool HitTestPath(Vector2 localPoint, float tolerance)
        => HitsOutline(GetPath(), MathF.Max(Style.StrokeWidth / 2f, 1.5f), Style.FillColor is { IsTransparent: false }, CombinedFillRule, localPoint, localPoint, tolerance, null, default);

    /// <summary>
    /// How the fill is hit, which has to match how it is drawn. Null fills each closed sub-path on its own, as the
    /// built-in shapes are drawn; a rule fills every sub-path together, open ones closed implicitly, as a canvas fills
    /// a path.
    /// </summary>
    private protected virtual BinkFillRule? CombinedFillRule => null;

    /// <summary>
    /// Measures the outline in the document under a stretching transform, where an outline drawn at one width in
    /// local units is wider across a stretch than along it. Whether a point is inside the fill does not depend on
    /// the transform, so that is still decided in local coordinates.
    /// </summary>
    protected override bool HitTestStretched(Vector2 point, float tolerance)
    {
        if (!Matrix3x2.Invert(Transform, out Matrix3x2 inverse)) return false;
        Vector2 local = Vector2.Transform(point, inverse);
        return HitsOutline(GetPath(), MathF.Max(Style.StrokeWidth / 2f, 1.5f), Style.FillColor is { IsTransparent: false }, CombinedFillRule, local, point, tolerance, Transform, inverse);
    }

    /// <summary>
    /// Whether a point is within <paramref name="tolerance"/> of a flattened outline of half-width
    /// <paramref name="halfWidth"/>, or inside it when it is filled. Without a transform everything is measured in
    /// local units; with one, the outline is measured in the document.
    /// </summary>
    private static bool HitsOutline(BinkPathGeometry path, float halfWidth, bool filled, BinkFillRule? combined, Vector2 localPoint, Vector2 documentPoint, float tolerance, Matrix3x2? transform, Matrix3x2 inverse)
    {
        bool hit = false;
        int winding = 0;
        path.Flatten(0.5f, (pts, closed) =>
        {
            if (hit) return;
            ReadOnlySpan<Vector2> p = pts.Span;
            for (int i = 1; i < p.Length && !hit; i++) hit = Near(p[i - 1], p[i]);
            if (!hit && closed && p.Length > 2) hit = Near(p[^1], p[0]);
            if (hit || !filled || p.Length < 3) return;
            if (combined is null)
            {
                if (closed) hit = BinkGeometryMath.PointInPolygon(localPoint, p);
            }
            else
            {
                winding += BinkGeometryMath.WindingNumber(localPoint, p);
            }
        });
        if (!hit && filled && combined is { } rule) hit = rule == BinkFillRule.EvenOdd ? (winding & 1) != 0 : winding != 0;
        return hit;

        bool Near(Vector2 a, Vector2 b)
        {
            if (transform is not { } m)
            {
                float r = halfWidth + tolerance;
                return BinkGeometryMath.DistanceToSegmentSquared(localPoint, a, b) <= r * r;
            }
            return BinkGeometryMath.NearTransformedSegment(documentPoint, a, b, m, inverse, halfWidth, tolerance);
        }
    }
}

/// <summary>A rectangle annotation, optionally with rounded corners.</summary>
public sealed record BinkRectangleShape : BinkShapeElement
{
    private BinkCachedPath<(BinkRect, float)>? _path;

    /// <summary>Creates a rectangle.</summary>
    public BinkRectangleShape(BinkRect rect) { Rect = rect.Normalize(); }

    /// <summary>Geometry in local coordinates.</summary>
    public BinkRect Rect { get; init; }

    /// <summary>Corner radius in local units.</summary>
    public float CornerRadius { get; init; }

    /// <inheritdoc />
    public override string TypeName => "Rectangle";

    /// <inheritdoc />
    protected override BinkRect GeometryBounds => Rect;

    /// <inheritdoc />
    /// <remarks>
    /// Built once per geometry and reused by every redraw, hit test and export. The canvas keeps a drawn path in
    /// the browser and refers to it by identity, so a shape that rebuilt its path on every draw would resend it
    /// every frame; moving the shape changes only its transform and keeps the path.
    /// </remarks>
    public override BinkPathGeometry GetPath()
    {
        (BinkRect, float) key = (Rect, CornerRadius);
        if (_path is { } cached && cached.Matches(key)) return cached.Path;
        BinkPathGeometry path = CornerRadius > 0 ? new BinkPathBuilder(12).AddRoundedRect(Rect, CornerRadius).ToGeometry() : new BinkPathBuilder(6).AddRect(Rect).ToGeometry();
        _path = new(key, path);
        return path;
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => HitTestPath(localPoint, tolerance);
}

/// <summary>An ellipse annotation inscribed in a rectangle.</summary>
public sealed record BinkEllipseShape : BinkShapeElement
{
    private BinkCachedPath<BinkRect>? _path;

    /// <summary>Creates an ellipse.</summary>
    public BinkEllipseShape(BinkRect rect) { Rect = rect.Normalize(); }

    /// <summary>Bounding rectangle in local coordinates.</summary>
    public BinkRect Rect { get; init; }

    /// <inheritdoc />
    public override string TypeName => "Ellipse";

    /// <inheritdoc />
    protected override BinkRect GeometryBounds => Rect;

    /// <inheritdoc />
    /// <remarks>Built once per geometry and reused; see <see cref="BinkRectangleShape.GetPath"/>.</remarks>
    public override BinkPathGeometry GetPath()
    {
        if (_path is { } cached && cached.Matches(Rect)) return cached.Path;
        BinkPathGeometry path = new BinkPathBuilder(8).AddEllipse(Rect).ToGeometry();
        _path = new(Rect, path);
        return path;
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => HitTestPath(localPoint, tolerance);
}

/// <summary>A straight line annotation with optional arrow heads at either end.</summary>
public sealed record BinkLineShape : BinkShapeElement
{
    private BinkCachedPath<(Vector2, Vector2, bool, bool, float, float)>? _path;

    /// <summary>Creates a line.</summary>
    public BinkLineShape(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }

    /// <summary>Start point in local coordinates.</summary>
    public Vector2 Start { get; init; }

    /// <summary>End point in local coordinates.</summary>
    public Vector2 End { get; init; }

    /// <summary>Draw an arrow head at the start.</summary>
    public bool StartArrow { get; init; }

    /// <summary>Draw an arrow head at the end.</summary>
    public bool EndArrow { get; init; }

    /// <summary>Arrow head length as a multiple of the stroke width (default 5).</summary>
    public float ArrowSize { get; init; } = 5f;

    /// <inheritdoc />
    public override string TypeName => StartArrow || EndArrow ? "Arrow" : "Line";

    /// <inheritdoc />
    protected override BinkRect GeometryBounds => BinkRect.FromPoints(Start, End).Inflate(HeadLength);

    private float HeadLength => (StartArrow || EndArrow) ? MathF.Max(Style.StrokeWidth, 1f) * ArrowSize : 0f;

    /// <inheritdoc />
    /// <remarks>Built once per geometry and reused; see <see cref="BinkRectangleShape.GetPath"/>.</remarks>
    public override BinkPathGeometry GetPath()
    {
        // The arrow heads are sized from the stroke width, so it is part of the geometry's identity.
        (Vector2, Vector2, bool, bool, float, float) key = (Start, End, StartArrow, EndArrow, ArrowSize, Style.StrokeWidth);
        if (_path is { } cached && cached.Matches(key)) return cached.Path;
        BinkPathGeometry path = BuildPath();
        _path = new(key, path);
        return path;
    }

    private BinkPathGeometry BuildPath()
    {
        var b = new BinkPathBuilder(12);
        b.MoveTo(Start).LineTo(End);
        float len = HeadLength;
        Vector2 dir = BinkGeometryMath.SafeNormalize(End - Start);
        if (dir != Vector2.Zero && len > 0)
        {
            if (EndArrow) AddHead(b, End, dir, len);
            if (StartArrow) AddHead(b, Start, -dir, len);
        }
        return b.ToGeometry();
    }

    private static void AddHead(BinkPathBuilder b, Vector2 tip, Vector2 dir, float len)
    {
        const float halfAngle = 0.46f; // ~26 degrees
        Vector2 left = tip - BinkGeometryMath.Rotate(dir, halfAngle) * len;
        Vector2 right = tip - BinkGeometryMath.Rotate(dir, -halfAngle) * len;
        b.MoveTo(left).LineTo(tip).LineTo(right);
    }

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context)
    {
        // Lines are never filled; arrow heads are open strokes so they match the line weight.
        if (Style.StrokeWidth <= 0) return;
        context.StrokePath(GetPath(), Style.ToStrokePaint());
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance)
    {
        float radius = MathF.Max(Style.StrokeWidth / 2f, 2f) + tolerance;
        return BinkGeometryMath.DistanceToSegmentSquared(localPoint, Start, End) <= radius * radius;
    }

    /// <summary>The line measured in the document under a stretching transform; see <see cref="BinkElement.HitTestStretched"/>.</summary>
    protected override bool HitTestStretched(Vector2 point, float tolerance)
        => Matrix3x2.Invert(Transform, out Matrix3x2 inverse)
           && BinkGeometryMath.NearTransformedSegment(point, Start, End, Transform, inverse, MathF.Max(Style.StrokeWidth / 2f, 2f), tolerance);

    /// <inheritdoc />
    public override bool IntersectsRect(BinkRect rect)
    {
        if (!Bounds.Intersects(rect)) return false;
        Vector2 s = Vector2.Transform(Start, Transform);
        Vector2 e = Vector2.Transform(End, Transform);
        // The line is drawn as wide as the transform makes it, which an even scale multiplies and a stretch across it widens.
        return BinkGeometryMath.SegmentIntersectsRect(s, e, rect.Inflate(BinkGeometryMath.TransformedHalfWidth(Start, End, Transform, Style.StrokeWidth / 2f)));
    }
}
