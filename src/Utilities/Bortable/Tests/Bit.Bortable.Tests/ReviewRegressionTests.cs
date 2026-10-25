using Bunit;

namespace Bit.Bortable.Tests;

/// <summary>
/// Defects found by the round-10 review, each pinned by the scenario that showed it: a late call about a drag that has
/// ended, a swap highlight carried out of a swap list by the keyboard, a keyboard clone brought home, and an
/// <c>OnStart</c> handler that throws. Then those of the round after: a keyboard item brought back to a list that
/// cannot be sorted, an item the page replaced with an equal one, a drop said to be "back" in another list, and a move
/// made by code out of a disabled list.
/// </summary>
[TestClass]
public class ReviewRegressionTests : BortableTestContext
{
    [TestMethod]
    public async Task ACallAboutADragThatHasEndedDoesNotTouchTheNextOne()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        var first = await DragStartAsync(list, "A");
        await DropAsync(list);
        var second = await DragStartAsync(list, "B");
        Assert.AreNotEqual(first!.Drag, second!.Drag);

        // The first drag's last position and its end arrive late, on Blazor Server, after the second one started.
        Assert.AreEqual(-1, await Renderer.Dispatcher.InvokeAsync(() => Zone(list).OverAsync(3, first.Drag)));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(list).EndAsync("drop", first.Drag));

        Assert.AreEqual("B", Coordinator.Context?.Item, "The second drag is still in progress.");
        Assert.AreSequenceEqual(["A", "B", "C"], items);

        Assert.AreEqual(1, await Renderer.Dispatcher.InvokeAsync(() => Zone(list).OverAsync(3, second.Drag)));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(list).EndAsync("drop", second.Drag));
        Assert.AreSequenceEqual(["A", "C", "B"], items);
    }

    [TestMethod]
    public async Task AKeyboardDragCarriedFromASwapListIntoASortingListLeavesNoSwapBehind()
    {
        List<string> swap = ["A", "B"], sort = ["X", "Y"];
        var s = RenderList(swap, "S", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Swap, true);
        });
        RenderList(sort, "L", p => p.Add(c => c.Group, "g"));

        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync(KeyOf(s, "A"), "pick", null, -1));
        // B highlighted as the swap target, then the item carried on into the sorting list and dropped there.
        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync("", "to", null, 1));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync("", "to", "L", 1));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync("", "drop", null, -1));

        Assert.AreSequenceEqual(["B"], swap);
        Assert.AreSequenceEqual(["X", "A", "Y"], sort);
    }

    [TestMethod]
    public async Task AKeyboardCloneBroughtHomeLandsInTheGapItWasBroughtTo()
    {
        List<string> palette = ["A", "B", "C"], target = ["X"];
        var p1 = RenderList(palette, "P", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Pull, BortablePull.Clone);
            p.Add(c => c.Clone, s => s + "'");
        });
        RenderList(target, "T", p => p.Add(c => c.Group, "g"));

        await Renderer.Dispatcher.InvokeAsync(() => Zone(p1).KeyAsync(KeyOf(p1, "A"), "pick", null, -1));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(p1).KeyAsync("", "to", "T", 0));
        Assert.AreSequenceEqual(["A'", "X"], target);

        // Back into the palette, into the gap between B and C - counted, as the browser counts it, with A still there.
        await Renderer.Dispatcher.InvokeAsync(() => Zone(p1).KeyAsync("", "to", "P", 2));

        Assert.AreSequenceEqual(["X"], target);
        Assert.AreSequenceEqual(["B", "A", "C"], palette);
    }

    [TestMethod]
    public async Task AnOnStartThatThrowsLeavesNoDragBehind()
    {
        List<string> items = ["A", "B"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p =>
        {
            p.Add(c => c.OnStart, (BortableEventArgs<string> _) => throw new InvalidOperationException("boom"));
            p.Add(c => c.OnEnd, e => log.Add(new("L1", "End", e)));
        });

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => DragStartAsync(list, "A"));

        Assert.IsNull(Coordinator.Context);
        Assert.IsTrue(Single(log, "End", "L1").Canceled);
        Assert.IsFalse(HasClass(list, "A", "bit-bortable-ghost"));

        // A move made by code is made and reported all the same, and the exception reaches the caller after.
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => list.InvokeAsync(() => list.Instance.MoveAsync("A", 1)));
        Assert.AreSequenceEqual(["B", "A"], items);
        Assert.HasCount(2, log.Where(r => r.Name == "End"));
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task AKeyboardCloneBroughtBackToAReadOnlyPaletteLeavesItAsItWas()
    {
        string[] palette = ["A", "B", "C"];
        List<string> target = ["X"];
        var p1 = RenderList(palette, "P", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Pull, BortablePull.Clone);
            p.Add(c => c.Clone, s => s + "'");
        });
        RenderList(target, "T", p => p.Add(c => c.Group, "g"));

        await Renderer.Dispatcher.InvokeAsync(() => Zone(p1).KeyAsync(KeyOf(p1, "A"), "pick", null, -1));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(p1).KeyAsync("", "to", "T", 0));
        // Back over the palette, an array that can be neither sorted nor changed: the copy goes, the original stays.
        var back = await Renderer.Dispatcher.InvokeAsync(() => Zone(p1).KeyAsync("", "to", "P", 3));

        Assert.IsTrue(back.Dragging);
        Assert.AreEqual("P", back.List);
        Assert.AreSequenceEqual(["X"], target);
        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, palette);
    }

    [TestMethod]
    public async Task AKeyboardItemBroughtBackToAListThatCannotSortReturnsToItsPlace()
    {
        List<string> left = ["A", "B", "C"], right = ["X"];
        var l1 = RenderList(left, "L1", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Sort, false);
        });
        RenderList(right, "L2", p => p.Add(c => c.Group, "g"));

        await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync(KeyOf(l1, "A"), "pick", null, -1));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync("", "to", "L2", 0));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync("", "to", "L1", 2));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync("", "drop", null, -1));

        Assert.AreSequenceEqual(["A", "B", "C"], left);
        Assert.AreSequenceEqual(["X"], right);
    }

    [TestMethod]
    public async Task AnArrowInAListThatCannotSortIsSaidToBeRefused()
    {
        List<string> items = ["A", "B"];
        var list = RenderList(items, extra: p => p.Add(c => c.Sort, false));

        await Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync(KeyOf(list, "A"), "pick", null, -1));
        var result = await Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync("", "to", null, 1));

        Assert.AreEqual("A cannot be moved there.", result.Announce);
        Assert.AreSequenceEqual(["A", "B"], items);
    }

    [TestMethod]
    public async Task ADragMovesTheInstanceTheListHoldsNotTheOneItFirstRendered()
    {
        var first = new Entity(1, "Old");
        List<Entity> items = [first, new(2, "B")];
        var list = RenderList(items, e => e.Name);
        var key = KeyOf(list, "Old");

        // The page replaces the item with an updated one, equal to it (the same id), and renders again.
        var updated = new Entity(1, "New");
        items[0] = updated;
        list.Render(p => p.Add(c => c.Items, items));

        await StartByKeyAsync(list, key);
        await DragOverAsync(list, 2);
        await DropAsync(list);

        Assert.AreSame(updated, items[1]);
        Assert.AreEqual("New", items[1].Name);
    }

    [TestMethod]
    public async Task ADropIntoAnotherUnnamedListAtTheSamePositionIsNotSaidToBeBack()
    {
        List<string> left = ["A"], right = ["X"];
        var l1 = RenderList(left, "L1", p => p.Add(c => c.Group, "g"));
        RenderList(right, "L2", p => p.Add(c => c.Group, "g"));

        await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync(KeyOf(l1, "A"), "pick", null, -1));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync("", "to", "L2", 0));
        var dropped = await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync("", "drop", null, -1));

        Assert.AreEqual("Dropped A at position 1 of 2.", dropped.Announce);
    }

    [TestMethod]
    public async Task MoveToAsyncFromADisabledListMovesNothing()
    {
        List<string> left = ["A"], right = ["X"];
        var l1 = RenderList(left, "L1", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Disabled, true);
        });
        RenderList(right, "L2", p => p.Add(c => c.Group, "g"));

        Assert.IsFalse(await l1.InvokeAsync(() => l1.Instance.MoveToAsync("A", "L2", 0)));

        Assert.AreSequenceEqual(["A"], left);
        Assert.AreSequenceEqual(["X"], right);
    }

    /// <summary>An item compared by its id, as an entity often is.</summary>
    private sealed class Entity(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public override bool Equals(object? obj) => obj is Entity other && other.Id == Id;
        public override int GetHashCode() => Id;
    }
}
