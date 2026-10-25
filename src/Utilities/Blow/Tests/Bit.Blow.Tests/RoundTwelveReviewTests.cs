using System.Text.Json.Nodes;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// What the review of the saving format, keyboard and click connections, remote edits during a gesture and render
/// scoping found, each pinned.
/// </summary>
[TestClass]
public class RoundTwelveReviewTests : BunitContext
{
    public RoundTwelveReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // Buttons as a browser reports them while the left button is held: a mouse moving with none held has lost its release.
    private static PointerEventArgs At(double x, double y) => new() { Button = 0, Buttons = 1, PointerId = 1, PointerType = "mouse", ClientX = x, ClientY = y };

    private static AngleSharp.Dom.IElement Node(IRenderedComponent<BlowCanvas> cut, string id) => cut.Find($".bit-blow-node[data-id='{id}']");

    private static AngleSharp.Dom.IElement Handle(IRenderedComponent<BlowCanvas> cut, string nodeId, string type, string handleId = "")
        => cut.Find($".bit-blow-node[data-id='{nodeId}'] .bit-blow-handle[data-handle-type='{type}'][data-handle-id='{handleId}']");

    private static string Heard(IRenderedComponent<BlowCanvas> cut) => cut.Find(".bit-blow-a11y-live").TextContent.TrimEnd('​');

    private static int[] NodeRenders(IRenderedComponent<BlowCanvas> cut)
        => cut.FindComponents<BlowNodeRenderer>().Select(c => c.RenderCount).ToArray();

    // ---- saving ----

    [TestMethod]
    public void ARoutedEdgeIsSavedAndReadBackAsRouted()
    {
        var document = new BlowDocument();
        document.Nodes.Add(new BlowNode { Id = "a" });
        document.Nodes.Add(new BlowNode { Id = "b" });
        document.Edges.Add(new BlowEdge { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Routed });

        var json = BlowSerializer.Serialize(document);
        StringAssert.Contains(json, "\"type\":\"routed\"");

        var result = BlowSerializer.Deserialize(json);
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(BlowEdgeType.Routed, result.Document!.Edges.Single().Type);
    }

    [TestMethod]
    [DataRow("\"schemaVersion\":\"1\",\"version\":0", BlowImportCodes.UnsupportedSchema)]
    [DataRow("\"schemaVersion\":null,\"version\":0", BlowImportCodes.UnsupportedSchema)]
    [DataRow("\"schemaVersion\":1,\"version\":null", BlowImportCodes.InvalidValue)]
    [DataRow("\"schemaVersion\":1,\"version\":\"2\"", BlowImportCodes.InvalidValue)]
    public void AVersionOfTheWrongKindIsReportedNotThrown(string versions, string code)
    {
        var result = BlowSerializer.Deserialize($"{{\"format\":\"bit-blow\",{versions},\"nodes\":[],\"edges\":[]}}");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(code, result.Issues.Single().Code);
    }

    [TestMethod]
    public void AMigrationThatNestsTooDeepIsReportedNotThrown()
    {
        var options = new BlowSerializerOptions { CurrentVersion = 1 };
        options.Migrations.Add(new BlowMigration(0, document =>
        {
            var deep = new JsonObject();
            var current = deep;
            for (var i = 0; i < 100; i++)
            {
                var next = new JsonObject();
                current["x"] = next;
                current = next;
            }
            document["metadata"] = deep;
            return document;
        }));

        var result = BlowSerializer.Deserialize("{\"format\":\"bit-blow\",\"schemaVersion\":1,\"version\":0,\"nodes\":[],\"edges\":[]}", options);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(BlowImportCodes.TooDeep, result.Issues.Single().Code);
    }

    [TestMethod]
    public void AReactFlowStrokeWidthNoDoubleHoldsIsReportedNotThrown()
    {
        var result = BlowReactFlowFormat.Import(
            "{\"nodes\":[{\"id\":\"a\",\"position\":{\"x\":0,\"y\":0}},{\"id\":\"b\",\"position\":{\"x\":0,\"y\":0}}]," +
            "\"edges\":[{\"id\":\"e\",\"source\":\"a\",\"target\":\"b\",\"style\":{\"strokeWidth\":1e999}}]}");

        Assert.IsNotNull(result.Document);
        Assert.AreEqual(1.5, result.Document.Edges.Single().StrokeWidth);
        Assert.IsTrue(result.Issues.Any(i => i.Code == BlowImportCodes.InvalidValue && i.Path == "$.edges[0].style.strokeWidth"));
    }

    // ---- connecting by clicks ----

    private static List<BlowNode> SourceAndFullInput() =>
    [
        new() { Id = "s", Position = new(0, 0) },
        new() { Id = "x", Position = new(300, 0) },
        new()
        {
            Id = "t", Position = new(0, 200),
            Ports = [new BlowPort { Id = "in", Type = BlowHandleType.Target, MaxConnections = 1 }],
        },
    ];

    private IRenderedComponent<BlowCanvas> RenderClickToConnect(List<BlowNode> nodes, List<BlowEdge> edges, List<BlowConnectEndInfo> ends)
        => Render<BlowCanvas>(p => p
            .Add(c => c.Nodes, nodes)
            .Add(c => c.Edges, edges)
            .Add(c => c.ConnectOnClick, true)
            .Add(c => c.OnConnectEnd, e => ends.Add(e)));

    [TestMethod]
    public void AClickOnAFullInputSaysWhyAsADropOnItWould()
    {
        var ends = new List<BlowConnectEndInfo>();
        var edges = new List<BlowEdge> { new() { Id = "x-t", Source = "x", Target = "t", TargetHandle = "in" } };
        var cut = RenderClickToConnect(SourceAndFullInput(), edges, ends);

        Handle(cut, "s", "source").PointerDown(At(75, 40));
        cut.Find(".bit-blow").PointerUp(At(75, 40));
        Assert.IsTrue(cut.Instance.IsConnecting, "the first click leaves the connection pending");

        Handle(cut, "t", "target", "in").PointerDown(At(75, 200));

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual(BlowConnectionRejection.TargetFull, ends.Single().Rejection);
        Assert.HasCount(1, edges);
    }

    [TestMethod]
    public async Task AnInputFreedSinceTheConnectionStartedTakesIt()
    {
        var ends = new List<BlowConnectEndInfo>();
        var edges = new List<BlowEdge> { new() { Id = "x-t", Source = "x", Target = "t", TargetHandle = "in" } };
        var cut = RenderClickToConnect(SourceAndFullInput(), edges, ends);

        Handle(cut, "s", "source").PointerDown(At(75, 40));
        cut.Find(".bit-blow").PointerUp(At(75, 40));

        // A collaborator frees the input while the connection waits.
        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync(edges: [edges[0]]));
        Handle(cut, "t", "target", "in").PointerDown(At(75, 200));

        Assert.IsTrue(ends.Single().IsValid, $"refused: {ends.Single().Rejection}");
        Assert.IsTrue(edges.Any(e => e.Source == "s" && e.Target == "t" && e.TargetHandle == "in"));
    }

    // ---- a keyboard connection aimed at a port that fills ----

    [TestMethod]
    public async Task EnterOnAPortFilledSinceItWasAimedAtSaysWhyAndKeepsWaiting()
    {
        JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js")
            .Setup<BlowMeasuredNode[]>("measureNodes", _ => true)
            .SetResult(
            [
                new("s", 150, 40, [new("", "source", "bottom", 75, 40)]),
                new("t", 150, 40, [new("a", "target", "top", 50, 0), new("b", "target", "top", 100, 0)]),
            ]);
        var nodes = new List<BlowNode>
        {
            new() { Id = "s" },
            new() { Id = "x", Position = new(300, 0) },
            new()
            {
                Id = "t", Position = new(0, 200),
                Ports =
                [
                    new BlowPort { Id = "a", Type = BlowHandleType.Target, MaxConnections = 1 },
                    new BlowPort { Id = "b", Type = BlowHandleType.Target, MaxConnections = 1 },
                ],
            },
        };
        var edges = new List<BlowEdge>();
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, edges));

        await Node(cut, "s").KeyDownAsync(new KeyboardEventArgs { Key = "c" });
        await Node(cut, "t").FocusAsync(new FocusEventArgs());
        await Node(cut, "t").KeyDownAsync(new KeyboardEventArgs { Key = "c" }); // steps from input a to input b

        await cut.InvokeAsync(() => cut.Instance.AddEdges(new BlowEdge { Id = "x-t", Source = "x", Target = "t", TargetHandle = "b" }));
        await Node(cut, "t").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsFalse(edges.Any(e => e.Source == "s"), "connected to a port that was never chosen");
        Assert.IsTrue(cut.Instance.IsConnecting);
        Assert.AreEqual(new BlowAriaLabels().ConnectionRejected(BlowConnectionRejection.TargetFull), Heard(cut));
    }

    // ---- presses that keep themselves from the pane ----

    [TestMethod]
    public void APressOnAHandleForgetsTheNodeAnEarlierPressLandedOn()
    {
        var nodeClicks = new List<string>();
        var cut = Render<BlowCanvas>(p => p
            .Add(c => c.Nodes, [new BlowNode { Id = "a" }, new BlowNode { Id = "b", Position = new(0, 200) }])
            .Add(c => c.Edges, [])
            .Add(c => c.OnNodeClick, n => nodeClicks.Add(n.Id)));
        var pane = cut.Find(".bit-blow");

        // A press on a node whose click never comes, then a press on a handle, whose click the pane hears.
        Node(cut, "a").PointerDown(At(10, 10));
        pane.PointerUp(At(10, 10));
        Handle(cut, "b", "source").PointerDown(At(75, 240));
        pane.PointerUp(At(75, 240));
        pane.Click(new MouseEventArgs { ClientX = 75, ClientY = 240 });

        Assert.IsEmpty(nodeClicks);
    }

    [TestMethod]
    public void ANodePressTheBrowserTookOverReportsNoClickLater()
    {
        var nodeClicks = new List<string>();
        var cut = Render<BlowCanvas>(p => p
            .Add(c => c.Nodes, [new BlowNode { Id = "a" }])
            .Add(c => c.Edges, [])
            .Add(c => c.OnNodeClick, n => nodeClicks.Add(n.Id)));
        var pane = cut.Find(".bit-blow");

        Node(cut, "a").PointerDown(At(10, 10));
        pane.PointerCancel(At(10, 10));
        pane.Click(new MouseEventArgs { ClientX = 400, ClientY = 400 });

        Assert.IsEmpty(nodeClicks);
    }

    [TestMethod]
    public async Task StartingAResizeEndsAKeyboardConnection()
    {
        JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js")
            .Setup<BlowMeasuredNode[]>("measureNodes", _ => true)
            .SetResult([new("a", 150, 40, [new("", "source", "bottom", 75, 40)]), new("b", 150, 40, [new("", "target", "top", 75, 0)])]);
        var cut = Render<BlowCanvas>(p => p
            .Add(c => c.Nodes, [new BlowNode { Id = "a" }, new BlowNode { Id = "b", Position = new(0, 200) }])
            .Add(c => c.Edges, []));
        await Node(cut, "a").KeyDownAsync(new KeyboardEventArgs { Key = "c" });
        Assert.IsTrue(cut.Instance.IsConnecting);

        await cut.InvokeAsync(() => cut.Instance.StartResize("b", BlowResizeDirection.Bottom, 0, 0, 10, 10, false, 1000, 1000, 1));

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual(new BlowAriaLabels().KeyboardConnectionCancelled, Heard(cut));
    }

    // ---- an edit from elsewhere during a drag ----

    [TestMethod]
    public async Task ARemoteEditDuringADragKeepsWhereTheDragStartedInTheHistory()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40 },
            new() { Id = "b", Position = new(300, 0), Width = 100, Height = 40 },
        };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, []).Add(c => c.HistoryEnabled, true));
        var pane = cut.Find(".bit-blow");

        Node(cut, "a").PointerDown(At(10, 10));
        pane.PointerMove(At(110, 10));
        await cut.InvokeAsync(() => cut.Instance.EditAsync(() => nodes[1].Position = new(500, 500),
            "remote", BlowChangeOrigin.Remote, recordHistory: false));
        pane.PointerMove(At(160, 10));
        pane.PointerUp(At(160, 10));
        Assert.AreEqual(new BlowXYPosition(150, 0), nodes[0].Position);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());

        Assert.AreEqual(new BlowXYPosition(0, 0), nodes.Single(n => n.Id == "a").Position, "undo left the node mid-drag");
        Assert.AreEqual(new BlowXYPosition(500, 500), nodes.Single(n => n.Id == "b").Position);
    }

    // ---- rendering only what changed ----

    [TestMethod]
    public async Task AnAnnouncementRendersNoNode()
    {
        var cut = Render<BlowCanvas>(p => p
            .Add(c => c.Nodes, [new BlowNode { Id = "a" }, new BlowNode { Id = "b", Position = new(0, 200) }])
            .Add(c => c.Edges, []));
        var before = NodeRenders(cut);

        await cut.InvokeAsync(() => cut.Instance.Announce("Step two of five"));

        Assert.AreEqual("Step two of five", Heard(cut));
        CollectionAssert.AreEqual(before, NodeRenders(cut));
    }

    [TestMethod]
    public void DraggingANodeFarFromARoutedEdgeDoesNotRenderTheEdgeAgain()
    {
        var cut = Render<BlowCanvas>(p => p
            .Add(c => c.Nodes,
            [
                new BlowNode { Id = "a", Position = new(0, 0), Width = 100, Height = 40 },
                new BlowNode { Id = "b", Position = new(0, 300), Width = 100, Height = 40 },
                new BlowNode { Id = "far", Position = new(3000, 3000), Width = 100, Height = 40 },
            ])
            .Add(c => c.Edges, [new BlowEdge { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Routed }]));
        var pane = cut.Find(".bit-blow");
        var edge = cut.FindComponent<BlowEdgeRenderer>();
        var path = cut.Find(".bit-blow-edge[data-id='e'] .bit-blow-edge-path").GetAttribute("d");

        Node(cut, "far").PointerDown(At(3010, 3010));
        pane.PointerMove(At(3050, 3010));
        var renders = edge.RenderCount;
        pane.PointerMove(At(3090, 3010));
        pane.PointerMove(At(3130, 3010));

        Assert.AreEqual(renders, edge.RenderCount, "the routed edge rendered again for a node nowhere near it");
        Assert.AreEqual(path, cut.Find(".bit-blow-edge[data-id='e'] .bit-blow-edge-path").GetAttribute("d"));
    }

    [TestMethod]
    public void ARoutedEdgeStillGoesRoundANodeDraggedIntoItsWay()
    {
        var cut = Render<BlowCanvas>(p => p
            .Add(c => c.Nodes,
            [
                new BlowNode { Id = "a", Position = new(0, 0), Width = 100, Height = 40 },
                new BlowNode { Id = "b", Position = new(0, 300), Width = 100, Height = 40 },
                new BlowNode { Id = "block", Position = new(3000, 3000), Width = 100, Height = 40 },
            ])
            .Add(c => c.Edges, [new BlowEdge { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Routed }]));
        var pane = cut.Find(".bit-blow");
        var straight = cut.Find(".bit-blow-edge[data-id='e'] .bit-blow-edge-path").GetAttribute("d");

        // Dragged from far away to right between the two ends.
        Node(cut, "block").PointerDown(At(3010, 3010));
        pane.PointerMove(At(10, 160));
        pane.PointerUp(At(10, 160));

        Assert.AreNotEqual(straight, cut.Find(".bit-blow-edge[data-id='e'] .bit-blow-edge-path").GetAttribute("d"));
    }
}
