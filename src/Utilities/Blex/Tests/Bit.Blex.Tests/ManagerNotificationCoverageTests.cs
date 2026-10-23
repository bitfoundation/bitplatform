using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// State moves in three ways that record no action of their own: an effect's lifecycle flags
/// (<c>IsLoading</c>/<c>Error</c>), a mutation an async action makes before its first <c>await</c>
/// completes, and a nested action inside an async one. A cross-store selector or subscription that
/// only watched recorded actions and restores kept reporting the value from before them.
/// </summary>
[TestClass]
public class ManagerNotificationCoverageTests
{
    [TestMethod]
    public async Task Selector_SeesAnEffectStartLoading()
    {
        var manager = new BlexManager();
        var store = new GatedStore();
        manager.Register(store);

        using var loading = manager.CreateSelector(() => store.SlowIsLoading);
        Assert.IsFalse(loading.Value);

        var run = store.Slow();
        Assert.IsTrue(loading.Value, "the selector still reports the value from before the effect started");

        store.Gate.SetResult();
        await run;
        Assert.IsFalse(loading.Value);
    }

    [TestMethod]
    public async Task Subscription_SeesAnEffectFailThatChangedNoState()
    {
        var manager = new BlexManager();
        var store = new FailingStore();
        manager.Register(store);

        var seen = new List<bool>();
        using var sub = manager.Subscribe(() => store.LoadError is not null, seen.Add);

        await store.Load("orders");

        Assert.AreSequenceEqual([true], seen);
    }

    [TestMethod]
    public async Task Selector_SeesAMutationMadeBeforeAnAsyncActionAwaits()
    {
        var manager = new BlexManager();
        var store = new CounterStore();
        manager.Register(store);

        using var label = manager.CreateSelector(() => store.Label);
        Assert.AreEqual("idle", label.Value);

        var load = store.LoadData();
        Assert.AreEqual("loading", label.Value);

        await load;
        Assert.AreEqual("loaded", label.Value);
    }

    [TestMethod]
    public async Task Subscription_SeesAnAsyncActionsChangesWhenItIsRecorded()
    {
        var manager = new BlexManager();
        var store = new GatedStore();
        manager.Register(store);

        var seen = new List<int>();
        using var sub = manager.Subscribe(() => store.Count, seen.Add);
        using var selector = manager.CreateSelector(() => store.Count);

        var run = store.Slow();     // parks on the gate: the store's async action is in flight
        store.Bump();               // its own action, folded into the one in flight

        Assert.AreEqual(100, selector.Value, "a pull-based selector sees the change at once");
        Assert.IsEmpty(seen, "a push subscription waits for the record, so a follow-up cannot overtake its cause");

        store.Gate.SetResult();
        await run;
        Assert.AreSequenceEqual([101], seen);
    }

    [TestMethod]
    public async Task A_reactor_to_an_effects_change_is_recorded_after_the_effect_and_undoes_cleanly()
    {
        var manager = new BlexManager();
        var gated = new GatedStore();
        var counter = new CounterStore();
        manager.Register(gated);
        manager.Register(counter);
        var history = new BlexHistory(manager);
        history.Start();
        var recorded = new List<string>();
        using var log = manager.Subscribe(ctx => recorded.Add(ctx.QualifiedName));

        // A reactor that dispatches a follow-up when the effect's result lands.
        using var reactor = manager.Subscribe(() => gated.Count, count => counter.Add(count));

        var run = gated.Slow();
        gated.Gate.SetResult();
        await run;

        Assert.AreSequenceEqual(["gated/Slow", "counter/Add"], recorded, "the cause is recorded before its reaction");
        Assert.AreEqual(1, counter.Count);

        history.Undo();   // the reaction
        Assert.AreEqual(0, counter.Count);
        Assert.AreEqual(1, gated.Count);

        history.Undo();   // the effect - a real step, not a no-op
        Assert.AreEqual(0, gated.Count);
    }

    [TestMethod]
    public void Subscription_StillFiresAfterTheActionIsRecorded()
    {
        // A subscription reacting to a synchronous action must still run after middleware and
        // DevTools have seen that action, so a follow-up it dispatches cannot overtake it.
        var order = new List<string>();
        var manager = new BlexManager([new RecordingMiddleware(order)]);
        var store = new CounterStore();
        manager.Register(store);

        using var sub = manager.Subscribe(() => store.Count, _ => order.Add("subscription"));

        store.Increment();

        Assert.AreSequenceEqual(["middleware:Increment", "subscription"], order);
    }

    [TestMethod]
    public void Selector_DoesNotMakeTheManagerCaptureArguments()
    {
        // A pull-based selector only needs to know that something moved; it must not switch on
        // the per-action argument capture and context allocation that observers pay for.
        var manager = new BlexManager();
        var store = new CounterStore();
        manager.Register(store);

        using var doubled = manager.CreateSelector(() => store.Count * 2);

        Assert.IsFalse(manager.HasObservers);
        store.Increment();
        Assert.AreEqual(2, doubled.Value);
    }

    private sealed class RecordingMiddleware(List<string> order) : IBlexMiddleware
    {
        public void OnAction(BlexActionContext context) => order.Add("middleware:" + context.ActionName);
    }
}
