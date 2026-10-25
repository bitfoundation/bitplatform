using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bortable.Tests;

/// <summary>
/// Dragging without a pointer, and what assistive technology is told. The browser layer turns keys into actions -
/// <c>pick</c> (Space or Enter on an item), <c>to</c> (an arrow, Home or End: the item or list in that direction,
/// which the browser works out from the layout), <c>drop</c> and <c>cancel</c> - and the list carries them out,
/// moving the item itself (into another list too), and answers what to announce and where the item is now, for the
/// focus to follow it.
/// </summary>
[TestClass]
public class KeyboardTests : BortableTestContext
{
    /// <summary>Space or Enter on the item labelled <paramref name="label"/>.</summary>
    private Task<BortableKeyResult> PickAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, string label)
        => Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync(KeyOf(list, label), "pick", null, -1));

    /// <summary>A step of the keyboard drag in progress, reported through the bridge of <paramref name="list"/>.</summary>

    private Task<BortableKeyResult> KeyAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, string action, string? toList = null, int index = -1)
        => Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync("", action, toList, index));

    // ---- the markup ----

    [TestMethod]
    public void TheListIsAListOfFocusableItemsWithOneTabStop()
    {
        var list = RenderList(["A", "B", "C"], "L1", p => p.Add(c => c.AriaLabel, "Tasks"));

        var container = Container(list);
        Assert.AreEqual("list", container.GetAttribute("role"));
        Assert.AreEqual("Tasks", container.GetAttribute("aria-label"));

        var items = list.FindAll("[data-bortable-item]");
        Assert.IsTrue(items.All(i => i.GetAttribute("role") == "listitem"));
        Assert.AreSequenceEqual(["0", "-1", "-1"], items.Select(i => i.GetAttribute("tabindex")).ToList());
        Assert.IsTrue(items.All(i => i.GetAttribute("aria-roledescription") == "sortable item"));
        Assert.IsTrue(items.All(i => i.GetAttribute("aria-describedby") == "L1-instructions"));

        var instructions = list.Find("#L1-instructions");
        Assert.IsTrue(instructions.HasAttribute("hidden"));
        StringAssert.Contains(instructions.TextContent, "Space or Enter");
    }

    [TestMethod]
    public async Task TheFocusedItemBecomesTheTabStop()
    {
        var list = RenderList(["A", "B", "C"]);

        await Renderer.Dispatcher.InvokeAsync(() => Zone(list).Focus(KeyOf(list, "C")));

        Assert.AreSequenceEqual(["-1", "-1", "0"], list.FindAll("[data-bortable-item]").Select(i => i.GetAttribute("tabindex")).ToList());
    }

    [TestMethod]
    public void WithoutTheKeyboardOrWhenDisabledNoItemIsFocusable()
    {
        foreach (var setup in new Action<ComponentParameterCollectionBuilder<Bortable<string>>>[] { p => p.Add(c => c.Keyboard, false), p => p.Add(c => c.Disabled, true) })
        {
            var list = RenderList(["A", "B"], extra: setup);

            var items = list.FindAll("[data-bortable-item]");
            Assert.IsTrue(items.All(i => !i.HasAttribute("tabindex") && !i.HasAttribute("aria-describedby")));
            Assert.IsEmpty(list.FindAll(".bit-bortable-instructions"));
            Assert.IsFalse(SentOptions(list)!.Keyboard);
        }
    }

    [TestMethod]
    public void TheTextsCanBeReplaced()
    {
        var messages = new BortableMessages { Instructions = "Leertaste zum Aufnehmen.", RoleDescription = "sortierbares Element" };
        var list = RenderList(["A"], "L1", p => p.Add(c => c.Messages, messages));

        Assert.AreEqual("sortierbares Element", list.Find("[data-bortable-item]").GetAttribute("aria-roledescription"));
        Assert.AreEqual("Leertaste zum Aufnehmen.", list.Find("#L1-instructions").TextContent);
    }

    [TestMethod]
    public void OtherAttributesGoToTheContainer()
    {
        var list = RenderList(["A"], extra: p =>
        {
            p.AddUnmatched("data-test", "board");
            p.AddUnmatched("title", "Backlog");
        });

        Assert.AreEqual("board", Container(list).GetAttribute("data-test"));
        Assert.AreEqual("Backlog", Container(list).GetAttribute("title"));
    }

    // ---- picking up ----

    [TestMethod]
    public async Task SpaceOnAnItemPicksItUpAndSaysWhereItIs()
    {
        var log = new List<Raised<string>>();
        var list = RenderList(["A", "B", "C"], "L1", p =>
        {
            p.Add(c => c.AriaLabel, "To do");
            Record(p, "L1", log);
        });

        var result = await PickAsync(list, "B");

        Assert.IsTrue(result.Dragging);
        Assert.AreEqual("Picked up B. It is at position 2 of 3 in To do.", result.Announce);
        Assert.AreEqual("L1", result.List);
        Assert.AreEqual(KeyOf(list, "B"), result.Key);
        Assert.AreSequenceEqual(["L1"], result.Lists!.ToList());
        Assert.AreEqual(BortableInput.Keyboard, Single(log, "Start", "L1").Input);
        // Picked up, not a ghost of itself: the item moves, no drag image does.
        Assert.IsTrue(HasClass(list, "B", "bit-bortable-chosen"));
        Assert.IsFalse(HasClass(list, "B", "bit-bortable-ghost"));
    }

    [TestMethod]
    public async Task ALockedItemSaysItCannotBeMoved()
    {
        var list = RenderList(["A", "Locked"], extra: p => p.Add(c => c.Filter, i => i == "Locked"));

        var result = await PickAsync(list, "Locked");

        Assert.IsFalse(result.Dragging);
        Assert.AreEqual("Locked cannot be moved.", result.Announce);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task ItemsAreNamedByItemLabel()
    {
        var list = RenderList(new List<Card> { new("1", "Write the spec") }, c => c.Id, extra: p => p.Add(c => c.ItemLabel, c => c.Title));

        var result = await PickAsync(list, "1");

        Assert.AreEqual("Picked up Write the spec. It is at position 1 of 1.", result.Announce);
    }

    private sealed record Card(string Id, string Title);

    // ---- moving ----

    [TestMethod]
    public async Task ArrowsMoveThePickedUpItemWithinItsList()
    {
        List<string> items = ["A", "B", "C", "D"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        await PickAsync(list, "B");
        var down = await KeyAsync(list, "to", index: 2);
        Assert.AreSequenceEqual(["A", "C", "B", "D"], items);
        Assert.AreEqual("B moved to position 3 of 4.", down.Announce);
        Assert.AreEqual(KeyOf(list, "B"), down.Key);

        var top = await KeyAsync(list, "to", index: 0);
        Assert.AreSequenceEqual(["B", "A", "C", "D"], items);
        Assert.AreEqual("B moved to position 1 of 4.", top.Announce);

        var dropped = await KeyAsync(list, "drop");
        Assert.IsFalse(dropped.Dragging);
        Assert.AreEqual("Dropped B at position 1 of 4.", dropped.Announce);
        Assert.AreEqual("Start:L1 Change:L1 Change:L1 ItemsChanged:L1 Update:L1 Sort:L1 End:L1", Sequence(log));
        Assert.AreEqual(BortableInput.Keyboard, Single(log, "Update", "L1").Input);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task ARefusedPositionIsSaidAndNothingMoves()
    {
        List<string> items = ["Pinned", "A", "B"];
        var list = RenderList(items, extra: p => p.Add(c => c.CanDrop, e => e.NewIndex > 0));

        await PickAsync(list, "A");
        var result = await KeyAsync(list, "to", index: 0);

        Assert.IsTrue(result.Dragging);
        Assert.AreEqual("A cannot be moved there.", result.Announce);
        Assert.AreSequenceEqual(["Pinned", "A", "B"], items);
    }

    [TestMethod]
    public async Task EscapePutsTheItemBackAndSaysSo()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        await PickAsync(list, "A");
        await KeyAsync(list, "to", index: 2);
        var result = await KeyAsync(list, "cancel");

        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.AreEqual("Cancelled. A is back at position 1 of 3.", result.Announce);
        Assert.IsFalse(result.Dragging);
        Assert.IsTrue(Single(log, "End", "L1").Canceled);
        Assert.IsFalse(log.Any(r => r.Name == "ItemsChanged"));
    }

    [TestMethod]
    public async Task ADropWhereItStartedSaysItIsBack()
    {
        var list = RenderList(["A", "B"]);

        await PickAsync(list, "A");
        var result = await KeyAsync(list, "drop");

        Assert.AreEqual("Dropped A. It is back at position 1 of 2.", result.Announce);
    }

    // ---- into other lists ----

    private (IRenderedComponent<Bortable<string>> L1, IRenderedComponent<Bortable<string>> L2) RenderPair(
        List<string> left, List<string> right, List<Raised<string>>? log = null,
        Action<ComponentParameterCollectionBuilder<Bortable<string>>>? leftExtra = null,
        Action<ComponentParameterCollectionBuilder<Bortable<string>>>? rightExtra = null)
    {
        var l1 = RenderList(left, "L1", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.AriaLabel, "To do");
            if (log is not null) Record(p, "L1", log);
            leftExtra?.Invoke(p);
        });
        var l2 = RenderList(right, "L2", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.AriaLabel, "Done");
            if (log is not null) Record(p, "L2", log);
            rightExtra?.Invoke(p);
        });
        return (l1, l2);
    }

    [TestMethod]
    public async Task AnArrowPastTheListCarriesTheItemIntoTheNextList()
    {
        List<string> left = ["A", "B"], right = ["X", "Y"];
        var log = new List<Raised<string>>();
        var (l1, l2) = RenderPair(left, right, log);

        var picked = await PickAsync(l1, "A");
        CollectionAssert.AreEquivalent(new[] { "L1", "L2" }, picked.Lists);

        var moved = await KeyAsync(l1, "to", "L2", 1);

        // The item itself moves - a keyboard drag has no native drag to keep alive.
        Assert.AreSequenceEqual(["B"], left);
        Assert.AreSequenceEqual(["X", "A", "Y"], right);
        Assert.AreEqual("A moved to position 2 of 3 in Done.", moved.Announce);
        Assert.AreEqual("L2", moved.List);
        Assert.AreEqual(KeyOf(l2, "A"), moved.Key);
        Assert.IsTrue(HasClass(l2, "A", "bit-bortable-chosen"));

        // Further moves happen in the list it is in now.
        await KeyAsync(l2, "to", index: 2);
        Assert.AreSequenceEqual(["X", "Y", "A"], right);

        var dropped = await KeyAsync(l2, "drop");
        Assert.AreEqual("Dropped A at position 3 of 3 in Done.", dropped.Announce);
        Assert.AreEqual("L2", dropped.List);
        Assert.AreEqual(
            "Start:L1 Change:L2 Change:L2 ItemsChanged:L1 ItemsChanged:L2 Remove:L1 Add:L2 Sort:L1 Sort:L2 End:L1",
            Sequence(log));
        var add = Single(log, "Add", "L2");
        Assert.AreEqual(0, add.OldIndex);
        Assert.AreEqual(2, add.NewIndex);
    }

    [TestMethod]
    public async Task CancellingACrossListMoveBringsTheItemHome()
    {
        List<string> left = ["A", "B"], right = ["X"];
        var log = new List<Raised<string>>();
        var (l1, l2) = RenderPair(left, right, log);

        await PickAsync(l1, "B");
        await KeyAsync(l1, "to", "L2", 0);
        var result = await KeyAsync(l2, "cancel");

        Assert.AreSequenceEqual(["A", "B"], left);
        Assert.AreSequenceEqual(["X"], right);
        Assert.AreEqual("Cancelled. B is back at position 2 of 2 in To do.", result.Announce);
        Assert.AreEqual("L1", result.List);
        Assert.IsFalse(log.Any(r => r.Name is "ItemsChanged" or "Add" or "Remove"));
    }

    [TestMethod]
    public async Task AnItemCarriedBackHomeIsJustReordered()
    {
        List<string> left = ["A", "B", "C"], right = ["X"];
        var (l1, l2) = RenderPair(left, right);

        await PickAsync(l1, "A");
        await KeyAsync(l1, "to", "L2", 0);
        await KeyAsync(l2, "to", "L1", 2);
        await KeyAsync(l1, "drop");

        Assert.AreSequenceEqual(["B", "C", "A"], left);
        Assert.AreSequenceEqual(["X"], right);
    }

    [TestMethod]
    public async Task AListThatDoesNotTakeTheItemIsSaidToRefuseIt()
    {
        List<string> left = ["A"], right = ["X"];
        var (l1, _) = RenderPair(left, right, rightExtra: p => p.Add(c => c.Put, false));

        var picked = await PickAsync(l1, "A");
        var result = await KeyAsync(l1, "to", "L2", 0);

        Assert.AreSequenceEqual(["L1"], picked.Lists!.ToList());
        Assert.AreEqual("A cannot be moved there.", result.Announce);
        Assert.AreSequenceEqual(["A"], left);
    }

    [TestMethod]
    public async Task AKeyboardCloneIsACopyThatComesAndGoes()
    {
        List<string> palette = ["Red", "Blue"], target = ["X"];
        var (l1, l2) = RenderPair(palette, target, leftExtra: p =>
        {
            p.Add(c => c.Pull, BortablePull.Clone);
            p.Add(c => c.Clone, s => s + " copy");
        });

        await PickAsync(l1, "Red");
        await KeyAsync(l1, "to", "L2", 1);
        Assert.AreSequenceEqual(["Red", "Blue"], palette);
        Assert.AreSequenceEqual(["X", "Red copy"], target);

        // Back over the palette, the copy goes away again.
        await KeyAsync(l2, "to", "L1", 0);
        Assert.AreSequenceEqual(["X"], target);
        Assert.AreSequenceEqual(["Red", "Blue"], palette);

        await KeyAsync(l1, "to", "L2", 0);
        await KeyAsync(l2, "drop");
        Assert.AreSequenceEqual(["Red copy", "X"], target);
        Assert.AreSequenceEqual(["Red", "Blue"], palette);
    }

    [TestMethod]
    public async Task ATargetThatCannotSortStillLetsTheCarriedItemBePlaced()
    {
        List<string> left = ["A"], right = ["X", "Y"];
        var (l1, l2) = RenderPair(left, right, rightExtra: p => p.Add(c => c.Sort, false));

        await PickAsync(l1, "A");
        await KeyAsync(l1, "to", "L2", 0);
        await KeyAsync(l2, "to", index: 2);

        // Sort="false" keeps a list's own items in order; an item arriving still picks its place, as with a pointer.
        Assert.AreSequenceEqual(["X", "Y", "A"], right);
    }

    [TestMethod]
    public async Task AKeyboardSwapHighlightsThenTrades()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items, extra: p => p.Add(c => c.Swap, true));

        await PickAsync(list, "A");
        var result = await KeyAsync(list, "to", index: 2);

        Assert.AreEqual("A will trade places with C.", result.Announce);
        Assert.AreEqual(KeyOf(list, "C"), result.Target);
        Assert.AreSequenceEqual(["A", "B", "C"], items);

        await KeyAsync(list, "drop");
        Assert.AreSequenceEqual(["C", "B", "A"], items);
    }

    [TestMethod]
    public async Task AMultiDragCarriesTheSelectionByKeyboardToo()
    {
        List<string> left = ["A", "B", "C"], right = ["X"];
        var (l1, _) = RenderPair(left, right, leftExtra: p => p.Add(c => c.MultiDrag, true));

        await ClickAsync(l1, "A");
        await ClickAsync(l1, "C");
        await PickAsync(l1, "A");
        await KeyAsync(l1, "to", "L2", 1);
        await KeyAsync(l1, "drop");

        Assert.AreSequenceEqual(["B"], left);
        Assert.AreSequenceEqual(["X", "A", "C"], right);
    }

    [TestMethod]
    public async Task KeysWithoutAKeyboardDragDoNothing()
    {
        List<string> items = ["A", "B"];
        var list = RenderList(items);

        Assert.IsFalse((await KeyAsync(list, "to", index: 1)).Dragging);
        Assert.IsFalse((await KeyAsync(list, "drop")).Dragging);

        // Nor do they touch a pointer drag.
        await DragStartAsync(list, "A");
        Assert.IsFalse((await KeyAsync(list, "to", index: 1)).Dragging);
        Assert.AreSequenceEqual(["A", "B"], items);
    }

    [TestMethod]
    public async Task ADisabledListOrOneWithoutTheKeyboardPicksNothingUp()
    {
        var off = RenderList(["A"], extra: p => p.Add(c => c.Keyboard, false));
        var disabled = RenderList(["A"], extra: p => p.Add(c => c.Disabled, true));

        Assert.IsFalse((await PickAsync(off, "A")).Dragging);
        Assert.IsFalse((await PickAsync(disabled, "A")).Dragging);
        Assert.IsNull(Coordinator.Context);
    }

    // ---- announcements of the other drags ----

    [TestMethod]
    public async Task APointerDragIsAnnouncedWhenItStartsAndEnds()
    {
        var list = RenderList(["A", "B", "C"]);

        var started = await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        var ended = await Renderer.Dispatcher.InvokeAsync(() => Zone(list).EndAsync("drop"));

        Assert.AreEqual("Picked up A. It is at position 1 of 3.", started!.Announce);
        Assert.AreEqual("Dropped A at position 3 of 3.", ended);
    }

    [TestMethod]
    public async Task AMoveMadeByCodeIsAnnounced()
    {
        var list = RenderList(["A", "B", "C"], extra: p => p.Add(c => c.AriaLabel, "Queue"));

        await list.InvokeAsync(() => list.Instance.MoveAsync("C", 0));

        Assert.AreEqual("C moved to position 1 of 3 in Queue.", Module.Invocations["announce"].Single().Arguments[0]);
    }

    [TestMethod]
    public async Task AnnouncementsCanBeReworded()
    {
        var messages = new BortableMessages { PickedUp = a => $"{a.Item} aufgenommen, {a.Position}/{a.Count}" };
        var list = RenderList(["A", "B"], extra: p => p.Add(c => c.Messages, messages));

        var result = await PickAsync(list, "B");

        Assert.AreEqual("B aufgenommen, 2/2", result.Announce);
    }
}
