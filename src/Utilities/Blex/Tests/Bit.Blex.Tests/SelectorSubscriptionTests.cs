using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// Equality-gated subscriptions: the store-level extension and its cross-store counterpart on
/// <see cref="BlexManager"/>. Both exist so a consumer pays for changes it actually cares about
/// rather than for every action in the application.
/// </summary>
[TestClass]
public class SelectorSubscriptionTests
{
    [TestMethod]
    public void StoreSelector_FiresOnlyWhenTheProjectionChanges()
    {
        var store = new CounterStore();
        var seen = new List<int>();

        using var sub = store.Subscribe(() => store.Count, seen.Add);

        store.Increment();
        store.Label = "unrelated"; // different field, same projection
        store.Increment();

        Assert.AreSequenceEqual([1, 2], seen);
    }

    [TestMethod]
    public void StoreSelector_FireImmediately_ThatThrows_DoesNotLeaveTheHandlerAttached()
    {
        var store = new CounterStore();
        var calls = 0;

        Assert.ThrowsExactly<InvalidOperationException>(() => store.Subscribe(
            () => store.Count,
            _ =>
            {
                calls++;
                throw new InvalidOperationException("callback boom");
            },
            fireImmediately: true));

        // The caller never got a token to dispose, so a handler left attached here would keep
        // firing for the lifetime of the store with no way to stop it.
        store.Increment();
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void StoreSelector_Dispose_StopsObserving()
    {
        var store = new CounterStore();
        var seen = new List<int>();
        var sub = store.Subscribe(() => store.Count, seen.Add);

        store.Increment();
        sub.Dispose();
        store.Increment();
        sub.Dispose(); // idempotent

        Assert.AreSequenceEqual([1], seen);
    }

    [TestMethod]
    public void ManagerSelector_FiresOnlyWhenTheCrossStoreProjectionChanges()
    {
        var counter = new CounterStore();
        var profile = new ProfileStore();
        var manager = new BlexManager();
        manager.Register(counter);
        manager.Register(profile);

        var seen = new List<string>();
        using var sub = manager.Subscribe(
            () => $"{profile.UserName}:{counter.Count}",
            seen.Add);

        counter.Increment();          // projection changes
        counter.Label = "unrelated";  // an action, but the projection is unchanged
        profile.SignIn("ada");        // a different store, projection changes

        Assert.AreSequenceEqual(["anonymous:1", "ada:1"], seen);
    }

    [TestMethod]
    public void ManagerSelector_ReportsPreviousAndCurrent()
    {
        var counter = new CounterStore();
        var manager = new BlexManager();
        manager.Register(counter);

        var transitions = new List<(int Previous, int Current)>();
        using var sub = manager.Subscribe<int>(() => counter.Count, (previous, current) => transitions.Add((previous, current)));

        counter.Increment();
        counter.Add(4);

        Assert.AreSequenceEqual([(0, 1), (1, 5)], transitions);
    }

    [TestMethod]
    public void ManagerSelector_FireImmediately_UsesTheValueAtSubscriptionTime()
    {
        var counter = new CounterStore();
        var manager = new BlexManager();
        manager.Register(counter);
        counter.Add(3);

        var seen = new List<int>();
        using var sub = manager.Subscribe(() => counter.Count, seen.Add, fireImmediately: true);

        Assert.AreSequenceEqual([3], seen);

        counter.Increment();
        Assert.AreSequenceEqual([3, 4], seen);
    }

    [TestMethod]
    public void ManagerSelector_FireImmediately_ThatThrows_DoesNotLeaveTheHandlerAttached()
    {
        var counter = new CounterStore();
        var manager = new BlexManager();
        manager.Register(counter);
        var calls = 0;

        Assert.ThrowsExactly<InvalidOperationException>(() => manager.Subscribe<int>(
            () => counter.Count,
            _ =>
            {
                calls++;
                throw new InvalidOperationException("callback boom");
            },
            fireImmediately: true));

        counter.Increment();
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void ManagerSelector_UsesTheSuppliedComparer()
    {
        var counter = new CounterStore();
        var manager = new BlexManager();
        manager.Register(counter);

        var seen = new List<int>();
        // "Only tell me when it crosses a multiple of 5."
        using var sub = manager.Subscribe(
            () => counter.Count,
            seen.Add,
            EqualityComparer<int>.Create((a, b) => a / 5 == b / 5));

        counter.Increment();  // 1 -- same bucket as 0
        counter.Add(4);       // 5 -- new bucket
        counter.Increment();  // 6 -- same bucket as 5

        Assert.AreSequenceEqual([5], seen);
    }

    [TestMethod]
    public void ManagerSelector_ThrowingCallback_IsIsolatedAndReported()
    {
        var counter = new CounterStore();
        var manager = new BlexManager();
        manager.Register(counter);
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        using var bad = manager.Subscribe<int>(() => counter.Count, _ => throw new InvalidOperationException("boom"));
        var seen = new List<int>();
        using var good = manager.Subscribe(() => counter.Count, seen.Add);

        counter.Increment(); // must not throw

        Assert.AreSequenceEqual([1], seen);
        Assert.Contains(e => e.Source == "subscriber", errors);
    }

    [TestMethod]
    public void ManagerSelector_Dispose_StopsObserving()
    {
        var counter = new CounterStore();
        var manager = new BlexManager();
        manager.Register(counter);

        var seen = new List<int>();
        var sub = manager.Subscribe(() => counter.Count, seen.Add);

        counter.Increment();
        sub.Dispose();
        counter.Increment();

        Assert.AreSequenceEqual([1], seen);
    }
}
