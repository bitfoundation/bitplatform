using System;
using System.Collections.Generic;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// The manager's public events sit directly on the dispatch path and are observed by persistence
/// and undo/redo. A handler attached with a raw <c>+=</c> (or a throwing subscription filter) must
/// be contained just like one registered through <see cref="BlexManager.Subscribe"/>.
/// </summary>
[TestClass]
public class PipelineIsolationTests
{
    [TestMethod]
    public void ThrowingActionDispatchedHandler_DoesNotStarveLaterHandlers()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        var store = new CounterStore();
        manager.Register(store);

        var before = 0;
        var after = 0;
        manager.ActionDispatched += _ => before++;
        manager.ActionDispatched += _ => throw new InvalidOperationException("observer boom");
        manager.ActionDispatched += _ => after++;

        store.Increment(); // must not throw

        Assert.AreEqual(1, before);
        Assert.AreEqual(1, after);
        Assert.Contains(e => e.Source == "subscriber" && e.Detail == "counter/Increment", errors);
    }

    [TestMethod]
    public void ThrowingActionDispatchedHandler_DoesNotBreakPersistenceOrHistory()
    {
        var manager = new BlexManager { OnError = _ => { } };
        var store = new CounterStore();
        manager.Register(store);

        var history = new BlexHistory(manager);
        manager.ActionDispatched += _ => throw new InvalidOperationException("observer boom");
        history.Start(); // registered *after* the thrower

        store.Increment();

        Assert.IsTrue(history.CanUndo);
        history.Undo();
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void ThrowingSubscriptionFilter_IsIsolated()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        var store = new CounterStore();
        manager.Register(store);

        var observed = 0;
        using var bad = manager.Subscribe(_ => { }, _ => throw new InvalidOperationException("filter boom"));
        using var good = manager.Subscribe(_ => observed++);

        store.Increment(); // must not throw

        Assert.AreEqual(1, observed);
        Assert.Contains(e => e.Source == "subscriber", errors);
    }

    [TestMethod]
    public void ThrowingStateRestoredHandler_DoesNotStarveLaterHandlers()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        var store = new CounterStore();
        manager.Register(store);

        store.Increment();
        var snapshot = manager.CaptureGlobalState();

        var after = 0;
        manager.StateRestored += () => throw new InvalidOperationException("restore observer boom");
        manager.StateRestored += () => after++;

        manager.RestoreGlobalState(snapshot); // must not throw

        Assert.AreEqual(1, after);
        Assert.Contains(e => e.Source == "restore", errors);
    }

    [TestMethod]
    public void ThrowingHistoryChangedHandler_DoesNotStarveLaterHandlers()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        var store = new CounterStore();
        manager.Register(store);

        using var history = new BlexHistory(manager);
        history.Start();

        var after = 0;
        history.Changed += () => throw new InvalidOperationException("ui boom");
        history.Changed += () => after++;

        store.Increment(); // Changed raised from inside the dispatch pipeline
        Assert.AreEqual(1, after);

        history.Undo(); // ...and from a direct call
        Assert.AreEqual(2, after);
        Assert.Contains(e => e.Source == "history", errors);
    }
}
