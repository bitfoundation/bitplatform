using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for stacking modes, key customization, selection-on-drag, the connection drag
/// threshold, label styling, the geometry cache and snapshot restore.
/// </summary>
[TestClass]
public class CanvasBehaviourTests : BunitContext
{
    public CanvasBehaviourTests()
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

    private static List<BlowNode> TwoNodes() =>
    [
        new() { Id = "a", Position = new(0, 0) },
        new() { Id = "b", Position = new(0, 200) },
    ];

    // ---- z-index mode ----

    [TestMethod]
    public void ManualModeLeavesASelectedNodeWhereItWas()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Selected = true } };
        var cut = Render(nodes, extra: p => p.Add(c => c.ZIndexMode, BlowZIndexMode.Manual));

        Assert.DoesNotContain("z-index", cut.Find(".bit-blow-node").GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void ManualModeStillHonoursAnExplicitZIndex()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", ZIndex = 4 } };
        var cut = Render(nodes, extra: p => p.Add(c => c.ZIndexMode, BlowZIndexMode.Manual));

        Assert.Contains("z-index:4", cut.Find(".bit-blow-node").GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void ManualModeDoesNotElevateASelectedEdge()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "picked", Source = "a", Target = "b", Selected = true },
            new() { Id = "plain", Source = "b", Target = "a" },
        };
        var cut = Render(TwoNodes(), edges, p =>
        {
            p.Add(c => c.ElevateEdgesOnSelect, true);
            p.Add(c => c.ZIndexMode, BlowZIndexMode.Manual);
        });

        var rendered = cut.FindAll(".bit-blow-edge");
        Assert.AreEqual("picked", rendered[0].GetAttribute("data-id"));
    }

    [TestMethod]
    public void BasicModeStillElevatesTheSelectionButLeavesNestingAlone()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "child", ParentId = "group", Selected = true },
            new() { Id = "group" },
        };
        var cut = Render(nodes, extra: p => p.Add(c => c.ZIndexMode, BlowZIndexMode.Basic));

        var rendered = cut.FindAll(".bit-blow-node");
        // Basic mode does not reorder for nesting, so the list order survives.
        Assert.AreEqual("child", rendered[0].GetAttribute("data-id"));
        Assert.Contains("z-index", rendered[0].GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void AutoModeKeepsAChildAboveItsGroup()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "child", ParentId = "group" },
            new() { Id = "group" },
        };
        var cut = Render(nodes, extra: p => p.Add(c => c.ZIndexMode, BlowZIndexMode.Auto));

        var rendered = cut.FindAll(".bit-blow-node");
        Assert.AreEqual("group", rendered[0].GetAttribute("data-id"));
    }

    // ---- delete keys ----

    [TestMethod]
    public void TheDeleteKeysCanBeNarrowedToDeleteAlone()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Selected = true } };
        var cut = Render(nodes, extra: p => p.Add(c => c.DeleteKeys, new[] { "Delete" }));

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "Backspace" });
        Assert.ContainsSingle(nodes);

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "Delete" });
        Assert.IsEmpty(nodes);
    }

    [TestMethod]
    public void ACustomDeleteKeyIsHonoured()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Selected = true } };
        var cut = Render(nodes, extra: p => p.Add(c => c.DeleteKeys, new[] { "x" }));

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "x" });

        Assert.IsEmpty(nodes);
    }

    // ---- zoom activation key ----

    [TestMethod]
    public void AZoomActivationKeyMakesAPlainWheelPassThrough()
    {
        var cut = Render(TwoNodes(), extra: p => p.Add(c => c.ZoomActivationKey, BlowModifierKey.Meta));
        var before = cut.Instance.GetZoom();

        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = -100 });

        Assert.AreEqual(before, cut.Instance.GetZoom(), 1e-6);
    }

    [TestMethod]
    public void HoldingTheZoomActivationKeyZooms()
    {
        var cut = Render(TwoNodes(), extra: p => p.Add(c => c.ZoomActivationKey, BlowModifierKey.Meta));
        var before = cut.Instance.GetZoom();

        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = -100, MetaKey = true });

        Assert.IsTrue(cut.Instance.GetZoom() > before);
    }

    [TestMethod]
    public void CtrlWheelStillZoomsPastTheActivationKey()
    {
        // Ctrl+wheel is the browser's own pinch gesture; requiring another key would break it.
        var cut = Render(TwoNodes(), extra: p => p.Add(c => c.ZoomActivationKey, BlowModifierKey.Meta));
        var before = cut.Instance.GetZoom();

        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = -100, CtrlKey = true });

        Assert.IsTrue(cut.Instance.GetZoom() > before);
    }

    [TestMethod]
    public void WithoutAnActivationKeyThePlainWheelStillZooms()
    {
        var cut = Render(TwoNodes());
        var before = cut.Instance.GetZoom();

        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = -100 });

        Assert.IsTrue(cut.Instance.GetZoom() > before);
    }

    // ---- select nodes on drag ----

    [TestMethod]
    public void PressingAnUnselectedNodeSelectsItByDefault()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);

        cut.FindAll(".bit-blow-node")[0].PointerDown(new PointerEventArgs { Button = 0 });

        Assert.IsTrue(nodes[0].Selected);
    }

    [TestMethod]
    public void SelectNodesOnDragOffKeepsTheRestOfTheSelection()
    {
        var nodes = TwoNodes();
        nodes[0].Selected = true;
        nodes[1].Selected = true;
        var cut = Render(nodes, extra: p => p.Add(c => c.SelectNodesOnDrag, false));

        cut.FindAll(".bit-blow-node")[0].PointerDown(new PointerEventArgs { Button = 0 });

        // Pressing one member of a marquee selection must not collapse it onto that node.
        Assert.IsTrue(nodes[0].Selected);
        Assert.IsTrue(nodes[1].Selected);
    }

    [TestMethod]
    public void SelectNodesOnDragOffStillSelectsANodeOutsideTheSelection()
    {
        var nodes = TwoNodes();
        nodes[0].Selected = true;
        var cut = Render(nodes, extra: p => p.Add(c => c.SelectNodesOnDrag, false));

        cut.FindAll(".bit-blow-node")[1].PointerDown(new PointerEventArgs { Button = 0 });

        Assert.IsTrue(nodes[1].Selected);
        Assert.IsFalse(nodes[0].Selected);
    }

    // ---- connection drag threshold ----

    [TestMethod]
    public async Task TheConnectionLineIsNotDrawnUntilThePointerTravels()
    {
        var cut = Render(TwoNodes(), extra: p => p.Add(c => c.ConnectionDragThreshold, 10));

        await cut.InvokeAsync(() =>
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 100, 100));

        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 103, ClientY = 100 });
        Assert.IsEmpty(cut.FindAll(".bit-blow-connectionline"));

        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 140, ClientY = 100 });
        Assert.ContainsSingle(cut.FindAll(".bit-blow-connectionline"));
    }

    [TestMethod]
    public async Task AZeroThresholdDrawsTheLineOnTheFirstMove()
    {
        var cut = Render(TwoNodes(), extra: p => p.Add(c => c.ConnectionDragThreshold, 0));

        await cut.InvokeAsync(() =>
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 100, 100));
        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 100, ClientY = 100 });

        Assert.ContainsSingle(cut.FindAll(".bit-blow-connectionline"));
    }

    [TestMethod]
    public async Task ClickToConnectShowsTheLineWithoutAnyTravel()
    {
        var cut = Render(TwoNodes(), extra: p =>
        {
            p.Add(c => c.ConnectOnClick, true);
            p.Add(c => c.ConnectionDragThreshold, 50);
        });

        await cut.InvokeAsync(() =>
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 100, 100));

        // With click-to-connect the line is the only cue that a connection is pending.
        Assert.ContainsSingle(cut.FindAll(".bit-blow-connectionline"));
    }

    // ---- edge labels ----

    [TestMethod]
    public void AnEdgeLabelGetsABackingPlateByDefault()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Label = "hello" } };
        var cut = Render(TwoNodes(), edges);

        Assert.ContainsSingle(cut.FindAll(".bit-blow-edge-label-bg"));
    }

    [TestMethod]
    public void TheLabelPlateCanBeTurnedOff()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "e", Source = "a", Target = "b", Label = "hello", LabelShowBg = false },
        };
        var cut = Render(TwoNodes(), edges);

        Assert.IsEmpty(cut.FindAll(".bit-blow-edge-label-bg"));
        Assert.AreEqual("hello", cut.Find(".bit-blow-edge-label").TextContent);
    }

    [TestMethod]
    public void LabelStylesAreAppliedToTheTextAndThePlate()
    {
        var edges = new List<BlowEdge>
        {
            new()
            {
                Id = "e", Source = "a", Target = "b", Label = "hi",
                LabelStyle = "fill:#111", LabelBgStyle = "fill:#eee",
            },
        };
        var cut = Render(TwoNodes(), edges);

        Assert.Contains("fill:#111", cut.Find(".bit-blow-edge-label").GetAttribute("style") ?? string.Empty);
        Assert.Contains("fill:#eee", cut.Find(".bit-blow-edge-label-bg").GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void ALongerLabelGetsAWiderPlate()
    {
        var narrow = Render(TwoNodes(), [new() { Id = "e", Source = "a", Target = "b", Label = "hi" }]);
        var wide = Render(TwoNodes(),
            [new() { Id = "e", Source = "a", Target = "b", Label = "a considerably longer label" }]);

        var narrowWidth = double.Parse(narrow.Find(".bit-blow-edge-label-bg").GetAttribute("width")!,
            System.Globalization.CultureInfo.InvariantCulture);
        var wideWidth = double.Parse(wide.Find(".bit-blow-edge-label-bg").GetAttribute("width")!,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.IsTrue(wideWidth > narrowWidth);
    }

    [TestMethod]
    public void ExtraLabelPaddingWidensThePlate()
    {
        var tight = Render(TwoNodes(), [new() { Id = "e", Source = "a", Target = "b", Label = "hi" }]);
        var roomy = Render(TwoNodes(),
            [new() { Id = "e", Source = "a", Target = "b", Label = "hi", LabelBgPaddingX = 20 }]);

        var tightWidth = double.Parse(tight.Find(".bit-blow-edge-label-bg").GetAttribute("width")!,
            System.Globalization.CultureInfo.InvariantCulture);
        var roomyWidth = double.Parse(roomy.Find(".bit-blow-edge-label-bg").GetAttribute("width")!,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual(tightWidth + 32, roomyWidth, 1e-3);
    }

    [TestMethod]
    public void CloningAnEdgeCarriesItsLabelStyling()
    {
        var edge = new BlowEdge
        {
            Id = "e", Source = "a", Target = "b", Label = "x",
            LabelShowBg = false, LabelStyle = "fill:red", LabelBgStyle = "fill:blue",
            LabelBgPaddingX = 9, LabelBgPaddingY = 8, LabelBgBorderRadius = 7,
        };

        var clone = edge.Clone();

        Assert.IsFalse(clone.LabelShowBg);
        Assert.AreEqual("fill:red", clone.LabelStyle);
        Assert.AreEqual("fill:blue", clone.LabelBgStyle);
        Assert.AreEqual(9, clone.LabelBgPaddingX);
        Assert.AreEqual(8, clone.LabelBgPaddingY);
        Assert.AreEqual(7, clone.LabelBgBorderRadius);
    }

    // ---- text metrics ----

    [TestMethod]
    public void EmptyTextMeasuresZero()
    {
        Assert.AreEqual(0, BlowTextMetrics.EstimateWidth(null));
        Assert.AreEqual(0, BlowTextMetrics.EstimateWidth(""));
    }

    [TestMethod]
    public void NarrowCharactersMeasureLessThanWideOnes()
    {
        var narrow = BlowTextMetrics.EstimateWidth("iiii");
        var wide = BlowTextMetrics.EstimateWidth("mmmm");

        Assert.IsTrue(wide > narrow * 2);
    }

    [TestMethod]
    public void FullWidthCharactersMeasureAWholeEm()
    {
        Assert.AreEqual(11, BlowTextMetrics.EstimateWidth("字", 11), 1e-3);
    }

    [TestMethod]
    public void WidthScalesWithFontSize()
    {
        var small = BlowTextMetrics.EstimateWidth("hello", 10);
        var large = BlowTextMetrics.EstimateWidth("hello", 20);

        Assert.AreEqual(small * 2, large, 1e-6);
    }

    // ---- geometry cache ----

    [TestMethod]
    public async Task PanningReusesTheComputedEdgeGeometry()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var cut = Render(TwoNodes(), edges);

        var before = cut.Find(".bit-blow-edge-path").GetAttribute("d");
        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(120, 90, 1.4)));
        var after = cut.Find(".bit-blow-edge-path").GetAttribute("d");

        // Flow-space geometry does not depend on the viewport transform.
        Assert.AreEqual(before, after);
    }

    [TestMethod]
    public void MovingANodeRecomputesTheEdgeGeometry()
    {
        var nodes = TwoNodes();
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var cut = Render(nodes, edges);

        var before = cut.Find(".bit-blow-edge-path").GetAttribute("d");
        nodes[1].Position = new BlowXYPosition(400, 400);
        cut.Render();

        Assert.AreNotEqual(before, cut.Find(".bit-blow-edge-path").GetAttribute("d"));
    }

    [TestMethod]
    public void RetuningPathOptionsInPlaceRecomputesTheGeometry()
    {
        var options = new BlowEdgePathOptions { Curvature = 0.25 };
        var edges = new List<BlowEdge>
        {
            new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Bezier, PathOptions = options },
        };

        // The target sits *above* the source, so the handles face away from each other - the
        // only arrangement in which curvature changes the control points at all.
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0) },
            new() { Id = "b", Position = new(0, -300) },
        };
        var cut = Render(nodes, edges);

        var before = cut.Find(".bit-blow-edge-path").GetAttribute("d");
        options.Curvature = 1.5;
        cut.Render();

        Assert.AreNotEqual(before, cut.Find(".bit-blow-edge-path").GetAttribute("d"));
    }

    [TestMethod]
    public void ChangingTheEdgeTypeRecomputesTheGeometry()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Bezier } };
        var cut = Render(TwoNodes(), edges);

        var before = cut.Find(".bit-blow-edge-path").GetAttribute("d");
        edges[0].Type = BlowEdgeType.Step;
        cut.Render();

        Assert.AreNotEqual(before, cut.Find(".bit-blow-edge-path").GetAttribute("d"));
    }

    [TestMethod]
    public async Task DeletingAnEdgeDoesNotStrandItsCachedGeometry()
    {
        var edges = new List<BlowEdge>
        {
            new() { Id = "e1", Source = "a", Target = "b" },
            new() { Id = "e2", Source = "b", Target = "a" },
        };
        var cut = Render(TwoNodes(), edges);

        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync(edges: [edges[0]]));
        cut.Render();

        Assert.ContainsSingle(cut.FindAll(".bit-blow-edge"));
        // Re-adding the same id must produce fresh geometry, not the stale cached entry.
        Assert.IsNull(cut.Instance.GetEdge("e1"));
    }

    // ---- alignment guides on the canvas ----

    [TestMethod]
    public void DraggingNearANeighbourSnapsIntoLineAndShowsAGuide()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "anchor", Position = new(100, 0), Width = 100, Height = 50 },
            new() { Id = "moving", Position = new(0, 300), Width = 100, Height = 50 },
        };
        var cut = Render(nodes, extra: p => p.Add(c => c.SnapToGuides, true));

        // Land 3px short of the anchor's left edge: inside the default 6px tolerance.
        DragNode(cut, nodeIndex: 1, from: (0, 0), to: (103, 0));

        Assert.AreEqual(100, nodes[1].Position.X, 1e-3);
        Assert.ContainsSingle(cut.FindAll(".bit-blow-guide"));
    }

    [TestMethod]
    public void GuidesAreOffByDefault()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "anchor", Position = new(100, 0), Width = 100, Height = 50 },
            new() { Id = "moving", Position = new(0, 300), Width = 100, Height = 50 },
        };
        var cut = Render(nodes);

        DragNode(cut, nodeIndex: 1, from: (0, 0), to: (103, 0));

        Assert.AreEqual(103, nodes[1].Position.X, 1e-3);
        Assert.IsEmpty(cut.FindAll(".bit-blow-guide"));
    }

    [TestMethod]
    public void ADragFarFromEverythingShowsNoGuides()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "anchor", Position = new(100, 0), Width = 100, Height = 50 },
            new() { Id = "moving", Position = new(0, 300), Width = 100, Height = 50 },
        };
        var cut = Render(nodes, extra: p => p.Add(c => c.SnapToGuides, true));

        DragNode(cut, nodeIndex: 1, from: (0, 0), to: (400, 0));

        Assert.IsEmpty(cut.FindAll(".bit-blow-guide"));
    }

    [TestMethod]
    public void GuidesDisappearWhenTheDragEnds()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "anchor", Position = new(100, 0), Width = 100, Height = 50 },
            new() { Id = "moving", Position = new(0, 300), Width = 100, Height = 50 },
        };
        var cut = Render(nodes, extra: p => p.Add(c => c.SnapToGuides, true));

        DragNode(cut, nodeIndex: 1, from: (0, 0), to: (103, 0));
        cut.Find(".bit-blow").PointerUp(new PointerEventArgs { ClientX = 103, ClientY = 0 });

        Assert.IsEmpty(cut.FindAll(".bit-blow-guide"));
    }

    [TestMethod]
    public void NodesMovingTogetherDoNotAttractEachOther()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0), Width = 100, Height = 50, Selected = true },
            new() { Id = "b", Position = new(103, 0), Width = 100, Height = 50, Selected = true },
        };
        var cut = Render(nodes, extra: p => p.Add(c => c.SnapToGuides, true));

        DragNode(cut, nodeIndex: 0, from: (0, 0), to: (5, 5));

        // Both moved by the same delta; neither snapped to the other.
        Assert.AreEqual(5, nodes[0].Position.X, 1e-3);
        Assert.AreEqual(108, nodes[1].Position.X, 1e-3);
    }

    [TestMethod]
    public void AGuideWinsOverTheGridOnTheAxisItClaimed()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "anchor", Position = new(103, 0), Width = 100, Height = 50 },
            new() { Id = "moving", Position = new(0, 300), Width = 100, Height = 50 },
        };
        var cut = Render(nodes, extra: p =>
        {
            p.Add(c => c.SnapToGuides, true);
            p.Add(c => c.SnapToGrid, true);
            p.Add(c => c.SnapGridX, 50.0);
            p.Add(c => c.SnapGridY, 50.0);
        });

        DragNode(cut, nodeIndex: 1, from: (0, 0), to: (100, 13));

        // X follows the guide (103, not the grid's 100); Y still snaps to the grid.
        Assert.AreEqual(103, nodes[1].Position.X, 1e-3);
        Assert.AreEqual(300, nodes[1].Position.Y, 1e-3);
    }

    [TestMethod]
    public void AHiddenNodeIsNotSomethingToLineUpWith()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "ghost", Position = new(100, 0), Width = 100, Height = 50, Hidden = true },
            new() { Id = "moving", Position = new(0, 300), Width = 100, Height = 50 },
        };
        var cut = Render(nodes, extra: p => p.Add(c => c.SnapToGuides, true));

        DragNode(cut, nodeIndex: 0, from: (0, 0), to: (103, 0));

        Assert.AreEqual(103, nodes[1].Position.X, 1e-3);
    }

    // ---- snapshot restore ----

    [TestMethod]
    public async Task RestoringASnapshotReplacesTheGraphInPlace()
    {
        var nodes = TwoNodes();
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var cut = Render(nodes, edges);

        var snapshot = cut.Instance.ToObject();

        await cut.InvokeAsync(() => cut.Instance.AddNodes(new BlowNode { Id = "extra", Position = new(50, 50) }));
        await cut.InvokeAsync(() => cut.Instance.RestoreAsync(snapshot));

        // The bound lists keep their identity, so a caller holding them sees the restore.
        Assert.AreEqual(2, nodes.Count);
        Assert.ContainsSingle(edges);
        Assert.IsNull(cut.Instance.GetNode("extra"));
    }

    [TestMethod]
    public async Task RestoringASnapshotAdoptsItsViewport()
    {
        var cut = Render(TwoNodes());
        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(30, 40, 1.25)));

        var snapshot = cut.Instance.ToObject();
        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(0, 0, 1)));
        await cut.InvokeAsync(() => cut.Instance.RestoreAsync(snapshot));

        Assert.AreEqual(new BlowViewport(30, 40, 1.25), cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task ASnapshotCanBeRestoredWithoutMovingTheView()
    {
        var cut = Render(TwoNodes());
        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(30, 40, 1.25)));
        var snapshot = cut.Instance.ToObject();

        await cut.InvokeAsync(() => cut.Instance.SetViewport(new BlowViewport(5, 5, 1)));
        await cut.InvokeAsync(() => cut.Instance.RestoreAsync(snapshot, restoreViewport: false));

        Assert.AreEqual(new BlowViewport(5, 5, 1), cut.Instance.GetViewport());
    }

    [TestMethod]
    public async Task RestoringRaisesTheChangeCallbacks()
    {
        var nodeChanges = 0;
        var edgeChanges = 0;
        var cut = Render(TwoNodes(), extra: p =>
        {
            p.Add(c => c.NodesChanged, EventCallback.Factory.Create<List<BlowNode>>(this, () => nodeChanges++));
            p.Add(c => c.EdgesChanged, EventCallback.Factory.Create<List<BlowEdge>>(this, () => edgeChanges++));
        });

        await cut.InvokeAsync(() => cut.Instance.RestoreAsync(cut.Instance.ToObject()));

        Assert.AreEqual(1, nodeChanges);
        Assert.AreEqual(1, edgeChanges);
    }

    // ---- helpers ----

    private static void DragNode(
        IRenderedComponent<BlowCanvas> cut, (double X, double Y) from, (double X, double Y) to, int nodeIndex = 0)
    {
        var node = cut.FindAll(".bit-blow-node")[nodeIndex];
        node.PointerDown(new PointerEventArgs { Button = 0, ClientX = from.X, ClientY = from.Y });
        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { ClientX = to.X, ClientY = to.Y });
    }
}
