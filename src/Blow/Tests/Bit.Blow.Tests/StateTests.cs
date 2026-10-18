using Bunit;

namespace Bit.Blow.Tests;

[TestClass]
public class StateTests : BunitContext
{
    public StateTests()
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

    // ---- cloning ----

    [TestMethod]
    public void CloningANodeCopiesEveryField()
    {
        var original = new BlowNode
        {
            Id = "a",
            Position = new(10, 20),
            Type = "custom",
            Label = "Label",
            Data = new { Value = 1 },
            Width = 120,
            Height = 60,
            Selected = true,
            Draggable = false,
            Selectable = false,
            Connectable = false,
            Hidden = true,
            Deletable = false,
            SourcePosition = BlowPosition.Right,
            TargetPosition = BlowPosition.Left,
            UseCustomDragHandle = true,
            Focusable = false,
            AriaLabel = "aria",
            ParentId = "p",
            ExtentParent = true,
            Extent = new BlowRect(1, 2, 3, 4),
            ExpandParent = true,
            Class = "cls",
            Style = "color:red",
            ZIndex = 9,
            Origin = new(0.5, 0.5),
        };

        var clone = original.Clone();

        Assert.AreNotSame(original, clone);
        Assert.AreEqual(original.Id, clone.Id);
        Assert.AreEqual(original.Position, clone.Position);
        Assert.AreEqual(original.Type, clone.Type);
        Assert.AreEqual(original.Label, clone.Label);
        Assert.AreSame(original.Data, clone.Data);
        Assert.AreEqual(original.Width, clone.Width);
        Assert.AreEqual(original.Height, clone.Height);
        Assert.AreEqual(original.Selected, clone.Selected);
        Assert.AreEqual(original.Draggable, clone.Draggable);
        Assert.AreEqual(original.Selectable, clone.Selectable);
        Assert.AreEqual(original.Connectable, clone.Connectable);
        Assert.AreEqual(original.Hidden, clone.Hidden);
        Assert.AreEqual(original.Deletable, clone.Deletable);
        Assert.AreEqual(original.SourcePosition, clone.SourcePosition);
        Assert.AreEqual(original.TargetPosition, clone.TargetPosition);
        Assert.AreEqual(original.UseCustomDragHandle, clone.UseCustomDragHandle);
        Assert.AreEqual(original.Focusable, clone.Focusable);
        Assert.AreEqual(original.AriaLabel, clone.AriaLabel);
        Assert.AreEqual(original.ParentId, clone.ParentId);
        Assert.AreEqual(original.ExtentParent, clone.ExtentParent);
        Assert.AreEqual(original.Extent, clone.Extent);
        Assert.AreEqual(original.ExpandParent, clone.ExpandParent);
        Assert.AreEqual(original.Class, clone.Class);
        Assert.AreEqual(original.Style, clone.Style);
        Assert.AreEqual(original.ZIndex, clone.ZIndex);
        Assert.AreEqual(original.Origin, clone.Origin);
    }

    [TestMethod]
    public void CloningANodeCanGiveItANewId()
    {
        var clone = new BlowNode { Id = "a", Label = "x" }.Clone("b");

        Assert.AreEqual("b", clone.Id);
        Assert.AreEqual("x", clone.Label);
    }

    [TestMethod]
    public void CloningAnEdgeCopiesEveryField()
    {
        var original = new BlowEdge
        {
            Id = "e",
            Source = "a",
            Target = "b",
            SourceHandle = "out",
            TargetHandle = "in",
            Type = BlowEdgeType.SmoothStep,
            PathOptions = new BlowEdgePathOptions { BorderRadius = 3 },
            Label = "label",
            Animated = true,
            Selected = true,
            Hidden = true,
            Selectable = false,
            Focusable = false,
            Deletable = false,
            Reconnectable = false,
            StrokeWidth = 4,
            Stroke = "#f00",
            MarkerEnd = false,
            MarkerStart = true,
            MarkerType = BlowMarkerType.ArrowClosed,
            Class = "cls",
            Style = "opacity:.5",
            InteractionWidth = 30,
            ZIndex = 2,
            Data = new { X = 1 },
        };

        var clone = original.Clone();

        Assert.AreNotSame(original, clone);
        Assert.AreEqual(original.Source, clone.Source);
        Assert.AreEqual(original.Target, clone.Target);
        Assert.AreEqual(original.SourceHandle, clone.SourceHandle);
        Assert.AreEqual(original.TargetHandle, clone.TargetHandle);
        Assert.AreEqual(original.Type, clone.Type);
        // Copied, not shared: retuning the clone must not retune the original.
        Assert.AreNotSame(original.PathOptions, clone.PathOptions);
        Assert.AreEqual(original.PathOptions!.BorderRadius, clone.PathOptions!.BorderRadius);
        Assert.AreEqual(original.Label, clone.Label);
        Assert.AreEqual(original.Animated, clone.Animated);
        Assert.AreEqual(original.Selected, clone.Selected);
        Assert.AreEqual(original.Hidden, clone.Hidden);
        Assert.AreEqual(original.Selectable, clone.Selectable);
        Assert.AreEqual(original.Focusable, clone.Focusable);
        Assert.AreEqual(original.Deletable, clone.Deletable);
        Assert.AreEqual(original.Reconnectable, clone.Reconnectable);
        Assert.AreEqual(original.StrokeWidth, clone.StrokeWidth);
        Assert.AreEqual(original.Stroke, clone.Stroke);
        Assert.AreEqual(original.MarkerEnd, clone.MarkerEnd);
        Assert.AreEqual(original.MarkerStart, clone.MarkerStart);
        Assert.AreEqual(original.MarkerType, clone.MarkerType);
        Assert.AreEqual(original.Class, clone.Class);
        Assert.AreEqual(original.Style, clone.Style);
        Assert.AreEqual(original.InteractionWidth, clone.InteractionWidth);
        Assert.AreEqual(original.ZIndex, clone.ZIndex);
        Assert.AreSame(original.Data, clone.Data);
    }

    // ---- snapshots ----

    [TestMethod]
    public void ADeepSnapshotDoesNotChangeWhenTheGraphIsEditedAfterwards()
    {
        var node = new BlowNode { Id = "a", Label = "before", Position = new(0, 0) };
        var nodes = new List<BlowNode> { node };
        var cut = Render(nodes);

        var snapshot = cut.Instance.ToObject();
        node.Label = "after";
        node.Position = new(999, 999);
        nodes.Add(new BlowNode { Id = "b" });

        Assert.ContainsSingle(snapshot.Nodes);
        Assert.AreEqual("before", snapshot.Nodes[0].Label);
        Assert.AreEqual(new BlowXYPosition(0, 0), snapshot.Nodes[0].Position);
    }

    [TestMethod]
    public void AShallowSnapshotHandsBackTheLiveInstances()
    {
        var node = new BlowNode { Id = "a", Label = "before" };
        var cut = Render([node]);

        var snapshot = cut.Instance.ToObject(deep: false);
        node.Label = "after";

        Assert.AreSame(node, snapshot.Nodes[0]);
        Assert.AreEqual("after", snapshot.Nodes[0].Label);
    }

    // ---- controlled viewport ----

    [TestMethod]
    public void AControlledViewportIsAdoptedFromTheParameter()
    {
        var cut = Render([new BlowNode { Id = "a" }], extra: p => p.Add(c => c.Viewport, new BlowViewport(10, 20, 1.5)));

        Assert.AreEqual(new BlowViewport(10, 20, 1.5), cut.Instance.GetViewport());
    }

    [TestMethod]
    public void UpdatingTheControlledViewportParameterMovesTheFlow()
    {
        var cut = Render([new BlowNode { Id = "a" }], extra: p => p.Add(c => c.Viewport, new BlowViewport(0, 0, 1)));

        cut.Render(p => p.Add(c => c.Viewport, new BlowViewport(50, 60, 2)));

        Assert.AreEqual(new BlowViewport(50, 60, 2), cut.Instance.GetViewport());
    }

    [TestMethod]
    public void AControlledViewportZoomIsClampedToTheConfiguredRange()
    {
        var cut = Render([new BlowNode { Id = "a" }], extra: p =>
        {
            p.Add(c => c.MaxZoom, 2.0);
            p.Add(c => c.Viewport, new BlowViewport(0, 0, 50));
        });

        Assert.AreEqual(2, cut.Instance.GetViewport().Zoom);
    }

    [TestMethod]
    public async Task ARerenderWithAnUnchangedControlledViewportDoesNotUndoAUserPan()
    {
        var controlled = new BlowViewport(0, 0, 1);
        var cut = Render([new BlowNode { Id = "a" }], extra: p => p.Add(c => c.Viewport, controlled));

        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(75, 0, 1)));
        cut.Render(p => p.Add(c => c.Viewport, controlled));

        Assert.AreEqual(75, cut.Instance.GetViewport().X);
    }

    [TestMethod]
    public async Task LeavingTheViewportParameterNullKeepsTheFlowUncontrolled()
    {
        var cut = Render([new BlowNode { Id = "a" }]);

        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(12, 34, 1)));
        cut.Render(p => p.Add(c => c.ElementsSelectable, true));

        Assert.AreEqual(new BlowViewport(12, 34, 1), cut.Instance.GetViewport());
    }

    // ---- default edge options ----

    [TestMethod]
    public async Task DefaultEdgeOptionsCarryPathOptionsAndStyleOntoNewEdges()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var edges = new List<BlowEdge>();
        var pathOptions = new BlowEdgePathOptions { BorderRadius = 12 };

        var cut = Render(nodes, edges, p => p.Add(c => c.DefaultEdgeOptions, new BlowDefaultEdgeOptions
        {
            Type = BlowEdgeType.SmoothStep,
            PathOptions = pathOptions,
            Style = "opacity:0.5",
            InteractionWidth = 24,
        }));

        await cut.InvokeAsync(() =>
        {
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
            cut.Instance.CompleteConnection("b", null, BlowHandleType.Target);
        });

        var edge = Assert.ContainsSingle(edges);
        // Each new edge gets its own copy, so tuning one edge does not retune every other.
        Assert.AreNotSame(pathOptions, edge.PathOptions);
        Assert.AreEqual(pathOptions.BorderRadius, edge.PathOptions!.BorderRadius);
        Assert.AreEqual("opacity:0.5", edge.Style);
        Assert.AreEqual(24, edge.InteractionWidth);
    }

    // ---- resize observation ----

    [TestMethod]
    public async Task TheResizeCallbackRemeasuresWithoutThrowing()
    {
        // The JS side calls this whenever the pane or a node changes size; under bUnit's loose
        // interop it returns no measurements, which is the "nothing to update" path.
        var cut = Render([new BlowNode { Id = "a" }]);

        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved());

        Assert.ContainsSingle(cut.FindAll(".bit-blow-node"));
    }
}
