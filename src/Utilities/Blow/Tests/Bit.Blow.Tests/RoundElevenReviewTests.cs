using System.Xml.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// What the review of the routing interface, the Barnes-Hut force layout, measuring only what changed and the
/// scoped drop found, each pinned.
/// </summary>
[TestClass]
public class RoundElevenReviewTests : BunitContext
{
    public RoundElevenReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<BlowNode> Three() =>
    [
        new() { Id = "a", Position = new(0, 0) },
        new() { Id = "b", Position = new(0, 200) },
        new() { Id = "c", Position = new(300, 0) },
    ];

    private static int[] NodeRenders(IRenderedComponent<BlowCanvas> cut)
        => cut.FindComponents<BlowNodeRenderer>().Select(c => c.RenderCount).ToArray();

    // Buttons as a browser reports them while the left button is held: a mouse moving with none held has lost its release.
    private static PointerEventArgs At(double x, double y) => new() { Button = 0, Buttons = 1, PointerId = 1, PointerType = "mouse", ClientX = x, ClientY = y };

    // ---- the drop and the click after it ----

    [TestMethod]
    public void TheClickTheBrowserFiresAfterADropRendersNoNode()
    {
        // The press captured the pointer on the pane, so the click after a real mouse drop lands on the pane.
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, []));
        var pane = cut.Find(".bit-blow");
        cut.FindAll(".bit-blow-node")[2].PointerDown(At(300, 0));
        pane.PointerMove(At(340, 20));
        pane.PointerUp(At(340, 20));
        var dropped = NodeRenders(cut);

        pane.Click(new MouseEventArgs { ClientX = 340, ClientY = 20 });

        CollectionAssert.AreEqual(dropped, NodeRenders(cut), "the click after a drop rendered nodes again");
    }

    [TestMethod]
    public async Task AnAsyncDragStopHandlerKeepsTheDropToTheMovedNode()
    {
        var gate = new TaskCompletionSource();
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, [])
            .Add(c => c.OnNodeDragStop, async _ => await gate.Task));
        var pane = cut.Find(".bit-blow");
        cut.FindAll(".bit-blow-node")[2].PointerDown(At(300, 0));
        pane.PointerMove(At(340, 20));
        var dragged = NodeRenders(cut);

        var up = pane.PointerUpAsync(At(340, 20));
        await cut.InvokeAsync(gate.SetResult);
        await up;

        var rendered = NodeRenders(cut).Zip(dragged, (after, before) => after > before).ToArray();
        CollectionAssert.AreEqual(new[] { false, false, true }, rendered);
    }

    [TestMethod]
    public void APressOnANodeReportedAtThePaneIsTheNodesClick()
    {
        var nodeClicks = new List<string>();
        var paneClicks = 0;
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, [])
            .Add(c => c.OnNodeClick, n => nodeClicks.Add(n.Id))
            .Add(c => c.OnPaneClick, _ => paneClicks++));
        var pane = cut.Find(".bit-blow");

        cut.FindAll(".bit-blow-node")[1].PointerDown(At(10, 210));
        pane.PointerUp(At(10, 210));
        pane.Click(new MouseEventArgs { ClientX = 10, ClientY = 210 });

        CollectionAssert.AreEqual(new[] { "b" }, nodeClicks);
        Assert.AreEqual(0, paneClicks);

        // And a click that really is on the pane is the pane's.
        pane.PointerDown(At(600, 600));
        pane.PointerUp(At(600, 600));
        pane.Click(new MouseEventArgs { ClientX = 600, ClientY = 600 });
        Assert.AreEqual(1, paneClicks);
        Assert.HasCount(1, nodeClicks);
    }

    [TestMethod]
    public void AClickTheBrowserReportsAtTheNodeIsReportedOnce()
    {
        var nodeClicks = 0;
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, []).Add(c => c.OnNodeClick, _ => nodeClicks++));

        cut.FindAll(".bit-blow-node")[1].PointerDown(At(10, 210));
        cut.Find(".bit-blow").PointerUp(At(10, 210));
        cut.FindAll(".bit-blow-node")[1].Click(new MouseEventArgs());

        Assert.AreEqual(1, nodeClicks);
    }

    /// <summary>An add-on that shows how many nodes are being dragged, as the contract says to: on GeometryVersion.</summary>
    private sealed class DraggingCount : ComponentBase
    {
        [CascadingParameter(Name = BlowCascadingNames.Context)] private IBlowContext Flow { get; set; } = default!;

        [CascadingParameter(Name = BlowCascadingNames.GeometryVersion)] private int GeometryVersion { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenElement(0, "span");
            b.AddAttribute(1, "class", "dragging-count");
            b.AddContent(2, Flow.CurrentNodes.Count(n => n.Dragging));
            b.CloseElement();
        }
    }

    [TestMethod]
    public void AnAddOnOnTheGeometryVersionSeesTheDragEnd()
    {
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, [])
            .Add(c => c.ChildContent, (RenderFragment)(b => { b.OpenComponent<DraggingCount>(0); b.CloseComponent(); })));
        var pane = cut.Find(".bit-blow");

        cut.FindAll(".bit-blow-node")[2].PointerDown(At(300, 0));
        Assert.AreEqual("1", cut.Find(".dragging-count").TextContent);
        pane.PointerMove(At(340, 20));
        pane.PointerUp(At(340, 20));

        Assert.AreEqual("0", cut.Find(".dragging-count").TextContent);
    }

    // ---- measuring ----

    private static BlowJSModuleSetup Measuring(BunitContext context, Func<BlowMeasuredNode[]> measured)
    {
        var module = context.JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");
        module.Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(measured());
        return new BlowJSModuleSetup(module);
    }

    private sealed record BlowJSModuleSetup(BunitJSModuleInterop Module)
    {
        /// <summary>The changedOnly and include arguments of the last measurement.</summary>
        public (bool ChangedOnly, string[] Include) Last
        {
            get
            {
                var call = Module.Invocations["measureNodes"].Last();
                return ((bool)call.Arguments[2]!, ((IEnumerable<string>)call.Arguments[3]!).ToArray());
            }
        }
    }

    private static BlowMeasuredNode[] Measured(params string[] ids) => ids.Select(id => new BlowMeasuredNode(id, 150, 40, [])).ToArray();

    [TestMethod]
    public void ANodeObjectPutBackAfterAnotherOfItsIdIsAskedForByName()
    {
        var x = new BlowNode { Id = "a" };
        var nodes = new List<BlowNode> { x, new() { Id = "b", Position = new(0, 200) }, new() { Id = "c", Position = new(0, 400) } };
        var js = Measuring(this, () => Measured("a", "b", "c"));
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, []));

        // Another object takes a's place: it is named, since the browser's memory of "a" is x's.
        var y = new BlowNode { Id = "a" };
        cut.Render(p => p.Add(c => c.Nodes, new List<BlowNode> { y, nodes[1], nodes[2] }));
        Assert.AreEqual((true, "a"), (js.Last.ChangedOnly, string.Join(",", js.Last.Include)));

        // x comes back: the browser's last report of "a" went to y, so x is named too.
        cut.Render(p => p.Add(c => c.Nodes, nodes));
        Assert.AreEqual((true, "a"), (js.Last.ChangedOnly, string.Join(",", js.Last.Include)));

        // Nothing changed hands since: nothing is named.
        cut.Render(p => p.Add(c => c.Nodes, nodes));
        Assert.AreEqual((true, ""), (js.Last.ChangedOnly, string.Join(",", js.Last.Include)));
    }

    [TestMethod]
    public async Task UpdateNodeInternalsReadsTheNamedNodesAfreshAndRefreshReadsThemAll()
    {
        var nodes = Three();
        var js = Measuring(this, () => Measured("a", "b", "c"));
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, []));

        await cut.InvokeAsync(() => cut.Instance.UpdateNodeInternals("b"));
        Assert.AreEqual((true, "b"), (js.Last.ChangedOnly, string.Join(",", js.Last.Include)));

        await cut.InvokeAsync(() => cut.Instance.UpdateNodeInternals());
        Assert.IsFalse(js.Last.ChangedOnly, "with no node named, every node is read afresh");

        await cut.InvokeAsync(() => cut.Instance.Refresh(remeasure: true));
        Assert.IsFalse(js.Last.ChangedOnly);
    }

    // ---- routing ----

    private sealed class CountingRouter(Func<BlowRouteRequest, BlowPathResult?> answer) : IBlowEdgeRouter
    {
        public int Calls { get; private set; }

        public BlowPathResult? Route(BlowRouteRequest request)
        {
            Calls++;
            return answer(request);
        }
    }

    private static List<BlowNode> Apart() =>
    [
        new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40, SourcePosition = BlowPosition.Right },
        new() { Id = "b", Position = new(500, 0), Width = 100, Height = 40, TargetPosition = BlowPosition.Left },
    ];

    [TestMethod]
    public void EdgesBuiltAfreshOnEveryRenderKeepTheirRoutes()
    {
        var router = new CountingRouter(_ => new BlowPathResult("M0 0L1 1", 0, 0));
        var nodes = Apart();
        static List<BlowEdge> Fresh() => [new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Routed }];
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, Fresh()).Add(c => c.Routing, router));
        var calls = router.Calls;

        for (var i = 0; i < 5; i++) cut.Render(p => p.Add(c => c.Edges, Fresh()));

        Assert.AreEqual(calls, router.Calls, "the same route was searched for again for each new edge object");
    }

    [TestMethod]
    public void TheBuiltInRoutersSettingsChangedInPlaceReRoute()
    {
        var nodes = Apart();
        nodes.Insert(1, new BlowNode { Id = "wall", Position = new(250, -100), Width = 60, Height = 240 });
        var options = new BlowRoutingOptions { Margin = 10 };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Routed } };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, edges).Add(c => c.Routing, options));
        var before = cut.Find(".bit-blow-edge-path").GetAttribute("d");

        options.Margin = 40;
        cut.Render();

        Assert.AreNotEqual(before, cut.Find(".bit-blow-edge-path").GetAttribute("d"));
    }

    [TestMethod]
    public void ARouterAnsweringWithAnEmptyPathGetsASmoothStepOnScreenAndInTheExport()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "routed", Source = "a", Target = "b", Type = BlowEdgeType.Routed },
            new() { Id = "step", Source = "a", Target = "b", Type = BlowEdgeType.SmoothStep },
        };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Apart()).Add(c => c.Edges, edges)
            .Add(c => c.Routing, new CountingRouter(_ => default(BlowPathResult))));

        Assert.AreEqual(cut.Find(".bit-blow-edge[data-id='step'] .bit-blow-edge-path").GetAttribute("d"),
            cut.Find(".bit-blow-edge[data-id='routed'] .bit-blow-edge-path").GetAttribute("d"));
        XDocument.Parse(cut.Instance.ToSvg()); // no exception, and well-formed
    }

    [TestMethod]
    [DataRow("M0 0 V900", 0, 0, 0, 900)]
    [DataRow("M100 20 h200 v300 h-50", 100, 20, 300, 320)]
    [DataRow("m10 10 l20 20 20 20", 10, 10, 50, 50)]
    [DataRow("M0 0 C 10 -50 90 150 100 0", 0, -50, 100, 150)]
    [DataRow("M0,0q50,80,100,0t100,0", 0, -80, 200, 80)]
    [DataRow("M0 0 C0 -40 60 -40 60 0 S120 40 120 0", 0, -40, 120, 40)]
    [DataRow("M1e2 2E1 L-3.5.5", -3.5, 0.5, 100, 20)]
    [DataRow("M0 0 A50 50 0 0 1 100 0", -100, -100, 100, 100)]
    [DataRow("M0 0 a50 50 0 01 100 0", -100, -100, 100, 100)]
    [DataRow("M0 0 L10 10 Z l5 5", 0, 0, 10, 10)]
    public void TheExportReadsEveryPathCommandForItsBounds(string path, double left, double top, double right, double bottom)
    {
        var points = BlowSvgExport.PointsOf(path);

        Assert.AreEqual(left, points.Min(p => p.X), 1e-9);
        Assert.AreEqual(top, points.Min(p => p.Y), 1e-9);
        Assert.AreEqual(right, points.Max(p => p.X), 1e-9);
        Assert.AreEqual(bottom, points.Max(p => p.Y), 1e-9);
    }

    [TestMethod]
    public void TheExportFitsARoutersPathOfHorizontalAndVerticalRuns()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Routed } };
        var router = new CountingRouter(r => new BlowPathResult($"M{r.Source.X} {r.Source.Y} V900 H{r.Target.X} V{r.Target.Y}", 0, 0));

        var svg = XDocument.Parse(BlowSvgExport.ToSvg(Apart(), edges, new BlowSvgExportOptions { Routing = router, Padding = 0 }));
        var box = svg.Root!.Attribute("viewBox")!.Value.Split(' ').Select(v => double.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

        Assert.IsGreaterThanOrEqualTo(900, box[1] + box[3], "the export cut off the route's lowest run");
    }

    [TestMethod]
    public void AMalformedPathKeepsWhatWasReadAndStops()
    {
        CollectionAssert.AreEqual(new[] { (0.0, 0.0), (5.0, 5.0) }, BlowSvgExport.PointsOf("M0 0 L5 5 L x").ToArray());
        Assert.IsEmpty(BlowSvgExport.PointsOf("10 20 L5 5"));
    }

    // ---- force layout with positions that are not numbers ----

    [TestMethod]
    [DataRow(double.NaN, 20)]
    [DataRow(double.PositiveInfinity, 20)]
    [DataRow(1e160, 20)]
    [DataRow(double.NaN, 600)]
    [DataRow(1e300, 600)]
    public void AForceLayoutStartingFromPositionsThatAreNotNumbersEnds(double bad, int count)
    {
        var nodes = Enumerable.Range(0, count).Select(i => new BlowNode { Id = $"n{i}", Width = 100, Height = 40, Position = new(i * 10, i * 5) }).ToList();
        nodes[3].Position = new(bad, 0);
        nodes[7].Position = new(0, -bad);
        var edges = Enumerable.Range(1, count - 1).Select(i => new BlowEdge { Id = $"e{i}", Source = $"n{(i - 1) / 2}", Target = $"n{i}" }).ToList();

        var task = Task.Run(() => BlowLayout.Force(nodes, edges, new BlowForceLayoutOptions { StartFromCurrentPositions = true, Iterations = 60 }));
        Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(30)), "the layout never ended");

        Assert.IsTrue(nodes.All(n => double.IsFinite(n.Position.X) && double.IsFinite(n.Position.Y)), "a position is not a number");
    }

    [TestMethod]
    public void APinnedNodeWithAPositionThatIsNotANumberIsLeftAloneAndTheRestStillLayOut()
    {
        var nodes = Enumerable.Range(0, 30).Select(i => new BlowNode { Id = $"n{i}", Width = 100, Height = 40 }).ToList();
        nodes[0].Position = new(double.NaN, 1e200);
        var edges = Enumerable.Range(1, 29).Select(i => new BlowEdge { Id = $"e{i}", Source = $"n{(i - 1) / 2}", Target = $"n{i}" }).ToList();

        var task = Task.Run(() => BlowLayout.Force(nodes, edges, new BlowForceLayoutOptions { IsPinned = n => n.Id == "n0" }));
        Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(30)), "the layout never ended");

        Assert.IsTrue(double.IsNaN(nodes[0].Position.X), "the pinned node was moved");
        Assert.IsTrue(nodes.Skip(1).All(n => double.IsFinite(n.Position.X) && double.IsFinite(n.Position.Y)));
        Assert.IsGreaterThan(20, nodes.Skip(1).Select(n => n.Position).Distinct().Count(), "the others did not lay out");
    }
}
