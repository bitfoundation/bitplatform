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
            new() { Id = "c-a", Source = "c", Target = "a", MarkerWidth = 12, MarkerHeight = 5 },
            new() { Id = "b-a", Source = "b", Target = "a", MarkerType = BlowMarkerType.ArrowClosed, MarkerWidth = 7, MarkerHeight = 7 },
        };

        var doc = XDocument.Parse(BlowSvgExport.ToSvg(nodes, edges));
        var paths = doc.Descendants(Svg + "path").Where(p => p.Parent!.Name != Svg + "marker").ToList();
        var markers = doc.Descendants(Svg + "marker").ToList();

        CollectionAssert.AreEqual(new[] { "m0", "m1", "m2", "m3" }, markers.Select(m => m.Attribute("id")!.Value).ToArray());
        CollectionAssert.AreEqual(new[] { "7", "7", "12", "7" }, markers.Select(m => m.Attribute("markerWidth")!.Value).ToArray());
        CollectionAssert.AreEqual(new[] { "7", "7", "5", "7" }, markers.Select(m => m.Attribute("markerHeight")!.Value).ToArray());
        Assert.AreEqual("red", markers[1].Element(Svg + "path")!.Attribute("stroke")!.Value);
        CollectionAssert.AreEqual(new[] { "url(#m0)", "url(#m1)", "url(#m0)", "url(#m2)", "url(#m3)" }, paths.Select(p => p.Attribute("marker-end")?.Value).ToArray());
        CollectionAssert.AreEqual(new[] { null, "url(#m1)", null, null, null }, paths.Select(p => p.Attribute("marker-start")?.Value).ToArray());
    }

    [TestMethod]
    public void ANodeWithTwoAncestorsIsPlacedByTheSumOfAllItsParents()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "g", Type = "group", Label = "Group", Position = new(200, 0), Width = 400, Height = 300 },
            new() { Id = "c", Label = "Child", ParentId = "g", Position = new(50, 50), Width = 200, Height = 150 },
            new() { Id = "d", Label = "Grandchild", ParentId = "c", Position = new(10, 20), Width = 100, Height = 40 },
        };

        var doc = XDocument.Parse(BlowSvgExport.ToSvg(nodes, []));
        var rect = doc.Descendants(Svg + "text").Single(t => t.Value == "Grandchild").Parent!.Element(Svg + "rect")!;

        Assert.AreEqual(("260", "70"), (rect.Attribute("x")!.Value, rect.Attribute("y")!.Value));
    }

    [TestMethod]
    public void EveryNodeIsPlacedWhereTheCanvasPlacesItHoweverDeepOrTangledItsParents()
    {
        // Two chains deeper than the canvas follows in one walk - one listed parents first, the other children first -
        // a parent cycle, a node that is its own parent and one whose parent is not in the list.
        var nodes = new List<BlowNode>();
        for (var i = 0; i < 60; i++) nodes.Add(Nested($"p{i}", i == 0 ? null : $"p{i - 1}"));
        for (var i = 59; i >= 0; i--) nodes.Add(Nested($"c{i}", i == 0 ? null : $"c{i - 1}"));
        nodes.AddRange([Nested("x", "z"), Nested("y", "x"), Nested("z", "y"), Nested("s", "s"), Nested("m", "nowhere")]);

        Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes));
        var doc = XDocument.Parse(BlowSvgExport.ToSvg(nodes, []));

        Assert.AreEqual(new BlowXYPosition(600, 60), nodes.Single(n => n.Id == "p59").AbsolutePosition, "the canvas did not sum the whole chain");
        Assert.AreEqual(new BlowXYPosition(600, 60), nodes.Single(n => n.Id == "c59").AbsolutePosition,
            "the canvas did not sum the whole of a chain listed children first");
        foreach (var node in nodes)
        {
            var rect = doc.Descendants(Svg + "text").Single(t => t.Value == node.Id).Parent!.Element(Svg + "rect")!;
            Assert.AreEqual((At(node.AbsolutePosition.X), At(node.AbsolutePosition.Y)), (rect.Attribute("x")!.Value, rect.Attribute("y")!.Value), node.Id);
        }

        static BlowNode Nested(string id, string? parent)
            => new() { Id = id, Label = id, ParentId = parent, Position = new(10, 1), Width = 20, Height = 10 };

        static string At(double value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    [TestMethod]
    public void AVeryLongParentChainIsExportedWithEveryParentSummedOnce()
    {
        const int length = 50_000;
        var nodes = Enumerable.Range(0, length)
            .Select(i => new BlowNode { Id = $"n{i}", ParentId = i == 0 ? null : $"n{i - 1}", Position = new(1, 0), Width = 10, Height = 10 })
            .ToList();

        var doc = XDocument.Parse(BlowSvgExport.ToSvg(nodes, [], new BlowSvgExportOptions { Background = null }));
        var rects = doc.Descendants(Svg + "rect").ToList();

        Assert.HasCount(length, rects);
        Assert.AreEqual("50000", rects[^1].Attribute("x")!.Value, "the deepest node is drawn last, at the sum of all its parents");
    }

    [TestMethod]
    public void ANodeUnderAHiddenAncestorIsLeftOutHoweverFarUpTheAncestorIs()
    {
        var nodes = Enumerable.Range(0, 80)
            .Select(i => new BlowNode { Id = $"n{i}", Label = $"n{i}", ParentId = i == 0 ? null : $"n{i - 1}", Hidden = i == 0, Width = 10, Height = 10 })
            .ToList();
        nodes.Add(new BlowNode { Id = "shown", Label = "shown", Width = 10, Height = 10 });

        var doc = XDocument.Parse(BlowSvgExport.ToSvg(nodes, [new BlowEdge { Id = "e", Source = "shown", Target = "n79" }]));

        CollectionAssert.AreEqual(new[] { "shown" }, doc.Descendants(Svg + "text").Select(t => t.Value).ToArray());
        Assert.IsEmpty(doc.Descendants(Svg + "path").Where(p => p.Parent?.Name != Svg + "marker"), "an edge to a node left out was drawn");
    }

    [TestMethod]
    public void OfNodesThatShareAnIdOnlyTheFirstIsDrawnAsOnTheCanvas()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Label = "first", Width = 100, Height = 40 },
            new() { Id = "a", Label = "second", Position = new(500, 500), Width = 20, Height = 10 },
        };

        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes));
        var doc = XDocument.Parse(BlowSvgExport.ToSvg(nodes, [], new BlowSvgExportOptions { Background = null }));

        Assert.HasCount(1, cut.FindAll(".bit-blow-node[data-id='a']"), "the canvas does not draw the second either");
        var rect = doc.Descendants(Svg + "rect").Single();
        Assert.AreEqual(("0", "0", "100", "40"),
            (rect.Attribute("x")!.Value, rect.Attribute("y")!.Value, rect.Attribute("width")!.Value, rect.Attribute("height")!.Value));
        CollectionAssert.AreEqual(new[] { "first" }, doc.Descendants(Svg + "text").Select(t => t.Value).ToArray());
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
