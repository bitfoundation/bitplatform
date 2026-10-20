using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

[TestClass]
public class InteractionTests : BunitContext
{
    public InteractionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<BlowCanvas> Render(
        List<BlowNode> nodes, List<BlowEdge>? edges = null, Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            extra?.Invoke(p);
        });

    // ---- node extents ----

    [TestMethod]
    public void ANodeExtentPinsTheNodeInsideItsRectangle()
    {
        var node = new BlowNode
        {
            Id = "n",
            Position = new(0, 0),
            Width = 100,
            Height = 50,
            Extent = new BlowRect(0, 0, 300, 200),
        };
        var cut = Render([node]);

        DragNode(cut, from: (0, 0), to: (1000, 1000));

        // Right/bottom edges stop at the extent, so the top-left lands at 200,150.
        Assert.AreEqual(200, node.Position.X, 1e-3);
        Assert.AreEqual(150, node.Position.Y, 1e-3);
    }

    [TestMethod]
    public void ANodeExtentAlsoClampsTowardTheOrigin()
    {
        var node = new BlowNode
        {
            Id = "n",
            Position = new(100, 100),
            Width = 50,
            Height = 50,
            Extent = new BlowRect(20, 30, 300, 200),
        };
        var cut = Render([node]);

        DragNode(cut, from: (0, 0), to: (-1000, -1000));

        Assert.AreEqual(20, node.Position.X, 1e-3);
        Assert.AreEqual(30, node.Position.Y, 1e-3);
    }

    [TestMethod]
    public void ExtentParentBeatsThePerNodeExtent()
    {
        var parent = new BlowNode { Id = "p", Position = new(0, 0), Width = 200, Height = 200 };
        var child = new BlowNode
        {
            Id = "c",
            ParentId = "p",
            Position = new(0, 0),
            Width = 50,
            Height = 50,
            ExtentParent = true,
            Extent = new BlowRect(0, 0, 10_000, 10_000),
        };
        var cut = Render([parent, child]);

        DragNode(cut, nodeIndex: 1, from: (0, 0), to: (1000, 1000));

        Assert.AreEqual(150, child.Position.X, 1e-3);
        Assert.AreEqual(150, child.Position.Y, 1e-3);
    }

    [TestMethod]
    public void ExpandParentGrowsTheParentInsteadOfClipping()
    {
        var parent = new BlowNode { Id = "p", Position = new(0, 0), Width = 200, Height = 200 };
        var child = new BlowNode
        {
            Id = "c",
            ParentId = "p",
            Position = new(100, 100),
            Width = 50,
            Height = 50,
            ExpandParent = true,
        };
        var cut = Render([parent, child]);

        DragNode(cut, nodeIndex: 1, from: (0, 0), to: (100, 0));

        // The child now ends at x = 250, so the parent has to reach at least that far.
        Assert.AreEqual(250, parent.Width);
        Assert.AreEqual(200, parent.Height);
    }

    [TestMethod]
    public void ExpandParentShiftsTheParentWhenTheChildGoesNegative()
    {
        var parent = new BlowNode { Id = "p", Position = new(500, 500), Width = 200, Height = 200 };
        var child = new BlowNode
        {
            Id = "c",
            ParentId = "p",
            Position = new(0, 0),
            Width = 50,
            Height = 50,
            ExpandParent = true,
        };
        var cut = Render([parent, child]);

        DragNode(cut, nodeIndex: 1, from: (0, 0), to: (-40, 0));

        // The parent's left edge moves out to swallow the child...
        Assert.AreEqual(460, parent.Position.X, 1e-3);
        Assert.AreEqual(240, parent.Width);
        // ...and the child is re-anchored so it doesn't visually jump.
        Assert.AreEqual(0, child.Position.X, 1e-3);
    }

    // ---- selection box ----

    [TestMethod]
    public void BoxSelectionPicksUpEdgesBetweenSelectedNodes()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0), Width = 50, Height = 50 },
            new() { Id = "b", Position = new(60, 0), Width = 50, Height = 50 },
            new() { Id = "far", Position = new(900, 900), Width = 50, Height = 50 },
        };
        var edges = new List<BlowEdge>
        {
            new() { Id = "inside", Source = "a", Target = "b" },
            new() { Id = "dangling", Source = "b", Target = "far" },
        };
        var cut = Render(nodes, edges, p => p.Add(c => c.SelectionOnDrag, true));

        DragSelectionBox(cut, from: (-10, -10), to: (200, 200));

        Assert.IsTrue(edges[0].Selected);
        Assert.IsFalse(edges[1].Selected);
    }

    [TestMethod]
    public void BoxSelectionLeavesEdgesAloneWhenTheOptionIsOff()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0), Width = 50, Height = 50 },
            new() { Id = "b", Position = new(60, 0), Width = 50, Height = 50 },
        };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var cut = Render(nodes, edges, p =>
        {
            p.Add(c => c.SelectionOnDrag, true);
            p.Add(c => c.SelectEdgesOnSelectionBox, false);
        });

        DragSelectionBox(cut, from: (-10, -10), to: (200, 200));

        Assert.IsTrue(nodes[0].Selected);
        Assert.IsFalse(edges[0].Selected);
    }

    [TestMethod]
    public void FullSelectionModeRequiresTheNodeToBeCompletelyInsideTheBox()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "inside", Position = new(10, 10), Width = 20, Height = 20 },
            new() { Id = "straddling", Position = new(90, 10), Width = 40, Height = 20 },
        };
        var cut = Render(nodes, extra: p =>
        {
            p.Add(c => c.SelectionOnDrag, true);
            p.Add(c => c.SelectionMode, BlowSelectionMode.Full);
        });

        DragSelectionBox(cut, from: (0, 0), to: (100, 100));

        Assert.IsTrue(nodes[0].Selected);
        Assert.IsFalse(nodes[1].Selected);
    }

    // ---- click suppression ----

    [TestMethod]
    public void ClickingANodeWithoutMovingReportsANodeClick()
    {
        var node = new BlowNode { Id = "n", Position = new(0, 0) };
        BlowNode? clicked = null;
        var cut = Render([node], extra: p => p.Add(
            c => c.OnNodeClick, EventCallback.Factory.Create<BlowNode>(this, n => clicked = n)));

        var element = cut.Find(".bit-blow-node");
        element.PointerDown(new PointerEventArgs { Button = 0, ClientX = 10, ClientY = 10 });
        element.Click();

        Assert.AreSame(node, clicked);
    }

    [TestMethod]
    public void DraggingANodeDoesNotReportANodeClick()
    {
        var node = new BlowNode { Id = "n", Position = new(0, 0) };
        var clicks = 0;
        var cut = Render([node], extra: p => p.Add(
            c => c.OnNodeClick, EventCallback.Factory.Create<BlowNode>(this, _ => clicks++)));

        var element = cut.Find(".bit-blow-node");
        element.PointerDown(new PointerEventArgs { Button = 0, ClientX = 10, ClientY = 10 });
        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { ClientX = 90, ClientY = 90 });
        element.Click();

        Assert.AreEqual(0, clicks);
    }

    [TestMethod]
    public void PanningTheCanvasDoesNotReportAPaneClick()
    {
        var clicks = 0;
        var cut = Render([new BlowNode { Id = "n" }], extra: p => p.Add(
            c => c.OnPaneClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicks++)));

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 120, ClientY = 60 });
        pane.PointerUp(new PointerEventArgs { ClientX = 120, ClientY = 60 });
        pane.Click();

        Assert.AreEqual(0, clicks);
    }

    [TestMethod]
    public void APlainClickOnThePaneIsStillReported()
    {
        var clicks = 0;
        var cut = Render([new BlowNode { Id = "n" }], extra: p => p.Add(
            c => c.OnPaneClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicks++)));

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        pane.PointerUp(new PointerEventArgs { ClientX = 0, ClientY = 0 });
        pane.Click();

        Assert.AreEqual(1, clicks);
    }

    // ---- panning ----

    [TestMethod]
    public void DraggingThePaneMovesTheViewport()
    {
        var cut = Render([new BlowNode { Id = "n" }]);

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 40, ClientY = 25 });

        Assert.AreEqual(40, cut.Instance.GetViewport().X, 1e-3);
        Assert.AreEqual(25, cut.Instance.GetViewport().Y, 1e-3);
    }

    [TestMethod]
    public void PanOnDragFalseLeavesTheViewportAlone()
    {
        var cut = Render([new BlowNode { Id = "n" }], extra: p => p.Add(c => c.PanOnDrag, false));

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 40, ClientY = 25 });

        Assert.AreEqual(0, cut.Instance.GetViewport().X);
    }

    [TestMethod]
    public void TheMiddleButtonPansWhenItIsListedInPanButtons()
    {
        var cut = Render([new BlowNode { Id = "n" }], extra: p => p.Add(c => c.PanButtons, new[] { 1 }));

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 1, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 30, ClientY = 0 });

        Assert.AreEqual(30, cut.Instance.GetViewport().X, 1e-3);
    }

    [TestMethod]
    public void HoldingThePanKeyPansEvenWithSelectionOnDrag()
    {
        var node = new BlowNode { Id = "n", Position = new(0, 0), Width = 50, Height = 50 };
        var cut = Render([node], extra: p => p.Add(c => c.SelectionOnDrag, true));

        var pane = cut.Find(".bit-blow");
        pane.KeyDown(new KeyboardEventArgs { Key = " " });
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 45, ClientY = 0 });

        Assert.AreEqual(45, cut.Instance.GetViewport().X, 1e-3);
        Assert.IsFalse(node.Selected);
    }

    [TestMethod]
    public void ReleasingThePanKeyRestoresSelectionOnDrag()
    {
        var node = new BlowNode { Id = "n", Position = new(0, 0), Width = 50, Height = 50 };
        var cut = Render([node], extra: p => p.Add(c => c.SelectionOnDrag, true));

        var pane = cut.Find(".bit-blow");
        pane.KeyDown(new KeyboardEventArgs { Key = " " });
        pane.KeyUp(new KeyboardEventArgs { Key = " " });
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 200, ClientY = 200 });

        Assert.AreEqual(0, cut.Instance.GetViewport().X);
        Assert.IsTrue(node.Selected);
    }

    // ---- stacking ----

    [TestMethod]
    public void ASelectedNodeIsLiftedAboveItsPeers()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Selected = true }, new() { Id = "b" } };
        var cut = Render(nodes);

        var rendered = cut.FindAll(".bit-blow-node");
        Assert.Contains("z-index:1000", rendered[0].GetAttribute("style") ?? string.Empty);
        Assert.DoesNotContain("z-index", rendered[1].GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void AnExplicitNodeZIndexWinsOverTheElevation()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Selected = true, ZIndex = 7 } };
        var cut = Render(nodes);

        Assert.Contains("z-index:7", cut.Find(".bit-blow-node").GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void ElevationCanBeTurnedOff()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Selected = true } };
        var cut = Render(nodes, extra: p => p.Add(c => c.ElevateNodesOnSelect, false));

        Assert.DoesNotContain("z-index", cut.Find(".bit-blow-node").GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void EdgesWithAHigherZIndexAreDrawnLast()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var edges = new List<BlowEdge>
        {
            new() { Id = "top", Source = "a", Target = "b", ZIndex = 5 },
            new() { Id = "bottom", Source = "a", Target = "b", ZIndex = 1 },
        };
        var cut = Render(nodes, edges);

        var rendered = cut.FindAll(".bit-blow-edge");
        Assert.AreEqual("bottom", rendered[0].GetAttribute("data-id"));
        Assert.AreEqual("top", rendered[1].GetAttribute("data-id"));
    }

    [TestMethod]
    public void ASelectedEdgeIsDrawnLastWhenEdgeElevationIsOn()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var edges = new List<BlowEdge>
        {
            new() { Id = "picked", Source = "a", Target = "b", Selected = true },
            new() { Id = "plain", Source = "a", Target = "b" },
        };
        var cut = Render(nodes, edges, p => p.Add(c => c.ElevateEdgesOnSelect, true));

        var rendered = cut.FindAll(".bit-blow-edge");
        Assert.AreEqual("plain", rendered[0].GetAttribute("data-id"));
        Assert.AreEqual("picked", rendered[1].GetAttribute("data-id"));
    }

    [TestMethod]
    public void GroupContainersRenderBehindTheirChildren()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "child", ParentId = "group" },
            new() { Id = "group" },
        };
        var cut = Render(nodes);

        var rendered = cut.FindAll(".bit-blow-node");
        Assert.AreEqual("group", rendered[0].GetAttribute("data-id"));
        Assert.AreEqual("child", rendered[1].GetAttribute("data-id"));
    }

    // ---- helpers ----

    private static void DragNode(
        IRenderedComponent<BlowCanvas> cut, (double X, double Y) from, (double X, double Y) to, int nodeIndex = 0)
    {
        var node = cut.FindAll(".bit-blow-node")[nodeIndex];
        node.PointerDown(new PointerEventArgs { Button = 0, ClientX = from.X, ClientY = from.Y });
        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { ClientX = to.X, ClientY = to.Y });
    }

    private static void DragSelectionBox(
        IRenderedComponent<BlowCanvas> cut, (double X, double Y) from, (double X, double Y) to)
    {
        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = from.X, ClientY = from.Y });
        pane.PointerMove(new PointerEventArgs { ClientX = to.X, ClientY = to.Y });
        pane.PointerUp(new PointerEventArgs { ClientX = to.X, ClientY = to.Y });
    }
}
