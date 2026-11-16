﻿using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bortable.Tests;

/// <summary>
/// The app's own handlers throwing during a drag the user makes. The browser layer's call that ran them is answered all
/// the same - what to announce, where the item is - every other handler still hears of the drag, and what the handler
/// threw is passed on as the list's own, for the nearest <c>ErrorBoundary</c>, rather than going back to the browser
/// layer, where nothing would report it. And an import of the browser layer that failed is tried again.
/// </summary>
[TestClass]
public class HandlerFailureTests : BortableTestContext
{
    [TestMethod]
    public async Task AnOnEndThatThrowsOnAPointerDropIsPassedOnAndTheDropIsStillAnnounced()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log, throwing: "End"));

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        var announced = await Renderer.Dispatcher.InvokeAsync(() => Zone(list).EndAsync("drop", 0));

        Assert.AreEqual("Dropped A at position 3 of 3.", announced);
        Assert.AreSequenceEqual(["B", "C", "A"], items);
        Assert.AreEqual("boom", (await PassedOnAsync()).Message);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task AHandlerThatThrowsOnAKeyboardDropIsPassedOnAndTheOthersStillRun()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log, throwing: "Update"));

        await KeyAsync(list, "pick", key: KeyOf(list, "A"));
        await KeyAsync(list, "to", index: 1);
        var dropped = await KeyAsync(list, "drop");

        Assert.IsFalse(dropped.Dragging);
        Assert.AreEqual("Dropped A at position 2 of 3.", dropped.Announce);
        Assert.AreSequenceEqual(["B", "A", "C"], items);
        // Sort and End still follow the Update that threw.
        Assert.AreEqual("Start:L1 Change:L1 ItemsChanged:L1 Update:L1 Sort:L1 End:L1", Sequence(log));
        Assert.AreEqual("boom", (await PassedOnAsync()).Message);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task AnOnChangeThatThrowsWhileThePointerMovesIsPassedOnAndThePositionStillAnswered()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderList(items, "L1", p => p.Add(c => c.OnChange, (BortableEventArgs<string> _) => throw new InvalidOperationException("boom")));

        await DragStartAsync(list, "A");
        var result = await OverResultAsync(list, 3);

        Assert.AreEqual(1, result);
        Assert.AreSequenceEqual(["B", "C", "A"], items);
        Assert.AreEqual("boom", (await PassedOnAsync()).Message);
    }

    [TestMethod]
    public async Task AHandlerThatThrowsDuringAMoveMadeByCodeStillLetsTheOthersRunAndReachesTheCaller()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => Record(p, "L1", log, throwing: "Sort"));

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => list.InvokeAsync(() => list.Instance.MoveAsync("A", 2)));

        Assert.AreEqual("boom", error.Message);
        Assert.AreSequenceEqual(["B", "C", "A"], items);
        Assert.Contains("End:L1", Sequence(log));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public async Task ExceptionsThatRefusedADropAndThenReportedItArePassedOnInTheOrderTheyWereThrown()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p =>
        {
            p.Add(c => c.ConfirmDrop, _ => throw new InvalidOperationException("refused"));
            p.Add(c => c.OnEnd, (BortableEventArgs<string> _) => throw new InvalidOperationException("end"));
            p.Add(c => c.ItemsChanged, (IList<string> _) => throw new InvalidOperationException("changed"));
        });

        await DragStartAsync(list, "A");
        await DragOverAsync(list, 3);
        await DropAsync(list);

        // One flat list, the ConfirmDrop that refused the drop first: nothing nested, nothing lost.
        var passedOn = Assert.IsInstanceOfType<AggregateException>(await PassedOnAsync());
        Assert.AreSequenceEqual(["refused", "changed", "end"], passedOn.InnerExceptions.Select(e => e.Message));
        Assert.AreSequenceEqual(["A", "B", "C"], items);
    }

    // ---- SelectedItemsChanged ----

    /// <summary>A MultiDrag list with B selected, whose SelectedItemsChanged throws from the next change on.</summary>
    private IRenderedComponent<Bortable<string>> RenderSelecting(List<string> items, List<Raised<string>> log, Func<bool> throws)
    {
        var list = RenderList(items, "L1", p =>
        {
            p.Add(c => c.MultiDrag, true);
            p.Add(c => c.SelectedItemsChanged, (IReadOnlyCollection<string> _) =>
            {
                if (throws()) throw new InvalidOperationException("boom");
            });
            Record(p, "L1", log);
        });
        return list;
    }

    [TestMethod]
    public async Task ASelectedItemsChangedThatThrowsAsADragDropsTheSelectionLetsTheDragGoOn()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        bool throws = false;
        var list = RenderSelecting(items, log, () => throws);
        await ClickAsync(list, "B");
        throws = true;

        // Picking up the unselected A drops the selection, which the handler refuses to hear.
        var started = await DragStartAsync(list, "A");

        Assert.IsNotNull(started, "The drag goes on.");
        Assert.AreEqual("boom", (await PassedOnAsync()).Message);
        Assert.AreEqual("", LabelsWithClass(list, "bit-bortable-selected"));
        Assert.AreEqual("Start:L1", Sequence(log));

        await DragOverAsync(list, 3);
        await DropAsync(list);

        Assert.AreSequenceEqual(["B", "C", "A"], items);
        Assert.IsTrue(Sequence(log).EndsWith("End:L1", StringComparison.Ordinal), Sequence(log));
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task ASelectedItemsChangedThatThrowsOnShiftSpaceIsPassedOnAndTheSelectionStillSaid()
    {
        List<string> items = ["A", "B", "C"];
        var list = RenderSelecting(items, [], () => true);

        var selected = await KeyAsync(list, "select", key: KeyOf(list, "B"));

        Assert.AreEqual("B selected. 1 item selected.", selected.Announce);
        Assert.AreEqual("B", LabelsWithClass(list, "bit-bortable-selected"));
        Assert.AreEqual("boom", (await PassedOnAsync()).Message);
    }

    // ---- a handler that takes its list off the page ----

    [TestMethod]
    public async Task AHandlerThatRemovesItsListAndThrowsIsPassedOnByAnotherListOfThePage()
    {
        List<string> items = ["A", "B", "C"];
        IRenderedComponent<Toggle>? host = null;
        host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l =>
        {
            l.Add(c => c.Id, "L1");
            l.Add(c => c.Items, items);
            l.Add(c => c.ItemTemplate, i => $"<span>{i}</span>");
            l.Add(c => c.OnEnd, (BortableEventArgs<string> _) =>
            {
                host!.Render(t => t.Add(h => h.Show, false));
                throw new InvalidOperationException("boom");
            });
        }));
        var l1 = host.FindComponent<Bortable<string>>();
        RenderList(["X"], "L2");

        await DragStartAsync(l1, "A");
        await DragOverAsync(l1, 3);
        var announced = await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).EndAsync("drop", 0));

        // Not the renderer's "no component with ID" for the list that is gone: the handler's own exception.
        Assert.AreEqual("Dropped A at position 3 of 3.", announced);
        Assert.AreEqual("boom", (await PassedOnAsync()).Message);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task AHandlerThatRemovesTheLastListAndThrowsDoesNotFailTheBrowsersCall()
    {
        List<string> items = ["A", "B", "C"];
        IRenderedComponent<Toggle>? host = null;
        host = Render<Toggle>(p => p.AddChildContent<Bortable<string>>(l =>
        {
            l.Add(c => c.Id, "L1");
            l.Add(c => c.Items, items);
            l.Add(c => c.ItemTemplate, i => $"<span>{i}</span>");
            l.Add(c => c.OnEnd, (BortableEventArgs<string> _) =>
            {
                host!.Render(t => t.Add(h => h.Show, false));
                throw new InvalidOperationException("boom");
            });
        }));
        var l1 = host.FindComponent<Bortable<string>>();

        await DragStartAsync(l1, "A");
        await DragOverAsync(l1, 3);
        var announced = await Renderer.Dispatcher.InvokeAsync(() => Zone(l1).EndAsync("drop", 0));

        // With no list left to raise it, it is logged.
        Assert.AreEqual("Dropped A at position 3 of 3.", announced);
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
        Assert.IsNull(Coordinator.Context);
    }
}

/// <summary>
/// An import of <c>bit-bortable.js</c> that failed is not kept, so the next list, or render, imports it again; one that
/// was canceled is made again at once.
/// </summary>
[TestClass]
public class ModuleImportTests : BunitContext
{
    [TestMethod]
    public async Task AFailedImportIsTriedAgain()
    {
        var js = new FirstImportPendingJSRuntime();
        var service = new BortableService(js, []);

        var failing = service.GetModuleAsync();
        js.FirstImport.SetException(new JSException("Failed to fetch dynamically imported module"));
        await Assert.ThrowsExactlyAsync<JSException>(() => failing);
        var module = await service.GetModuleAsync();

        Assert.IsNotNull(module);
        Assert.AreEqual(2, js.Imports);
    }

    [TestMethod]
    public async Task AListWhoseImportFailedImportsAgainOnItsNextRender()
    {
        var js = new FirstImportPendingJSRuntime();
        var list = RenderList(js);

        // Renders that come while the import is pending do not wait for it: its failure is met once, not once each.
        list.Render();
        list.Render();

        // The import fails only once the first render is over, as a download does when the browser answers.
        js.FirstImport.SetException(new JSException("Failed to fetch dynamically imported module"));
        Assert.IsInstanceOfType<JSException>(await Renderer.UnhandledException);

        // bUnit throws the exception the last render left unhandled once more, from the next one.
        Assert.ThrowsExactly<JSException>(() => list.Render());
        list.Render();

        Assert.AreEqual(2, js.Imports);
        Assert.AreEqual(1, js.ModuleCalls.Count(call => call == "register"));
    }

    [TestMethod]
    public void ACanceledImportIsMadeAgainAtOnce()
    {
        var js = new FirstImportPendingJSRuntime();
        var list = RenderList(js);

        // A canceled import (an interop call that timed out) is not left for a render that may never come.
        js.FirstImport.SetCanceled();

        list.WaitForAssertion(() => Assert.Contains("register", js.ModuleCalls));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
        Assert.AreEqual(2, js.Imports);
    }

    private IRenderedComponent<Bortable<string>> RenderList(IJSRuntime js)
    {
        Services.AddBitBortableServices();
        Services.AddSingleton(js);

        return Render<Bortable<string>>(p => p
            .Add(c => c.Items, new List<string> { "A" })
            .Add(c => c.ItemTemplate, i => $"<span>{i}</span>"));
    }

    private sealed class FirstImportPendingJSRuntime : IJSRuntime
    {
        // The first import stays pending until the test fails or cancels it, as a module's download does until the
        // browser answers; the next one answers at once.
        public TaskCompletionSource<IJSObjectReference> FirstImport { get; } = new();

        public int Imports { get; private set; }

        public List<string> ModuleCalls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier != "import") return ValueTask.FromResult(default(TValue)!);
            return ++Imports == 1
                ? new ValueTask<TValue>((Task<TValue>)(object)FirstImport.Task)
                : ValueTask.FromResult((TValue)(object)new Module(ModuleCalls));
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }

    private sealed class Module(List<string> calls) : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            calls.Add(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
