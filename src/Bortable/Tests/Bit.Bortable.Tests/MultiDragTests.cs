using Bunit;

namespace Bit.Bortable.Tests;

/// <summary>
/// Multi-drag (the SortableJS MultiDrag plugin). Clicks build a selection; dragging a selected item moves
/// only that one live, and the drop gathers the rest of the selection around it as one contiguous block in
/// the original order. That gather step is pure index arithmetic over two lists, so these tests pin where
/// the block lands, what the event arguments report, and when the selection is dropped.
/// </summary>
[TestClass]
public class MultiDragTests : BortableTestContext
{
    private const string Selected = "bit-bortable-selected";

    private IRenderedComponent<Bortable<string>> RenderMultiList(
        List<string> items, string id = "L1", List<Raised<string>>? log = null,
        Action<ComponentParameterCollectionBuilder<Bortable<string>>>? extra = null)
        => RenderList(items, id, p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.Group, "g");
            if (log is not null) Record(p, id, log);
            extra?.Invoke(p);
        });

    // ---- selection ----

    [TestMethod]
    public async Task ClickingAnItemTogglesItsSelection()
    {
        var list = RenderMultiList(["A", "B", "C"]);

        await ClickAsync(list, "B");
        await ClickAsync(list, "C");
        Assert.AreEqual("B,C", LabelsWithClass(list, Selected));

        await ClickAsync(list, "B");
        Assert.AreEqual("C", LabelsWithClass(list, Selected));
    }

    [TestMethod]
    public async Task AFilteredItemCannotBeSelected()
    {
        var list = RenderMultiList(["A", "Locked"], extra: p => p.Add(c => c.Filter, i => i == "Locked"));

        await ClickAsync(list, "Locked");

        Assert.AreEqual("", LabelsWithClass(list, Selected));
    }

    [TestMethod]
    public async Task ReplacingTheItemsPrunesTheSelection()
    {
        var list = RenderMultiList(["A", "B", "C"]);
        await ClickAsync(list, "A");
        await ClickAsync(list, "B");

        list.Render(p => p.Add(c => c.Items, new List<string> { "B", "C" }));
        Assert.AreEqual("B", LabelsWithClass(list, Selected));

        // A was forgotten when it left the list, so it does not come back selected.
        list.Render(p => p.Add(c => c.Items, new List<string> { "A", "B", "C" }));
        Assert.AreEqual("B", LabelsWithClass(list, Selected));
    }

    [TestMethod]
    public async Task AnItemRemovedInPlaceLeavesTheSelection()
    {
        List<string> items = ["A", "B"];
        var list = RenderMultiList(items);
        await ClickAsync(list, "A");

        // The app edits the bound list itself and re-renders: the same list instance comes back.
        items.Remove("A");
        list.Render(p => p.Add(c => c.Items, items));
        items.Insert(0, "A");
        list.Render(p => p.Add(c => c.Items, items));

        Assert.AreEqual("", LabelsWithClass(list, Selected));
    }

    [TestMethod]
    public async Task TurningMultiDragOffClearsTheSelection()
    {
        var list = RenderMultiList(["A", "B"]);
        await ClickAsync(list, "A");

        list.Render(p => p.Add(c => c.MultiDrag, false));
        list.Render(p => p.Add(c => c.MultiDrag, true));

        Assert.AreEqual("", LabelsWithClass(list, Selected));
    }

    // ---- within a list ----

    [TestMethod]
    public async Task DraggingASelectedItemMovesTheWholeSelection()
    {
        List<string> items = ["A", "B", "C", "D", "E"];
        var log = new List<Raised<string>>();
        var list = RenderMultiList(items, log: log);
        await ClickAsync(list, "B");
        await ClickAsync(list, "D");

        await DragStartAsync(list, "B");
        await DragOverAsync(list, 5);
        // Only the dragged item moves during the drag; the rest of the selection waits for the drop.
        Assert.AreSequenceEqual(["A", "C", "D", "E", "B"], items);

        await DropAsync(list);

        // B lands after the three unselected items it passed, and D joins it.
        Assert.AreSequenceEqual(["A", "C", "E", "B", "D"], items);
        Assert.AreEqual("A,C,E,B,D", Labels(list));
        var update = Single(log, "Update", "L1");
        Assert.AreEqual("B", update.Item);
        Assert.AreSequenceEqual(["B", "D"], update.Items);
        Assert.AreEqual(1, update.OldIndex);
        Assert.AreEqual(3, update.NewIndex);
    }

    [TestMethod]
    public async Task TheBlockKeepsItsOriginalOrderWhicheverItemIsDragged()
    {
        List<string> items = ["A", "B", "C", "D", "E"];
        var list = RenderMultiList(items);
        await ClickAsync(list, "A");
        await ClickAsync(list, "C");

        // C is dragged to the top, ahead of A, but the block still lands as A, C.
        await DragStartAsync(list, "C");
        await DragOverAsync(list, 0);
        await DropAsync(list);

        Assert.AreSequenceEqual(["A", "C", "B", "D", "E"], items);
    }

    [TestMethod]
    public async Task TheSelectionIsClearedOnceItHasMoved()
    {
        var list = RenderMultiList(["A", "B", "C"]);
        await ClickAsync(list, "A");
        await ClickAsync(list, "B");

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);

        Assert.AreEqual("", LabelsWithClass(list, Selected));
    }

    [TestMethod]
    public async Task StartAndChangeReportTheWholeSelection()
    {
        var log = new List<Raised<string>>();
        var list = RenderMultiList(["A", "B", "C", "D"], log: log);
        await ClickAsync(list, "B");
        await ClickAsync(list, "D");

        await DragStartAsync(list, "D");
        await DragOverAsync(list, 0);

        Assert.AreSequenceEqual(["B", "D"], Single(log, "Start", "L1").Items);
        Assert.AreSequenceEqual(["B", "D"], Single(log, "Change", "L1").Items);
    }

    [TestMethod]
    public async Task DraggingAnUnselectedItemDropsTheSelectionAndMovesOnlyThatItem()
    {
        List<string> items = ["A", "B", "C", "D"];
        var log = new List<Raised<string>>();
        var list = RenderMultiList(items, log: log);
        await ClickAsync(list, "A");
        await ClickAsync(list, "B");

        await DragStartAsync(list, "C");
        Assert.AreEqual("", LabelsWithClass(list, Selected));

        await DragOverAsync(list, 0);
        await DropAsync(list);

        Assert.AreSequenceEqual(["C", "A", "B", "D"], items);
        Assert.AreSequenceEqual(["C"], Single(log, "Update", "L1").Items);
    }

    [TestMethod]
    public async Task ASingleSelectedItemDragsLikeAnyOther()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderMultiList(items, log: log);
        await ClickAsync(list, "A");

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);

        Assert.AreSequenceEqual(["B", "C", "A"], items);
        Assert.AreSequenceEqual(["A"], Single(log, "Update", "L1").Items);
    }

    // ---- across lists ----

    [TestMethod]
    public async Task TheSelectionMovesToAnotherListAsOneBlock()
    {
        List<string> left = ["A", "B", "C", "D"], right = ["X", "Y"];
        var log = new List<Raised<string>>();
        var l1 = RenderMultiList(left, "L1", log);
        var l2 = RenderList(right, "L2", p =>
        {
            p.Add(c => c.Group, "g");
            Record(p, "L2", log);
        });
        await ClickAsync(l1, "A");
        await ClickAsync(l1, "C");

        await DragStartAsync(l1, "C");
        await DragOverAsync(l2, 1);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["B", "D"], left);
        Assert.AreSequenceEqual(["X", "A", "C", "Y"], right);
        Assert.AreEqual("X,A,C,Y", Labels(l2));
        var add = Single(log, "Add", "L2");
        Assert.AreEqual("C", add.Item);
        Assert.AreSequenceEqual(["A", "C"], add.Items);
        Assert.AreEqual(2, add.OldIndex);
        Assert.AreEqual(2, add.NewIndex);
        Assert.AreSequenceEqual(["A", "C"], Single(log, "Remove", "L1").Items);
        Assert.AreEqual("", LabelsWithClass(l1, Selected));
    }

    [TestMethod]
    public async Task ATargetThatCannotTakeEverySelectedItemRefusesTheWholeBlock()
    {
        List<string> left = ["A", "B", "C"], right = ["X", "B"];
        var l1 = RenderMultiList(left, "L1");
        var l2 = RenderList(right, "L2", p => p.Add(c => c.Group, "g"));
        await ClickAsync(l1, "A");
        await ClickAsync(l1, "B");

        // A alone would fit, but B is already in the target, and half a selection is not a multi-drag.
        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        Assert.IsNull(Placeholder(l2));
        await DropAsync(l2);

        Assert.AreSequenceEqual(["A", "B", "C"], left);
        Assert.AreSequenceEqual(["X", "B"], right);
    }

    [TestMethod]
    public async Task WithACloneOnlyTheDraggedItemIsCopied()
    {
        List<string> left = ["A", "B", "C"], right = [];
        var log = new List<Raised<string>>();
        var l1 = RenderMultiList(left, "L1", log, p => p.Add(c => c.Pull, BortablePull.Clone));
        var l2 = RenderList(right, "L2", p =>
        {
            p.Add(c => c.Group, "g");
            Record(p, "L2", log);
        });
        await ClickAsync(l1, "A");
        await ClickAsync(l1, "B");

        await DragStartAsync(l1, "B");
        await DragOverAsync(l2, 0);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["A", "B", "C"], left);
        Assert.AreSequenceEqual(["B"], right);
        Assert.AreSequenceEqual(["B"], Single(log, "Add", "L2").Items);
        // The preview already said so: only the dragged item is on its way.
        Assert.AreSequenceEqual(["B"], Single(log, "Change", "L2").Items);
    }

    [TestMethod]
    public async Task ACloneOfASelectionOfMixedTypesPreviewsOnlyTheDraggedItem()
    {
        var log = new List<Raised<string>>();
        var l1 = RenderList(new List<object> { "a", 42 }, o => o.ToString()!, "L1", p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.Group, "g");
            p.Add(c => c.Pull, BortablePull.Clone);
        });
        List<string> right = [];
        var l2 = RenderList(right, "L2", p =>
        {
            p.Add(c => c.Group, "g");
            Record(p, "L2", log);
        });
        await ClickAsync(l1, "a");
        await ClickAsync(l1, "42");

        // 42 is no string: reporting the whole selection to a list of strings would fail to cast it.
        await DragStartAsync(l1, "a");
        await DragOverAsync(l2, 0);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["a"], Single(log, "Change", "L2").Items);
        Assert.AreSequenceEqual(["a"], right);
    }
}
