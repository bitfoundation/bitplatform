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
}
