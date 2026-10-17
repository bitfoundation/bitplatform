using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class ManagerExtrasTests
{
    [TestMethod]
    public void OnError_ReceivesSubscriberFailures()
    {
        var store = new CounterStore();
        var manager = new BlexManager();
        manager.Register(store);
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        using var sub = manager.Subscribe(_ => throw new InvalidOperationException("boom"));
        store.Increment();

        Assert.AreEqual(1, store.Count); // dispatch survived
        var error = Assert.ContainsSingle(errors);
        Assert.AreEqual("subscriber", error.Source);
        Assert.AreEqual("boom", error.Exception.Message);
        Assert.AreEqual("counter/Increment", error.Detail);
    }

    [TestMethod]
    public void OnError_ReceivesMiddlewareFailures()
    {
        var errors = new List<BlexError>();
        var store = new CounterStore();
        var manager = new BlexManager([new BlexDelegateMiddleware(_ => throw new InvalidOperationException("mw"))])
        {
            OnError = errors.Add,
        };
        manager.Register(store);

        store.Increment();

        Assert.AreEqual(1, store.Count);
        var error = Assert.ContainsSingle(errors);
        Assert.AreEqual("middleware", error.Source);
    }

    [TestMethod]
    public void ThrowingOnErrorHandler_DoesNotBreakDispatch()
    {
        var store = new CounterStore();
        var manager = new BlexManager();
        manager.Register(store);
        manager.OnError = _ => throw new InvalidOperationException("handler is broken too");

        using var sub = manager.Subscribe(_ => throw new InvalidOperationException("boom"));
        store.Increment();

        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public void StateRestored_IsRaisedAfterRestoreGlobalState()
    {
        var store = new CounterStore();
        var manager = new BlexManager();
        manager.Register(store);

        var before = manager.CaptureGlobalState();
        store.Increment();

        var raised = 0;
        manager.StateRestored += () => raised++;
        manager.RestoreGlobalState(before);

        Assert.AreEqual(1, raised);
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void Restore_WithCorruptSlice_SkipsStoreButRestoresOthers()
    {
        var counter = new CounterStore();
        var settings = new SettingsStore();
        var manager = new BlexManager();
        manager.Register(counter);
        manager.Register(settings);
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        counter.Increment();
        settings.SetTheme("dark");

        var snapshot = manager.CaptureGlobalState();
        snapshot["counter"] = new System.Text.Json.Nodes.JsonObject
        {
            ["Count"] = "not-an-int",
        };

        manager.RestoreGlobalState(snapshot);

        Assert.AreEqual("dark", settings.Theme); // unaffected store restored fine
        var error = Assert.ContainsSingle(errors);
        Assert.AreEqual("restore", error.Source);
    }

    [TestMethod]
    public void Unregister_RemovesStoreFromPipeline()
    {
        var store = new CounterStore();
        var manager = new BlexManager();
        manager.Register(store);

        manager.Unregister(store);

        Assert.IsEmpty(manager.Stores);
        Assert.IsNull(manager.GetStore<CounterStore>());
    }

    [TestMethod]
    public void Unregister_DetachesStore_ActionsNoLongerObserved()
    {
        var store = new CounterStore();
        var manager = new BlexManager();
        manager.Register(store);
        var seen = new List<string>();
        using var sub = manager.Subscribe(ctx => seen.Add(ctx.ActionName));

        store.Increment();
        Assert.ContainsSingle(seen);

        manager.Unregister(store);
        store.Increment(); // still works standalone...
        Assert.AreEqual(2, store.Count);
        Assert.ContainsSingle(seen); // ...but is no longer observed
    }

    [TestMethod]
    public void InFlightSnapshot_IsFrozen_BeforeReactorDispatchesFollowUp()
    {
        var counter = new CounterStore();
        var settings = new SettingsStore();
        var manager = new BlexManager();
        manager.Register(counter);
        manager.Register(settings);

        // First subscriber reacts to the counter action by mutating another store.
        using var reactor = manager.SubscribeTo<CounterStore>(_ =>
        {
            if (settings.Theme == "light")
                settings.SetTheme("dark");
        });

        // Second subscriber (registered later) lazily reads the ORIGINAL action's snapshot.
        var snapshots = new List<(string Action, string Theme)>();
        using var observer = manager.Subscribe(ctx => snapshots.Add(
            (ctx.ActionName, ctx.GlobalState["settings"]!["Theme"]!.GetValue<string>())));

        counter.Increment();

        // The Increment snapshot must show the world as of Increment -- before the reactor's
        // follow-up SetTheme mutated it.
        var incrementSnapshot = snapshots.Find(s => s.Action == "Increment");
        Assert.AreEqual("light", incrementSnapshot.Theme);
        var themeSnapshot = snapshots.Find(s => s.Action == "SetTheme");
        Assert.AreEqual("dark", themeSnapshot.Theme);
    }

    [TestMethod]
    public void History_RefreshesPresent_AfterExternalRestore()
    {
        var store = new CounterStore();
        var manager = new BlexManager();
        manager.Register(store);
        using var history = new BlexHistory(manager);
        history.Start();

        store.Increment();                       // 1
        var snapshotAt1 = manager.CaptureGlobalState();
        store.Increment();                       // 2

        manager.RestoreGlobalState(snapshotAt1); // external jump (DevTools-style) back to 1
        store.Increment();                       // 2 again, from the jumped-to state

        history.Undo();

        // Undo must return to the state the user actually saw before the last action (1, the
        // post-jump present) -- not the stale pre-jump snapshot (2).
        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public void ThrowingSubscriber_DuringStandaloneSet_DoesNotLeakDirtyFlag()
    {
        var store = new CounterStore();
        var manager = new BlexManager();
        manager.Register(store);
        var log = new List<string>();
        using var sub = manager.Subscribe(ctx => log.Add(ctx.ActionName));

        void Throwing() => throw new InvalidOperationException("render boom");
        store.StateChanged += Throwing;
        try
        {
            store.Count = 42;
        }
        catch (InvalidOperationException)
        {
            // The subscriber's exception propagates to the caller; that's fine.
        }

        store.StateChanged -= Throwing;

        // The set itself must still have been recorded (the mutation applied)...
        Assert.Contains("Set Count", log);

        // ...and a later no-op batch must NOT be recorded as a phantom action.
        store.Batch("Noop", () => { });
        Assert.DoesNotContain("Noop", log);
    }

    [TestMethod]
    public void RestoreFromInsideReactor_DoesNotCorruptInFlightSnapshots()
    {
        var store = new CounterStore();
        var manager = new BlexManager();
        manager.Register(store);
        var baseline = manager.CaptureGlobalState(); // Count = 0

        // Reactor 1 (subscribed first) undoes everything by restoring the baseline.
        using var reactor = manager.SubscribeToAction("Add", _ => manager.RestoreGlobalState(baseline));

        // Observer 2 (subscribed later) lazily reads the ORIGINAL action's snapshot.
        var counts = new List<int>();
        using var observer = manager.Subscribe(ctx => counts.Add(
            ctx.GlobalState["counter"]!["Count"]!.GetValue<int>()));

        store.Add(7);

        // The Add snapshot must show 7 (frozen before the reactor's restore), even though the
        // live store is back at 0.
        Assert.AreSequenceEqual([7], counts);
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void MalformedDevToolsMessage_DoesNotThrow()
    {
        var manager = new BlexManager();
        manager.Register(new CounterStore());

        manager.HandleDevToolsMessage("""{"type":"DISPATCH","payload":{"type":"JUMP_TO_STATE"},"state":"{corrupt"}""");
        manager.HandleDevToolsMessage("not json at all");
    }
}
