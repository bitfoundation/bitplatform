using Bunit;

namespace Bit.Bortable.Tests;

/// <summary>
/// A drag as an operation with a defined outcome: proposed while the pointer moves (and vetted by
/// <c>CanDrop</c> and <c>MaxItems</c>), committed when it is dropped (and confirmed by <c>ConfirmDrop</c>), or put
/// back - by a cancel, by a release outside every list with <c>RevertOnSpill</c>, or by a refused confirmation -
/// leaving no partial change behind. The events carry how the drag was made and how it ended.
/// </summary>
[TestClass]
public class OperationTests : BortableTestContext
{
    private (IRenderedComponent<Bortable<string>> L1, IRenderedComponent<Bortable<string>> L2) RenderPair(
        List<string> left, List<string> right, List<Raised<string>>? log = null,
        Action<ComponentParameterCollectionBuilder<Bortable<string>>>? leftExtra = null,
        Action<ComponentParameterCollectionBuilder<Bortable<string>>>? rightExtra = null)
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

    // ---- cancelling and spilling ----

    [TestMethod]
    public async Task ACancelPutsALiveReorderBack()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        Assert.AreSequenceEqual(["B", "C", "A"], items);
        await EndAsync(list, "cancel");

        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.AreEqual("A,B,C", Labels(list));
        Assert.AreEqual("Start:L1 Change:L1 End:L1", Sequence(log));
        var end = Single(log, "End", "L1");
        Assert.IsTrue(end.Canceled);
        Assert.AreEqual(0, end.NewIndex);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task ACancelDropsACrossListPreviewAndShowsTheOriginalAgain()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var log = new List<Raised<string>>();
        var (l1, l2) = RenderPair(left, right, log);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        await EndAsync(l1, "cancel");

        Assert.AreSequenceEqual(["A", "B"], left);
        Assert.AreSequenceEqual(["X"], right);
        Assert.IsNull(Placeholder(l2));
        Assert.IsFalse(HasClass(l1, "A", "bit-bortable-hidden"));
        Assert.IsFalse(log.Any(r => r.Name is "ItemsChanged" or "Add" or "Remove" or "Sort"));
        Assert.IsTrue(Single(log, "End", "L1").Canceled);
    }

    [TestMethod]
    public async Task WithoutRevertOnSpillAReleaseOutsideKeepsWhatTheDragDid()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var (l1, l2) = RenderPair(left, right);

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        await DragEndAsync(l1, "A");

        // SortableJS: once previewed in a list, the item lands there wherever it is released.
        Assert.AreSequenceEqual(["B"], left);
        Assert.AreSequenceEqual(["X", "A"], right);
    }

    [TestMethod]
    public async Task WithRevertOnSpillAReleaseOutsidePutsEverythingBack()
    {
        List<string> left = ["A", "B", "C"], right = ["X"];
        var log = new List<Raised<string>>();
        var (l1, l2) = RenderPair(left, right, log, leftExtra: p => p.Add(c => c.RevertOnSpill, true));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l1, 3);
        await DragOverAsync(l2, 0);
        await DragEndAsync(l1, "A");

        Assert.AreSequenceEqual(["A", "B", "C"], left);
        Assert.AreSequenceEqual(["X"], right);
        Assert.IsFalse(log.Any(r => r.Name == "ItemsChanged"));
        Assert.IsTrue(Single(log, "End", "L1").Canceled);
    }

    [TestMethod]
    public async Task WithRevertOnSpillADropOnAListStillCommits()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var (l1, l2) = RenderPair(left, right, leftExtra: p => p.Add(c => c.RevertOnSpill, true));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["B"], left);
        Assert.AreSequenceEqual(["A", "X"], right);
    }

    [TestMethod]
    public async Task ACancelKeepsAnEditTheAppMadeMidDrag()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items);

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        // Someone else adds an item while A is dragged.
        items.Add("D");
        list.Render(p => p.Add(c => c.Items, items));
        await EndAsync(list, "cancel");

        // The cancel undoes the drag's own move, not the app's edit.
        Assert.AreSequenceEqual(["A", "B", "C", "D"], items);
    }

    [TestMethod]
    public async Task ACancelledSwapTradesNothing()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items, extra: p => p.Add(c => c.Swap, true));

        await DragStartAsync(list, "A");
        await HoverItemAsync(list, 2);
        await EndAsync(list, "cancel");

        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.AreEqual("", LabelsWithClass(list, "bit-bortable-swap-highlight"));
    }

    // ---- CanDrop ----

    [TestMethod]
    public async Task CanDropRefusesAPositionWithinTheList()
    {
        List<string> items = ["Pinned", "A", "B"];
        var list = RenderList(items, extra: p => p.Add(c => c.CanDrop, e => e.NewIndex > 0));

        await DragStartAsync(list, "B");

        Assert.AreEqual(-1, await OverResultAsync(list, 0), "Above the pinned item.");
        Assert.AreSequenceEqual(["Pinned", "A", "B"], items);
        Assert.AreEqual(1, await OverResultAsync(list, 1));
        Assert.AreSequenceEqual(["Pinned", "B", "A"], items);
    }

    [TestMethod]
    public async Task CanDropIsAskedWithTheProposedMove()
    {
        List<string> left = ["A", "B"], right = ["X", "Y"];
        BortableEventArgs<string>? asked = null;
        var (l1, l2) = RenderPair(left, right, rightExtra: p => p.Add(c => c.CanDrop, e => { asked = e; return true; }));

        await DragStartAsync(l1, "B");
        await DragOverAsync(l2, 1);

        Assert.IsNotNull(asked);
        Assert.AreEqual("B", asked.Item);
        Assert.AreEqual(1, asked.OldIndex);
        Assert.AreEqual(1, asked.NewIndex);
        Assert.AreEqual("L1", asked.FromId);
        Assert.AreEqual("L2", asked.ToId);
        Assert.AreEqual(BortableInput.Mouse, asked.Input);
        Assert.IsFalse(asked.IsClone);
    }

    [TestMethod]
    public async Task CanDropRefusesAnItemFromAnotherList()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var (l1, l2) = RenderPair(left, right, rightExtra: p => p.Add(c => c.CanDrop, e => e.Item != "A"));

        await DragStartAsync(l1, "A");
        Assert.AreEqual(-1, await OverResultAsync(l2, 0));
        Assert.IsNull(Placeholder(l2));
        Assert.IsFalse(HasClass(l1, "A", "bit-bortable-hidden"));
        await DropAsync(l2);

        Assert.AreSequenceEqual(["A", "B"], left);
        Assert.AreSequenceEqual(["X"], right);
    }

    [TestMethod]
    public async Task ACanDropThatThrowsRefusesThePositionAndTheDragGoesOn()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p =>
        {
            p.Add(c => c.CanDrop, e => e.NewIndex == 2 ? throw new InvalidOperationException("offline") : true);
            Record(p, "L1", log);
        });

        await DragStartAsync(list, "A");

        // Refused, and passed on as the list's own: the browser layer has no use for it.
        Assert.AreEqual(-1, await OverResultAsync(list, 3));
        Assert.AreEqual("offline", (await PassedOnAsync()).Message);
        Assert.AreSequenceEqual(["A", "B", "C"], items);

        Assert.IsNotNull(Coordinator.Context, "The drag goes on.");
        Assert.AreEqual(1, await OverResultAsync(list, 2));
        await DropAsync(list);

        Assert.AreSequenceEqual(["B", "A", "C"], items);
        Assert.IsFalse(Single(log, "End", "L1").Canceled);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task AMoveMadeByCodeThatCanDropThrowsOnIsReportedAndThrowsToTheCaller()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p =>
        {
            p.Add(c => c.CanDrop, e => e.NewIndex == 0 ? throw new InvalidOperationException("offline") : true);
            Record(p, "L1", log);
        });

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => list.InvokeAsync(() => list.Instance.MoveAsync("C", 0)));

        Assert.AreEqual("offline", error.Message);
        Assert.AreSequenceEqual(["A", "B", "C"], items);
        // The drag the move made is over, and reported: OnEnd still follows OnStart.
        Assert.IsNull(Coordinator.Context);
        Assert.AreEqual("Start:L1 End:L1", Sequence(log));
        Assert.IsTrue(Single(log, "End", "L1").Canceled);

        Assert.IsTrue(await list.InvokeAsync(() => list.Instance.MoveAsync("C", 1)), "The next move is not blocked.");
        Assert.AreSequenceEqual(["A", "C", "B"], items);
    }

    [TestMethod]
    public async Task ARefusedPositionKeepsThePreviewWhereItWas()
    {
        List<string> left = ["A"], right = ["X", "Y", "Z"];
        var (l1, l2) = RenderPair(left, right, rightExtra: p => p.Add(c => c.CanDrop, e => e.NewIndex != 0));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 2);
        Assert.AreEqual(-1, await OverResultAsync(l2, 0));

        Assert.AreEqual(2, PlaceholderIndex(l2));
    }

    // ---- MaxItems ----

    [TestMethod]
    public async Task AFullListDoesNotTakeTheDrag()
    {
        List<string> left = ["A"], right = ["X", "Y"];
        var (l1, l2) = RenderPair(left, right, rightExtra: p => p.Add(c => c.MaxItems, 2));

        var started = await DragStartAsync(l1, "A");

        Assert.DoesNotContain("L2", started!.Lists);
        Assert.IsFalse(await DragOverAsync(l2, 0));
    }

    [TestMethod]
    public async Task AListWithRoomTakesItemsUntilItIsFull()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var (l1, l2) = RenderPair(left, right, rightExtra: p => p.Add(c => c.MaxItems, 2));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        await DropAsync(l2);
        var second = await DragStartAsync(l1, "B");

        Assert.AreSequenceEqual(["X", "A"], right);
        Assert.DoesNotContain("L2", second!.Lists);
    }

    [TestMethod]
    public async Task AFullListStillSortsItsOwnItems()
    {
        List<string> items = ["A", "B"];
        var list = RenderList(items, extra: p => p.Add(c => c.MaxItems, 2));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 2);
        await DropAsync(list);

        Assert.AreSequenceEqual(["B", "A"], items);
    }

    [TestMethod]
    public async Task AMultiDragCountsEveryItemItCarries()
    {
        List<string> left = ["A", "B", "C"], right = ["X"];
        var (l1, _) = RenderPair(left, right,
            leftExtra: p => p.Add(c => c.MultiDrag, true),
            rightExtra: p => p.Add(c => c.MaxItems, 3));

        await ClickAsync(l1, "A");
        await ClickAsync(l1, "B");
        await ClickAsync(l1, "C");
        var started = await DragStartAsync(l1, "A");

        // 1 + 3 is more than 3.
        Assert.DoesNotContain("L2", started!.Lists);
    }

    // ---- ConfirmDrop ----

    [TestMethod]
    public async Task AConfirmedDropRaisesItsEventsOnceConfirmed()
    {
        List<string> left = ["A"], right = ["X"];
        var log = new List<Raised<string>>();
        var answer = new TaskCompletionSource<bool>();
        var (l1, l2) = RenderPair(left, right, log, rightExtra: p => p.Add(c => c.ConfirmDrop, _ => answer.Task));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        var drop = DropAsync(l2);

        // Shown at once, reported once confirmed.
        Assert.AreSequenceEqual(["X", "A"], right);
        Assert.IsFalse(log.Any(r => r.Name is "ItemsChanged" or "Add" or "End"));

        answer.SetResult(true);
        await drop;

        Assert.AreEqual("Start:L1 Change:L2 ItemsChanged:L1 ItemsChanged:L2 Remove:L1 Add:L2 Sort:L1 Sort:L2 End:L1", Sequence(log));
        Assert.IsFalse(Single(log, "End", "L1").Canceled);
    }

    [TestMethod]
    public async Task ARefusedDropPutsEveryListBack()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var log = new List<Raised<string>>();
        BortableEventArgs<string>? asked = null;
        var (l1, l2) = RenderPair(left, right, log, rightExtra: p => p.Add(c => c.ConfirmDrop, e => { asked = e; return Task.FromResult(false); }));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["A", "B"], left);
        Assert.AreSequenceEqual(["X"], right);
        Assert.AreEqual("A,B", Labels(l1));
        Assert.AreEqual("X", Labels(l2));
        // ItemsChanged tells the page the lists it may have rendered with the drop are back; nothing else is raised.
        Assert.AreEqual("Start:L1 Change:L2 ItemsChanged:L1 ItemsChanged:L2 End:L1", Sequence(log));
        Assert.IsTrue(Single(log, "End", "L1").Canceled);
        // It was asked about the move as committed: A at 0 of L2.
        Assert.AreEqual("L2", asked!.ToId);
        Assert.AreEqual(0, asked.NewIndex);
    }

    [TestMethod]
    public async Task ARefusedReorderIsPutBack()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items, extra: p => p.Add(c => c.ConfirmDrop, _ => Task.FromResult(false)));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);

        Assert.AreSequenceEqual(["A", "B", "C"], items);
    }

    [TestMethod]
    public async Task NoDragStartsWhileADropIsConfirmed()
    {
        List<string> items = ["A", "B", "C"];
        var answer = new TaskCompletionSource<bool>();
        var list = RenderList(items, extra: p => p.Add(c => c.ConfirmDrop, _ => answer.Task));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        var drop = DropAsync(list);

        Assert.IsNull(await DragStartAsync(list, "B"));
        Assert.IsFalse(await list.InvokeAsync(() => list.Instance.MoveAsync("B", 0)));

        answer.SetResult(true);
        await drop;
        Assert.IsNotNull(await DragStartAsync(list, "B"));
    }

    [TestMethod]
    public async Task ADragAskedForAsADropEndsDoesNotStartOnListsTheDropMayPutBack()
    {
        List<string> items = ["A", "B", "C"];
        var answer = new TaskCompletionSource<bool>();
        var list = RenderList(items, extra: p => p.Add(c => c.ConfirmDrop, _ => answer.Task));
        var b = KeyOf(list, "B");

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);

        // The drop and the next drag both wait for the gate, the drop first: the drag was asked for before the drop
        // was being confirmed, and gets the gate once it is.
        await Coordinator.Gate.WaitAsync();
        var drop = DropAsync(list);
        var start = StartByKeyAsync(list, b);
        Coordinator.Gate.Release();

        Assert.IsNull(await start);
        Assert.IsNull(Coordinator.Context);

        answer.SetResult(false);
        await drop;
        Assert.AreSequenceEqual(["A", "B", "C"], items);
    }

    [TestMethod]
    public async Task ADropThatChangesNothingIsNotConfirmed()
    {
        int asked = 0;
        var list = RenderList(["A", "B"], extra: p => p.Add(c => c.ConfirmDrop, _ => { asked++; return Task.FromResult(true); }));

        await DragStartAsync(list, "A");
        await DropAsync(list);

        Assert.AreEqual(0, asked);
    }

    [TestMethod]
    public async Task AConfirmationThatThrowsPutsTheListsBack()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p =>
        {
            p.Add(c => c.ConfirmDrop, _ => throw new InvalidOperationException("offline"));
            Record(p, "L1", log);
        });

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);

        await DropAsync(list);
        Assert.AreEqual("offline", (await PassedOnAsync()).Message);
        Assert.AreSequenceEqual(["A", "B", "C"], items);
        // OnEnd still follows OnStart, reporting the drag cancelled.
        Assert.IsTrue(Single(log, "End", "L1").Canceled);
        Assert.IsNotNull(await DragStartAsync(list, "B"), "The next drag is not blocked.");
    }

    // ---- what the events report ----

    [TestMethod]
    public async Task TheEventsReportHowTheDragWasMade()
    {
        var log = new List<Raised<string>>();
        var list = RenderList(["A", "B"], "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "A", input: "touch");
        await DragOverAsync(list, 2);
        await DropAsync(list);
        await DragStartAsync(list, "A", input: "pen");
        await DropAsync(list);

        Assert.IsTrue(log.Take(5).Where(r => r.Args is not null).All(r => r.Args!.Input == BortableInput.Touch));
        Assert.AreEqual(BortableInput.Pen, log[^1].Args!.Input);
    }

    [TestMethod]
    public async Task ACloneIsReportedAsOne()
    {
        var log = new List<Raised<Card>>();
        List<Card> source = [new("a")], target = [];
        var s = RenderList(source, c => c.Name, "S", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Pull, BortablePull.Clone);
            p.Add(c => c.Clone, c => new Card(c.Name));
        });
        var t = RenderList(target, c => c.Name, "T", p =>
        {
            p.Add(c => c.Group, "g");
            Record(p, "T", log);
        });

        await DragStartAsync(s, "a");
        await DragOverAsync(t, 0);
        await DropAsync(t);

        Assert.IsTrue(log.Where(r => r.Args is not null).All(r => r.Args!.IsClone));
        Assert.AreNotSame(source[0], Single(log, "Add", "T").Item);
    }

    private sealed class Card(string name)
    {
        public string Name { get; } = name;
    }

    // ---- moves made by code ----

    [TestMethod]
    public async Task MoveAsyncMovesAnItemAsADragWould()
    {
        List<string> items = ["A", "B", "C", "D"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        Assert.IsTrue(await list.InvokeAsync(() => list.Instance.MoveAsync("A", 2)));

        Assert.AreSequenceEqual(["B", "C", "A", "D"], items);
        Assert.AreEqual("Start:L1 Change:L1 ItemsChanged:L1 Update:L1 Sort:L1 End:L1", Sequence(log));
        var update = Single(log, "Update", "L1");
        Assert.AreEqual(BortableInput.Api, update.Input);
        Assert.AreEqual(0, update.OldIndex);
        Assert.AreEqual(2, update.NewIndex);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task MoveAsyncMovesUpAndToTheEnds()
    {
        List<string> items = ["A", "B", "C", "D"];
        var list = RenderList(items);

        Assert.IsTrue(await list.InvokeAsync(() => list.Instance.MoveAsync("D", 0)));
        Assert.AreSequenceEqual(["D", "A", "B", "C"], items);
        Assert.IsTrue(await list.InvokeAsync(() => list.Instance.MoveAsync("D", 3)));
        Assert.AreSequenceEqual(["A", "B", "C", "D"], items);
        Assert.IsTrue(await list.InvokeAsync(() => list.Instance.MoveAsync("C", 1)));
        Assert.AreSequenceEqual(["A", "C", "B", "D"], items);
    }

    [TestMethod]
    public async Task MoveAsyncRefusesWhatADragCouldNotDo()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items, extra: p => p.Add(c => c.CanDrop, e => e.NewIndex != 0));

        Assert.IsFalse(await list.InvokeAsync(() => list.Instance.MoveAsync("B", 0)), "CanDrop refuses it.");
        Assert.IsFalse(await list.InvokeAsync(() => list.Instance.MoveAsync("B", 1)), "Already there.");
        Assert.IsFalse(await list.InvokeAsync(() => list.Instance.MoveAsync("B", 3)), "Out of range.");
        Assert.IsFalse(await list.InvokeAsync(() => list.Instance.MoveAsync("Z", 0)), "Not in the list.");
        list.Render(p => p.Add(c => c.Sort, false));
        Assert.IsFalse(await list.InvokeAsync(() => list.Instance.MoveAsync("C", 1)), "Sort is off.");

        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task MoveAsyncWaitsForNoDragButTheUsers()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items);

        await DragStartAsync(list, "A");

        Assert.IsFalse(await list.InvokeAsync(() => list.Instance.MoveAsync("C", 0)));
        Assert.AreEqual("A", Coordinator.Context?.Item);
    }

    [TestMethod]
    public async Task MoveAsyncMovesALockedItem()
    {
        List<string> items = ["A", "Locked"];
        var list = RenderList(items, extra: p => p.Add(c => c.Filter, i => i == "Locked"));

        Assert.IsTrue(await list.InvokeAsync(() => list.Instance.MoveAsync("Locked", 0)));
        Assert.AreSequenceEqual(["Locked", "A"], items);
    }

    [TestMethod]
    public async Task MoveToAsyncMovesAnItemIntoAnotherList()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var log = new List<Raised<string>>();
        var (l1, _) = RenderPair(left, right, log);

        Assert.IsTrue(await l1.InvokeAsync(() => l1.Instance.MoveToAsync("B", "L2", 0)));

        Assert.AreSequenceEqual(["A"], left);
        Assert.AreSequenceEqual(["B", "X"], right);
        Assert.AreEqual("Start:L1 Change:L2 ItemsChanged:L1 ItemsChanged:L2 Remove:L1 Add:L2 Sort:L1 Sort:L2 End:L1", Sequence(log));
        Assert.AreEqual(BortableInput.Api, Single(log, "Add", "L2").Input);
    }

    [TestMethod]
    public async Task MoveToAsyncFollowsTheRulesOfTheLists()
    {
        List<string> left = ["A"], right = ["X"];
        var (l1, l2) = RenderPair(left, right, rightExtra: p => p.Add(c => c.Put, false));

        Assert.IsFalse(await l1.InvokeAsync(() => l1.Instance.MoveToAsync("A", "L2", 0)), "Put is off.");
        Assert.IsFalse(await l1.InvokeAsync(() => l1.Instance.MoveToAsync("A", "nowhere", 0)), "No such list.");
        Assert.IsFalse(await l1.InvokeAsync(() => l1.Instance.MoveToAsync("A", "L2", 5)), "Out of range.");

        Assert.AreSequenceEqual(["A"], left);
        Assert.AreSequenceEqual(["X"], right);
        Assert.IsNull(Placeholder(l2));
    }

    [TestMethod]
    public async Task MoveToAsyncFromACloneSourcePutsACopyThere()
    {
        List<string> palette = ["Red"], target = [];
        var (l1, _) = RenderPair(palette, target, leftExtra: p =>
        {
            p.Add(c => c.Pull, BortablePull.Clone);
            p.Add(c => c.Clone, s => s + " copy");
        });

        Assert.IsTrue(await l1.InvokeAsync(() => l1.Instance.MoveToAsync("Red", "L2", 0)));

        Assert.AreSequenceEqual(["Red"], palette);
        Assert.AreSequenceEqual(["Red copy"], target);
    }

    [TestMethod]
    public async Task MoveToAsyncIntoASwapListTradesPlaces()
    {
        List<string> left = ["A", "B"], right = ["X", "Y"];
        var (l1, _) = RenderPair(left, right,
            leftExtra: p => p.Add(c => c.Swap, true),
            rightExtra: p => p.Add(c => c.Swap, true));

        Assert.IsTrue(await l1.InvokeAsync(() => l1.Instance.MoveToAsync("A", "L2", 1)));

        Assert.AreSequenceEqual(["Y", "B"], left);
        Assert.AreSequenceEqual(["X", "A"], right);
    }

    [TestMethod]
    public async Task MoveAsyncAsksConfirmDropAndRespectsItsAnswer()
    {
        List<string> items = ["A", "B"];
        var list = RenderList(items, extra: p => p.Add(c => c.ConfirmDrop, _ => Task.FromResult(false)));

        Assert.IsFalse(await list.InvokeAsync(() => list.Instance.MoveAsync("A", 1)));
        Assert.AreSequenceEqual(["A", "B"], items);
    }
}
