using System.Numerics;

namespace Bit.Bmage.Drawing;

/// <summary>How the ends of an open stroked path are drawn.</summary>
public enum LineCap
{
    /// <summary>Ends exactly at the endpoint.</summary>
    Butt = 0,
    /// <summary>Extends by half the stroke width with a semicircle.</summary>
    Round,
    /// <summary>Extends by half the stroke width with a square.</summary>
    Square,
}

/// <summary>How corners between segments are drawn.</summary>
public enum LineJoin
{
    /// <summary>Extends the outer edges until they meet, falling back to a bevel past the miter limit.</summary>
    Miter = 0,
    /// <summary>Rounds the corner with an arc.</summary>
    Round,
    /// <summary>Cuts the corner off with a straight edge.</summary>
    Bevel,
}

/// <summary>Stroke appearance: width, caps, joins and an optional dash pattern.</summary>
public sealed record StrokeStyle
{
    /// <summary>A 1px solid stroke with round caps and joins.</summary>
    public static StrokeStyle Default { get; } = new();

    /// <summary>Stroke width in pixels. Must be positive.</summary>
    public float Width { get; init; } = 1f;

    public LineCap Cap { get; init; } = LineCap.Round;

    public LineJoin Join { get; init; } = LineJoin.Round;

    /// <summary>Maximum ratio of miter length to stroke width before falling back to a bevel.</summary>
    public float MiterLimit { get; init; } = 4f;

    /// <summary>
    /// Dash lengths in pixels, alternating on and off. Null or empty means a solid line. Values are used cyclically,
    /// so a single value produces equal dashes and gaps.
    /// </summary>
    public IReadOnlyList<float>? DashPattern { get; init; }

    /// <summary>Distance into the dash pattern at which to start.</summary>
    public float DashOffset { get; init; }

    /// <summary>Creates a dashed variant of this style.</summary>
    public StrokeStyle WithDashes(params float[] pattern) => this with { DashPattern = pattern };

    /// <summary>Returns a style scaled for a proxy image.</summary>
    public StrokeStyle ForScale(double scale)
    {
        if (scale == 1) return this;
        return this with
        {
            Width = (float)Math.Max(0.1, Width * scale),
            DashPattern = DashPattern?.Select(d => (float)(d * scale)).ToArray(),
            DashOffset = (float)(DashOffset * scale),
        };
    }
}

/// <summary>Converts a path into the outline of its stroke, which is then filled by the rasteriser.</summary>
public static class Stroker
{
    /// <summary>
    /// Builds the fillable outline of stroking <paramref name="path"/> with <paramref name="style"/>. The result is
    /// filled with the non-zero rule.
    /// </summary>
    public static VectorPath CreateStrokeOutline(VectorPath path, StrokeStyle style)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(style);
        if (style.Width <= 0) throw new ArgumentOutOfRangeException(nameof(style), "Stroke width must be positive.");
        var builder = new PathBuilder();
        var half = style.Width / 2;

        for (var c = 0; c < path.Contours.Count; c++)
        {
            var points = Dedupe(path.Contours[c], path.Closed[c]);
            if (points.Count < 2)
            {
                // A degenerate contour still paints a dot for round or square caps.
                if (points.Count == 1 && style.Cap != LineCap.Butt) AddDot(builder, points[0], half, style.Cap);
                continue;
            }
            var closed = path.Closed[c] && points.Count > 2;
            if (style.DashPattern is { Count: > 0 } dashes && dashes.Any(d => d > 0))
            {
                foreach (var segment in ApplyDashes(points, closed, dashes, style.DashOffset))
                    if (segment.Count >= 2) StrokePolyline(builder, segment, false, half, style);
                    else if (segment.Count == 1 && style.Cap != LineCap.Butt) AddDot(builder, segment[0], half, style.Cap);
            }
            else
            {
                StrokePolyline(builder, points, closed, half, style);
            }
        }
        return builder.Build();
    }

    private static List<Vector2> Dedupe(Vector2[] points, bool closed)
    {
        var result = new List<Vector2>(points.Length);
        foreach (var p in points)
            if (result.Count == 0 || Vector2.DistanceSquared(result[^1], p) > 1e-10f) result.Add(p);
        // A closed contour that repeats its start point would otherwise get a zero-length closing segment. An open one
        // that returns to its start still needs that final segment drawn.
        if (closed && result.Count > 2 && Vector2.DistanceSquared(result[0], result[^1]) <= 1e-10f) result.RemoveAt(result.Count - 1);
        return result;
    }

    private static void AddDot(PathBuilder builder, Vector2 center, float radius, LineCap cap)
    {
        if (cap == LineCap.Round)
            builder.AddEllipse(new System.Drawing.RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2));
        else
            builder.AddRectangle(new System.Drawing.RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2));
    }

    /// <summary>
    /// Emits the outline of one polyline. Each segment contributes a quad; joins and caps are emitted as separate
    /// closed shapes. Overlaps are harmless because the result is filled with the non-zero rule.
    /// </summary>
    private static void StrokePolyline(PathBuilder builder, List<Vector2> points, bool closed, float half, StrokeStyle style)
    {
        var n = points.Count;
        var segments = closed ? n : n - 1;
        for (var i = 0; i < segments; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % n];
            var dir = Vector2.Normalize(b - a);
            var normal = new Vector2(-dir.Y, dir.X) * half;
            builder.MoveTo(a + normal);
            builder.LineTo(b + normal);
            builder.LineTo(b - normal);
            builder.LineTo(a - normal);
            builder.Close();
        }

        // Joins at interior vertices (all vertices when closed).
        var firstJoin = closed ? 0 : 1;
        var lastJoin = closed ? n : n - 1;
        for (var i = firstJoin; i < lastJoin; i++)
        {
            var prev = points[(i - 1 + n) % n];
            var cur = points[i];
            var next = points[(i + 1) % n];
            AddJoin(builder, prev, cur, next, half, style);
        }

        if (!closed)
        {
            AddCap(builder, points[1], points[0], half, style.Cap);
            AddCap(builder, points[n - 2], points[n - 1], half, style.Cap);
        }
    }

    private static void AddJoin(PathBuilder builder, Vector2 prev, Vector2 cur, Vector2 next, float half, StrokeStyle style)
    {
        var d0 = Vector2.Normalize(cur - prev);
        var d1 = Vector2.Normalize(next - cur);
        var cross = d0.X * d1.Y - d0.Y * d1.X;
        if (MathF.Abs(cross) < 1e-6f) return; // collinear: the segment quads already meet cleanly

        switch (style.Join)
        {
            case LineJoin.Round:
                // Wound the same way as the segment quads. AddEllipse winds the opposite way, which under the non-zero
                // rule cancels the quads wherever the two overlap and leaves holes in the stroke.
                builder.MoveTo(cur + new Vector2(half, 0));
                builder.ArcTo(cur, half, half, 0, -MathF.Tau);
                builder.Close();
                return;
            case LineJoin.Bevel:
                AddBevel(builder, cur, d0, d1, half, cross);
                return;
            default:
            {
                // Miter: intersect the two outer edges. Fall back to a bevel when the spike gets too long.
                var sign = cross > 0 ? -1f : 1f;
                var n0 = new Vector2(-d0.Y, d0.X) * half * sign;
                var n1 = new Vector2(-d1.Y, d1.X) * half * sign;
                var sinHalf = MathF.Sqrt(MathF.Max(0f, (1 - Vector2.Dot(d0, d1)) / 2));
                if (sinHalf < 1e-4f || 1 / sinHalf > style.MiterLimit)
                {
                    AddBevel(builder, cur, d0, d1, half, cross);
                    return;
                }
                var bisector = Vector2.Normalize(n0 + n1);
                var tip = cur + bisector * (half / sinHalf);
                builder.MoveTo(cur + n0);
                builder.LineTo(tip);
                builder.LineTo(cur + n1);
                builder.LineTo(cur);
                builder.Close();
                return;
            }
        }
    }

    private static void AddBevel(PathBuilder builder, Vector2 cur, Vector2 d0, Vector2 d1, float half, float cross)
    {
        var sign = cross > 0 ? -1f : 1f;
        var n0 = new Vector2(-d0.Y, d0.X) * half * sign;
        var n1 = new Vector2(-d1.Y, d1.X) * half * sign;
        builder.MoveTo(cur + n0);
        builder.LineTo(cur + n1);
        builder.LineTo(cur);
        builder.Close();
    }

    private static void AddCap(PathBuilder builder, Vector2 from, Vector2 end, float half, LineCap cap)
    {
        if (cap == LineCap.Butt) return;
        var dir = Vector2.Normalize(end - from);
        if (!float.IsFinite(dir.X) || !float.IsFinite(dir.Y)) return;
        var normal = new Vector2(-dir.Y, dir.X) * half;
        if (cap == LineCap.Square)
        {
            var ext = dir * half;
            builder.MoveTo(end + normal);
            builder.LineTo(end + normal + ext);
            builder.LineTo(end - normal + ext);
            builder.LineTo(end - normal);
            builder.Close();
        }
        else
        {
            var angle = MathF.Atan2(normal.Y, normal.X);
            builder.MoveTo(end + normal);
            builder.ArcTo(end, half, half, angle, -MathF.PI);
            builder.Close();
        }
    }

    /// <summary>Splits a polyline into the "on" runs of a dash pattern.</summary>
    private static IEnumerable<List<Vector2>> ApplyDashes(List<Vector2> points, bool closed, IReadOnlyList<float> pattern, float offset)
    {
        var lengths = pattern.Where(p => p >= 0).ToArray();
        if (lengths.Length == 0 || lengths.All(l => l <= 0)) { yield return points; yield break; }
        var total = lengths.Sum();
        if (total <= 0) { yield return points; yield break; }

        var index = 0;
        var remaining = lengths[0];
        var on = true;
        // Consume the dash offset first.
        var period = lengths.Length % 2 == 1 ? total * 2 : total;
        var skip = offset % period;
        if (skip < 0) skip += period; // a negative offset shifts the pattern the other way
        while (skip > 0)
        {
            if (skip < remaining) { remaining -= skip; break; }
            skip -= remaining;
            index = (index + 1) % lengths.Length;
            remaining = lengths[index];
            on = !on;
        }

        var current = on ? new List<Vector2> { points[0] } : [];
        var n = points.Count;
        var segments = closed ? n : n - 1;
        for (var i = 0; i < segments; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % n];
            var segLength = Vector2.Distance(a, b);
            var travelled = 0f;
            while (segLength - travelled > remaining)
            {
                travelled += remaining;
                var p = Vector2.Lerp(a, b, travelled / segLength);
                if (on)
                {
                    current.Add(p);
                    yield return current;
                    current = [];
                }
                else current = [p];
                index = (index + 1) % lengths.Length;
                remaining = Math.Max(lengths[index], 1e-4f);
                on = !on;
            }
            remaining -= segLength - travelled;
            if (on) current.Add(b);
        }
        if (on && current.Count >= 2) yield return current;
    }
}
