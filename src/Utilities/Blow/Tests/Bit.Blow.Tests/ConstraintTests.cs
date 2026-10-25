using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for keyboard movement against the grid, extents applied to resizing, and the
/// key-collision guards on the selection bookkeeping.
/// </summary>
[TestClass]
public class ConstraintTests : BunitContext
{
    public ConstraintTests()
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

    private static void Arrow(IRenderedComponent<BlowCanvas> cut, string key, bool shift = false) =>
        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

    // ---- arrow keys and the grid ----

    [TestMethod]
    public void ArrowKeysPullANodeOntoTheGrid()
    {
        // Starts off-grid: stepping by the cell size alone would keep it off the grid forever.
        var nodes = new List<BlowNode> { new() { Id = "a", Position = new(7, 7), Selected = true } };
        var cut = Render(nodes, extra: p =>
        {
            p.Add(c => c.SnapToGrid, true);
            p.Add(c => c.SnapGridX, 15.0);
            p.Add(c => c.SnapGridY, 15.0);
        });

        Arrow(cut, "ArrowRight");

        Assert.AreEqual(15, nodes[0].Position.X, 1e-3);
        Assert.AreEqual(0, nodes[0].Position.Y, 1e-3);
    }

    [TestMethod]
    public void EachAxisUsesItsOwnGridStep()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Position = new(0, 0), Selected = true } };
        var cut = Render(nodes, extra: p =>
        {
            p.Add(c => c.SnapToGrid, true);
            p.Add(c => c.SnapGridX, 40.0);
            p.Add(c => c.SnapGridY, 10.0);
        });

        Arrow(cut, "ArrowRight");
        Assert.AreEqual(40, nodes[0].Position.X, 1e-3);

        Arrow(cut, "ArrowDown");
        Assert.AreEqual(10, nodes[0].Position.Y, 1e-3);
    }

    [TestMethod]
    public void OneGridPressMovesOneCell()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Position = new(0, 0), Selected = true } };
        var cut = Render(nodes, extra: p =>
        {
            p.Add(c => c.SnapToGrid, true);
            p.Add(c => c.SnapGridX, 15.0);
        });

        Arrow(cut, "ArrowRight");
        Arrow(cut, "ArrowRight");

        Assert.AreEqual(30, nodes[0].Position.X, 1e-3);
    }

    [TestMethod]
    public void WithoutAGridArrowKeysMoveByAFixedStep()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Position = new(7, 0), Selected = true } };
        var cut = Render(nodes);

        Arrow(cut, "ArrowRight");

        // No grid, so nothing to align to: the node keeps its offset.
        Assert.AreEqual(12, nodes[0].Position.X, 1e-3);
    }

    [TestMethod]
    public void ShiftArrowStillMovesFurther()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Position = new(0, 0), Selected = true } };
        var cut = Render(nodes);

        Arrow(cut, "ArrowRight", shift: true);

        Assert.AreEqual(25, nodes[0].Position.X, 1e-3);
    }

    [TestMethod]
    public void ArrowMovementStillRespectsTheNodeExtent()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0), Width = 50, Height = 50, Selected = true },
        };
        var cut = Render(nodes, extra: p => p.Add(c => c.NodeExtent, new BlowRect(0, 0, 100, 100)));

        for (var i = 0; i < 20; i++) Arrow(cut, "ArrowRight");

        Assert.AreEqual(50, nodes[0].Position.X, 1e-3);
    }

    // ---- resize against an extent ----

    [TestMethod]
    public async Task ANodePinnedInsideItsParentCannotBeResizedOutOfIt()
    {
        var parent = new BlowNode { Id = "p", Position = new(0, 0), Width = 300, Height = 200 };
        var child = new BlowNode
        {
            Id = "c",
            ParentId = "p",
            Position = new(50, 40),
            Width = 100,
            Height = 60,
            ExtentParent = true,
        };
        var cut = Render([parent, child]);

        await cut.InvokeAsync(() => cut.Instance.StartResize(
            "c", BlowResizeDirection.BottomRight, 0, 0, 20, 20, false, double.MaxValue, double.MaxValue, 1));

        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 5000, ClientY = 5000 });

        // The child's right/bottom edges stop at the parent's.
        Assert.AreEqual(250, child.Width!.Value, 1e-3);
        Assert.AreEqual(160, child.Height!.Value, 1e-3);
    }

    [TestMethod]
    public async Task ResizingFromTheLeftStopsAtTheExtentsLeftEdge()
    {
        var node = new BlowNode
        {
            Id = "n",
            Position = new(100, 100),
            Width = 100,
            Height = 60,
            Extent = new BlowRect(40, 40, 400, 400),
        };
        var cut = Render([node]);

        await cut.InvokeAsync(() => cut.Instance.StartResize(
            "n", BlowResizeDirection.Left, 0, 0, 20, 20, false, double.MaxValue, double.MaxValue, 1));

        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { PointerId = 1, ClientX = -5000, ClientY = 0 });

        // The right edge is anchored at 200 and the extent starts at 40, so 160 is the widest.
        Assert.AreEqual(160, node.Width!.Value, 1e-3);
        Assert.AreEqual(40, node.Position.X, 1e-3);
    }

    [TestMethod]
    public async Task AnExplicitMaxSizeStillWinsWhenItIsTighter()
    {
        var node = new BlowNode
        {
            Id = "n",
            Position = new(0, 0),
            Width = 100,
            Height = 60,
            Extent = new BlowRect(0, 0, 1000, 1000),
        };
        var cut = Render([node]);

        await cut.InvokeAsync(() => cut.Instance.StartResize(
            "n", BlowResizeDirection.BottomRight, 0, 0, 20, 20, false, 150, 90, 1));

        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 5000, ClientY = 5000 });

        Assert.AreEqual(150, node.Width!.Value, 1e-3);
        Assert.AreEqual(90, node.Height!.Value, 1e-3);
    }

    [TestMethod]
    public async Task AnUnconstrainedNodeResizesFreely()
    {
        var node = new BlowNode { Id = "n", Position = new(0, 0), Width = 100, Height = 60 };
        var cut = Render([node]);

        await cut.InvokeAsync(() => cut.Instance.StartResize(
            "n", BlowResizeDirection.BottomRight, 0, 0, 20, 20, false, double.MaxValue, double.MaxValue, 1));

        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 400, ClientY = 300 });

        Assert.AreEqual(500, node.Width!.Value, 1e-3);
        Assert.AreEqual(360, node.Height!.Value, 1e-3);
    }

    [TestMethod]
    public async Task ResizingStillStopsAtTheMinimum()
    {
        var node = new BlowNode { Id = "n", Position = new(0, 0), Width = 100, Height = 60 };
        var cut = Render([node]);

        await cut.InvokeAsync(() => cut.Instance.StartResize(
            "n", BlowResizeDirection.BottomRight, 0, 0, 40, 30, false, double.MaxValue, double.MaxValue, 1));

        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { PointerId = 1, ClientX = -5000, ClientY = -5000 });

        Assert.AreEqual(40, node.Width!.Value, 1e-3);
        Assert.AreEqual(30, node.Height!.Value, 1e-3);
    }

    // ---- id collisions across the node/edge selection sets ----

    [TestMethod]
    public void ANodeIdThatLooksLikeAnEdgeKeyDoesNotConfuseBoxSelection()
    {
        // The pre-selection set holds both node and edge ids. Tagging with a bare "e:" prefix
        // would put the node "e:x" and the edge "x" at the same entry, and the additive box
        // would then revive an edge that was never selected.
        var nodes = new List<BlowNode>
        {
            new() { Id = "e:x", Position = new(0, 0), Width = 50, Height = 50, Selected = true },
            new() { Id = "other", Position = new(0, 200), Width = 50, Height = 50 },
        };
        var edges = new List<BlowEdge> { new() { Id = "x", Source = "e:x", Target = "other" } };
        var cut = Render(nodes, edges);

        // An additive box drawn over empty space: it should add nothing at all.
        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ShiftKey = true, ClientX = 900, ClientY = 900 });
        pane.PointerMove(new PointerEventArgs { ClientX = 1000, ClientY = 1000 });
        pane.PointerUp(new PointerEventArgs { ClientX = 1000, ClientY = 1000 });

        Assert.IsTrue(nodes[0].Selected);   // survives, because it was selected before the box
        Assert.IsFalse(edges[0].Selected);  // was never selected, and must not be revived
    }

    [TestMethod]
    public void SelectionChangeIsStillRaisedWhenAnEdgeReplacesASimilarlyNamedNode()
    {
        var changes = new List<BlowSelectionChange>();
        var nodes = new List<BlowNode> { new() { Id = "e:x", Position = new(0, 0) } };
        var edges = new List<BlowEdge> { new() { Id = "x", Source = "e:x", Target = "e:x" } };

        var cut = Render(nodes, edges, p => p.Add(c => c.OnSelectionChange,
            EventCallback.Factory.Create<BlowSelectionChange>(this, changes.Add)));

        cut.Find(".bit-blow-node").PointerDown(new PointerEventArgs { Button = 0 });
        cut.Find(".bit-blow-edge").PointerDown(new PointerEventArgs { Button = 0 });

        Assert.AreEqual(2, changes.Count);
        Assert.ContainsSingle(changes[0].Nodes);
        Assert.ContainsSingle(changes[1].Edges);
    }

    // ---- a handle outside a flow ----

    [TestMethod]
    public void AHandleWithoutAFlowRendersInsteadOfThrowing()
    {
        var cut = Render<BlowHandle>();

        Assert.ContainsSingle(cut.FindAll(".bit-blow-handle"));
    }
}
