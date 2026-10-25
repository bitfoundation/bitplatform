using System;
using System.Collections.Generic;
using System.ComponentModel;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// A throwing <see cref="IBlexStore.StateChanged"/> / <see cref="INotifyPropertyChanged.PropertyChanged"/>
/// subscriber must not starve the other subscribers, nor the dispatch pipeline behind them.
/// </summary>
[TestClass]
public class NotificationIsolationTests
{
    [TestMethod]
    public void ThrowingSubscriber_DoesNotStarveOtherSubscribers()
    {
        var manager = new BlexManager();
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        var store = new CounterStore();
        manager.Register(store);

        var before = 0;
        var after = 0;
        store.StateChanged += () => before++;
        store.StateChanged += () => throw new InvalidOperationException("render boom");
        store.StateChanged += () => after++;

        store.Increment(); // must not throw

        Assert.AreEqual(1, before);
        Assert.AreEqual(1, after); // the subscriber registered *after* the thrower still ran
        Assert.AreEqual(1, store.Count);
        Assert.Contains(e => e.Source == "subscriber" && e.Detail == "counter", errors);
    }

    [TestMethod]
    public void ThrowingSubscriber_DoesNotBreakDispatchObservers()
    {
        var manager = new BlexManager();
        manager.OnError = _ => { };

        var store = new CounterStore();
        manager.Register(store);

        var recorded = new List<string>();
        using var sub = manager.Subscribe(ctx => recorded.Add(ctx.ActionName));
        store.StateChanged += () => throw new InvalidOperationException("render boom");

        store.Increment();
        store.Count = 5; // standalone "Set Count" path

        Assert.AreSequenceEqual(["Increment", "Set Count"], recorded);
    }

    [TestMethod]
    public void ThrowingPropertyChangedSubscriber_IsIsolated()
    {
        var manager = new BlexManager();
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        var store = new CounterStore();
        manager.Register(store);

        var seen = 0;
        ((INotifyPropertyChanged)store).PropertyChanged += (_, _) => throw new InvalidOperationException("binding boom");
        ((INotifyPropertyChanged)store).PropertyChanged += (_, _) => seen++;

        store.Increment(); // must not throw

        Assert.AreEqual(1, seen);
        Assert.Contains(e => e.Source == "subscriber", errors);
    }

    [TestMethod]
    public void ThrowingSubscriber_OnDetachedStore_DoesNotThrow()
    {
        // No manager attached: the failure has nowhere to be reported but must still be contained.
        var store = new CounterStore();
        var seen = 0;
        store.StateChanged += () => throw new InvalidOperationException("boom");
        store.StateChanged += () => seen++;

        store.Increment();

        Assert.AreEqual(1, seen);
        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public void ThrowingSelectorSubscription_IsIsolated()
    {
        var manager = new BlexManager();
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        var store = new CounterStore();
        manager.Register(store);

        using var bad = store.Subscribe(() => store.Count, _ => throw new InvalidOperationException("selector boom"));
        var observed = new List<int>();
        using var good = store.Subscribe(() => store.Count, observed.Add);

        store.Increment();
        store.Increment();

        Assert.AreSequenceEqual([1, 2], observed);
        Assert.AreEqual(2, errors.Count);
    }
}
