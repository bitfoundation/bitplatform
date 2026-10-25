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
        Assert.IsTrue(attached.All(a => (bool)a[1]! == false), "vertical: up and down");
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
}
