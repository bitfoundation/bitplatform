using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// A shape through a list of points: a closed polygon (a diamond, a triangle, a star, any outline) or an open
/// polyline, optionally smoothed into a curve through its points, and optionally with arrow heads at the ends of an
/// open one, which is what an elbow connector in a diagram is.
/// </summary>
public sealed record BinkPolygonShape : BinkShapeElement
{
    /// <summary>The most points a polygon may have.</summary>
    public const int MaxPoints = 10_000;

    private readonly Vector2[] _points = [];
    private BinkCachedPath<(Vector2[], bool, bool, bool, bool, float, float)>? _path;

    /// <summary>Creates a polygon or polyline.</summary>
    /// <param name="points">At least two points, in local coordinates.</param>
    /// <param name="isClosed">True for a closed outline that can be filled; false for an open polyline.</param>
    public BinkPolygonShape(IEnumerable<Vector2> points, bool isClosed = true)
    {
        ArgumentNullException.ThrowIfNull(points);
        Points = [.. points];
        IsClosed = isClosed;
    }

    /// <summary>The points in local coordinates. Copied on assignment, so a caller cannot change them afterwards.</summary>
    /// <exception cref="ArgumentException">Fewer than two points, more than <see cref="MaxPoints"/>, or a point that is not finite.</exception>
    public IReadOnlyList<Vector2> Points
    {
        get => _points;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Count < 2) throw new ArgumentException("A polygon needs at least two points.", nameof(value));
            if (value.Count > MaxPoints) throw new ArgumentException($"A polygon can have at most {MaxPoints} points.", nameof(value));
            Vector2[] points = [.. value];
            foreach (Vector2 p in points)
            {
                if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) throw new ArgumentException("Polygon points must be finite.", nameof(value));
            }
            _points = points;
        }
    }

    /// <summary>Whether the outline returns to its first point and can be filled. An open polyline is never filled.</summary>
    public bool IsClosed { get; init; } = true;

    /// <summary>Whether the outline is a smooth curve through the points rather than straight segments between them.</summary>
    public bool Smooth { get; init; }

    /// <summary>Draw an arrow head at the first point. Open polylines only.</summary>
    public bool StartArrow { get; init; }

    /// <summary>Draw an arrow head at the last point. Open polylines only.</summary>
    public bool EndArrow { get; init; }

    /// <summary>Arrow head length as a multiple of the stroke width (default 5).</summary>
    public float ArrowSize { get; init; } = 5f;

    /// <inheritdoc />
    public override string TypeName => IsClosed ? "Polygon" : StartArrow || EndArrow ? "Arrow" : "Polyline";

    private bool HasArrows => !IsClosed && (StartArrow || EndArrow);

    private float HeadLength => HasArrows ? MathF.Max(Style.StrokeWidth, 1f) * ArrowSize : 0f;

    /// <inheritdoc />
    protected override BinkRect GeometryBounds => GetPath().Bounds.Inflate(HeadLength);

    /// <inheritdoc />
    /// <remarks>Built once per geometry and reused; see <see cref="BinkRectangleShape.GetPath"/>.</remarks>
    public override BinkPathGeometry GetPath()
    {
        // The arrow heads are sized from the stroke width, so it is part of the geometry's identity, as for lines.
        (Vector2[], bool, bool, bool, bool, float, float) key = (_points, IsClosed, Smooth, StartArrow, EndArrow, ArrowSize, HasArrows ? Style.StrokeWidth : 0f);
        if (_path is { } cached && cached.Matches(key)) return cached.Path;
        BinkPathGeometry path = BuildPath();
        _path = new(key, path);
        return path;
    }

    private BinkPathGeometry BuildPath()
    {
        var b = new BinkPathBuilder(_points.Length * 4 + 12);
        ReadOnlySpan<Vector2> points = _points;
        if (IsClosed)
        {
            if (Smooth) b.AddClosedCatmullRom(points);
            else b.AddPolygon(points);
            return b.ToGeometry();
        }
        if (Smooth) b.AddCatmullRom(points);
        else b.AddPolyline(points);
        float length = HeadLength;
        if (length > 0)
        {
            // The heads follow the last and first segments, which is also the direction a smooth curve leaves its ends.
            if (EndArrow) AddHead(b, points[^1], Direction(points, ^1), length);
            if (StartArrow) AddHead(b, points[0], Direction(points, 0), length);
        }
        return b.ToGeometry();
    }

    private static Vector2 Direction(ReadOnlySpan<Vector2> points, Index end)
    {
        // Pointing out of the shape at that end, past any repeated points.
        int at = end.GetOffset(points.Length);
        int step = end.IsFromEnd ? -1 : 1;
        for (int i = at + step; i >= 0 && i < points.Length; i += step)
        {
            Vector2 d = BinkGeometryMath.SafeNormalize(points[at] - points[i]);
            if (d != Vector2.Zero) return d;
        }
        return Vector2.Zero;
    }

    private static void AddHead(BinkPathBuilder b, Vector2 tip, Vector2 dir, float len)
    {
        if (dir == Vector2.Zero) return;
        const float halfAngle = 0.46f; // ~26 degrees, as for lines
        b.MoveTo(tip - BinkGeometryMath.Rotate(dir, halfAngle) * len).LineTo(tip).LineTo(tip - BinkGeometryMath.Rotate(dir, -halfAngle) * len);
    }

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context)
    {
        if (IsClosed)
        {
            base.DrawCore(context);
            return;
        }
        // An open polyline is never filled; its arrow heads are open strokes so they match the line weight.
        if (Style.StrokeWidth <= 0) return;
        context.StrokePath(GetPath(), Style.ToStrokePaint());
    }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => HitTestPath(localPoint, tolerance);

    /// <inheritdoc />
    /// <remarks>
    /// Tested against the outline in document space, so a marquee across the empty corner of a diagonal shape's
    /// bounds does not select it. A filled polygon is also caught by a marquee drawn inside it.
    /// </remarks>
    public override bool IntersectsRect(BinkRect rect)
    {
        if (!Bounds.Intersects(rect)) return false;
        if (rect.Contains(Bounds)) return true;
        Matrix3x2 m = Transform;
        float half = Style.StrokeWidth / 2f;
        bool filled = IsClosed && Style.FillColor is { IsTransparent: false };
        bool hit = false;
        GetPath().Flatten(0.5f, (pts, closed) =>
        {
            if (hit) return;
            ReadOnlySpan<Vector2> p = pts.Span;
            for (int i = 1; i < p.Length && !hit; i++) hit = Crosses(p[i - 1], p[i]);
            if (!hit && closed && p.Length > 2) hit = Crosses(p[^1], p[0]);
            if (!hit && filled && closed && p.Length > 2 && Matrix3x2.Invert(m, out Matrix3x2 inverse))
            {
                hit = BinkGeometryMath.PointInPolygon(Vector2.Transform(rect.Center, inverse), p);
            }
        });
        return hit;

        bool Crosses(Vector2 a, Vector2 b)
            => BinkGeometryMath.SegmentIntersectsRect(Vector2.Transform(a, m), Vector2.Transform(b, m), rect.Inflate(BinkGeometryMath.TransformedHalfWidth(a, b, m, half)));
    }

    /// <summary>
    /// A regular polygon with <paramref name="sides"/> sides, stretched to fill <paramref name="rect"/>, with a vertex
    /// at the top (a triangle points up, a hexagon has flat sides left and right).
    /// </summary>
    /// <param name="rect">The box the polygon fills.</param>
    /// <param name="sides">The number of sides, at least three.</param>
    public static BinkPolygonShape Regular(BinkRect rect, int sides)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 3);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sides, MaxPoints);
        var points = new Vector2[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = -MathF.PI / 2f + i * MathF.Tau / sides;
            points[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }
        return new BinkPolygonShape(FitTo(points, rect));
    }

    /// <summary>A diamond: the four edge midpoints of <paramref name="rect"/>, the decision box of a flowchart.</summary>
    public static BinkPolygonShape Diamond(BinkRect rect)
    {
        rect = rect.Normalize();
        return new BinkPolygonShape([new(rect.Center.X, rect.Top), new(rect.Right, rect.Center.Y), new(rect.Center.X, rect.Bottom), new(rect.Left, rect.Center.Y)]);
    }

    /// <summary>An isosceles triangle filling <paramref name="rect"/>, pointing up.</summary>
    public static BinkPolygonShape Triangle(BinkRect rect)
    {
        rect = rect.Normalize();
        return new BinkPolygonShape([new(rect.Center.X, rect.Top), new(rect.Right, rect.Bottom), new(rect.Left, rect.Bottom)]);
    }

    /// <summary>A star with <paramref name="points"/> points, stretched to fill <paramref name="rect"/>, one point at the top.</summary>
    /// <param name="rect">The box the star fills.</param>
    /// <param name="points">The number of points, at least three.</param>
    /// <param name="innerRatio">How far in the notches between the points reach, from 0 (the centre) to 1 (no notches).</param>
    public static BinkPolygonShape Star(BinkRect rect, int points = 5, float innerRatio = 0.5f)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(points, 3);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(points, MaxPoints / 2);
        float inner = float.IsFinite(innerRatio) ? Math.Clamp(innerRatio, 0.05f, 1f) : 0.5f;
        var vertices = new Vector2[points * 2];
        for (int i = 0; i < vertices.Length; i++)
        {
            float angle = -MathF.PI / 2f + i * MathF.PI / points;
            float radius = i % 2 == 0 ? 1f : inner;
            vertices[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }
        return new BinkPolygonShape(FitTo(vertices, rect));
    }

    /// <summary>Scales and moves points so their bounds are exactly <paramref name="rect"/>.</summary>
    private static Vector2[] FitTo(Vector2[] points, BinkRect rect)
    {
        rect = rect.Normalize();
        BinkRect bounds = BinkGeometryMath.Bounds(points);
        float sx = bounds.Width > BinkGeometryMath.Epsilon ? rect.Width / bounds.Width : 1f;
        float sy = bounds.Height > BinkGeometryMath.Epsilon ? rect.Height / bounds.Height : 1f;
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Vector2(rect.Left + (points[i].X - bounds.Left) * sx, rect.Top + (points[i].Y - bounds.Top) * sy);
        }
        return points;
    }
}
