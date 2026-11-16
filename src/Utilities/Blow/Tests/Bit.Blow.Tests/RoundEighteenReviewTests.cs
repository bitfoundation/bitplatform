using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// The round 18 review, of round 17's fixes: each case asserts the documented behaviour and failed on the code before
/// its fix.
/// </summary>
[TestClass]
public class RoundEighteenReviewTests : BunitContext
{
    public RoundEighteenReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitJSModuleInterop Module() => JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");

    private IRenderedComponent<BlowCanvas> Render(
        List<BlowNode> nodes, List<BlowEdge>? edges = null, Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            extra?.Invoke(p);
        });

    private static readonly BlowDomRect AtTop = new(0, 0, 800, 600);

    // ---- the page scrolls the pane ----

    [TestMethod]
    public async Task ADragGrabbedJustAfterThePageScrolledDoesNotJumpByTheScroll()
    {
        // The pane was at the top of the page; the page then scrolled it 200px up, and the node was grabbed before the
        // browser reported where the pane ended up.
        Module().Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, -200, 800, 600));
        var node = new BlowNode { Id = "n", Position = new(10, 10), Width = 20, Height = 20 };
        var cut = Render([node]);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(false, AtTop));
        cut.Instance.OnPaneMoved();

        cut.Find(".bit-blow-node").PointerDown(new PointerEventArgs { Button = 0, ClientX = 20, ClientY = -180 });
        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { Buttons = 1, ClientX = 70, ClientY = -180 });

        Assert.AreEqual(new BlowXYPosition(60, 10), node.Position);
    }

    [TestMethod]
    public async Task WhileThePageGoesOnScrollingEveryMoveReadsThePaneAgain()
    {
        var module = Module();
        module.Setup<BlowDomRect?>("getRect", _ => true).SetResult(AtTop);
        var cut = Render([new BlowNode { Id = "n", Position = new(10, 10), Width = 20, Height = 20 }]);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(false, AtTop));
        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 300, ClientY = 300 });

        cut.Instance.OnPaneMoved();
        var before = Reads();
        pane.PointerMove(new PointerEventArgs { Buttons = 1, ClientX = 310, ClientY = 300 });
        pane.PointerMove(new PointerEventArgs { Buttons = 1, ClientX = 320, ClientY = 300 });
        Assert.AreEqual(before + 2, Reads(), "a reading taken mid-scroll does not end the scroll");

        // The browser reports the rectangle the scrolling stopped at.
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(false, AtTop, pageScrolling: false));
        before = Reads();
        pane.PointerMove(new PointerEventArgs { Buttons = 1, ClientX = 330, ClientY = 300 });
        Assert.AreEqual(before, Reads());

        int Reads() => module.Invocations["getRect"].Count;
    }

    [TestMethod]
    public async Task MovesThatArriveWhileThePaneIsBeingReadShareTheReading()
    {
        // A reading that takes a while to come back, as over a Blazor Server circuit.
        var module = Module();
        var read = module.Setup<BlowDomRect?>("getRect", _ => true);
        var node = new BlowNode { Id = "n", Position = new(10, 10), Width = 20, Height = 20 };
        var cut = Render([node]);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(false, AtTop));
        cut.Instance.OnPaneMoved();

        cut.Find(".bit-blow-node").PointerDown(new PointerEventArgs { Button = 0, ClientX = 20, ClientY = -180 });
        var before = module.Invocations["getRect"].Count;
        var pane = cut.Find(".bit-blow");
        var first = pane.TriggerEventAsync("onpointermove", new PointerEventArgs { Buttons = 1, ClientX = 40, ClientY = -180 });
        var second = pane.TriggerEventAsync("onpointermove", new PointerEventArgs { Buttons = 1, ClientX = 70, ClientY = -180 });

        Assert.AreEqual(before, module.Invocations["getRect"].Count);

        read.SetResult(new BlowDomRect(0, -200, 800, 600));
        await Task.WhenAll(first, second);
        Assert.AreEqual(new BlowXYPosition(60, 10), node.Position);
    }

    [TestMethod]
    public async Task ScreenToFlowPositionAsyncReadsThePaneAgainAfterThePageScrolled()
    {
        Module().Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, -100, 800, 600));
        var cut = Render([new BlowNode { Id = "n" }]);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(false, AtTop));
        cut.Instance.OnPaneMoved();

        var flow = await cut.InvokeAsync(() => cut.Instance.ScreenToFlowPositionAsync(10, -90));
        var screen = await cut.InvokeAsync(() => cut.Instance.FlowToScreenPositionAsync(new BlowXYPosition(10, 10)));

        Assert.AreEqual(new BlowXYPosition(10, 10), flow);
        Assert.AreEqual(new BlowXYPosition(10, -90), screen);
    }

    [TestMethod]
    public async Task APinchKeepsItsPointUnderTheFingersWhenThePaneMovesMidPinch()
    {
        Module().Setup<BlowDomRect?>("getRect", _ => true).SetResult(AtTop);
        var cut = Render([new BlowNode { Id = "n", Position = new(500, 500), Width = 20, Height = 20 }]);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(false, AtTop));
        var pane = cut.Find(".bit-blow");

        // The midpoint starts over flow point (150, 100).
        pane.PointerDown(new PointerEventArgs { PointerId = 1, PointerType = "touch", Button = 0, ClientX = 100, ClientY = 100 });
        pane.PointerDown(new PointerEventArgs { PointerId = 2, PointerType = "touch", Button = 0, ClientX = 200, ClientY = 100 });

        // The page moves the pane 50px up under fingers that stay where they are.
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(false, new BlowDomRect(0, -50, 800, 600)));
        pane.PointerMove(new PointerEventArgs { PointerId = 1, PointerType = "touch", ClientX = 100, ClientY = 100 });

        var under = cut.Instance.GetViewport().ScreenToFlow(150, 150);
        Assert.AreEqual(150, under.X, 1e-6);
        Assert.AreEqual(100, under.Y, 1e-6);
    }

    // ---- nodes nested in a hidden node ----

    [TestMethod]
    public void FindNodesFindsANodeWhoseParentWasShownAgainBeforeTheNextRender()
    {
        var group = new BlowNode { Id = "g", Hidden = true, Width = 200, Height = 200 };
        var nodes = new List<BlowNode> { group, new() { Id = "c", Label = "child", ParentId = "g", Position = new(10, 10) } };
        var cut = Render(nodes);

        group.Hidden = false;

        Assert.ContainsSingle(cut.Instance.FindNodes("child"));
    }

    private static List<BlowNode> SelectedInAndOutOfAHiddenGroup() =>
    [
        new() { Id = "g", Hidden = true, Position = new(0, 0), Width = 300, Height = 300 },
        new() { Id = "c1", ParentId = "g", Position = new(10, 10), Width = 20, Height = 20, Selected = true },
        new() { Id = "c2", ParentId = "g", Position = new(100, 50), Width = 20, Height = 20, Selected = true },
        new() { Id = "v1", Position = new(400, 10), Width = 20, Height = 20, Selected = true },
        new() { Id = "v2", Position = new(500, 80), Width = 20, Height = 20, Selected = true },
    ];

    [TestMethod]
    public async Task AligningTheSelectionLeavesANodeInAHiddenGroupWhereItIs()
    {
        var nodes = SelectedInAndOutOfAHiddenGroup();
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.AlignSelectionAsync(BlowAlignEdge.Top));

        Assert.AreEqual(new BlowXYPosition(100, 50), nodes.Single(n => n.Id == "c2").Position);
        Assert.AreEqual(10, nodes.Single(n => n.Id == "v2").Position.Y);
    }

    [TestMethod]
    public async Task DistributingTheSelectionLeavesANodeInAHiddenGroupWhereItIs()
    {
        var nodes = SelectedInAndOutOfAHiddenGroup();
        nodes.Add(new BlowNode { Id = "v3", Position = new(900, 10), Width = 20, Height = 20, Selected = true });
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.DistributeSelectionAsync(horizontal: true));

        Assert.AreEqual(new BlowXYPosition(10, 10), nodes.Single(n => n.Id == "c1").Position);
        Assert.AreEqual(new BlowXYPosition(100, 50), nodes.Single(n => n.Id == "c2").Position);
    }

    [TestMethod]
    public void InvertingTheSelectionLeavesOutANodeInAHiddenGroup()
    {
        var nodes = SelectedInAndOutOfAHiddenGroup();
        nodes.Add(new BlowNode { Id = "v3" });

        var inverted = BlowSelection.Invert(nodes, nodes.Where(n => n.Id is "v1" or "v2"));

        CollectionAssert.AreEquivalent(new[] { "v3" }, inverted.Select(n => n.Id).ToArray());
    }

    [TestMethod]
    public void TheConnectionRulesRefuseANodeInAHiddenGroup()
    {
        var nodes = SelectedInAndOutOfAHiddenGroup();
        var graph = new BlowGraphIndex(nodes, []);

        Assert.IsFalse(BlowConnectionRules.Begin(graph, "c1", null, BlowHandleType.Source).CanStart);
        Assert.AreEqual(BlowConnectionRejection.NotConnectable,
            BlowConnectionRules.Begin(graph, "v1", null, BlowHandleType.Source).Check("c1", null, BlowHandleType.Target).Rejection);
        Assert.IsTrue(BlowConnectionRules.Begin(graph, "v1", null, BlowHandleType.Source).Check("v2", null, BlowHandleType.Target).IsValid);
    }

    // ---- the right button pans ----

    [TestMethod]
    public void WithTheRightButtonPanningOnPaneContextMenuIsNotRaised()
    {
        var raised = 0;
        var panning = Render([new BlowNode { Id = "n" }], extra: p =>
        {
            p.Add(c => c.PanButtons, new[] { 0, 2 });
            p.Add(c => c.OnPaneContextMenu, (MouseEventArgs _) => raised++);
        });
        var plain = Render([new BlowNode { Id = "n" }], extra: p => p.Add(c => c.OnPaneContextMenu, (MouseEventArgs _) => raised++));

        panning.Find(".bit-blow").ContextMenu(new MouseEventArgs { Button = 2 });
        Assert.AreEqual(0, raised);

        plain.Find(".bit-blow").ContextMenu(new MouseEventArgs { Button = 2 });
        Assert.AreEqual(1, raised);
    }

    // ---- click to connect ----

    [TestMethod]
    public async Task APointerAlreadyOverAHandleWhenItStartsListeningIsReportedOverIt()
    {
        Module().Setup<BlowHandleHit?>("hoveredHandle", _ => true).SetResult(new BlowHandleHit("b", "", "target", "top"));
        BlowConnectionLineContext? last = null;
        var cut = Render([new BlowNode { Id = "a" }, new BlowNode { Id = "b", Position = new(0, 200) }], extra: p =>
        {
            p.Add(c => c.ConnectOnClick, true);
            p.Add(c => c.OnConnectionChange, (BlowConnectionLineContext? ctx) => last = ctx);
        });

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 0, 0));

        cut.WaitForAssertion(() => Assert.IsTrue(last?.IsValid));
    }

    [TestMethod]
    public async Task ThePointerStillOverTheHandleItClickedIsNotReportedOverATarget()
    {
        Module().Setup<BlowHandleHit?>("hoveredHandle", _ => true).SetResult(new BlowHandleHit("a", "", "source", "bottom"));
        BlowConnectionLineContext? last = null;
        var cut = Render([new BlowNode { Id = "a" }, new BlowNode { Id = "b", Position = new(0, 200) }], extra: p =>
        {
            p.Add(c => c.ConnectOnClick, true);
            p.Add(c => c.OnConnectionChange, (BlowConnectionLineContext? ctx) => last = ctx);
        });

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 0, 0));

        Assert.IsNotNull(last);
        Assert.IsFalse(last.IsValid);
        Assert.AreEqual(BlowConnectionRejection.None, last.Rejection);
    }
}
