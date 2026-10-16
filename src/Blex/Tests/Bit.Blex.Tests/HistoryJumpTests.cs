using System;
using System.Linq;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// Multi-step history navigation (redux-undo's <c>jump</c>/<c>jumpToPast</c>/<c>jumpToFuture</c>)
/// and the labels a history panel renders.
/// </summary>
[TestClass]
public class HistoryJumpTests
{
    private static (BlexManager Manager, CounterStore Store, BlexHistory History) Setup()
    {
        var manager = new BlexManager();
        var store = new CounterStore();
        manager.Register(store);
        var history = new BlexHistory(manager);
        history.Start();
        return (manager, store, history);
    }

    [TestMethod]
    public void Jump_MovesSeveralStepsBackAtOnce()
    {
        var (_, store, history) = Setup();
        store.Increment();
        store.Increment();
        store.Increment();
        Assert.AreEqual(3, store.Count);

        history.Jump(-2);

        Assert.AreEqual(1, store.Count);
        Assert.AreEqual(1, history.UndoCount);
        Assert.AreEqual(2, history.RedoCount);
    }

    [TestMethod]
    public void Jump_AppliesOnlyTheDestination()
    {
        var (_, store, history) = Setup();
        store.Increment();
        store.Increment();
        store.Increment();

        var renders = 0;
        store.StateChanged += () => renders++;
        history.Jump(-3);

        Assert.AreEqual(0, store.Count);
        Assert.AreEqual(1, renders);   // one restore, not three
    }

    [TestMethod]
    public void Jump_ForwardRedoesSeveralSteps()
    {
        var (_, store, history) = Setup();
        store.Increment();
        store.Increment();
        store.Increment();
        history.Jump(-3);

        history.Jump(2);

        Assert.AreEqual(2, store.Count);
    }

    [TestMethod]
    public void Jump_IsClampedToWhatTheStacksHold()
    {
        var (_, store, history) = Setup();
        store.Increment();

        history.Jump(-99);
        Assert.AreEqual(0, store.Count);
        Assert.IsFalse(history.CanUndo);

        history.Jump(99);
        Assert.AreEqual(1, store.Count);
        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void JumpOfZero_OrOnEmptyStacks_DoesNothing()
    {
        var (_, store, history) = Setup();
        store.Increment();

        var changes = 0;
        history.Changed += () => changes++;

        history.Jump(0);
        history.Jump(5);   // nothing to redo

        Assert.AreEqual(0, changes);
        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public void JumpToPast_IndexesTheOldestStateFirst()
    {
        var (_, store, history) = Setup();
        store.Increment();   // 1
        store.Increment();   // 2
        store.Increment();   // 3

        history.JumpToPast(0);   // the oldest retained state

        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void JumpToFuture_IndexesTheNearestRedoFirst()
    {
        var (_, store, history) = Setup();
        store.Increment();
        store.Increment();
        store.Increment();
        history.JumpToPast(0);

        history.JumpToFuture(0);
        Assert.AreEqual(1, store.Count);

        history.JumpToFuture(1);
        Assert.AreEqual(3, store.Count);
    }

    [TestMethod]
    public void Labels_NameTheStepsInNavigationOrder()
    {
        var (_, store, history) = Setup();
        store.Increment();
        store.Add(5);
        store.Reset();

        Assert.AreSequenceEqual(["counter/Reset", "counter/Add", "counter/Increment"], history.UndoLabels);
        Assert.IsEmpty(history.RedoLabels);
        Assert.AreEqual("counter/Reset", history.NextUndoLabel);

        history.Undo();

        Assert.AreSequenceEqual(["counter/Add", "counter/Increment"], history.UndoLabels);
        Assert.AreSequenceEqual(["counter/Reset"], history.RedoLabels);
    }

    [TestMethod]
    public void Labels_AreEmptyBeforeAnythingHappens()
    {
        var (_, _, history) = Setup();
        Assert.IsEmpty(history.UndoLabels);
        Assert.IsEmpty(history.RedoLabels);
    }

    [TestMethod]
    public void Labels_AreCachedBetweenChanges()
    {
        var (_, store, history) = Setup();
        store.Increment();

        // Rendering a history panel reads these once per item; rebuilding on every access would
        // make the natural loop quadratic.
        var labels = history.UndoLabels;
        Assert.AreSame(labels, history.UndoLabels);

        var before = history.UndoLabels;
        store.Increment();
        Assert.AreNotSame(before, history.UndoLabels);
        Assert.AreEqual(2, history.UndoLabels.Count);
    }

    [TestMethod]
    public void UndoLabels_MatchTheJumpToPastIndexTheyDocument()
    {
        var (_, store, history) = Setup();
        store.Increment();
        store.Add(5);
        store.Reset();

        // Item i is what JumpToPast(UndoCount - 1 - i) reverts: picking the last item must land on
        // the state before the very first action.
        var labels = history.UndoLabels;
        history.JumpToPast(history.UndoCount - 1 - (labels.Count - 1));

        Assert.AreEqual(0, store.Count);
        Assert.AreEqual("idle", store.Label);
    }
}
