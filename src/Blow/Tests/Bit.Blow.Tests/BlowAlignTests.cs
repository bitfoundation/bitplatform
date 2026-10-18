namespace Bit.Blow.Tests;

/// <summary>
/// Tests for the align and distribute helpers - the "tidy this up" operations every drawing
/// tool has. They work in the nodes' own coordinate space and touch nothing but Position.
/// </summary>
[TestClass]
public class BlowAlignTests
{
    private static BlowNode At(string id, double x, double y, double w = 100, double h = 40) =>
        new() { Id = id, Position = new(x, y), Width = w, Height = h };

    // ---- align ----

    [TestMethod]
    public void AligningLeftPutsEveryNodeOnTheLeftmostEdge()
    {
        List<BlowNode> nodes = [At("a", 10, 0), At("b", 60, 50), At("c", 200, 100)];

        BlowAlign.Align(nodes, BlowAlignEdge.Left);

        foreach (var n in nodes) Assert.AreEqual(10, n.Position.X, 1e-6);
    }

    [TestMethod]
    public void AligningRightLinesUpTrailingEdgesRatherThanOrigins()
    {
        List<BlowNode> nodes = [At("wide", 0, 0, w: 200), At("narrow", 50, 60, w: 50)];

        BlowAlign.Align(nodes, BlowAlignEdge.Right);

        // The rightmost edge is at 200; the narrow node's origin has to land at 150 to reach it.
        Assert.AreEqual(0, nodes[0].Position.X, 1e-6);
        Assert.AreEqual(150, nodes[1].Position.X, 1e-6);
    }

    [TestMethod]
    public void AligningOnAnAxisLeavesTheOtherAxisAlone()
    {
        List<BlowNode> nodes = [At("a", 10, 5), At("b", 60, 90)];

        BlowAlign.Align(nodes, BlowAlignEdge.Left);

        Assert.AreEqual(5, nodes[0].Position.Y, 1e-6);
        Assert.AreEqual(90, nodes[1].Position.Y, 1e-6);
    }

    [TestMethod]
    public void CentringPutsEveryNodeOnTheGroupCentreline()
    {
        List<BlowNode> nodes = [At("a", 0, 0, w: 100), At("b", 100, 50, w: 200)];

        BlowAlign.Align(nodes, BlowAlignEdge.CenterX);

        // The group spans 0..300, so its centre is 150.
        Assert.AreEqual(150, nodes[0].Position.X + 50, 1e-6);
        Assert.AreEqual(150, nodes[1].Position.X + 100, 1e-6);
    }

    [TestMethod]
    public void AligningVerticallyUsesTopsAndBottoms()
    {
        List<BlowNode> nodes = [At("a", 0, 20, h: 40), At("b", 0, 200, h: 60)];

        BlowAlign.Align(nodes, BlowAlignEdge.Bottom);

        // The lowest edge is 260; each node's origin sits its own height above it.
        Assert.AreEqual(220, nodes[0].Position.Y, 1e-6);
        Assert.AreEqual(200, nodes[1].Position.Y, 1e-6);
    }

    [TestMethod]
    public void AligningReportsOnlyTheNodesThatActuallyMoved()
    {
        List<BlowNode> nodes = [At("a", 10, 0), At("b", 10, 50), At("c", 90, 100)];

        var moved = BlowAlign.Align(nodes, BlowAlignEdge.Left);

        Assert.ContainsSingle(moved);
        Assert.AreEqual("c", moved[0].Id);
    }

    [TestMethod]
    public void AligningNeedsTwoNodesToMeanAnything()
    {
        List<BlowNode> one = [At("a", 10, 0)];

        Assert.IsEmpty(BlowAlign.Align(one, BlowAlignEdge.Left));
        Assert.AreEqual(10, one[0].Position.X, 1e-6);
    }

    [TestMethod]
    public void HiddenAndUndraggableNodesAreLeftWhereTheyAre()
    {
        var pinned = At("pinned", 500, 0);
        pinned.Draggable = false;
        var hidden = At("hidden", 700, 0);
        hidden.Hidden = true;

        List<BlowNode> nodes = [At("a", 10, 0), At("b", 60, 50), pinned, hidden];

        BlowAlign.Align(nodes, BlowAlignEdge.Left);

        Assert.AreEqual(10, nodes[0].Position.X, 1e-6);
        Assert.AreEqual(10, nodes[1].Position.X, 1e-6);
        Assert.AreEqual(500, pinned.Position.X, 1e-6);
        Assert.AreEqual(700, hidden.Position.X, 1e-6);
    }

    // ---- distribute ----

    [TestMethod]
    public void DistributingSpacesTheGapsEvenlyAndKeepsTheEndsPut()
    {
        // 3 nodes of width 100 spanning 0..500 leave 200 of empty space, so 100 per gap.
        List<BlowNode> nodes = [At("a", 0, 0), At("b", 130, 0), At("c", 400, 0)];

        BlowAlign.DistributeHorizontally(nodes);

        Assert.AreEqual(0, nodes[0].Position.X, 1e-6);
        Assert.AreEqual(200, nodes[1].Position.X, 1e-6);
        Assert.AreEqual(400, nodes[2].Position.X, 1e-6);
    }

    [TestMethod]
    public void DistributingRespectsDifferingNodeSizes()
    {
        List<BlowNode> nodes = [At("a", 0, 0, w: 100), At("b", 150, 0, w: 200), At("c", 500, 0, w: 100)];

        BlowAlign.DistributeHorizontally(nodes);

        // Span 0..600 holds 400 of node, leaving 200 for two gaps.
        Assert.AreEqual(0, nodes[0].Position.X, 1e-6);
        Assert.AreEqual(200, nodes[1].Position.X, 1e-6);
        Assert.AreEqual(500, nodes[2].Position.X, 1e-6);
    }

    [TestMethod]
    public void AFixedGapPacksFromTheFirstNodeInstead()
    {
        List<BlowNode> nodes = [At("a", 10, 0, w: 100), At("b", 400, 0, w: 100)];

        BlowAlign.DistributeHorizontally(nodes, gap: 20);

        Assert.AreEqual(10, nodes[0].Position.X, 1e-6);
        Assert.AreEqual(130, nodes[1].Position.X, 1e-6);
    }

    [TestMethod]
    public void DistributingFollowsWhereTheNodesAreRatherThanListOrder()
    {
        // Given out of order; the one furthest left must stay furthest left.
        List<BlowNode> nodes = [At("right", 400, 0), At("left", 0, 0), At("middle", 130, 0)];

        BlowAlign.DistributeHorizontally(nodes);

        Assert.AreEqual(400, nodes[0].Position.X, 1e-6);
        Assert.AreEqual(0, nodes[1].Position.X, 1e-6);
        Assert.AreEqual(200, nodes[2].Position.X, 1e-6);
    }

    [TestMethod]
    public void DistributingVerticallyWorksTheSameWayDownTheOtherAxis()
    {
        List<BlowNode> nodes = [At("a", 0, 0, h: 40), At("b", 0, 55, h: 40), At("c", 0, 240, h: 40)];

        BlowAlign.DistributeVertically(nodes);

        Assert.AreEqual(0, nodes[0].Position.Y, 1e-6);
        Assert.AreEqual(120, nodes[1].Position.Y, 1e-6);
        Assert.AreEqual(240, nodes[2].Position.Y, 1e-6);
    }

    [TestMethod]
    public void DistributingTwoNodesWithNoGapGivenIsANoOp()
    {
        // With only the two ends there is nothing between them to redistribute.
        List<BlowNode> nodes = [At("a", 0, 0), At("b", 400, 0)];

        Assert.IsEmpty(BlowAlign.DistributeHorizontally(nodes));
        Assert.AreEqual(400, nodes[1].Position.X, 1e-6);
    }

    [TestMethod]
    public void DistributingReportsOnlyTheNodesThatActuallyMoved()
    {
        List<BlowNode> nodes = [At("a", 0, 0), At("b", 200, 0), At("c", 400, 0)];

        Assert.IsEmpty(BlowAlign.DistributeHorizontally(nodes));
    }
}
