using Bunit;

namespace Bit.Bortable.Tests;

/// <summary>
/// Which lists take a drag beyond the single <c>Group</c>: <c>PutGroups</c>, by which a list takes the items of other
/// groups (a trash list for several boards), and <c>MinItems</c>, by which a list keeps a number of items of its own.
/// </summary>
[TestClass]
public class GroupRulesTests : BortableTestContext
{
    private IRenderedComponent<Bortable<string>> RenderIn(string id, List<string> items, string? group,
        Action<ComponentParameterCollectionBuilder<Bortable<string>>>? extra = null, List<Raised<string>>? log = null)
        => RenderList(items, id, p =>
        {
            if (group is not null) p.Add(c => c.Group, group);
            if (log is not null) Record(p, id, log);
            extra?.Invoke(p);
        });

    // ---- PutGroups ----

    [TestMethod]
    public async Task AListTakesTheItemsOfTheGroupsItNames()
    {
        List<string> tasks = ["T1"], bugs = ["B1"], trash = [];
        var t = RenderIn("Tasks", tasks, "tasks");
        var b = RenderIn("Bugs", bugs, "bugs");
        var bin = RenderIn("Trash", trash, "trash", p => p.Add(c => c.PutGroups, ["tasks", "bugs"]));

        var fromTasks = await DragStartAsync(t, "T1");
        Assert.Contains("Trash", fromTasks!.Lists);
        Assert.DoesNotContain("Bugs", fromTasks.Lists);
        await DragOverAsync(bin, 0);
        await DropAsync(bin);

        var fromBugs = await DragStartAsync(b, "B1");
        Assert.Contains("Trash", fromBugs!.Lists);
        await DragOverAsync(bin, 1);
        await DropAsync(bin);

        Assert.AreSequenceEqual(["T1", "B1"], trash);
        Assert.IsEmpty(tasks);
        Assert.IsEmpty(bugs);
    }

    [TestMethod]
    public async Task PutGroupsLetsItemsInOnly()
    {
        // The trash takes the tasks, but its items go back only to the lists of its own group.
        List<string> tasks = ["T1"], trash = ["Old"];
        RenderIn("Tasks", tasks, "tasks");
        var bin = RenderIn("Trash", trash, "trash", p => p.Add(c => c.PutGroups, ["tasks"]));

        var started = await DragStartAsync(bin, "Old");

        Assert.AreSequenceEqual(["Trash"], started!.Lists);
    }

    [TestMethod]
    public async Task AListWithoutAGroupOfItsOwnTakesTheGroupsItNames()
    {
        List<string> tasks = ["T1"], bin = [];
        RenderIn("Tasks", tasks, "tasks");
        var t = RenderIn("Tasks2", ["T2"], "tasks");
        RenderIn("Bin", bin, null, p => p.Add(c => c.PutGroups, ["tasks"]));

        var started = await DragStartAsync(t, "T2");

        Assert.Contains("Bin", started!.Lists);
    }

    [TestMethod]
    public async Task PutGroupsStillObeysPutAndMaxItems()
    {
        var t = RenderIn("Tasks", ["T1"], "tasks");
        RenderIn("Closed", [], "trash", p =>
        {
            p.Add(c => c.PutGroups, ["tasks"]);
            p.Add(c => c.Put, false);
        });
        RenderIn("Full", ["X"], "trash", p =>
        {
            p.Add(c => c.PutGroups, ["tasks"]);
            p.Add(c => c.MaxItems, 1);
        });

        var started = await DragStartAsync(t, "T1");

        Assert.DoesNotContain("Closed", started!.Lists);
        Assert.DoesNotContain("Full", started.Lists);
    }

    [TestMethod]
    public async Task AnItemFromAnUngroupedListGoesNowhere()
    {
        var lone = RenderIn("Lone", ["A"], null);
        RenderIn("Bin", [], null, p => p.Add(c => c.PutGroups, ["tasks"]));

        var started = await DragStartAsync(lone, "A");

        Assert.AreSequenceEqual(["Lone"], started!.Lists);
    }

    [TestMethod]
    public async Task MoveToAsyncFollowsPutGroups()
    {
        List<string> tasks = ["T1"], trash = [];
        var t = RenderIn("Tasks", tasks, "tasks");
        RenderIn("Trash", trash, "trash", p => p.Add(c => c.PutGroups, ["tasks"]));
        RenderIn("Other", [], "other");

        Assert.IsFalse(await t.InvokeAsync(() => t.Instance.MoveToAsync("T1", "Other", 0)));
        Assert.IsTrue(await t.InvokeAsync(() => t.Instance.MoveToAsync("T1", "Trash", 0)));
        Assert.AreSequenceEqual(["T1"], trash);
    }

    // ---- MinItems ----

    [TestMethod]
    public async Task AListAtItsMinimumGivesNoItemAway()
    {
        List<string> left = ["A"], right = [];
        var l1 = RenderIn("L1", left, "g", p => p.Add(c => c.MinItems, 1));
        var l2 = RenderIn("L2", right, "g");

        var started = await DragStartAsync(l1, "A");

        Assert.DoesNotContain("L2", started!.Lists);
        Assert.IsFalse(await DragOverAsync(l2, 0));
        await DropAsync(l1);
        Assert.AreSequenceEqual(["A"], left);
    }

    [TestMethod]
    public async Task AListAboveItsMinimumGivesItemsAwayDownToIt()
    {
        List<string> left = ["A", "B"], right = [];
        var l1 = RenderIn("L1", left, "g", p => p.Add(c => c.MinItems, 1));
        var l2 = RenderIn("L2", right, "g");

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        await DropAsync(l2);
        var second = await DragStartAsync(l1, "B");

        Assert.AreSequenceEqual(["A"], right);
        Assert.DoesNotContain("L2", second!.Lists);
    }

    [TestMethod]
    public async Task AListAtItsMinimumStillSortsAndClones()
    {
        List<string> items = ["A", "B"], copies = [];
        var list = RenderIn("L1", items, "g", p =>
        {
            p.Add(c => c.MinItems, 2);
            p.Add(c => c.Pull, BortablePull.Clone);
        });
        var target = RenderIn("L2", copies, "g");

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 2);
        await DropAsync(list);
        Assert.AreSequenceEqual(["B", "A"], items);

        var cloning = await DragStartAsync(list, "A");
        Assert.Contains("L2", cloning!.Lists);
        await DragOverAsync(target, 0);
        await DropAsync(target);
        Assert.AreSequenceEqual(["A"], copies);
        Assert.AreSequenceEqual(["B", "A"], items);
    }

    [TestMethod]
    public async Task AListAtItsMinimumStillSwaps()
    {
        List<string> left = ["A"], right = ["X"];
        var l1 = RenderIn("L1", left, "g", p => p.Add(c => c.MinItems, 1));
        var l2 = RenderIn("L2", right, "g", p => p.Add(c => c.Swap, true));

        var started = await DragStartAsync(l1, "A");
        Assert.Contains("L2", started!.Lists);
        await HoverItemAsync(l2, 0);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["X"], left);
        Assert.AreSequenceEqual(["A"], right);
    }

    [TestMethod]
    public async Task AMultiDragCountsEveryItemItWouldTakeAway()
    {
        List<string> left = ["A", "B", "C"], right = [];
        var l1 = RenderIn("L1", left, "g", p =>
        {
            p.Add(c => c.MinItems, 1);
            p.Add(c => c.MultiDrag, true);
        });
        RenderIn("L2", right, "g");

        await ClickAsync(l1, "A");
        await ClickAsync(l1, "B");
        await ClickAsync(l1, "C");
        var all = await DragStartAsync(l1, "A");
        await EndAsync(l1, "cancel");
        Assert.DoesNotContain("L2", all!.Lists);

        await ClickAsync(l1, "C");
        var two = await DragStartAsync(l1, "A");
        Assert.Contains("L2", two!.Lists);
    }

    [TestMethod]
    public async Task MinItemsIsAskedAgainAtTheDrop()
    {
        // The preview was allowed with two items in L1; the page took one out before the drop.
        List<string> left = ["A", "B"], right = [];
        var log = new List<Raised<string>>();
        var l1 = RenderIn("L1", left, "g", p => p.Add(c => c.MinItems, 1), log);
        var l2 = RenderIn("L2", right, "g");

        await DragStartAsync(l1, "A");
        Assert.IsTrue(await DragOverAsync(l2, 0));
        left.Remove("B");
        await DropAsync(l2);

        Assert.AreSequenceEqual(["A"], left);
        Assert.IsEmpty(right);
        Assert.IsTrue(log.Single(r => r.Name == "End").Args!.Canceled);
    }

    [TestMethod]
    public async Task MinItemsIsAskedAgainAtTheDropOfAMultiDrag()
    {
        List<string> left = ["A", "B", "C"], right = [];
        var l1 = RenderIn("L1", left, "g", p =>
        {
            p.Add(c => c.MinItems, 1);
            p.Add(c => c.MultiDrag, true);
        });
        var l2 = RenderIn("L2", right, "g");

        await ClickAsync(l1, "A");
        await ClickAsync(l1, "B");
        await DragStartAsync(l1, "A");
        Assert.IsTrue(await DragOverAsync(l2, 0));
        left.Remove("C");
        await DropAsync(l2);

        Assert.AreSequenceEqual(["A", "B"], left);
        Assert.IsEmpty(right);
    }

    [TestMethod]
    public async Task MoveToAsyncFollowsMinItems()
    {
        List<string> left = ["A"], right = [];
        var l1 = RenderIn("L1", left, "g", p => p.Add(c => c.MinItems, 1));
        RenderIn("L2", right, "g");

        Assert.IsFalse(await l1.InvokeAsync(() => l1.Instance.MoveToAsync("A", "L2", 0)));
        Assert.AreSequenceEqual(["A"], left);
    }

    [TestMethod]
    public async Task AKeyboardDragCarriedOutCountsTheItemAsGone()
    {
        // Carried out of L1 into L2, the item may go on to L3: L1 already let it go, and has its minimum still.
        List<string> l1Items = ["A", "B"], l2Items = [], l3Items = [];
        var l1 = RenderIn("L1", l1Items, "g", p => p.Add(c => c.MinItems, 1));
        RenderIn("L2", l2Items, "g");
        RenderIn("L3", l3Items, "g");

        var picked = await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync(KeyOf(l1, "A"), "pick", null, -1));
        var intoL2 = await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync("", "to", "L2", 0, picked.Drag));
        var intoL3 = await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync("", "to", "L3", 0, picked.Drag));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).KeyAsync("", "drop", null, -1, picked.Drag));

        Assert.Contains("L3", intoL2.Lists!);
        Assert.AreEqual("L3", intoL3.List);
        Assert.AreSequenceEqual(["B"], l1Items);
        Assert.AreSequenceEqual(["A"], l3Items);
    }
}
