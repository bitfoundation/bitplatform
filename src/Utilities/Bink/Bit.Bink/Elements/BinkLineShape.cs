using System.Numerics;

namespace Bit.Bink;

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
