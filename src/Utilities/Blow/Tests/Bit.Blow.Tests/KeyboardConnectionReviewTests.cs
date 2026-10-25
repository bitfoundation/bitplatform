using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Regressions found by the independent review of the keyboard and validation work: the pointer and a connection
/// made from the keyboard, a late key press, stale state between gestures, and the minimap's keys.
/// </summary>
[TestClass]
public class KeyboardConnectionReviewTests : BunitContext
{
    public KeyboardConnectionReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitJSModuleInterop Module() => JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");

    private static BlowMeasuredNode Measured(string id, params BlowMeasuredHandle[] handles) => new(id, 150, 40, handles);

    private static BlowMeasuredHandle Out() => new("", "source", "bottom", 75, 40);

    private static BlowMeasuredHandle In() => new("", "target", "top", 75, 0);

    private static List<BlowNode> AToB() =>
    [
        new() { Id = "a", Label = "Source" },
        new() { Id = "b", Label = "Sink", Position = new(0, 200) },
    ];

    private IRenderedComponent<BlowCanvas> Render(List<BlowNode> nodes, List<BlowEdge> edges,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null, BunitJSModuleInterop? module = null)
    {
        (module ?? Module()).Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult([Measured("a", Out()), Measured("b", In())]);
        return Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges);
            extra?.Invoke(p);
        });
    }

    private static AngleSharp.Dom.IElement Node(IRenderedComponent<BlowCanvas> cut, string id) => cut.Find($".bit-blow-node[data-id='{id}']");

    private static string Heard(IRenderedComponent<BlowCanvas> cut) => cut.Find(".bit-blow-a11y-live").TextContent.TrimEnd('​');

    private static async Task Aim(IRenderedComponent<BlowCanvas> cut)
    {
        await Node(cut, "a").KeyDownAsync(new KeyboardEventArgs { Key = "c" });
        await Node(cut, "b").FocusAsync(new FocusEventArgs());
        Assert.IsTrue(cut.Instance.IsConnecting);
    }

    [TestMethod]
    public async Task TheMouseLeavingTheCanvasDoesNotEndAKeyboardConnection()
    {
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render(AToB(), [], p => p.Add(c => c.OnConnectEnd, e => ends.Add(e)));
        await Aim(cut);

        await cut.Find(".bit-blow").PointerLeaveAsync(new PointerEventArgs { PointerId = 1, PointerType = "mouse", ClientX = 900, ClientY = 900 });

        Assert.IsTrue(cut.Instance.IsConnecting);
        Assert.IsEmpty(ends);
    }

    [TestMethod]
    public async Task AHoveringMouseDoesNotSteerAKeyboardConnection()
    {
        var cut = Render(AToB(), []);
        await Aim(cut);
        var before = cut.Find(".bit-blow-connectionline").GetAttribute("d");

        // The pane does not even listen to the pointer for it.
        Assert.ThrowsExactly<MissingEventHandlerException>(() =>
            cut.Find(".bit-blow").PointerMove(new PointerEventArgs { PointerId = 1, PointerType = "mouse", ClientX = 700, ClientY = 20 }));
        Assert.AreEqual(before, cut.Find(".bit-blow-connectionline").GetAttribute("d"));
    }

    [TestMethod]
    public async Task APressOnAHandleEndsTheKeyboardConnectionBeforeStartingItsOwn()
    {
        int starts = 0, ends = 0;
        var cut = Render(AToB(), [], p =>
        {
            p.Add(c => c.OnConnectStart, _ => starts++);
            p.Add(c => c.OnConnectEnd, _ => ends++);
        });
        await Node(cut, "a").KeyDownAsync(new KeyboardEventArgs { Key = "c" });

        await cut.Find(".bit-blow-node[data-id='b'] .bit-blow-handle")
            .PointerDownAsync(new PointerEventArgs { Button = 0, PointerId = 1, PointerType = "mouse", ClientX = 75, ClientY = 200 });

        Assert.AreEqual(2, starts);
        Assert.AreEqual(1, ends, "the keyboard connection was never ended");
        Assert.AreEqual("Connection cancelled.", Heard(cut));

        // Enter is a selection again, not the end of the pointer's drag.
        await Node(cut, "b").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        Assert.IsTrue(cut.Instance.GetNode("b")!.Selected);
    }

    [TestMethod]
    public async Task APressOnThePaneEndsTheKeyboardConnectionAndSaysSo()
    {
        var cut = Render(AToB(), []);
        await Aim(cut);

        await cut.Find(".bit-blow").PointerDownAsync(new PointerEventArgs { Button = 0, PointerId = 1, PointerType = "mouse", ClientX = 600, ClientY = 600 });

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual("Connection cancelled.", Heard(cut));
    }

    [TestMethod]
    public async Task ALateEnterOnAnEndedConnectionLeavesNothingBehindForTheNextGesture()
    {
        var module = Module();
        var phase = 0;
        module.Setup<bool>("isEditableFocused", _ => phase == 0).SetResult(false);
        var pending = module.Setup<bool>("isEditableFocused", _ => phase == 1);
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render(AToB(), [], p => p.Add(c => c.OnConnectEnd, e => ends.Add(e)), module);
        await Aim(cut);

        // Enter waits on the browser while a click on the pane ends the connection.
        phase = 1;
        var enter = Node(cut, "b").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        phase = 0;
        await cut.Find(".bit-blow").PointerDownAsync(new PointerEventArgs { Button = 0, PointerId = 1, PointerType = "mouse", ClientX = 600, ClientY = 600 });
        await cut.Find(".bit-blow").PointerUpAsync(new PointerEventArgs { Button = 0, PointerId = 1, PointerType = "mouse", ClientX = 600, ClientY = 600 });
        await cut.InvokeAsync(() => pending.SetResult(false));
        await enter;
        Assert.AreEqual("Connection cancelled.", Heard(cut), "the late Enter acted on a connection that had ended");

        // An ordinary drag from a's handle, released on empty pane, is no refusal.
        ends.Clear();
        await cut.Find(".bit-blow-node[data-id='a'] .bit-blow-handle")
            .PointerDownAsync(new PointerEventArgs { Button = 0, PointerId = 2, PointerType = "mouse", ClientX = 75, ClientY = 40 });
        await cut.Find(".bit-blow").PointerMoveAsync(new PointerEventArgs { PointerId = 2, PointerType = "mouse", ClientX = 800, ClientY = 800, Buttons = 1 });
        await cut.Find(".bit-blow").PointerUpAsync(new PointerEventArgs { Button = 0, PointerId = 2, PointerType = "mouse", ClientX = 800, ClientY = 800 });
        Assert.AreEqual(BlowConnectionRejection.None, ends.Single().Rejection);
    }

    [TestMethod]
    public async Task AKeyboardConnectionWhoseSourceWentEndsWhenAimed()
    {
        var nodes = AToB();
        nodes.Add(new BlowNode { Id = "c", Label = "Other", Position = new(300, 200) });
        var cut = Render(nodes, []);
        await Node(cut, "a").KeyDownAsync(new KeyboardEventArgs { Key = "c" });

        nodes.RemoveAt(0);
        await cut.InvokeAsync(() => cut.Instance.Refresh(remeasure: false));
        await Node(cut, "b").FocusAsync(new FocusEventArgs());

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.AreEqual(BlowAriaLabels.Default.ConnectionRejected(BlowConnectionRejection.MissingNode), Heard(cut));
    }

    [TestMethod]
    public async Task EscapeOnTheMinimapReachesThePaneButItsArrowsDoNot()
    {
        var module = Module();
        module.Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, 0, 800, 600));
        var nodes = AToB();
        nodes[1].Selected = true;
        var cut = Render(nodes, [], p => p.AddChildContent<BlowMiniMap>(), module);
        await Node(cut, "a").KeyDownAsync(new KeyboardEventArgs { Key = "c" });

        // The key bubbles from the minimap to the pane, as in the browser.
        var map = cut.Find(".bit-blow-minimap");
        await map.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.AreEqual(new BlowXYPosition(0, 200), nodes[1].Position, "the minimap's arrow also moved the selection");
        Assert.AreEqual(80, cut.Instance.GetViewport().X, 0.001, "the minimap's arrow did not move the view");

        // A key the minimap does not act on reaches the pane: Escape abandons the connection.
        await cut.Find(".bit-blow-minimap").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        Assert.IsFalse(cut.Instance.IsConnecting);

        // And the pane's own arrows still move the selection afterwards.
        await cut.Find(".bit-blow").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.AreNotEqual(new BlowXYPosition(0, 200), nodes[1].Position);
    }

    [TestMethod]
    public async Task AnEdgeToAHiddenNodeIsNotFocusedSelectedOrFound()
    {
        var focus = Module().Setup<bool>("focusElement", _ => true);
        var nodes = AToB();
        nodes[1].Hidden = true;
        var edges = new List<BlowEdge> { new() { Id = "a-b", Source = "a", Target = "b", Label = "wire" } };
        var cut = Render(nodes, edges);

        Assert.IsFalse(await cut.InvokeAsync(() => cut.Instance.FocusEdgeAsync("a-b", select: true)));
        await cut.InvokeAsync(() => cut.Instance.SelectAsync(edges: edges));
        Assert.IsFalse(edges[0].Selected);
        Assert.IsEmpty(cut.Instance.FindEdges("wire"));
        Assert.IsEmpty(focus.Invocations);
    }
}
