using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <see cref="BmotionReorderGroup{TItem}"/>: every item renders as a draggable row, and a
/// drag that carries a row past its neighbour's midpoint commits the new order on release - driven
/// here through the same drag callbacks the bridge makes, against a fake 50px-per-row layout.
/// </summary>
[TestClass]
public class ReorderGroupTests
{
    private const double RowHeight = 50;

    private static readonly RenderFragment<string> Row = item => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "row");
        b.AddContent(2, item);
        b.CloseElement();
    };

    private static string[] RowIds(IRenderedComponent<BmotionReorderGroup<string>> cut)
        => cut.FindAll("div.row").Select(d => d.Id ?? "").ToArray();

    private static string[] RowTexts(IRenderedComponent<BmotionReorderGroup<string>> cut)
        => cut.FindAll("div.row").Select(d => d.TextContent).ToArray();

    // Stacks the rows top to bottom in the order they were first rendered (their untransformed layout).
    private static void UseVerticalLayout(BmotionTestContext ctx, string[] ids)
        => ctx.Interop.BoundingRectProvider = id =>
        {
            int index = Array.IndexOf(ids, id);
            return index < 0 ? null : new BmotionBoundingRect { Left = 0, Top = index * RowHeight, Width = 200, Height = RowHeight };
        };

    // Drags the row with the given id down by dy and releases it, the way the bridge reports it, then
    // ticks the frame loop until the release has settled. The release callback itself only returns
    // once the row has animated into its slot - which takes frames - so it is awaited last.
    private static async Task DragRow(BmotionTestContext ctx, IRenderedComponent<BmotionReorderGroup<string>> cut, string id, double dy)
    {
        var row = (Bmotion)ctx.Interop.EventListenerRefs[id];
        await cut.InvokeAsync(row.OnPointerDown_Drag);
        await cut.InvokeAsync(() => row.SetDragPosition(0, dy));
        await cut.InvokeAsync(row.OnDragMove);
        var release = cut.InvokeAsync(() => row.OnPointerUp_Drag(0, 0));
        await ctx.RunFramesAsync(0, 3000);
        await release.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [TestMethod]
    public void RendersEveryItemAsAVerticallyDraggableRow()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b", "c" })
            .Add(p => p.ChildContent, Row));

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, RowTexts(cut));
        var ids = RowIds(cut);
        Assert.AreEqual(3, ids.Distinct().Count(), "each row gets its own engine id");
        foreach (var id in ids)
        {
            var flags = (IReadOnlyDictionary<string, object?>)ctx.Interop.Calls
                .Last(c => c.Method == "attachEventListeners" && (string?)c.Args[0] == id).Args[1]!;
            Assert.AreEqual(true, flags["drag"]);
            Assert.AreEqual("y", flags["dragAxis"], "the default axis is vertical");
            Assert.AreEqual(true, flags["dragAutoScroll"], "a row carried to the edge of its scroller scrolls it");
        }
    }

    [TestMethod]
    public void AutoScroll_CanBeTurnedOff()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b" })
            .Add(p => p.AutoScroll, false)
            .Add(p => p.ChildContent, Row));

        foreach (var id in RowIds(cut))
        {
            var flags = (IReadOnlyDictionary<string, object?>)ctx.Interop.Calls
                .Last(c => c.Method == "attachEventListeners" && (string?)c.Args[0] == id).Args[1]!;
            Assert.IsFalse(flags.ContainsKey("dragAutoScroll"));
        }
    }

    [TestMethod]
    public async Task DraggingARowPastItsNeighbour_CommitsTheNewOrderOnRelease()
    {
        using var ctx = new BmotionTestContext();
        List<string>? committed = null;
        int reorders = 0;
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b", "c" })
            .Add(p => p.ItemsChanged, EventCallback.Factory.Create<List<string>>(this, l => committed = l))
            .Add(p => p.OnReorder, EventCallback.Factory.Create(this, () => reorders++))
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseVerticalLayout(ctx, ids);

        // "a" (centre 25) dragged 60px down: its centre (85) passes b's slot centre (75) but not c's (125).
        // The release settles the row into its new slot on the frame loop; the commit follows it.
        await DragRow(ctx, cut, ids[0], 60);

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "b", "a", "c" }, committed));
        Assert.AreEqual(1, reorders);
        CollectionAssert.AreEqual(new[] { "b", "a", "c" }, RowTexts(cut), "the rows re-render in the committed order");
    }

    [TestMethod]
    public async Task ADragThatDoesNotCrossAMidpoint_CommitsNothing()
    {
        using var ctx = new BmotionTestContext();
        bool changed = false;
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b", "c" })
            .Add(p => p.ItemsChanged, EventCallback.Factory.Create<List<string>>(this, _ => changed = true))
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseVerticalLayout(ctx, ids);

        await DragRow(ctx, cut, ids[0], 20); // centre 45 < 75
        await cut.InvokeAsync(() => { });

        Assert.IsFalse(changed);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, RowTexts(cut));
    }

    [TestMethod]
    public async Task WithoutABinding_TheBoundListIsReorderedInPlace()
    {
        using var ctx = new BmotionTestContext();
        var items = new List<string> { "a", "b", "c" };
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, items)
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseVerticalLayout(ctx, ids);

        // "c" (centre 125) dragged 110px up: past b's (75) and a's (25) slot centres.
        await DragRow(ctx, cut, ids[2], -110);

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "c", "a", "b" }, items));
    }

    [TestMethod]
    public void DuplicateKeys_AreRejected()
    {
        using var ctx = new BmotionTestContext();
        Assert.ThrowsExactly<InvalidOperationException>(() => ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "a" })
            .Add(p => p.ChildContent, Row)));
    }

    // ── Keyboard ──────────────────────────────────────────────────────────────

    private static BmotionReorderGroup<string> KeysOf(BmotionTestContext ctx, string id)
        => (BmotionReorderGroup<string>)ctx.Interop.ReorderKeyRefs[id];

    // Runs a step that settles on the frame loop (a drop animates the item into its slot before committing).
    private static async Task Settle(BmotionTestContext ctx, Task step)
    {
        await ctx.RunFramesAsync(0, 3000);
        await ctx.FinishWaapiAsync();
        await step.WaitAsync(TimeSpan.FromSeconds(10));
    }

    private static string[] Announced(BmotionTestContext ctx)
        => ctx.Interop.ArgsOf("announce").Select(a => (string)a[0]!).ToArray();

    [TestMethod]
    public void EveryRow_ListensForTheKeyboard_AlongTheListsAxis()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b", "c" })
            .Add(p => p.ChildContent, Row));

        var attached = ctx.Interop.ArgsOf("attachReorderKeys");
        CollectionAssert.AreEquivalent(RowIds(cut), attached.Select(a => (string)a[0]!).ToArray());
        Assert.IsTrue(attached.All(a => (string)a[1]! == "y"), "vertical: up and down");
    }

    [TestMethod]
    public void WithKeyboardOff_NoRowListens()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b" })
            .Add(p => p.Keyboard, false)
            .Add(p => p.ChildContent, Row));

        Assert.IsFalse(ctx.Interop.WasCalled("attachReorderKeys"));
    }

    [TestMethod]
    public async Task PickUp_MoveTwice_Drop_CommitsTheNewOrder_AnnouncesEachStep_AndKeepsFocus()
    {
        using var ctx = new BmotionTestContext();
        List<string>? committed = null;
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b", "c" })
            .Add(p => p.ItemsChanged, EventCallback.Factory.Create<List<string>>(this, l => committed = l))
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseVerticalLayout(ctx, ids);
        var keys = KeysOf(ctx, ids[0]);

        Assert.IsTrue(await cut.InvokeAsync(() => keys.OnReorderPickUp(ids[0])));
        await cut.InvokeAsync(() => keys.OnReorderMove(ids[0], 1));
        await cut.InvokeAsync(() => keys.OnReorderMove(ids[0], 1));
        await cut.InvokeAsync(() => keys.OnReorderMove(ids[0], 1)); // already last: nothing to do
        Assert.IsNull(committed, "nothing is committed while the item is held");
        await Settle(ctx, cut.InvokeAsync(() => keys.OnReorderDrop(ids[0])));

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "b", "c", "a" }, committed));
        CollectionAssert.AreEqual(new[] { "b", "c", "a" }, RowTexts(cut));
        CollectionAssert.AreEqual(new[]
        {
            "Picked up, position 1 of 3. Arrow keys move it, Space drops it, Escape cancels.",
            "Position 2 of 3.",
            "Position 3 of 3.",
            "Dropped at position 3 of 3.",
        }, Announced(ctx));
        cut.WaitForAssertion(() => Assert.AreEqual(ids[0], ctx.Interop.ArgsOf("focusElement").LastOrDefault()?[0], "focus stays on the moved item"));
    }

    [TestMethod]
    public async Task Escape_PutsTheItemBack_AndCommitsNothing()
    {
        using var ctx = new BmotionTestContext();
        bool changed = false;
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b", "c" })
            .Add(p => p.ItemsChanged, EventCallback.Factory.Create<List<string>>(this, _ => changed = true))
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseVerticalLayout(ctx, ids);
        var keys = KeysOf(ctx, ids[1]);

        await cut.InvokeAsync(() => keys.OnReorderPickUp(ids[1]));
        await cut.InvokeAsync(() => keys.OnReorderMove(ids[1], -1));
        await Settle(ctx, cut.InvokeAsync(() => keys.OnReorderCancel(ids[1])));

        Assert.IsFalse(changed);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, RowTexts(cut));
        Assert.AreEqual("Cancelled, back at position 2 of 3.", Announced(ctx)[^1]);
        // Picking up again works: the cancelled session is over.
        Assert.IsTrue(await cut.InvokeAsync(() => keys.OnReorderPickUp(ids[1])));
    }

    [TestMethod]
    public async Task OnlyOneItem_IsHeldAtATime()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b" })
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseVerticalLayout(ctx, ids);

        Assert.IsTrue(await cut.InvokeAsync(() => KeysOf(ctx, ids[0]).OnReorderPickUp(ids[0])));
        Assert.IsFalse(await cut.InvokeAsync(() => KeysOf(ctx, ids[1]).OnReorderPickUp(ids[1])));
    }

    [TestMethod]
    public async Task APointerDrag_StartedWhileAPickUpIsMeasuring_WaitsForIt_ThenPutsTheItemBack()
    {
        using var ctx = new BmotionTestContext();
        List<string>? committed = null;
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b", "c" })
            .Add(p => p.ItemsChanged, EventCallback.Factory.Create<List<string>>(this, l => committed = l))
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseVerticalLayout(ctx, ids);
        // The pick-up's measurement is a round trip still on its way (Blazor Server) when the pointer takes over.
        var measured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ctx.Interop.BoundingRectGate = measured.Task;
        var pickUp = cut.InvokeAsync(() => KeysOf(ctx, ids[2]).OnReorderPickUp(ids[2]));
        var row = (Bmotion)ctx.Interop.EventListenerRefs[ids[0]];
        var dragStart = cut.InvokeAsync(row.OnPointerDown_Drag);

        measured.SetResult();
        Assert.IsTrue(await pickUp.WaitAsync(TimeSpan.FromSeconds(10)));
        await dragStart.WaitAsync(TimeSpan.FromSeconds(10));
        await cut.InvokeAsync(() => row.SetDragPosition(0, 60));
        await cut.InvokeAsync(row.OnDragMove);
        await Settle(ctx, cut.InvokeAsync(() => row.OnPointerUp_Drag(0, 0)));

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "b", "a", "c" }, committed));
        Assert.AreEqual("Cancelled, back at position 3 of 3.", Announced(ctx)[^1], "the held item went back first");
    }

    [TestMethod]
    public void ANewAxisOrHandle_ReattachesEveryRow()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b" })
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);

        cut.Render(ps => ps.Add(p => p.Axis, BmDragAxis.X).Add(p => p.HandleSelector, ".grip"));

        foreach (var id in ids)
        {
            var latest = ctx.Interop.ArgsOf("attachReorderKeys").Last(a => (string)a[0]! == id);
            Assert.AreEqual("x", latest[1]);
            Assert.AreEqual(".grip", latest[2]);
        }
    }

    [TestMethod]
    public async Task TurningTheKeyboardOff_PutsAHeldItemBack_AndDetachesEveryRow()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b", "c" })
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseVerticalLayout(ctx, ids);
        await cut.InvokeAsync(() => KeysOf(ctx, ids[0]).OnReorderPickUp(ids[0]));

        cut.Render(ps => ps.Add(p => p.Keyboard, false));

        cut.WaitForAssertion(() => CollectionAssert.AreEquivalent(ids, ctx.Interop.ArgsOf("detachReorderKeys").Select(a => (string)a[0]!).ToArray()));
        Assert.AreEqual("Cancelled, back at position 1 of 3.", Announced(ctx)[^1]);

        cut.Render(ps => ps.Add(p => p.Keyboard, true));
        cut.WaitForAssertion(() => Assert.AreEqual(2 * ids.Length, ctx.Interop.CountOf("attachReorderKeys"), "turned back on, every row listens again"));
    }

    [TestMethod]
    public async Task Announcements_CanBeWorded()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "a", "b" })
            .Add(p => p.Announce, a => $"{a.Item}: {a.Event} {a.Position}/{a.Count}")
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseVerticalLayout(ctx, ids);

        await cut.InvokeAsync(() => KeysOf(ctx, ids[0]).OnReorderPickUp(ids[0]));
        await cut.InvokeAsync(() => KeysOf(ctx, ids[0]).OnReorderMove(ids[0], 1));

        CollectionAssert.AreEqual(new[] { "a: PickedUp 1/2", "a: Moved 2/2" }, Announced(ctx));
    }

    // ── Grids ─────────────────────────────────────────────────────────────────

    // Three columns of 60x40 cells, 4px apart, in the order the items were first rendered.
    private static void UseGridLayout(BmotionTestContext ctx, string[] ids)
        => ctx.Interop.BoundingRectProvider = id =>
        {
            int index = Array.IndexOf(ids, id);
            return index < 0 ? null : new BmotionBoundingRect { Left = (index % 3) * 64, Top = (index / 3) * 44, Width = 60, Height = 40 };
        };

    [TestMethod]
    public async Task AGridItem_TakesTheCellNearestItsCentre()
    {
        using var ctx = new BmotionTestContext();
        List<string>? committed = null;
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "1", "2", "3", "4", "5", "6" })
            .Add(p => p.Axis, BmDragAxis.Both)
            .Add(p => p.ItemsChanged, EventCallback.Factory.Create<List<string>>(this, l => committed = l))
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseGridLayout(ctx, ids);
        var flags = (IReadOnlyDictionary<string, object?>)ctx.Interop.Calls.Last(c => c.Method == "attachEventListeners").Args[1]!;
        Assert.IsNull(flags.GetValueOrDefault("dragAxis"), "a grid item drags freely");

        // "1" dragged a cell right and a cell down: its centre sits in what was "5"'s cell.
        var row = (Bmotion)ctx.Interop.EventListenerRefs[ids[0]];
        await cut.InvokeAsync(row.OnPointerDown_Drag);
        await cut.InvokeAsync(() => row.SetDragPosition(64, 44));
        await cut.InvokeAsync(row.OnDragMove);
        var release = cut.InvokeAsync(() => row.OnPointerUp_Drag(0, 0));
        await Settle(ctx, release);

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "2", "3", "4", "5", "1", "6" }, committed));
    }

    [TestMethod]
    public async Task AGridItem_MovesARowAtATime_FromTheKeyboard()
    {
        using var ctx = new BmotionTestContext();
        List<string>? committed = null;
        var cut = ctx.Render<BmotionReorderGroup<string>>(ps => ps
            .Add(p => p.Items, new List<string> { "1", "2", "3", "4", "5", "6" })
            .Add(p => p.Axis, BmDragAxis.Both)
            .Add(p => p.ItemsChanged, EventCallback.Factory.Create<List<string>>(this, l => committed = l))
            .Add(p => p.ChildContent, Row));
        var ids = RowIds(cut);
        UseGridLayout(ctx, ids);
        Assert.AreEqual("both", ctx.Interop.ArgsOf("attachReorderKeys").First()[1]);
        var keys = KeysOf(ctx, ids[5]);

        await cut.InvokeAsync(() => keys.OnReorderPickUp(ids[5]));
        await cut.InvokeAsync(() => keys.OnReorderMoveRow(ids[5], -1));
        await Settle(ctx, cut.InvokeAsync(() => keys.OnReorderDrop(ids[5])));

        cut.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "1", "2", "6", "3", "4", "5" }, committed));
    }
}
