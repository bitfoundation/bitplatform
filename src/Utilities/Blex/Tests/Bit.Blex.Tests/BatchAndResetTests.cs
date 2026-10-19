using System.Threading.Tasks;
using Bit.Blex.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class BatchAndResetTests
{
    [TestMethod]
    public void Batch_GroupsAdHocMutations_IntoOneActionAndNotification()
    {
        using var harness = BlexTestHarness.For<CounterStore>();
        var store = harness.Store;

        var notifications = store.CountNotifications(() =>
            store.Batch("Apply preset", () =>
            {
                store.Count = 10;
                store.Label = "preset";
            }));

        Assert.AreEqual(1, notifications);
        Assert.AreEqual(1, harness.Log.Count);
        Assert.AreEqual("Apply preset", harness.Log.Last!.ActionName);
        Assert.AreEqual(10, store.Count);
        Assert.AreEqual("preset", store.Label);
    }

    [TestMethod]
    public void Batch_IsVetoable_ByFilterMiddleware()
    {
        var store = new CounterStore();
        var manager = new BlexManager([new BlexFilterMiddleware(ctx => ctx.ActionName != "Blocked")]);
        manager.Register(store);

        store.Batch("Blocked", () => store.Count = 99);

        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void ResetState_RestoresInitialValues_AndRecordsAction()
    {
        using var harness = BlexTestHarness.For<CounterStore>();
        var store = harness.Store;

        store.Add(41);
        store.Label = "changed";
        store.ResetState();

        Assert.AreEqual(0, store.Count);
        Assert.AreEqual("idle", store.Label);
        Assert.AreEqual("ResetState", harness.Log.Last!.ActionName);
    }

    [TestMethod]
    public void ResetState_InvalidatesComputedValues()
    {
        using var harness = BlexTestHarness.For<CounterStore>();
        var store = harness.Store;

        store.Add(5);
        Assert.AreEqual(10, store.DoubleCount);

        store.ResetState();
        Assert.AreEqual(0, store.DoubleCount);
    }

    [TestMethod]
    public void ResetState_OnAnUnchangedStore_RecordsNothingAndDoesNotNotify()
    {
        using var harness = BlexTestHarness.For<CounterStore>();
        var store = harness.Store;
        var notifications = 0;
        store.StateChanged += () => notifications++;

        store.ResetState();

        Assert.AreEqual(0, harness.Log.Count);
        Assert.AreEqual(0, notifications);

        // ...and it still works once the store has actually drifted.
        store.Add(3);
        store.ResetState();
        Assert.AreEqual(0, store.Count);
        Assert.AreEqual("ResetState", harness.Log.Last!.ActionName);
    }

    [TestMethod]
    public void ResetState_IsNoOp_WhenStoreWasNeverRegistered()
    {
        var store = new CounterStore();
        store.Increment();

        store.ResetState(); // no manager, no captured baseline -> nothing happens

        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public void RestoreState_PublicHydration_NotifiesAndInvalidatesComputed()
    {
        var store = new CounterStore();
        store.Increment();
        var snapshot = store.SerializeState();

        var fresh = new CounterStore();
        var notified = 0;
        fresh.StateChanged += () => notified++;
        fresh.RestoreState(snapshot);

        Assert.AreEqual(1, notified);
        Assert.AreEqual(1, fresh.Count);
        Assert.AreEqual(2, fresh.DoubleCount); // computed must not be stale
    }

    [TestMethod]
    public async Task NestedSyncAction_InsideAsyncAction_StillNotifies()
    {
        var store = new CounterStore();
        var notifications = 0;
        store.StateChanged += () => notifications++;

        // A sync [BlexActionAttribute] invoked while an async action is awaiting must flush its own
        // notification; the async action's record wraps it, but the render must not be lost.
        var task = store.LoadData(); // async action: sets Label, awaits, sets Count/Label

        store.Increment(); // nested relative to the in-flight async action

        Assert.IsTrue(notifications >= 2, $"expected the nested action to notify, got {notifications}");
        await task;
    }
}
