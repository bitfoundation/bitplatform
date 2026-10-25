namespace Bit.Blow;

public static partial class BlowLayout
{
    /// <summary>
    /// Arranges a graph by simulation - for a network with no hierarchy to show: connected nodes pull together, all
    /// nodes push apart, and the result settles into clusters. Overlaps left at the end are pushed apart, so no two
    /// nodes overlap.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fruchterman-Reingold, with no randomness: the same graph and options always give the same layout, so laying
    /// out twice does not reshuffle the user's picture. Nodes start on a spiral in the order given, or where they are
    /// with <see cref="BlowForceLayoutOptions.StartFromCurrentPositions"/>. Pinned nodes
    /// (<see cref="BlowForceLayoutOptions.IsPinned"/>) stay put and the rest arrange around them.
    /// </para>
    /// <para>
    /// Each iteration compares every pair of nodes, so the cost grows with the square of the node count: fine for a few
    /// hundred nodes, slow for thousands. Lower <see cref="BlowForceLayoutOptions.Iterations"/> for a large graph, or
    /// use <see cref="Layered"/> or <see cref="Tree"/>, which are linear.
    /// </para>
    /// </remarks>
    public static void Force(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges, BlowForceLayoutOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        var opts = options ?? new BlowForceLayoutOptions();
        var n = nodes.Count;
        if (n == 0) return;

        var sizes = ResolveSizes(nodes, opts.FallbackSize);
        var (outgoing, _) = BuildAdjacency(nodes, edges);
        var pinned = new bool[n];
        for (var i = 0; i < n; i++) pinned[i] = opts.IsPinned?.Invoke(nodes[i]) ?? false;
        var anyPinned = Array.IndexOf(pinned, true) >= 0;

        // Centres of the nodes' boxes; the simulation works on those.
        var x = new double[n];
        var y = new double[n];
        // Bounded both ways: a length of zero divides by zero, and an astronomic one overflows the forces to NaN.
        var k = Math.Clamp(double.IsNaN(opts.IdealEdgeLength) ? 1 : opts.IdealEdgeLength, 1, 1e6);
        for (var i = 0; i < n; i++)
        {
            if (opts.StartFromCurrentPositions || pinned[i])
            {
                var (left, top) = TopLeft(nodes[i], sizes[i], opts);
                x[i] = left + sizes[i].Width / 2;
                y[i] = top + sizes[i].Height / 2;
            }
            else
            {
                // A golden-angle spiral: evenly spread, no two points alike, and the same every time.
                var r = k * Math.Sqrt(i + 0.5);
                var a = i * 2.399963229728653;
                x[i] = r * Math.Cos(a);
                y[i] = r * Math.Sin(a);
            }
        }

        var dx = new double[n];
        var dy = new double[n];
        var iterations = Math.Max(0, opts.Iterations);
        var start = k * Math.Sqrt(n);
        for (var step = 0; step < iterations; step++)
        {
            Array.Clear(dx);
            Array.Clear(dy);

            // Every pair repels: k^2 / d.
            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    var ex = x[i] - x[j];
                    var ey = y[i] - y[j];
                    var d2 = ex * ex + ey * ey;
                    if (d2 < 0.01)
                    {
                        // Two nodes on the same point: part them along a direction that depends only on their indices.
                        ex = 0.1 * (1 + (i % 7));
                        ey = 0.1 * (1 + (j % 5));
                        d2 = ex * ex + ey * ey;
                    }
                    var force = k * k / d2;
                    dx[i] += ex * force;
                    dy[i] += ey * force;
                    dx[j] -= ex * force;
                    dy[j] -= ey * force;
                }
            }

            // Connected nodes attract: d^2 / k.
            for (var i = 0; i < n; i++)
            {
                foreach (var j in outgoing[i])
                {
                    var ex = x[i] - x[j];
                    var ey = y[i] - y[j];
                    var d = Math.Sqrt(ex * ex + ey * ey);
                    if (d < 1e-9) continue;
                    var force = d / k;
                    dx[i] -= ex * force;
                    dy[i] -= ey * force;
                    dx[j] += ex * force;
                    dy[j] += ey * force;
                }
            }

            // Each node moves along its net force, no further than the temperature, which cools to nothing.
            var temperature = start * (1 - (double)step / iterations);
            for (var i = 0; i < n; i++)
            {
                if (pinned[i]) continue;
                var length = Math.Sqrt(dx[i] * dx[i] + dy[i] * dy[i]);
                if (length < 1e-9) continue;
                var move = Math.Min(length, temperature);
                x[i] += dx[i] / length * move;
                y[i] += dy[i] / length * move;
            }
        }

        if (!RemoveOverlaps(x, y, sizes, pinned, opts.NodeGap)) Separate(x, y, sizes, pinned, opts.NodeGap);

        // The arrangement's top-left goes to the origin - unless something is pinned, which fixes where it is.
        double shiftX = 0, shiftY = 0;
        if (!anyPinned)
        {
            double minLeft = double.MaxValue, minTop = double.MaxValue;
            for (var i = 0; i < n; i++)
            {
                minLeft = Math.Min(minLeft, x[i] - sizes[i].Width / 2);
                minTop = Math.Min(minTop, y[i] - sizes[i].Height / 2);
            }
            shiftX = opts.Origin.X - minLeft;
            shiftY = opts.Origin.Y - minTop;
        }

        for (var i = 0; i < n; i++)
        {
            if (pinned[i]) continue;
            Place(nodes[i], sizes[i], opts, x[i] - sizes[i].Width / 2 + shiftX, y[i] - sizes[i].Height / 2 + shiftY);
        }
    }

    /// <summary>The top-left corner of a node's box from its position and origin, in its parent's space.</summary>
    private static (double Left, double Top) TopLeft(BlowNode node, BlowDimensions size, BlowLayoutOptions opts)
    {
        var origin = node.Origin ?? opts.NodeOrigin;
        return (node.Position.X - origin.X * size.Width, node.Position.Y - origin.Y * size.Height);
    }

    /// <summary>
    /// Pushes overlapping boxes apart, each pair along the axis it overlaps least on, until none overlaps (with
    /// <paramref name="gap"/> to spare) or a bound on passes is reached. Pairs are found by sweeping along x, so a
    /// pass costs little more than a sort when little overlaps.
    /// </summary>
    /// <returns>False when the bound was reached with boxes still overlapping.</returns>
    private static bool RemoveOverlaps(double[] x, double[] y, BlowDimensions[] sizes, bool[] pinned, double gap)
    {
        var n = x.Length;
        var order = Enumerable.Range(0, n).ToArray();
        var widest = 0.0;
        foreach (var size in sizes) widest = Math.Max(widest, size.Width);

        for (var pass = 0; pass < 200; pass++)
        {
            Array.Sort(order, (a, b) => x[a].CompareTo(x[b]));
            var moved = false;
            for (var oi = 0; oi < n; oi++)
            {
                var i = order[oi];
                for (var oj = oi + 1; oj < n; oj++)
                {
                    var j = order[oj];
                    // Sorted by centre: once a centre is further right than any two half-widths can reach, stop.
                    if (x[j] - x[i] >= widest + gap) break;

                    var overlapX = (sizes[i].Width + sizes[j].Width) / 2 + gap - Math.Abs(x[j] - x[i]);
                    var overlapY = (sizes[i].Height + sizes[j].Height) / 2 + gap - Math.Abs(y[j] - y[i]);
                    if (overlapX <= 0 || overlapY <= 0) continue;

                    // Who gives way: both halves, or all of it to the one that is not pinned.
                    if (pinned[i] && pinned[j]) continue;
                    var shareI = pinned[i] ? 0 : pinned[j] ? 1 : 0.5;
                    var shareJ = 1 - shareI;

                    if (overlapX < overlapY)
                    {
                        var sign = x[j] >= x[i] ? 1 : -1;
                        x[i] -= sign * overlapX * shareI;
                        x[j] += sign * overlapX * shareJ;
                    }
                    else
                    {
                        var sign = y[j] > y[i] || (y[j] == y[i] && j > i) ? 1 : -1;
                        y[i] -= sign * overlapY * shareI;
                        y[j] += sign * overlapY * shareJ;
                    }
                    moved = true;
                }
            }
            if (!moved) return true;
        }
        return false;
    }

    /// <summary>
    /// The last resort when pushing pairs apart does not settle (a dense pile, a tiny edge length): places the nodes
    /// one at a time - pinned ones first, where they are - moving each to the right of whatever placed box it still
    /// overlaps until it overlaps none. Each move clears one more box, so it ends, and nothing overlaps after it.
    /// </summary>
    private static void Separate(double[] x, double[] y, BlowDimensions[] sizes, bool[] pinned, double gap)
    {
        var n = x.Length;
        var order = Enumerable.Range(0, n).OrderBy(i => pinned[i] ? 0 : 1).ThenBy(i => x[i]).ThenBy(i => i).ToArray();
        var placed = new List<int>(n);
        foreach (var i in order)
        {
            if (!pinned[i])
            {
                for (var moved = true; moved;)
                {
                    moved = false;
                    foreach (var j in placed)
                    {
                        var overlapX = (sizes[i].Width + sizes[j].Width) / 2 + gap - Math.Abs(x[j] - x[i]);
                        var overlapY = (sizes[i].Height + sizes[j].Height) / 2 + gap - Math.Abs(y[j] - y[i]);
                        if (overlapX <= 1e-9 || overlapY <= 1e-9) continue;
                        x[i] = x[j] + (sizes[i].Width + sizes[j].Width) / 2 + gap;
                        moved = true;
                    }
                }
            }
            placed.Add(i);
        }
    }
}
