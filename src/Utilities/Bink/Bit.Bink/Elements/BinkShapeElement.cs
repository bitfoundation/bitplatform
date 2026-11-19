using System.Numerics;

namespace Bit.Bink;

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
