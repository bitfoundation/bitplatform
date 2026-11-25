using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Sortable;

/// <summary>
/// The app's own handlers throwing during a drag the user makes. The browser layer's call that ran them is answered all
/// the same - what to announce, where the item is - every other handler still hears of the drag, and what the handler
/// threw is passed on as the list's own, for the nearest <c>ErrorBoundary</c>, rather than going back to the browser
/// layer, where nothing would report it.
/// </summary>
[TestClass]
public class BitSortableHandlerFailureTests : BitSortableTestContext
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
        var list = RenderList(items, "L1", p => p.Add(c => c.OnChange, (BitSortableEventArgs<string> _) => throw new InvalidOperationException("boom")));

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
            p.Add(c => c.OnEnd, (BitSortableEventArgs<string> _) => throw new InvalidOperationException("end"));
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
    private IRenderedComponent<BitSortable<string>> RenderSelecting(List<string> items, List<Raised<string>> log, Func<bool> throws)
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
        Assert.AreEqual("", LabelsWithClass(list, "bit-srt-sel"));
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
        Assert.AreEqual("B", LabelsWithClass(list, "bit-srt-sel"));
        Assert.AreEqual("boom", (await PassedOnAsync()).Message);
    }

    // ---- a handler that takes its list off the page ----

    [TestMethod]
    public async Task AHandlerThatRemovesItsListAndThrowsIsPassedOnByAnotherListOfThePage()
    {
        List<string> items = ["A", "B", "C"];
        IRenderedComponent<Toggle>? host = null;
        host = Render<Toggle>(p => p.AddChildContent<BitSortable<string>>(l =>
        {
            l.Add(c => c.Id, "L1");
            l.Add(c => c.Items, items);
            l.Add(c => c.ItemTemplate, i => $"<span>{i}</span>");
            l.Add(c => c.OnEnd, (BitSortableEventArgs<string> _) =>
            {
                host!.Render(t => t.Add(h => h.Show, false));
                throw new InvalidOperationException("boom");
            });
        }));
        var l1 = host.FindComponent<BitSortable<string>>();
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
        host = Render<Toggle>(p => p.AddChildContent<BitSortable<string>>(l =>
        {
            l.Add(c => c.Id, "L1");
            l.Add(c => c.Items, items);
            l.Add(c => c.ItemTemplate, i => $"<span>{i}</span>");
            l.Add(c => c.OnEnd, (BitSortableEventArgs<string> _) =>
            {
                host!.Render(t => t.Add(h => h.Show, false));
                throw new InvalidOperationException("boom");
            });
        }));
        var l1 = host.FindComponent<BitSortable<string>>();

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
/// The list's script as the lazy scripts load it (<see cref="BitBlazorUIScripts.UseLazyScripts"/>): imported once the
/// list has rendered, once for every list of the scope, before the list registers with it; and an import that failed or
/// was canceled is not kept, so the registration that follows imports it again rather than leaving the list unregistered.
/// In bundle mode, every other test's, the script is the package bundle's, which the page loads, and nothing is imported.
/// </summary>
/// <remarks>The script mode is process-wide, so these tests never run next to others.</remarks>
[TestClass, DoNotParallelize]
public class BitSortableScriptImportTests : BitSortableTestContext
{
    [TestInitialize]
    public void UseLazyScripts() => BitBlazorUIScripts.UseLazyScripts();

    [TestCleanup]
    public void UseBundledScripts() => BitBlazorUIScripts.UseBundledScripts();

    private static bool IsTheListsScript(object? url) => url is string s && s.EndsWith("/BitSortable.js", StringComparison.Ordinal);

    [TestMethod]
    public void TheScriptIsImportedOnceTheListHasRendered()
    {
        RenderList(["A"]);

        // Every engine decision that needs a measurement goes through this script, so a list that never imports it
        // silently refuses every drag.
        var invocations = JSInterop.Invocations.ToList();
        var import = invocations.FindIndex(i => i.Identifier == "import" && IsTheListsScript(i.Arguments[0]));
        Assert.IsGreaterThanOrEqualTo(0, import, string.Join(", ", invocations.Select(i => $"{i.Identifier} {string.Join(" ", i.Arguments)}")));
        Assert.AreEqual("./_content/Bit.BlazorUI.Extras/scripts/modules/BitSortable.js", invocations[import].Arguments[0]);
        Assert.IsLessThan(invocations.FindIndex(i => i.Identifier == Register), import, "The list registered before its script was imported.");
    }

    [TestMethod]
    public void EveryListOfTheScopeSharesOneImport()
    {
        RenderList(["A"]);
        RenderList(["B"]);
        RenderList(["C"]);

        // A tree with a list per node still imports the script once; each list then registers with it.
        Assert.ContainsSingle(JSInterop.Invocations["import"].Where(i => IsTheListsScript(i.Arguments[0])));
        Assert.HasCount(3, JSInterop.Invocations[Register]);
    }

    [TestMethod]
    public void UntilTheScriptIsImportedTheListIsNotRegistered()
    {
        // Interactive but still waiting for the script: the import has not completed, so the browser layer does not know
        // the list and routes no drag to it. (Prerendered, a list makes no call at all.)
        JSInterop.SetupVoid("import", _ => true);
        RenderList(["A", "B"]);

        Assert.IsNotEmpty(JSInterop.Invocations["import"]);
        Assert.IsEmpty(JSInterop.Invocations[Register]);
    }

    [TestMethod]
    public void AFailedImportIsTriedAgainByTheRegistration()
    {
        var js = UseRuntime();
        var list = RenderList(["A"]);

        // Renders that come while the import is pending do not wait for it, nor register on their own.
        list.Render();
        list.Render();
        Assert.IsEmpty(js.Calls.Where(c => c == Register));

        // The import fails only once the first render is over, as a download does when the browser answers.
        js.FirstImport.SetException(new JSException("Failed to fetch dynamically imported module"));

        // The renders that were waiting for it may hear of the failure, as they did of the module import's (bUnit throws
        // the exception a render left unhandled once more, from the next one); what matters is that the list does not
        // stay unregistered: a later render imports the script again and registers the list.
        for (int i = 0; i < 3 && js.Calls.Contains(Register) is false; i++)
        {
            try { list.Render(); } catch (JSException) { }
        }

        Assert.ContainsSingle(js.Calls.Where(c => c == Register), $"The list never registered: {string.Join(", ", js.Calls)}");
        Assert.IsGreaterThan(1, js.Calls.Count(c => c == "import"), "The failed import was not tried again.");
    }

    [TestMethod]
    public void ACanceledImportIsMadeAgainAtOnce()
    {
        var js = UseRuntime();
        var list = RenderList(["A"]);

        // A canceled import (an interop call that timed out) is not left for a render that may never come.
        js.FirstImport.SetCanceled();

        list.WaitForAssertion(() => Assert.ContainsSingle(js.Calls.Where(c => c == Register)));
        Assert.IsFalse(Renderer.UnhandledException.IsCompleted);
        Assert.IsGreaterThan(1, js.Calls.Count(c => c == "import"));
    }

    private FirstImportPendingJSRuntime UseRuntime()
    {
        var js = new FirstImportPendingJSRuntime();
        Services.AddSingleton<IJSRuntime>(js);
        return js;
    }

    private sealed class FirstImportPendingJSRuntime : IJSRuntime
    {
        // The first import stays pending until the test fails or cancels it, as a module's download does until the
        // browser answers; the next ones answer at once.
        public TaskCompletionSource FirstImport { get; } = new();

        private int _imports;

        public List<string> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add(identifier);
            if (identifier == "import" && ++_imports == 1) return Pending<TValue>();
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        private async ValueTask<TValue> Pending<TValue>()
        {
            await FirstImport.Task;
            return default!;
        }
    }
}
