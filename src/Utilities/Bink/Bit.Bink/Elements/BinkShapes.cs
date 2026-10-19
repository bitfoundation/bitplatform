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

    /// <summary>Paint for the outline.</summary>
    public BinkStrokePaint ToStrokePaint() => new(StrokeColor, StrokeWidth, Opacity, BlendMode, BinkLineCap.Round, BinkLineJoin.Round, Dash);

    /// <summary>Paint for the fill.</summary>
    public BinkFillPaint ToFillPaint() => new(FillColor ?? BinkColor.Transparent, Opacity, BlendMode);

    /// <inheritdoc />
    public bool Equals(BinkShapeStyle? other)
        => other is not null && StrokeColor == other.StrokeColor && StrokeWidth == other.StrokeWidth && FillColor == other.FillColor
           && Opacity == other.Opacity && BlendMode == other.BlendMode && (Dash ?? []).AsSpan().SequenceEqual(other.Dash ?? []);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(StrokeColor, StrokeWidth, FillColor, Opacity, BlendMode);
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
    public override BinkRect LocalBounds => GeometryBounds.Inflate(Style.StrokeWidth / 2f + 1f);

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
    {
        BinkPathGeometry path = GetPath();
        float radius = MathF.Max(Style.StrokeWidth / 2f, 1.5f) + tolerance;
        float r2 = radius * radius;
        bool hit = false;
        bool filled = Style.FillColor is { IsTransparent: false };
        path.Flatten(0.5f, (pts, closed) =>
        {
            if (hit) return;
            ReadOnlySpan<Vector2> p = pts.Span;
            for (int i = 1; i < p.Length; i++)
            {
                if (BinkGeometryMath.DistanceToSegmentSquared(localPoint, p[i - 1], p[i]) <= r2) { hit = true; return; }
            }
            if (closed && p.Length > 2)
            {
                if (BinkGeometryMath.DistanceToSegmentSquared(localPoint, p[^1], p[0]) <= r2) { hit = true; return; }
                if (filled && BinkGeometryMath.PointInPolygon(localPoint, p)) hit = true;
            }
        });
        return hit;
    }
}

/// <summary>A rectangle annotation, optionally with rounded corners.</summary>
public sealed record BinkRectangleShape : BinkShapeElement
{
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
    public override BinkPathGeometry GetPath() => CornerRadius > 0 ? new BinkPathBuilder(12).AddRoundedRect(Rect, CornerRadius).ToGeometry() : new BinkPathBuilder(6).AddRect(Rect).ToGeometry();

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => HitTestPath(localPoint, tolerance);
}

/// <summary>An ellipse annotation inscribed in a rectangle.</summary>
public sealed record BinkEllipseShape : BinkShapeElement
{
    /// <summary>Creates an ellipse.</summary>
    public BinkEllipseShape(BinkRect rect) { Rect = rect.Normalize(); }

    /// <summary>Bounding rectangle in local coordinates.</summary>
    public BinkRect Rect { get; init; }

    /// <inheritdoc />
    public override string TypeName => "Ellipse";

    /// <inheritdoc />
    protected override BinkRect GeometryBounds => Rect;

    /// <inheritdoc />
    public override BinkPathGeometry GetPath() => new BinkPathBuilder(8).AddEllipse(Rect).ToGeometry();

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => HitTestPath(localPoint, tolerance);
}

/// <summary>A straight line annotation with optional arrow heads at either end.</summary>
public sealed record BinkLineShape : BinkShapeElement
{
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
    public override BinkPathGeometry GetPath()
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
        context.StrokePath(GetPath(), Style.ToStrokePaint() with { Join = BinkLineJoin.Round, Cap = BinkLineCap.Round });
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance)
    {
        float radius = MathF.Max(Style.StrokeWidth / 2f, 2f) + tolerance;
        return BinkGeometryMath.DistanceToSegmentSquared(localPoint, Start, End) <= radius * radius;
    }

    /// <inheritdoc />
    public override bool IntersectsRect(BinkRect rect)
    {
        if (!Bounds.Intersects(rect)) return false;
        Vector2 s = Vector2.Transform(Start, Transform);
        Vector2 e = Vector2.Transform(End, Transform);
        return BinkGeometryMath.SegmentIntersectsRect(s, e, rect.Inflate(Style.StrokeWidth / 2f));
    }
}
