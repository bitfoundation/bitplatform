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
        // Swap mode hit-tests the item under the pointer rather than a gap, and a highlight is not a position change.
        Assert.IsTrue(SentOptions(list)!.Swap);
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
    public async Task CanDropRefusesASwapTarget()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderSwapList(items, extra: p => p.Add(c => c.CanDrop, e => e.NewIndex != 2));

        await DragStartAsync(list, "A");
        Assert.AreEqual(1, await OverResultAsync(list, 1));
        Assert.AreEqual(-1, await OverResultAsync(list, 2));

        Assert.AreEqual("", LabelsWithClass(list, Highlight));
        await DropAsync(list);
        Assert.AreSequenceEqual(["A", "B", "C"], items);
    }

    [TestMethod]
    public async Task ASwapAsksCanDropAboutTheDraggedItemAlone()
    {
        IReadOnlyList<string>? asked = null;
        var list = RenderSwapList(["A", "B", "C"], extra: p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.CanDrop, e => { asked = e.Items; return true; });
        });
        await ClickAsync(list, "A");
        await ClickAsync(list, "B");

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 2);

        Assert.AreSequenceEqual(["A"], asked);
    }

    [TestMethod]
    public async Task ACrossListSwapAsksTheSourceListAboutTheItemComingBack()
    {
        List<string> left = ["A", "B"], right = ["X", "Y"];
        BortableEventArgs<string>? asked = null;
        var l1 = RenderSwapList(left, "L1", extra: p => p.Add(c => c.CanDrop, e => { asked = e; return e.Item != "Y"; }));
        var l2 = RenderSwapList(right, "L2");

        await DragStartAsync(l1, "B");
        Assert.AreEqual(-1, await OverResultAsync(l2, 1));

        Assert.AreEqual("", LabelsWithClass(l2, Highlight));
        Assert.IsNotNull(asked);
        Assert.AreEqual("Y", asked.Item);
        Assert.AreEqual("L2", asked.FromId);
        Assert.AreEqual("L1", asked.ToId);
        Assert.AreEqual(1, asked.OldIndex);
        Assert.AreEqual(1, asked.NewIndex);

        Assert.AreEqual(1, await OverResultAsync(l2, 0));
        await DropAsync(l2);
        Assert.AreSequenceEqual(["A", "X"], left);
        Assert.AreSequenceEqual(["B", "Y"], right);
    }

    [TestMethod]
    public async Task ASwapOutOfACloneListIsProposedAsTheItemItself()
    {
        List<string> left = ["A"], right = ["X"];
        BortableEventArgs<string>? asked = null;
        var l1 = RenderSwapList(left, "L1", extra: p => p.Add(c => c.Pull, BortablePull.Clone));
        var l2 = RenderSwapList(right, "L2", extra: p => p.Add(c => c.CanDrop, e => { asked = e; return true; }));

        await DragStartAsync(l1, "A");
        await HoverItemAsync(l2, 0);
        await DropAsync(l2);

        Assert.IsNotNull(asked);
        Assert.AreEqual("A", asked.Item);
        Assert.IsFalse(asked.IsClone);
        Assert.AreSequenceEqual(["X"], left);
        Assert.AreSequenceEqual(["A"], right);
    }

    [TestMethod]
    public async Task AKeyboardSwapRefusesAPartnerTheCarryingListCannotHold()
    {
        var source = new List<object> { "a", "b" };
        var words = new List<string> { "x" };
        var swap = new List<object> { 42 };
        var s = RenderList(source, o => o.ToString()!, "S", p => p.Add(c => c.Group, "g"));
        RenderList(words, w => w, "W", p => p.Add(c => c.Group, "g"));
        RenderList(swap, o => o.ToString()!, "Z", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Swap, true);
        });

        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync(KeyOf(s, "a"), "pick", null, -1));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync("", "to", "W", 0));
        var over = await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync("", "to", "Z", 0));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync("", "drop", null, -1));

        Assert.IsNull(over.Target);
        Assert.AreSequenceEqual(["a", "x"], words);
        CollectionAssert.AreEqual(new object[] { "b" }, source);
        CollectionAssert.AreEqual(new object[] { 42 }, swap);
    }

    [TestMethod]
    public async Task AKeyboardSwapRefusesAPartnerTheCarryingListAlreadyHolds()
    {
        // The swap would move Z's "x" into W, which holds an "x" already: a duplicate @key. S, the list the drag
        // started in, does not hold one, so it is W that has to be asked.
        List<string> source = ["a", "b"], words = ["x"], swap = ["x"];
        var s = RenderList(source, "S", p => p.Add(c => c.Group, "g"));
        RenderList(words, "W", p => p.Add(c => c.Group, "g"));
        var z = RenderSwapList(swap, "Z");

        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync(KeyOf(s, "a"), "pick", null, -1));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync("", "to", "W", 0));
        var over = await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync("", "to", "Z", 0));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync("", "drop", null, -1));

        Assert.IsNull(over.Target);
        Assert.AreEqual("", LabelsWithClass(z, Highlight));
        Assert.AreSequenceEqual(["a", "x"], words);
        Assert.AreSequenceEqual(["b"], source);
        Assert.AreSequenceEqual(["x"], swap);
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

    [TestMethod]
    public async Task ASwapListThatRefusesTheItemLeavesThePreviewOfAnotherListAlone()
    {
        List<string> left = ["A"], right = ["X"];
        var l1 = RenderList(left, "L1", p => p.Add(c => c.Group, "g"));
        var l2 = RenderList(right, "L2", p => p.Add(c => c.Group, "g"));
        var l3 = RenderList(["Y"], "L3", p =>
        {
            p.Add(c => c.Group, "other");
            p.Add(c => c.Swap, true);
        });

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        // On the way out the pointer crosses a swap list of another group, as a sorting list of it would.
        await HoverItemAsync(l3, 0);

        Assert.AreEqual(1, PlaceholderIndex(l2));
        Assert.AreEqual("", LabelsWithClass(l3, Highlight));

        // Released outside every list, the item still lands where it was previewed.
        await DragEndAsync(l1, "A");
        Assert.IsEmpty(left);
        Assert.AreSequenceEqual(["X", "A"], right);
    }

    [TestMethod]
    public async Task ASwapListThatRefusesTheItemLeavesTheSwapTargetOfAnotherListAlone()
    {
        List<string> left = ["A"], right = ["X"];
        var l1 = RenderSwapList(left, "L1");
        var l2 = RenderSwapList(right, "L2");
        var l3 = RenderSwapList(["Y"], "L3", extra: p => p.Add(c => c.Put, false));

        await DragStartAsync(l1, "A");
        await HoverItemAsync(l2, 0);
        await HoverItemAsync(l3, 0);

        Assert.AreEqual("X", LabelsWithClass(l2, Highlight));
        await DragEndAsync(l1, "A");
        Assert.AreSequenceEqual(["X"], left);
        Assert.AreSequenceEqual(["A"], right);
    }
}
