using Bunit;

namespace Bit.Bortable.Tests;

/// <summary>
/// Moving items between lists. Unlike a within-list sort, a cross-list move is only previewed during the
/// drag (a placeholder in the target, the original hidden in the source) and committed when the drag ends,
/// because moving the DOM node between components mid-drag would recreate it and cancel the browser's drag.
/// These tests pin that two-phase behaviour, the event order across both lists, and every pull/put rule
/// that must refuse a move.
/// </summary>
[TestClass]
public class SharedListTests : BortableTestContext
{
    private const string Hidden = "bit-bortable-hidden";

    private (IRenderedComponent<Bortable<string>> L1, IRenderedComponent<Bortable<string>> L2) RenderPair(
        List<string> left, List<string> right,
        Action<ComponentParameterCollectionBuilder<Bortable<string>>>? leftExtra = null,
        Action<ComponentParameterCollectionBuilder<Bortable<string>>>? rightExtra = null,
        List<Raised<string>>? log = null)
    {
        var l1 = RenderList(left, "L1", p =>
        {
            p.Add(c => c.Group, "g");
            if (log is not null) Record(p, "L1", log);
            leftExtra?.Invoke(p);
        });
        var l2 = RenderList(right, "L2", p =>
        {
            p.Add(c => c.Group, "g");
            if (log is not null) Record(p, "L2", log);
            rightExtra?.Invoke(p);
        });
        return (l1, l2);
    }

    // ---- preview ----

    [TestMethod]
    public async Task HoveringAListOfTheGroupPreviewsTheDropAndHidesTheOriginal()
    {
        List<string> left = ["A", "B", "C"], right = ["X", "Y"];
        var (l1, l2) = RenderPair(left, right);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);

        var placeholder = Placeholder(l2);
        Assert.IsNotNull(placeholder);
        Assert.AreEqual(1, PlaceholderIndex(l2));
        Assert.AreEqual("A", placeholder.TextContent.Trim());
        Assert.IsTrue(placeholder.ClassList.Contains("bit-bortable-ghost"));
        Assert.AreEqual("true", placeholder.GetAttribute("aria-hidden"));
        Assert.IsTrue(HasClass(l1, "A", Hidden));
        // Nothing has moved yet: the data changes only when the drag ends.
        Assert.AreSequenceEqual(["A", "B", "C"], left);
        Assert.AreSequenceEqual(["X", "Y"], right);
    }

    [TestMethod]
    public async Task ThePreviewRaisesChangeOnTheTarget()
    {
        var log = new List<Raised<string>>();
        var (l1, l2) = RenderPair(["A", "B"], ["X", "Y"], log: log);

        await DragStartAsync(l1, "B");
        await DragOverAsync(l2, 2);
        await DragOverAsync(l2, 2); // same slot: no second event

        Assert.AreEqual("Start:L1 Change:L2", Sequence(log));
        var change = Single(log, "Change", "L2");
        Assert.AreEqual(1, change.OldIndex);
        Assert.AreEqual(2, change.NewIndex);
        Assert.AreEqual("L1", change.FromId);
        Assert.AreEqual("L2", change.ToId);
    }

    [TestMethod]
    public async Task ThePlaceholderFollowsThePointerWithinTheTarget()
    {
        var (l1, l2) = RenderPair(["A"], ["X", "Y", "Z"]);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        Assert.AreEqual(0, PlaceholderIndex(l2));

        await DragOverAsync(l2, 3);
        Assert.AreEqual(3, PlaceholderIndex(l2));
        Assert.ContainsSingle(l2.FindAll("[data-bortable-placeholder]"));
    }

    [TestMethod]
    public async Task ADropIndexPastTheEndOfTheTargetIsClamped()
    {
        List<string> right = ["X", "Y"];
        var (l1, l2) = RenderPair(["A"], right);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 42);
        Assert.AreEqual(2, PlaceholderIndex(l2));

        await DropAsync(l2);
        Assert.AreSequenceEqual(["X", "Y", "A"], right);
    }

    [TestMethod]
    public async Task ADeadZoneOverTheTargetKeepsThePreviewWhereItWas()
    {
        var (l1, l2) = RenderPair(["A"], ["X", "Y"]);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, -1);
        Assert.IsNull(Placeholder(l2));

        await DragOverAsync(l2, 1);
        await DragOverAsync(l2, -1);
        Assert.AreEqual(1, PlaceholderIndex(l2));
    }

    [TestMethod]
    public async Task ThePlaceholderMovesFromOneTargetToAnother()
    {
        List<string> middle = ["M"], last = ["Z"];
        var (l1, l2) = RenderPair(["A"], middle);
        var l3 = RenderList(last, "L3", p => p.Add(c => c.Group, "g"));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        await DragOverAsync(l3, 1);

        Assert.IsNull(Placeholder(l2));
        Assert.AreEqual(1, PlaceholderIndex(l3));

        await DropAsync(l3);
        Assert.AreSequenceEqual(["M"], middle);
        Assert.AreSequenceEqual(["Z", "A"], last);
    }

    [TestMethod]
    public async Task ThePlaceholderUsesTheTargetsItemClassesForTheDraggedItem()
    {
        var (l1, l2) = RenderPair(["Banana"], ["X"], rightExtra: p =>
        {
            p.Add(c => c.ItemClass, "card");
            p.Add(c => c.ItemClassSelector, i => i.StartsWith('B') ? "tinted" : null);
        });

        await DragStartAsync(l1, "Banana");
        await DragOverAsync(l2, 0);

        // The preview should look like the item will once dropped there.
        var classes = Placeholder(l2)!.ClassList;
        Assert.IsTrue(classes.Contains("bit-bortable-item"));
        Assert.IsTrue(classes.Contains("card"));
        Assert.IsTrue(classes.Contains("tinted"));
        Assert.IsTrue(classes.Contains("bit-bortable-chosen"));
    }

    // ---- commit ----

    [TestMethod]
    public async Task DroppingMovesTheItemIntoTheTarget()
    {
        List<string> left = ["A", "B", "C"], right = ["X", "Y"];
        var (l1, l2) = RenderPair(left, right);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["B", "C"], left);
        Assert.AreSequenceEqual(["X", "A", "Y"], right);
        Assert.AreEqual("B,C", Labels(l1));
        Assert.AreEqual("X,A,Y", Labels(l2));
        Assert.IsNull(Placeholder(l2));
        Assert.AreEqual("", LabelsWithClass(l2, "bit-bortable-ghost"));
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task ACrossListMoveRaisesRemoveAddSortSortEndInOrder()
    {
        var log = new List<Raised<string>>();
        var (l1, l2) = RenderPair(["A", "B", "C"], ["X", "Y"], log: log);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        await DropAsync(l2);

        Assert.AreEqual(
            "Start:L1 Change:L2 ItemsChanged:L1 ItemsChanged:L2 Remove:L1 Add:L2 Sort:L1 Sort:L2 End:L1",
            Sequence(log));
        foreach (var raised in log.Where(r => r.Args is not null && r.Name is not ("Start" or "Change")))
        {
            var e = raised.Args!;
            Assert.AreEqual("A", e.Item);
            Assert.AreEqual(0, e.OldIndex);
            Assert.AreEqual(1, e.NewIndex);
            Assert.AreEqual("L1", e.FromId);
            Assert.AreEqual("L2", e.ToId);
            Assert.AreEqual("g", e.FromGroup);
            Assert.AreEqual("g", e.ToGroup);
            Assert.IsTrue(e.CrossedLists);
        }
    }

    [TestMethod]
    public async Task ReleasingOutsideAnyListStillCommitsThePreviewedMove()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var log = new List<Raised<string>>();
        var (l1, l2) = RenderPair(left, right, log: log);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        // SortableJS keeps an item where it was last previewed; only dragend arrives, on the source item.
        await DragEndAsync(l1, "A");

        Assert.AreSequenceEqual(["B"], left);
        Assert.AreSequenceEqual(["A", "X"], right);
        Assert.AreEqual("End:L1", Sequence(log.TakeLast(1)));
    }

    [TestMethod]
    public async Task DroppingIntoAnEmptyList()
    {
        List<string> left = ["A"], right = [];
        var (l1, l2) = RenderPair(left, right);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        await DropAsync(l2);

        Assert.IsEmpty(left);
        Assert.AreSequenceEqual(["A"], right);
    }

    [TestMethod]
    public async Task ItemsCanBeMovedBackAndForth()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var (l1, l2) = RenderPair(left, right);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        await DropAsync(l2);

        await DragStartAsync(l2, "A");
        await DragOverAsync(l1, 2);
        await DropAsync(l1);

        Assert.AreSequenceEqual(["B", "A"], left);
        Assert.AreSequenceEqual(["X"], right);
    }

    // ---- coming back ----

    [TestMethod]
    public async Task HoveringTheHomeListAgainTakesTheItemBack()
    {
        List<string> left = ["A", "B", "C"], right = ["X"];
        var log = new List<Raised<string>>();
        var (l1, l2) = RenderPair(left, right, log: log);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        await DragOverAsync(l1, 0);

        Assert.IsNull(Placeholder(l2));
        Assert.IsFalse(HasClass(l1, "A", Hidden));
        Assert.IsTrue(HasClass(l1, "A", "bit-bortable-ghost"));

        await DropAsync(l1);
        Assert.AreSequenceEqual(["A", "B", "C"], left);
        Assert.AreSequenceEqual(["X"], right);
        Assert.AreEqual("Start:L1 Change:L2 End:L1", Sequence(log));
    }

    [TestMethod]
    public async Task ComingBackAtAnotherPositionReordersTheHomeList()
    {
        List<string> left = ["A", "B", "C"], right = ["X"];
        var log = new List<Raised<string>>();
        var (l1, l2) = RenderPair(left, right, log: log);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        // The drop index of the home list still counts the hidden original.
        await DragOverAsync(l1, 3);
        await DropAsync(l1);

        Assert.AreSequenceEqual(["B", "C", "A"], left);
        Assert.AreSequenceEqual(["X"], right);
        Assert.IsTrue(log.Any(r => r.Name == "Update" && r.List == "L1"));
        Assert.IsFalse(log.Any(r => r.List == "L2" && r.Name is "Add" or "Sort" or "ItemsChanged"));
    }

    [TestMethod]
    public async Task ASourceThatCannotSortRestoresTheItemAtItsOriginalPlace()
    {
        List<string> left = ["A", "B", "C"], right = ["X"];
        var (l1, l2) = RenderPair(left, right, leftExtra: p => p.Add(c => c.Sort, false));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        await DragOverAsync(l1, 3);

        Assert.IsNull(Placeholder(l2));
        Assert.AreEqual("A,B,C", Labels(l1));
        Assert.IsFalse(HasClass(l1, "A", Hidden));

        await DropAsync(l1);
        Assert.AreSequenceEqual(["A", "B", "C"], left);
        Assert.AreSequenceEqual(["X"], right);
    }

    [TestMethod]
    public async Task ASourceThatCannotSortCanStillGiveItemsAway()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var (l1, l2) = RenderPair(left, right, leftExtra: p => p.Add(c => c.Sort, false));

        await DragStartAsync(l1, "B");
        await DragOverAsync(l2, 0);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["A"], left);
        Assert.AreSequenceEqual(["B", "X"], right);
    }

    [TestMethod]
    public async Task ATargetThatCannotSortStillReceivesItems()
    {
        List<string> left = ["A"], right = ["X", "Y"];
        var (l1, l2) = RenderPair(left, right, rightExtra: p => p.Add(c => c.Sort, false));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        await DropAsync(l2);

        // Sort governs the list's own order, not what it accepts: the item lands where it was previewed.
        Assert.IsEmpty(left);
        Assert.AreSequenceEqual(["X", "A", "Y"], right);
    }

    // ---- refusals ----

    [TestMethod]
    public async Task ListsInDifferentGroupsDoNotExchangeItems()
    {
        List<string> left = ["A"], right = ["X"];
        var l1 = RenderList(left, "L1", p => p.Add(c => c.Group, "g"));
        // Group names are compared ordinally: "G" is another group.
        var l2 = RenderList(right, "L2", p => p.Add(c => c.Group, "G"));

        await AssertRefusedAsync(l1, l2, left, right);
    }

    [TestMethod]
    public async Task ListsWithoutAGroupDoNotExchangeItems()
    {
        List<string> left = ["A"], right = ["X"];
        var l1 = RenderList(left, "L1");
        var l2 = RenderList(right, "L2");

        await AssertRefusedAsync(l1, l2, left, right);
    }

    [TestMethod]
    public async Task AListWithoutAGroupDoesNotTakeItemsFromAGroupedOne()
    {
        List<string> left = ["A"], right = ["X"];
        var l1 = RenderList(left, "L1", p => p.Add(c => c.Group, "g"));
        var l2 = RenderList(right, "L2");

        await AssertRefusedAsync(l1, l2, left, right);
    }

    [TestMethod]
    public async Task PutFalseRefusesItems()
    {
        List<string> left = ["A"], right = ["X"];
        var (l1, l2) = RenderPair(left, right, rightExtra: p => p.Add(c => c.Put, false));

        await AssertRefusedAsync(l1, l2, left, right);
    }

    [TestMethod]
    public async Task ADisabledTargetRefusesItems()
    {
        List<string> left = ["A"], right = ["X"];
        var (l1, l2) = RenderPair(left, right, rightExtra: p => p.Add(c => c.Disabled, true));

        await AssertRefusedAsync(l1, l2, left, right);
    }

    [TestMethod]
    public async Task PullNoneKeepsItemsInTheirList()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var (l1, l2) = RenderPair(left, right, leftExtra: p => p.Add(c => c.Pull, BortablePull.None));

        await AssertRefusedAsync(l1, l2, left, right);
    }

    [TestMethod]
    public async Task PullNoneStillAllowsSortingWithinTheList()
    {
        List<string> left = ["A", "B"];
        var (l1, _) = RenderPair(left, ["X"], leftExtra: p => p.Add(c => c.Pull, BortablePull.None));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l1, 2);
        await DropAsync(l1);

        Assert.AreSequenceEqual(["B", "A"], left);
    }

    [TestMethod]
    public async Task ATargetAlreadyHoldingAnEqualItemRefusesIt()
    {
        // Blazor needs a unique @key per item, so a second "A" could not even be rendered.
        List<string> left = ["A"], right = ["X", "A"];
        var (l1, l2) = RenderPair(left, right);

        await AssertRefusedAsync(l1, l2, left, right);
    }

    [TestMethod]
    public async Task AListOfAnotherItemTypeIgnoresTheItem()
    {
        List<string> left = ["A"];
        List<int> numbers = [1, 2];
        var log = new List<Raised<string>>();
        var l1 = RenderList(left, "L1", p =>
        {
            p.Add(c => c.Group, "g");
            Record(p, "L1", log);
        });
        var l2 = RenderList(numbers, n => n.ToString(), "L2", p => p.Add(c => c.Group, "g"));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        Assert.IsNull(Placeholder(l2));

        await DropAsync(l2);
        Assert.AreSequenceEqual(["A"], left);
        Assert.AreSequenceEqual([1, 2], numbers);
        Assert.AreEqual("Start:L1 End:L1", Sequence(log));
    }

    [TestMethod]
    public async Task AListNestedInsideTheDraggedItemRefusesIt()
    {
        List<string> left = ["A"], right = ["X"];
        var (l1, l2) = RenderPair(left, right);

        // The browser layer reports that the hovered list sits inside the dragged item (a nested list):
        // accepting it would put the item inside itself.
        WouldNest = true;
        await AssertRefusedAsync(l1, l2, left, right);
    }

    /// <summary>Drags the first item of <paramref name="l1"/> over <paramref name="l2"/> and drops it there.</summary>
    private async Task AssertRefusedAsync<TTarget>(
        IRenderedComponent<Bortable<string>> l1, IRenderedComponent<Bortable<TTarget>> l2,
        List<string> left, List<TTarget> right)
    {
        var before = (Left: left.ToList(), Right: right.ToList());

        await DragStartAsync(l1, left[0]);
        await DragOverAsync(l2, 0);

        Assert.IsNull(Placeholder(l2), "A refusing list must not preview the item.");
        Assert.IsFalse(HasClass(l1, left[0], Hidden), "The original must stay visible when no list takes it.");

        await DropAsync(l2);

        Assert.AreSequenceEqual(before.Left, left);
        Assert.AreSequenceEqual(before.Right, right);
        Assert.IsNull(Coordinator.Context);
    }
}
