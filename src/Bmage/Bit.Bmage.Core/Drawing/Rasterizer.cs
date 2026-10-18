using System.Buffers;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Geometry;

namespace Bit.Bmage.Drawing;

/// <summary>How overlapping contours combine when filling a path.</summary>
public enum FillRule
{
    /// <summary>A point is inside when the winding number is non-zero. The usual choice.</summary>
    NonZero = 0,
    /// <summary>A point is inside when it is crossed an odd number of times, so overlaps cut holes.</summary>
    EvenOdd,
}

/// <summary>
/// Receives the anti-aliased coverage for one row: values in [0,1] for pixels starting at <paramref name="x0"/>.
/// </summary>
/// <param name="y">The row being filled.</param>
/// <param name="x0">The x coordinate of the first value in <paramref name="coverage"/>.</param>
/// <param name="coverage">Per-pixel coverage. Valid only for the duration of the call.</param>
public delegate void CoverageHandler(int y, int x0, ReadOnlySpan<float> coverage);

/// <summary>
/// Anti-aliased scanline polygon rasteriser. Coverage is computed by sampling each pixel row at several sub-scanlines
/// and accumulating exact horizontal span coverage, giving smooth edges without super-sampling the whole image.
/// </summary>
public static class Rasterizer
{
    /// <summary>Number of sub-scanlines sampled per pixel row. Higher is smoother vertically.</summary>
    public const int SubSamples = 5;

    private readonly record struct Edge(float X0, float Y0, float X1, float Y1, int Winding);

    /// <summary>
    /// Computes per-pixel coverage in [0,1] for a path over <paramref name="clip"/> and hands each covered span to
    /// <paramref name="blend"/>. Coverage is never allocated for the whole image, only one row at a time.
    /// </summary>
    public static void Rasterize(VectorPath path, Rectangle clip, FillRule rule, CoverageHandler blend)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(blend);
        if (path.IsEmpty || clip.Width <= 0 || clip.Height <= 0) return;

        var edges = BuildEdges(path);
        if (edges.Count == 0) return;

        var bounds = path.Bounds;
        // Clamp in double before converting: a huge or infinite coordinate would otherwise overflow int and wrap,
        // making the range empty and the whole shape vanish.
        var top = (int)Math.Max(clip.Top, Math.Floor((double)bounds.Top));
        var bottom = (int)Math.Min(clip.Bottom, Math.Ceiling((double)bounds.Bottom) + 1);
        var left = (int)Math.Max(clip.Left, Math.Floor((double)bounds.Left));
        var right = (int)Math.Min(clip.Right, Math.Ceiling((double)bounds.Right) + 1);
        if (top >= bottom || left >= right) return;

        // Sort edges by their top, so a scanline only has to consider the ones that are actually open. Without this,
        // every sub-scanline walks every edge, and a path with thousands of segments (text, or a detailed drawing)
        // costs the product of the two.
        var sorted = edges.ToArray();
        Array.Sort(sorted, static (a, b) => a.Y0.CompareTo(b.Y0));

        var width = right - left;
        var pool = ArrayPool<float>.Shared;
        var coverage = pool.Rent(width);
        var crossings = new List<(float X, int Winding)>(16);
        var active = new List<int>(32);
        var nextEdge = 0;
        try
        {
            var span = coverage.AsSpan(0, width);
            const float weight = 1f / SubSamples;
            for (var y = top; y < bottom; y++)
            {
                span.Clear();
                var any = false;
                for (var s = 0; s < SubSamples; s++)
                {
                    var sy = y + (s + 0.5f) / SubSamples;

                    // Sub-scanlines advance monotonically, so the active set can be maintained incrementally.
                    while (nextEdge < sorted.Length && sorted[nextEdge].Y0 <= sy) active.Add(nextEdge++);
                    for (var i = active.Count - 1; i >= 0; i--)
                        if (sorted[active[i]].Y1 <= sy) active.RemoveAt(i);
                    if (active.Count < 2) continue;

                    crossings.Clear();
                    foreach (var index in active)
                    {
                        var e = sorted[index];
                        if (sy < e.Y0 || sy >= e.Y1) continue;
                        var t = (sy - e.Y0) / (e.Y1 - e.Y0);
                        crossings.Add((e.X0 + t * (e.X1 - e.X0), e.Winding));
                    }
                    if (crossings.Count < 2) continue;
                    crossings.Sort(static (a, b) => a.X.CompareTo(b.X));

                    var winding = 0;
                    for (var i = 0; i < crossings.Count - 1; i++)
                    {
                        winding += crossings[i].Winding;
                        var inside = rule == FillRule.NonZero ? winding != 0 : (winding & 1) != 0;
                        if (!inside) continue;
                        var x0 = crossings[i].X;
                        var x1 = crossings[i + 1].X;
                        if (x1 <= left || x0 >= right) continue;
                        any |= AccumulateSpan(span, left, right, x0, x1, weight);
                    }
                }
                if (any) blend(y, left, span);
            }
        }
        finally
        {
            pool.Return(coverage);
        }
    }

    /// <summary>Adds horizontal coverage for the span [x0,x1) to a row accumulator, with exact partial pixels at the ends.</summary>
    private static bool AccumulateSpan(Span<float> row, int left, int right, float x0, float x1, float weight)
    {
        if (x0 < left) x0 = left;
        if (x1 > right) x1 = right;
        if (x1 <= x0) return false;
        var ix0 = (int)MathF.Floor(x0);
        var ix1 = (int)MathF.Floor(x1);
        if (ix0 == ix1)
        {
            row[ix0 - left] += (x1 - x0) * weight;
            return true;
        }
        row[ix0 - left] += (ix0 + 1 - x0) * weight;
        for (var x = ix0 + 1; x < ix1; x++) row[x - left] += weight;
        if (ix1 < right) row[ix1 - left] += (x1 - ix1) * weight;
        return true;
    }

    private static List<Edge> BuildEdges(VectorPath path)
    {
        var edges = new List<Edge>();
        for (var c = 0; c < path.Contours.Count; c++)
        {
            var points = path.Contours[c];
            var n = points.Length;
            if (n < 2) continue;
            // Filling always treats contours as closed; open contours get an implicit closing edge.
            for (var i = 0; i < n; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % n];
                if (i == n - 1 && a == points[0]) break;
                AddEdge(edges, a, b);
            }
        }
        return edges;
    }

    private static void AddEdge(List<Edge> edges, Vector2 a, Vector2 b)
    {
        // A non-finite coordinate cannot be rasterised: NaN breaks the edge sort and the span arithmetic.
        if (!float.IsFinite(a.X) || !float.IsFinite(a.Y) || !float.IsFinite(b.X) || !float.IsFinite(b.Y)) return;
        if (Math.Abs(a.Y - b.Y) < 1e-9f) return; // horizontal edges contribute nothing
        if (a.Y < b.Y) edges.Add(new Edge(a.X, a.Y, b.X, b.Y, 1));
        else edges.Add(new Edge(b.X, b.Y, a.X, a.Y, -1));
    }

    /// <summary>Fills a path into a buffer with a solid colour, blending with source-over alpha.</summary>
    public static void Fill(ImageBuffer target, VectorPath path, Rgba32 color, FillRule rule = FillRule.NonZero, Rectangle? clip = null, float opacity = 1f)
    {
        ArgumentNullException.ThrowIfNull(target);
        var area = clip ?? target.Bounds;
        area.Intersect(target.Bounds);
        if (area.IsEmptyArea() || opacity <= 0) return;
        Rasterize(path, area, rule, (y, x0, coverage) =>
        {
            var row = target.GetRow(y);
            for (var i = 0; i < coverage.Length; i++)
            {
                var a = coverage[i];
                if (a <= 0.0005f) continue;
                if (a > 1f) a = 1f;
                var x = x0 + i;
                if (x < area.Left || x >= area.Right) continue;
                row[x] = Compositor.SourceOver(row[x], color, a * opacity);
            }
        });
    }

    /// <summary>Fills a path by sampling a paint function for each covered pixel.</summary>
    public static void Fill(ImageBuffer target, VectorPath path, IPaint paint, FillRule rule = FillRule.NonZero, Rectangle? clip = null, float opacity = 1f)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(paint);
        var area = clip ?? target.Bounds;
        area.Intersect(target.Bounds);
        if (area.IsEmptyArea() || opacity <= 0) return;
        Rasterize(path, area, rule, (y, x0, coverage) =>
        {
            var row = target.GetRow(y);
            for (var i = 0; i < coverage.Length; i++)
            {
                var a = coverage[i];
                if (a <= 0.0005f) continue;
                if (a > 1f) a = 1f;
                var x = x0 + i;
                if (x < area.Left || x >= area.Right) continue;
                row[x] = Compositor.SourceOver(row[x], paint.GetColor(x, y), a * opacity);
            }
        });
    }
}

/// <summary>Supplies a colour per pixel; implemented by solid colours and gradients.</summary>
public interface IPaint
{
    /// <summary>Returns the colour to paint at the given pixel.</summary>
    Rgba32 GetColor(int x, int y);
}

/// <summary>A single colour paint.</summary>
public sealed class SolidPaint : IPaint
{
    public SolidPaint(Rgba32 color) => Color = color;
    public Rgba32 Color { get; }
    public Rgba32 GetColor(int x, int y) => Color;
}

/// <summary>A linear gradient between two points.</summary>
public sealed class LinearGradientPaint : IPaint
{
    private readonly Vector2 _start;
    private readonly Vector2 _axis;
    private readonly float _lengthSquared;
    private readonly (float Offset, Rgba32 Color)[] _stops;

    public LinearGradientPaint(Vector2 start, Vector2 end, IReadOnlyList<(float Offset, Rgba32 Color)> stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Count < 2) throw new ArgumentException("A gradient needs at least two stops.", nameof(stops));
        _start = start;
        _axis = end - start;
        _lengthSquared = _axis.LengthSquared();
        _stops = stops.OrderBy(s => s.Offset).ToArray();
    }

    public Rgba32 GetColor(int x, int y)
    {
        if (_lengthSquared <= 0) return _stops[0].Color;
        var t = Vector2.Dot(new Vector2(x + 0.5f, y + 0.5f) - _start, _axis) / _lengthSquared;
        return Sample(_stops, Math.Clamp(t, 0f, 1f));
    }

    internal static Rgba32 Sample((float Offset, Rgba32 Color)[] stops, float t)
    {
        if (t <= stops[0].Offset) return stops[0].Color;
        if (t >= stops[^1].Offset) return stops[^1].Color;
        for (var i = 1; i < stops.Length; i++)
        {
            if (t > stops[i].Offset) continue;
            var a = stops[i - 1];
            var b = stops[i];
            var span = b.Offset - a.Offset;
            var f = span <= 0 ? 0 : (t - a.Offset) / span;
            return new Rgba32(
                (byte)(a.Color.R + (b.Color.R - a.Color.R) * f),
                (byte)(a.Color.G + (b.Color.G - a.Color.G) * f),
                (byte)(a.Color.B + (b.Color.B - a.Color.B) * f),
                (byte)(a.Color.A + (b.Color.A - a.Color.A) * f));
        }
        return stops[^1].Color;
    }
}

/// <summary>A radial gradient from a centre point out to a radius.</summary>
public sealed class RadialGradientPaint : IPaint
{
    private readonly Vector2 _center;
    private readonly float _radius;
    private readonly (float Offset, Rgba32 Color)[] _stops;

    public RadialGradientPaint(Vector2 center, float radius, IReadOnlyList<(float Offset, Rgba32 Color)> stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Count < 2) throw new ArgumentException("A gradient needs at least two stops.", nameof(stops));
        if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        _center = center;
        _radius = radius;
        _stops = stops.OrderBy(s => s.Offset).ToArray();
    }

    public Rgba32 GetColor(int x, int y)
    {
        var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), _center) / _radius;
        return LinearGradientPaint.Sample(_stops, Math.Clamp(d, 0f, 1f));
    }
}

/// <summary>Alpha compositing helpers operating on straight-alpha RGBA.</summary>
public static class Compositor
{
    /// <summary>Composites <paramref name="source"/> over <paramref name="destination"/> with an extra coverage/alpha factor.</summary>
    public static Rgba32 SourceOver(Rgba32 destination, Rgba32 source, float alpha)
    {
        var sa = source.A / 255f * Math.Clamp(alpha, 0f, 1f);
        if (sa <= 0f) return destination;
        var da = destination.A / 255f;
        var outA = sa + da * (1 - sa);
        if (outA <= 0f) return Rgba32.Transparent;
        var inv = 1f / outA;
        return new Rgba32(
            Rgba32.ClampToByte((source.R * sa + destination.R * da * (1 - sa)) * inv),
            Rgba32.ClampToByte((source.G * sa + destination.G * da * (1 - sa)) * inv),
            Rgba32.ClampToByte((source.B * sa + destination.B * da * (1 - sa)) * inv),
            Rgba32.ClampToByte(outA * 255f));
    }

    /// <summary>Erases from the destination: reduces its alpha by the coverage.</summary>
    public static Rgba32 Erase(Rgba32 destination, float alpha)
    {
        var a = Math.Clamp(alpha, 0f, 1f);
        if (a <= 0f) return destination;
        return destination.WithAlpha(Rgba32.ClampToByte(destination.A * (1 - a)));
    }
}
