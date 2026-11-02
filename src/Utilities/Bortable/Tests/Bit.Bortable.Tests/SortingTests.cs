using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bortable.Tests;

/// <summary>
/// Reordering within one list: the item moves in the bound list live, on every dragover that reaches a new
/// position, and the drop only settles it and raises the completion events. These tests pin the index
/// arithmetic (a drop index is "insert before", measured with the dragged item still in the list), the
/// event order SortableJS users rely on, and every switch that must keep a list still.
/// </summary>
[TestClass]
public class SortingTests : BortableTestContext
{
    [TestMethod]
    public async Task DraggingDownReordersTheListLive()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items);

        await DragStartAsync(list, "A");
        // "Insert before index 3", counted with A still at 0, is the end of the list.
        await DragOverAsync(list, 3);

        Assert.AreSequenceEqual(["B", "C", "A"], items);
        Assert.AreEqual("B,C,A", Labels(list));
    }

    [TestMethod]
    public async Task DraggingUpInsertsBeforeTheMeasuredItem()
    {
        List<string> items = ["A", "B", "C", "D"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "D");
        await DragOverAsync(list, 1);
        await DropAsync(list);

        Assert.AreSequenceEqual(["A", "D", "B", "C"], items);
        var update = Single(log, "Update", "L1");
        Assert.AreEqual(3, update.OldIndex);
        Assert.AreEqual(1, update.NewIndex);
    }

    [TestMethod]
    public async Task DroppingRaisesItemsChangedThenUpdateSortAndEnd()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);

        // ItemsChanged first, so a handler of the completion events already sees the bound field updated.
        Assert.AreEqual("Start:L1 Change:L1 ItemsChanged:L1 Update:L1 Sort:L1 End:L1", Sequence(log));
        foreach (var name in new[] { "Update", "Sort", "End" })
        {
            var e = Single(log, name, "L1");
            Assert.AreEqual("A", e.Item);
            Assert.AreSequenceEqual(["A"], e.Items);
            Assert.AreEqual(0, e.OldIndex);
            Assert.AreEqual(2, e.NewIndex);
            Assert.AreEqual("L1", e.FromId);
            Assert.AreEqual("L1", e.ToId);
            Assert.IsFalse(e.CrossedLists);
        }
    }

    [TestMethod]
    public async Task TheCompletionEventsSeeTheFinalOrder()
    {
        List<string> items = ["A", "B", "C"];
        string? seenByUpdate = null;
        var list = RenderList(items, extra: p => p.Add(c => c.OnUpdate, _ => seenByUpdate = string.Join(",", items)));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 2);
        await DropAsync(list);

        Assert.AreEqual("B,A,C", seenByUpdate);
    }

    [TestMethod]
    public async Task OnStartReportsTheDraggedItemAndWhereItWas()
    {
        var log = new List<Raised<string>>();
        var list = RenderList(["A", "B", "C"], "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "B");

        var start = Single(log, "Start", "L1");
        Assert.AreEqual("B", start.Item);
        Assert.AreEqual(1, start.OldIndex);
        Assert.AreEqual(1, start.NewIndex);
        Assert.AreEqual("L1", start.FromId);
        Assert.AreEqual("L1", start.ToId);
    }

    [TestMethod]
    public async Task OnChangeIsRaisedOnlyWhenTheItemReachesANewPosition()
    {
        var log = new List<Raised<string>>();
        var list = RenderList(["A", "B", "C", "D"], "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 2); // A -> 1
        await DragOverAsync(list, 2); // dragover keeps firing while the pointer rests: same place, no event
        await DragOverAsync(list, 4); // A -> 3

        var changes = log.Where(r => r.Name == "Change").Select(r => r.Args!).ToList();
        Assert.AreSequenceEqual([1, 3], changes.Select(c => c.NewIndex));
        Assert.IsTrue(changes.All(c => c.OldIndex == 0));
    }

    [TestMethod]
    public async Task ADragThatChangesNothingRaisesOnlyStartAndEnd()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "B");
        await DropAsync(list);

        Assert.AreEqual("Start:L1 End:L1", Sequence(log));
        var end = Single(log, "End", "L1");
        Assert.AreEqual(1, end.OldIndex);
        Assert.AreEqual(1, end.NewIndex);
        Assert.AreSequenceEqual(["A", "B", "C"], items);
    }

    [TestMethod]
    public async Task ARoundTripRaisesNoUpdateOrSort()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DragOverAsync(list, 0);
        await DropAsync(list);

        // The list moved twice and ended where it started: as in SortableJS, that is not an update.
        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.IsFalse(log.Any(r => r.Name is "Update" or "Sort"));
        Assert.AreEqual("End", log[^1].Name);
    }

    [TestMethod]
    public async Task ARoundTripDoesNotRaiseItemsChanged()
    {
        var log = new List<Raised<string>>();
        var list = RenderList(["A", "B", "C"], "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DragOverAsync(list, 0);
        await DropAsync(list);

        Assert.IsFalse(log.Any(r => r.Name == "ItemsChanged"));
    }

    [TestMethod]
    public async Task AnEditMadeByTheAppMidDragIsNotReportedAsTheDragsUpdate()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "A");
        // Someone else deletes A while it is being dragged; the page re-renders with the edited list.
        items.Remove("A");
        list.Render(p => p.Add(c => c.Items, items));
        await DropAsync(list);

        Assert.AreEqual("Start:L1 End:L1", Sequence(log));
    }

    [TestMethod]
    public async Task SortFalseKeepsTheOrder()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p =>
        {
            p.Add(c => c.Sort, false);
            Record(p, "L1", log);
        });

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);

        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.AreEqual("A,B,C", Labels(list));
        Assert.AreEqual("Start:L1 End:L1", Sequence(log));
    }

    [TestMethod]
    public async Task ADisabledListIgnoresTheDrag()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p =>
        {
            p.Add(c => c.Disabled, true);
            Record(p, "L1", log);
        });

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);

        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.IsEmpty(log);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task AFilteredItemCannotBeDragged()
    {
        List<string> items = ["A", "Locked", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p =>
        {
            p.Add(c => c.Filter, i => i == "Locked");
            Record(p, "L1", log);
        });

        await DragStartAsync(list, "Locked");
        await DragOverAsync(list, 3);

        Assert.AreSequenceEqual(["A", "Locked", "C"], items);
        Assert.IsEmpty(log);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task OtherItemsCanMovePastAFilteredOne()
    {
        List<string> items = ["A", "Locked", "C"];
        var list = RenderList(items, extra: p => p.Add(c => c.Filter, i => i == "Locked"));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);

        // The filter only stops the item itself from being picked up, as in SortableJS.
        Assert.AreSequenceEqual(["Locked", "C", "A"], items);
    }

    [TestMethod]
    public async Task AStartForAnItemTheListNoLongerHoldsIsRefused()
    {
        List<string> items = ["A", "B"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));
        string key = KeyOf(list, "A");

        // The browser names the item it saw rendered; on Blazor Server the app may have removed it since.
        items.Remove("A");
        list.Render(p => p.Add(c => c.Items, items));
        var started = await StartByKeyAsync(list, key);
        await DragOverAsync(list, 1);
        await DropAsync(list);

        Assert.IsNull(started);
        Assert.AreSequenceEqual(["B"], items);
        Assert.IsEmpty(log);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task AStartForAnUnknownKeyIsRefused()
    {
        var list = RenderList(["A", "B"]);

        Assert.IsNull(await StartByKeyAsync(list, "not-a-key"));
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task EachItemKeepsItsKeyAcrossRendersAndMoves()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items);
        var before = items.ToDictionary(i => i, i => KeyOf(list, i));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);
        list.Render(p => p.Add(c => c.Class, "re-rendered"));

        foreach (var item in items) Assert.AreEqual(before[item], KeyOf(list, item));
        Assert.HasCount(3, before.Values.Distinct());
    }

    [TestMethod]
    public async Task TheStartNamesTheListsThatTakeTheDrag()
    {
        var list = RenderList(["A", "B"], "L1");

        var started = await DragStartAsync(list, "A");

        Assert.IsNotNull(started);
        Assert.AreSequenceEqual(["L1"], started.Lists.ToList());
    }

    [TestMethod]
    public async Task TheDeadZoneKeepsTheCurrentOrder()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items);

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 2);
        // -1 is the browser layer's "between swap zones": nothing should move, not even back.
        await DragOverAsync(list, -1);

        Assert.AreSequenceEqual(["B", "A", "C"], items);
    }

    [TestMethod]
    public async Task ADropIndexPastTheEndIsClamped()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items);

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 42);

        Assert.AreSequenceEqual(["B", "C", "A"], items);
    }

    [TestMethod]
    public async Task ReleasingWithoutADropStillFinishesTheDrag()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        // Released outside any list: only dragend fires, on the dragged item.
        await DragEndAsync(list, "A");

        Assert.AreEqual("Start:L1 Change:L1 ItemsChanged:L1 Update:L1 Sort:L1 End:L1", Sequence(log));
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task DropFollowedByDragEndFinishesTheDragOnce()
    {
        var log = new List<Raised<string>>();
        var list = RenderList(["A", "B", "C"], "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        // The browser fires both on a successful drop; whichever comes first wins.
        await DropAsync(list);
        await DragEndAsync(list, "A");

        Assert.ContainsSingle(log.Where(r => r.Name == "End"));
        Assert.ContainsSingle(log.Where(r => r.Name == "ItemsChanged"));
    }

    [TestMethod]
    public async Task TheDraggedItemIsMarkedOnlyWhileItIsDragged()
    {
        var list = RenderList(["A", "B", "C"]);

        await DragStartAsync(list, "B");
        Assert.AreEqual("B", LabelsWithClass(list, "bit-bortable-ghost"));
        Assert.AreEqual("B", LabelsWithClass(list, "bit-bortable-chosen"));

        await DragOverAsync(list, 0);
        // The marking follows the item to its new place.
        Assert.AreEqual("B,A,C", Labels(list));
        Assert.AreEqual("B", LabelsWithClass(list, "bit-bortable-ghost"));

        await DropAsync(list);
        Assert.AreEqual("", LabelsWithClass(list, "bit-bortable-ghost"));
        Assert.AreEqual("", LabelsWithClass(list, "bit-bortable-chosen"));
    }

    [TestMethod]
    public void TheHitTestOptionsAreSentToTheBrowserLayer()
    {
        var list = RenderList(["A", "B"], "L1", p =>
        {
            p.Add(c => c.SwapThreshold, 0.65);
            p.Add(c => c.InvertSwap, true);
            p.Add(c => c.InvertedSwapThreshold, 0.3);
            p.Add(c => c.Direction, BortableDirection.Horizontal);
            p.Add(c => c.Animation, 200);
        });

        // The browser layer measures the drop position itself, with these: see dropIndex in bit-bortable.ts.
        var sent = SentOptions(list)!;
        Assert.AreEqual("L1", sent.Id);
        Assert.AreEqual(0.65, sent.SwapThreshold);
        Assert.IsTrue(sent.InvertSwap);
        Assert.AreEqual(0.3, sent.InvertedSwapThreshold);
        Assert.AreEqual("horizontal", sent.Direction);
        Assert.AreEqual(200, sent.Animation);
        Assert.IsFalse(sent.Swap);
    }

    [TestMethod]
    public void EachDirectionIsSentAsTheNameTheBrowserLayerExpects()
    {
        var list = RenderList(["A", "B"]);
        var sent = new List<string>();

        foreach (var direction in new[] { BortableDirection.Auto, BortableDirection.Vertical, BortableDirection.Horizontal })
        {
            list.Render(p => p.Add(c => c.Direction, direction));
            sent.Add(SentOptions(list)!.Direction);
        }

        Assert.AreSequenceEqual(["auto", "vertical", "horizontal"], sent);
    }

    [TestMethod]
    public void TheListRegistersOnceAndIsReconfiguredOnlyWhenAnOptionChanges()
    {
        var list = RenderList(["A"]);

        list.Render(p => p.Add(c => c.Class, "unrelated"));
        list.Render(p => p.Add(c => c.Animation, 300));
        list.Render(p => p.Add(c => c.Style, "color: red"));

        Assert.ContainsSingle(Module.Invocations["register"]);
        Assert.ContainsSingle(Module.Invocations["configure"]);
        Assert.AreEqual(300, SentOptions(list)!.Animation);
    }

    [TestMethod]
    public async Task OverAnswersWhetherAListChangedWhichIsWhenTheBrowserAnimates()
    {
        var list = RenderList(["A", "B", "C"]);

        await DragStartAsync(list, "A");

        Assert.IsTrue(await DragOverAsync(list, 3));
        // The same position again (the pointer rests): nothing to animate.
        Assert.IsFalse(await DragOverAsync(list, 3));
        Assert.IsFalse(await DragOverAsync(list, -1));
        // The browser layer snapshots and plays the slides around these calls itself.
        Assert.IsEmpty(Module.Invocations["capture"]);
        Assert.IsEmpty(Module.Invocations["play"]);
    }

    [TestMethod]
    public async Task WithoutAnimationTheListStillReorders()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items, extra: p => p.Add(c => c.Animation, 0));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);

        Assert.AreSequenceEqual(["B", "C", "A"], items);
        Assert.AreEqual(0, SentOptions(list)!.Animation);
    }
}
