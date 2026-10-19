using Bunit;

namespace Bit.Bortable.Tests;

/// <summary>
/// Swap mode (the SortableJS Swap plugin). Nothing moves while dragging: the item under the pointer - as
/// reported by the browser layer's <c>itemIndexAt</c> - is only highlighted, and the drop trades the two
/// items' places. Across lists a swap moves an item each way, so both lists must accept the other's item;
/// these tests pin the highlight, the trade, and the cases where no swap target may be offered.
/// </summary>
[TestClass]
public class SwapTests : BortableTestContext
{
    private const string Highlight = "bit-bortable-swap-highlight";

    private IRenderedComponent<Bortable<string>> RenderSwapList(
        List<string> items, string id = "L1", List<Raised<string>>? log = null,
        Action<ComponentParameterCollectionBuilder<Bortable<string>>>? extra = null)
        => RenderList(items, id, p =>
        {
            p.Add(c => c.Swap, true);
            p.Add(c => c.Group, "g");
            if (log is not null) Record(p, id, log);
            extra?.Invoke(p);
        });

    [TestMethod]
    public async Task TheItemUnderThePointerIsHighlightedAndNothingMoves()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderSwapList(items, log: log);

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 2);

        Assert.AreEqual("C", LabelsWithClass(list, Highlight));
        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.AreEqual("A,B,C", Labels(list));
        // Swap mode never asks for an insertion point, and a highlight is not a position change.
        Assert.IsEmpty(DropIndexCalls);
        Assert.AreEqual("Start:L1", Sequence(log));
    }

    [TestMethod]
    public async Task TheHighlightFollowsThePointer()
    {
        var list = RenderSwapList(["A", "B", "C"]);

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 2);
        await HoverItemAsync(list, 1);

        Assert.AreEqual("B", LabelsWithClass(list, Highlight));
    }

    [TestMethod]
    public async Task DroppingTradesTheTwoItems()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderSwapList(items, log: log);

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 2);
        await DropAsync(list);

        Assert.AreSequenceEqual(["C", "B", "A"], items);
        Assert.AreEqual("C,B,A", Labels(list));
        Assert.AreEqual("", LabelsWithClass(list, Highlight));
        Assert.AreEqual("Start:L1 ItemsChanged:L1 Update:L1 Sort:L1 End:L1", Sequence(log));
        var update = Single(log, "Update", "L1");
        Assert.AreEqual("A", update.Item);
        Assert.AreEqual(0, update.OldIndex);
        Assert.AreEqual(2, update.NewIndex);
    }

    [TestMethod]
    public async Task HoveringTheDraggedItemItselfOffersNoSwap()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderSwapList(items);

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 2);
        await HoverItemAsync(list, 0);

        Assert.AreEqual("", LabelsWithClass(list, Highlight));
        await DropAsync(list);
        Assert.AreSequenceEqual(["A", "B", "C"], items);
    }

    [TestMethod]
    public async Task HoveringNoItemClearsTheHighlight()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderSwapList(items);

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 1);
        await HoverItemAsync(list, -1);

        Assert.AreEqual("", LabelsWithClass(list, Highlight));
        await DropAsync(list);
        Assert.AreSequenceEqual(["A", "B", "C"], items);
    }

    [TestMethod]
    public async Task ASwapListThatCannotSortDoesNotSwapWithinItself()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderSwapList(items, extra: p => p.Add(c => c.Sort, false));

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 2);

        Assert.AreEqual("", LabelsWithClass(list, Highlight));
        await DropAsync(list);
        Assert.AreSequenceEqual(["A", "B", "C"], items);
    }

    [TestMethod]
    public async Task SwappingAcrossListsOfAGroup()
    {
        List<string> left = ["A", "B", "C"], right = ["X", "Y"];
        var log = new List<Raised<string>>();
        var l1 = RenderSwapList(left, "L1", log);
        var l2 = RenderSwapList(right, "L2", log);

        await DragStartAsync(l1, "A");
        await HoverItemAsync(l2, 1);
        Assert.AreEqual("Y", LabelsWithClass(l2, Highlight));

        await DropAsync(l2);

        Assert.AreSequenceEqual(["Y", "B", "C"], left);
        Assert.AreSequenceEqual(["X", "A"], right);
        Assert.AreEqual(
            "Start:L1 ItemsChanged:L1 ItemsChanged:L2 Remove:L1 Add:L2 Sort:L1 Sort:L2 End:L1",
            Sequence(log));
        var add = Single(log, "Add", "L2");
        Assert.AreEqual(0, add.OldIndex);
        Assert.AreEqual(1, add.NewIndex);
        Assert.IsTrue(add.CrossedLists);
    }

    [TestMethod]
    public async Task TheHighlightMovesFromOneListToAnother()
    {
        var l1 = RenderSwapList(["A", "B"], "L1");
        var l2 = RenderSwapList(["X", "Y"], "L2");

        await DragStartAsync(l1, "A");
        await HoverItemAsync(l2, 0);
        await HoverItemAsync(l1, 1);

        Assert.AreEqual("", LabelsWithClass(l2, Highlight));
        Assert.AreEqual("B", LabelsWithClass(l1, Highlight));
    }

    [TestMethod]
    public async Task ACrossListSwapNeedsTheSourceToAcceptTheTargetsItem()
    {
        List<string> left = ["A"], right = ["X"];
        // The swap would move X into L1, which takes nothing.
        var l1 = RenderSwapList(left, "L1", extra: p => p.Add(c => c.Put, false));
        var l2 = RenderSwapList(right, "L2");

        await DragStartAsync(l1, "A");
        await HoverItemAsync(l2, 0);

        Assert.AreEqual("", LabelsWithClass(l2, Highlight));
        await DropAsync(l2);
        Assert.AreSequenceEqual(["A"], left);
        Assert.AreSequenceEqual(["X"], right);
    }

    [TestMethod]
    public async Task ACrossListSwapNeedsTheTargetToLetItsItemGo()
    {
        List<string> left = ["A"], right = ["X"];
        var l1 = RenderSwapList(left, "L1");
        var l2 = RenderSwapList(right, "L2", extra: p => p.Add(c => c.Pull, BortablePull.None));

        await DragStartAsync(l1, "A");
        await HoverItemAsync(l2, 0);

        Assert.AreEqual("", LabelsWithClass(l2, Highlight));
    }

    [TestMethod]
    public async Task AnItemTheSourceAlreadyHoldsIsNotOfferedAsASwapTarget()
    {
        List<string> left = ["A", "B"], right = ["X", "B"];
        var l1 = RenderSwapList(left, "L1");
        var l2 = RenderSwapList(right, "L2");

        await DragStartAsync(l1, "A");
        // Trading A for L2's B would leave L1 holding B twice.
        await HoverItemAsync(l2, 1);

        Assert.AreEqual("", LabelsWithClass(l2, Highlight));
        await DropAsync(l2);
        Assert.AreSequenceEqual(["A", "B"], left);
        Assert.AreSequenceEqual(["X", "B"], right);
    }
}
