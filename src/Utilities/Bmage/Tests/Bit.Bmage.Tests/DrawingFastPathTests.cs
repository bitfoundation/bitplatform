using System.Buffers;
using System.Drawing;
using System.Numerics;
using Bit.Bmage.Drawing;

namespace Bit.Bmage.Tests;

/// <summary>
/// Filling shapes skips work the general path would repeat: rows only handle the columns their spans touched, and fully
/// covered pixels skip the general composite. The result must be the pixels the straightforward rasteriser produces.
/// </summary>
[TestClass]
public sealed class DrawingFastPathTests
{
    [TestMethod]
    public void FillsMatchTheStraightforwardRasteriser()
    {
        var random = new Random(17);
        var colors = new[] { new Rgba32(200, 30, 60), new Rgba32(20, 160, 90, 128), new Rgba32(255, 255, 255, 1), new Rgba32(0, 0, 0, 254) };
        for (var round = 0; round < 60; round++)
        {
            using var canvasImage = round % 2 == 0 ? FastPathEquivalenceTests.Noise(90, 70, seed: round) : ImageBuffer.Create(90, 70, new Rgba32(10, 20, 30));
            using var expected = canvasImage.Clone();
            var path = RandomPath(random);
            var color = colors[random.Next(colors.Length)];
            var opacity = random.Next(3) == 0 ? 0.6f : 1f;
            Rectangle? clip = random.Next(3) == 0 ? new Rectangle(10, 5, 60, 50) : null;
            var rule = random.Next(2) == 0 ? FillRule.NonZero : FillRule.EvenOdd;

            Rasterizer.Fill(canvasImage, path, color, rule, clip, opacity);
            ReferenceFill(expected, path, color, rule, clip, opacity);
            FastPathEquivalenceTests.AssertSamePixels(expected, canvasImage, $"round {round}");
        }
    }

    private static VectorPath RandomPath(Random random)
    {
        var builder = new PathBuilder();
        switch (random.Next(4))
        {
            case 0: builder.AddRectangle(new RectangleF(random.Next(-10, 60) + 0.3f, random.Next(-10, 50) + 0.7f, random.Next(1, 70) + 0.4f, random.Next(1, 50) + 0.1f)); break;
            case 1: builder.AddEllipse(new RectangleF(random.Next(0, 50), random.Next(0, 40), random.Next(5, 60), random.Next(5, 40))); break;
            case 2:
                // A thin diagonal stroke, whose bounding box is mostly empty.
                return Stroker.CreateStrokeOutline(new PathBuilder().MoveTo(random.Next(0, 20), random.Next(0, 10)).LineTo(random.Next(60, 90), random.Next(50, 70)).Build(), new StrokeStyle { Width = 2.5f });
            default:
                builder.MoveTo(random.Next(90), random.Next(70));
                for (var i = 0; i < 6; i++) builder.LineTo(random.Next(-5, 95) + 0.5f, random.Next(-5, 75) + 0.25f);
                builder.Close();
                break;
        }
        return builder.Build();
    }

    private static void ReferenceFill(ImageBuffer target, VectorPath path, Rgba32 color, FillRule rule, Rectangle? clip, float opacity)
    {
        var area = clip ?? target.Bounds;
        area.Intersect(target.Bounds);
        Reference.Rasterize(path, area, rule, (y, x0, coverage) =>
        {
            for (var i = 0; i < coverage.Length; i++)
            {
                var a = coverage[i];
                if (a <= 0.0005f) continue;
                if (a > 1f) a = 1f;
                var x = x0 + i;
                if (x < area.Left || x >= area.Right) continue;
                target[x, y] = Compositor.SourceOver(target[x, y], color, a * opacity);
            }
        });
    }

    /// <summary>The rasteriser as it was, clearing and handing on every row's full width.</summary>
    private static class Reference
    {
        private const int SubSamples = Rasterizer.SubSamples;

    private readonly record struct Edge(float X0, float Y0, float X1, float Y1, int Winding);

    /// <summary>
    /// Computes per-pixel coverage in [0,1] for a path over <paramref name="clip"/> and hands each covered span to
    /// <paramref name="blend"/>. Coverage is never allocated for the whole image, only one row at a time.
    /// </summary>
    public static void Rasterize(VectorPath path, Rectangle clip, FillRule rule, Bit.Bmage.Drawing.CoverageHandler blend)
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

    }
}
