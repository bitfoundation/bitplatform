namespace Bit.Blow;

/// <summary>
/// Arranges nodes automatically. These are the layouts a flow editor reaches for constantly -
/// tidying an imported graph, laying out a tree, dropping a set of nodes into a grid - and
/// having them in the box means not pulling in a JavaScript layout engine to get them.
/// </summary>
/// <remarks>
/// Every method writes to <see cref="BlowNode.Position"/> (and the layered layout, unless
/// <see cref="BlowLayeredLayoutOptions.AlignHandlesToDirection"/> is off, to the nodes' source and target
/// handle sides) and nothing else, so the result composes
/// with the rest of the library: call one, then <c>FitView()</c>. Positions are parent-relative,
/// so lay out one parent's children at a time (or a set of root nodes) rather than mixing depths
/// in a single call.
/// </remarks>
public static class BlowLayout
{
    /// <summary>
    /// Arranges a directed graph in layers, the way a flow chart or an org chart is drawn:
    /// every node sits one layer beyond the furthest node pointing at it, and nodes within a
    /// layer are ordered to keep edges from crossing.
    /// </summary>
    /// <remarks>
    /// A Sugiyama-style layout, minus the parts that need an optimisation solver: layers come
    /// from a topological longest path, ordering from repeated barycentre sweeps. Cycles do not
    /// break it - the edges that close them simply are not allowed to push a node further out.
    /// </remarks>
    public static void Layered(
        IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges, BlowLayeredLayoutOptions? options = null)
    {
        var opts = options ?? new BlowLayeredLayoutOptions();
        if (nodes.Count == 0) return;

        var sizes = ResolveSizes(nodes, opts.FallbackSize);
        var (outgoing, incoming) = BuildAdjacency(nodes, edges);

        var layers = AssignLayers(nodes.Count, outgoing, incoming);
        OrderWithinLayers(layers, outgoing, incoming, opts.CrossingReductionPasses);
        PositionLayers(nodes, sizes, layers, opts);

        if (opts.AlignHandlesToDirection) AlignHandles(nodes, opts.Direction);
    }

    /// <summary>Arranges nodes in a grid, row by row, in the order they were given.</summary>
    public static void Grid(IReadOnlyList<BlowNode> nodes, BlowGridLayoutOptions? options = null)
    {
        var opts = options ?? new BlowGridLayoutOptions();
        if (nodes.Count == 0) return;

        var sizes = ResolveSizes(nodes, opts.FallbackSize);

        var columns = opts.Columns is { } requested && requested > 0
            ? requested
            : (int)Math.Ceiling(Math.Sqrt(nodes.Count));

        // Uniform cells keep the grid reading as a grid even when the nodes differ in size.
        var cellWidth = 0.0;
        var cellHeight = 0.0;
        foreach (var size in sizes)
        {
            cellWidth = Math.Max(cellWidth, size.Width);
            cellHeight = Math.Max(cellHeight, size.Height);
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var column = i % columns;
            var row = i / columns;

            Place(nodes[i], sizes[i], opts,
                opts.Origin.X + column * (cellWidth + opts.ColumnGap) + (cellWidth - sizes[i].Width) / 2,
                opts.Origin.Y + row * (cellHeight + opts.RowGap) + (cellHeight - sizes[i].Height) / 2);
        }
    }

    /// <summary>
    /// Puts a node's box at the given top-left, writing whichever point of it the node's
    /// <see cref="BlowNode.Position"/> names (see <see cref="BlowLayoutOptions.NodeOrigin"/>).
    /// </summary>
    private static void Place(BlowNode node, BlowDimensions size, BlowLayoutOptions opts, double left, double top)
    {
        var origin = node.Origin ?? opts.NodeOrigin;
        node.Position = new BlowXYPosition(left + origin.X * size.Width, top + origin.Y * size.Height);
    }

    /// <summary>
    /// Arranges nodes evenly around a ring. Useful for showing that a set of nodes are peers
    /// rather than a hierarchy, and for untangling a small densely-connected graph.
    /// </summary>
    public static void Circle(IReadOnlyList<BlowNode> nodes, BlowCircleLayoutOptions? options = null)
    {
        var opts = options ?? new BlowCircleLayoutOptions();
        if (nodes.Count == 0) return;

        var sizes = ResolveSizes(nodes, opts.FallbackSize);

        if (nodes.Count == 1)
        {
            Place(nodes[0], sizes[0], opts, opts.Origin.X, opts.Origin.Y);
            return;
        }

        var widest = 0.0;
        var tallest = 0.0;
        foreach (var size in sizes)
        {
            widest = Math.Max(widest, size.Width);
            tallest = Math.Max(tallest, size.Height);
        }

        // Neighbours on a ring sit side by side near the top and bottom but stacked near the
        // sides, so they have to clear each other along whichever dimension is larger.
        var radius = opts.Radius ?? DeriveRadius(nodes.Count, Math.Max(widest, tallest), opts.NodeGap);

        // The origin names the top-left of the ring's bounding box, matching every other layout.
        var centreX = opts.Origin.X + radius + widest / 2;
        var centreY = opts.Origin.Y + radius + tallest / 2;

        for (var i = 0; i < nodes.Count; i++)
        {
            // Start at twelve o'clock and go clockwise, which is how a ring is read.
            var angle = (opts.StartAngle - 90) * Math.PI / 180 + i * 2 * Math.PI / nodes.Count;

            Place(nodes[i], sizes[i], opts,
                centreX + Math.Cos(angle) * radius - sizes[i].Width / 2,
                centreY + Math.Sin(angle) * radius - sizes[i].Height / 2);
        }
    }

    /// <summary>Radius at which neighbours on the ring clear each other.</summary>
    private static double DeriveRadius(int count, double largest, double gap)
    {
        // The chord between neighbours is 2 * r * sin(pi / n); solve it for the room we need.
        var needed = largest + gap;
        var chord = 2 * Math.Sin(Math.PI / count);
        return chord > 1e-6 ? Math.Max(needed, needed / chord) : needed * count;
    }

    /// <summary>
    /// The size to lay each node out at. A node that has been given an explicit size, or that
    /// the canvas has already measured, is taken at its word; anything else is a node that has
    /// never been on screen, and the caller's fallback describes it better than the library's
    /// generic default could.
    /// </summary>
    private static BlowDimensions[] ResolveSizes(IReadOnlyList<BlowNode> nodes, BlowDimensions fallback)
    {
        var sizes = new BlowDimensions[nodes.Count];
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var measured = node.Measured;

            var width = node.Width ?? (measured.Width > 0 ? measured.Width : fallback.Width);
            var height = node.Height ?? (measured.Height > 0 ? measured.Height : fallback.Height);

            sizes[i] = new BlowDimensions(Math.Max(1, width), Math.Max(1, height));
        }
        return sizes;
    }

    // ---- layered layout internals ----

    /// <summary>
    /// Adjacency as index lists. Edges touching a node outside the set being laid out are
    /// ignored, so laying out a subset behaves as if the rest of the graph were not there.
    /// </summary>
    private static (List<int>[] Outgoing, List<int>[] Incoming) BuildAdjacency(
        IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges)
    {
        var index = new Dictionary<string, int>(nodes.Count);
        for (var i = 0; i < nodes.Count; i++) index.TryAdd(nodes[i].Id, i);

        var outgoing = new List<int>[nodes.Count];
        var incoming = new List<int>[nodes.Count];
        for (var i = 0; i < nodes.Count; i++)
        {
            outgoing[i] = [];
            incoming[i] = [];
        }

        foreach (var e in edges)
        {
            if (!index.TryGetValue(e.Source, out var s) || !index.TryGetValue(e.Target, out var t)) continue;
            if (s == t) continue; // a self-loop says nothing about layering
            outgoing[s].Add(t);
            incoming[t].Add(s);
        }

        return (outgoing, incoming);
    }

    /// <summary>
    /// Groups nodes into layers by longest path from a root. Nodes are visited in topological
    /// order, so each is placed once every node that must precede it already has been; a cycle is
    /// broken where the ordering enters it rather than looping forever.
    /// </summary>
    private static List<List<int>> AssignLayers(int count, List<int>[] outgoing, List<int>[] incoming)
    {
        var order = TopologicalOrder(count, outgoing, incoming);

        var layer = new int[count];
        var placed = new bool[count];

        foreach (var i in order)
        {
            var deepest = -1;
            foreach (var predecessor in incoming[i])
            {
                // Only predecessors already placed count. The rest close a cycle, and letting
                // one of those push this node outward is exactly how a cycle runs away.
                if (placed[predecessor]) deepest = Math.Max(deepest, layer[predecessor]);
            }
            layer[i] = deepest + 1;
            placed[i] = true;
        }

        var depth = 0;
        foreach (var l in layer) depth = Math.Max(depth, l);

        var layers = new List<List<int>>(depth + 1);
        for (var i = 0; i <= depth; i++) layers.Add([]);

        // Walking the topological order rather than the node list seeds each layer with a
        // sensible starting sequence, which the crossing-reduction sweeps then refine.
        foreach (var i in order) layers[layer[i]].Add(i);

        return layers;
    }

    /// <summary>
    /// Kahn's algorithm, breaking cycles as it meets them. When every node left is waiting on
    /// another one, the cycle is entered at the node it most plausibly starts at - one already
    /// reached from the ordered part of the graph, waiting on as few others as possible - and the
    /// sort carries on from there. Simply appending the leftovers in list order instead would
    /// put a node downstream of a cycle (the "End" after a retry loop) ahead of the cycle itself.
    /// </summary>
    private static List<int> TopologicalOrder(int count, List<int>[] outgoing, List<int>[] incoming)
    {
        var remaining = new int[count];
        for (var i = 0; i < count; i++) remaining[i] = incoming[i].Count;

        var queue = new Queue<int>();
        for (var i = 0; i < count; i++)
        {
            if (remaining[i] == 0) queue.Enqueue(i);
        }

        var order = new List<int>(count);
        var visited = new bool[count];

        while (order.Count < count)
        {
            if (queue.Count == 0) queue.Enqueue(CycleEntry(count, incoming, remaining, visited));

            var i = queue.Dequeue();
            if (visited[i]) continue;
            order.Add(i);
            visited[i] = true;

            foreach (var next in outgoing[i])
            {
                if (!visited[next] && --remaining[next] == 0) queue.Enqueue(next);
            }
        }

        return order;
    }

    /// <summary>The unvisited node at which to break into a cycle; see <see cref="TopologicalOrder"/>.</summary>
    private static int CycleEntry(int count, List<int>[] incoming, int[] remaining, bool[] visited)
    {
        var best = -1;
        var bestReached = false;
        for (var i = 0; i < count; i++)
        {
            if (visited[i]) continue;

            // Reached: at least one of its predecessors has already been ordered.
            var reached = remaining[i] < incoming[i].Count;
            if (best < 0
                || (reached && !bestReached)
                || (reached == bestReached && remaining[i] < remaining[best]))
            {
                best = i;
                bestReached = reached;
            }
        }
        return best;
    }

    /// <summary>
    /// Reorders each layer so edges cross as little as possible, by repeatedly moving every node
    /// to the average position of its neighbours in the adjacent layer. Sweeping down and then
    /// back up lets an improvement propagate in both directions.
    /// </summary>
    private static void OrderWithinLayers(
        List<List<int>> layers, List<int>[] outgoing, List<int>[] incoming, int passes)
    {
        if (passes <= 0 || layers.Count < 2) return;

        var position = new Dictionary<int, int>();
        RecordPositions(layers, position);

        // Each layer's new order is recorded as soon as it is sorted, so the next layer in the
        // sweep is sorted against it - that is what lets one sweep carry an improvement all the
        // way down (or up) the graph rather than a single layer per pass.
        for (var pass = 0; pass < passes; pass++)
        {
            if (pass % 2 == 0)
            {
                for (var l = 1; l < layers.Count; l++)
                {
                    SortByBarycentre(layers[l], incoming, position);
                    RecordPositions(layers[l], position);
                }
            }
            else
            {
                for (var l = layers.Count - 2; l >= 0; l--)
                {
                    SortByBarycentre(layers[l], outgoing, position);
                    RecordPositions(layers[l], position);
                }
            }
        }
    }

    private static void RecordPositions(List<List<int>> layers, Dictionary<int, int> position)
    {
        foreach (var layer in layers) RecordPositions(layer, position);
    }

    private static void RecordPositions(List<int> layer, Dictionary<int, int> position)
    {
        for (var i = 0; i < layer.Count; i++) position[layer[i]] = i;
    }

    private static void SortByBarycentre(List<int> layer, List<int>[] neighbours, Dictionary<int, int> position)
    {
        if (layer.Count < 2) return;

        // A node with no neighbours in the adjacent layer has no opinion, so it keeps its
        // current slot rather than being swept to one end.
        var keyed = new (double Key, int Order, int Node)[layer.Count];
        for (var i = 0; i < layer.Count; i++)
        {
            var node = layer[i];

            var sum = 0.0;
            var counted = 0;
            foreach (var n in neighbours[node])
            {
                if (!position.TryGetValue(n, out var p)) continue;
                sum += p;
                counted++;
            }

            keyed[i] = (counted > 0 ? sum / counted : i, i, node);
        }

        Array.Sort(keyed, static (a, b) =>
            a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.Order.CompareTo(b.Order));

        for (var i = 0; i < keyed.Length; i++) layer[i] = keyed[i].Node;
    }

    /// <summary>
    /// Turns layer membership and within-layer order into coordinates. Layers are packed by
    /// their own thickness rather than a fixed step, so a layer of tall nodes does not overlap
    /// the next one, and each layer is centred against the widest so the result looks balanced.
    /// </summary>
    private static void PositionLayers(
        IReadOnlyList<BlowNode> nodes, BlowDimensions[] sizes, List<List<int>> layers, BlowLayeredLayoutOptions opts)
    {
        var horizontal = opts.Direction is BlowLayoutDirection.LeftToRight or BlowLayoutDirection.RightToLeft;
        var reversed = opts.Direction is BlowLayoutDirection.BottomToTop or BlowLayoutDirection.RightToLeft;

        // "Along" runs within a layer; "across" runs from one layer to the next.
        var alongExtent = new double[layers.Count];
        var acrossExtent = new double[layers.Count];

        for (var l = 0; l < layers.Count; l++)
        {
            var along = 0.0;
            var across = 0.0;
            foreach (var i in layers[l])
            {
                along += horizontal ? sizes[i].Height : sizes[i].Width;
                across = Math.Max(across, horizontal ? sizes[i].Width : sizes[i].Height);
            }
            if (layers[l].Count > 1) along += (layers[l].Count - 1) * opts.NodeGap;

            alongExtent[l] = along;
            acrossExtent[l] = across;
        }

        var widest = 0.0;
        foreach (var extent in alongExtent) widest = Math.Max(widest, extent);

        var totalAcross = 0.0;
        foreach (var extent in acrossExtent) totalAcross += extent;
        if (layers.Count > 1) totalAcross += (layers.Count - 1) * opts.LayerGap;

        var acrossCursor = 0.0;
        for (var l = 0; l < layers.Count; l++)
        {
            var alongCursor = (widest - alongExtent[l]) / 2;

            foreach (var i in layers[l])
            {
                var alongSize = horizontal ? sizes[i].Height : sizes[i].Width;
                var acrossSize = horizontal ? sizes[i].Width : sizes[i].Height;

                // Centre each node within its layer's thickness, so a short node in a tall layer
                // sits on the layer's midline rather than its leading edge.
                var offset = acrossCursor + (acrossExtent[l] - acrossSize) / 2;
                var placed = reversed ? totalAcross - offset - acrossSize : offset;

                if (horizontal) Place(nodes[i], sizes[i], opts, opts.Origin.X + placed, opts.Origin.Y + alongCursor);
                else Place(nodes[i], sizes[i], opts, opts.Origin.X + alongCursor, opts.Origin.Y + placed);

                alongCursor += alongSize + opts.NodeGap;
            }

            acrossCursor += acrossExtent[l] + opts.LayerGap;
        }
    }

    /// <summary>
    /// Points every node's handles along the flow, so a left-to-right layout routes its edges
    /// left-to-right instead of top-to-bottom. Only the built-in node renderer reads these;
    /// a custom template places its own handles.
    /// </summary>
    private static void AlignHandles(IReadOnlyList<BlowNode> nodes, BlowLayoutDirection direction)
    {
        var (source, target) = direction switch
        {
            BlowLayoutDirection.LeftToRight => (BlowPosition.Right, BlowPosition.Left),
            BlowLayoutDirection.RightToLeft => (BlowPosition.Left, BlowPosition.Right),
            BlowLayoutDirection.BottomToTop => (BlowPosition.Top, BlowPosition.Bottom),
            _ => (BlowPosition.Bottom, BlowPosition.Top),
        };

        foreach (var n in nodes)
        {
            n.SourcePosition = source;
            n.TargetPosition = target;
        }
    }
}
