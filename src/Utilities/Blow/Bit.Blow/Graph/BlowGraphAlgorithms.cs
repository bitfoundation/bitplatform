namespace Bit.Blow;

/// <summary>Which edges a traversal follows from a node.</summary>
public enum BlowTraversalDirection
{
    /// <summary>Along edges leaving the node, towards its targets.</summary>
    Outgoing,

    /// <summary>Against edges arriving at the node, towards its sources.</summary>
    Incoming,

    /// <summary>Along every edge attached to the node, ignoring direction.</summary>
    Both,
}

/// <summary>
/// The result of <see cref="BlowGraphAlgorithms.TopologicalSort(BlowGraphIndex)"/>: the nodes that could be put
/// in dependency order, and those that could not because they are on a cycle or downstream of one.
/// </summary>
/// <param name="Order">Every node after all the nodes with an edge into it. Ties keep the original node order.</param>
/// <param name="Unordered">The nodes left over because a cycle blocks them, in original node order.</param>
public sealed record BlowTopologicalOrder(IReadOnlyList<BlowNode> Order, IReadOnlyList<BlowNode> Unordered)
{
    /// <summary>True when every node could be ordered, which is when the graph has no cycle.</summary>
    public bool IsAcyclic => Unordered.Count == 0;
}

/// <summary>Counts that describe a graph's shape at a glance.</summary>
/// <param name="NodeCount">Nodes in the graph.</param>
/// <param name="EdgeCount">Edges between nodes in the graph (dangling edges are counted separately).</param>
/// <param name="RootCount">Nodes with no incoming edge.</param>
/// <param name="LeafCount">Nodes with no outgoing edge.</param>
/// <param name="IsolatedCount">Nodes with no edge at all.</param>
/// <param name="ComponentCount">Separate pieces the graph falls into, ignoring edge direction.</param>
/// <param name="SelfLoopCount">Edges from a node to itself.</param>
/// <param name="MaxInDegree">The most incoming edges any one node has.</param>
/// <param name="MaxOutDegree">The most outgoing edges any one node has.</param>
/// <param name="IsAcyclic">True when the graph has no directed cycle (self-loops included).</param>
/// <param name="DanglingEdgeCount">Edges naming a node that is not in the graph.</param>
public sealed record BlowGraphStatistics(
    int NodeCount, int EdgeCount, int RootCount, int LeafCount, int IsolatedCount, int ComponentCount,
    int SelfLoopCount, int MaxInDegree, int MaxOutDegree, bool IsAcyclic, int DanglingEdgeCount);

/// <summary>
/// The graph questions flow applications keep asking: what runs first, is there a cycle, what depends on this
/// node, can this reach that, which parts are disconnected, what is the shortest route. All of them run without a
/// UI, in time linear in the size of the graph (shortest weighted path: n log n), and none of them recurse, so a
/// graph of any depth cannot overflow the stack.
/// </summary>
/// <remarks>
/// Each method has an overload taking a <see cref="BlowGraphIndex"/>, for asking several questions about the same
/// graph without rebuilding the lookups each time, and one taking the node and edge lists. Edges whose source or
/// target is not in the node list are ignored. Hidden nodes and edges are treated like any other: pass only the
/// elements you want considered.
/// </remarks>
public static class BlowGraphAlgorithms
{
    // ---- ordering and cycles ----

    /// <summary>
    /// Puts the nodes in dependency order: every node comes after all the nodes with an edge into it - the order
    /// to run the steps of a pipeline in. Nodes that cannot be ordered because of a cycle are returned separately.
    /// </summary>
    public static BlowTopologicalOrder TopologicalSort(BlowGraphIndex graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        // Kahn's algorithm, seeded and drained in node order so the result is stable for a given input.
        var inDegree = new Dictionary<string, int>(graph.Nodes.Count, StringComparer.Ordinal);
        foreach (var n in graph.Nodes) inDegree[n.Id] = graph.Incoming(n.Id).Count;

        var order = new List<BlowNode>(graph.Nodes.Count);
        var ready = new Queue<BlowNode>();
        foreach (var n in graph.Nodes)
        {
            if (inDegree[n.Id] == 0) ready.Enqueue(n);
        }

        while (ready.Count > 0)
        {
            var n = ready.Dequeue();
            order.Add(n);
            foreach (var e in graph.Outgoing(n.Id))
            {
                if (--inDegree[e.Target] == 0) ready.Enqueue(graph.GetNode(e.Target)!);
            }
        }

        var unordered = order.Count == graph.Nodes.Count
            ? []
            : graph.Nodes.Where(n => inDegree[n.Id] > 0).ToList();
        return new BlowTopologicalOrder(order, unordered);
    }

    /// <inheritdoc cref="TopologicalSort(BlowGraphIndex)"/>
    public static BlowTopologicalOrder TopologicalSort(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges)
        => TopologicalSort(new BlowGraphIndex(nodes, edges));

    /// <summary>True when following edges can lead from a node back to itself. A self-loop is a cycle.</summary>
    public static bool HasCycle(BlowGraphIndex graph) => !TopologicalSort(graph).IsAcyclic;

    /// <inheritdoc cref="HasCycle(BlowGraphIndex)"/>
    public static bool HasCycle(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges)
        => HasCycle(new BlowGraphIndex(nodes, edges));

    /// <summary>
    /// Splits the graph into strongly connected components: groups of nodes that can each reach every other one
    /// along edges. A node on no cycle is a component of its own. Components come out in reverse topological
    /// order (a component before any component with an edge into it).
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<BlowNode>> StronglyConnectedComponents(BlowGraphIndex graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        // Tarjan's algorithm with an explicit stack of (node, next-edge) frames instead of recursion.
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        var low = new Dictionary<string, int>(StringComparer.Ordinal);
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<BlowNode>();
        var frames = new Stack<(BlowNode Node, int Edge)>();
        var components = new List<IReadOnlyList<BlowNode>>();
        var counter = 0;

        foreach (var root in graph.Nodes)
        {
            if (index.ContainsKey(root.Id)) continue;

            Visit(root);
            while (frames.Count > 0)
            {
                var (node, next) = frames.Pop();
                var outgoing = graph.Outgoing(node.Id);

                if (next < outgoing.Count)
                {
                    frames.Push((node, next + 1));
                    var target = graph.GetNode(outgoing[next].Target)!;
                    if (!index.ContainsKey(target.Id))
                    {
                        Visit(target);
                    }
                    else if (onStack.Contains(target.Id))
                    {
                        low[node.Id] = Math.Min(low[node.Id], index[target.Id]);
                    }
                    continue;
                }

                // Every edge of this node has been followed: hand its low-link to the caller's frame.
                if (frames.Count > 0)
                {
                    var parent = frames.Peek().Node;
                    low[parent.Id] = Math.Min(low[parent.Id], low[node.Id]);
                }

                if (low[node.Id] == index[node.Id])
                {
                    var component = new List<BlowNode>();
                    BlowNode member;
                    do
                    {
                        member = stack.Pop();
                        onStack.Remove(member.Id);
                        component.Add(member);
                    } while (!ReferenceEquals(member, node));
                    component.Reverse();
                    components.Add(component);
                }
            }
        }

        return components;

        void Visit(BlowNode n)
        {
            index[n.Id] = low[n.Id] = counter++;
            stack.Push(n);
            onStack.Add(n.Id);
            frames.Push((n, 0));
        }
    }

    /// <inheritdoc cref="StronglyConnectedComponents(BlowGraphIndex)"/>
    public static IReadOnlyList<IReadOnlyList<BlowNode>> StronglyConnectedComponents(
        IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges)
        => StronglyConnectedComponents(new BlowGraphIndex(nodes, edges));

    /// <summary>
    /// The cycles of the graph, one group of nodes per strongly connected component that holds a cycle: every
    /// node of a group can reach every other one. A node with a self-loop is a group of one.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<BlowNode>> FindCycles(BlowGraphIndex graph)
        => StronglyConnectedComponents(graph)
            .Where(c => c.Count > 1 || graph.Outgoing(c[0].Id).Any(e => e.Target == c[0].Id))
            .ToList();

    /// <inheritdoc cref="FindCycles(BlowGraphIndex)"/>
    public static IReadOnlyList<IReadOnlyList<BlowNode>> FindCycles(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges)
        => FindCycles(new BlowGraphIndex(nodes, edges));

    /// <summary>
    /// Splits the graph into the pieces it falls apart into when edge direction is ignored. Pieces come out in
    /// the order of their first node, and each lists its nodes in node order.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<BlowNode>> ConnectedComponents(BlowGraphIndex graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var component = new Dictionary<string, int>(StringComparer.Ordinal);
        var count = 0;
        foreach (var n in graph.Nodes)
        {
            if (component.ContainsKey(n.Id)) continue;
            foreach (var reached in Walk(graph, n.Id, BlowTraversalDirection.Both, null))
                component[reached.Id] = count;
            count++;
        }

        var groups = new List<BlowNode>[count];
        for (var i = 0; i < count; i++) groups[i] = [];
        foreach (var n in graph.Nodes) groups[component[n.Id]].Add(n);
        return groups;
    }

    /// <inheritdoc cref="ConnectedComponents(BlowGraphIndex)"/>
    public static IReadOnlyList<IReadOnlyList<BlowNode>> ConnectedComponents(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges)
        => ConnectedComponents(new BlowGraphIndex(nodes, edges));

    // ---- reachability ----

    /// <summary>
    /// The nodes reachable from <paramref name="startId"/>, nearest first (breadth-first). The start node itself is
    /// never included, even when a cycle leads back to it; <see cref="IsReachable(BlowGraphIndex, string, string, BlowTraversalDirection)"/>
    /// with the same id twice says whether it is on one. Unknown start ids reach nothing.
    /// </summary>
    /// <param name="graph">The graph.</param>
    /// <param name="startId">The node to start from.</param>
    /// <param name="direction">Which edges to follow.</param>
    /// <param name="maxDepth">How many edges away to look at most; null for no limit, 1 for the direct neighbours.</param>
    public static IReadOnlyList<BlowNode> Traverse(
        BlowGraphIndex graph, string startId, BlowTraversalDirection direction = BlowTraversalDirection.Outgoing, int? maxDepth = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!graph.ContainsNode(startId)) return [];

        return Walk(graph, startId, direction, maxDepth).Skip(1).ToList();
    }

    /// <summary>Everything downstream of a node: the nodes it can reach along edges, nearest first.</summary>
    public static IReadOnlyList<BlowNode> GetDescendants(BlowGraphIndex graph, string nodeId)
        => Traverse(graph, nodeId, BlowTraversalDirection.Outgoing);

    /// <inheritdoc cref="GetDescendants(BlowGraphIndex, string)"/>
    public static IReadOnlyList<BlowNode> GetDescendants(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges, string nodeId)
        => GetDescendants(new BlowGraphIndex(nodes, edges), nodeId);

    /// <summary>Everything upstream of a node: the nodes that can reach it along edges, nearest first.</summary>
    public static IReadOnlyList<BlowNode> GetAncestors(BlowGraphIndex graph, string nodeId)
        => Traverse(graph, nodeId, BlowTraversalDirection.Incoming);

    /// <inheritdoc cref="GetAncestors(BlowGraphIndex, string)"/>
    public static IReadOnlyList<BlowNode> GetAncestors(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges, string nodeId)
        => GetAncestors(new BlowGraphIndex(nodes, edges), nodeId);

    /// <summary>
    /// True when following edges from <paramref name="fromId"/> leads to <paramref name="toId"/>. A node reaches
    /// itself only through a cycle.
    /// </summary>
    public static bool IsReachable(BlowGraphIndex graph, string fromId, string toId,
        BlowTraversalDirection direction = BlowTraversalDirection.Outgoing)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!graph.ContainsNode(fromId) || !graph.ContainsNode(toId)) return false;
        if (fromId == toId) return ReachesItself(graph, fromId, direction);

        foreach (var n in Walk(graph, fromId, direction, null))
        {
            if (n.Id == toId) return true;
        }
        return false;
    }

    /// <inheritdoc cref="IsReachable(BlowGraphIndex, string, string, BlowTraversalDirection)"/>
    public static bool IsReachable(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges, string fromId, string toId)
        => IsReachable(new BlowGraphIndex(nodes, edges), fromId, toId);

    /// <summary>
    /// The shortest route from one node to another, as the nodes along it from first to last, or null when there
    /// is none. Without a <paramref name="weight"/> every edge counts as one step; with one, the route with the
    /// smallest total weight is found.
    /// </summary>
    /// <param name="graph">The graph.</param>
    /// <param name="fromId">Where the route starts.</param>
    /// <param name="toId">Where it ends.</param>
    /// <param name="weight">The cost of crossing an edge. Must be zero or more, and finite.</param>
    /// <param name="direction">Which edges the route may use.</param>
    /// <exception cref="ArgumentOutOfRangeException">An edge weighs less than zero, or is not a finite number.</exception>
    public static IReadOnlyList<BlowNode>? ShortestPath(BlowGraphIndex graph, string fromId, string toId,
        Func<BlowEdge, double>? weight = null, BlowTraversalDirection direction = BlowTraversalDirection.Outgoing)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!graph.ContainsNode(fromId) || !graph.ContainsNode(toId)) return null;
        if (fromId == toId) return [graph.GetNode(fromId)!];

        var previous = new Dictionary<string, string>(StringComparer.Ordinal);

        if (weight is null)
        {
            // Unweighted: breadth-first search finds the fewest edges.
            var queue = new Queue<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal) { fromId };
            queue.Enqueue(fromId);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                foreach (var (next, _) in Neighbours(graph, id, direction))
                {
                    if (!seen.Add(next)) continue;
                    previous[next] = id;
                    if (next == toId) return Route(graph, previous, fromId, toId);
                    queue.Enqueue(next);
                }
            }
            return null;
        }

        // Weighted: Dijkstra over a binary heap.
        var distance = new Dictionary<string, double>(StringComparer.Ordinal) { [fromId] = 0 };
        var settled = new HashSet<string>(StringComparer.Ordinal);
        var frontier = new PriorityQueue<string, double>();
        frontier.Enqueue(fromId, 0);
        while (frontier.TryDequeue(out var id, out var cost))
        {
            if (!settled.Add(id)) continue;
            if (id == toId) return Route(graph, previous, fromId, toId);

            foreach (var (next, edge) in Neighbours(graph, id, direction))
            {
                var w = weight(edge);
                if (w < 0 || !double.IsFinite(w))
                    throw new ArgumentOutOfRangeException(nameof(weight), w, $"Edge '{edge.Id}' has a weight that is negative or not finite.");

                var candidate = cost + w;
                if (distance.TryGetValue(next, out var known) && known <= candidate) continue;
                distance[next] = candidate;
                previous[next] = id;
                frontier.Enqueue(next, candidate);
            }
        }
        return null;
    }

    /// <inheritdoc cref="ShortestPath(BlowGraphIndex, string, string, Func{BlowEdge, double}?, BlowTraversalDirection)"/>
    public static IReadOnlyList<BlowNode>? ShortestPath(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges,
        string fromId, string toId, Func<BlowEdge, double>? weight = null)
        => ShortestPath(new BlowGraphIndex(nodes, edges), fromId, toId, weight);

    // ---- shape ----

    /// <summary>The nodes nothing points into: where a flow starts.</summary>
    public static IReadOnlyList<BlowNode> GetRoots(BlowGraphIndex graph)
        => graph.Nodes.Where(n => graph.Incoming(n.Id).Count == 0).ToList();

    /// <summary>The nodes that point nowhere: where a flow ends.</summary>
    public static IReadOnlyList<BlowNode> GetLeaves(BlowGraphIndex graph)
        => graph.Nodes.Where(n => graph.Outgoing(n.Id).Count == 0).ToList();

    /// <summary>The nodes with no edge at all.</summary>
    public static IReadOnlyList<BlowNode> GetIsolatedNodes(BlowGraphIndex graph)
        => graph.Nodes.Where(n => graph.Incoming(n.Id).Count == 0 && graph.Outgoing(n.Id).Count == 0).ToList();

    /// <summary>
    /// The nodes that cannot be reached from any of <paramref name="startIds"/> - in a workflow, the steps that
    /// can never run. Start ids that are not in the graph are ignored.
    /// </summary>
    public static IReadOnlyList<BlowNode> GetUnreachable(BlowGraphIndex graph, IEnumerable<string> startIds)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(startIds);

        var reached = new HashSet<string>(StringComparer.Ordinal);
        foreach (var start in startIds)
        {
            if (!graph.ContainsNode(start) || !reached.Add(start)) continue;
            foreach (var n in Walk(graph, start, BlowTraversalDirection.Outgoing, null)) reached.Add(n.Id);
        }
        return graph.Nodes.Where(n => !reached.Contains(n.Id)).ToList();
    }

    /// <summary>Counts describing the graph's shape.</summary>
    public static BlowGraphStatistics GetStatistics(BlowGraphIndex graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        int roots = 0, leaves = 0, isolated = 0, maxIn = 0, maxOut = 0;
        foreach (var n in graph.Nodes)
        {
            var inDegree = graph.Incoming(n.Id).Count;
            var outDegree = graph.Outgoing(n.Id).Count;
            if (inDegree == 0) roots++;
            if (outDegree == 0) leaves++;
            if (inDegree == 0 && outDegree == 0) isolated++;
            maxIn = Math.Max(maxIn, inDegree);
            maxOut = Math.Max(maxOut, outDegree);
        }

        var connected = graph.Edges.Count - graph.DanglingEdges.Count;
        var selfLoops = graph.Edges.Count(e => e.Source == e.Target && graph.ContainsNode(e.Source));
        return new BlowGraphStatistics(
            graph.Nodes.Count, connected, roots, leaves, isolated, ConnectedComponents(graph).Count, selfLoops,
            maxIn, maxOut, !HasCycle(graph), graph.DanglingEdges.Count);
    }

    /// <inheritdoc cref="GetStatistics(BlowGraphIndex)"/>
    public static BlowGraphStatistics GetStatistics(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges)
        => GetStatistics(new BlowGraphIndex(nodes, edges));

    // ---- helpers ----

    /// <summary>Breadth-first walk from a node, yielding the start node first and then every node reached, once each.</summary>
    private static IEnumerable<BlowNode> Walk(BlowGraphIndex graph, string startId, BlowTraversalDirection direction, int? maxDepth)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<(string Id, int Depth)>();
        queue.Enqueue((startId, 0));

        yield return graph.GetNode(startId)!;
        seen.Add(startId);

        while (queue.Count > 0)
        {
            var (id, depth) = queue.Dequeue();
            if (maxDepth is { } limit && depth >= limit) continue;

            foreach (var (next, _) in Neighbours(graph, id, direction))
            {
                if (!seen.Add(next)) continue;
                yield return graph.GetNode(next)!;
                queue.Enqueue((next, depth + 1));
            }
        }
    }

    /// <summary>
    /// True when following edges from a node leads back to it. Ignoring direction, only a self-loop does: every
    /// undirected edge could otherwise be walked there and back.
    /// </summary>
    private static bool ReachesItself(BlowGraphIndex graph, string id, BlowTraversalDirection direction)
    {
        if (direction == BlowTraversalDirection.Both)
            return graph.Outgoing(id).Any(e => e.Target == id);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(id);
        while (queue.Count > 0)
        {
            foreach (var (next, _) in Neighbours(graph, queue.Dequeue(), direction))
            {
                if (next == id) return true;
                if (seen.Add(next)) queue.Enqueue(next);
            }
        }
        return false;
    }

    private static IEnumerable<(string Id, BlowEdge Edge)> Neighbours(BlowGraphIndex graph, string id, BlowTraversalDirection direction)
    {
        if (direction != BlowTraversalDirection.Incoming)
        {
            foreach (var e in graph.Outgoing(id)) yield return (e.Target, e);
        }
        if (direction != BlowTraversalDirection.Outgoing)
        {
            foreach (var e in graph.Incoming(id)) yield return (e.Source, e);
        }
    }

    private static List<BlowNode> Route(BlowGraphIndex graph, Dictionary<string, string> previous, string fromId, string toId)
    {
        var route = new List<BlowNode>();
        for (var id = toId; ; id = previous[id])
        {
            route.Add(graph.GetNode(id)!);
            if (id == fromId) break;
        }
        route.Reverse();
        return route;
    }
}
