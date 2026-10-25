using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Covers the paths that fire before (or without) a full layout pass, where measured geometry
/// isn't available yet.
/// </summary>
[TestClass]
public class GeometryFallbackTests : BunitContext
{
    public GeometryFallbackTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void AnUnrenderedNodeReportsItsRectangleFromItsPosition()
    {
        var node = new BlowNode { Id = "a", Position = new(40, 60), Width = 100, Height = 50 };

        var rect = node.GetRect();

        Assert.AreEqual(40, rect.X);
        Assert.AreEqual(60, rect.Y);
        Assert.AreEqual(140, rect.Right);
        Assert.AreEqual(110, rect.Bottom);
    }

    [TestMethod]
    public void BoundsOfUnrenderedNodesAreStillMeaningful()
    {
        var bounds = BlowGraphUtils.GetNodesBounds(
        [
            new BlowNode { Id = "a", Position = new(0, 0), Width = 100, Height = 50 },
            new BlowNode { Id = "b", Position = new(300, 200), Width = 100, Height = 50 },
        ]);

        Assert.AreEqual(0, bounds.X);
        Assert.AreEqual(400, bounds.Right);
        Assert.AreEqual(250, bounds.Bottom);
    }

    [TestMethod]
    public void LayoutOverwritesTheFallbackForChildNodes()
    {
        var parent = new BlowNode { Id = "p", Position = new(100, 100) };
        var child = new BlowNode { Id = "c", Position = new(10, 10), ParentId = "p" };

        // Before layout the fallback is the raw (parent-relative) position...
        Assert.AreEqual(new BlowXYPosition(10, 10), child.AbsolutePosition);

        Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, new List<BlowNode> { parent, child });
            p.Add(c => c.Edges, new List<BlowEdge>());
        });

        // ...and afterwards it is the resolved absolute position.
        Assert.AreEqual(new BlowXYPosition(110, 110), child.AbsolutePosition);
    }

    [TestMethod]
    public void EdgesBetweenUnmeasuredNodesStillGetAPath()
    {
        // With no measured handles the anchors fall back to the node's edge midpoints, which
        // is what keeps the very first render from drawing every edge at the origin.
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, new List<BlowNode>
            {
                new() { Id = "a", Position = new(0, 0) },
                new() { Id = "b", Position = new(0, 200) },
            });
            p.Add(c => c.Edges, new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } });
        });

        var path = cut.Find(".bit-blow-edge-path").GetAttribute("d");
        Assert.IsFalse(string.IsNullOrEmpty(path));
        Assert.DoesNotContain("NaN", path);
    }

    [TestMethod]
    public void HandlePositionsOnTheNodeSteerTheFallbackAnchors()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0), Width = 100, Height = 40, SourcePosition = BlowPosition.Right },
            new() { Id = "b", Position = new(300, 0), Width = 100, Height = 40, TargetPosition = BlowPosition.Left },
        };
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Straight } });
        });

        // Right edge of a (100,20) to left edge of b (300,20).
        Assert.AreEqual("M 100,20 L 300,20", cut.Find(".bit-blow-edge-path").GetAttribute("d"));
    }

    [TestMethod]
    public async Task AScrollOnlyResizeNotificationSkipsRemeasuring()
    {
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, new List<BlowNode> { new() { Id = "a" } });
            p.Add(c => c.Edges, new List<BlowEdge>());
        });

        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(measureNodes: false));

        Assert.ContainsSingle(cut.FindAll(".bit-blow-node"));
    }

    [TestMethod]
    public void ADragStartedFromADragHandleMovesTheNode()
    {
        // Regression: the pane only listens for pointer moves while a gesture is active, and a
        // BlowDragHandle drag has to re-render the canvas to get that listener attached.
        var node = new BlowNode { Id = "a", Position = new(0, 0), UseCustomDragHandle = true };
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, new List<BlowNode> { node });
            p.Add(c => c.Edges, new List<BlowEdge>());
            p.Add(c => c.NodeTemplate, (RenderFragment<BlowNode>)(_ => builder =>
            {
                builder.OpenComponent<BlowDragHandle>(0);
                builder.CloseComponent();
            }));
        });

        cut.Find(".bit-blow-drag-handle").PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { ClientX = 60, ClientY = 30 });

        Assert.AreEqual(60, node.Position.X, 1e-3);
        Assert.AreEqual(30, node.Position.Y, 1e-3);
    }

    [TestMethod]
    public void ANodeBodyPressSelectsButDoesNotStartADragWhenACustomDragHandleIsRequired()
    {
        var node = new BlowNode { Id = "a", Position = new(0, 0), UseCustomDragHandle = true };
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, new List<BlowNode> { node });
            p.Add(c => c.Edges, new List<BlowEdge>());
        });

        cut.Find(".bit-blow-node").PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });

        Assert.IsTrue(node.Selected);
        // No gesture started, so the pane doesn't even listen for moves - attempting to drag
        // the body cannot move the node.
        Assert.ThrowsExactly<Bunit.MissingEventHandlerException>(() =>
            cut.Find(".bit-blow").PointerMove(new PointerEventArgs { ClientX = 60, ClientY = 30 }));
        Assert.AreEqual(0, node.Position.X);
    }

    [TestMethod]
    public void AnIdleCanvasDoesNotListenForPointerMoves()
    {
        // The move listener is the hottest event on the canvas; leaving it attached while
        // nothing is happening costs a round-trip per mouse move.
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, new List<BlowNode> { new() { Id = "a" } });
            p.Add(c => c.Edges, new List<BlowEdge>());
        });

        Assert.ThrowsExactly<Bunit.MissingEventHandlerException>(() =>
            cut.Find(".bit-blow").PointerMove(new PointerEventArgs { ClientX = 10, ClientY = 10 }));
    }
}
