using Bunit;

namespace Bit.Blow.Tests;

/// <summary>
/// The tree, radial and force layouts; laying out a selection from the canvas; and collapsing a node with children.
/// </summary>
[TestClass]
public class MoreLayoutTests : BunitContext
{
    public MoreLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // ---- fixtures ----

    private static BlowNode Box(string id, double width = 100, double height = 40) => new() { Id = id, Width = width, Height = height };

    private static BlowEdge Link(string source, string target) => new() { Id = $"{source}-{target}", Source = source, Target = target };

    /// <summary>A tree of the given size whose branches differ: node i hangs under node (i - 1) / 3, every fifth one wide.</summary>
    private static (List<BlowNode> Nodes, List<BlowEdge> Edges) Tree(int count)
    {
        var nodes = Enumerable.Range(0, count).Select(i => Box($"n{i}", i % 5 == 0 ? 180 : 90, i % 7 == 0 ? 70 : 40)).ToList();
        var edges = Enumerable.Range(1, count - 1).Select(i => Link($"n{(i - 1) / 3}", $"n{i}")).ToList();
        return (nodes, edges);
    }

    private static BlowRect Rect(BlowNode node) => new(node.Position.X, node.Position.Y, node.Width!.Value, node.Height!.Value);

    private static void AssertNoOverlap(IReadOnlyList<BlowNode> nodes, double gap = 0)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            for (var j = i + 1; j < nodes.Count; j++)
            {
                var a = Rect(nodes[i]);
                var b = Rect(nodes[j]);
                var apart = a.Right + gap <= b.Left + 1e-6 || b.Right + gap <= a.Left + 1e-6
                            || a.Bottom + gap <= b.Top + 1e-6 || b.Bottom + gap <= a.Top + 1e-6;
                Assert.IsTrue(apart, $"{nodes[i].Id} {a} and {nodes[j].Id} {b} overlap");
            }
        }
    }

    private static BlowXYPosition Centre(BlowNode node) => new(node.Position.X + node.Width!.Value / 2, node.Position.Y + node.Height!.Value / 2);

    // ---- tree ----

    [TestMethod]
    public void ATreeCentresEachParentOverItsChildrenOneLevelDown()
    {
        var nodes = new List<BlowNode> { Box("root"), Box("a"), Box("b"), Box("c") };
        var edges = new List<BlowEdge> { Link("root", "a"), Link("root", "b"), Link("root", "c") };

        BlowLayout.Tree(nodes, edges, new BlowTreeLayoutOptions { LevelGap = 50, NodeGap = 20 });

        Assert.AreEqual(Centre(nodes[2]).X, Centre(nodes[0]).X, 1e-6, "the root is not centred over its children");
        Assert.AreEqual(nodes[0].Position.Y + 40 + 50, nodes[1].Position.Y, 1e-6);
        Assert.AreEqual(nodes[1].Position.X + 100 + 20, nodes[2].Position.X, 1e-6);
        AssertNoOverlap(nodes);
    }

    [TestMethod]
    public void ATreeWithBranchesOfEverySizeHasNoOverlaps()
    {
        var (nodes, edges) = Tree(300);

        BlowLayout.Tree(nodes, edges);

        AssertNoOverlap(nodes);
    }

    [TestMethod]
    public void ATreeGrowsTheWayItIsAskedTo()
    {
        var (nodes, edges) = Tree(13);
        BlowLayout.Tree(nodes, edges, new BlowTreeLayoutOptions { Direction = BlowLayoutDirection.LeftToRight });
        Assert.IsTrue(nodes[0].Position.X < nodes[1].Position.X && nodes[1].Position.X < nodes[4].Position.X);
        Assert.AreEqual(BlowPosition.Right, nodes[0].SourcePosition);

        BlowLayout.Tree(nodes, edges, new BlowTreeLayoutOptions { Direction = BlowLayoutDirection.BottomToTop });
        Assert.IsGreaterThan(nodes[1].Position.Y, nodes[0].Position.Y);
        AssertNoOverlap(nodes);
    }

    [TestMethod]
    public void ACycleAndSeparateTreesAreLaidOutSideBySide()
    {
        var nodes = new List<BlowNode> { Box("a"), Box("b"), Box("c"), Box("x"), Box("y") };
        var edges = new List<BlowEdge> { Link("a", "b"), Link("b", "c"), Link("c", "a"), Link("x", "y") };

        BlowLayout.Tree(nodes, edges);

        AssertNoOverlap(nodes);
        var copy = nodes.Select(n => n.Clone()).ToList();
        BlowLayout.Tree(copy, edges);
        CollectionAssert.AreEqual(nodes.Select(n => n.Position).ToList(), copy.Select(n => n.Position).ToList(), "laying out twice differed");
    }

    [TestMethod]
    public void AVeryDeepChainIsLaidOutWithoutRecursion()
    {
        var nodes = Enumerable.Range(0, 20_000).Select(i => Box($"n{i}")).ToList();
        var edges = Enumerable.Range(1, 19_999).Select(i => Link($"n{i - 1}", $"n{i}")).ToList();

        BlowLayout.Tree(nodes, edges);

        Assert.IsGreaterThan(nodes[^2].Position.Y, nodes[^1].Position.Y);
    }

    // ---- radial ----

    [TestMethod]
    public void ARadialLayoutPutsTheRootInTheMiddleAndEachLevelFurtherOut()
    {
        var (nodes, edges) = Tree(40);

        BlowLayout.Radial(nodes, edges);

        var root = Centre(nodes[0]);
        double Distance(int i) => Math.Sqrt(Math.Pow(Centre(nodes[i]).X - root.X, 2) + Math.Pow(Centre(nodes[i]).Y - root.Y, 2));
        Assert.AreEqual(Distance(1), Distance(2), 1e-6, "the first ring is not a ring");
        Assert.AreEqual(Distance(1), Distance(3), 1e-6);
        Assert.IsGreaterThan(Distance(1), Distance(4));
        AssertNoOverlap(nodes);
    }

    [TestMethod]
    public void ARadialLayoutOfABigUnevenTreeHasNoOverlaps()
    {
        var (nodes, edges) = Tree(250);

        BlowLayout.Radial(nodes, edges, new BlowRadialLayoutOptions { NodeGap = 10, RingGap = 30 });

        AssertNoOverlap(nodes);
        Assert.IsTrue(nodes.Min(n => n.Position.X) >= -1e-6 && nodes.Min(n => n.Position.Y) >= -1e-6, "the result does not start at the origin");
    }

    // ---- force ----

    [TestMethod]
    public void AForceLayoutIsTheSameEveryTimeAndLeavesNothingOverlapping()
    {
        var (nodes, edges) = Tree(60);
        var copy = nodes.Select(n => n.Clone()).ToList();

        BlowLayout.Force(nodes, edges, new BlowForceLayoutOptions { Iterations = 150, NodeGap = 10 });
        BlowLayout.Force(copy, edges, new BlowForceLayoutOptions { Iterations = 150, NodeGap = 10 });

        CollectionAssert.AreEqual(nodes.Select(n => n.Position).ToList(), copy.Select(n => n.Position).ToList());
        AssertNoOverlap(nodes, gap: 10 - 1e-3);
    }

    [TestMethod]
    public void AForceLayoutKeepsConnectedNodesCloserThanUnconnectedOnes()
    {
        // Two clusters of five, each fully connected, and nothing between them.
        var nodes = Enumerable.Range(0, 10).Select(i => Box($"n{i}")).ToList();
        var edges = new List<BlowEdge>();
        for (var c = 0; c < 2; c++)
            for (var i = 0; i < 5; i++)
                for (var j = i + 1; j < 5; j++)
                    edges.Add(Link($"n{c * 5 + i}", $"n{c * 5 + j}"));

        BlowLayout.Force(nodes, edges);

        double D(int a, int b) => Math.Sqrt(Math.Pow(Centre(nodes[a]).X - Centre(nodes[b]).X, 2) + Math.Pow(Centre(nodes[a]).Y - Centre(nodes[b]).Y, 2));
        var within = Enumerable.Range(0, 5).SelectMany(i => Enumerable.Range(i + 1, 4 - i).Select(j => D(i, j))).Average();
        var across = Enumerable.Range(0, 5).SelectMany(i => Enumerable.Range(5, 5).Select(j => D(i, j))).Average();
        Assert.IsLessThan(across, within);
    }

    [TestMethod]
    public void APinnedNodeStaysWhereItIs()
    {
        var (nodes, edges) = Tree(20);
        nodes[0].Position = new(1000, 1000);

        BlowLayout.Force(nodes, edges, new BlowForceLayoutOptions { IsPinned = n => n.Id == "n0" });

        Assert.AreEqual(new BlowXYPosition(1000, 1000), nodes[0].Position);
        AssertNoOverlap(nodes);
    }

    [TestMethod]
    public void EveryBuiltInLayoutIsAnIBlowLayout()
    {
        var (nodes, edges) = Tree(10);
        IBlowLayout[] layouts =
        [
            new BlowLayeredLayoutOptions(), new BlowTreeLayoutOptions(), new BlowRadialLayoutOptions(),
            new BlowForceLayoutOptions(), new BlowGridLayoutOptions(), new BlowCircleLayoutOptions(),
        ];

        foreach (var layout in layouts)
        {
            layout.Arrange(nodes, edges);
            AssertNoOverlap(nodes);
        }
    }

    // ---- laying out from the canvas ----

    private readonly List<BlowGraphChange> _changes = [];

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge> edges)
        => Render<BlowCanvas>(p => p
            .Add(c => c.Nodes, nodes)
            .Add(c => c.Edges, edges)
            .Add(c => c.HistoryEnabled, true)
            .Add(c => c.OnGraphChanged, change => _changes.Add(change)));

    [TestMethod]
    public async Task LayingOutTheSelectionTidiesItWhereItIsAndLeavesTheRestAlone()
    {
        var (nodes, edges) = Tree(7);
        for (var i = 0; i < nodes.Count; i++) nodes[i].Position = new(500 + i * 13, 300 + i * 7);
        var outsider = Box("outsider");
        outsider.Position = new(-400, -400);
        nodes.Add(outsider);
        foreach (var n in nodes.Take(7)) n.Selected = true;
        var cut = Render(nodes, edges);
        var topLeft = new BlowXYPosition(nodes.Take(7).Min(n => n.Position.X), nodes.Take(7).Min(n => n.Position.Y));

        var arranged = await cut.InvokeAsync(() => cut.Instance.LayoutAsync(new BlowTreeLayoutOptions()));

        Assert.HasCount(7, arranged);
        Assert.AreEqual(new BlowXYPosition(-400, -400), outsider.Position);
        Assert.AreEqual(topLeft, new BlowXYPosition(arranged.Min(n => n.Position.X), arranged.Min(n => n.Position.Y)));
        AssertNoOverlap(arranged);

        Assert.AreEqual(BlowChangeKind.Moved, _changes[^1].Kind);
        Assert.AreEqual("layout", cut.Instance.History!.UndoLabel);
        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.AreEqual(new BlowXYPosition(500, 300), nodes[0].Position);
    }

    [TestMethod]
    public async Task OnlyNodesSharingAParentAreLaidOutTogether()
    {
        var nodes = new List<BlowNode>
        {
            Box("a"), Box("b"),
            new() { Id = "g", Type = "group", Position = new(0, 400), Width = 400, Height = 300 },
            new() { Id = "child", ParentId = "g", Position = new(10, 10), Width = 50, Height = 30 },
        };
        var cut = Render(nodes, []);

        var arranged = await cut.InvokeAsync(() => cut.Instance.LayoutAsync(new BlowGridLayoutOptions(), [nodes[0], nodes[1], nodes[3]]));

        CollectionAssert.AreEqual(new[] { "a", "b" }, arranged.Select(n => n.Id).ToArray());
        Assert.AreEqual(new BlowXYPosition(10, 10), nodes[3].Position);
    }

    [TestMethod]
    public async Task AGroupIsPlacedByWhatItsChildrenConnectTo()
    {
        var nodes = new List<BlowNode>
        {
            Box("source"),
            new() { Id = "g", Type = "group", Width = 300, Height = 200 },
            new() { Id = "inside", ParentId = "g", Position = new(20, 20), Width = 100, Height = 40 },
            Box("sink"),
        };
        var edges = new List<BlowEdge> { Link("source", "inside"), Link("inside", "sink") };
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.LayoutAsync(new BlowLayeredLayoutOptions { Direction = BlowLayoutDirection.LeftToRight }));

        // source -> g -> sink, by way of the edges into and out of the group's child.
        Assert.IsTrue(nodes[0].Position.X < nodes[1].Position.X && nodes[1].Position.X < nodes[3].Position.X,
            $"source {nodes[0].Position}, group {nodes[1].Position}, sink {nodes[3].Position}");
        Assert.AreEqual(new BlowXYPosition(20, 20), nodes[2].Position, "a child was moved within its group");
    }

    // ---- collapsing ----

    private static (List<BlowNode> Nodes, List<BlowEdge> Edges) Grouped() => (
    [
        new() { Id = "outside", Position = new(-300, 0), Width = 100, Height = 40 },
        new() { Id = "g", Type = "group", Position = new(0, 0), Width = 400, Height = 300 },
        new() { Id = "a", ParentId = "g", Position = new(20, 40), Width = 100, Height = 40, Selected = true },
        new() { Id = "inner", Type = "group", ParentId = "g", Position = new(150, 100), Width = 200, Height = 150 },
        new() { Id = "deep", ParentId = "inner", Position = new(20, 40), Width = 100, Height = 40 },
    ],
    [
        Link("outside", "a"),
        Link("a", "deep"),
    ]);

    [TestMethod]
    public async Task CollapsingHidesTheChildrenAndDrawsEdgesFromOutsideToTheNodeItself()
    {
        var (nodes, edges) = Grouped();
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[1]));

        Assert.IsTrue(nodes[1].Collapsed);
        CollectionAssert.AreEqual(new[] { "outside", "g" }, cut.FindAll(".bit-blow-node").Select(e => e.GetAttribute("data-id")).ToArray());
        Assert.IsTrue(cut.Find(".bit-blow-node[data-id='g']").ClassList.Contains("collapsed"));
        Assert.IsFalse(nodes[2].Selected, "a hidden node stayed selected, for the delete key to remove unseen");

        // The edge into the group is drawn, ending at the group; the one inside it is not drawn at all.
        Assert.AreEqual("outside-a", cut.Find(".bit-blow-edge").GetAttribute("data-id"));
        var end = cut.Instance.GetEdgeContext(edges[0]).Target;
        Assert.AreEqual(new BlowXYPosition(200, 0), end, "the edge does not end at the collapsed group");
        StringAssert.Contains(cut.Find(".bit-blow-a11y-live").TextContent, "collapsed, 3 nodes hidden");
        Assert.AreEqual("collapse", cut.Instance.History!.UndoLabel);
    }

    [TestMethod]
    public async Task ExpandingShowsTheChildrenButNotThoseOfANestedCollapsedNode()
    {
        var (nodes, edges) = Grouped();
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[3], true));   // inner
        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[1], true));   // g
        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[1], false));

        Assert.IsFalse(nodes[2].Hidden);
        Assert.IsFalse(nodes[3].Hidden);
        Assert.IsTrue(nodes[4].Hidden, "a node inside a still-collapsed group came back");

        // a -> deep now ends at the collapsed inner group.
        Assert.HasCount(2, cut.FindAll(".bit-blow-edge"));

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.IsTrue(nodes.Single(n => n.Id == "g").Collapsed);
        Assert.IsTrue(nodes.Single(n => n.Id == "a").Hidden);
    }

    [TestMethod]
    public void ACollapsedNodeIsSavedAndReadBack()
    {
        var (nodes, edges) = Grouped();
        nodes[1].Collapsed = true;

        var json = BlowSerializer.Serialize(new BlowDocument { Nodes = nodes, Edges = edges });
        var read = BlowSerializer.Deserialize(json).Document!;

        Assert.IsTrue(read.Nodes.Single(n => n.Id == "g").Collapsed);
        StringAssert.Contains(json, "\"collapsed\":true");
    }
}
