namespace Bit.Blow;

public static partial class BlowLayout
{
    /// <summary>
    /// Arranges a graph as a tree - an org chart, a file system, a decision tree: each node centred over the nodes it
    /// leads to, every subtree in a band of its own, so nothing overlaps however the branches differ in size.
    /// </summary>
    /// <remarks>
    /// The tree is the graph's breadth-first spanning forest: a node hangs under the first node found leading to it,
    /// starting from the roots (nodes nothing leads to) in the order given, and then from the first node not yet
    /// reached, which is how a cycle gets a root. Edges outside the tree are left to the edge renderer. Linear in the
    /// size of the graph, and iterative, so a ten-thousand-deep chain is laid out like any other.
    /// </remarks>
    public static void Tree(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges, BlowTreeLayoutOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        var opts = options ?? new BlowTreeLayoutOptions();
        if (nodes.Count == 0) return;

        var sizes = ResolveSizes(nodes, opts.FallbackSize);
        var (outgoing, incoming) = BuildAdjacency(nodes, edges);
        var forest = SpanningForest(nodes.Count, outgoing, incoming);

        var horizontal = opts.Direction is BlowLayoutDirection.LeftToRight or BlowLayoutDirection.RightToLeft;
        var reversed = opts.Direction is BlowLayoutDirection.BottomToTop or BlowLayoutDirection.RightToLeft;
        double Across(int i) => horizontal ? sizes[i].Height : sizes[i].Width;   // along a level
        double Along(int i) => horizontal ? sizes[i].Width : sizes[i].Height;    // from level to level

        // How wide a band each subtree needs: the node itself, or its children side by side, whichever is wider.
        // Children come after their parent in the breadth-first order, so walking it backwards visits them first.
        var band = new double[nodes.Count];
        for (var k = forest.Order.Count - 1; k >= 0; k--)
        {
            var i = forest.Order[k];
            var children = forest.Children[i];
            var sum = 0.0;
            foreach (var c in children) sum += band[c];
            if (children.Count > 1) sum += (children.Count - 1) * opts.NodeGap;
            band[i] = Math.Max(Across(i), sum);
        }

        // Each level is as thick as its thickest node.
        var levels = 0;
        foreach (var d in forest.Depth) levels = Math.Max(levels, d + 1);
        var thickness = new double[levels];
        for (var i = 0; i < nodes.Count; i++) thickness[forest.Depth[i]] = Math.Max(thickness[forest.Depth[i]], Along(i));
        var levelStart = new double[levels];
        for (var l = 1; l < levels; l++) levelStart[l] = levelStart[l - 1] + thickness[l - 1] + opts.LevelGap;
        var total = levelStart[levels - 1] + thickness[levels - 1];

        // Each node is centred in its band, and its children's bands are centred under it - parents first.
        var bandStart = new double[nodes.Count];
        var cursor = 0.0;
        foreach (var root in forest.Roots)
        {
            bandStart[root] = cursor;
            cursor += band[root] + opts.NodeGap;
        }
        foreach (var i in forest.Order)
        {
            var children = forest.Children[i];
            if (children.Count == 0) continue;
            var sum = 0.0;
            foreach (var c in children) sum += band[c];
            sum += (children.Count - 1) * opts.NodeGap;
            var at = bandStart[i] + (band[i] - sum) / 2;
            foreach (var c in children)
            {
                bandStart[c] = at;
                at += band[c] + opts.NodeGap;
            }
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var across = bandStart[i] + (band[i] - Across(i)) / 2;
            var level = forest.Depth[i];
            var along = levelStart[level] + (thickness[level] - Along(i)) / 2;
            if (reversed) along = total - along - Along(i);

            if (horizontal) Place(nodes[i], sizes[i], opts, opts.Origin.X + along, opts.Origin.Y + across);
            else Place(nodes[i], sizes[i], opts, opts.Origin.X + across, opts.Origin.Y + along);
        }

        if (opts.AlignHandlesToDirection) AlignHandles(nodes, opts.Direction);
    }

    /// <summary>
    /// Arranges a graph in rings around its root - a mind map, a network seen from one machine: the root at the
    /// centre, each node one ring further out than the node leading to it, each subtree in a wedge sized by how much
    /// it holds. Rings are spaced and sized so no two nodes overlap.
    /// </summary>
    /// <remarks>
    /// The tree is the same breadth-first spanning forest <see cref="Tree"/> uses. A graph with several roots gets an
    /// empty centre, with the roots on the first ring. <see cref="BlowLayoutOptions.Origin"/> is the top-left of the
    /// result, as for every layout.
    /// </remarks>
    public static void Radial(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges, BlowRadialLayoutOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        var opts = options ?? new BlowRadialLayoutOptions();
        if (nodes.Count == 0) return;

        var sizes = ResolveSizes(nodes, opts.FallbackSize);
        var (outgoing, incoming) = BuildAdjacency(nodes, edges);
        var forest = SpanningForest(nodes.Count, outgoing, incoming, opts.IsRoot is { } isRoot ? i => isRoot(nodes[i]) : null);

        // With one root it is the centre; with several, the centre is empty and every ring moves out by one.
        var shift = forest.Roots.Count == 1 ? 0 : 1;
        var ring = new int[nodes.Count];
        var rings = 0;
        for (var i = 0; i < nodes.Count; i++)
        {
            ring[i] = forest.Depth[i] + shift;
            rings = Math.Max(rings, ring[i] + 1);
        }

        // A subtree's wedge is in proportion to its leaves, so a crowded branch gets the room it needs.
        var leaves = new double[nodes.Count];
        for (var k = forest.Order.Count - 1; k >= 0; k--)
        {
            var i = forest.Order[k];
            var sum = 0.0;
            foreach (var c in forest.Children[i]) sum += leaves[c];
            leaves[i] = Math.Max(1, sum);
        }

        var start = new double[nodes.Count];
        var sweep = new double[nodes.Count];
        var allLeaves = 0.0;
        foreach (var root in forest.Roots) allLeaves += leaves[root];
        var angle = opts.StartAngle * Math.PI / 180;
        foreach (var root in forest.Roots)
        {
            start[root] = angle;
            sweep[root] = 2 * Math.PI * leaves[root] / allLeaves;
            angle += sweep[root];
        }
        foreach (var i in forest.Order)
        {
            var at = start[i];
            foreach (var c in forest.Children[i])
            {
                start[c] = at;
                sweep[c] = sweep[i] * leaves[c] / leaves[i];
                at += sweep[c];
            }
        }

        // A node is kept clear of the others by the circle round its box. On a ring, neighbours are at least the
        // narrowest wedge apart, so the ring has to be big enough for that chord to clear two such circles; and each
        // ring has to clear the one inside it.
        var reach = new double[rings];
        var narrowest = new double[rings];
        var count = new int[rings];
        Array.Fill(narrowest, 2 * Math.PI);
        for (var i = 0; i < nodes.Count; i++)
        {
            reach[ring[i]] = Math.Max(reach[ring[i]], Math.Sqrt(sizes[i].Width * sizes[i].Width + sizes[i].Height * sizes[i].Height) / 2);
            narrowest[ring[i]] = Math.Min(narrowest[ring[i]], sweep[i]);
            count[ring[i]]++;
        }

        var radius = new double[rings];
        for (var r = 1; r < rings; r++)
        {
            var clearInner = radius[r - 1] + reach[r - 1] + reach[r] + opts.RingGap;
            var clearNeighbours = count[r] < 2 ? 0 : (2 * reach[r] + opts.NodeGap) / (2 * Math.Sin(Math.Min(narrowest[r], Math.PI) / 2));
            radius[r] = Math.Max(clearInner, clearNeighbours);
        }

        // Centres first, then the whole arrangement moved so its top-left is the origin.
        var centres = new BlowXYPosition[nodes.Count];
        double left = double.MaxValue, top = double.MaxValue;
        for (var i = 0; i < nodes.Count; i++)
        {
            var mid = start[i] + sweep[i] / 2 - Math.PI / 2; // twelve o'clock, clockwise
            var r = radius[ring[i]];
            centres[i] = new BlowXYPosition(Math.Cos(mid) * r, Math.Sin(mid) * r);
            left = Math.Min(left, centres[i].X - sizes[i].Width / 2);
            top = Math.Min(top, centres[i].Y - sizes[i].Height / 2);
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            Place(nodes[i], sizes[i], opts,
                opts.Origin.X + centres[i].X - sizes[i].Width / 2 - left,
                opts.Origin.Y + centres[i].Y - sizes[i].Height / 2 - top);
        }
    }

    /// <summary>A breadth-first spanning forest: who hangs under whom, how deep, and the order nodes were reached in.</summary>
    private sealed record Forest(List<int> Roots, List<int>[] Children, int[] Depth, List<int> Order);

    private static Forest SpanningForest(int count, List<int>[] outgoing, List<int>[] incoming, Func<int, bool>? isRoot = null)
    {
        var children = new List<int>[count];
        for (var i = 0; i < count; i++) children[i] = [];
        var depth = new int[count];
        var reached = new bool[count];
        var roots = new List<int>();
        var order = new List<int>(count);
        var queue = new Queue<int>();

        void Grow(int root)
        {
            roots.Add(root);
            reached[root] = true;
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                order.Add(n);
                foreach (var next in outgoing[n])
                {
                    if (reached[next]) continue;
                    reached[next] = true;
                    depth[next] = depth[n] + 1;
                    children[n].Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        // The roots asked for, else the nodes nothing leads to; then whatever a cycle kept out of reach. Roots asked
        // for are claimed before any tree grows, so one reachable from another is still a root of its own.
        if (isRoot is not null)
        {
            var asked = Enumerable.Range(0, count).Where(isRoot).ToList();
            foreach (var root in asked) reached[root] = true;
            foreach (var root in asked)
            {
                reached[root] = false;
                Grow(root);
            }
        }
        for (var i = 0; i < count; i++)
        {
            if (!reached[i] && isRoot is null && incoming[i].Count == 0) Grow(i);
        }
        for (var i = 0; i < count; i++)
        {
            if (!reached[i]) Grow(i);
        }

        return new Forest(roots, children, depth, order);
    }
}
