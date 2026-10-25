using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// What the review of keyboard reconnection found, each pinned: how an abandoned or refused connection reports its
/// end, a graph that changes while an end waits, the pointer neither steering nor completing what the keyboard holds,
/// and the end that stays put staying where it is now. Most of it holds for a keyboard connection from a node too.
/// </summary>
[TestClass]
public class KeyboardReconnectionReviewTests : BunitContext
{
    public KeyboardReconnectionReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge> edges, BlowMeasuredNode[] measured,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
    {
        JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js").Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(measured);
        return Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges);
            extra?.Invoke(p);
        });
    }

    private static BlowMeasuredNode Measured(string id, params BlowMeasuredHandle[] handles) => new(id, 150, 40, handles);

    private static BlowMeasuredHandle Out(string id = "", double x = 75) => new(id, "source", "bottom", x, 40);

    private static BlowMeasuredHandle In(string id = "", double x = 75) => new(id, "target", "top", x, 0);

    private static string Heard(IRenderedComponent<BlowCanvas> cut) => cut.Find(".bit-blow-a11y-live").TextContent.TrimEnd('​');

    private static Task NodeKey(IRenderedComponent<BlowCanvas> cut, string id, string key)
        => cut.Find($".bit-blow-node[data-id='{id}']").KeyDownAsync(new KeyboardEventArgs { Key = key });

    private static Task EdgeKey(IRenderedComponent<BlowCanvas> cut, string id, string key)
        => cut.Find($".bit-blow-edge[data-id='{id}']").KeyDownAsync(new KeyboardEventArgs { Key = key });

    private static Task Focus(IRenderedComponent<BlowCanvas> cut, string id)
        => cut.Find($".bit-blow-node[data-id='{id}']").FocusAsync(new FocusEventArgs());

    private static Task Escape(IRenderedComponent<BlowCanvas> cut)
        => cut.Find(".bit-blow").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

    private static PointerEventArgs Press => new() { Button = 0, PointerId = 1, PointerType = "mouse" };

    /// <summary>a -> b, with c an input beside b and d an output beside a.</summary>
    private static (List<BlowNode> Nodes, List<BlowEdge> Edges, BlowMeasuredNode[] Measured) Graph() =>
    (
        [
            new() { Id = "a", Label = "Source" },
            new() { Id = "b", Label = "Sink", Position = new(0, 200) },
            new() { Id = "c", Label = "Other sink", Position = new(300, 200) },
            new() { Id = "d", Label = "Other source", Position = new(300, 0) },
        ],
        [new() { Id = "e", Source = "a", Target = "b" }],
        [Measured("a", Out()), Measured("b", In()), Measured("c", In()), Measured("d", Out())]
    );

    /// <summary>The same with c's only input taking one connection, and f holding it.</summary>
    private static (List<BlowNode> Nodes, List<BlowEdge> Edges, BlowMeasuredNode[] Measured) FullInput()
    {
        var (nodes, _, _) = Graph();
        nodes[2].Ports = [new BlowPort { Id = "in", Type = BlowHandleType.Target, MaxConnections = 1 }];
        return (nodes,
            [new() { Id = "e", Source = "a", Target = "b" }, new() { Id = "f", Source = "d", Target = "c", TargetHandle = "in" }],
            [Measured("a", Out()), Measured("b", In()), Measured("c", In("in")), Measured("d", Out())]);
    }

    // ---- how it ends ----

    [TestMethod]
    [DataRow("escape")]
    [DataRow("key-again")]
    [DataRow("pane")]
    public async Task AnAbandonedReconnectEndsInvalidWhereverItWasAimed(string how)
    {
        var (nodes, edges, measured) = Graph();
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.OnConnectEnd, e => ends.Add(e)));

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "c");
        switch (how)
        {
            case "escape": await Escape(cut); break;
            case "key-again": await EdgeKey(cut, "e", "c"); break;
            default: await cut.Find(".bit-blow").PointerDownAsync(new PointerEventArgs { Button = 0, PointerId = 1, PointerType = "mouse", ClientX = 600, ClientY = 600 }); break;
        }

        var end = ends.First();
        Assert.IsFalse(end.IsValid, $"{how}: an abandoned reconnect was reported as made");
        Assert.AreEqual(BlowConnectionRejection.None, end.Rejection);
        Assert.AreEqual("b", edges[0].Target);
    }

    [TestMethod]
    public async Task AnAbandonedConnectionFromANodeEndsInvalidToo()
    {
        var (nodes, _, measured) = Graph();
        var edges = new List<BlowEdge>();
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.OnConnectEnd, e => ends.Add(e)));

        await NodeKey(cut, "a", "c");
        await Focus(cut, "c");
        await Escape(cut);

        Assert.IsFalse(ends.Single().IsValid);
        Assert.IsEmpty(edges);
    }

    [TestMethod]
    public async Task APortThatFilledUpWhileAimedAtSaysSoOnEnterAndTheEndWaits()
    {
        var (nodes, _, _) = Graph();
        nodes[2].Ports = [new BlowPort { Id = "in", Type = BlowHandleType.Target, MaxConnections = 1 }];
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var cut = Render(nodes, edges, [Measured("a", Out()), Measured("b", In()), Measured("c", In("in")), Measured("d", Out())]);

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "c");
        await cut.InvokeAsync(() => cut.Instance.EditAsync(() => edges.Add(new BlowEdge { Id = "f", Source = "d", Target = "c", TargetHandle = "in" })));
        await NodeKey(cut, "c", "Enter");

        Assert.AreEqual(BlowAriaLabels.Default.ConnectionRejected(BlowConnectionRejection.TargetFull), Heard(cut));
        Assert.IsTrue(cut.Instance.IsConnecting, "a refused target ended the reconnect");
        Assert.AreEqual("b", edges[0].Target);
    }

    [TestMethod]
    public async Task ARuleThatRefusesByTheTimeEnterIsPressedSaysWhy()
    {
        // Enter judges the connection as of then: a rule that has changed its mind is reported, not swallowed.
        var (nodes, edges, measured) = Graph();
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render(nodes, edges, measured, p => p
            .Add(c => c.OnConnectEnd, e => ends.Add(e))
            .Add(c => c.IsValidConnection, _ => !blocked));

        await NodeKey(cut, "d", "c");
        await Focus(cut, "c");
        blocked = true;
        await NodeKey(cut, "c", "Enter");

        Assert.AreEqual(BlowAriaLabels.Default.ConnectionRejected(BlowConnectionRejection.RejectedByRule), Heard(cut));
        Assert.HasCount(1, edges);
        Assert.IsTrue(cut.Instance.IsConnecting);
    }

    private bool blocked;

    // ---- the graph moving on while the end waits ----

    [TestMethod]
    public async Task ANodeAddedAfterThePickUpCanBeAimedAt()
    {
        var (nodes, edges, measured) = Graph();
        var cut = Render(nodes, edges, [.. measured, Measured("x", In())]);

        await EdgeKey(cut, "e", "c");
        nodes.Add(new BlowNode { Id = "x", Label = "New sink", Position = new(600, 200) });
        cut.Render(p => p.Add(c => c.Nodes, nodes));
        await Focus(cut, "x");
        Assert.AreEqual("Press Enter to connect to New sink.", Heard(cut));

        await NodeKey(cut, "x", "Enter");
        Assert.AreEqual("x", edges[0].Target);
    }

    [TestMethod]
    public async Task APortFreedAfterThePickUpCanBeAimedAt()
    {
        var (nodes, edges, measured) = FullInput();
        var cut = Render(nodes, edges, measured);

        await EdgeKey(cut, "e", "c");
        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync(edges: [edges[1]]));
        await Focus(cut, "c");

        Assert.AreEqual("Press Enter to connect to Other sink, in.", Heard(cut));
    }

    [TestMethod]
    public async Task TheEndThatStaysIsWhereverItIsNow()
    {
        var (nodes, edges, measured) = Graph();
        var cut = Render(nodes, edges, measured);

        await EdgeKey(cut, "e", "c");
        await cut.InvokeAsync(() => cut.Instance.EditAsync(() => edges[0].Source = "d"));
        await Focus(cut, "c");
        await NodeKey(cut, "c", "Enter");

        Assert.AreEqual(("d", "c"), (edges[0].Source, edges[0].Target), "the source the app set was put back");
    }

    [TestMethod]
    public async Task AConfirmedMoveIsNotUndoneByMovingTheOtherEnd()
    {
        var (nodes, edges, measured) = Graph();
        var pending = new Queue<TaskCompletionSource<bool>>();
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.OnBeforeConnect, _ =>
        {
            var answer = new TaskCompletionSource<bool>();
            pending.Enqueue(answer);
            return answer.Task;
        }));

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "c");
        await NodeKey(cut, "c", "Enter");   // target to c, waiting for the app
        await EdgeKey(cut, "e", "c");
        await EdgeKey(cut, "e", "c");       // the source end, picked up while the target was still b
        await cut.InvokeAsync(() => pending.Dequeue().SetResult(true));
        Assert.AreEqual(("a", "c"), (edges[0].Source, edges[0].Target));

        await Focus(cut, "d");
        await NodeKey(cut, "d", "Enter");
        await cut.InvokeAsync(() => pending.Dequeue().SetResult(true));

        Assert.AreEqual(("d", "c"), (edges[0].Source, edges[0].Target), "moving the source put the target back on b");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ReconnectingTurnedOffMeanwhileIsRespected(bool onTheCanvas)
    {
        var (nodes, edges, measured) = Graph();
        var cut = Render(nodes, edges, measured);

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "c");
        if (onTheCanvas) cut.Render(p => p.Add(c => c.EdgesReconnectable, false));
        else edges[0].Reconnectable = false;
        await NodeKey(cut, "c", "Enter");

        Assert.AreEqual("b", edges[0].Target);
        Assert.AreEqual(BlowAriaLabels.Default.ConnectionRejected(BlowConnectionRejection.NotConnectable), Heard(cut));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task DeletingThePickedUpEdgeEndsIt(bool fromTheKeyboard)
    {
        var (nodes, edges, measured) = Graph();
        var ends = new List<BlowEdgeReconnectEnd>();
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.OnReconnectEnd, e => ends.Add(e)));

        await EdgeKey(cut, "e", "Enter"); // selected, so Delete takes it
        await EdgeKey(cut, "e", "c");
        if (fromTheKeyboard) await EdgeKey(cut, "e", "Delete");
        else await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync(edges: [edges[0]]));

        Assert.IsEmpty(edges);
        Assert.IsFalse(cut.Instance.IsConnecting, "the reconnect of a deleted edge carried on");
        Assert.IsEmpty(cut.FindAll(".bit-blow-connectionline"));
        Assert.IsFalse(ends.Single().Rewired);
        if (fromTheKeyboard) Assert.AreEqual(BlowAriaLabels.Default.Deleted(0, 1), Heard(cut));
    }

    // ---- the pointer ----

    [TestMethod]
    public async Task APointerOverAHandleDoesNotSteerIt()
    {
        var (nodes, edges, _) = Graph();
        var cut = Render(nodes, edges, [Measured("a", Out()), Measured("b", In()), Measured("c", In(), Out()), Measured("d", Out())]);

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "c");
        await cut.Find(".bit-blow-node[data-id='c'] .bit-blow-handle.source").TriggerEventAsync("onpointerenter", new PointerEventArgs());
        await NodeKey(cut, "c", "Enter");

        Assert.AreEqual(("a", "c"), (edges[0].Source, edges[0].Target), "Enter did not attach where it was announced");
    }

    [TestMethod]
    [DataRow("cannot-start")]
    [DataRow("connect-on-click")]
    [DataRow("full")]
    public async Task APressOnAHandleAbandonsIt(string handle)
    {
        var (nodes, edges, measured) = handle == "full" ? FullInput() : Graph();
        var cut = Render(nodes, edges, measured, p =>
        {
            if (handle == "connect-on-click") p.Add(c => c.ConnectOnClick, true);
            if (handle == "cannot-start")
            {
                p.Add(c => c.NodeTemplate, (RenderFragment<BlowNode>)(node => b =>
                {
                    b.OpenComponent<BlowHandle>(0);
                    b.AddAttribute(1, nameof(BlowHandle.Type), node.Id is "a" or "d" ? BlowHandleType.Source : BlowHandleType.Target);
                    b.AddAttribute(2, nameof(BlowHandle.IsConnectableStart), false);
                    b.CloseComponent();
                }));
            }
        });

        await EdgeKey(cut, "e", "c");
        await cut.Find(".bit-blow-node[data-id='c'] .bit-blow-handle").PointerDownAsync(Press);
        Assert.AreEqual("Reconnection cancelled. The edge is unchanged.", Heard(cut));
        await cut.Find(".bit-blow-node[data-id='c'] .bit-blow-handle").PointerUpAsync(Press);

        Assert.AreEqual(("a", "b"), (edges[0].Source, edges[0].Target), $"{handle}: a press attached the end the keyboard held");
    }

    [TestMethod]
    public async Task APressOnADragHandleAbandonsIt()
    {
        var (nodes, edges, measured) = Graph();
        foreach (var n in nodes) n.UseCustomDragHandle = true;
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.NodeTemplate, (RenderFragment<BlowNode>)(node => b =>
        {
            b.OpenComponent<BlowDragHandle>(0);
            b.CloseComponent();
            b.OpenComponent<BlowHandle>(1);
            b.AddAttribute(2, nameof(BlowHandle.Type), node.Id is "a" or "d" ? BlowHandleType.Source : BlowHandleType.Target);
            b.CloseComponent();
        })));

        await EdgeKey(cut, "e", "c");
        await cut.Find(".bit-blow-node[data-id='c'] .bit-blow-drag-handle")
            .PointerDownAsync(new PointerEventArgs { Button = 0, PointerId = 1, PointerType = "mouse", ClientX = 10, ClientY = 10 });

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual("Reconnection cancelled. The edge is unchanged.", Heard(cut));
    }

    [TestMethod]
    public async Task TheBrowserTakingAPointerOverLeavesItAlone()
    {
        var (nodes, edges, measured) = Graph();
        var cut = Render(nodes, edges, measured);

        await EdgeKey(cut, "e", "c");
        var heard = Heard(cut);
        await cut.Find(".bit-blow").TriggerEventAsync("onpointercancel", new PointerEventArgs { PointerId = 7 });

        Assert.IsTrue(cut.Instance.IsConnecting, "a pointercancel ended what the keyboard was doing");
        Assert.AreEqual(heard, Heard(cut));
        await Focus(cut, "c");
        await NodeKey(cut, "c", "Enter");
        Assert.AreEqual("c", edges[0].Target);
    }

    // ---- the node it starts from ----

    [TestMethod]
    public async Task EnterOnTheNodeItStartsFromAimsBeforeItLoopsBack()
    {
        var (nodes, edges, _) = Graph();
        var cut = Render(nodes, edges, [Measured("a", Out(), In()), Measured("b", In()), Measured("c", In()), Measured("d", Out())],
            p => p.Add(c => c.AllowSelfConnections, true));

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "a");
        await NodeKey(cut, "a", "Enter");
        Assert.AreEqual(("a", "b"), (edges[0].Source, edges[0].Target), "one key press looped the edge back onto its source");
        Assert.AreEqual("Press Enter to connect to Source.", Heard(cut));

        await NodeKey(cut, "a", "Enter");
        Assert.AreEqual(("a", "a"), (edges[0].Source, edges[0].Target));
    }
}
