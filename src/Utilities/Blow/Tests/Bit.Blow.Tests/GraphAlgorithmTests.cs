namespace Bit.Blow.Tests;

/// <summary>
/// The graph algorithms: ordering, cycles, components, reachability, shortest paths and statistics - run with no
/// UI, and with graphs deep enough that a recursive implementation would overflow the stack.
/// </summary>
[TestClass]
public class GraphAlgorithmTests
{
    private static List<BlowNode> Nodes(params string[] ids) => ids.Select(id => new BlowNode { Id = id }).ToList();

    private static List<BlowEdge> Edges(params string[] pairs) => pairs
        .Select(p => p.Split('>'))
        .Select(p => new BlowEdge { Id = $"{p[0]}>{p[1]}", Source = p[0], Target = p[1] })
        .ToList();

    private static string[] Ids(IEnumerable<BlowNode> nodes) => nodes.Select(n => n.Id).ToArray();

    // ---- topological order and cycles ----

    [TestMethod]
    public void ATopologicalOrderPutsEveryNodeAfterItsPredecessors()
    {
        var nodes = Nodes("d", "c", "b", "a");
        var edges = Edges("a>b", "b>c", "a>c", "c>d");

        var result = BlowGraphAlgorithms.TopologicalSort(nodes, edges);

        Assert.IsTrue(result.IsAcyclic);
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, Ids(result.Order));
    }

    [TestMethod]
    public void ATopologicalOrderKeepsTheGivenOrderBetweenIndependentNodes()
    {
        var result = BlowGraphAlgorithms.TopologicalSort(Nodes("x", "y", "z"), []);

        CollectionAssert.AreEqual(new[] { "x", "y", "z" }, Ids(result.Order));
    }

    [TestMethod]
    public void NodesBlockedByACycleAreReportedRatherThanOrdered()
    {
        var nodes = Nodes("start", "a", "b", "after");
        var edges = Edges("start>a", "a>b", "b>a", "b>after");

        var result = BlowGraphAlgorithms.TopologicalSort(nodes, edges);

        Assert.IsFalse(result.IsAcyclic);
        CollectionAssert.AreEqual(new[] { "start" }, Ids(result.Order));
        CollectionAssert.AreEqual(new[] { "a", "b", "after" }, Ids(result.Unordered));
    }

    [TestMethod]
    public void ASelfLoopIsACycle()
    {
        Assert.IsTrue(BlowGraphAlgorithms.HasCycle(Nodes("a"), Edges("a>a")));
        Assert.IsFalse(BlowGraphAlgorithms.HasCycle(Nodes("a", "b"), Edges("a>b")));
    }

    [TestMethod]
    public void FindCyclesReturnsEachCycleAsAGroupAndIgnoresAcyclicNodes()
    {
        var nodes = Nodes("a", "b", "c", "d", "e", "f");
        var edges = Edges("a>b", "b>c", "c>a", "c>d", "e>e", "d>f");

        var cycles = BlowGraphAlgorithms.FindCycles(nodes, edges);

        Assert.HasCount(2, cycles);
        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, Ids(cycles.Single(c => c.Count == 3)));
        CollectionAssert.AreEqual(new[] { "e" }, Ids(cycles.Single(c => c.Count == 1)));
    }

    [TestMethod]
    public void StronglyConnectedComponentsCoverEveryNodeOnce()
    {
        var nodes = Nodes("a", "b", "c", "d");
        var edges = Edges("a>b", "b>a", "b>c", "c>d", "d>c");

        var components = BlowGraphAlgorithms.StronglyConnectedComponents(nodes, edges);

        Assert.HasCount(2, components);
        CollectionAssert.AreEquivalent(new[] { "a", "b", "c", "d" }, components.SelectMany(Ids).ToArray());
        // Reverse topological order: the downstream component first.
        CollectionAssert.AreEquivalent(new[] { "c", "d" }, Ids(components[0]));
    }

    [TestMethod]
    public void AVeryDeepGraphDoesNotOverflowTheStack()
    {
        const int depth = 100_000;
        var nodes = Enumerable.Range(0, depth).Select(i => new BlowNode { Id = $"n{i}" }).ToList();
        var edges = Enumerable.Range(0, depth - 1)
            .Select(i => new BlowEdge { Id = $"e{i}", Source = $"n{i}", Target = $"n{i + 1}" })
            .ToList();
        edges.Add(new BlowEdge { Id = "back", Source = $"n{depth - 1}", Target = "n0" });
        var graph = new BlowGraphIndex(nodes, edges);

        Assert.HasCount(1, BlowGraphAlgorithms.StronglyConnectedComponents(graph));
        Assert.IsTrue(BlowGraphAlgorithms.HasCycle(graph));
        Assert.HasCount(depth - 1, BlowGraphAlgorithms.GetDescendants(graph, "n0"));
        Assert.HasCount(depth, BlowGraphAlgorithms.ShortestPath(graph, "n0", $"n{depth - 1}")!);
    }

    // ---- components and reachability ----

    [TestMethod]
    public void ConnectedComponentsIgnoreEdgeDirection()
    {
        var nodes = Nodes("a", "b", "c", "x", "y", "lonely");
        var edges = Edges("a>b", "c>b", "y>x");

        var components = BlowGraphAlgorithms.ConnectedComponents(nodes, edges);

        Assert.HasCount(3, components);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Ids(components[0]));
        CollectionAssert.AreEqual(new[] { "x", "y" }, Ids(components[1]));
        CollectionAssert.AreEqual(new[] { "lonely" }, Ids(components[2]));
    }

    [TestMethod]
    public void DescendantsAndAncestorsAreNearestFirstAndExcludeTheStart()
    {
        var nodes = Nodes("a", "b", "c", "d");
        var edges = Edges("a>b", "b>c", "c>a", "a>d");
        var graph = new BlowGraphIndex(nodes, edges);

        CollectionAssert.AreEqual(new[] { "b", "d", "c" }, Ids(BlowGraphAlgorithms.GetDescendants(graph, "a")));
        CollectionAssert.AreEqual(new[] { "c", "b" }, Ids(BlowGraphAlgorithms.GetAncestors(graph, "a")));
    }

    [TestMethod]
    public void TraversalCanBeLimitedToADepth()
    {
        var graph = new BlowGraphIndex(Nodes("a", "b", "c"), Edges("a>b", "b>c"));

        CollectionAssert.AreEqual(new[] { "b" },
            Ids(BlowGraphAlgorithms.Traverse(graph, "a", BlowTraversalDirection.Outgoing, maxDepth: 1)));
        // Ignoring direction, outgoing edges are followed before incoming ones.
        CollectionAssert.AreEqual(new[] { "c", "a" },
            Ids(BlowGraphAlgorithms.Traverse(graph, "b", BlowTraversalDirection.Both)));
    }

    [TestMethod]
    public void ANodeReachesItselfOnlyThroughACycle()
    {
        Assert.IsFalse(BlowGraphAlgorithms.IsReachable(Nodes("a", "b"), Edges("a>b"), "a", "a"));
        Assert.IsTrue(BlowGraphAlgorithms.IsReachable(Nodes("a", "b"), Edges("a>b", "b>a"), "a", "a"));
        Assert.IsTrue(BlowGraphAlgorithms.IsReachable(Nodes("a", "b"), Edges("a>b"), "a", "b"));
        Assert.IsFalse(BlowGraphAlgorithms.IsReachable(Nodes("a", "b"), Edges("a>b"), "b", "a"));
    }

    [TestMethod]
    public void UnknownIdsReachNothingAndHaveNoPath()
    {
        var graph = new BlowGraphIndex(Nodes("a"), []);

        Assert.IsEmpty(BlowGraphAlgorithms.GetDescendants(graph, "ghost"));
        Assert.IsFalse(BlowGraphAlgorithms.IsReachable(graph, "a", "ghost"));
        Assert.IsNull(BlowGraphAlgorithms.ShortestPath(graph, "a", "ghost"));
    }

    // ---- shortest paths ----

    [TestMethod]
    public void TheShortestPathTakesTheFewestEdges()
    {
        var nodes = Nodes("a", "b", "c", "d");
        var edges = Edges("a>b", "b>c", "c>d", "a>d");

        CollectionAssert.AreEqual(new[] { "a", "d" }, Ids(BlowGraphAlgorithms.ShortestPath(nodes, edges, "a", "d")!));
    }

    [TestMethod]
    public void AWeightedShortestPathMinimisesTheTotalWeight()
    {
        var nodes = Nodes("a", "b", "c", "d");
        var edges = Edges("a>b", "b>c", "c>d", "a>d");
        double Weight(BlowEdge e) => e.Id == "a>d" ? 10 : 1;

        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" },
            Ids(BlowGraphAlgorithms.ShortestPath(nodes, edges, "a", "d", Weight)!));
    }

    [TestMethod]
    public void ANegativeWeightIsRefused()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            BlowGraphAlgorithms.ShortestPath(Nodes("a", "b"), Edges("a>b"), "a", "b", _ => -1));
    }

    [TestMethod]
    public void NoPathIsNull()
    {
        Assert.IsNull(BlowGraphAlgorithms.ShortestPath(Nodes("a", "b"), Edges("b>a"), "a", "b"));
        CollectionAssert.AreEqual(new[] { "a" }, Ids(BlowGraphAlgorithms.ShortestPath(Nodes("a"), [], "a", "a")!));
    }

    // ---- shape ----

    [TestMethod]
    public void RootsLeavesIsolatedAndUnreachableNodes()
    {
        var graph = new BlowGraphIndex(Nodes("in", "mid", "out", "alone", "orphan"), Edges("in>mid", "mid>out", "orphan>out"));

        CollectionAssert.AreEqual(new[] { "in", "alone", "orphan" }, Ids(BlowGraphAlgorithms.GetRoots(graph)));
        CollectionAssert.AreEqual(new[] { "out", "alone" }, Ids(BlowGraphAlgorithms.GetLeaves(graph)));
        CollectionAssert.AreEqual(new[] { "alone" }, Ids(BlowGraphAlgorithms.GetIsolatedNodes(graph)));
        CollectionAssert.AreEqual(new[] { "alone", "orphan" }, Ids(BlowGraphAlgorithms.GetUnreachable(graph, ["in"])));
    }

    [TestMethod]
    public void StatisticsDescribeTheGraph()
    {
        var nodes = Nodes("a", "b", "c", "d");
        var edges = Edges("a>b", "a>c", "c>c");
        edges.Add(new BlowEdge { Id = "dangling", Source = "a", Target = "ghost" });

        var stats = BlowGraphAlgorithms.GetStatistics(nodes, edges);

        Assert.AreEqual(4, stats.NodeCount);
        Assert.AreEqual(3, stats.EdgeCount);
        Assert.AreEqual(1, stats.DanglingEdgeCount);
        Assert.AreEqual(2, stats.RootCount);       // a, d
        Assert.AreEqual(2, stats.LeafCount);       // b, d
        Assert.AreEqual(1, stats.IsolatedCount);   // d
        Assert.AreEqual(2, stats.ComponentCount);
        Assert.AreEqual(1, stats.SelfLoopCount);
        Assert.AreEqual(2, stats.MaxOutDegree);    // a
        Assert.AreEqual(2, stats.MaxInDegree);     // c: from a and from itself
        Assert.IsFalse(stats.IsAcyclic);
    }

    // ---- the index ----

    [TestMethod]
    public void TheIndexKeepsTheFirstOfDuplicateIdsAndSetsDanglingEdgesAside()
    {
        var first = new BlowNode { Id = "a" };
        var nodes = new List<BlowNode> { first, new() { Id = "a" }, new() { Id = "b" } };
        var edges = new List<BlowEdge>
        {
            new() { Id = "e", Source = "a", Target = "b" },
            new() { Id = "e", Source = "b", Target = "a" },
            new() { Id = "x", Source = "a", Target = "missing" },
        };

        var graph = new BlowGraphIndex(nodes, edges);

        Assert.AreSame(first, graph.GetNode("a"));
        Assert.HasCount(2, graph.Nodes);
        Assert.HasCount(2, graph.Edges);
        CollectionAssert.AreEqual(new[] { "x" }, graph.DanglingEdges.Select(e => e.Id).ToArray());
        Assert.HasCount(1, graph.Outgoing("a"));
    }

    [TestMethod]
    public void HandleCountsTreatAMissingAndAnEmptyHandleIdAlike()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "1", Source = "a", Target = "b", SourceHandle = null },
            new() { Id = "2", Source = "a", Target = "b", SourceHandle = "" },
            new() { Id = "3", Source = "a", Target = "b", SourceHandle = "x" },
        };
        var graph = new BlowGraphIndex(Nodes("a", "b"), edges);

        Assert.AreEqual(2, graph.CountAt("a", BlowHandleType.Source, null));
        Assert.AreEqual(1, graph.CountAt("a", BlowHandleType.Source, "x"));
        Assert.AreEqual(3, graph.CountAt("b", BlowHandleType.Target, null));
    }
}
