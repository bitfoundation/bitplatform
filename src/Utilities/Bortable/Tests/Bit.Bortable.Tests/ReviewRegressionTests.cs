using Bunit;

namespace Bit.Bortable.Tests;

/// <summary>
/// Defects found by the round-10 review, each pinned by the scenario that showed it: a late call about a drag that has
/// ended, a swap highlight carried out of a swap list by the keyboard, a keyboard clone brought home, and an
/// <c>OnStart</c> handler that throws.
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
}
