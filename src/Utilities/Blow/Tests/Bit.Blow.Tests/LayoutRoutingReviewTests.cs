using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bunit;

namespace Bit.Blow.Tests;

/// <summary>
/// Regressions found by the independent review of layouts, collapsing, routing, SVG export and run states.
/// </summary>
[TestClass]
public class LayoutRoutingReviewTests : BunitContext
{
    public LayoutRoutingReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge>? edges = null,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            p.Add(c => c.HistoryEnabled, true);
            extra?.Invoke(p);
        });

    private static BlowNode Box(string id, double x = 0, double y = 0, string? parent = null, double w = 100, double h = 40)
        => new() { Id = id, Position = new(x, y), Width = w, Height = h, ParentId = parent };

    private static (List<BlowNode> Nodes, List<BlowEdge> Edges) Nested() => (
    [
        Box("outside", -300, 0),
        Box("g", 0, 0, w: 400, h: 300),
        Box("a", 20, 40, "g"),
        Box("inner", 150, 100, "g", 200, 150),
        Box("deep", 20, 40, "inner"),
    ],
    [
        new() { Id = "outside-a", Source = "outside", Target = "a" },
        new() { Id = "a-deep", Source = "a", Target = "deep" },
    ]);

    private static void AssertNoOverlap(IReadOnlyList<BlowNode> nodes)
    {
        for (var i = 0; i < nodes.Count; i++)
            for (var j = i + 1; j < nodes.Count; j++)
            {
                var a = nodes[i].GetRect();
                var b = nodes[j].GetRect();
                var apart = a.Right <= b.Left + 1e-6 || b.Right <= a.Left + 1e-6 || a.Bottom <= b.Top + 1e-6 || b.Bottom <= a.Top + 1e-6;
                Assert.IsTrue(apart, $"{nodes[i].Id} and {nodes[j].Id} overlap");
            }
    }

    // ---- collapsing ----

    [TestMethod]
    public async Task ExpandingAGroupThatIsItselfFoldedAwayKeepsItsChildrenFoldedAway()
    {
        var (nodes, edges) = Nested();
        var cut = Render(nodes, edges);
        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[3], true));   // inner
        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[1], true));   // g

        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[3], false));  // inner, while g is collapsed

        Assert.IsTrue(nodes[4].Hidden, "a child of a group inside a collapsed group came out on its own");
        CollectionAssert.AreEqual(new[] { "outside", "g" }, cut.FindAll(".bit-blow-node").Select(e => e.GetAttribute("data-id")).ToArray());

        // Expanding g now brings inner's children back with it, inner no longer being collapsed.
        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[1], false));
        Assert.IsFalse(nodes[4].Hidden);
    }

    [TestMethod]
    public async Task EdgesFoldedAwayLeaveTheSelectionAndSelectAllPassesThemOver()
    {
        var (nodes, edges) = Nested();
        edges[1].Selected = true;
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[1], true));
        Assert.IsFalse(edges[1].Selected, "an edge nobody can see stayed selected for the delete key");

        await cut.InvokeAsync(() => cut.Instance.SelectAllAsync());
        Assert.IsTrue(edges[0].Selected, "the edge drawn to the collapsed group is on show");
        Assert.IsFalse(edges[1].Selected);
    }

    [TestMethod]
    public async Task ACollapsedGroupIsDescribedWithTheEdgesDrawnToIt()
    {
        var (nodes, edges) = Nested();
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[1], true));

        var ids = cut.Find(".bit-blow-node[data-id='g']").GetAttribute("aria-describedby")!.Split(' ');
        StringAssert.StartsWith(cut.Find($"[id='{ids[0]}']").TextContent, "Collapsed. 1 incoming connection.");
    }

    // ---- layout from the canvas ----

    [TestMethod]
    public async Task LayingOutWithPinnedNodesLeavesThemWhereTheyAre()
    {
        var nodes = Enumerable.Range(0, 6).Select(i => Box($"n{i}", i * 7, i * 3)).ToList();
        var edges = Enumerable.Range(1, 5).Select(i => new BlowEdge { Id = $"e{i}", Source = "n0", Target = $"n{i}" }).ToList();
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.LayoutAsync(new BlowForceLayoutOptions { IsPinned = n => n.Id == "n0" }));

        Assert.AreEqual(new BlowXYPosition(0, 0), nodes[0].Position);
    }

    [TestMethod]
    public async Task TheCallersOptionsAreLeftAsTheyWere()
    {
        var nodes = new List<BlowNode> { Box("a"), Box("b", 300) };
        var cut = Render(nodes, extra: p => p.Add(c => c.NodeOrigin, new BlowXYPosition(0.5, 0.5)));
        var layout = new BlowGridLayoutOptions();
        var export = new BlowSvgExportOptions();

        await cut.InvokeAsync(() => cut.Instance.LayoutAsync(layout));
        cut.Instance.ToSvg(export);

        Assert.AreEqual(BlowXYPosition.Zero, layout.NodeOrigin);
        Assert.AreEqual(BlowXYPosition.Zero, export.NodeOrigin);
        Assert.IsNull(export.Routing);
    }

    // ---- force and radial ----

    [TestMethod]
    [DataRow(2.0, 300, 60)]
    [DataRow(10.0, 300, 100)]
    [DataRow(180.0, 0, 200)]
    [DataRow(1e200, 50, 30)]
    public void AForceLayoutLeavesNoOverlapsWhateverItIsGiven(double length, int iterations, int count)
    {
        var nodes = Enumerable.Range(0, count).Select(i => Box($"n{i}")).ToList();
        var edges = Enumerable.Range(1, count - 1).Select(i => new BlowEdge { Id = $"e{i}", Source = $"n{(i - 1) / 2}", Target = $"n{i}" }).ToList();

        BlowLayout.Force(nodes, edges, new BlowForceLayoutOptions { IdealEdgeLength = length, Iterations = iterations, StartFromCurrentPositions = iterations == 0 });

        Assert.IsTrue(nodes.All(n => double.IsFinite(n.Position.X) && double.IsFinite(n.Position.Y)), "a position is not a number");
        AssertNoOverlap(nodes);
    }

    [TestMethod]
    public void ARootAskedForIsARootEvenWhenAnotherLeadsToIt()
    {
        var nodes = new List<BlowNode> { Box("a"), Box("b"), Box("a1"), Box("b1") };
        var edges = new List<BlowEdge>
        {
            new() { Id = "a-b", Source = "a", Target = "b" },
            new() { Id = "a-a1", Source = "a", Target = "a1" },
            new() { Id = "b-b1", Source = "b", Target = "b1" },
        };

        BlowLayout.Radial(nodes, edges, new BlowRadialLayoutOptions { IsRoot = n => n.Id is "a" or "b" });

        // Two roots share the first ring around an empty centre, their children the second.
        var centre = new BlowXYPosition(nodes.Average(n => n.Position.X + 50), nodes.Average(n => n.Position.Y + 20));
        double Distance(BlowNode n) => Math.Sqrt(Math.Pow(n.Position.X + 50 - centre.X, 2) + Math.Pow(n.Position.Y + 20 - centre.Y, 2));
        Assert.AreEqual(Distance(nodes[0]), Distance(nodes[1]), 1e-6);
        Assert.IsGreaterThan(Distance(nodes[0]), Distance(nodes[2]));
    }

    // ---- routing ----

    private static bool Crosses(BlowXYPosition a, BlowXYPosition b, BlowRect r)
    {
        if (Math.Abs(a.Y - b.Y) < 1e-9)
            return a.Y > r.Top && a.Y < r.Bottom && Math.Max(Math.Min(a.X, b.X), r.Left) < Math.Min(Math.Max(a.X, b.X), r.Right);
        return a.X > r.Left && a.X < r.Right && Math.Max(Math.Min(a.Y, b.Y), r.Top) < Math.Min(Math.Max(a.Y, b.Y), r.Bottom);
    }

    [TestMethod]
    public void ANodeCloseToAHandleIsStillGoneRoundIfNotByTheFullMargin()
    {
        // A node 30 above a target handle that faces up, between it and the source: the stub ends inside the node's
        // margin but not inside the node, so the route has to go round the node itself.
        var blocker = new BlowRect(380, -70, 240, 40);
        var route = BlowEdgeRouting.Orthogonal(new(0, -200), BlowPosition.Right, new(500, 0), BlowPosition.Top, [blocker]);

        Assert.IsNotNull(route);
        for (var i = 1; i < route.Count; i++)
            Assert.IsFalse(Crosses(route[i - 1], route[i], blocker), $"segment {route[i - 1]} -> {route[i]} runs through the node");
    }

    // ---- SVG export ----

    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    [TestMethod]
    public void CharactersXmlForbidsAreLeftOutRatherThanBreakingTheImage()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Label = "bad￾￿\uD800 end 😀", Width = 100, Height = 40 } };

        var svg = BlowSvgExport.ToSvg(nodes, []);
        var doc = XDocument.Parse(svg);

        Assert.AreEqual("bad end 😀", doc.Descendants(Svg + "text").Single().Value.Replace("  ", " "));
        _ = new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true).GetBytes(svg);
    }

    [TestMethod]
    public void TheImageIsBigEnoughForASelfLoop()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Width = 100, Height = 40, SourcePosition = BlowPosition.Left, TargetPosition = BlowPosition.Left } };
        var edges = new List<BlowEdge> { new() { Id = "loop", Source = "a", Target = "a" } };

        var root = XDocument.Parse(BlowSvgExport.ToSvg(nodes, edges)).Root!;
        var left = double.Parse(root.Attribute("viewBox")!.Value.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
        var path = root.Descendants(Svg + "path").Single(p => p.Parent!.Name != Svg + "marker").Attribute("d")!.Value;
        var minX = Regex.Matches(path, @"-?\d+(\.\d+)?").Select((m, i) => (Value: double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture), i))
            .Where(p => p.i % 2 == 0).Min(p => p.Value);

        Assert.IsLessThanOrEqualTo(minX, left, $"the loop reaches {minX} but the image starts at {left}");
    }

    [TestMethod]
    public async Task TheCanvasExportDrawsEdgesToACollapsedGroupAndRoutesWithTheNodesNearby()
    {
        var (nodes, edges) = Nested();
        // Far-off nodes enough to have passed the router's obstacle limit, had every node counted.
        for (var i = 0; i < 120; i++) nodes.Add(Box($"far{i}", 5000 + i % 12 * 150, 5000 + i / 12 * 80));
        nodes.Add(Box("blocker", -150, -60, w: 40, h: 160));
        edges.Add(new BlowEdge { Id = "routed", Source = "outside", Target = "g", Type = BlowEdgeType.Routed });
        nodes[0].SourcePosition = BlowPosition.Right;
        nodes[1].TargetPosition = BlowPosition.Left;
        var cut = Render(nodes, edges);
        await cut.InvokeAsync(() => cut.Instance.SetCollapsedAsync(nodes[1], true));

        var doc = XDocument.Parse(cut.Instance.ToSvg());
        var paths = doc.Descendants(Svg + "path").Where(p => p.Parent!.Name != Svg + "marker").Select(p => p.Attribute("d")!.Value).ToList();

        Assert.HasCount(2, paths, "the edge drawn to the collapsed group is missing from the image");
        var canvasRouted = cut.Find(".bit-blow-edge[data-id='routed'] .bit-blow-edge-path").GetAttribute("d");
        CollectionAssert.Contains(paths, canvasRouted, "the exported route differs from the one on screen");
    }

    // ---- run states ----

    [TestMethod]
    public void RunStatesUseNeitherTheRingsNorTheBorderColourThatSelectionAndProblemsUse()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Bit.Blow", "Styles", "bit-blow.css"))) dir = dir.Parent;
        var css = File.ReadAllText(Path.Combine(dir!.FullName, "Bit.Blow", "Styles", "bit-blow.css"));

        foreach (Match rule in Regex.Matches(css, @"\.bit-blow-node\.bit-blow-run-(\w+)\s*\{([^}]*)\}"))
        {
            var body = rule.Groups[2].Value;
            Assert.DoesNotContain("box-shadow", body, rule.Value);
            Assert.DoesNotContain("border-color", body, rule.Value);
            Assert.DoesNotContain("opacity", body, rule.Value);
        }

        // The running ring comes before the selection rule, so selection and focus win when both apply.
        Assert.IsLessThan(css.IndexOf(".bit-blow-node.selected {", StringComparison.Ordinal),
            css.IndexOf(".bit-blow-node.bit-blow-run-running {", StringComparison.Ordinal));
    }
}
