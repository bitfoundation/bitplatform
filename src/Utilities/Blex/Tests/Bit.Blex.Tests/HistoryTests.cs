using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class HistoryTests
{
    private static (BlexManager, CounterStore, BlexHistory) Setup(int max = 100)
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager, max);
        history.Start();
        return (manager, counter, history);
    }

    [TestMethod]
    public void Undo_RevertsToPreviousState()
    {
        var (_, counter, history) = Setup();

        counter.Increment(); // 1
        counter.Increment(); // 2
        Assert.AreEqual(2, counter.Count);

        history.Undo();
        Assert.AreEqual(1, counter.Count);

        history.Undo();
        Assert.AreEqual(0, counter.Count);
    }

    [TestMethod]
    public void Redo_ReappliesUndoneState()
    {
        var (_, counter, history) = Setup();
        counter.Increment(); // 1
        counter.Increment(); // 2

        history.Undo(); // 1
        history.Undo(); // 0
        Assert.AreEqual(0, counter.Count);

        history.Redo(); // 1
        Assert.AreEqual(1, counter.Count);
        history.Redo(); // 2
        Assert.AreEqual(2, counter.Count);
    }

    [TestMethod]
    public void CanUndo_CanRedo_ReflectAvailability()
    {
        var (_, counter, history) = Setup();
        Assert.IsFalse(history.CanUndo);
        Assert.IsFalse(history.CanRedo);

        counter.Increment();
        Assert.IsTrue(history.CanUndo);
        Assert.IsFalse(history.CanRedo);

        history.Undo();
        Assert.IsFalse(history.CanUndo);
        Assert.IsTrue(history.CanRedo);
    }

    [TestMethod]
    public void NewAction_ClearsRedoStack()
    {
        var (_, counter, history) = Setup();
        counter.Increment(); // 1
        counter.Increment(); // 2
        history.Undo();      // 1, redo has [2]
        Assert.IsTrue(history.CanRedo);

        counter.Add(10);     // new branch -> redo cleared
        Assert.IsFalse(history.CanRedo);
        Assert.AreEqual(11, counter.Count);
    }

    [TestMethod]
    public void Undo_DoesNotRecordNewActions()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager);
        history.Start();

        counter.Increment();
        counter.Increment();
        history.Undo(); // restore should not create history entries

        // After one undo we are at count 1, with redo available and one undo left.
        Assert.IsTrue(history.CanRedo);
        Assert.IsTrue(history.CanUndo);
        Assert.AreEqual(1, counter.Count);
    }

    [TestMethod]
    public void UndoRedoLabels_AndCounts_TrackActions()
    {
        var (_, store, history) = Setup();

        Assert.IsNull(history.NextUndoLabel);
        Assert.AreEqual(0, history.UndoCount);

        store.Increment();
        store.Add(5);

        Assert.AreEqual(2, history.UndoCount);
        Assert.AreEqual("counter/Add", history.NextUndoLabel);
        Assert.IsNull(history.NextRedoLabel);

        history.Undo();
        Assert.AreEqual("counter/Increment", history.NextUndoLabel);
        Assert.AreEqual("counter/Add", history.NextRedoLabel);
        Assert.AreEqual(1, history.RedoCount);

        history.Redo();
        Assert.AreEqual("counter/Add", history.NextUndoLabel);
        Assert.IsNull(history.NextRedoLabel);
    }

    [TestMethod]
    public void ThrowingChangedHandler_DoesNotBreakObserversRegisteredAfterHistory()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        var history = new BlexHistory(manager);
        history.Start(); // subscribes to ActionDispatched first
        history.Changed += () => throw new System.InvalidOperationException("ui boom");

        var errors = new System.Collections.Generic.List<BlexError>();
        manager.OnError = errors.Add;

        // A later observer (like the persistor) must still be notified.
        var observed = 0;
        using var sub = manager.Subscribe(_ => observed++);

        counter.Increment(); // must not throw

        Assert.AreEqual(1, observed);
        Assert.Contains(e => e.Source == "history", errors);
        Assert.AreEqual(1, counter.Count);
    }

    [TestMethod]
    public void MaxEntries_CapsUndoDepth()
    {
        var (_, counter, history) = Setup(max: 2);

        counter.Increment(); // 1
        counter.Increment(); // 2
        counter.Increment(); // 3 (oldest baseline dropped)

        history.Undo(); // 2
        history.Undo(); // 1
        Assert.AreEqual(1, counter.Count);
        Assert.IsFalse(history.CanUndo); // only 2 entries retained
    }

    [TestMethod]
    public void Changed_FiresOnRecordAndUndo()
    {
        var (_, counter, history) = Setup();
        var fires = 0;
        history.Changed += () => fires++;

        counter.Increment();
        Assert.AreEqual(1, fires);

        history.Undo();
        Assert.AreEqual(2, fires);
    }
}
