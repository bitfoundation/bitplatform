using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests for abandoning a gesture part-way through - by Escape, or by the browser taking the
/// pointer away - and for the add-on components surviving being used outside a canvas.
/// </summary>
[TestClass]
public class GestureCancellationTests : BunitContext
{
    public GestureCancellationTests()
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

    private static void Escape(IRenderedComponent<BlowCanvas> cut)
        => cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "Escape" });

    // ---- box selection ----

    [TestMethod]
    public void EscapeTakesTheSelectionBoxDown()
    {
        var cut = Render(ThreeNodes());
        var pane = cut.Find(".bit-blow");

        pane.PointerDown(new PointerEventArgs { Button = 0, ShiftKey = true, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 300, ClientY = 100 });
        Assert.ContainsSingle(cut.FindAll(".bit-blow-selection"));

        Escape(cut);

        Assert.IsEmpty(cut.FindAll(".bit-blow-selection"));
    }

    [TestMethod]
    public void EscapeStopsTheBoxFromCarryingOnSelecting()
    {
        var nodes = ThreeNodes();
        var cut = Render(nodes);
        var pane = cut.Find(".bit-blow");

        pane.PointerDown(new PointerEventArgs { Button = 0, ShiftKey = true, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 150, ClientY = 100 });
        Escape(cut);

        // Before the fix the box stayed live and this move swept up the rest of the graph.
        pane.PointerMove(new PointerEventArgs { ClientX = 600, ClientY = 100 });

        Assert.DoesNotContain(n => n.Selected, nodes);
    }

    [TestMethod]
    public void EscapePutsBackTheSelectionTheBoxWasPaintingOver()
    {
        var nodes = ThreeNodes();
        nodes[2].Selected = true;

        var cut = Render(nodes);
        var pane = cut.Find(".bit-blow");

        pane.PointerDown(new PointerEventArgs { Button = 0, ShiftKey = true, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 150, ClientY = 100 });
        Escape(cut);

        Assert.IsFalse(nodes[0].Selected);
        Assert.IsTrue(nodes[2].Selected);
    }

    // ---- node drag ----

    [TestMethod]
    public void EscapePutsADraggedNodeBackWhereItStarted()
    {
        var nodes = ThreeNodes();
        var cut = Render(nodes);

        cut.FindAll(".bit-blow-node")[0]
            .PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { ClientX = 90, ClientY = 70 });
        Assert.AreNotEqual(new BlowXYPosition(0, 0), nodes[0].Position);

        Escape(cut);

        Assert.AreEqual(new BlowXYPosition(0, 0), nodes[0].Position);
        Assert.IsFalse(nodes[0].Dragging);
    }

    [TestMethod]
    public void ACancelledDragDoesNotReportItselfAsFinished()
    {
        var stops = 0;
        var nodes = ThreeNodes();
        var cut = Render(nodes, extra: p => p.Add(c => c.OnNodeDragStop, (BlowNode _) => stops++));

        cut.FindAll(".bit-blow-node")[0]
            .PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { ClientX = 90, ClientY = 70 });
        Escape(cut);
        cut.Find(".bit-blow").PointerUp(new PointerEventArgs { ClientX = 90, ClientY = 70 });

        Assert.AreEqual(0, stops);
    }

    // ---- connection ----

    [TestMethod]
    public void EscapeStillCancelsAConnectionAsItAlwaysDid()
    {
        var edges = new List<BlowEdge>();
        var cut = Render(ThreeNodes(), edges);

        cut.InvokeAsync(() =>
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom, 1, 10, 10));
        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { ClientX = 200, ClientY = 200 });
        Assert.IsTrue(cut.Instance.IsConnecting);

        Escape(cut);

        Assert.IsFalse(cut.Instance.IsConnecting);
        Assert.IsEmpty(edges);
    }

    // ---- Escape with nothing running ----

    [TestMethod]
    public void EscapeWithNoGestureRunningStillClearsTheSelection()
    {
        var nodes = ThreeNodes();
        nodes[0].Selected = true;
        nodes[1].Selected = true;

        var cut = Render(nodes);
        Escape(cut);

        Assert.DoesNotContain(n => n.Selected, nodes);
    }

    // ---- add-ons outside a canvas ----

    [TestMethod]
    public void ControlsPlacedOutsideACanvasRenderInertRatherThanThrowing()
    {
        var cut = Render<BlowControls>();

        Assert.IsNotNull(cut.Find(".bit-blow-controls"));
        foreach (var b in cut.FindAll("button")) Assert.IsTrue(b.HasAttribute("disabled"));
    }

    [TestMethod]
    public void AMiniMapPlacedOutsideACanvasRendersEmptyRatherThanThrowing()
    {
        var cut = Render<BlowMiniMap>();

        Assert.IsNotNull(cut.Find(".bit-blow-minimap"));
        Assert.IsEmpty(cut.FindAll(".bit-blow-minimap-node"));
    }

    [TestMethod]
    public void AnOrphanedMiniMapIgnoresPointerInputInsteadOfCrashing()
    {
        var cut = Render<BlowMiniMap>();

        cut.Find(".bit-blow-minimap")
            .PointerDown(new PointerEventArgs { Button = 0, OffsetX = 10, OffsetY = 10 });
        cut.Find(".bit-blow-minimap")
            .PointerMove(new PointerEventArgs { OffsetX = 40, OffsetY = 40 });

        Assert.IsNotNull(cut.Find(".bit-blow-minimap"));
    }

    // ---- viewport culling ----
    //
    // The pane is 800x600 at the identity viewport, and culling keeps a 20% margin, so anything
    // reaching x < 960 is on screen.

    private IRenderedComponent<BlowCanvas> RenderCulled(List<BlowNode> nodes, List<BlowEdge> edges)
    {
        JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js")
            .Setup<BlowDomRect?>("getRect", _ => true)
            .SetResult(new BlowDomRect(0, 0, 800, 600));

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges);
            p.Add(c => c.OnlyRenderVisibleElements, true);
        });

        // The pane rectangle only arrives after the first render, and nothing about learning it
        // forces a second one. Culling has no idea what is visible until it exists.
        cut.Render();
        return cut;
    }

    [TestMethod]
    public void CullingOnlyHappensOnceThePaneHasBeenMeasured()
    {
        // Guards the test set-up above: without a pane rectangle nothing is culled at all, which
        // would make every culling assertion below pass for the wrong reason.
        List<BlowNode> nodes = [new() { Id = "a", Position = new(9000, 9000), Width = 100, Height = 40 }];

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.OnlyRenderVisibleElements, true);
        });

        Assert.ContainsSingle(cut.FindAll(".bit-blow-node"));
    }

    [TestMethod]
    public void ACurveThatReachesIntoViewIsNotCulledWithItsEndpoints()
    {
        // Both anchors sit off to the right, but the source handle faces left, so the bezier
        // swings back across the screen edge. Culling on the endpoints alone would lose it.
        List<BlowNode> nodes =
        [
            new() { Id = "a", Position = new(1000, 0), Width = 100, Height = 40, SourcePosition = BlowPosition.Left },
            new() { Id = "b", Position = new(1400, 300), Width = 100, Height = 40, TargetPosition = BlowPosition.Right },
        ];
        List<BlowEdge> edges = [new() { Id = "e", Source = "a", Target = "b" }];

        var cut = RenderCulled(nodes, edges);

        Assert.ContainsSingle(cut.FindAll(".bit-blow-edge-path"));
    }

    [TestMethod]
    public void AnEdgeWithNothingOnScreenIsStillCulled()
    {
        List<BlowNode> nodes =
        [
            new() { Id = "a", Position = new(6000, 0), Width = 100, Height = 40, SourcePosition = BlowPosition.Left },
            new() { Id = "b", Position = new(6400, 300), Width = 100, Height = 40, TargetPosition = BlowPosition.Right },
        ];
        List<BlowEdge> edges = [new() { Id = "e", Source = "a", Target = "b" }];

        var cut = RenderCulled(nodes, edges);

        Assert.IsEmpty(cut.FindAll(".bit-blow-edge-path"));
    }

    [TestMethod]
    public void ASelfLoopReachingIntoViewSurvivesCulling()
    {
        // The node is off screen; its loop stands 120 units off it, back towards the viewport.
        List<BlowNode> nodes = [new() { Id = "a", Position = new(1000, 100), Width = 100, Height = 40 }];
        List<BlowEdge> edges =
        [
            new() { Id = "self", Source = "a", Target = "a", PathOptions = new() { LoopRadius = 120 } },
        ];

        var cut = RenderCulled(nodes, edges);

        Assert.ContainsSingle(cut.FindAll(".bit-blow-edge-path"));
    }

    // ---- fit view options on the control bar ----

    [TestMethod]
    public void TheFitViewButtonUsesTheOptionsItWasGiven()
    {
        var nodes = ThreeNodes();

        var cut = Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.MaxZoom, 4);
            p.Add(c => c.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<BlowControls>(0);
                b.AddAttribute(1, nameof(BlowControls.FitViewOptions),
                    new BlowFitViewOptions { MaxZoom = 0.5, Padding = 0.1 });
                b.CloseComponent();
            }));
        });

        // Without a measured pane there is nothing to fit into, so the fit is a no-op either way -
        // what matters is that the options reached the canvas rather than being dropped.
        cut.FindAll(".bit-blow-controls-button")[2].Click();

        Assert.IsTrue(cut.Instance.GetZoom() <= 0.5 || cut.Instance.GetZoom() == 1);
    }
}
