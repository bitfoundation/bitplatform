using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Regression tests for defects found in a full review of the library: each test pins down one
/// behaviour that used to be wrong.
/// </summary>
[TestClass]
public class ReviewRegressionTests : BunitContext
{
    public ReviewRegressionTests()
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

    private BunitJSModuleInterop Module() => JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");

    private static T WithCulture<T>(string name, Func<T> action)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(name);
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // ---- edge ids ----

    [TestMethod]
    public async Task ConnectingTheSamePairTwiceGivesTwoDistinctEdges()
    {
        var edges = new List<BlowEdge>();
        var cut = Render(TwoNodes(), edges, p => p.Add(c => c.PreventDuplicateEdges, false));

        for (var i = 0; i < 2; i++)
        {
            await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));
            await cut.InvokeAsync(() => cut.Instance.CompleteConnection("b", null, BlowHandleType.Target));
        }

        Assert.AreEqual(2, edges.Count);
        Assert.AreNotEqual(edges[0].Id, edges[1].Id);
        Assert.AreEqual(2, cut.FindAll(".bit-blow-edge").Count);
    }

    [TestMethod]
    public async Task ConnectingAgainAfterAReconnectDoesNotReuseTheOldId()
    {
        // The edge created for a->b keeps its id when reconnected to c, so the id a fresh a->b
        // connection would be given is already taken.
        List<BlowNode> nodes = [.. TwoNodes(), new() { Id = "c", Position = new(300, 200) }];
        var edges = new List<BlowEdge> { new() { Id = "bit-blow-a--b", Source = "a", Target = "c" } };
        var cut = Render(nodes, edges);

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));
        await cut.InvokeAsync(() => cut.Instance.CompleteConnection("b", null, BlowHandleType.Target));

        Assert.AreEqual(2, edges.Count);
        Assert.AreEqual(2, edges.Select(e => e.Id).Distinct().Count());
    }

    [TestMethod]
    public void AddEdgeTreatsAMissingAndAnEmptyHandleAsTheSame()
    {
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b", SourceHandle = "" } };

        BlowGraphUtils.AddEdge(new BlowConnection("a", "b", null, null), edges);

        Assert.ContainsSingle(edges);
    }

    // ---- gestures ----

    [TestMethod]
    public async Task AConnectionStartedWithoutAPointerEndsOnAnyRelease()
    {
        // The no-pointer overload used to capture pointer 0, after which only a release of
        // pointer 0 - which a mouse in Chrome never sends - could end the gesture.
        var cut = Render(TwoNodes());

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));
        cut.Find(".bit-blow").PointerUp(new PointerEventArgs { PointerId = 1, ClientX = 500, ClientY = 500 });

        Assert.IsFalse(cut.Instance.IsConnecting);
    }

    [TestMethod]
    public void ASecondFingerAbandonsABoxSelectionAndGivesTheSelectionBack()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes);
        var pane = cut.Find(".bit-blow");

        pane.PointerDown(new PointerEventArgs { PointerId = 1, Button = 0, ShiftKey = true, ClientX = -10, ClientY = -10 });
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 400, ClientY = 400 });
        Assert.IsTrue(nodes[0].Selected);

        pane.PointerDown(new PointerEventArgs { PointerId = 2, Button = 0, ClientX = 300, ClientY = 300 });

        Assert.IsFalse(nodes[0].Selected);
        Assert.IsFalse(nodes[1].Selected);
        Assert.IsEmpty(cut.FindAll(".bit-blow-selection"));
    }

    [TestMethod]
    public void ARightClickOnThePaneDoesNotLeaveAGestureRunning()
    {
        var cut = Render(TwoNodes());
        var pane = cut.Find(".bit-blow");

        // A context menu can swallow the matching pointer-up, so none is sent here.
        pane.PointerDown(new PointerEventArgs { PointerId = 1, Button = 2, ClientX = 10, ClientY = 10 });

        Assert.IsFalse(cut.Find(".bit-blow").HasAttribute("blazor:onpointermove"));
    }

    [TestMethod]
    public void WithNoSelectionKeyTheDefaultMultiSelectionKeyDoesNotDrawABox()
    {
        var cut = Render(TwoNodes(), extra: p => p.Add(c => c.SelectionKey, (BlowModifierKey?)null));
        var pane = cut.Find(".bit-blow");

        pane.PointerDown(new PointerEventArgs { Button = 0, ShiftKey = true, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 300, ClientY = 100 });

        Assert.IsEmpty(cut.FindAll(".bit-blow-selection"));
        Assert.AreEqual(300, cut.Instance.GetViewport().X, 1e-6);
    }

    [TestMethod]
    public async Task OnConnectEndSaysWhetherTheConnectionWasDropped()
    {
        var ends = new List<BlowConnectEndInfo>();
        var cut = Render(TwoNodes(), extra: p =>
            p.Add(c => c.OnConnectEnd, EventCallback.Factory.Create<BlowConnectEndInfo>(this, ends.Add)));
        var pane = cut.Find(".bit-blow");

        // Abandoned with Escape: not a drop.
        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 10, 10));
        pane.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // Dragged out and released over empty space: a drop.
        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 10, 10));
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 400, ClientY = 400 });
        pane.PointerUp(new PointerEventArgs { PointerId = 1, ClientX = 400, ClientY = 400 });

        Assert.AreEqual(2, ends.Count);
        Assert.IsFalse(ends[0].IsDropped);
        Assert.IsTrue(ends[1].IsDropped);
        Assert.IsFalse(ends[1].IsValid);
        Assert.IsFalse(ends[1].IsReconnect);
    }

    // ---- handles that refuse connections ----

    private IRenderedComponent<BlowCanvas> RenderMeasured(bool targetAcceptsDrops, Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
    {
        Module().Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(
        [
            new("a", 150, 40, [new BlowMeasuredHandle("", "source", "bottom", 75, 40)]),
            new("b", 150, 40, [new BlowMeasuredHandle("", "target", "top", 75, 0, targetAcceptsDrops)]),
        ]);
        return Render(TwoNodes(), [], extra);
    }

    [TestMethod]
    [DataRow(true, 1)]
    [DataRow(false, 0)]
    public async Task ReleasingNearAHandleOnlySnapsToItWhenItAcceptsDrops(bool accepts, int expectedEdges)
    {
        var edges = new List<BlowEdge>();
        Module().Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(
        [
            new("a", 150, 40, [new BlowMeasuredHandle("", "source", "bottom", 75, 40)]),
            new("b", 150, 40, [new BlowMeasuredHandle("", "target", "top", 75, 0, accepts)]),
        ]);
        var cut = Render(TwoNodes(), edges);
        var pane = cut.Find(".bit-blow");

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 75, 40));
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 78, ClientY = 205 });
        pane.PointerUp(new PointerEventArgs { PointerId = 1, ClientX = 78, ClientY = 205 });

        Assert.AreEqual(expectedEdges, edges.Count);
    }

    [TestMethod]
    public async Task TheConnectionLineReportsAValidTargetWhileTheDragIsCaptured()
    {
        // The pane captures the pointer, so handles never see it enter: validity has to come
        // from where the line ends rather than from hover events.
        BlowConnectionLineContext? last = null;
        var cut = RenderMeasured(true, p =>
            p.Add(c => c.OnConnectionChange, EventCallback.Factory.Create<BlowConnectionLineContext?>(this, ctx => last = ctx)));
        var pane = cut.Find(".bit-blow");

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 75, 40));
        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 300, ClientY = 300 });
        Assert.IsFalse(last!.IsValid);

        pane.PointerMove(new PointerEventArgs { PointerId = 1, ClientX = 78, ClientY = 205 });
        Assert.IsTrue(last!.IsValid);
    }

    [TestMethod]
    public void AHandleThatRefusesConnectionsSaysSoInTheDom()
    {
        RenderFragment<BlowNode> template = _ => builder =>
        {
            builder.OpenComponent<BlowHandle>(0);
            builder.AddAttribute(1, nameof(BlowHandle.Type), BlowHandleType.Target);
            builder.AddAttribute(2, nameof(BlowHandle.IsConnectable), false);
            builder.CloseComponent();
            builder.OpenComponent<BlowHandle>(3);
            builder.AddAttribute(4, nameof(BlowHandle.Type), BlowHandleType.Source);
            builder.CloseComponent();
        };
        var cut = Render([new BlowNode { Id = "a" }], extra: p => p.Add(c => c.NodeTemplate, template));

        var handles = cut.FindAll(".bit-blow-handle");
        Assert.AreEqual("false", handles[0].GetAttribute("data-connectable-end"));
        Assert.DoesNotContain("connectable", handles[0].ClassList);
        Assert.IsNull(handles[1].GetAttribute("data-connectable-end"));
        Assert.Contains("connectable", handles[1].ClassList);
    }

    [TestMethod]
    public async Task ADisposedHandleDoesNotRemoveTheRegistrationThatReplacedIt()
    {
        var cut = Render(TwoNodes());
        var key = BlowHandleRegistration.Key("b", BlowHandleType.Target, null);
        var old = new BlowHandleRegistration { IsValidConnection = _ => true };
        var replacement = new BlowHandleRegistration { IsValidConnection = _ => false };

        await cut.InvokeAsync(() =>
        {
            cut.Instance.RegisterHandle(key, old);
            cut.Instance.RegisterHandle(key, replacement);
            cut.Instance.UnregisterHandle(key, old);
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
        });

        // The replacement's rule still applies.
        Assert.IsFalse(cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target));
    }

    // ---- wheel ----

    [TestMethod]
    public void ATrackpadSizedWheelDeltaZoomsFarLessThanAMouseNotch()
    {
        var small = Render(TwoNodes());
        small.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = -4 });

        var notch = Render(TwoNodes());
        notch.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = -100 });

        Assert.IsInRange(1.0001, 1.02, small.Instance.GetZoom());
        Assert.IsInRange(1.1, 1.2, notch.Instance.GetZoom());
    }

    [TestMethod]
    public void ACtrlWheelNotchDoesNotZoomByMoreThanOneStep()
    {
        var cut = Render(TwoNodes());
        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = -100, CtrlKey = true });

        Assert.IsInRange(1.1, 1.2, cut.Instance.GetZoom());
    }

    [TestMethod]
    public void HorizontalPanOnScrollFollowsAHorizontalTrackpadSwipe()
    {
        var cut = Render(TwoNodes(), extra: p =>
        {
            p.Add(c => c.PanOnScroll, true);
            p.Add(c => c.PanOnScrollMode, BlowPanOnScrollMode.Horizontal);
        });

        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaX = 40, DeltaY = 0 });

        Assert.AreEqual(-20, cut.Instance.GetViewport().X, 1e-6);
        Assert.AreEqual(0, cut.Instance.GetViewport().Y, 1e-6);
    }

    [TestMethod]
    public void AShiftWheelTheSystemAlreadyTurnedHorizontalStillPans()
    {
        var cut = Render(TwoNodes(), extra: p => p.Add(c => c.PanOnScroll, true));

        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaX = 40, DeltaY = 0, ShiftKey = true });

        Assert.AreEqual(-20, cut.Instance.GetViewport().X, 1e-6);
    }

    [TestMethod]
    public void ALineBasedWheelPansByLinesNotPixels()
    {
        var cut = Render(TwoNodes(), extra: p => p.Add(c => c.PanOnScroll, true));

        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = 3, DeltaMode = 1 });

        Assert.AreEqual(-30, cut.Instance.GetViewport().Y, 1e-6);
    }

    // ---- fit view on init ----

    [TestMethod]
    public void FitViewOnInitWaitsForAGraphThatArrivesLater()
    {
        Module().Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, 0, 800, 600));

        var cut = Render([], extra: p => p.Add(c => c.FitViewOnInit, true));
        Assert.AreEqual(BlowViewport.Identity, cut.Instance.GetViewport());

        cut.Render(p => p.Add(c => c.Nodes,
            [new BlowNode { Id = "a", Position = new(2000, 2000), Width = 100, Height = 50 }]));

        Assert.AreNotEqual(BlowViewport.Identity, cut.Instance.GetViewport());

        // Once only: a node added later does not refit the view the user may since have moved.
        var fitted = cut.Instance.GetViewport();
        cut.Render(p => p.Add(c => c.Nodes,
            [new BlowNode { Id = "a", Position = new(2000, 2000), Width = 100, Height = 50 },
             new BlowNode { Id = "b", Position = new(-5000, -5000), Width = 100, Height = 50 }]));
        Assert.AreEqual(fitted, cut.Instance.GetViewport());
    }

    // ---- history ----

    [TestMethod]
    public async Task AddingNodesThroughTheApiIsAnUndoStep()
    {
        var nodes = TwoNodes();
        var cut = Render(nodes, extra: p => p.Add(c => c.HistoryEnabled, true));

        await cut.InvokeAsync(() => cut.Instance.AddNodes(new BlowNode { Id = "c" }));
        Assert.AreEqual(3, nodes.Count);

        await cut.InvokeAsync(() => cut.Instance.UndoAsync());
        Assert.AreEqual(2, nodes.Count);
    }

    [TestMethod]
    public void RestylingAnEdgeIsRecordedAsAStep()
    {
        var history = new BlowHistory();
        var edge = new BlowEdge { Id = "e", Source = "a", Target = "b" };
        history.Capture(new BlowSnapshot([], [edge.Clone()], BlowViewport.Identity));

        edge.Animated = true;
        edge.Stroke = "red";

        Assert.IsTrue(history.Capture(new BlowSnapshot([], [edge.Clone()], BlowViewport.Identity)));
    }

    [TestMethod]
    public void ReplacingANodesDataIsRecordedAsAStep()
    {
        var history = new BlowHistory();
        var node = new BlowNode { Id = "a", Data = "before" };
        history.Capture(new BlowSnapshot([node.Clone()], [], BlowViewport.Identity));

        node.Data = "after";

        Assert.IsTrue(history.Capture(new BlowSnapshot([node.Clone()], [], BlowViewport.Identity)));
    }

    [TestMethod]
    public void SelectingIsNotRecordedAsAStep()
    {
        var history = new BlowHistory();
        var node = new BlowNode { Id = "a" };
        history.Capture(new BlowSnapshot([node.Clone()], [], BlowViewport.Identity));

        node.Selected = true;

        Assert.IsFalse(history.Capture(new BlowSnapshot([node.Clone()], [], BlowViewport.Identity)));
    }

    [TestMethod]
    public void ClonedEdgesDoNotSharePathOptions()
    {
        var original = new BlowEdge { Id = "e", Source = "a", Target = "b", PathOptions = new() { Curvature = 0.5 } };
        var clone = original.Clone();

        clone.PathOptions!.Curvature = 1;

        Assert.AreEqual(0.5, original.PathOptions.Curvature);
    }

    // ---- geometry ----

    [TestMethod]
    public void AStepEdgeIntoATopHandleAboveItDoesNotRunThroughTheTarget()
    {
        // Right out of the source, into the top of a target up and to the right: the corner has
        // to be taken above the target, not level with the source (which would run the edge up
        // through the target's body and back down into its handle).
        var result = BlowEdgePath.Compute(BlowEdgeType.Step, 0, 0, BlowPosition.Right, 100, -100, BlowPosition.Top);

        Assert.AreEqual("M0 0L20 0L20 -120L100 -120L100 -100", result.Path);
    }

    [TestMethod]
    public void ASmoothStepLabelSitsOnTheLongestStraightRun()
    {
        var result = BlowEdgePath.SmoothStep(0, 0, BlowPosition.Right, 100, 100, BlowPosition.Top);

        Assert.AreEqual(60, result.LabelX, 1e-6);
        Assert.AreEqual(0, result.LabelY, 1e-6);
    }

    [TestMethod]
    public void StepPositionOnlySlidesTheBendAlongTheTravelAxis()
    {
        var result = BlowEdgePath.SmoothStep(0, 0, BlowPosition.Right, 200, 100, BlowPosition.Left, stepPosition: 0.2);

        // The vertical run sits 20% of the way along; the label stays on it, halfway across.
        Assert.AreEqual(20 + (180 - 20) * 0.2, result.LabelX, 1e-6);
        Assert.AreEqual(50, result.LabelY, 1e-6);
    }

    [TestMethod]
    public void ASelfLoopOnAWideNodeClearsTheNode()
    {
        var node = new BlowRect(0, 0, 150, 40);
        var loop = BlowEdgePath.Loop(75, 40, BlowPosition.Bottom, 75, 0, BlowPosition.Top, 60, node);

        // The midpoint of the curve is left of the node, not hidden behind it.
        Assert.IsTrue(loop.LabelX < node.Left, $"loop midpoint at x={loop.LabelX} is behind the node");
    }

    [TestMethod]
    public void ASelfLoopWithSubPixelMisalignedAnchorsStillLoops()
    {
        var loop = BlowEdgePath.Loop(75, 40, BlowPosition.Bottom, 75.0001, 0, BlowPosition.Top, 60);

        Assert.IsTrue(Math.Abs(loop.LabelX - 75) > 30, "the loop collapsed onto the node");
    }

    [TestMethod]
    public void GetViewportForBoundsToleratesAnInvertedZoomRange()
    {
        var vp = BlowGraphUtils.GetViewportForBounds(new BlowRect(0, 0, 100, 100), 800, 600, minZoom: 2, maxZoom: 1);

        Assert.AreEqual(1, vp.Zoom, 1e-6);
    }

    // ---- layout ----

    [TestMethod]
    public void ANodeDownstreamOfACycleIsLaidOutAfterIt()
    {
        List<BlowNode> nodes =
        [
            new() { Id = "start", Width = 100, Height = 40 },
            new() { Id = "end", Width = 100, Height = 40 },
            new() { Id = "a", Width = 100, Height = 40 },
            new() { Id = "b", Width = 100, Height = 40 },
        ];
        List<BlowEdge> edges =
        [
            new() { Id = "1", Source = "start", Target = "a" },
            new() { Id = "2", Source = "a", Target = "b" },
            new() { Id = "3", Source = "b", Target = "a" },
            new() { Id = "4", Source = "b", Target = "end" },
        ];

        BlowLayout.Layered(nodes, edges);

        var y = nodes.ToDictionary(n => n.Id, n => n.Position.Y);
        Assert.IsTrue(y["start"] < y["a"]);
        Assert.IsTrue(y["a"] < y["b"]);
        Assert.IsTrue(y["b"] < y["end"]);
    }

    [TestMethod]
    public void ACircleOfTallNodesDoesNotOverlap()
    {
        List<BlowNode> nodes =
        [
            new() { Id = "a", Width = 50, Height = 300 },
            new() { Id = "b", Width = 50, Height = 300 },
        ];

        BlowLayout.Circle(nodes);

        var top = nodes.OrderBy(n => n.Position.Y).ToArray();
        Assert.IsTrue(top[1].Position.Y >= top[0].Position.Y + 300, "the two nodes overlap");
    }

    [TestMethod]
    public void LayoutsPlaceNodesByTheirBoxWhenTheCanvasUsesACentredOrigin()
    {
        List<BlowNode> nodes =
        [
            new() { Id = "a", Width = 100, Height = 40 },
            new() { Id = "b", Width = 200, Height = 40 },
        ];

        BlowLayout.Grid(nodes, new BlowGridLayoutOptions { Columns = 1, NodeOrigin = new(0.5, 0.5) });

        // Centred in equal cells, so with a centre origin both positions share an x.
        Assert.AreEqual(nodes[0].Position.X, nodes[1].Position.X, 1e-6);
    }

    [TestMethod]
    public void DistributingNodesWiderThanTheirSpanKeepsThemInOrderWithoutOverlap()
    {
        List<BlowNode> nodes =
        [
            new() { Id = "a", Position = new(0, 0), Width = 500, Height = 40 },
            new() { Id = "b", Position = new(100, 0), Width = 50, Height = 40 },
            new() { Id = "c", Position = new(200, 0), Width = 50, Height = 40 },
        ];

        BlowAlign.DistributeHorizontally(nodes);

        Assert.IsTrue(nodes[1].Position.X >= nodes[0].Position.X + 500);
        Assert.IsTrue(nodes[2].Position.X >= nodes[1].Position.X + 50);
    }

    [TestMethod]
    public void AligningWithACentredOriginLinesUpTheLeftEdges()
    {
        List<BlowNode> nodes =
        [
            new() { Id = "a", Position = new(100, 0), Width = 100, Height = 40 },
            new() { Id = "b", Position = new(300, 100), Width = 200, Height = 40 },
        ];

        BlowAlign.Align(nodes, BlowAlignEdge.Left, new BlowXYPosition(0.5, 0.5));

        // Left edges: a at 100 - 50 = 50, so b's centre goes to 50 + 100.
        Assert.AreEqual(100, nodes[0].Position.X, 1e-6);
        Assert.AreEqual(150, nodes[1].Position.X, 1e-6);
    }

    // ---- clipboard ----

    [TestMethod]
    public void ACopiedChildKeepsItsPlaceOnACanvasWithACentredOrigin()
    {
        var child = new BlowNode { Id = "c", Position = new(10, 10), Width = 100, Height = 40, ParentId = "p" };
        child.AbsolutePosition = new BlowXYPosition(500, 300); // top-left, as the canvas resolves it

        var fragment = BlowClipboard.Copy([child], [], new BlowXYPosition(0.5, 0.5));

        // A root's position names its centre on this canvas, so the top-left stays at 500,300.
        Assert.AreEqual(new BlowXYPosition(550, 320), fragment.Nodes[0].Position);
        Assert.IsNull(fragment.Nodes[0].ParentId);
    }

    [TestMethod]
    public void ACopiedChildsExtentMovesIntoTheFlowsCoordinates()
    {
        var child = new BlowNode
        {
            Id = "c", Position = new(10, 10), ParentId = "p", Extent = new BlowRect(0, 0, 200, 200),
        };
        child.AbsolutePosition = new BlowXYPosition(510, 310);

        var copy = BlowClipboard.Copy([child], []).Nodes[0];

        Assert.AreEqual(new BlowRect(500, 300, 200, 200), copy.Extent);
    }

    [TestMethod]
    public void AnIdFactoryThatIgnoresTheAttemptDoesNotHangPaste()
    {
        var fragment = new BlowFragment([new BlowNode { Id = "a" }], []);

        var pasted = BlowClipboard.Paste(fragment, new BlowPasteOptions
        {
            ExistingIds = ["a", "a_c"],
            IdFactory = (id, _) => id + "_c",
        });

        Assert.AreNotEqual("a_c", pasted.Nodes[0].Id);
    }

    [TestMethod]
    public void PastedNodesReportWhereTheyLanded()
    {
        var node = new BlowNode { Id = "a", Position = new(100, 100), Width = 50, Height = 50 };
        node.AbsolutePosition = new BlowXYPosition(100, 100);
        var fragment = BlowClipboard.Copy([node], []);

        var pasted = BlowClipboard.Paste(fragment, new BlowPasteOptions { Offset = new(24, 24) });

        var rect = pasted.Nodes[0].GetRect();
        Assert.AreEqual(124, rect.X, 1e-6);
        Assert.AreEqual(124, rect.Y, 1e-6);
    }

    // ---- rendering ----

    [TestMethod]
    public void ANonDraggableNodeIsNotMarkedWithTheNoDragEscapeHatch()
    {
        // "nodrag" makes the interaction guard swallow every press inside the element, which
        // would make the node unselectable and its handles dead.
        var cut = Render([new BlowNode { Id = "a", Draggable = false }]);

        var node = cut.Find(".bit-blow-node");
        Assert.DoesNotContain("nodrag", node.ClassList);
        Assert.Contains("undraggable", node.ClassList);
    }

    [TestMethod]
    public void AnExplicitWidthOverridesTheStylesheetMinimum()
    {
        var cut = Render([new BlowNode { Id = "a", Width = 40, Height = 30 }]);

        Assert.Contains("min-width:0", cut.Find(".bit-blow-node").GetAttribute("style") ?? string.Empty);
    }

    [TestMethod]
    public void ANegativeZIndexIsWrittenAsValidCssInEveryCulture()
    {
        var style = WithCulture("sv-SE", () =>
        {
            var cut = Render([new BlowNode { Id = "a", ZIndex = -1 }]);
            return cut.Find(".bit-blow-node").GetAttribute("style")!;
        });

        Assert.Contains("z-index:-1;", style);
    }

    [TestMethod]
    public void NodesAndEdgesDoNotListenForMouseMovesNobodyHandles()
    {
        var cut = Render(TwoNodes(), [new BlowEdge { Id = "e", Source = "a", Target = "b" }]);

        Assert.IsFalse(cut.Find(".bit-blow-node").HasAttribute("blazor:onmousemove"));
        Assert.IsFalse(cut.Find(".bit-blow-node").HasAttribute("blazor:onmouseenter"));
        Assert.IsFalse(cut.Find(".bit-blow-edge").HasAttribute("blazor:onmousemove"));
    }

    [TestMethod]
    public void ANodeKeepsTheBrowserContextMenuWhenTheAppHasNone()
    {
        var plain = Render(TwoNodes());
        Assert.IsFalse(plain.Find(".bit-blow-node").HasAttribute("blazor:oncontextmenu:preventdefault"));

        var handled = Render(TwoNodes(), extra: p =>
            p.Add(c => c.OnNodeContextMenu, EventCallback.Factory.Create<BlowNodeMouseEvent>(this, _ => { })));
        Assert.IsTrue(handled.Find(".bit-blow-node").HasAttribute("blazor:oncontextmenu:preventdefault"));
    }

    [TestMethod]
    public void AddOnsPlacedOutsideACanvasRenderInertlyInsteadOfThrowing()
    {
        Render<BlowBackground>();
        Render<BlowViewportPortal>(p => p.AddChildContent("<span>x</span>"));
        Render<BlowNodeToolbar>(p => p.Add(c => c.Visible, true));
        Render<BlowNodeResizer>(p => p.Add(c => c.Visible, true));
        Render<BlowNodeResizeControl>(p => p.Add(c => c.Visible, true));
        var drag = Render<BlowDragHandle>();
        drag.Find(".bit-blow-drag-handle").PointerDown(new PointerEventArgs { Button = 0 });
    }

    [TestMethod]
    public void TheMiniMapDoesNotSlideTheViewWhenZoomingAtTheLimit()
    {
        Module().Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, 0, 800, 600));
        var cut = Render(TwoNodes(), extra: p =>
        {
            p.Add(c => c.MaxZoom, 1);
            p.Add(c => c.DefaultViewport, new BlowViewport(30, 40, 1));
            p.AddChildContent<BlowMiniMap>(m => m.Add(x => x.Zoomable, true));
        });
        var before = cut.Instance.GetViewport();

        cut.Find(".bit-blow-minimap").Wheel(new WheelEventArgs { DeltaY = -100 });

        Assert.AreEqual(before, cut.Instance.GetViewport());
    }

    // ---- text ----

    [TestMethod]
    public void PositionsReadWithoutAmbiguityInACommaDecimalCulture()
    {
        var text = WithCulture("de-DE", () => new BlowXYPosition(1.5, 2.5).ToString());

        Assert.AreEqual("(1,5; 2,5)", text);
    }

    [TestMethod]
    public void AnEmojiIsMeasuredAsOneCharacterAndCurlyQuotesAsOrdinaryText()
    {
        var emoji = BlowTextMetrics.EstimateWidth("\U0001F600", 10);
        var quotes = BlowTextMetrics.EstimateWidth("“x”", 10);

        Assert.AreEqual(10, emoji, 1e-6);
        Assert.IsTrue(quotes < 3 * 10 * 0.9);
    }
}
