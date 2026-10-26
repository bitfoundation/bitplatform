using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Moving an end of an existing edge without a pointer: the connection key on a focused edge picks up its target end
/// (pressed again, its source end), focus aims it, Enter attaches it and Escape leaves the edge as it was - judged by
/// the same rules and raising the same events as dragging the end would.
/// </summary>
[TestClass]
public class KeyboardReconnectionTests : BunitContext
{
    public KeyboardReconnectionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitJSModuleInterop Module() => JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge> edges, BlowMeasuredNode[] measured,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
    {
        Module().Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(measured);
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

    private static AngleSharp.Dom.IElement NodeElement(IRenderedComponent<BlowCanvas> cut, string id)
        => cut.Find($".bit-blow-node[data-id='{id}']");

    private static AngleSharp.Dom.IElement EdgeElement(IRenderedComponent<BlowCanvas> cut, string id)
        => cut.Find($".bit-blow-edge[data-id='{id}']");

    private static string Heard(IRenderedComponent<BlowCanvas> cut)
        => cut.Find(".bit-blow-a11y-live").TextContent.TrimEnd('​');

    private static Task NodeKey(IRenderedComponent<BlowCanvas> cut, string nodeId, string key)
        => NodeElement(cut, nodeId).KeyDownAsync(new KeyboardEventArgs { Key = key });

    private static Task EdgeKey(IRenderedComponent<BlowCanvas> cut, string edgeId, string key, bool ctrl = false)
        => EdgeElement(cut, edgeId).KeyDownAsync(new KeyboardEventArgs { Key = key, CtrlKey = ctrl });

    private static Task Focus(IRenderedComponent<BlowCanvas> cut, string nodeId)
        => NodeElement(cut, nodeId).FocusAsync(new FocusEventArgs());

    /// <summary>a -> b, with c beside b and d beside a.</summary>
    private static (List<BlowNode> Nodes, List<BlowEdge> Edges, BlowMeasuredNode[] Measured) ThreeTargets() =>
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

    [TestMethod]
    public async Task TheConnectionKeyOnAnEdgeFocusAndEnterMoveItsTargetEnd()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var reconnects = new List<BlowEdgeReconnect>();
        var starts = new List<BlowEdgeReconnectStart>();
        var ends = new List<BlowEdgeReconnectEnd>();
        var cut = Render(nodes, edges, measured, p => p
            .Add(c => c.OnReconnect, r => reconnects.Add(r))
            .Add(c => c.OnReconnectStart, s => starts.Add(s))
            .Add(c => c.OnReconnectEnd, e => ends.Add(e)));
        var edge = edges.Single();

        await EdgeKey(cut, "e", "c");
        Assert.IsTrue(cut.Instance.IsConnecting);
        Assert.AreEqual(
            "Moving the end of Edge from Source to Sink at Sink. Move to another node and press Enter to attach it there, "
            + "C to move its other end instead, or Escape to leave it as it was.", Heard(cut));
        Assert.AreEqual(new BlowEdgeReconnectStart(edge, BlowHandleType.Target), starts.Single());
        Assert.HasCount(1, cut.FindAll(".bit-blow-connectionline"), "nothing shows the end being moved");

        await Focus(cut, "c");
        Assert.AreEqual("Press Enter to connect to Other sink.", Heard(cut));
        await NodeKey(cut, "c", "Enter");

        Assert.AreSame(edge, edges.Single(), "the edge was replaced rather than rewired");
        Assert.AreEqual(("e", "a", "c"), (edge.Id, edge.Source, edge.Target));
        Assert.AreEqual("Reconnected Source to Other sink", Heard(cut));
        Assert.AreEqual(edge, reconnects.Single().Edge);
        Assert.AreEqual(new BlowEdgeReconnectEnd(edge, BlowHandleType.Target, Rewired: true), ends.Single());
        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.IsFalse(cut.Instance.GetNode("c")!.Selected, "Enter selected the node instead of attaching the end");
    }

    [TestMethod]
    public async Task PressingTheKeyAgainMovesTheSourceEndInstead()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var ends = new List<BlowEdgeReconnectEnd>();
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.OnReconnectEnd, e => ends.Add(e)));

        await EdgeKey(cut, "e", "c");
        await EdgeKey(cut, "e", "c");
        StringAssert.StartsWith(Heard(cut), "Moving the end of Edge from Source to Sink at Source.");

        // Stepping from one end to the other ends the first, as letting go of a drag would.
        Assert.AreEqual(new BlowEdgeReconnectEnd(edges[0], BlowHandleType.Target, Rewired: false), ends.Single());

        await Focus(cut, "d");
        await NodeKey(cut, "d", "Enter");
        Assert.AreEqual(("d", "b"), (edges[0].Source, edges[0].Target));

        // And a third press goes back to the target end.
        await EdgeKey(cut, "e", "c");
        await EdgeKey(cut, "e", "c");
        await EdgeKey(cut, "e", "c");
        StringAssert.StartsWith(Heard(cut), "Moving the end of Edge from Other source to Sink at Sink.");
    }

    [TestMethod]
    public async Task EscapeLeavesTheEdgeAsItWasAndSaysSo()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var ends = new List<BlowEdgeReconnectEnd>();
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.OnReconnectEnd, e => ends.Add(e)));

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "c");
        await cut.Find(".bit-blow").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual(("a", "b"), (edges[0].Source, edges[0].Target));
        Assert.AreEqual("Reconnection cancelled. The edge is unchanged.", Heard(cut));
        Assert.IsFalse(ends.Single().Rewired);
        Assert.IsEmpty(cut.FindAll(".bit-blow-connectionline"));
    }

    [TestMethod]
    public async Task APressOnThePaneAbandonsItAndSaysSo()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var cut = Render(nodes, edges, measured);

        await EdgeKey(cut, "e", "c");
        await cut.Find(".bit-blow").PointerDownAsync(new PointerEventArgs { Button = 0, PointerId = 1, PointerType = "mouse", ClientX = 600, ClientY = 600 });

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual("Reconnection cancelled. The edge is unchanged.", Heard(cut));
    }

    [TestMethod]
    public async Task TheRulesOfAConnectionApply()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "n", Label = "Number", Ports = [new BlowPort { Id = "value", Type = BlowHandleType.Source, DataType = "number" }] },
            new() { Id = "s", Label = "Sum", Position = new(0, 200), Ports = [new BlowPort { Id = "in", Type = BlowHandleType.Target, DataType = "number" }] },
            new() { Id = "t", Label = "Text", Position = new(300, 200), Ports = [new BlowPort { Id = "in", Type = BlowHandleType.Target, DataType = "text" }] },
        };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "n", SourceHandle = "value", Target = "s", TargetHandle = "in" } };
        var cut = Render(nodes, edges, [Measured("n", Out("value")), Measured("s", In("in")), Measured("t", In("in"))]);

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "t");
        Assert.AreEqual(BlowAriaLabels.Default.ConnectionRejected(BlowConnectionRejection.IncompatibleTypes), Heard(cut));

        await NodeKey(cut, "t", "Enter");
        Assert.AreEqual("s", edges[0].Target, "an incompatible end was attached");
        Assert.IsTrue(cut.Instance.IsConnecting, "a refused target ended the reconnection");
    }

    [TestMethod]
    public async Task TheEndCanGoBackToAFullPortItAlreadyHolds()
    {
        // The input takes one connection, and this edge is it: moving the edge's end does not count it twice.
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Label = "Source", Ports = [new BlowPort { Id = "out", Type = BlowHandleType.Source }] },
            new()
            {
                Id = "b", Label = "Sink", Position = new(0, 200),
                Ports = [new BlowPort { Id = "x", Type = BlowHandleType.Target, MaxConnections = 1, Label = "First" },
                    new BlowPort { Id = "y", Type = BlowHandleType.Target, Label = "Second" }],
            },
        };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", SourceHandle = "out", Target = "b", TargetHandle = "y" } };
        var cut = Render(nodes, edges, [Measured("a", Out("out")), Measured("b", In("x", 40), In("y", 110))]);

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "b");
        Assert.AreEqual("Press Enter to connect to Sink, First.", Heard(cut));
        await NodeKey(cut, "b", "Enter");
        Assert.AreEqual("x", edges[0].TargetHandle);

        // Now it holds the full one; it can be moved off it and back on.
        await EdgeKey(cut, "e", "c");
        await Focus(cut, "b");
        Assert.AreEqual("Press Enter to connect to Sink, First.", Heard(cut));
        await NodeKey(cut, "b", "c");
        Assert.AreEqual("Press Enter to connect to Sink, Second.", Heard(cut));
        await NodeKey(cut, "b", "Enter");
        Assert.AreEqual("y", edges[0].TargetHandle);
    }

    [TestMethod]
    public async Task TheKeyOnTheNodeThatStaysDoesNotStartANewConnection()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var cut = Render(nodes, edges, measured);

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "c");
        await NodeKey(cut, "a", "c");

        Assert.IsTrue(cut.Instance.IsConnecting);
        await Focus(cut, "c");
        await NodeKey(cut, "c", "Enter");
        Assert.HasCount(1, edges, "a new connection was made instead of the edge being moved");
        Assert.AreEqual(("a", "c"), (edges[0].Source, edges[0].Target));
    }

    [TestMethod]
    public async Task AnEdgeThatCannotBeReconnectedSaysSo()
    {
        var (nodes, edges, measured) = ThreeTargets();
        edges[0].Reconnectable = false;
        var cut = Render(nodes, edges, measured);

        await EdgeKey(cut, "e", "c");

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual("Edge from Source to Sink cannot be reconnected.", Heard(cut));
    }

    [TestMethod]
    public async Task WithReconnectingOffTheKeyDoesNothingAndTheInstructionsDoNotOfferIt()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.EdgesReconnectable, false));

        await EdgeKey(cut, "e", "c");
        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual(BlowAriaLabels.Default.EdgeInstructions, InstructionsOf(cut, EdgeElement(cut, "e")));

        var on = Render(nodes, edges, measured);
        Assert.AreEqual($"{BlowAriaLabels.Default.EdgeInstructions} {BlowAriaLabels.Default.KeyboardReconnectionInstructions("C")}",
            InstructionsOf(on, EdgeElement(on, "e")));

        var noKey = Render(nodes, edges, measured, p => p.Add(c => c.ConnectionKey, null));
        await EdgeKey(noKey, "e", "c");
        Assert.IsFalse(noKey.Instance.IsConnecting);
        Assert.AreEqual(BlowAriaLabels.Default.EdgeInstructions, InstructionsOf(noKey, EdgeElement(noKey, "e")));
    }

    [TestMethod]
    public async Task WithAModifierHeldNothingIsPickedUp()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var cut = Render(nodes, edges, measured);

        await EdgeKey(cut, "e", "c", ctrl: true);
        Assert.IsFalse(cut.Instance.IsConnecting, "Ctrl+C picked up an end");
    }

    [TestMethod]
    public async Task AnEdgeDeletedMeanwhileEndsItWithTheReason()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var cut = Render(nodes, edges, measured);

        await EdgeKey(cut, "e", "c");
        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync(edges: [edges[0]]));
        await Focus(cut, "c");

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual(BlowAriaLabels.Default.ConnectionRejected(BlowConnectionRejection.MissingNode), Heard(cut));
        Assert.IsEmpty(edges);
    }

    [TestMethod]
    public async Task OneUndoPutsTheEndBack()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.HistoryEnabled, true));

        await EdgeKey(cut, "e", "c");
        await Focus(cut, "c");
        await NodeKey(cut, "c", "Enter");
        Assert.AreEqual("c", edges[0].Target);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.AreEqual(("a", "b"), (edges.Single().Source, edges.Single().Target));
    }

    [TestMethod]
    public async Task AConnectionFromANodeEndsWhenAnEdgeIsPickedUp()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var connectEnds = new List<BlowConnectEndInfo>();
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.OnConnectEnd, e => connectEnds.Add(e)));

        await NodeKey(cut, "d", "c");
        await EdgeKey(cut, "e", "c");

        Assert.IsFalse(connectEnds.Single().IsReconnect, "the connection from the node did not end first");
        StringAssert.StartsWith(Heard(cut), "Moving the end of Edge from Source to Sink at Sink.");

        await Focus(cut, "c");
        await NodeKey(cut, "c", "Enter");
        Assert.HasCount(1, edges);
        Assert.AreEqual(("a", "c"), (edges[0].Source, edges[0].Target));
    }

    [TestMethod]
    public async Task TheStringsAreLocalizable()
    {
        var (nodes, edges, measured) = ThreeTargets();
        var labels = new BlowAriaLabels
        {
            KeyboardReconnectionStarted = (edge, end, key) => $"{edge} bei {end} gelöst ({key}).",
            KeyboardReconnectionCancelled = "Abgebrochen.",
            Reconnected = (s, t) => $"{s} jetzt mit {t}.",
            EdgeLabel = (s, t) => $"{s} nach {t}",
        };
        var cut = Render(nodes, edges, measured, p => p.Add(c => c.AriaLabels, labels));

        await EdgeKey(cut, "e", "c");
        Assert.AreEqual("Source nach Sink bei Sink gelöst (C).", Heard(cut));
        await Focus(cut, "c");
        await NodeKey(cut, "c", "Enter");
        Assert.AreEqual("Source jetzt mit Other sink.", Heard(cut));

        await EdgeKey(cut, "e", "c");
        await cut.Find(".bit-blow").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        Assert.AreEqual("Abgebrochen.", Heard(cut));
    }

    /// <summary>The keyboard instructions alone, among what the element is described by.</summary>
    private static string InstructionsOf(IRenderedComponent<BlowCanvas> cut, AngleSharp.Dom.IElement element)
    {
        var ids = element.GetAttribute("aria-describedby");
        Assert.IsNotNull(ids, "the element is not described by anything");
        return cut.Find($"[id='{ids.Split(' ').Single(id => id.Contains("-help-"))}']").TextContent;
    }
}
