using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bortable.Tests;

/// <summary>
/// The drag as a piece of shared state. One scoped coordinator holds the drag for every list, so a drag that
/// is never finished - its list removed mid-drag, its dragend lost, the circuit gone - must not leave state
/// behind that breaks the next drag or throws from a later event. These tests take the lists away, skip
/// events and fail the browser layer at awkward moments, and check that the coordinator always recovers.
/// </summary>
[TestClass]
public class LifecycleTests : BortableTestContext
{
    [TestMethod]
    public async Task TheCoordinatorHoldsTheDragOnlyWhileItIsInProgress()
    {
        var list = RenderList(["A", "B", "C"]);
        Assert.IsNull(Coordinator.Context);

        await DragStartAsync(list, "B");
        var ctx = Coordinator.Context;
        Assert.IsNotNull(ctx);
        Assert.AreEqual("B", ctx.Item);
        Assert.AreEqual(1, ctx.OldIndex);
        Assert.AreSame(list.Instance, ctx.Source);

        await DragOverAsync(list, 0);
        await DropAsync(list);

        Assert.IsNull(Coordinator.Context);
        Assert.IsTrue(ctx.Finished);
    }

    [TestMethod]
    public async Task TheCoordinatorIsClearedAfterACrossListDrop()
    {
        var source = RenderList(["A"], "L1", p => p.Add(c => c.Group, "g"));
        var target = RenderList(["X"], "L2", p => p.Add(c => c.Group, "g"));

        await DragStartAsync(source, "A");
        await DragOverAsync(target, 0);
        await DropAsync(target);

        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task EventsWithoutADragAreIgnored()
    {
        List<string> items = ["A", "B"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        // A drag that started outside any list (a file, text from another page) still fires these.
        await DragOverAsync(list, 0);
        await DropAsync(list);
        await DragEndAsync(list, "A");

        Assert.AreSequenceEqual(["A", "B"], items);
        Assert.IsEmpty(log);
    }

    [TestMethod]
    public async Task ADragWhoseEndWasNeverSeenIsDiscardedByTheNextOne()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        // No drop or dragend: the browser lost it (e.g. the element was re-created mid-drag).
        await DragStartAsync(list, "B");

        Assert.AreEqual("B", Coordinator.Context?.Item);
        Assert.AreEqual("B", LabelsWithClass(list, "bit-bortable-ghost"));

        await DragOverAsync(list, 3);
        await DropAsync(list);

        // The first drag's live reorder (B, C, A) stays, and the second one is applied on top of it.
        Assert.AreSequenceEqual(["C", "A", "B"], items);
        Assert.IsNull(Coordinator.Context);
        Assert.AreEqual("B", log[^1].Args!.Item);
        // The first drag is reported as it ends, before the second one starts: OnEnd follows every OnStart.
        Assert.AreEqual(
            "Start:L1 Change:L1 ItemsChanged:L1 Update:L1 Sort:L1 End:L1 Start:L1 Change:L1 ItemsChanged:L1 Update:L1 Sort:L1 End:L1",
            Sequence(log));
        Assert.AreEqual("A", log[5].Args!.Item);
    }

    [TestMethod]
    public async Task ADragEndedByTheNextOneIsConfirmedBeforeTheNextOneStarts()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        BortableEventArgs<string>? asked = null;
        var answer = new TaskCompletionSource<bool>();
        var list = RenderList(items, "L1", p =>
        {
            Record(p, "L1", log);
            p.Add(c => c.ConfirmDrop, e => { asked = e; return answer.Task; });
        });

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        var next = DragStartAsync(list, "B");

        Assert.IsFalse(next.IsCompleted);
        Assert.IsNotNull(asked);
        Assert.AreEqual("A", asked.Item);
        Assert.IsNull(Coordinator.Context);
        Assert.IsNull(await DragStartAsync(list, "C"));

        answer.SetResult(false);
        Assert.IsNotNull(await next);

        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.AreEqual("Start:L1 Change:L1 ItemsChanged:L1 End:L1 Start:L1", Sequence(log));
        Assert.IsTrue(log[3].Args!.Canceled);
        Assert.AreEqual("B", Coordinator.Context?.Item);
        Assert.AreEqual(1, Coordinator.Context?.OldIndex);
    }

    [TestMethod]
    public async Task ASlowItemsChangedHandlerDoesNotHoldUpTheNextDrag()
    {
        List<string> items = ["A", "B", "C"];
        var saving = new TaskCompletionSource();
        var list = RenderList(items, "L1", p => p.Add(c => c.ItemsChanged, async (IList<string> _) => await saving.Task));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        var drop = DropAsync(list);
        Assert.IsFalse(drop.IsCompleted, "The app is still saving the first drag.");

        // A drag started meanwhile must not wait for the save: its dragover, drop and dragend would all be lost.
        var next = DragStartAsync(list, "B");
        Assert.IsTrue(next.IsCompleted);
        Assert.AreEqual("B", Coordinator.Context?.Item);
        await DragOverAsync(list, 3);

        saving.SetResult();
        await drop;
        await DropAsync(list);

        Assert.AreSequenceEqual(["C", "A", "B"], items);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task RemovingTheSourceListMidDragAbandonsTheDrag()
    {
        List<string> right = ["X", "Y"];
        var host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l => l
            .Add(c => c.Id, "L1")
            .Add(c => c.Group, "g")
            .Add(c => c.Items, new List<string> { "A", "B" })
            .Add(c => c.ItemTemplate, i => $"<span>{i}</span>")));
        var l1 = host.FindComponent<Bortable<string>>();
        var l2 = RenderList(right, "L2", p => p.Add(c => c.Group, "g"));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        Assert.IsNotNull(Placeholder(l2));

        host.Render(p => p.Add(t => t.Show, false));

        Assert.IsNull(Coordinator.Context);
        Assert.IsNull(Placeholder(l2));

        // The drop of the abandoned drag arrives late and must change nothing.
        await DropAsync(l2);
        Assert.AreSequenceEqual(["X", "Y"], right);

        // ...and the next drag works as usual.
        await DragStartAsync(l2, "X");
        await DragOverAsync(l2, 2);
        await DropAsync(l2);
        Assert.AreSequenceEqual(["Y", "X"], right);
    }

    [TestMethod]
    public async Task RemovingTheSourceListMidDragReportsWhatTheDragAlreadyChanged()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l =>
        {
            l.Add(c => c.Id, "L1");
            l.Add(c => c.Items, items);
            l.Add(c => c.ItemTemplate, i => $"<span>{i}</span>");
            Record(l, "L1", log);
        }));
        var l1 = host.FindComponent<Bortable<string>>();

        await DragStartAsync(l1, "A");
        await DragOverAsync(l1, 3);

        // The list closes (a tab, an @if panel) with the item already moved in the bound list.
        host.Render(p => p.Add(t => t.Show, false));

        Assert.AreSequenceEqual(["B", "C", "A"], items);
        Assert.AreEqual("Start:L1 Change:L1 ItemsChanged:L1 Update:L1 Sort:L1 End:L1", Sequence(log));
        var end = Single(log, "End", "L1");
        Assert.AreEqual(0, end.OldIndex);
        Assert.AreEqual(2, end.NewIndex);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task RemovingTheSourceListMidDragPutsBackAReorderItsConfirmDropCouldNotBeAskedAbout()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        bool asked = false;
        var host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l =>
        {
            l.Add(c => c.Id, "L1");
            l.Add(c => c.Items, items);
            l.Add(c => c.ItemTemplate, i => $"<span>{i}</span>");
            l.Add(c => c.ConfirmDrop, _ => { asked = true; return Task.FromResult(true); });
            Record(l, "L1", log);
        }));
        var l1 = host.FindComponent<Bortable<string>>();

        await DragStartAsync(l1, "A");
        await DragOverAsync(l1, 3);
        host.Render(p => p.Add(t => t.Show, false));

        Assert.IsFalse(asked);
        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.AreEqual("Start:L1 Change:L1 ItemsChanged:L1 End:L1", Sequence(log));
        Assert.IsTrue(Single(log, "End", "L1").Canceled);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task RemovingTheSourceListOfAKeyboardDragPutsBackWhatItCarriedIntoAListThatConfirmsDrops()
    {
        List<string> source = ["a", "b"], words = ["x"];
        var log = new List<Raised<string>>();
        bool asked = false;
        var host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l =>
        {
            l.Add(c => c.Id, "S");
            l.Add(c => c.Group, "g");
            l.Add(c => c.Items, source);
            l.Add(c => c.ItemTemplate, i => $"<span>{i}</span>");
            Record(l, "S", log);
        }));
        var s = host.FindComponent<Bortable<string>>();
        RenderList(words, "W", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.ConfirmDrop, _ => { asked = true; return Task.FromResult(true); });
            Record(p, "W", log);
        });

        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync(KeyOf(s, "a"), "pick", null, -1));
        await Renderer.Dispatcher.InvokeAsync(() => Zone(s).KeyAsync("", "to", "W", 0));
        Assert.AreSequenceEqual(["a", "x"], words);

        // The list the drag started in closes: the move into W was never dropped, so W cannot have confirmed it.
        host.Render(p => p.Add(t => t.Show, false));

        Assert.IsFalse(asked);
        Assert.AreSequenceEqual(["x"], words);
        Assert.AreSequenceEqual(["a", "b"], source);
        Assert.IsTrue(Single(log, "End", "S").Canceled);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task ARemovedListLeavesTheBrowserLayerEvenWhenAHandlerOfItsDragThrows()
    {
        List<string> items = ["A", "B", "C"];
        var host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l =>
        {
            l.Add(c => c.Id, "L1");
            l.Add(c => c.Items, items);
            l.Add(c => c.ItemTemplate, i => $"<span>{i}</span>");
            l.Add(c => c.ConfirmDrop, _ => Task.FromResult(true));
            l.Add(c => c.OnEnd, (BortableEventArgs<string> _) => throw new InvalidOperationException("boom"));
        }));
        var l1 = host.FindComponent<Bortable<string>>();
        var bridge = (DotNetObjectReference<BortableBridge>)Module.Invocations["register"].Single().Arguments[1]!;

        await DragStartAsync(l1, "A");
        await DragOverAsync(l1, 3);

        // The renderer reports what the handler threw as the list's disposal failing.
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => host.Render(p => p.Add(t => t.Show, false)));

        Assert.AreEqual("boom", error.Message);
        Assert.AreSequenceEqual(["A", "B", "C"], items);
        Assert.ContainsSingle(Module.Invocations["unregister"]);
        Assert.ThrowsExactly<ObjectDisposedException>(() => bridge.Value);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task RemovingTheSourceListClearsASwapHighlightInAnotherList()
    {
        var host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l => l
            .Add(c => c.Group, "g")
            .Add(c => c.Swap, true)
            .Add(c => c.Items, new List<string> { "A" })
            .Add(c => c.ItemTemplate, i => $"<span>{i}</span>")));
        var l1 = host.FindComponent<Bortable<string>>();
        var l2 = RenderList(["X"], "L2", p =>
        {
            p.Add(c => c.Group, "g");
            p.Add(c => c.Swap, true);
        });

        await DragStartAsync(l1, "A");
        await HoverItemAsync(l2, 0);
        Assert.AreEqual("X", LabelsWithClass(l2, "bit-bortable-swap-highlight"));

        host.Render(p => p.Add(t => t.Show, false));

        Assert.AreEqual("", LabelsWithClass(l2, "bit-bortable-swap-highlight"));
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task RemovingTheTargetListMidDragKeepsTheDragAlive()
    {
        List<string> left = ["A", "B"];
        var log = new List<Raised<string>>();
        var l1 = RenderList(left, "L1", p =>
        {
            p.Add(c => c.Group, "g");
            Record(p, "L1", log);
        });
        var host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l => l
            .Add(c => c.Group, "g")
            .Add(c => c.Items, new List<string> { "X" })
            .Add(c => c.ItemTemplate, i => $"<span>{i}</span>")));
        var l2 = host.FindComponent<Bortable<string>>();

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        host.Render(p => p.Add(t => t.Show, false));

        // The drag is still the user's: it just has nowhere to go but home.
        Assert.IsNotNull(Coordinator.Context);
        Assert.IsNull(Coordinator.Context.PlaceholderZone);

        await DragEndAsync(l1, "A");

        Assert.AreSequenceEqual(["A", "B"], left);
        Assert.AreEqual("Start:L1 End:L1", Sequence(log));
        Assert.IsFalse(HasClass(l1, "A", "bit-bortable-hidden"));
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task RemovingTheTargetListMidDragShowsTheOriginalAgain()
    {
        var l1 = RenderList(["A", "B"], "L1", p => p.Add(c => c.Group, "g"));
        var host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l => l
            .Add(c => c.Group, "g")
            .Add(c => c.Items, new List<string> { "X" })
            .Add(c => c.ItemTemplate, i => $"<span>{i}</span>")));
        var l2 = host.FindComponent<Bortable<string>>();

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        Assert.IsTrue(HasClass(l1, "A", "bit-bortable-hidden"));

        host.Render(p => p.Add(t => t.Show, false));

        Assert.IsFalse(HasClass(l1, "A", "bit-bortable-hidden"));
    }

    [TestMethod]
    public async Task AListThatLeftThePageIgnoresALateDropPosition()
    {
        var l1 = RenderList(["A", "B"], "L1", p => p.Add(c => c.Group, "g"));
        var host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l => l
            .Add(c => c.Id, "L2")
            .Add(c => c.Group, "g")
            .Add(c => c.Items, new List<string> { "X" })
            .Add(c => c.ItemTemplate, i => $"<span>{i}</span>")));
        var l2 = host.FindComponent<Bortable<string>>();

        await DragStartAsync(l1, "A");
        var bridgeOfL2 = Zone(l2);

        // L2 leaves the page while the browser's call naming it is still on its way (a round trip under Blazor Server).
        host.Render(p => p.Add(t => t.Show, false));
        Assert.AreEqual(-1, await Renderer.Dispatcher.InvokeAsync(() => bridgeOfL2.OverAsync(0)));
        await DropAsync(l1);

        // Had the disposed L2 become the drop target, the drop would have moved A into a list nobody sees.
        Assert.AreEqual("A,B", Labels(l1));
        Assert.IsFalse(HasClass(l1, "A", "bit-bortable-hidden"));
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task DisposingEveryListMidDragDoesNotThrow()
    {
        var list = RenderList(["A", "B"]);

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 2);
        await DisposeComponentsAsync();

        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task AnAbandonedDragDropsItsPendingPreviewAndKeepsWhatItAlreadyDid()
    {
        List<string> left = ["A", "B", "C"], right = ["X"];
        var log = new List<Raised<string>>();
        var l1 = RenderList(left, "L1", p =>
        {
            p.Add(c => c.Group, "g");
            Record(p, "L1", log);
        });
        var l2 = RenderList(right, "L2", p => p.Add(c => c.Group, "g"));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l1, 2);
        await DragOverAsync(l2, 1);
        // The browser lost the end of the drag (its element was removed, and no dragend ever came).
        await EndAsync(l1, "abandon");

        Assert.AreSequenceEqual(["B", "A", "C"], left);
        Assert.AreSequenceEqual(["X"], right);
        Assert.IsNull(Placeholder(l2));
        Assert.IsFalse(HasClass(l1, "A", "bit-bortable-hidden"));
        Assert.AreEqual("Start:L1 Change:L1 ItemsChanged:L1 Update:L1 Sort:L1 End:L1", Sequence(log));
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task TheEndCanBeReportedThroughAnyListOfThePage()
    {
        List<string> left = ["A"], right = ["X"];
        var l1 = RenderList(left, "L1", p => p.Add(c => c.Group, "g"));
        var l2 = RenderList(right, "L2", p => p.Add(c => c.Group, "g"));

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 0);
        // The browser layer falls back to another list's bridge when the source list's is gone.
        await DropAsync(l2);

        Assert.IsEmpty(left);
        Assert.AreSequenceEqual(["A", "X"], right);
    }

    [TestMethod]
    public void UntilTheModuleIsImportedTheListIsNotRegistered()
    {
        // Prerendered, or interactive but still waiting for the module: the import has not completed, so the
        // browser layer does not know the list and routes no drag to it.
        using var pending = new BunitContext();
        pending.JSInterop.Mode = JSRuntimeMode.Loose;
        pending.Services.AddBitBortableServices();
        pending.JSInterop.AddInvocationHandler(new PendingImport());
        pending.Render<Bortable<string>>(p => p
            .Add(c => c.Items, new List<string> { "A", "B" })
            .Add(c => c.ItemTemplate, i => $"<span>{i}</span>"));

        Assert.ContainsSingle(pending.JSInterop.Invocations["import"]);
        Assert.IsEmpty(pending.JSInterop.Invocations.Where(i => i.Identifier == "register"));
    }

    /// <summary>Answers the module import with a task that never completes.</summary>
    private sealed class PendingImport() : JSRuntimeInvocationHandlerBase<IJSObjectReference>(i => i.Identifier == "import", false);

    [TestMethod]
    public async Task EachTestContextHasItsOwnCoordinator()
    {
        // Scoped: on Blazor Server every circuit is a scope, so two users must never share a drag.
        using var other = new BunitContext();
        other.Services.AddBitBortableServices();
        var list = RenderList(["A"]);

        await DragStartAsync(list, "A");

        Assert.IsNotNull(Coordinator.Context);
        Assert.IsNull(other.Services.GetRequiredService<BortableService>().Context);
    }
}
