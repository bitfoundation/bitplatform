using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// The round 17 review, which drove the canvas in every hosting model: each case asserts the documented behaviour
/// and failed on the code before its fix.
/// </summary>
[TestClass]
public class RoundSeventeenReviewTests : BunitContext
{
    public RoundSeventeenReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitJSModuleInterop Module() => JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");

    private IRenderedComponent<BlowCanvas> Render(
        List<BlowNode> nodes, List<BlowEdge>? edges = null, Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, edges ?? []);
            extra?.Invoke(p);
        });

    // ---- the README's PanButtons = [1] with SelectionOnDrag ----

    [TestMethod]
    public void WithTheMiddleButtonPanningALeftDragDrawsTheSelectionBoxTheReadmeShows()
    {
        var node = new BlowNode { Id = "n", Position = new(10, 10), Width = 20, Height = 20 };
        var cut = Render([node], extra: p =>
        {
            p.Add(c => c.SelectionOnDrag, true);
            p.Add(c => c.PanButtons, new[] { 1 });
        });

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 0, ClientY = 0 });
        pane.PointerMove(new PointerEventArgs { ClientX = 100, ClientY = 100 });
        pane.PointerUp(new PointerEventArgs { ClientX = 100, ClientY = 100 });

        Assert.IsTrue(node.Selected);
        Assert.AreEqual(BlowViewport.Identity, cut.Instance.GetViewport());
    }

    [TestMethod]
    public void WithTheLeftButtonNotPanningAClickOnThePaneStillDeselects()
    {
        var node = new BlowNode { Id = "n", Position = new(10, 10), Width = 20, Height = 20, Selected = true };
        var cut = Render([node], extra: p => p.Add(c => c.PanButtons, new[] { 1 }));

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = 300, ClientY = 300 });
        pane.PointerUp(new PointerEventArgs { ClientX = 300, ClientY = 300 });
        pane.Click(new MouseEventArgs { Button = 0, ClientX = 300, ClientY = 300 });

        Assert.IsFalse(node.Selected);
    }

    // ---- nodes nested in a hidden node ----

    private static List<BlowNode> HiddenGroup() =>
    [
        new() { Id = "g", Hidden = true, Position = new(0, 0), Width = 200, Height = 200 },
        new() { Id = "c", ParentId = "g", Position = new(10, 10), Width = 20, Height = 20 },
        new() { Id = "v", Position = new(400, 10), Width = 20, Height = 20 },
    ];

    [TestMethod]
    public void SelectAllLeavesOutANodeNestedInAHiddenOne()
    {
        var nodes = HiddenGroup();
        var cut = Render(nodes);

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "a", CtrlKey = true });

        Assert.IsFalse(nodes.Single(n => n.Id == "c").Selected);
        Assert.IsTrue(nodes.Single(n => n.Id == "v").Selected);
    }

    [TestMethod]
    public void SelectAllThenDeleteKeepsANodeNestedInAHiddenOne()
    {
        var nodes = HiddenGroup();
        var cut = Render(nodes);

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "a", CtrlKey = true });
        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "Delete" });

        CollectionAssert.AreEquivalent(new[] { "g", "c" }, nodes.Select(n => n.Id).ToArray());
    }

    [TestMethod]
    public void TheSelectionBoxPassesOverANodeNestedInAHiddenOne()
    {
        var nodes = HiddenGroup();
        var cut = Render(nodes, extra: p => p.Add(c => c.SelectionOnDrag, true));

        var pane = cut.Find(".bit-blow");
        pane.PointerDown(new PointerEventArgs { Button = 0, ClientX = -5, ClientY = -5 });
        pane.PointerMove(new PointerEventArgs { ClientX = 600, ClientY = 300 });
        pane.PointerUp(new PointerEventArgs { ClientX = 600, ClientY = 300 });

        Assert.IsFalse(nodes.Single(n => n.Id == "c").Selected);
        Assert.IsTrue(nodes.Single(n => n.Id == "v").Selected);
    }

    [TestMethod]
    public void TheMiniMapLeavesOutANodeNestedInAHiddenOne()
    {
        var cut = Render(HiddenGroup(), extra: p => p.AddChildContent<BlowMiniMap>());

        Assert.HasCount(1, cut.FindAll(".bit-blow-minimap-node"));
    }

    // ---- two fingers ----

    [TestMethod]
    public void TwoFingersMovedTogetherPanTheView()
    {
        var cut = Render([new BlowNode { Id = "n", Position = new(500, 500), Width = 20, Height = 20 }]);
        var pane = cut.Find(".bit-blow");

        pane.PointerDown(new PointerEventArgs { PointerId = 1, PointerType = "touch", Button = 0, ClientX = 100, ClientY = 100 });
        pane.PointerDown(new PointerEventArgs { PointerId = 2, PointerType = "touch", Button = 0, ClientX = 200, ClientY = 100 });
        pane.PointerMove(new PointerEventArgs { PointerId = 1, PointerType = "touch", ClientX = 130, ClientY = 120 });
        pane.PointerMove(new PointerEventArgs { PointerId = 2, PointerType = "touch", ClientX = 230, ClientY = 120 });

        var viewport = cut.Instance.GetViewport();
        Assert.AreEqual(30, viewport.X, 1e-6);
        Assert.AreEqual(20, viewport.Y, 1e-6);
        Assert.AreEqual(1, viewport.Zoom, 1e-6);
    }

    [TestMethod]
    public void APinchKeepsThePointUnderTheFingersUnderThemAsTheyMove()
    {
        var cut = Render([new BlowNode { Id = "n", Position = new(500, 500), Width = 20, Height = 20 }]);
        var pane = cut.Find(".bit-blow");

        // The midpoint starts over flow point (150, 100), and ends at (190, 140) with the fingers twice as far apart.
        pane.PointerDown(new PointerEventArgs { PointerId = 1, PointerType = "touch", Button = 0, ClientX = 100, ClientY = 100 });
        pane.PointerDown(new PointerEventArgs { PointerId = 2, PointerType = "touch", Button = 0, ClientX = 200, ClientY = 100 });
        pane.PointerMove(new PointerEventArgs { PointerId = 1, PointerType = "touch", ClientX = 90, ClientY = 140 });
        pane.PointerMove(new PointerEventArgs { PointerId = 2, PointerType = "touch", ClientX = 290, ClientY = 140 });

        var viewport = cut.Instance.GetViewport();
        Assert.AreEqual(2, viewport.Zoom, 1e-6);
        var under = viewport.ScreenToFlow(190, 140);
        Assert.AreEqual(150, under.X, 1e-6);
        Assert.AreEqual(100, under.Y, 1e-6);
    }

    // ---- async handlers that fail ----

    [TestMethod]
    public async Task AnAsyncOnConnectHandlerThatThrowsReachesTheRenderer()
    {
        var nodes = new List<BlowNode> { new() { Id = "a" }, new() { Id = "b" } };
        var edges = new List<BlowEdge>();
        var cut = Render(nodes, edges, p => p.Add(c => c.OnConnect, EventCallback.Factory.Create<BlowConnection>(this, async _ =>
        {
            await Task.Yield();
            throw new InvalidOperationException("OnConnect failed");
        })));

        await cut.InvokeAsync(() =>
        {
            cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom);
            cut.Instance.CompleteConnection("b", null, BlowHandleType.Target);
        });

        var failure = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual("OnConnect failed", failure.Message);
        Assert.ContainsSingle(edges);
    }

    [TestMethod]
    public async Task AnAsyncOnGraphChangedHandlerThatThrowsReachesTheRenderer()
    {
        var node = new BlowNode { Id = "n", Selected = true };
        var cut = Render([node], extra: p => p.Add(c => c.OnGraphChanged, EventCallback.Factory.Create<BlowGraphChange>(this, async _ =>
        {
            await Task.Yield();
            throw new InvalidOperationException("OnGraphChanged failed");
        })));

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        var failure = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual("OnGraphChanged failed", failure.Message);
    }

    // ---- keys ----

    [TestMethod]
    [DataRow(true, false, false)]
    [DataRow(false, true, false)]
    [DataRow(false, false, true)]
    public void AnArrowWithAltCtrlOrMetaIsLeftToTheBrowser(bool alt, bool ctrl, bool meta)
    {
        var node = new BlowNode { Id = "1", Selected = true };
        var cut = Render([node]);

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = alt, CtrlKey = ctrl, MetaKey = meta });

        Assert.AreEqual(0, node.Position.X);
    }

    [TestMethod]
    public void ShiftWithAnArrowStillMovesByTheCoarseStep()
    {
        var node = new BlowNode { Id = "1", Selected = true };
        var cut = Render([node]);

        cut.Find(".bit-blow").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", ShiftKey = true });

        Assert.IsLessThan(0, node.Position.X);
    }

    // ---- what crosses a Blazor Server circuit ----

    [TestMethod]
    public async Task AHandleListensForThePointerOnlyWhileAConnectionIsDrawn()
    {
        var cut = Render([new BlowNode { Id = "a" }, new BlowNode { Id = "b" }]);

        Assert.ThrowsExactly<MissingEventHandlerException>(() => cut.FindAll(".bit-blow-handle")[0].PointerEnter(new PointerEventArgs()));

        await cut.InvokeAsync(() => cut.Instance.StartConnection("a", null, BlowHandleType.Source, BlowPosition.Bottom));

        var target = cut.FindAll(".bit-blow-handle[data-handle-type='target']").Single(h => h.GetAttribute("data-id") == "b");
        target.PointerEnter(new PointerEventArgs());
        Assert.IsTrue(cut.Instance.IsValidConnectionTarget("b", null, BlowHandleType.Target));
    }

    [TestMethod]
    public async Task AWheelAfterThePageScrolledAnchorsOnWhereThePaneIsNow()
    {
        // The pane was at the top of the page; the page then scrolled it 100px up. JS only says the rectangle is stale.
        Module().Setup<BlowDomRect?>("getRect", _ => true).SetResult(new BlowDomRect(0, -100, 800, 600));
        var cut = Render([new BlowNode { Id = "n" }]);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(false, new BlowDomRect(0, 0, 800, 600)));

        cut.Instance.OnPaneMoved();
        cut.Find(".bit-blow").Wheel(new WheelEventArgs { DeltaY = -100, ClientX = 0, ClientY = -100 });

        // The wheel is over the pane's top-left corner, which zooming about leaves where it is.
        var viewport = cut.Instance.GetViewport();
        Assert.IsGreaterThan(1, viewport.Zoom);
        Assert.AreEqual(0, viewport.X, 1e-6);
        Assert.AreEqual(0, viewport.Y, 1e-6);
    }

    [TestMethod]
    public void WithTheRightButtonPanningThePaneHoldsBackTheContextMenu()
    {
        var panning = Render([new BlowNode { Id = "n" }], extra: p => p.Add(c => c.PanButtons, new[] { 0, 2 }));
        var plain = Render([new BlowNode { Id = "n" }]);

        Assert.IsTrue(panning.Find(".bit-blow").HasAttribute("blazor:oncontextmenu:preventdefault"));
        Assert.IsFalse(plain.Find(".bit-blow").HasAttribute("blazor:oncontextmenu:preventdefault"));
    }
}
