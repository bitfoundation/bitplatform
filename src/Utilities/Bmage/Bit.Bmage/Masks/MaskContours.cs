using System.Numerics;

namespace Bit.Bmage.Masks;

/// <summary>
/// Traces the outline of a mask: the closed curves where its coverage crosses half, as an editor draws a selection's
/// marching ants or an exporter turns a selection into a path.
/// </summary>
/// <remarks>
/// Marching squares over the pixel centres, with each crossing placed by linear interpolation between the two values
/// either side of it, so an anti-aliased edge is traced to a fraction of a pixel. The mask is treated as uncovered
/// beyond its edges, so every curve closes. Ambiguous saddle cells are resolved by their centre's value, which keeps
/// curves from crossing.
/// </remarks>
public static class MaskContours
{
    /// <summary>The closed outlines of <paramref name="mask"/> at <paramref name="threshold"/>, in its pixel coordinates.</summary>
    /// <param name="mask">The mask to trace.</param>
    /// <param name="threshold">The coverage, 1 to 254, the outline follows. Half by default.</param>
    /// <param name="maxPoints">The most points returned, all curves together; tracing stops beyond it, so a mask of noise cannot produce millions.</param>
    /// <param name="cancellationToken">Checked every 64 rows.</param>
    public static IReadOnlyList<Vector2[]> Trace(MaskBuffer mask, byte threshold = 128, int maxPoints = 200_000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mask);
        if (threshold is 0 or 255) throw new ArgumentOutOfRangeException(nameof(threshold), "The threshold must lie strictly between uncovered and covered.");
        int width = mask.Width, height = mask.Height;
        // Corners of the grid run from -1 to the size, a ring of uncovered values around the mask, so outlines close.
        var cellsX = width + 1;
        var cellsY = height + 1;

        float Value(int x, int y) => (uint)x < (uint)width && (uint)y < (uint)height ? mask[x, y] : 0f;

        // Each segment joins the crossing on one edge of a cell to the crossing on another. An edge is named by the cell
        // corner it starts from and its direction: horizontal edges (x, y)-(x+1, y), vertical edges (x, y)-(x, y+1).
        var next = new Dictionary<long, long>();
        var points = new Dictionary<long, Vector2>();
        long Key(int x, int y, bool vertical) => (((long)(y + 1) * (cellsX + 1) + (x + 1)) << 1) | (vertical ? 1L : 0L);
        Vector2 Crossing(int x0, int y0, int x1, int y1)
        {
            var a = Value(x0, y0);
            var b = Value(x1, y1);
            var t = Math.Clamp((threshold - a) / (b - a), 0f, 1f);
            // Pixel centres sit at half-pixel positions.
            return new Vector2(x0 + 0.5f + (x1 - x0) * t, y0 + 0.5f + (y1 - y0) * t);
        }
        void Link(long from, Vector2 a, long to, Vector2 b)
        {
            points[from] = a;
            points[to] = b;
            next[from] = to;
        }

        for (var y = -1; y < height; y++)
        {
            if ((y & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
            // A mask of noise has an edge at nearly every pixel; past what could be returned, keep no more of them.
            if (next.Count > maxPoints) break;
            for (var x = -1; x < width; x++)
            {
                // The cell's corners, clockwise from the top left.
                var tl = Value(x, y) >= threshold;
                var tr = Value(x + 1, y) >= threshold;
                var br = Value(x + 1, y + 1) >= threshold;
                var bl = Value(x, y + 1) >= threshold;
                var index = (tl ? 8 : 0) | (tr ? 4 : 0) | (br ? 2 : 0) | (bl ? 1 : 0);
                if (index is 0 or 15) continue;

                var top = Key(x, y, false);
                var bottom = Key(x, y + 1, false);
                var left = Key(x, y, true);
                var right = Key(x + 1, y, true);
                Vector2 T() => Crossing(x, y, x + 1, y);
                Vector2 B() => Crossing(x, y + 1, x + 1, y + 1);
                Vector2 L() => Crossing(x, y, x, y + 1);
                Vector2 R() => Crossing(x + 1, y, x + 1, y + 1);

                // Segments run with the covered side on their right, so every outline turns the same way and chains.
                switch (index)
                {
                    case 1: Link(left, L(), bottom, B()); break;
                    case 2: Link(bottom, B(), right, R()); break;
                    case 3: Link(left, L(), right, R()); break;
                    case 4: Link(right, R(), top, T()); break;
                    case 5:
                        if ((Value(x, y) + Value(x + 1, y) + Value(x + 1, y + 1) + Value(x, y + 1)) / 4 >= threshold)
                        {
                            Link(left, L(), top, T());
                            Link(right, R(), bottom, B());
                        }
                        else
                        {
                            Link(left, L(), bottom, B());
                            Link(right, R(), top, T());
                        }
                        break;
                    case 6: Link(bottom, B(), top, T()); break;
                    case 7: Link(left, L(), top, T()); break;
                    case 8: Link(top, T(), left, L()); break;
                    case 9: Link(top, T(), bottom, B()); break;
                    case 10:
                        if ((Value(x, y) + Value(x + 1, y) + Value(x + 1, y + 1) + Value(x, y + 1)) / 4 >= threshold)
                        {
                            Link(top, T(), right, R());
                            Link(bottom, B(), left, L());
                        }
                        else
                        {
                            Link(top, T(), left, L());
                            Link(bottom, B(), right, R());
                        }
                        break;
                    case 11: Link(top, T(), right, R()); break;
                    case 12: Link(right, R(), left, L()); break;
                    case 13: Link(right, R(), bottom, B()); break;
                    case 14: Link(bottom, B(), left, L()); break;
                }
            }
        }

        // Chains the segments into closed curves.
        var curves = new List<Vector2[]>();
        var total = 0;
        var visited = new HashSet<long>();
        foreach (var start in next.Keys)
        {
            if (visited.Contains(start)) continue;
            var curve = new List<Vector2>();
            var at = start;
            while (visited.Add(at))
            {
                curve.Add(points[at]);
                if (!next.TryGetValue(at, out at)) break;
            }
            if (curve.Count < 3) continue;
            total += curve.Count;
            if (total > maxPoints) break;
            curves.Add([.. curve]);
        }
        return curves;
    }
}
