using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Blow.Tests;

/// <summary>
/// Component-level tests. The canvas talks to a tiny JS module for measuring; bUnit's loose
/// JS interop returns defaults for all of it, which is exactly the "not measured yet" path.
/// </summary>
[TestClass]
public class BlowCanvasTests : BunitContext
{
    public BlowCanvasTests()
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

    // ---- rendering ----

    [TestMethod]
    public void RendersOneElementPerVisibleNodeAndEdge()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "1", Position = new(0, 0) },
            new() { Id = "2", Position = new(0, 100) },
            new() { Id = "3", Position = new(0, 200), Hidden = true },
        };
        var edges = new List<BlowEdge>
        {
            new() { Id = "e1", Source = "1", Target = "2" },
            new() { Id = "e2", Source = "2", Target = "3", Hidden = true },
        };

        var cut = Render(nodes, edges);

        Assert.AreEqual(2, cut.FindAll(".bit-blow-node").Count);
        Assert.ContainsSingle(cut.FindAll(".bit-blow-edge"));
    }

    [TestMethod]
    public void DuplicateNodeIdsDoNotThrow()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "dup", Position = new(0, 0) },
            new() { Id = "dup", Position = new(0, 100) },
        };

        var cut = Render(nodes);

        // Elements are keyed by id and Blazor will not diff a repeated key, so only the node
        // the id actually resolves to is drawn - a reported mistake rather than a crash.
        Assert.ContainsSingle(cut.FindAll(".bit-blow-node"));
        Assert.AreSame(nodes[0], cut.Instance.GetNode("dup"));
    }

    [TestMethod]
    public void AGraphWithDuplicateIdsSurvivesARerender()
    {
        var nodes = new List<BlowNode> { new() { Id = "dup" }, new() { Id = "dup" } };
        var edges = new List<BlowEdge>
        {
            new() { Id = "same", Source = "dup", Target = "dup" },
            new() { Id = "same", Source = "dup", Target = "dup" },
        };

        var cut = Render(nodes, edges);
        cut.Render();
        cut.Render();

        Assert.ContainsSingle(cut.FindAll(".bit-blow-node"));
        Assert.ContainsSingle(cut.FindAll(".bit-blow-edge"));
    }

    [TestMethod]
    public void EdgeReferencingAMissingNodeRendersWithoutThrowing()
    {
        var nodes = new List<BlowNode> { new() { Id = "1", Position = new(0, 0) } };
        var edges = new List<BlowEdge> { new() { Id = "e1", Source = "1", Target = "ghost" } };

        var cut = Render(nodes, edges);

        Assert.ContainsSingle(cut.FindAll(".bit-blow-edge"));
    }

    [TestMethod]
    public void AttributionIsRenderedUnlessHidden()
    {
        var nodes = new List<BlowNode> { new() { Id = "1" } };

        Assert.ContainsSingle(Render(nodes).FindAll(".bit-blow-attribution"));
        Assert.IsEmpty(Render(nodes, extra: p => p.Add(c => c.HideAttribution, true))
            .FindAll(".bit-blow-attribution"));
    }

    // ---- layout ----

    [TestMethod]
    public void ChildNodePositionsAreResolvedRelativeToTheirParent()
    {
        var parent = new BlowNode { Id = "p", Position = new(100, 100), Width = 400, Height = 300 };
        var child = new BlowNode { Id = "c", Position = new(20, 30), ParentId = "p" };

        Render([parent, child]);

        Assert.AreEqual(new BlowXYPosition(120, 130), child.AbsolutePosition);
    }

    [TestMethod]
    public void GrandchildPositionsAccumulateThroughTheWholeChain()
    {
        var a = new BlowNode { Id = "a", Position = new(10, 10) };
        var b = new BlowNode { Id = "b", Position = new(10, 10), ParentId = "a" };
        var c = new BlowNode { Id = "c", Position = new(10, 10), ParentId = "b" };

        Render([a, b, c]);

        Assert.AreEqual(new BlowXYPosition(30, 30), c.AbsolutePosition);
    }

    [TestMethod]
    public void ParentCyclesDoNotHangTheRender()
    {
        var a = new BlowNode { Id = "a", Position = new(1, 1), ParentId = "b" };
        var b = new BlowNode { Id = "b", Position = new(1, 1), ParentId = "a" };

        var cut = Render([a, b]);

        Assert.AreEqual(2, cut.FindAll(".bit-blow-node").Count);
    }

    [TestMethod]
    public void NodeOriginShiftsTheRenderedPositionByAFractionOfTheSize()
    {
        var node = new BlowNode { Id = "n", Position = new(100, 100), Width = 200, Height = 100 };

        Render([node], extra: p => p.Add(c => c.NodeOrigin, new BlowXYPosition(0.5, 0.5)));

        // Centre origin: the position now refers to the middle of the node.
        Assert.AreEqual(new BlowXYPosition(0, 50), node.AbsolutePosition);
    }

    [TestMethod]
    public void PerNodeOriginOverridesTheCanvasDefault()
    {
        var node = new BlowNode { Id = "n", Position = new(100, 100), Width = 200, Height = 100, Origin = new(0, 0) };

        Render([node], extra: p => p.Add(c => c.NodeOrigin, new BlowXYPosition(0.5, 0.5)));

        Assert.AreEqual(new BlowXYPosition(100, 100), node.AbsolutePosition);
    }

    // ---- selection ----

    [TestMethod]
    public void ClickingANodeSelectsIt()
    {
        var node = new BlowNode { Id = "1", Position = new(0, 0) };
        var cut = Render([node]);

        cut.Find(".bit-blow-node").PointerDown(new() { Button = 0 });

        Assert.IsTrue(node.Selected);
    }

    [TestMethod]
    public void ANonDraggableNodeIsStillSelectable()
    {
        // Regression: the renderer used to swallow pointer-down for non-draggable nodes,
        // which made them impossible to select.
        var node = new BlowNode { Id = "1", Position = new(0, 0), Draggable = false };
        var cut = Render([node]);

        cut.Find(".bit-blow-node").PointerDown(new() { Button = 0 });

        Assert.IsTrue(node.Selected);
    }

    [TestMethod]
    public void ANonSelectableNodeIsNotSelected()
    {
        var node = new BlowNode { Id = "1", Position = new(0, 0), Selectable = false };
        var cut = Render([node]);

        cut.Find(".bit-blow-node").PointerDown(new() { Button = 0 });

        Assert.IsFalse(node.Selected);
    }

    [TestMethod]
    public void SelectingASecondNodeWithoutTheModifierReplacesTheSelection()
    {
        var a = new BlowNode { Id = "a", Position = new(0, 0) };
        var b = new BlowNode { Id = "b", Position = new(0, 100) };
        var cut = Render([a, b]);

        cut.FindAll(".bit-blow-node")[0].PointerDown(new() { Button = 0 });
        cut.FindAll(".bit-blow-node")[1].PointerDown(new() { Button = 0 });

        Assert.IsFalse(a.Selected);
        Assert.IsTrue(b.Selected);
    }

    [TestMethod]
    public void HoldingTheMultiSelectionKeyAddsToTheSelection()
    {
        var a = new BlowNode { Id = "a", Position = new(0, 0) };
        var b = new BlowNode { Id = "b", Position = new(0, 100) };
        var cut = Render([a, b]);

        cut.FindAll(".bit-blow-node")[0].PointerDown(new() { Button = 0 });
        cut.FindAll(".bit-blow-node")[1].PointerDown(new() { Button = 0, ShiftKey = true });

        Assert.IsTrue(a.Selected);
        Assert.IsTrue(b.Selected);
    }

    [TestMethod]
    public async Task SelectAllSelectsEverySelectableVisibleElement()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a" },
            new() { Id = "b", Selectable = false },
            new() { Id = "c", Hidden = true },
        };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.SelectAllAsync());

        Assert.IsTrue(nodes[0].Selected);
        Assert.IsFalse(nodes[1].Selected);
        Assert.IsFalse(nodes[2].Selected);
        Assert.IsTrue(edges[0].Selected);
    }

    [TestMethod]
    public void SelectionChangeIsNotRaisedWhenTheSelectionDidNotChange()
    {
        var node = new BlowNode { Id = "1", Position = new(0, 0) };
        var raised = 0;
        var cut = Render([node], extra: p => p.Add(
            c => c.OnSelectionChange, EventCallback.Factory.Create<BlowSelectionChange>(this, _ => raised++)));

        // Two identical clicks on the same node: only the first changes anything.
        cut.Find(".bit-blow-node").PointerDown(new() { Button = 0 });
        cut.Find(".bit-blow-node").PointerDown(new() { Button = 0 });

        Assert.AreEqual(1, raised);
    }

    // ---- deletion ----

    [TestMethod]
    public async Task DeleteElementsRemovesNodesAndTheirEdges()
    {
        var a = new BlowNode { Id = "a" };
        var b = new BlowNode { Id = "b" };
        var nodes = new List<BlowNode> { a, b };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync([a]));

        Assert.AreSequenceEqual(["b"], nodes.Select(n => n.Id));
        Assert.IsEmpty(edges);
    }

    [TestMethod]
    public async Task DeleteElementsSkipsNonDeletableNodes()
    {
        var a = new BlowNode { Id = "a", Deletable = false };
        var nodes = new List<BlowNode> { a };
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync([a]));

        Assert.ContainsSingle(nodes);
    }

    [TestMethod]
    public async Task OnBeforeDeleteCanVetoTheDeletion()
    {
        var a = new BlowNode { Id = "a" };
        var nodes = new List<BlowNode> { a };
        var cut = Render(nodes, extra: p => p.Add(
            c => c.OnBeforeDelete, (IReadOnlyList<BlowNode> _, IReadOnlyList<BlowEdge> _) => false));

        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync([a]));

        Assert.ContainsSingle(nodes);
    }

    [TestMethod]
    public async Task OnBeforeDeleteSeesTheCascadedEdges()
    {
        var a = new BlowNode { Id = "a" };
        var nodes = new List<BlowNode> { a };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        IReadOnlyList<BlowEdge> seen = [];

        var cut = Render(nodes, edges, p => p.Add(
            c => c.OnBeforeDelete, (IReadOnlyList<BlowNode> _, IReadOnlyList<BlowEdge> e) => { seen = e; return true; }));

        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync([a]));

        Assert.AreSequenceEqual(["e"], seen.Select(e => e.Id));
    }

    [TestMethod]
    public async Task DeleteEventsCarryWhatWasRemoved()
    {
        var a = new BlowNode { Id = "a" };
        var nodes = new List<BlowNode> { a };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };

        IReadOnlyList<BlowNode> deletedNodes = [];
        IReadOnlyList<BlowEdge> deletedEdges = [];

        var cut = Render(nodes, edges, p =>
        {
            p.Add(c => c.OnNodesDelete, EventCallback.Factory.Create<IReadOnlyList<BlowNode>>(this, n => deletedNodes = n));
            p.Add(c => c.OnEdgesDelete, EventCallback.Factory.Create<IReadOnlyList<BlowEdge>>(this, e => deletedEdges = e));
        });

        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync([a]));

        Assert.AreSequenceEqual(["a"], deletedNodes.Select(n => n.Id));
        Assert.AreSequenceEqual(["e"], deletedEdges.Select(e => e.Id));
    }

    // ---- connections ----

    [TestMethod]
    public async Task ConnectionValidityRejectsSelfConnectionsByDefault()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));

        Assert.IsFalse(cut.Instance.IsValidConnectionTarget("a", null, BlowHandleType.Target));
        Assert.IsTrue(cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target));
    }

    [TestMethod]
    public async Task SelfConnectionsAreAllowedBetweenDistinctHandlesWhenOptedIn()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" } };
        var cut = Render(nodes, extra: p => p.Add(c => c.AllowSelfConnections, true));

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", "out", BlowHandleType.Source, BlowPosition.Bottom));

        Assert.IsTrue(cut.Instance.IsValidConnectionTarget("a", "in", BlowHandleType.Target));
        Assert.IsFalse(cut.Instance.IsValidConnectionTarget("a", "out", BlowHandleType.Source));
    }

    [TestMethod]
    public async Task StrictModeRejectsSourceToSourceButLooseModeAllowsIt()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };

        var strict = Render(nodes);
        await strict.InvokeAsync(() => strict.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));
        Assert.IsFalse(strict.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Source));

        var loose = Render(nodes, extra: p => p.Add(c => c.ConnectionMode, BlowConnectionMode.Loose));
        await loose.InvokeAsync(() => loose.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));
        Assert.IsTrue(loose.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Source));
    }

    [TestMethod]
    public async Task ANonConnectableNodeCannotStartOrReceiveAConnection()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a" },
            new() { Id = "locked", Connectable = false },
        };
        var cut = Render(nodes);

        await cut.InvokeAsync(() => cut.Instance.StartConnection("locked", null, BlowHandleType.Source, BlowPosition.Bottom));
        Assert.IsFalse(cut.Instance.IsConnecting);

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));
        Assert.IsFalse(cut.Instance.IsValidConnectionTarget("locked", null, BlowHandleType.Target));
    }

    [TestMethod]
    public async Task NodesConnectableFalseDisablesConnectingEntirely()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var cut = Render(nodes, extra: p => p.Add(c => c.NodesConnectable, false));

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));

        Assert.IsFalse(cut.Instance.IsConnecting);
    }

    [TestMethod]
    public async Task DuplicateConnectionsAreRejected()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));

        Assert.IsFalse(cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target));
    }

    [TestMethod]
    public async Task DuplicateConnectionsAreAllowedWhenThePreventionIsTurnedOff()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var cut = Render(nodes, edges, p => p.Add(c => c.PreventDuplicateEdges, false));

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));

        Assert.IsTrue(cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target));
    }

    [TestMethod]
    public async Task IsValidConnectionPredicateIsHonoured()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" }, new() { Id = "c" } };
        var cut = Render(nodes, extra: p => p.Add(
            c => c.IsValidConnection, (BlowConnection conn) => conn.Target == "c"));

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));

        Assert.IsFalse(cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target));
        Assert.IsTrue(cut.Instance.IsValidConnectionTarget("c", null, BlowHandleType.Target));
    }

    [TestMethod]
    public async Task CompletingAConnectionAddsAnEdgeAndRaisesOnConnect()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var edges = new List<BlowEdge>();
        BlowConnection? connected = null;

        var cut = Render(nodes, edges, p => p.Add(
            c => c.OnConnect, EventCallback.Factory.Create<BlowConnection>(this, conn => connected = conn)));

        await cut.InvokeAsync(() =>
        {
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
            cut.Instance.CompleteConnection("b", null, BlowHandleType.Target);
        });

        Assert.ContainsSingle(edges);
        Assert.AreEqual("a", edges[0].Source);
        Assert.AreEqual("b", edges[0].Target);
        Assert.AreEqual("a", connected?.Source);
    }

    [TestMethod]
    public async Task CompletingOnAnInvalidTargetAddsNothing()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" } };
        var edges = new List<BlowEdge>();
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() =>
        {
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
            cut.Instance.CompleteConnection("a", null, BlowHandleType.Target); // self-connection
        });

        Assert.IsEmpty(edges);
        Assert.IsFalse(cut.Instance.IsConnecting);
    }

    [TestMethod]
    public async Task DefaultEdgeOptionsAreAppliedToNewEdges()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var edges = new List<BlowEdge>();
        var cut = Render(nodes, edges, p => p.Add(c => c.DefaultEdgeOptions, new BlowDefaultEdgeOptions
        {
            Animated = true,
            Type = BlowEdgeType.SmoothStep,
            Stroke = "#f00",
            StrokeWidth = 3,
        }));

        await cut.InvokeAsync(() =>
        {
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
            cut.Instance.CompleteConnection("b", null, BlowHandleType.Target);
        });

        var edge = Assert.ContainsSingle(edges);
        Assert.IsTrue(edge.Animated);
        Assert.AreEqual(BlowEdgeType.SmoothStep, edge.Type);
        Assert.AreEqual("#f00", edge.Stroke);
        Assert.AreEqual(3, edge.StrokeWidth);
    }

    [TestMethod]
    public async Task DescribeConnectionNormalisesTargetInitiatedDrags()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var cut = Render(nodes);

        // Dragging *from* a target handle means the other end becomes the source.
        await cut.InvokeAsync(() => cut.Instance.StartConnection("b", "in", BlowHandleType.Target, BlowPosition.Top));
        var connection = cut.Instance.DescribeConnection("a", "out", BlowHandleType.Source);

        Assert.AreEqual("a", connection.Source);
        Assert.AreEqual("out", connection.SourceHandle);
        Assert.AreEqual("b", connection.Target);
        Assert.AreEqual("in", connection.TargetHandle);
    }

    // ---- viewport ----

    [TestMethod]
    public async Task SetViewportClampsZoomToTheConfiguredRange()
    {
        var cut = Render([new BlowNode { Id = "a" }], extra: p =>
        {
            p.Add(c => c.MinZoom, 0.5);
            p.Add(c => c.MaxZoom, 2);
        });

        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(0, 0, 99)));
        Assert.AreEqual(2, cut.Instance.GetViewport().Zoom);

        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(0, 0, 0.01)));
        Assert.AreEqual(0.5, cut.Instance.GetViewport().Zoom);
    }

    [TestMethod]
    public async Task ZoomInAndOutStayWithinTheConfiguredRange()
    {
        var cut = Render([new BlowNode { Id = "a" }], extra: p =>
        {
            p.Add(c => c.MinZoom, 0.5);
            p.Add(c => c.MaxZoom, 1.5);
        });

        for (var i = 0; i < 20; i++) await cut.InvokeAsync(() => cut.Instance.ZoomIn());
        Assert.AreEqual(1.5, cut.Instance.GetZoom(), 1e-6);

        for (var i = 0; i < 40; i++) await cut.InvokeAsync(() => cut.Instance.ZoomOut());
        Assert.AreEqual(0.5, cut.Instance.GetZoom(), 1e-6);
    }

    [TestMethod]
    public async Task ScreenAndFlowConversionsAreInverses()
    {
        var cut = Render([new BlowNode { Id = "a" }]);
        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(30, -15, 1.25)));

        var flow = cut.Instance.ScreenToFlowPosition(200, 120);
        var screen = cut.Instance.FlowToScreenPosition(flow);

        Assert.AreEqual(200, screen.X, 1e-6);
        Assert.AreEqual(120, screen.Y, 1e-6);
    }

    // ---- imperative API ----

    [TestMethod]
    public async Task AddNodesAndAddEdgesAppendToTheBoundLists()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" } };
        var edges = new List<BlowEdge>();
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() =>
        {
            cut.Instance.AddNodes(new BlowNode { Id = "b" });
            cut.Instance.AddEdges(new BlowEdge { Id = "e", Source = "a", Target = "b" });
        });

        Assert.AreSequenceEqual(["a", "b"], nodes.Select(n => n.Id));
        Assert.AreSequenceEqual(["e"], edges.Select(e => e.Id));
        Assert.IsNotNull(cut.Instance.GetNode("b"));
        Assert.IsNotNull(cut.Instance.GetEdge("e"));
        Assert.IsNull(cut.Instance.GetNode("missing"));
    }

    [TestMethod]
    public void GetIntersectingNodesRespectsPartialVersusFullContainment()
    {
        var inside = new BlowNode { Id = "inside", Position = new(10, 10), Width = 20, Height = 20 };
        var straddling = new BlowNode { Id = "straddling", Position = new(90, 10), Width = 40, Height = 20 };
        var outside = new BlowNode { Id = "outside", Position = new(500, 500), Width = 20, Height = 20 };
        var cut = Render([inside, straddling, outside]);

        var area = new BlowRect(0, 0, 100, 100);

        Assert.AreSequenceEqual(["inside", "straddling"], cut.Instance.GetIntersectingNodes(area).Select(n => n.Id));
        Assert.AreSequenceEqual(["inside"], cut.Instance.GetIntersectingNodes(area, partially: false).Select(n => n.Id));
    }

    [TestMethod]
    public void GetIntersectingNodesForANodeExcludesTheNodeItself()
    {
        var a = new BlowNode { Id = "a", Position = new(0, 0), Width = 100, Height = 100 };
        var b = new BlowNode { Id = "b", Position = new(50, 50), Width = 100, Height = 100 };
        var cut = Render([a, b]);

        Assert.AreSequenceEqual(["b"], cut.Instance.GetIntersectingNodes(a).Select(n => n.Id));
    }

    [TestMethod]
    public async Task ToObjectSnapshotsNodesEdgesAndViewport()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" } };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "a" } };
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(5, 6, 1)));
        var snapshot = cut.Instance.ToObject();

        Assert.ContainsSingle(snapshot.Nodes);
        Assert.ContainsSingle(snapshot.Edges);
        Assert.AreEqual(new BlowViewport(5, 6, 1), snapshot.Viewport);
    }

    [TestMethod]
    public void GetNodeConnectionsReadsThroughToTheEdgeList()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", SourceHandle = "out" } };
        var cut = Render(nodes, edges);

        Assert.ContainsSingle(cut.Instance.GetNodeConnections("a"));
        Assert.ContainsSingle(cut.Instance.GetHandleConnections("a", BlowHandleType.Source, "out"));
        Assert.IsEmpty(cut.Instance.GetHandleConnections("a", BlowHandleType.Target, "out"));
    }
}
