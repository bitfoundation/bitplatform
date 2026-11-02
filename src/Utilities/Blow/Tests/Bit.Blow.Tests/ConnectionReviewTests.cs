using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Regressions found reviewing the connection pipeline: a refusal under the pointer is final, a confirmation never
/// takes the next gesture's pointer capture or its end event, loose mode counts and judges ports the same way
/// everywhere, a reconnected edge can go back onto its own full handle, and a drop is judged against the graph as
/// it is when the user lets go.
/// </summary>
[TestClass]
public class ConnectionReviewTests : BunitContext
{
    public ConnectionReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitJSModuleInterop Module() => JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge> edges,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges);
            extra?.Invoke(p);
        });

    private static BlowPort In(string? id = null, int? max = null, string? type = null)
        => new() { Id = id, Type = BlowHandleType.Target, MaxConnections = max, DataType = type };

    [TestMethod]
    public async Task ReleasingOnAHandleThatRefusesDoesNotSnapToAValidOneNearby()
    {
        // b's input is full; d's input, valid, is 15 pixels from where the user lets go - inside the snapping radius.
        var nodes = new List<BlowNode>
        {
            new() { Id = "a" },
            new() { Id = "b", Position = new(0, 200), Ports = [In(max: 1)] },
            new() { Id = "c", Position = new(300, 0) },
            new() { Id = "d", Position = new(15, 200) },
        };
        var edges = new List<BlowEdge> { new() { Id = "full", Source = "c", Target = "b" } };
        var ends = new List<BlowConnectEndInfo>();
        var module = Module();
        module.Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(
        [
            new("a", 150, 40, [new BlowMeasuredHandle("", "source", "bottom", 75, 40)]),
            new("b", 150, 40, [new BlowMeasuredHandle("", "target", "top", 75, 0)]),
            new("d", 150, 40, [new BlowMeasuredHandle("", "target", "top", 75, 0)]),
        ]);
        module.Setup<BlowHandleHit?>("elementDataAtPoint", _ => true).SetResult(new BlowHandleHit("b", "", "target", "top"));
        var cut = Render(nodes, edges, p => p.Add(c => c.OnConnectEnd, e => ends.Add(e)));
        var pane = cut.Find(".bit-blow");

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 75, 40));
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 75, ClientY = 201 });
        pane.PointerUp(new PointerEventArgs { PointerId = 1, ClientX = 75, ClientY = 201 });

        Assert.HasCount(1, edges, "the drop on the full input connected to the neighbouring one");
        Assert.AreEqual(BlowConnectionRejection.TargetFull, ends.Single().Rejection);
    }

    [TestMethod]
    public async Task AConfirmationThatThrowsStillEndsItsGesture()
    {
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render([new() { Id = "a" }, new() { Id = "b" }], [], p => p
            .Add(c => c.OnConnectEnd, e => ends.Add(e))
            .Add(c => c.OnBeforeConnect, async _ =>
            {
                await Task.Yield();
                throw new InvalidOperationException("offline");
            }));

        await cut.InvokeAsync(() =>
        {
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
            cut.Instance.CompleteConnection("b", null, BlowHandleType.Target);
        });
        await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsFalse(ends.Single().IsValid);
        Assert.AreEqual(BlowConnectionRejection.RejectedByRule, ends.Single().Rejection);
    }

    [TestMethod]
    public async Task APendingConfirmationDoesNotReleaseTheNextGesturesPointer()
    {
        var confirmation = new TaskCompletionSource<bool>();
        var captures = new List<string>();
        var module = Module();
        module.SetupVoid("setPointerCapture", _ => true).SetVoidResult();
        module.SetupVoid("releasePointerCapture", _ => true).SetVoidResult();
        module.Setup<BlowHandleHit?>("elementDataAtPoint", _ => true).SetResult(new BlowHandleHit("b", "", "target", "top"));
        var cut = Render([new() { Id = "a" }, new() { Id = "b", Position = new(0, 200) }, new() { Id = "c" }], [],
            p => p.Add(c => c.OnBeforeConnect, _ => confirmation.Task));
        var pane = cut.Find(".bit-blow");

        // First gesture: dropped on b, so the confirmation starts.
        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 0, 0));
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 50, ClientY = 50 });
        pane.PointerUp(new PointerEventArgs { PointerId = 1, ClientX = 75, ClientY = 200 });

        // Second gesture, by the same mouse, while the first is still being confirmed.
        await cut.InvokeAsync(() => cut.Instance.StartConnection("c", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 0, 0));
        var releasesBefore = JSInterop.Invocations.Count(i => i.Identifier == "releasePointerCapture");

        await cut.InvokeAsync(() => confirmation.SetResult(true));

        Assert.IsTrue(cut.Instance.IsConnecting, "the confirmation ended the next gesture");
        Assert.AreEqual(releasesBefore, JSInterop.Invocations.Count(i => i.Identifier == "releasePointerCapture"),
            "the confirmation released the pointer the next gesture is holding");
    }

    [TestMethod]
    public async Task ADropIsJudgedAgainstTheGraphAsItIsWhenTheUserLetsGo()
    {
        var edges = new List<BlowEdge>();
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render([new() { Id = "a" }, new() { Id = "b" }], edges, p => p
            .Add(c => c.PreventCycles, true)
            .Add(c => c.OnConnectEnd, e => ends.Add(e)));

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));
        // Mid-drag, a collaborator connects b to a.
        edges.Add(new BlowEdge { Id = "remote", Source = "b", Target = "a" });
        await cut.InvokeAsync(() => cut.Instance.CompleteConnection("b", null, BlowHandleType.Target));

        Assert.HasCount(1, edges, "the drop closed a cycle that appeared during the drag");
        Assert.AreEqual(BlowConnectionRejection.Cycle, ends.Single().Rejection);
    }

    [TestMethod]
    public async Task AReconnectedEdgeCanBeDroppedBackOntoItsOwnFullHandle()
    {
        var edge = new BlowEdge { Id = "e", Source = "a", Target = "b" };
        var edges = new List<BlowEdge> { edge };
        var cut = Render([new() { Id = "a" }, new() { Id = "b", Position = new(0, 200), Ports = [In(max: 1)] }], edges,
            p => p.Add(c => c.OnConnectEnd, _ => { }));

        // Select the edge so its reconnect anchors are drawn, and grab the target end.
        cut.Find(".bit-blow-edge").PointerDown(new PointerEventArgs { Button = 0 });
        cut.FindAll(".bit-blow-edge-reconnect-hit")[1].PointerDown(new PointerEventArgs { Button = 0, PointerId = 1 });

        var target = cut.Find(".bit-blow-node[data-id='b'] .bit-blow-handle.target");
        Assert.IsNull(target.GetAttribute("data-connectable-end"), "the handle the edge came off refuses it back");
        Assert.Contains("bit-blow-handle-valid", target.ClassName!);

        await cut.InvokeAsync(() => cut.Instance.CompleteConnection("b", null, BlowHandleType.Target));

        Assert.HasCount(1, edges);
        Assert.AreEqual("b", edge.Target);
    }

    [TestMethod]
    public async Task LooseModeCountsAHandlesEdgesAtEitherEnd()
    {
        // b's handle "x" is a source with room for one edge, and that edge already arrives at it as a target end.
        var nodes = new List<BlowNode>
        {
            new() { Id = "a" },
            new() { Id = "b", Ports = [new BlowPort { Id = "x", Type = BlowHandleType.Source, MaxConnections = 1 }] },
            new() { Id = "c" },
        };
        var edges = new List<BlowEdge> { new() { Id = "in", Source = "a", Target = "b", TargetHandle = "x" } };
        var cut = Render(nodes, edges, p => p.Add(c => c.ConnectionMode, BlowConnectionMode.Loose));

        await cut.InvokeAsync(() => cut.Instance.StartConnection("c", null, BlowHandleType.Source, BlowPosition.Bottom));

        Assert.AreEqual(BlowConnectionRejection.TargetFull, cut.Instance.CheckConnection("b", "x", BlowHandleType.Source).Rejection);
        Assert.IsTrue(cut.Instance.IsHandleAtLimit("b", "x", BlowHandleType.Source));
    }

    [TestMethod]
    public void LooseModeEdgesNamingAPortOfTheOtherTypeAreNotReportedAsUnknown()
    {
        // Loose mode lets a target handle start an edge, which stores it as the edge's source end.
        var nodes = new List<BlowNode>
        {
            new() { Id = "b", Ports = [In("in", type: "number")] },
            new() { Id = "c", Ports = [In("in", type: "number")] },
        };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "b", SourceHandle = "in", Target = "c", TargetHandle = "in" } };

        Assert.IsTrue(BlowGraphValidator.CreateDefault(BlowConnectionMode.Loose).Validate(nodes, edges).IsValid);
        Assert.IsFalse(BlowGraphValidator.CreateDefault().Validate(nodes, edges).IsValid, "strict mode should still flag it");
    }

    [TestMethod]
    public async Task HoveringAHandleThatRefusesReportsWhyOnTheConnectionLine()
    {
        var lines = new List<BlowConnectionLineContext?>();
        var edges = new List<BlowEdge> { new() { Id = "full", Source = "c", Target = "b" } };
        var cut = Render([new() { Id = "a" }, new() { Id = "b", Ports = [In(max: 1)] }, new() { Id = "c" }], edges, p => p
            .Add(c => c.ConnectionRadius, 0)
            .Add(c => c.OnConnectionChange, l => lines.Add(l)));

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));
        cut.Find(".bit-blow-node[data-id='b'] .bit-blow-handle.target").PointerEnter(new PointerEventArgs());

        Assert.IsFalse(lines[^1]!.IsValid);
        Assert.AreEqual(BlowConnectionRejection.TargetFull, lines[^1]!.Rejection);
    }
}
