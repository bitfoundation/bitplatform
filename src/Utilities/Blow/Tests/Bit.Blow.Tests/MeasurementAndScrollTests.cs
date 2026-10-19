using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for what the canvas learns from the DOM (measured handles) and for the wheel gesture,
/// which the browser reports as a bare burst of events with no start or end of its own.
/// </summary>
[TestClass]
public class MeasurementAndScrollTests : BunitContext
{
    public MeasurementAndScrollTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitJSModuleInterop Module() => JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");

    private static BlowMeasuredHandle Handle(string id, string type, string position = "bottom",
        double offsetX = 50, double offsetY = 40)
        => new(id, type, position, offsetX, offsetY);

    // ---- duplicate handle ids ----

    [TestMethod]
    public void TwoHandlesOfOneTypeSharingAnIdAreReported()
    {
        var module = Module();
        module.Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(
        [
            new("a", 100, 40, [Handle("out", "source"), Handle("out", "source", offsetY: 10)]),
        ]);

        var errors = new List<BlowError>();
        Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [new BlowNode { Id = "a" }]);
            p.Add(c => c.OnError, EventCallback.Factory.Create<BlowError>(this, errors.Add));
        });

        var error = Assert.ContainsSingle(e => e.Code == BlowErrorCode.DuplicateHandleId, errors);
        Assert.Contains("'out'", error.Message);
    }

    [TestMethod]
    public void TwoAnonymousHandlesOfOneTypeAreReportedWithAdviceToNameThem()
    {
        var module = Module();
        module.Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(
        [
            new("a", 100, 40, [Handle("", "source"), Handle("", "source", offsetY: 10)]),
        ]);

        var errors = new List<BlowError>();
        Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [new BlowNode { Id = "a" }]);
            p.Add(c => c.OnError, EventCallback.Factory.Create<BlowError>(this, errors.Add));
        });

        var error = Assert.ContainsSingle(e => e.Code == BlowErrorCode.DuplicateHandleId, errors);
        Assert.Contains("Id", error.Message);
    }

    [TestMethod]
    public void HandlesOfDifferentTypesMaySharpenTheSameId()
    {
        // A source "a" and a target "a" are different handles, and an edge names them separately.
        var module = Module();
        module.Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(
        [
            new("a", 100, 40, [Handle("a", "source"), Handle("a", "target", "top", offsetY: 0)]),
        ]);

        var errors = new List<BlowError>();
        Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [new BlowNode { Id = "a" }]);
            p.Add(c => c.OnError, EventCallback.Factory.Create<BlowError>(this, errors.Add));
        });

        Assert.DoesNotContain(e => e.Code == BlowErrorCode.DuplicateHandleId, errors);
    }

    [TestMethod]
    public void MeasuredHandlesBecomeTheEdgeAnchors()
    {
        var module = Module();
        module.Setup<BlowMeasuredNode[]>("measureNodes", _ => true).SetResult(
        [
            new("a", 100, 40, [Handle("", "source", "right", offsetX: 100, offsetY: 20)]),
            new("b", 100, 40, [Handle("", "target", "left", offsetX: 0, offsetY: 20)]),
        ]);

        var nodes = new List<BlowNode>
        {
            new() { Id = "a", Position = new(0, 0) },
            new() { Id = "b", Position = new(300, 0) },
        };

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, [new BlowEdge { Id = "e", Source = "a", Target = "b" }]);
        });

        // The path has to start at the source handle's measured anchor, not the node's edge midpoint.
        var path = cut.Find(".bit-blow-edge-path").GetAttribute("d")!;
        Assert.StartsWith("M100,20", path);
    }

    // ---- auto-pan on node focus ----

    /// <summary>
    /// Gives the canvas a pane rectangle, which it otherwise has no way to learn under bUnit -
    /// and without which it cannot tell whether a node is on screen.
    /// </summary>
    private IRenderedComponent<BlowCanvas> RenderWithPane(
        List<BlowNode> nodes, Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
    {
        Module().Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, 0, 800, 600));
        return Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            extra?.Invoke(p);
        });
    }

    private static List<BlowNode> NearAndFar() =>
    [
        new() { Id = "near", Position = new(100, 100), Width = 100, Height = 40 },
        new() { Id = "far", Position = new(4000, 3000), Width = 100, Height = 40 },
    ];

    [TestMethod]
    public void TabbingToAnOffScreenNodeBringsItIntoView()
    {
        var cut = RenderWithPane(NearAndFar(), p => p.Add(c => c.AutoPanOnNodeFocusDuration, 0));

        cut.FindAll(".bit-blow-node")[1].Focus();

        var viewport = cut.Instance.GetViewport();
        var node = cut.Instance.GetNode("far")!;
        var topLeft = viewport.FlowToScreen(new BlowXYPosition(node.GetRect().Left, node.GetRect().Top));

        Assert.IsInRange(0, 800, topLeft.X);
        Assert.IsInRange(0, 600, topLeft.Y);
    }

    [TestMethod]
    public void TabbingToANodeAlreadyOnScreenLeavesTheViewportAlone()
    {
        var cut = RenderWithPane(NearAndFar(), p => p.Add(c => c.AutoPanOnNodeFocusDuration, 0));

        cut.FindAll(".bit-blow-node")[0].Focus();

        Assert.AreEqual(BlowViewport.Identity, cut.Instance.GetViewport());
    }

    [TestMethod]
    public void TurningAutoPanOnNodeFocusOffAttachesNoFocusListenerAtAll()
    {
        // Not merely inert: with a listener per node, focus would round-trip into .NET for every
        // Tab press on a graph that asked for none of it.
        var cut = RenderWithPane(NearAndFar(), p => p.Add(c => c.AutoPanOnNodeFocus, false));

        Assert.ThrowsExactly<Bunit.MissingEventHandlerException>(() => cut.FindAll(".bit-blow-node")[1].Focus());
        Assert.AreEqual(BlowViewport.Identity, cut.Instance.GetViewport());
    }

    [TestMethod]
    public void RevealingAFocusedNodeMovesOnlyAsFarAsItHasTo()
    {
        // A node just off the right edge should slide into view, not be re-centred: re-centring
        // on every Tab throws away the reader's sense of where they are in the graph.
        List<BlowNode> nodes = [new() { Id = "edge", Position = new(820, 100), Width = 100, Height = 40 }];
        var cut = RenderWithPane(nodes, p => p.Add(c => c.AutoPanOnNodeFocusDuration, 0));

        cut.Find(".bit-blow-node").Focus();

        var viewport = cut.Instance.GetViewport();
        Assert.AreEqual(0, viewport.Y, 1e-6);

        // Its right edge (920) lands on the pane's right edge less the auto-pan margin (800-30).
        Assert.AreEqual(770 - 920, viewport.X, 1e-6);
    }

    // ---- the wheel gesture ----

    [TestMethod]
    public void ScrollingRaisesMoveStartOnce()
    {
        var starts = 0;
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [new BlowNode { Id = "a" }]);
            p.Add(c => c.OnMoveStart, (BlowViewport _) => starts++);
        });

        var pane = cut.Find(".bit-blow");
        pane.Wheel(new WheelEventArgs { DeltaY = -100, ClientX = 10, ClientY = 10 });
        pane.Wheel(new WheelEventArgs { DeltaY = -100, ClientX = 10, ClientY = 10 });
        pane.Wheel(new WheelEventArgs { DeltaY = -100, ClientX = 10, ClientY = 10 });

        Assert.AreEqual(1, starts);
    }

    [TestMethod]
    public async Task ScrollingRaisesMoveEndOnceTheWheelGoesQuiet()
    {
        var ended = new TaskCompletionSource<BlowViewport>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [new BlowNode { Id = "a" }]);
            p.Add(c => c.OnMoveEnd, (BlowViewport v) => ended.TrySetResult(v));
        });

        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = -100, ClientX = 10, ClientY = 10 });

        // The end is inferred from a quiet period rather than any DOM event, so it cannot have
        // arrived yet - and must arrive shortly afterwards without further input.
        Assert.IsFalse(ended.Task.IsCompleted);

        var finished = await Task.WhenAny(ended.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.AreSame(ended.Task, finished);
        Assert.IsTrue((await ended.Task).Zoom > 1);
    }

    [TestMethod]
    public async Task AContinuingScrollKeepsPushingTheEndOfTheGestureBack()
    {
        var ends = 0;
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [new BlowNode { Id = "a" }]);
            p.Add(c => c.OnMoveEnd, (BlowViewport _) => ends++);
        });

        var pane = cut.Find(".bit-blow");
        for (var i = 0; i < 4; i++)
        {
            pane.Wheel(new WheelEventArgs { DeltaY = -100, ClientX = 10, ClientY = 10 });
            await Task.Delay(60);
        }

        // Four bursts 60ms apart are one gesture, not four.
        Assert.AreEqual(0, ends);
        await Task.Delay(500);
        Assert.AreEqual(1, ends);
    }

    [TestMethod]
    public void AWheelEventThatIsIgnoredDoesNotStartAGesture()
    {
        var starts = 0;
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [new BlowNode { Id = "a" }]);
            p.Add(c => c.ZoomOnScroll, false);
            p.Add(c => c.OnMoveStart, (BlowViewport _) => starts++);
        });

        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = -100, ClientX = 10, ClientY = 10 });

        Assert.AreEqual(0, starts);
    }

    [TestMethod]
    public void PanningOnScrollAlsoBracketsItselfWithMoveStart()
    {
        var starts = 0;
        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, [new BlowNode { Id = "a" }]);
            p.Add(c => c.PanOnScroll, true);
            p.Add(c => c.OnMoveStart, (BlowViewport _) => starts++);
        });

        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = 40, ClientX = 10, ClientY = 10 });

        Assert.AreEqual(1, starts);
    }
}
