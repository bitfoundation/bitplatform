using System.Xml.Linq;
using Bunit;

namespace Bit.Blow.Tests;

/// <summary>
/// The router behind <see cref="BlowEdgeType.Routed"/> edges is the app's to replace: what it is asked, when it is
/// asked again, what a refusal falls back to, and the SVG export routing as the canvas does.
/// </summary>
[TestClass]
public class EdgeRouterTests : BunitContext
{
    public EdgeRouterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Draws a straight line, or refuses; records what it was asked.</summary>
    private sealed class RecordingRouter(Func<BlowRouteRequest, BlowPathResult?>? answer = null) : IBlowEdgeRouter
    {
        public List<BlowRouteRequest> Requests { get; } = [];

        public BlowPathResult? Route(BlowRouteRequest request)
        {
            Requests.Add(request);
            return answer is null
                ? new BlowPathResult($"M{request.Source.X} {request.Source.Y}L{request.Target.X} {request.Target.Y}Z", 0, 0)
                : answer(request);
        }
    }

    /// <summary>a at the left, b at the right, a node between them and one far away.</summary>
    private static List<BlowNode> Nodes() =>
    [
        new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40, SourcePosition = BlowPosition.Right },
        new() { Id = "wall", Position = new(250, -100), Width = 60, Height = 240 },
        new() { Id = "b", Position = new(500, 0), Width = 100, Height = 40, TargetPosition = BlowPosition.Left },
        new() { Id = "far", Position = new(5000, 5000), Width = 100, Height = 40 },
    ];

    private static string PathOf(IRenderedComponent<BlowCanvas> cut, string edgeId)
        => cut.Find($".bit-blow-edge[data-id='{edgeId}'] .bit-blow-edge-path").GetAttribute("d")!;

    [TestMethod]
    public void TheCanvasDrawsARoutedEdgeWithTheRouterItIsGiven()
    {
        var router = new RecordingRouter();
        var nodes = Nodes();
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Routed } };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, edges).Add(c => c.Routing, router));

        Assert.AreEqual("M100 20L500 20Z", PathOf(cut, "e"));

        var request = router.Requests.Last();
        Assert.AreSame(edges[0], request.Edge);
        Assert.AreEqual((new BlowXYPosition(100, 20), BlowPosition.Right, new BlowXYPosition(500, 20), BlowPosition.Left),
            (request.Source, request.SourceSide, request.Target, request.TargetSide));

        // The nodes near it, its own two included; not the one far away.
        CollectionAssert.AreEquivalent(
            new[] { nodes[0].GetRect(), nodes[1].GetRect(), nodes[2].GetRect() }, request.Obstacles.ToArray());
    }

    [TestMethod]
    public void ARouterThatFindsNoRouteGetsASmoothStep()
    {
        var nodes = Nodes();
        var edges = new List<BlowEdge>
        {
            new() { Id = "routed", Source = "a", Target = "b", Type = BlowEdgeType.Routed },
            new() { Id = "step", Source = "a", Target = "b", Type = BlowEdgeType.SmoothStep },
        };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, edges)
            .Add(c => c.Routing, new RecordingRouter(_ => null)));

        Assert.AreEqual(PathOf(cut, "step"), PathOf(cut, "routed"));
    }

    [TestMethod]
    public void TheRouterIsAskedAgainOnlyWhenSomethingNearTheEdgeMovedOrItWasReplaced()
    {
        var router = new RecordingRouter();
        var nodes = Nodes();
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Routed } };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, edges).Add(c => c.Routing, router));
        var asked = router.Requests.Count;

        // Far away: nothing to ask.
        nodes[3].Position = new(6000, 6000);
        cut.Render(p => p.Add(c => c.Nodes, nodes));
        Assert.HasCount(asked, router.Requests, "a node far from the edge moving re-routed it");

        // Near it: asked again.
        nodes[1].Position = new(250, 400);
        cut.Render(p => p.Add(c => c.Nodes, nodes));
        Assert.HasCount(asked + 1, router.Requests);

        // A new router routes the edge afresh.
        var replacement = new RecordingRouter(r => new BlowPathResult("M0 0", 0, 0));
        cut.Render(p => p.Add(c => c.Routing, replacement));
        Assert.AreEqual("M0 0", PathOf(cut, "e"));
    }

    [TestMethod]
    public void EachEdgeIsRoutedForItselfEvenBetweenTheSameTwoNodes()
    {
        // A router that separates parallel edges by their id: the canvas must not hand one edge's route to the other.
        var router = new RecordingRouter(r => new BlowPathResult($"M0 0L{(r.Edge.Id == "one" ? 1 : 2)} 0", 0, 0));
        var edges = new List<BlowEdge>
        {
            new() { Id = "one", Source = "a", Target = "b", Type = BlowEdgeType.Routed },
            new() { Id = "two", Source = "a", Target = "b", Type = BlowEdgeType.Routed },
        };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Nodes()).Add(c => c.Edges, edges).Add(c => c.Routing, router));

        Assert.AreEqual("M0 0L1 0", PathOf(cut, "one"));
        Assert.AreEqual("M0 0L2 0", PathOf(cut, "two"));
    }

    [TestMethod]
    public void TheBuiltInRouterIsTheRoutingOptions()
    {
        var nodes = Nodes();
        var options = new BlowRoutingOptions { Margin = 10, BorderRadius = 0 };
        var request = new BlowRouteRequest(new BlowEdge { Id = "e", Source = "a", Target = "b" }, new(100, 20), BlowPosition.Right,
            new(500, 20), BlowPosition.Left, nodes.Take(3).Select(n => n.GetRect()).ToList());

        var expected = BlowEdgeRouting.Route(request.Source, request.SourceSide, request.Target, request.TargetSide, request.Obstacles, options);
        Assert.IsNotNull(expected);
        Assert.AreEqual(expected, options.Route(request));

        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Routed } };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, edges).Add(c => c.Routing, options));
        Assert.AreEqual(expected.Value.Path, PathOf(cut, "e"));
    }

    [TestMethod]
    public void TheExportRoutesWithTheCanvassRouter()
    {
        var router = new RecordingRouter(_ => new BlowPathResult("M1 2L3 4", 0, 0));
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Routed } };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Nodes()).Add(c => c.Edges, edges).Add(c => c.Routing, router));

        var svg = XDocument.Parse(cut.Instance.ToSvg());
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.IsTrue(svg.Descendants(ns + "path").Any(p => (string?)p.Attribute("d") == "M1 2L3 4"), "the export did not use the router");

        // And on its own, with a router in its options.
        var alone = XDocument.Parse(BlowSvgExport.ToSvg(Nodes(), edges, new BlowSvgExportOptions { Routing = router }));
        Assert.IsTrue(alone.Descendants(ns + "path").Any(p => (string?)p.Attribute("d") == "M1 2L3 4"));
    }
}
