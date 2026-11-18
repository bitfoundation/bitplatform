using System.Buffers;
using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// Anti-aliased scanline polygon rasteriser. Coverage is computed by sampling each pixel row at several sub-scanlines
/// and accumulating exact horizontal span coverage, giving smooth edges without super-sampling the whole image.
/// </summary>
public static class BmageRasterizer
{
    /// <summary>Number of sub-scanlines sampled per pixel row. Higher is smoother vertically.</summary>
    public const int SubSamples = 5;

    private readonly record struct Edge(float X0, float Y0, float X1, float Y1, int Winding);

    /// <summary>
    /// Computes per-pixel coverage in [0,1] for a path over <paramref name="clip"/> and hands each covered span to
    /// <paramref name="blend"/>. Coverage is never allocated for the whole image, only one row at a time.
    /// </summary>
    /// <remarks>
    /// Each row's span runs from the first to the last column the path covers in that row, not across the whole clip,
    /// and rows the path does not touch are not handed on at all: columns outside the span have no coverage.
    /// </remarks>
    public static void Rasterize(BmageVectorPath path, Rectangle clip, BmageFillRule rule, BmageCoverageHandler blend)
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
            span.Clear();
            const float weight = 1f / SubSamples;
            // The columns the current row's spans touched. Only those are handed on and cleared afterwards, so a thin
            // diagonal stroke costs its own area per row rather than the width of its bounding box.
            var dirtyStart = width;
            var dirtyEnd = 0;
            for (var y = top; y < bottom; y++)
            {
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
                        var inside = rule == BmageFillRule.NonZero ? winding != 0 : (winding & 1) != 0;
                        if (!inside) continue;
                        var x0 = crossings[i].X;
                        var x1 = crossings[i + 1].X;
                        if (x1 <= left || x0 >= right) continue;
                        if (AccumulateSpan(span, left, right, x0, x1, weight, out var from, out var to))
                        {
                            any = true;
                            dirtyStart = Math.Min(dirtyStart, from);
                            dirtyEnd = Math.Max(dirtyEnd, to);
                        }
                    }
                }
                if (any)
                {
                    var dirty = span[dirtyStart..dirtyEnd];
                    blend(y, left + dirtyStart, dirty);
                    dirty.Clear();
                    dirtyStart = width;
                    dirtyEnd = 0;
                }
            }
        }
        finally
        {
            pool.Return(coverage);
        }
    }

    /// <summary>
    /// Adds horizontal coverage for the span [x0,x1) to a row accumulator, with exact partial pixels at the ends, and
    /// reports the accumulator columns it wrote.
    /// </summary>
    private static bool AccumulateSpan(Span<float> row, int left, int right, float x0, float x1, float weight, out int from, out int to)
    {
        from = to = 0;
        if (x0 < left) x0 = left;
        if (x1 > right) x1 = right;
        if (x1 <= x0) return false;
        var ix0 = (int)MathF.Floor(x0);
        var ix1 = (int)MathF.Floor(x1);
        from = ix0 - left;
        if (ix0 == ix1)
        {
            row[ix0 - left] += (x1 - x0) * weight;
            to = from + 1;
            return true;
        }
        row[ix0 - left] += (ix0 + 1 - x0) * weight;
        for (var x = ix0 + 1; x < ix1; x++) row[x - left] += weight;
        to = ix1 - left;
        if (ix1 < right)
        {
            row[ix1 - left] += (x1 - ix1) * weight;
            to++;
        }
        return true;
    }

    private static List<Edge> BuildEdges(BmageVectorPath path)
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
    public static void Fill(BmageImageBuffer target, BmageVectorPath path, BmageRgba32 color, BmageFillRule rule = BmageFillRule.NonZero, Rectangle? clip = null, float opacity = 1f)
    {
        ArgumentNullException.ThrowIfNull(target);
        var area = clip ?? target.Bounds;
        area.Intersect(target.Bounds);
        if (area.IsEmptyArea() || opacity <= 0) return;
        // Where a pixel is fully covered the composite only depends on the destination pixel, and for an opaque one
        // only on each of its channels, so those pixels skip the general formula: an opaque colour is written as is,
        // and a translucent one over an opaque pixel goes through three 256-entry tables built from that formula.
        using var full = BmageCompositor.SolidFill.Create(color, opacity);
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
                row[x] = a == 1f ? full.Apply(row[x]) : BmageCompositor.SourceOver(row[x], color, a * opacity);
            }
        });
    }

    /// <summary>Fills a path by sampling a paint function for each covered pixel.</summary>
    public static void Fill(BmageImageBuffer target, BmageVectorPath path, IBmagePaint paint, BmageFillRule rule = BmageFillRule.NonZero, Rectangle? clip = null, float opacity = 1f)
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
                row[x] = BmageCompositor.SourceOver(row[x], paint.GetColor(x, y), a * opacity);
            }
        });
    }
}
