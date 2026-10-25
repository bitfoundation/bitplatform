namespace Bit.Blow.Tests;

[TestClass]
public class GraphUtilsTests
{
    private static BlowNode NodeAt(string id, double x, double y, double w = 100, double h = 50) =>
        new() { Id = id, Position = new(x, y), Width = w, Height = h, AbsolutePosition = new(x, y) };

    [TestMethod]
    public void AddEdge_AddsTheConnectionOnce()
    {
        var edges = new List<BlowEdge>();
        var connection = new BlowConnection("a", "b", null, null);

        BlowGraphUtils.AddEdge(connection, edges);
        BlowGraphUtils.AddEdge(connection, edges);

        Assert.ContainsSingle(edges);
        Assert.AreEqual("a", edges[0].Source);
        Assert.AreEqual("b", edges[0].Target);
    }

    [TestMethod]
    public void AddEdge_TreatsDifferentHandlesAsDifferentConnections()
    {
        var edges = new List<BlowEdge>();

        BlowGraphUtils.AddEdge(new BlowConnection("a", "b", "out1", null), edges);
        BlowGraphUtils.AddEdge(new BlowConnection("a", "b", "out2", null), edges);

        Assert.AreEqual(2, edges.Count);
    }

    [TestMethod]
    public void AddEdge_IgnoresConnectionsMissingAnEndpoint()
    {
        var edges = new List<BlowEdge>();

        BlowGraphUtils.AddEdge(new BlowConnection(null, "b", null, null), edges);
        BlowGraphUtils.AddEdge(new BlowConnection("a", null, null, null), edges);

        Assert.IsEmpty(edges);
    }

    [TestMethod]
    public void AddEdge_FallsBackToAUniqueIdWhenTheDeterministicOneIsTaken()
    {
        var connection = new BlowConnection("a", "b", null, null);
        var edges = new List<BlowEdge>
        {
            // An unrelated edge squatting on the id the connection would generate.
            new() { Id = BlowGraphUtils.BuildEdgeId(connection), Source = "x", Target = "y" },
        };

        BlowGraphUtils.AddEdge(connection, edges);

        Assert.AreEqual(2, edges.Count);
        Assert.AreEqual(2, edges.Select(e => e.Id).Distinct().Count());
    }

    [TestMethod]
    public void BuildEdgeId_IsDeterministicAndIncludesHandles()
    {
        var a = BlowGraphUtils.BuildEdgeId(new BlowConnection("1", "2", "out", "in"));
        var b = BlowGraphUtils.BuildEdgeId(new BlowConnection("1", "2", "out", "in"));
        var noHandles = BlowGraphUtils.BuildEdgeId(new BlowConnection("1", "2", null, null));

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, noHandles);
        Assert.Contains("out", a);
        Assert.Contains("in", a);
    }

    [TestMethod]
    public void ReconnectEdge_RewiresTheMatchingEdgeOnly()
    {
        var target = new BlowEdge { Id = "e1", Source = "a", Target = "b" };
        var other = new BlowEdge { Id = "e2", Source = "a", Target = "c" };
        var edges = new List<BlowEdge> { target, other };

        BlowGraphUtils.ReconnectEdge(target, new BlowConnection("a", "d", null, "in"), edges);

        Assert.AreEqual("d", target.Target);
        Assert.AreEqual("in", target.TargetHandle);
        Assert.AreEqual("c", other.Target);
    }

    [TestMethod]
    public void GetConnectedEdges_ReturnsEdgesTouchingAnyOfTheNodes()
    {
        var a = NodeAt("a", 0, 0);
        var edges = new List<BlowEdge>
        {
            new() { Id = "e1", Source = "a", Target = "b" },
            new() { Id = "e2", Source = "b", Target = "a" },
            new() { Id = "e3", Source = "b", Target = "c" },
        };

        var connected = BlowGraphUtils.GetConnectedEdges([a], edges);

        Assert.AreSequenceEqual(["e1", "e2"], connected.Select(e => e.Id));
    }

    [TestMethod]
    public void GetIncomersAndOutgoers_FollowEdgeDirection()
    {
        var a = NodeAt("a", 0, 0);
        var b = NodeAt("b", 0, 100);
        var c = NodeAt("c", 0, 200);
        var nodes = new List<BlowNode> { a, b, c };
        var edges = new List<BlowEdge>
        {
            new() { Id = "e1", Source = "a", Target = "b" },
            new() { Id = "e2", Source = "b", Target = "c" },
        };

        Assert.AreSequenceEqual(["a"], BlowGraphUtils.GetIncomers(b, nodes, edges).Select(n => n.Id));
        Assert.AreSequenceEqual(["c"], BlowGraphUtils.GetOutgoers(b, nodes, edges).Select(n => n.Id));
        Assert.IsEmpty(BlowGraphUtils.GetIncomers(a, nodes, edges));
    }

    [TestMethod]
    public void GetNodesBounds_UnionsTheNodeRectangles()
    {
        var bounds = BlowGraphUtils.GetNodesBounds([NodeAt("a", 0, 0, 100, 50), NodeAt("b", 200, 100, 50, 50)]);

        Assert.AreEqual(0, bounds.X);
        Assert.AreEqual(0, bounds.Y);
        Assert.AreEqual(250, bounds.Right);
        Assert.AreEqual(150, bounds.Bottom);
    }

    [TestMethod]
    public void GetNodesBounds_ReturnsAnEmptyRectForNoNodes()
    {
        var bounds = BlowGraphUtils.GetNodesBounds([]);

        Assert.AreEqual(new BlowRect(0, 0, 0, 0), bounds);
    }

    [TestMethod]
    public void GetViewportForBounds_CentersTheBoundsAndRespectsTheZoomRange()
    {
        var viewport = BlowGraphUtils.GetViewportForBounds(
            new BlowRect(0, 0, 100, 100), paneWidth: 200, paneHeight: 200,
            minZoom: 0.5, maxZoom: 4, padding: 0);

        Assert.AreEqual(2, viewport.Zoom, 1e-6);
        // The centre of the bounds (50,50) must land on the centre of the pane (100,100).
        var center = viewport.FlowToScreen(new BlowXYPosition(50, 50));
        Assert.AreEqual(100, center.X, 1e-6);
        Assert.AreEqual(100, center.Y, 1e-6);
    }

    [TestMethod]
    public void GetViewportForBounds_ClampsToMaxZoom()
    {
        var viewport = BlowGraphUtils.GetViewportForBounds(
            new BlowRect(0, 0, 10, 10), 400, 400, minZoom: 0.5, maxZoom: 2, padding: 0);

        Assert.AreEqual(2, viewport.Zoom, 1e-6);
    }

    [TestMethod]
    public void GetNodeConnections_FiltersByTypeAndHandle()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "e1", Source = "a", Target = "b", SourceHandle = "out" },
            new() { Id = "e2", Source = "c", Target = "a", TargetHandle = "in" },
        };

        Assert.AreEqual(2, BlowGraphUtils.GetNodeConnections("a", edges).Count);
        Assert.ContainsSingle(BlowGraphUtils.GetNodeConnections("a", edges, BlowHandleType.Source));
        Assert.ContainsSingle(BlowGraphUtils.GetNodeConnections("a", edges, BlowHandleType.Target));
        Assert.ContainsSingle(BlowGraphUtils.GetHandleConnections("a", BlowHandleType.Source, "out", edges));
        Assert.IsEmpty(BlowGraphUtils.GetHandleConnections("a", BlowHandleType.Source, "nope", edges));
    }

    [TestMethod]
    public void GetNodeIntersection_LandsOnTheBorderFacingTheOtherNode()
    {
        var left = NodeAt("l", 0, 0, 100, 100);
        var right = NodeAt("r", 300, 0, 100, 100);

        var point = BlowGraphUtils.GetNodeIntersection(left, right);

        // Same vertical centre, so the crossing point is the middle of the right border.
        Assert.AreEqual(100, point.X, 1e-6);
        Assert.AreEqual(50, point.Y, 1e-6);
        Assert.AreEqual(BlowPosition.Right, BlowGraphUtils.GetEdgePosition(left, point));
    }

    [TestMethod]
    public void GetFloatingEdgeParams_ReturnsOppositeFacingSides()
    {
        var left = NodeAt("l", 0, 0, 100, 100);
        var right = NodeAt("r", 300, 0, 100, 100);

        var (_, sourcePos, _, targetPos) = BlowGraphUtils.GetFloatingEdgeParams(left, right);

        Assert.AreEqual(BlowPosition.Right, sourcePos);
        Assert.AreEqual(BlowPosition.Left, targetPos);
    }

    [TestMethod]
    public void GetNodeIntersection_HandlesZeroSizedNodes()
    {
        var zero = new BlowNode { Id = "z", Position = new(0, 0), Width = 0, Height = 0, AbsolutePosition = new(0, 0) };
        var other = NodeAt("o", 100, 100);

        var point = BlowGraphUtils.GetNodeIntersection(zero, other);

        Assert.IsFalse(double.IsNaN(point.X));
        Assert.IsFalse(double.IsNaN(point.Y));
    }
}
