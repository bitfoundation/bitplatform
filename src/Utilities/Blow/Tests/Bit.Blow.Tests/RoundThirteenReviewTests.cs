using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// What driving every demo page in Chromium, Firefox and WebKit found in the library, each pinned.
/// </summary>
[TestClass]
public class RoundThirteenReviewTests : BunitContext
{
    public RoundThirteenReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static List<BlowNode> Three() =>
    [
        new() { Id = "a", Position = new(0, 0) },
        new() { Id = "b", Position = new(0, 200) },
        new() { Id = "c", Position = new(300, 0) },
    ];

    // Buttons as a browser reports them while the left button is held: a mouse moving with none held has lost its release.
    private static PointerEventArgs At(double x, double y) => new() { Button = 0, Buttons = 1, PointerId = 1, PointerType = "mouse", ClientX = x, ClientY = y };

    // ---- a double click on a node ----

    [TestMethod]
    public void ADoubleClickOnANodeReportedAtThePaneIsTheNodesAndDoesNotZoom()
    {
        // Both presses capture the pointer on the pane, so the browser reports the double click there too.
        var doubleClicks = new List<string>();
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, [])
            .Add(c => c.OnNodeDoubleClick, e => doubleClicks.Add(e.Node.Id)));
        var pane = cut.Find(".bit-blow");
        var zoom = cut.Instance.GetViewport().Zoom;

        for (var i = 0; i < 2; i++)
        {
            cut.FindAll(".bit-blow-node")[1].PointerDown(At(10, 210));
            pane.PointerUp(At(10, 210));
            pane.Click(new MouseEventArgs { ClientX = 10, ClientY = 210 });
        }
        pane.DoubleClick(new MouseEventArgs { ClientX = 10, ClientY = 210 });

        CollectionAssert.AreEqual(new[] { "b" }, doubleClicks);
        Assert.AreEqual(zoom, cut.Instance.GetViewport().Zoom, "the double click on a node zoomed the canvas");
    }

    [TestMethod]
    public void ADoubleClickOnThePaneAfterANodeClickStillZooms()
    {
        var doubleClicks = 0;
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, [])
            .Add(c => c.OnNodeDoubleClick, _ => doubleClicks++));
        var pane = cut.Find(".bit-blow");
        var zoom = cut.Instance.GetViewport().Zoom;

        cut.FindAll(".bit-blow-node")[1].PointerDown(At(10, 210));
        pane.PointerUp(At(10, 210));
        pane.Click(new MouseEventArgs { ClientX = 10, ClientY = 210 });
        pane.PointerDown(At(600, 600));
        pane.PointerUp(At(600, 600));
        pane.Click(new MouseEventArgs { ClientX = 600, ClientY = 600 });
        pane.DoubleClick(new MouseEventArgs { ClientX = 600, ClientY = 600 });

        Assert.AreEqual(0, doubleClicks);
        Assert.IsGreaterThan(zoom, cut.Instance.GetViewport().Zoom, "a double click on the pane did not zoom");
    }

    // ---- focus after a deletion ----

    [TestMethod]
    public async Task DeletingTheFocusedNodeFromTheKeyboardGivesFocusBackToThePane()
    {
        // The browser hands the focus of a removed element to the page, and the flow's keys are heard on the pane: the
        // key after a Delete went nowhere.
        var nodes = Three();
        nodes[1].Selected = true;
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, []));

        await cut.FindAll(".bit-blow-node")[1].KeyDownAsync(new KeyboardEventArgs { Key = "Delete" });

        Assert.HasCount(2, nodes);
        Assert.IsTrue(JSInterop.Invocations.Any(i => i.Identifier == "keepFocus"), "focus was not handed back to the pane");
    }

    // ---- keys the browser must not also act on ----

    [TestMethod]
    public void TheBrowserIsToldWhichKeysDelete()
    {
        // Backspace goes back a page in WebKit, taking the graph with it, unless the flow keeps it.
        var module = JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");
        Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, []));
        var policy = (BlowInputPolicy)module.Invocations["installInteractionGuards"].Last().Arguments[4]!;
        Assert.AreEqual("Delete\nBackspace", policy.DeleteKeys);

        var off = JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");
        Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, []).Add(c => c.DeleteKeyEnabled, false));
        Assert.IsNull(((BlowInputPolicy)off.Invocations["installInteractionGuards"].Last().Arguments[4]!).DeleteKeys);
    }

    // ---- a release that never arrives ----

    [TestMethod]
    public void AMouseMovingWithNoButtonHeldEndsTheDragItsLostReleaseWouldHaveEnded()
    {
        // Let go outside the window, or taken by the browser for a drag of its own (WebKit, over selected text): without
        // this the node kept following the pointer until the next click.
        var nodes = Three();
        var stops = 0;
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, [])
            .Add(c => c.OnNodeDragStop, _ => stops++));
        var pane = cut.Find(".bit-blow");

        cut.FindAll(".bit-blow-node")[1].PointerDown(At(10, 210));
        pane.PointerMove(At(40, 230));
        Assert.IsTrue(nodes[1].Dragging);

        pane.PointerMove(new PointerEventArgs { Buttons = 0, PointerId = 1, PointerType = "mouse", ClientX = 300, ClientY = 400 });

        Assert.IsFalse(nodes[1].Dragging, "the drag outlived its release");
        Assert.AreEqual(1, stops);
        Assert.AreEqual(new BlowXYPosition(30, 220), nodes[1].Position, "the move that ended the drag also moved the node");
    }

    // ---- the minimap's own colours ----

    [TestMethod]
    public void TheMinimapPaintsItsNodesFromItsParametersAndTheSelectionWhenItHasNoColourFunction()
    {
        var nodes = Three();
        nodes[0].Selected = true;
        var plain = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, [])
            .Add(c => c.ChildContent, b => { b.OpenComponent<BlowMiniMap>(0); b.CloseComponent(); }));
        var rects = plain.FindAll(".bit-blow-minimap-node");
        Assert.AreEqual("var(--bit-blow-node-selected)", rects[0].GetAttribute("fill"));
        Assert.AreEqual("var(--bit-blow-minimap-node)", rects[1].GetAttribute("fill"));

        var coloured = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, [])
            .Add(c => c.ChildContent, b =>
            {
                b.OpenComponent<BlowMiniMap>(0);
                b.AddAttribute(1, nameof(BlowMiniMap.NodeColorFunc), (Func<BlowNode, string>)(_ => "#16a34a"));
                b.CloseComponent();
            }));
        Assert.AreEqual("#16a34a", coloured.FindAll(".bit-blow-minimap-node")[0].GetAttribute("fill"),
            "the colour function did not decide a selected node's fill");
    }

    // ---- reconnecting ----

    [TestMethod]
    public void TheReconnectAnchorsAreDrawnAboveTheNodes()
    {
        // In the edge layer under the nodes, the handle an edge ends on - its hit area at least 12 screen pixels across -
        // covered the dot it is grabbed by.
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three())
            .Add(c => c.Edges, [new BlowEdge { Id = "e", Source = "a", Target = "b", Selected = true }]));

        Assert.HasCount(2, cut.FindAll(".bit-blow-edge-reconnects .bit-blow-edge-reconnect-hit"));
        Assert.IsEmpty(cut.FindAll(".bit-blow-edges .bit-blow-edge-reconnect-hit"));
    }

    // ---- intersections asked for mid-drag ----

    [TestMethod]
    public void IntersectionsAskedForWhileDraggingSeeTheMoveJustMade()
    {
        // OnNodeDrag comes after the node moved but before the render that lays it out again: the answer was the last
        // move's, so a highlight was always one step behind and stayed wrong once the pointer stopped.
        var nodes = new List<BlowNode>
        {
            new() { Id = "drag", Position = new(0, 0), Width = 100, Height = 50 },
            new() { Id = "other", Position = new(300, 0), Width = 100, Height = 50 },
        };
        IReadOnlyList<BlowNode> overlapping = [];
        IRenderedComponent<BlowCanvas> cut = null!;
        cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, [])
            .Add(c => c.OnNodeDrag, n => overlapping = cut.Instance.GetIntersectingNodes(n)));
        var pane = cut.Find(".bit-blow");

        cut.FindAll(".bit-blow-node")[0].PointerDown(At(10, 10));
        pane.PointerMove(At(260, 10));

        CollectionAssert.AreEqual(new[] { "other" }, overlapping.Select(n => n.Id).ToArray());
    }

    // ---- a pinch with a finger on a node ----

    [TestMethod]
    public void TwoFingersZoomEvenWhenTheFirstLandedOnANode()
    {
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, []));
        var pane = cut.Find(".bit-blow");
        var zoom = cut.Instance.GetViewport().Zoom;
        PointerEventArgs Touch(long id, double x, double y) => new() { Button = 0, Buttons = 1, PointerId = id, PointerType = "touch", ClientX = x, ClientY = y };

        cut.FindAll(".bit-blow-node")[0].PointerDown(Touch(1, 10, 10));
        pane.PointerDown(Touch(2, 110, 10));
        pane.PointerMove(Touch(2, 210, 10));

        Assert.IsGreaterThan(zoom, cut.Instance.GetViewport().Zoom, "the pinch did not zoom");
        Assert.AreEqual(new BlowXYPosition(0, 0), cut.Instance.GetNode("a")!.Position, "the finger on the node dragged it");
    }

    // ---- what auto-pan says ----

    [TestMethod]
    public async Task AnAutoPanThatMovedTheViewEndsAsAMoveEnds()
    {
        // A readout of the viewport kept by OnViewportChanged stayed where the drag started.
        var changes = new List<BlowViewport>();
        var module = JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");
        module.Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, 0, 800, 600));
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, Three()).Add(c => c.Edges, [])
            .Add(c => c.OnViewportChanged, v => changes.Add(v)));
        var pane = cut.Find(".bit-blow");

        cut.FindAll(".bit-blow-node")[0].PointerDown(At(10, 10));
        pane.PointerMove(At(795, 300));
        await Task.Delay(200);
        var panned = cut.Instance.GetViewport();
        Assert.IsLessThan(0, panned.X, "the drag at the edge did not auto-pan");
        pane.PointerUp(At(795, 300));

        Assert.IsNotEmpty(changes, "OnViewportChanged was not raised for the auto-pan");
        Assert.IsLessThanOrEqualTo(panned.X, changes[^1].X);
    }

    // ---- a selection the app made ----

    [TestMethod]
    public void ClearingASelectionTheAppMadeIsReported()
    {
        var nodes = Three();
        nodes[0].Selected = true;
        var changes = new List<BlowSelectionChange>();
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, [])
            .Add(c => c.OnSelectionChange, s => changes.Add(s)));
        var pane = cut.Find(".bit-blow");

        pane.PointerDown(At(600, 600));
        pane.PointerUp(At(600, 600));
        pane.Click(new MouseEventArgs { ClientX = 600, ClientY = 600 });

        Assert.IsFalse(nodes[0].Selected);
        Assert.HasCount(1, changes, "clearing a selection never reported was not reported either");
        Assert.IsEmpty(changes[0].Nodes);
    }

    [TestMethod]
    public async Task EscapeSaysTheSelectionIsCleared()
    {
        var nodes = Three();
        nodes[0].Selected = true;
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, []));

        await cut.Find(".bit-blow").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.Contains("Selection cleared", cut.Find(".bit-blow-a11y-live").TextContent);
    }
}
