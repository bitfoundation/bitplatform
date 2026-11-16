using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blow.Tests;

/// <summary>
/// The round 19 review, of round 18's fixes: each case asserts the documented behaviour and failed on the code before
/// its fix.
/// </summary>
[TestClass]
public class RoundNineteenReviewTests : BunitContext
{
    public RoundNineteenReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitJSModuleInterop Module() => JSInterop.SetupModule("./_content/Bit.Blow/bit-blow.js");

    private IRenderedComponent<BlowCanvas> Render(
        List<BlowNode> nodes, Action<ComponentParameterCollectionBuilder<BlowCanvas>>? extra = null)
        => Render<BlowCanvas>(p =>
        {
            p.Add(c => c.Nodes, nodes);
            p.Add(c => c.Edges, []);
            extra?.Invoke(p);
        });

    private static readonly BlowDomRect AtTop = new(0, 0, 800, 600);

    // ---- the page scrolls the pane ----

    [TestMethod]
    public async Task ADragStartIsMovedByWhereThePaneWasAsItWasPressedNotByWhereItIsOnceRead()
    {
        // The page had scrolled the pane 200px up when the node was pressed, and scrolled it 50px more before the
        // reading got back. The pointer then moves only sideways - but the pane went on moving up under it, so the
        // node, which follows the flow point under the pointer, moves down by those 50px as well.
        var atPress = new BlowDomRect(0, -200, 800, 600);
        var now = new BlowDomRect(0, -250, 800, 600);
        var module = Module();
        module.Setup<BlowDomRect?>("getRect", _ => true).SetResult(now);
        module.Setup<BlowPaneRectsAtPointerDown?>("paneRectsAtPointerDown", _ => true).SetResult(new(atPress, now));
        var node = new BlowNode { Id = "n", Position = new(10, 10), Width = 20, Height = 20 };
        var cut = Render([node]);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(false, AtTop));
        cut.Instance.OnPaneMoved();

        cut.Find(".bit-blow-node").PointerDown(new PointerEventArgs { Button = 0, ClientX = 20, ClientY = -180 });
        cut.Find(".bit-blow").PointerMove(new PointerEventArgs { Buttons = 1, ClientX = 70, ClientY = -180 });

        Assert.AreEqual(new BlowXYPosition(60, 60), node.Position);
    }

    [TestMethod]
    public async Task AMoveThatWaitedOnTheReadingIsDroppedWhenAnotherGestureBeganMeanwhile()
    {
        // A reading that takes a while to come back, as over a Blazor Server circuit.
        var read = Module().Setup<BlowDomRect?>("getRect", _ => true);
        var cut = Render([new BlowNode { Id = "n", Position = new(500, 500), Width = 20, Height = 20 }]);
        await cut.InvokeAsync(() => cut.Instance.OnResizeObserved(false, AtTop));
        cut.Instance.OnPaneMoved();
        var pane = cut.Find(".bit-blow");

        // A pan, whose one move waits on the reading; it ends and another begins elsewhere before the reading is back.
        _ = pane.TriggerEventAsync("onpointerdown", new PointerEventArgs { PointerId = 1, Button = 0, ClientX = 300, ClientY = 300 });
        var move = cut.Find(".bit-blow").TriggerEventAsync("onpointermove", new PointerEventArgs { PointerId = 1, Buttons = 1, ClientX = 400, ClientY = 300 });
        await cut.Find(".bit-blow").TriggerEventAsync("onpointerup", new PointerEventArgs { PointerId = 1, Button = 0, ClientX = 400, ClientY = 300 });
        _ = cut.Find(".bit-blow").TriggerEventAsync("onpointerdown", new PointerEventArgs { PointerId = 1, Button = 0, ClientX = 100, ClientY = 100 });

        read.SetResult(AtTop);
        await move;

        // The second pan has not moved yet: the first's move, 300px from where the second began, is not applied to it.
        Assert.AreEqual(0, cut.Instance.GetViewport().X);
    }

    // ---- nodes in a node hidden since the last render ----

    private static List<BlowNode> GroupWithAChild() =>
    [
        new() { Id = "g", Position = new(0, 0), Width = 300, Height = 300 },
        new() { Id = "c", ParentId = "g", Position = new(10, 10), Width = 20, Height = 20, Selected = true },
        new() { Id = "v", Position = new(400, 10), Width = 20, Height = 20, Selected = true },
    ];

    [TestMethod]
    public void ANodeWhoseParentWasJustHiddenCannotStartAConnection()
    {
        var nodes = GroupWithAChild();
        var cut = Render(nodes);
        Assert.IsTrue(cut.Instance.CanStartConnection("c", null, BlowHandleType.Source));

        nodes.Single(n => n.Id == "g").Hidden = true;

        Assert.IsFalse(cut.Instance.CanStartConnection("c", null, BlowHandleType.Source));
        Assert.IsTrue(cut.Instance.CanStartConnection("v", null, BlowHandleType.Source));
    }

    [TestMethod]
    public async Task MovingTheSelectionLeavesANodeWhoseParentWasJustHiddenWhereItIs()
    {
        var nodes = GroupWithAChild();
        var cut = Render(nodes);

        nodes.Single(n => n.Id == "g").Hidden = true;
        await cut.InvokeAsync(() => cut.Instance.MoveSelectionAsync(10, 0));

        Assert.AreEqual(new BlowXYPosition(10, 10), nodes.Single(n => n.Id == "c").Position);
        Assert.AreEqual(new BlowXYPosition(410, 10), nodes.Single(n => n.Id == "v").Position);
    }
}
