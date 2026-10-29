using System.Xml.Linq;
using Bunit;

namespace Bit.Blow.Tests;

/// <summary>
/// Edges routed round the nodes in their way, the graph exported as SVG, and run states shown on nodes.
/// </summary>
[TestClass]
public class RoutingExportAndRunTests : BunitContext
{
    public RoutingExportAndRunTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // ---- routing ----

    /// <summary>True when an axis-aligned segment passes through the inside of a rectangle.</summary>
    private static bool Crosses(BlowXYPosition a, BlowXYPosition b, BlowRect r)
    {
        if (Math.Abs(a.Y - b.Y) < 1e-9)
            return a.Y > r.Top && a.Y < r.Bottom && Math.Max(Math.Min(a.X, b.X), r.Left) < Math.Min(Math.Max(a.X, b.X), r.Right);
        return a.X > r.Left && a.X < r.Right && Math.Max(Math.Min(a.Y, b.Y), r.Top) < Math.Min(Math.Max(a.Y, b.Y), r.Bottom);
    }

    private static void AssertClear(IReadOnlyList<BlowXYPosition> route, IEnumerable<BlowRect> obstacles)
    {
        for (var i = 1; i < route.Count; i++)
        {
            Assert.IsTrue(Math.Abs(route[i].X - route[i - 1].X) < 1e-9 || Math.Abs(route[i].Y - route[i - 1].Y) < 1e-9,
                $"segment {i} is not horizontal or vertical");
            foreach (var r in obstacles)
                Assert.IsFalse(Crosses(route[i - 1], route[i], r), $"segment {route[i - 1]} -> {route[i]} crosses {r}");
        }
    }

    [TestMethod]
    public void ARouteGoesRoundTheNodeInTheWay()
    {
        var source = new BlowRect(0, 0, 100, 40);
        var target = new BlowRect(500, 0, 100, 40);
        var wall = new BlowRect(250, -100, 60, 240);

        var route = BlowEdgeRouting.Orthogonal(new(100, 20), BlowPosition.Right, new(500, 20), BlowPosition.Left,
            [source, target, wall], new BlowRoutingOptions { Margin = 10 });

        Assert.IsNotNull(route);
        Assert.AreEqual(new BlowXYPosition(100, 20), route[0]);
        Assert.AreEqual(new BlowXYPosition(500, 20), route[^1]);
        // Clear of the wall by the margin, and of its own two nodes too.
        AssertClear(route, [new BlowRect(240, -110, 80, 260), source, target]);
        Assert.IsTrue(route[1].X > 100 && Math.Abs(route[1].Y - 20) < 1e-9, "the route does not leave its handle straight");
    }

    [TestMethod]
    public void WithNothingInTheWayARouteHasNoNeedlessBends()
    {
        var route = BlowEdgeRouting.Orthogonal(new(100, 20), BlowPosition.Right, new(500, 20), BlowPosition.Left, []);

        Assert.IsNotNull(route);
        Assert.HasCount(2, route, string.Join(" ", route));
    }

    [TestMethod]
    public void ATargetWalledInHasNoRoute()
    {
        var walls = new List<BlowRect>
        {
            new(380, -200, 20, 440), new(620, -200, 20, 440), new(380, -200, 260, 20), new(380, 220, 260, 20),
        };

        Assert.IsNull(BlowEdgeRouting.Orthogonal(new(100, 20), BlowPosition.Right, new(500, 20), BlowPosition.Left, walls,
            new BlowRoutingOptions { Margin = 5 }));
    }

    [TestMethod]
    public void ARouteThroughAFieldOfNodesStaysClearOfAllOfThem()
    {
        var obstacles = new List<BlowRect>();
        for (var i = 0; i < 6; i++)
            for (var j = 0; j < 4; j++)
                obstacles.Add(new BlowRect(150 + i * 120, -150 + j * 110, 60, 50));

        var route = BlowEdgeRouting.Orthogonal(new(0, 20), BlowPosition.Right, new(1000, 200), BlowPosition.Left, obstacles,
            new BlowRoutingOptions { Margin = 8 });

        Assert.IsNotNull(route);
        AssertClear(route, obstacles.Select(r => new BlowRect(r.X - 8, r.Y - 8, r.Width + 16, r.Height + 16)));
    }

    [TestMethod]
    public void TheCanvasRoutesARoutedEdgeAroundTheNodeBetweenItsEnds()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40, SourcePosition = BlowPosition.Right },
            new() { Id = "wall", Position = new(250, -100), Width = 60, Height = 240 },
            new() { Id = "b", Position = new(500, 0), Width = 100, Height = 40, TargetPosition = BlowPosition.Left },
        };
        var edges = new List<BlowEdge>
        {
            new() { Id = "routed", Source = "a", Target = "b", Type = BlowEdgeType.Routed },
            new() { Id = "plain", Source = "a", Target = "b", Type = BlowEdgeType.SmoothStep },
        };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, edges));

        var routed = cut.Find(".bit-blow-edge[data-id='routed'] .bit-blow-edge-path").GetAttribute("d")!;
        var plain = cut.Find(".bit-blow-edge[data-id='plain'] .bit-blow-edge-path").GetAttribute("d")!;
        Assert.AreNotEqual(plain, routed);

        // Moving the wall away straightens the route.
        nodes[1].Position = new(250, 400);
        cut.Render();
        var straight = cut.Find(".bit-blow-edge[data-id='routed'] .bit-blow-edge-path").GetAttribute("d")!;
        Assert.AreEqual("M100 20L500 20", straight, "with nothing in the way the route is a straight line");
    }

    // ---- SVG export ----

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [TestMethod]
    public void TheExportIsWellFormedSvgWithEveryShownNodeAndEdge()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Label = "Start", Position = new(0, 0), Width = 100, Height = 40 },
            new() { Id = "g", Type = "group", Label = "Group", Position = new(200, 0), Width = 300, Height = 200 },
            new() { Id = "b", Label = "Inside", ParentId = "g", Position = new(50, 50), Width = 100, Height = 40 },
            new() { Id = "hidden", Label = "Hidden", Hidden = true },
        };
        var edges = new List<BlowEdge>
        {
            new() { Id = "a-b", Source = "a", Target = "b", Label = "go", MarkerEnd = true },
            new() { Id = "a-hidden", Source = "a", Target = "hidden" },
        };

        var doc = XDocument.Parse(BlowSvgExport.ToSvg(nodes, edges));

        var texts = doc.Descendants(Svg + "text").Select(t => t.Value).ToList();
        CollectionAssert.IsSubsetOf(new[] { "Start", "Group", "Inside", "go" }, texts);
        Assert.DoesNotContain("Hidden", texts);
        Assert.HasCount(1, doc.Descendants(Svg + "marker"));
        Assert.AreEqual("Flow diagram", doc.Root!.Element(Svg + "title")!.Value);

        // A child is placed inside its group: 200 + 50.
        var inside = doc.Descendants(Svg + "text").Single(t => t.Value == "Inside").Parent!.Element(Svg + "rect")!;
        Assert.AreEqual("250", inside.Attribute("x")!.Value);
    }

    [TestMethod]
    public void EdgesAskingForTheSameArrowheadShareOneMarkerNumberedInFirstUseOrder()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Width = 100, Height = 40 },
            new() { Id = "b", Position = new(300, 0), Width = 100, Height = 40 },
            new() { Id = "c", Position = new(300, 200), Width = 100, Height = 40 },
        };
        var edges = new List<BlowEdge>
        {
            new() { Id = "a-b", Source = "a", Target = "b" },
            new() { Id = "a-c", Source = "a", Target = "c", MarkerStart = true, MarkerColor = "red" },
            new() { Id = "b-c", Source = "b", Target = "c" },
        };

        var doc = XDocument.Parse(BlowSvgExport.ToSvg(nodes, edges));
        var paths = doc.Descendants(Svg + "path").Where(p => p.Parent!.Name != Svg + "marker").ToList();

        CollectionAssert.AreEqual(new[] { "m0", "m1" }, doc.Descendants(Svg + "marker").Select(m => m.Attribute("id")!.Value).ToArray());
        CollectionAssert.AreEqual(new[] { "url(#m0)", "url(#m1)", "url(#m0)" }, paths.Select(p => p.Attribute("marker-end")?.Value).ToArray());
        CollectionAssert.AreEqual(new[] { null, "url(#m1)", null }, paths.Select(p => p.Attribute("marker-start")?.Value).ToArray());
    }

    [TestMethod]
    public void NothingInAGraphBecomesMarkupOrAnUnsafeValueInTheExport()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Label = "<script>alert(1)</script> & \"quotes\"", Width = 100, Height = 40 },
            new() { Id = "b", Label = "b", Position = new(300, 0), Width = 100, Height = 40 },
        };
        var edges = new List<BlowEdge>
        {
            new() { Id = "e", Source = "a", Target = "b", Stroke = "url(javascript:alert(1))", MarkerColor = "red\" onload=\"x" },
        };

        var svg = BlowSvgExport.ToSvg(nodes, edges, new BlowSvgExportOptions { Title = "</title><script/>" });
        var doc = XDocument.Parse(svg);

        Assert.IsEmpty(doc.Descendants(Svg + "script"));
        Assert.DoesNotContain("javascript", svg);
        Assert.DoesNotContain("onload", svg);
        Assert.IsTrue(doc.Descendants(Svg + "text").Any(t => t.Value == "<script>alert(1)</script> & \"quotes\""));
    }

    [TestMethod]
    public void TheCanvasExportsWhatItShows()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Label = "Only", Width = 100, Height = 40 } };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.AriaLabel, "My flow"));

        var doc = XDocument.Parse(cut.Instance.ToSvg());

        Assert.AreEqual("My flow", doc.Root!.Element(Svg + "title")!.Value);
        Assert.AreEqual("Only", doc.Descendants(Svg + "text").Single().Value);
    }

    // ---- run states ----

    [TestMethod]
    public void RunStatesMarkNodesAndAreReadOut()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Label = "Fetch" }, new() { Id = "b", Label = "Parse" }, new() { Id = "c", Label = "Store" } };
        var states = new Dictionary<string, BlowRunState>
        {
            ["a"] = new(BlowRunStatus.Succeeded),
            ["b"] = new(BlowRunStatus.Failed, "Malformed input."),
        };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.RunStates, states));

        Assert.IsTrue(cut.Find(".bit-blow-node[data-id='a']").ClassList.Contains("bit-blow-run-succeeded"));
        Assert.IsTrue(cut.Find(".bit-blow-node[data-id='b']").ClassList.Contains("bit-blow-run-failed"));
        Assert.IsFalse(cut.Find(".bit-blow-node[data-id='c']").ClassName!.Contains("bit-blow-run-"));
        var ids = cut.Find(".bit-blow-node[data-id='b']").GetAttribute("aria-describedby")!.Split(' ');
        StringAssert.StartsWith(cut.Find($"[id='{ids[0]}']").TextContent, "Failed: Malformed input.");

        // The run moves on: the same dictionary, changed, handed over again.
        states["c"] = new(BlowRunStatus.Running);
        cut.Render(p => p.Add(c => c.RunStates, states));
        Assert.IsTrue(cut.Find(".bit-blow-node[data-id='c']").ClassList.Contains("bit-blow-run-running"));
    }
}
