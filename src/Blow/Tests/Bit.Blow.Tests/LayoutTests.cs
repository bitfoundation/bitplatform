namespace Bit.Blow.Tests;

/// <summary>Tests for the built-in auto-layouts and the alignment-guide geometry.</summary>
[TestClass]
public class LayoutTests
{
    private static List<BlowNode> Chain(int count, double width = 100, double height = 40)
    {
        var nodes = new List<BlowNode>(count);
        for (var i = 0; i < count; i++)
            nodes.Add(new BlowNode { Id = i.ToString(), Width = width, Height = height });
        return nodes;
    }

    private static List<BlowEdge> Path(params string[] ids)
    {
        var edges = new List<BlowEdge>();
        for (var i = 0; i + 1 < ids.Length; i++)
            edges.Add(new BlowEdge { Id = $"e{i}", Source = ids[i], Target = ids[i + 1] });
        return edges;
    }

    // ---- layered ----

    [TestMethod]
    public void AChainIsLaidOutOneNodePerLayer()
    {
        var nodes = Chain(3);
        BlowLayout.Layered(nodes, Path("0", "1", "2"));

        // Top-to-bottom: each node strictly below the last, all on the same column.
        Assert.IsTrue(nodes[1].Position.Y > nodes[0].Position.Y);
        Assert.IsTrue(nodes[2].Position.Y > nodes[1].Position.Y);
        Assert.AreEqual(nodes[0].Position.X, nodes[1].Position.X, 1e-6);
    }

    [TestMethod]
    public void SiblingsShareALayerAndSitSideBySide()
    {
        var nodes = Chain(3);
        var edges = new List<BlowEdge>
        {
            new() { Id = "a", Source = "0", Target = "1" },
            new() { Id = "b", Source = "0", Target = "2" },
        };

        BlowLayout.Layered(nodes, edges);

        Assert.AreEqual(nodes[1].Position.Y, nodes[2].Position.Y, 1e-6);
        Assert.AreNotEqual(nodes[1].Position.X, nodes[2].Position.X);
    }

    [TestMethod]
    public void ANodeSitsBeyondItsFurthestPredecessor()
    {
        // 0 -> 1 -> 2 and 0 -> 2: node 2 belongs on the third layer, not the second.
        var nodes = Chain(3);
        var edges = new List<BlowEdge>
        {
            new() { Id = "a", Source = "0", Target = "1" },
            new() { Id = "b", Source = "1", Target = "2" },
            new() { Id = "c", Source = "0", Target = "2" },
        };

        BlowLayout.Layered(nodes, edges);

        Assert.IsTrue(nodes[2].Position.Y > nodes[1].Position.Y);
    }

    [TestMethod]
    public void LeftToRightFlowsAlongTheXAxis()
    {
        var nodes = Chain(3);
        BlowLayout.Layered(nodes, Path("0", "1", "2"),
            new BlowLayeredLayoutOptions { Direction = BlowLayoutDirection.LeftToRight });

        Assert.IsTrue(nodes[1].Position.X > nodes[0].Position.X);
        Assert.AreEqual(nodes[0].Position.Y, nodes[1].Position.Y, 1e-6);
    }

    [TestMethod]
    public void ReversedDirectionsMirrorTheLayout()
    {
        var down = Chain(3);
        BlowLayout.Layered(down, Path("0", "1", "2"),
            new BlowLayeredLayoutOptions { Direction = BlowLayoutDirection.TopToBottom });

        var up = Chain(3);
        BlowLayout.Layered(up, Path("0", "1", "2"),
            new BlowLayeredLayoutOptions { Direction = BlowLayoutDirection.BottomToTop });

        Assert.IsTrue(down[2].Position.Y > down[0].Position.Y);
        Assert.IsTrue(up[2].Position.Y < up[0].Position.Y);
    }

    [TestMethod]
    public void HandlesArePointedAlongTheFlow()
    {
        var nodes = Chain(2);
        BlowLayout.Layered(nodes, Path("0", "1"),
            new BlowLayeredLayoutOptions { Direction = BlowLayoutDirection.LeftToRight });

        Assert.AreEqual(BlowPosition.Right, nodes[0].SourcePosition);
        Assert.AreEqual(BlowPosition.Left, nodes[0].TargetPosition);
    }

    [TestMethod]
    public void HandleAlignmentCanBeDeclined()
    {
        var nodes = Chain(2);
        nodes[0].SourcePosition = BlowPosition.Top;

        BlowLayout.Layered(nodes, Path("0", "1"),
            new BlowLayeredLayoutOptions { Direction = BlowLayoutDirection.LeftToRight, AlignHandlesToDirection = false });

        Assert.AreEqual(BlowPosition.Top, nodes[0].SourcePosition);
    }

    [TestMethod]
    public void ACycleDoesNotHangTheLayout()
    {
        var nodes = Chain(3);
        var edges = new List<BlowEdge>
        {
            new() { Id = "a", Source = "0", Target = "1" },
            new() { Id = "b", Source = "1", Target = "2" },
            new() { Id = "c", Source = "2", Target = "0" },
        };

        BlowLayout.Layered(nodes, edges);

        // Every node got a finite position; the back edge did not push anything to infinity.
        foreach (var n in nodes) {
            Assert.IsTrue(double.IsFinite(n.Position.X));
            Assert.IsTrue(double.IsFinite(n.Position.Y));
        }
    }

    [TestMethod]
    public void AGraphThatIsAllCycleIsStillLaidOut()
    {
        var nodes = Chain(2);
        var edges = new List<BlowEdge>
        {
            new() { Id = "a", Source = "0", Target = "1" },
            new() { Id = "b", Source = "1", Target = "0" },
        };

        BlowLayout.Layered(nodes, edges);

        Assert.AreNotEqual(nodes[0].Position, nodes[1].Position);
    }

    [TestMethod]
    public void ASelfLoopDoesNotPushANodeOutOfItsLayer()
    {
        var nodes = Chain(2);
        var edges = new List<BlowEdge>
        {
            new() { Id = "loop", Source = "0", Target = "0" },
            new() { Id = "a", Source = "0", Target = "1" },
        };

        BlowLayout.Layered(nodes, edges);

        Assert.IsTrue(nodes[1].Position.Y > nodes[0].Position.Y);
    }

    [TestMethod]
    public void EdgesTouchingNodesOutsideTheSetAreIgnored()
    {
        var nodes = Chain(2);
        var edges = new List<BlowEdge>
        {
            new() { Id = "a", Source = "0", Target = "1" },
            new() { Id = "outside", Source = "1", Target = "not-here" },
        };

        BlowLayout.Layered(nodes, edges);

        Assert.IsTrue(nodes[1].Position.Y > nodes[0].Position.Y);
    }

    [TestMethod]
    public void DisconnectedNodesAllLandOnTheFirstLayer()
    {
        var nodes = Chain(3);
        BlowLayout.Layered(nodes, []);

        Assert.AreEqual(nodes[0].Position.Y, nodes[1].Position.Y, 1e-6);
        Assert.AreEqual(nodes[1].Position.Y, nodes[2].Position.Y, 1e-6);
    }

    [TestMethod]
    public void GapsAreHonoured()
    {
        var nodes = Chain(2, width: 100, height: 40);
        BlowLayout.Layered(nodes, Path("0", "1"), new BlowLayeredLayoutOptions { LayerGap = 200 });

        Assert.AreEqual(240, nodes[1].Position.Y - nodes[0].Position.Y, 1e-3);
    }

    [TestMethod]
    public void TheOriginShiftsTheWholeLayout()
    {
        var nodes = Chain(2);
        BlowLayout.Layered(nodes, Path("0", "1"),
            new BlowLayeredLayoutOptions { Origin = new BlowXYPosition(500, 300) });

        Assert.AreEqual(500, nodes[0].Position.X, 1e-3);
        Assert.AreEqual(300, nodes[0].Position.Y, 1e-3);
    }

    [TestMethod]
    public void TallAndShortLayersDoNotOverlap()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "0", Width = 100, Height = 200 },
            new() { Id = "1", Width = 100, Height = 40 },
        };

        BlowLayout.Layered(nodes, Path("0", "1"), new BlowLayeredLayoutOptions { LayerGap = 10 });

        // Layer thickness comes from the nodes in it, so the tall node's bottom clears the next.
        Assert.IsTrue(nodes[1].Position.Y >= nodes[0].Position.Y + 200);
    }

    [TestMethod]
    public void CrossingReductionOrdersSiblingsToMatchTheirParents()
    {
        // a -> c, b -> d. Listing the second layer as [d, c] crosses the edges; the sweeps
        // should swap them back.
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Width = 100, Height = 40 },
            new() { Id = "b", Width = 100, Height = 40 },
            new() { Id = "d", Width = 100, Height = 40 },
            new() { Id = "c", Width = 100, Height = 40 },
        };
        var edges = new List<BlowEdge>
        {
            new() { Id = "1", Source = "a", Target = "c" },
            new() { Id = "2", Source = "b", Target = "d" },
        };

        BlowLayout.Layered(nodes, edges);

        var a = nodes.First(n => n.Id == "a");
        var b = nodes.First(n => n.Id == "b");
        var c = nodes.First(n => n.Id == "c");
        var d = nodes.First(n => n.Id == "d");

        Assert.AreEqual(a.Position.X < b.Position.X, c.Position.X < d.Position.X);
    }

    [TestMethod]
    public void CrossingReductionCanBeSkipped()
    {
        var nodes = Chain(2);
        BlowLayout.Layered(nodes, [], new BlowLayeredLayoutOptions { CrossingReductionPasses = 0 });

        Assert.IsTrue(nodes[0].Position.X < nodes[1].Position.X);
    }

    [TestMethod]
    public void AnEmptyGraphIsANoOp()
    {
        BlowLayout.Layered([], []);
        BlowLayout.Grid([]);
        BlowLayout.Circle([]);
    }

    [TestMethod]
    public void UnmeasuredNodesFallBackToTheConfiguredSize()
    {
        var nodes = new List<BlowNode> { new() { Id = "0" }, new() { Id = "1" } };
        BlowLayout.Layered(nodes, Path("0", "1"),
            new BlowLayeredLayoutOptions { FallbackSize = new BlowDimensions(80, 300), LayerGap = 0 });

        Assert.AreEqual(300, nodes[1].Position.Y - nodes[0].Position.Y, 1e-3);
    }

    // ---- grid ----

    [TestMethod]
    public void AGridFillsRowsBeforeColumns()
    {
        var nodes = Chain(4);
        BlowLayout.Grid(nodes, new BlowGridLayoutOptions { Columns = 2 });

        Assert.AreEqual(nodes[0].Position.Y, nodes[1].Position.Y, 1e-6);
        Assert.IsTrue(nodes[2].Position.Y > nodes[0].Position.Y);
        Assert.AreEqual(nodes[0].Position.X, nodes[2].Position.X, 1e-6);
    }

    [TestMethod]
    public void AGridWithoutAColumnCountIsRoughlySquare()
    {
        var nodes = Chain(9);
        BlowLayout.Grid(nodes);

        // Nine nodes in three columns: the fourth starts a new row.
        Assert.AreEqual(nodes[0].Position.Y, nodes[2].Position.Y, 1e-6);
        Assert.IsTrue(nodes[3].Position.Y > nodes[0].Position.Y);
    }

    [TestMethod]
    public void GridGapsAreHonoured()
    {
        var nodes = Chain(2, width: 100);
        BlowLayout.Grid(nodes, new BlowGridLayoutOptions { Columns = 2, ColumnGap = 60 });

        Assert.AreEqual(160, nodes[1].Position.X - nodes[0].Position.X, 1e-3);
    }

    [TestMethod]
    public void DifferentlySizedNodesAreCentredInTheirCells()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "wide", Width = 200, Height = 40 },
            new() { Id = "narrow", Width = 100, Height = 40 },
        };

        BlowLayout.Grid(nodes, new BlowGridLayoutOptions { Columns = 2, ColumnGap = 0 });

        // The narrow node sits centred in a cell as wide as the widest node.
        Assert.AreEqual(250, nodes[1].Position.X, 1e-3);
    }

    [TestMethod]
    public void AnInvalidColumnCountFallsBackToTheSquareGrid()
    {
        var nodes = Chain(4);
        BlowLayout.Grid(nodes, new BlowGridLayoutOptions { Columns = 0 });

        Assert.AreEqual(nodes[0].Position.Y, nodes[1].Position.Y, 1e-6);
        Assert.IsTrue(nodes[2].Position.Y > nodes[0].Position.Y);
    }

    // ---- circle ----

    [TestMethod]
    public void ACircleSpreadsNodesAroundARing()
    {
        var nodes = Chain(4, width: 40, height: 40);
        BlowLayout.Circle(nodes, new BlowCircleLayoutOptions { Radius = 100 });

        var centres = nodes
            .Select(n => new BlowXYPosition(n.Position.X + 20, n.Position.Y + 20))
            .ToList();

        var cx = centres.Average(p => p.X);
        var cy = centres.Average(p => p.Y);

        foreach (var p in centres)
        {
            var r = Math.Sqrt(Math.Pow(p.X - cx, 2) + Math.Pow(p.Y - cy, 2));
            Assert.AreEqual(100, r, 1e-3);
        }
    }

    [TestMethod]
    public void ASingleNodeSitsAtTheOrigin()
    {
        var nodes = Chain(1);
        BlowLayout.Circle(nodes, new BlowCircleLayoutOptions { Origin = new BlowXYPosition(10, 20) });

        Assert.AreEqual(new BlowXYPosition(10, 20), nodes[0].Position);
    }

    [TestMethod]
    public void MoreNodesNeedABiggerDerivedRadius()
    {
        var few = Chain(3, width: 100, height: 40);
        var many = Chain(12, width: 100, height: 40);

        BlowLayout.Circle(few);
        BlowLayout.Circle(many);

        Assert.IsTrue(Spread(many) > Spread(few));

        static double Spread(List<BlowNode> nodes) =>
            nodes.Max(n => n.Position.X) - nodes.Min(n => n.Position.X);
    }

    [TestMethod]
    public void TheFirstNodeStartsAtTheTopOfTheRing()
    {
        var nodes = Chain(4, width: 40, height: 40);
        BlowLayout.Circle(nodes, new BlowCircleLayoutOptions { Radius = 100 });

        var topMost = nodes.OrderBy(n => n.Position.Y).First();
        Assert.AreEqual("0", topMost.Id);
    }

    // ---- alignment guides ----

    [TestMethod]
    public void NothingNearbyMeansNoSnap()
    {
        var result = BlowAlignment.Snap(new BlowRect(0, 0, 100, 50), [new BlowRect(500, 500, 100, 50)], 6);

        Assert.AreEqual(0, result.OffsetX);
        Assert.AreEqual(0, result.OffsetY);
        Assert.IsEmpty(result.Guides);
    }

    [TestMethod]
    public void ANearlyAlignedLeftEdgeSnapsFlush()
    {
        var result = BlowAlignment.Snap(new BlowRect(3, 200, 100, 50), [new BlowRect(0, 0, 100, 50)], 6);

        Assert.AreEqual(-3, result.OffsetX, 1e-6);
        Assert.Contains(g => g.Orientation == BlowGuideOrientation.Vertical, result.Guides);
    }

    [TestMethod]
    public void CentresAlignToo()
    {
        // Moving rect is 50 wide, other is 100 wide; their centres are 2 apart.
        var result = BlowAlignment.Snap(new BlowRect(27, 200, 50, 50), [new BlowRect(0, 0, 100, 50)], 6);

        Assert.AreEqual(-2, result.OffsetX, 1e-6);
    }

    [TestMethod]
    public void BothAxesCanSnapAtOnce()
    {
        var result = BlowAlignment.Snap(new BlowRect(2, 3, 100, 50), [new BlowRect(0, 0, 100, 50)], 6);

        Assert.AreEqual(-2, result.OffsetX, 1e-6);
        Assert.AreEqual(-3, result.OffsetY, 1e-6);
        Assert.AreEqual(2, result.Guides.Count);
    }

    [TestMethod]
    public void EachAxisCanSnapToADifferentNeighbour()
    {
        var result = BlowAlignment.Snap(
            new BlowRect(2, 303, 100, 50),
            [new BlowRect(0, 0, 100, 50), new BlowRect(400, 300, 100, 50)],
            6);

        Assert.AreEqual(-2, result.OffsetX, 1e-6);
        Assert.AreEqual(-3, result.OffsetY, 1e-6);
    }

    [TestMethod]
    public void TheNearestAlignmentWins()
    {
        var result = BlowAlignment.Snap(
            new BlowRect(5, 200, 100, 50),
            [new BlowRect(0, 0, 100, 50), new BlowRect(4, 0, 100, 50)],
            6);

        Assert.AreEqual(-1, result.OffsetX, 1e-6);
    }

    [TestMethod]
    public void AZeroToleranceDisablesSnapping()
    {
        var result = BlowAlignment.Snap(new BlowRect(0, 0, 100, 50), [new BlowRect(0, 200, 100, 50)], 0);

        Assert.IsEmpty(result.Guides);
    }

    [TestMethod]
    public void AGuideSpansBothRectangles()
    {
        var result = BlowAlignment.Snap(new BlowRect(0, 400, 100, 50), [new BlowRect(0, 0, 100, 50)], 6);

        var guide = Assert.ContainsSingle(g => g.Orientation == BlowGuideOrientation.Vertical, result.Guides);
        Assert.AreEqual(0, guide.Start, 1e-6);
        Assert.AreEqual(450, guide.End, 1e-6);
    }

    [TestMethod]
    public void AGuideSitsOnTheLineThatWasMatched()
    {
        var result = BlowAlignment.Snap(new BlowRect(3, 400, 100, 50), [new BlowRect(0, 0, 100, 50)], 6);

        var guide = Assert.ContainsSingle(g => g.Orientation == BlowGuideOrientation.Vertical, result.Guides);
        Assert.AreEqual(0, guide.Coordinate, 1e-6);
    }

    [TestMethod]
    public void RightEdgesAlignToLeftEdges()
    {
        // Moving rect's right edge (102) is 2 from the other's left edge (100).
        var result = BlowAlignment.Snap(new BlowRect(2, 200, 100, 50), [new BlowRect(100, 0, 100, 50)], 6);

        Assert.AreEqual(-2, result.OffsetX, 1e-6);
    }

    [TestMethod]
    public void NoCandidatesMeansNoSnap()
    {
        var result = BlowAlignment.Snap(new BlowRect(0, 0, 100, 50), [], 6);

        Assert.IsEmpty(result.Guides);
    }
}
