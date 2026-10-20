using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for the selection-wide gestures (dragging and right-clicking several nodes at once),
/// the separate selection key, and the viewport helpers that can ease rather than jump.
/// </summary>
[TestClass]
public class SelectionAndViewportTests : BunitContext
{
    public SelectionAndViewportTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<BlowCanvas> Render(
        List<BlowNode> nodes, List<BlowEdge>? edges = null,
        Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            extra?.Invoke(p);
        });

    private static List<BlowNode> ThreeNodes() =>
    [
        new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40 },
        new() { Id = "b", Position = new(200, 0), Width = 100, Height = 40 },
        new() { Id = "c", Position = new(400, 0), Width = 100, Height = 40 },
    ];

    // ---- selection drag events ----

    [TestMethod]
    public void DraggingSeveralSelectedNodesReportsThemAsOneSelectionDrag()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = true;
        nodes[1].Selected = true;

        BlowSelectionMouseEvent? started = null;
        var moves = 0;
        BlowSelectionMouseEvent? stopped = null;

        var cut = Render(nodes, extra: p =>
        {
            p.Add(c => c.OnSelectionDragStart, (BlowSelectionMouseEvent e) => started = e);
            p.Add(c => c.OnSelectionDrag, (BlowSelectionMouseEvent _) => moves++);
            p.Add(c => c.OnSelectionDragStop, (BlowSelectionMouseEvent e) => stopped = e);
        });

        DragNode(cut, from: (0, 0), to: (50, 50));

        Assert.IsNotNull(started);
        Assert.AreSequenceEqual(["a", "b"], started!.Value.Nodes.Select(n => n.Id));
        Assert.IsTrue(moves > 0);
        Assert.IsNotNull(stopped);
        Assert.AreEqual(2, stopped!.Value.Nodes.Count);
    }

    [TestMethod]
    public void NudgingASingleNodeIsANodeDragAndNotASelectionDrag()
    {
        var nodes = ThreeNodes();
        var selectionDrags = 0;
        var nodeDrags = 0;

        var cut = Render(nodes, extra: p =>
        {
            p.Add(c => c.OnSelectionDragStart, (BlowSelectionMouseEvent _) => selectionDrags++);
            p.Add(c => c.OnNodeDragStart, (BlowNode _) => nodeDrags++);
        });

        DragNode(cut, from: (0, 0), to: (50, 50));

        Assert.AreEqual(0, selectionDrags);
        Assert.AreEqual(1, nodeDrags);
    }

    [TestMethod]
    public void ASelectionDragCarriesTheNodesThatMovedNotJustTheOneGrabbed()
    {
        var nodes = ThreeNodes();
        foreach (var n in nodes) n.Selected = true;

        BlowSelectionMouseEvent? started = null;
        var cut = Render(nodes, extra: p =>
            p.Add(c => c.OnSelectionDragStart, (BlowSelectionMouseEvent e) => started = e));

        DragNode(cut, from: (0, 0), to: (10, 10));

        Assert.AreEqual(3, started!.Value.Nodes.Count);
    }

    [TestMethod]
    public void RightClickingOneOfSeveralSelectedNodesReportsTheWholeSelection()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = true;
        nodes[2].Selected = true;

        BlowSelectionMouseEvent? menu = null;
        BlowNodeMouseEvent? nodeMenu = null;

        var cut = Render(nodes, extra: p =>
        {
            p.Add(c => c.OnSelectionContextMenu, (BlowSelectionMouseEvent e) => menu = e);
            p.Add(c => c.OnNodeContextMenu, (BlowNodeMouseEvent e) => nodeMenu = e);
        });

        cut.FindAll(".bit-blow-node")[0].ContextMenu(new MouseEventArgs());

        Assert.IsNotNull(nodeMenu);
        Assert.AreEqual("a", nodeMenu!.Value.Node.Id);
        Assert.IsNotNull(menu);
        Assert.AreSequenceEqual(["a", "c"], menu!.Value.Nodes.Select(n => n.Id));
    }

    [TestMethod]
    public void RightClickingAnUnselectedNodeReportsOnlyThatNode()
    {
        var nodes = ThreeNodes();
        nodes[1].Selected = true;
        nodes[2].Selected = true;

        var selectionMenus = 0;
        var cut = Render(nodes, extra: p =>
            p.Add(c => c.OnSelectionContextMenu, (BlowSelectionMouseEvent _) => selectionMenus++));

        cut.FindAll(".bit-blow-node")[0].ContextMenu(new MouseEventArgs());

        Assert.AreEqual(0, selectionMenus);
    }

    // ---- selection key ----

    [TestMethod]
    public void HoldingTheSelectionKeyDrawsABoxRatherThanPanning()
    {
        var cut = Render(ThreeNodes());

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ShiftKey = true, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 300, ClientY = 100 });

        Assert.ContainsSingle(cut.FindAll(".bit-blow-selection"));
    }

    [TestMethod]
    public void TheSelectionKeyCanBeMovedOffShift()
    {
        var cut = Render(ThreeNodes(), extra: p =>
        {
            p.Add(c => c.SelectionKey, BlowModifierKey.Alt);
            p.Add(c => c.MultiSelectionKey, BlowModifierKey.Meta);
        });

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, AltKey = true, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 300, ClientY = 100 });

        Assert.ContainsSingle(cut.FindAll(".bit-blow-selection"));
    }

    [TestMethod]
    public void WithNoSelectionKeyAPlainDragPansInstead()
    {
        var cut = Render(ThreeNodes(), extra: p =>
        {
            p.Add(c => c.SelectionKey, (BlowModifierKey?)null);
            p.Add(c => c.MultiSelectionKey, BlowModifierKey.Meta);
        });

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ShiftKey = true, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 300, ClientY = 100 });

        Assert.IsEmpty(cut.FindAll(".bit-blow-selection"));
        Assert.AreEqual(300, cut.Instance.GetViewport().X, 1e-6);
    }

    // ---- align / distribute through the canvas ----

    [TestMethod]
    public async Task AligningTheSelectionMovesOnlySelectedNodes()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = true;
        nodes[1].Selected = true;

        var cut = Render(nodes);
        await cut.InvokeAsync(() => cut.Instance.AlignSelectionAsync(BlowAlignEdge.Left));

        Assert.AreEqual(0, nodes[0].Position.X, 1e-6);
        Assert.AreEqual(0, nodes[1].Position.X, 1e-6);
        Assert.AreEqual(400, nodes[2].Position.X, 1e-6);
    }

    [TestMethod]
    public async Task AligningTheSelectionRaisesNodesChanged()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = true;
        nodes[1].Selected = true;

        var changed = 0;
        var cut = Render(nodes, extra: p => p.Add(c => c.NodesChanged, (List<BlowNode> _) => changed++));

        await cut.InvokeAsync(() => cut.Instance.AlignSelectionAsync(BlowAlignEdge.Left));

        Assert.AreEqual(1, changed);
    }

    [TestMethod]
    public async Task AligningAnAlreadyAlignedSelectionChangesNothing()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = true;
        nodes[1].Selected = true;
        nodes[1].Position = new(0, 100);

        var changed = 0;
        var cut = Render(nodes, extra: p => p.Add(c => c.NodesChanged, (List<BlowNode> _) => changed++));

        await cut.InvokeAsync(() => cut.Instance.AlignSelectionAsync(BlowAlignEdge.Left));

        Assert.AreEqual(0, changed);
    }

    [TestMethod]
    public async Task DistributingTheSelectionEvensOutTheGaps()
    {
        var nodes = ThreeNodes();
        nodes[1].Position = new(120, 0);
        foreach (var n in nodes) n.Selected = true;

        var cut = Render(nodes);
        await cut.InvokeAsync(() => cut.Instance.DistributeSelectionAsync(horizontal: true));

        Assert.AreEqual(0, nodes[0].Position.X, 1e-6);
        Assert.AreEqual(200, nodes[1].Position.X, 1e-6);
        Assert.AreEqual(400, nodes[2].Position.X, 1e-6);
    }

    [TestMethod]
    public async Task AlignedNodesStillRespectTheirExtent()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = true;
        nodes[1].Selected = true;
        // The second node may not travel left of x = 150, so aligning left stops it there.
        nodes[1].Extent = new BlowRect(150, 0, 400, 400);

        var cut = Render(nodes);
        await cut.InvokeAsync(() => cut.Instance.AlignSelectionAsync(BlowAlignEdge.Left));

        Assert.AreEqual(0, nodes[0].Position.X, 1e-6);
        Assert.AreEqual(150, nodes[1].Position.X, 1e-6);
    }

    [TestMethod]
    public async Task AligningDoesNothingWhenNodesAreNotDraggable()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = true;
        nodes[1].Selected = true;

        var cut = Render(nodes, extra: p => p.Add(c => c.NodesDraggable, false));
        await cut.InvokeAsync(() => cut.Instance.AlignSelectionAsync(BlowAlignEdge.Left));

        Assert.AreEqual(200, nodes[1].Position.X, 1e-6);
    }

    // ---- viewport transitions ----

    [TestMethod]
    public async Task SetViewportAsyncWithNoDurationAppliesImmediately()
    {
        var cut = Render(ThreeNodes());

        await cut.InvokeAsync(() => cut.Instance.SetViewportAsync(new BlowViewport(50, 60, 1.5), 0));

        Assert.AreEqual(new BlowViewport(50, 60, 1.5), cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task AnAnimatedViewportEndsExactlyOnItsTarget()
    {
        var cut = Render(ThreeNodes());

        await cut.InvokeAsync(() => cut.Instance.SetViewportAsync(new BlowViewport(120, -40, 2), 40));

        Assert.AreEqual(new BlowViewport(120, -40, 2), cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task AnAnimatedViewportBracketsItselfWithMoveStartAndMoveEnd()
    {
        var starts = 0;
        var ends = 0;
        var cut = Render(ThreeNodes(), extra: p =>
        {
            p.Add(c => c.OnMoveStart, (BlowViewport _) => starts++);
            p.Add(c => c.OnMoveEnd, (BlowViewport _) => ends++);
        });

        await cut.InvokeAsync(() => cut.Instance.SetViewportAsync(new BlowViewport(10, 10, 1.2), 40));

        Assert.AreEqual(1, starts);
        Assert.AreEqual(1, ends);
    }

    [TestMethod]
    public async Task AnAnimatedViewportIsStillClampedToTheZoomRange()
    {
        var cut = Render(ThreeNodes(), extra: p => p.Add(c => c.MaxZoom, 1.5));

        await cut.InvokeAsync(() => cut.Instance.SetViewportAsync(new BlowViewport(0, 0, 10), 0));

        Assert.AreEqual(1.5, cut.Instance.GetViewport().Zoom, 1e-6);
    }

    [TestMethod]
    public async Task StartingAPanAbandonsAViewportTransitionRatherThanFightingIt()
    {
        var cut = Render(ThreeNodes());

        // Kick off a long transition, then grab the pane; the drag must win outright.
        var animation = cut.InvokeAsync(() => cut.Instance.SetViewportAsync(new BlowViewport(9999, 9999, 2), 5000));

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 25, ClientY = 15 });
        pane.PointerUp(new PointerEventArgs { ClientX = 25, ClientY = 15 });

        await animation;

        var viewport = cut.Instance.GetViewport();
        Assert.IsTrue(viewport.X < 100, $"the transition kept running: X was {viewport.X}");
    }

    [TestMethod]
    public async Task ZoomToAsyncLandsOnTheRequestedZoom()
    {
        var cut = Render(ThreeNodes());

        await cut.InvokeAsync(() => cut.Instance.ZoomToAsync(1.8, 30));

        Assert.AreEqual(1.8, cut.Instance.GetZoom(), 1e-6);
    }

    [TestMethod]
    public async Task SetCenterAsyncPutsThePointAtThePaneCentre()
    {
        var cut = Render(ThreeNodes());

        await cut.InvokeAsync(() => cut.Instance.SetCenterAsync(100, 50, 1, 0));

        // With no measured pane the centre is (0,0), so the flow point lands on the origin.
        var viewport = cut.Instance.GetViewport();
        Assert.AreEqual(-100, viewport.X, 1e-6);
        Assert.AreEqual(-50, viewport.Y, 1e-6);
    }

    // ---- helpers ----

    private static void DragNode(
        IRenderedComponent<BlowCanvas> cut, (double X, double Y) from, (double X, double Y) to, int nodeIndex = 0)
    {
        var node = cut.FindAll(".bit-blow-node")[nodeIndex];
        node.PointerDown(new PointerEventArgs { Button = 0, ClientX = from.X, ClientY = from.Y });

        var pane = cut.Find(".bit-blow");
        pane.PointerMove(new PointerEventArgs { ClientX = to.X, ClientY = to.Y });
        pane.PointerUp(new PointerEventArgs { ClientX = to.X, ClientY = to.Y });
    }
}
