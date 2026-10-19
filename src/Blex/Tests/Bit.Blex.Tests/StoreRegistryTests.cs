using System.Collections.Generic;
using System.Linq;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// The registry is copy-on-write, so a lazily-loaded feature that registers (or tears down) a store
/// from inside a notification cannot invalidate a walk already in progress.
/// </summary>
[TestClass]
public class StoreRegistryTests
{
    [TestMethod]
    public void RegisteringFromInsideANotification_DoesNotInvalidateTheEnumeration()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        var added = false;
        counter.StateChanged += () =>
        {
            if (added)
                return;
            added = true;
            manager.Register(new SettingsStore()); // a lazily-loaded feature arriving mid-dispatch
        };

        // CaptureGlobalState walks the registry; the notification fires underneath it.
        counter.Increment();

        var state = manager.CaptureGlobalState();
        Assert.IsTrue(state.ContainsKey("counter"));
        Assert.IsTrue(state.ContainsKey("settings"));
    }

    [TestMethod]
    public void SnapshotTakenBeforeRegistration_IsUnaffectedByIt()
    {
        var manager = new BlexManager();
        manager.Register(new CounterStore());

        var snapshot = manager.Stores;
        manager.Register(new SettingsStore());

        Assert.ContainsSingle(snapshot);            // the old view is frozen...
        Assert.AreEqual(2, manager.Stores.Count); // ...and the live one moved on
    }

    [TestMethod]
    public void SnapshotTakenBeforeUnregistration_IsUnaffectedByIt()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var settings = new SettingsStore();
        manager.Register(counter);
        manager.Register(settings);

        var snapshot = manager.Stores;
        manager.Unregister(counter);

        Assert.AreEqual(2, snapshot.Count);
        Assert.AreSequenceEqual([settings], manager.Stores);
    }

    [TestMethod]
    public void Unregister_PreservesTheOrderOfTheRemainingStores()
    {
        var manager = new BlexManager();
        var a = new CounterStore();
        var b = new SettingsStore();
        var c = new ProfileStore();
        manager.Register(a);
        manager.Register(b);
        manager.Register(c);

        manager.Unregister(b);

        Assert.AreSequenceEqual(["counter", "profile"], manager.Stores.Select(s => s.Name));
    }

    [TestMethod]
    public void UnregisteringAnUnknownStore_IsANoOp()
    {
        var manager = new BlexManager();
        manager.Register(new CounterStore());

        manager.Unregister(new SettingsStore());

        Assert.ContainsSingle(manager.Stores);
    }

    [TestMethod]
    public void GetStoreByName_MatchesTheGlobalStateKeys()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        manager.Register(new SettingsStore());

        Assert.AreSame(counter, manager.GetStore("counter"));
        Assert.IsNull(manager.GetStore("nope"));
        foreach (var kvp in manager.CaptureGlobalState())
        {
            Assert.IsNotNull(manager.GetStore(kvp.Key));
        }
    }

    [TestMethod]
    public void GetStoreByName_ForgetsAnUnregisteredStore()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        manager.Unregister(counter);

        Assert.IsNull(manager.GetStore("counter"));
    }

    [TestMethod]
    public void UnregisteredStore_StopsBeingObserved_ButKeepsWorking()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var seen = new List<string>();
        using var sub = manager.Subscribe(ctx => seen.Add(ctx.ActionName));

        counter.Increment();
        manager.Unregister(counter);
        counter.Increment();

        Assert.AreSequenceEqual(["Increment"], seen);
        Assert.AreEqual(2, counter.Count); // the store itself still works standalone
    }
}
