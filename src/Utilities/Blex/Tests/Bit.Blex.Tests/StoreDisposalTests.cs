using System;
using System.Threading.Tasks;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// Disposing a store quiesces it: in-flight effects are cancelled and pending debounce timers stop,
/// so a scope torn down mid-request does not leave work running against it.
/// </summary>
[TestClass]
public class StoreDisposalTests
{
    [TestMethod]
    public async Task Dispose_CancelsAnInFlightEffect()
    {
        var manager = new BlexManager();
        var store = new FailingStore();
        manager.Register(store);

        var watch = store.Watch();
        Assert.IsTrue(store.WatchIsLoading);

        store.Dispose();
        await watch.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.IsFalse(store.WatchIsLoading);
        Assert.AreEqual(0, store.Value);      // the body never got past the gate
        Assert.IsNull(store.WatchError);     // cancellation is a normal outcome, not a failure
    }

    [TestMethod]
    public async Task Dispose_StopsAPendingDebounce()
    {
        var manager = new BlexManager();
        var store = new FailingStore();
        manager.Register(store);

        var pending = store.SlowDebounce();   // parks for 10 s unless cancelled
        store.Dispose();

        // Without cancelling the debounce source this would sit out the full interval.
        await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(0, store.Value);
    }

    [TestMethod]
    public void Dispose_IsIdempotent()
    {
        var store = new FailingStore();
        store.Dispose();
        store.Dispose();
    }

    [TestMethod]
    public void Dispose_LeavesTheStoreUsable()
    {
        var manager = new BlexManager();
        var store = new FailingStore();
        manager.Register(store);
        store.Dispose();

        // Disposal is a best-effort quiesce, not a hard kill: the store still serializes and still
        // dispatches, so teardown ordering cannot strand a final persistence flush.
        store.Set(5);
        Assert.AreEqual(5, store.Value);
        Assert.AreEqual(5, store.SerializeState()["Value"]!.GetValue<int>());
    }

    [TestMethod]
    public void AStoreWithoutCancellableEffects_DisposesCleanly()
    {
        var store = new CounterStore();
        store.Dispose();
        store.Increment();
        Assert.AreEqual(1, store.Count);
    }
}
