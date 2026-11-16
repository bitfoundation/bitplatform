using Bunit;
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
    private Task<BortableKeyResult> KeyAsync<TItem>(IRenderedComponent<Bortable<TItem>> list, string action, string? key = null, int index = -1)
        => Renderer.Dispatcher.InvokeAsync(() => Zone(list).KeyAsync(key ?? "", action, null, index));

    /// <summary>Wires every callback into <paramref name="log"/>, as <c>Record</c> does; the one named <paramref name="throwing"/> throws once logged.</summary>
    private static void RecordThrowing(ComponentParameterCollectionBuilder<Bortable<string>> p, string list, List<Raised<string>> log, string throwing)
    {
        void On(string name, BortableEventArgs<string> e)
        {
            log.Add(new(list, name, e));
            if (name == throwing) throw new InvalidOperationException("boom");
        }

        p.Add(c => c.OnStart, e => On("Start", e));
        p.Add(c => c.OnChange, e => On("Change", e));
        p.Add(c => c.OnUpdate, e => On("Update", e));
        p.Add(c => c.OnRemove, e => On("Remove", e));
        p.Add(c => c.OnAdd, e => On("Add", e));
        p.Add(c => c.OnSort, e => On("Sort", e));
        p.Add(c => c.OnEnd, e => On("End", e));
        p.Add(c => c.ItemsChanged, _ => log.Add(new(list, "ItemsChanged", null)));
    }

    [TestMethod]
    public async Task AnOnEndThatThrowsOnAPointerDropIsPassedOnAndTheDropIsStillAnnounced()
    {
        List<string> items = ["A", "B", "C"];
        var log = new List<Raised<string>>();
        var list = RenderList(items, "L1", p => RecordThrowing(p, "L1", log, "End"));

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
        var list = RenderList(items, "L1", p => RecordThrowing(p, "L1", log, "Update"));

        await KeyAsync(list, "pick", KeyOf(list, "A"));
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
        var list = RenderList(items, "L1", p => RecordThrowing(p, "L1", log, "Sort"));

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => list.InvokeAsync(() => list.Instance.MoveAsync("A", 2)));

        Assert.AreEqual("boom", error.Message);
        Assert.AreSequenceEqual(["B", "C", "A"], items);
        Assert.Contains("End:L1", Sequence(log));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
    }
}

/// <summary>An import of <c>bit-bortable.js</c> that failed, or was canceled, is not kept, so the next list, or render, imports it again.</summary>
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

        // The import fails only once the first render is over, as a download does when the browser answers.
        js.FirstImport.SetException(new JSException("Failed to fetch dynamically imported module"));
        Assert.IsInstanceOfType<JSException>(await Renderer.UnhandledException);

        // bUnit throws the exception the last render left unhandled once more, from the next one.
        Assert.ThrowsExactly<JSException>(() => list.Render());
        list.Render();

        Assert.AreEqual(2, js.Imports);
        Assert.Contains("register", js.ModuleCalls);
    }

    [TestMethod]
    public void AListWhoseImportWasCanceledImportsAgainOnItsNextRender()
    {
        var js = new FirstImportPendingJSRuntime();
        var list = RenderList(js);

        // A canceled import (an interop call that timed out) throws nothing: the list is left without its module.
        js.FirstImport.SetCanceled();
        list.Render();

        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
        Assert.AreEqual(2, js.Imports);
        Assert.Contains("register", js.ModuleCalls);
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
