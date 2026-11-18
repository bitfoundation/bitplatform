
namespace Bit.Bink;

/// <summary>Shared geometry helpers for brushes that render a constant-width centerline.</summary>
public static class BinkCenterlineBrushes
{
    /// <summary>Builds a smooth centerline path (Catmull-Rom) to be stroked at <paramref name="width"/>.</summary>
    public static BinkStrokeGeometry Build(BinkStrokePointsView points, BinkPathBuilder builder, float width, BinkLineCap cap, BinkLineJoin join)
    {
        builder.Reset();
        int n = points.Count;
        if (n == 0) return BinkStrokeGeometry.Empty;
        if (n == 1)
        {
            // A single tap: draw a dot of the line width so it is visible.
            builder.AddCircle(points.PositionAt(0), width / 2f);
            return new BinkStrokeGeometry(builder.ToGeometry(), BinkStrokeGeometryMode.Fill, 0f, cap, join, points.Bounds.Inflate(width / 2f + 1f));
        }
        Span<System.Numerics.Vector2> pts = n <= 1024 ? stackalloc System.Numerics.Vector2[n] : new System.Numerics.Vector2[n];
        for (int i = 0; i < n; i++) pts[i] = points.PositionAt(i);
        builder.AddCatmullRom(pts);
        // Round caps and joins stay within half the width of the centerline; square caps reach the corner of a
        // half-width square and miter joins reach the miter limit (4, the SVG and raster default).
        float reach = join == BinkLineJoin.Miter ? 4f : cap == BinkLineCap.Square ? MathF.Sqrt(2f) : 1f;
        float pad = width / 2f * reach + 1f;
        // The spline overshoots its points where the stroke turns sharply, so the points' bounds are too small.
        // The curve stays inside the hull of its control points, which the builder's bounds cover.
        return new BinkStrokeGeometry(builder.ToGeometry(), BinkStrokeGeometryMode.Centerline, width, cap, join, builder.Bounds.Inflate(pad));
    }

    /// <summary>
    /// Grows point bounds to cover the spline <see cref="Build"/> draws through them. A Catmull-Rom control point
    /// sits at most a sixth of the distance between its neighbours away from its point, so the curve can leave the
    /// points' bounds by up to a sixth of their size on each axis.
    /// </summary>
    internal static BinkRect WithSplineOvershoot(BinkRect pointBounds)
        => pointBounds.IsUnset ? pointBounds : pointBounds.Inflate(pointBounds.Width / 6f, pointBounds.Height / 6f);
}
