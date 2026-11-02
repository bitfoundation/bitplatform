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
    /// In a small graph every pair of nodes pushes on each other exactly. Past <see cref="ExactRepulsionLimit"/> nodes the
    /// push of a far-off group of nodes is taken as that of one heavy node at their centre (Barnes-Hut, with
    /// <see cref="BlowForceLayoutOptions.Theta"/> saying how far off is far enough), so an iteration costs n log n
    /// rather than n²: 5,000 nodes lay out in about a second.
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
            var (left, top) = TopLeft(nodes[i], sizes[i], opts);
            // A position that is not a number, or so far out that its square overflows, is no place to start from:
            // the node starts on the spiral instead. A pinned node at such a place is never moved, so it only stands in
            // on the spiral for the others to arrange round.
            if (Usable(left) && Usable(top) && (pinned[i] || opts.StartFromCurrentPositions))
            {
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
        var theta = double.IsNaN(opts.Theta) ? 0 : Math.Min(opts.Theta, 2);
        var tree = n > ExactRepulsionLimit && theta > 0 ? new RepulsionTree(n) : null;
        for (var step = 0; step < iterations; step++)
        {
            Array.Clear(dx);
            Array.Clear(dy);

            // Every pair repels: k^2 / d - exactly, or with far-off groups of nodes taken as one.
            if (tree is null)
            {
                for (var i = 0; i < n; i++)
                {
                    for (var j = i + 1; j < n; j++)
                    {
                        var (fx, fy) = Repulsion(x, y, i, j, k * k);
                        dx[i] += fx;
                        dy[i] += fy;
                        dx[j] -= fx;
                        dy[j] -= fy;
                    }
                }
            }
            else
            {
                tree.Build(x, y);
                for (var i = 0; i < n; i++)
                {
                    var (fx, fy) = tree.Repel(i, x, y, k * k, theta * theta);
                    dx[i] += fx;
                    dy[i] += fy;
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
                // No push at all, or one that overflowed (a pinned node at an absurd distance): the node stays put.
                if (length < 1e-9 || !double.IsFinite(length)) continue;
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

    /// <summary>
    /// The most nodes a force layout pushes apart pair by pair; a larger graph takes far-off groups as one
    /// (<see cref="BlowForceLayoutOptions.Theta"/>). Below about this size the exact sums are the faster.
    /// </summary>
    public const int ExactRepulsionLimit = 500;

    /// <summary>How node <paramref name="j"/> pushes node <paramref name="i"/>: k² / d, away from it.</summary>
    internal static (double X, double Y) Repulsion(double[] x, double[] y, int i, int j, double k2)
    {
        var ex = x[i] - x[j];
        var ey = y[i] - y[j];
        var d2 = ex * ex + ey * ey;
        if (d2 < 0.01)
        {
            // Two nodes on the same point: part them along a direction that depends only on their indices, the lower
            // index going one way and the higher the other.
            var (low, high) = i < j ? (i, j) : (j, i);
            ex = 0.1 * (1 + (low % 7));
            ey = 0.1 * (1 + (high % 5));
            if (i > j) (ex, ey) = (-ex, -ey);
            d2 = ex * ex + ey * ey;
        }
        var force = k2 / d2;
        return (ex * force, ey * force);
    }

    /// <summary>
    /// A quadtree over the nodes' centres for Barnes-Hut repulsion. Every cell knows how many nodes it holds and their
    /// mean position, so a cell small enough for its distance - and not holding the node being pushed - pushes as one
    /// node of that weight. A leaf holds up to <see cref="LeafSize"/> nodes, pushed exactly. Built afresh each iteration,
    /// into arrays kept between them; deterministic, so the layout is.
    /// </summary>
    internal sealed class RepulsionTree
    {
        /// <summary>The most nodes a leaf holds before it splits. Pushing a handful exactly is cheaper than a deeper tree.</summary>
        private const int LeafSize = 8;

        /// <summary>Deeper than this, a leaf takes any number of nodes rather than splitting: only piled-up nodes get here.</summary>
        private const int MaxDepth = 40;

        private double[] _left, _top, _size, _sumX, _sumY;
        private int[] _count, _firstChild, _firstNode;
        private readonly int[] _nextNode;
        private readonly int[] _stack = new int[4 * MaxDepth + 8];
        private int _cells;

        public RepulsionTree(int n)
        {
            var capacity = n + 1;
            _left = new double[capacity];
            _top = new double[capacity];
            _size = new double[capacity];
            _sumX = new double[capacity];
            _sumY = new double[capacity];
            _count = new int[capacity];
            _firstChild = new int[capacity];
            _firstNode = new int[capacity];
            _nextNode = new int[n];
        }

        public void Build(double[] x, double[] y)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            for (var i = 0; i < x.Length; i++)
            {
                minX = Math.Min(minX, x[i]);
                minY = Math.Min(minY, y[i]);
                maxX = Math.Max(maxX, x[i]);
                maxY = Math.Max(maxY, y[i]);
            }

            // A square a little larger than the points' extent, so the furthest ones fall inside it.
            var size = Math.Max(maxX - minX, maxY - minY);
            size += size * 1e-9 + 1e-9;
            _cells = 0;
            NewCell(minX, minY, size);
            for (var i = 0; i < x.Length; i++) Insert(i, x, y);

            // From here on a cell's sums are its centre of weight.
            for (var c = 0; c < _cells; c++)
            {
                if (_count[c] == 0) continue;
                _sumX[c] /= _count[c];
                _sumY[c] /= _count[c];
            }
        }

        private int NewCell(double left, double top, double size)
        {
            if (_cells == _count.Length) Grow();
            var c = _cells++;
            (_left[c], _top[c], _size[c]) = (left, top, size);
            (_sumX[c], _sumY[c], _count[c]) = (0, 0, 0);
            (_firstChild[c], _firstNode[c]) = (-1, -1);
            return c;
        }

        private void Grow()
        {
            var capacity = _count.Length * 2;
            Array.Resize(ref _left, capacity);
            Array.Resize(ref _top, capacity);
            Array.Resize(ref _size, capacity);
            Array.Resize(ref _sumX, capacity);
            Array.Resize(ref _sumY, capacity);
            Array.Resize(ref _count, capacity);
            Array.Resize(ref _firstChild, capacity);
            Array.Resize(ref _firstNode, capacity);
        }

        private int Quadrant(int cell, double px, double py)
        {
            var half = _size[cell] / 2;
            return (py >= _top[cell] + half ? 2 : 0) + (px >= _left[cell] + half ? 1 : 0);
        }

        private void Insert(int i, double[] x, double[] y)
        {
            var cell = 0;
            for (var depth = 0; ; depth++)
            {
                if (_firstChild[cell] < 0)
                {
                    if (_count[cell] < LeafSize || depth >= MaxDepth)
                    {
                        _count[cell]++;
                        _sumX[cell] += x[i];
                        _sumY[cell] += y[i];
                        _nextNode[i] = _firstNode[cell];
                        _firstNode[cell] = i;
                        return;
                    }
                    Split(cell, x, y);
                }

                _count[cell]++;
                _sumX[cell] += x[i];
                _sumY[cell] += y[i];
                cell = _firstChild[cell] + Quadrant(cell, x[i], y[i]);
            }
        }

        /// <summary>Turns a full leaf into four quarters, its nodes moved into them.</summary>
        private void Split(int cell, double[] x, double[] y)
        {
            var half = _size[cell] / 2;
            var first = NewCell(_left[cell], _top[cell], half);
            NewCell(_left[cell] + half, _top[cell], half);
            NewCell(_left[cell], _top[cell] + half, half);
            NewCell(_left[cell] + half, _top[cell] + half, half);
            _firstChild[cell] = first;

            for (var node = _firstNode[cell]; node >= 0;)
            {
                var next = _nextNode[node];
                var child = first + Quadrant(cell, x[node], y[node]);
                _nextNode[node] = _firstNode[child];
                _firstNode[child] = node;
                _count[child]++;
                _sumX[child] += x[node];
                _sumY[child] += y[node];
                node = next;
            }
            _firstNode[cell] = -1;
        }

        /// <summary>The push on node <paramref name="i"/> from all the others.</summary>
        public (double X, double Y) Repel(int i, double[] x, double[] y, double k2, double theta2)
        {
            double fx = 0, fy = 0;
            var xi = x[i];
            var yi = y[i];
            var stack = _stack;
            var top = 0;
            stack[top++] = 0;
            while (top > 0)
            {
                var cell = stack[--top];
                var first = _firstChild[cell];
                if (first < 0)
                {
                    for (var j = _firstNode[cell]; j >= 0; j = _nextNode[j])
                    {
                        if (j == i) continue;
                        var ex = xi - x[j];
                        var ey = yi - y[j];
                        var d2 = ex * ex + ey * ey;
                        if (d2 < 0.01)
                        {
                            var (px, py) = Repulsion(x, y, i, j, k2);
                            fx += px;
                            fy += py;
                            continue;
                        }
                        var force = k2 / d2;
                        fx += ex * force;
                        fy += ey * force;
                    }
                    continue;
                }

                var cx = xi - _sumX[cell];
                var cy = yi - _sumY[cell];
                var cd2 = cx * cx + cy * cy;
                var size = _size[cell];
                if (size * size < theta2 * cd2
                    && !(xi >= _left[cell] && xi < _left[cell] + size && yi >= _top[cell] && yi < _top[cell] + size))
                {
                    var force = k2 * _count[cell] / cd2;
                    fx += cx * force;
                    fy += cy * force;
                    continue;
                }

                for (var q = 3; q >= 0; q--)
                {
                    if (_count[first + q] > 0) stack[top++] = first + q;
                }
            }
            return (fx, fy);
        }
    }

    /// <summary>Whether a coordinate can take part in the simulation: a number, and not so large that its square overflows.</summary>
    private static bool Usable(double coordinate) => double.IsFinite(coordinate) && Math.Abs(coordinate) < 1e12;

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
                    // Written so a coordinate that is not a number (a pinned node's) overlaps nothing.
                    if (!(overlapX > 0) || !(overlapY > 0)) continue;

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
                        // A coordinate that is not a number overlaps nothing; treated as overlapping, it never stops.
                        if (!(overlapX > 1e-9) || !(overlapY > 1e-9)) continue;
                        x[i] = x[j] + (sizes[i].Width + sizes[j].Width) / 2 + gap;
                        moved = true;
                    }
                }
            }
            placed.Add(i);
        }
    }
}
