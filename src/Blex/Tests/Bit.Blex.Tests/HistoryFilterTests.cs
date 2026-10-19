using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// The history filter (redux-undo's <c>filter</c>) decides which actions get their own undo step.
/// A filtered action still applies and still moves the present forward -- it simply does not become
/// a separate step, so the next <see cref="BlexHistory.Undo"/> reverts it together with the recorded
/// action before it. That distinction is what keeps keystroke-level noise out of an undo stack the
/// user thinks about in whole edits.
/// </summary>
[TestClass]
public class HistoryFilterTests
{
    private static (BlexManager Manager, CounterStore Counter, BlexHistory History) Setup(
        Func<BlexActionContext, bool>? filter)
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager, 100, filter);
        history.Start();
        return (manager, counter, history);
    }

    [TestMethod]
    public void FilteredActions_DoNotBecomeTheirOwnUndoStep()
    {
        // "Set X" is what a direct property assignment records -- typically per keystroke.
        var (_, counter, history) = Setup(ctx => !ctx.ActionName.StartsWith("Set ", StringComparison.Ordinal));

        counter.Increment(); // recorded  -> Count 1
        counter.Count = 5;   // filtered  -> Count 5
        counter.Count = 9;   // filtered  -> Count 9

        Assert.AreEqual(1, history.UndoCount);

        history.Undo();
        Assert.AreEqual(0, counter.Count); // back past the filtered writes *and* the increment
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void FilteredActions_StillAdvanceThePresent_SoUndoNeverRestoresAStateTheUserNeverSaw()
    {
        var (_, counter, history) = Setup(ctx => ctx.ActionName != "Add");

        counter.Increment(); // recorded -> 1
        counter.Add(10);     // filtered -> 11
        counter.Increment(); // recorded -> 12

        Assert.AreEqual(12, counter.Count);

        // The step pushed for the last increment must be the *post-filter* state (11), not the
        // stale pre-filter one (1).
        history.Undo();
        Assert.AreEqual(11, counter.Count);
    }

    [TestMethod]
    public void FilteredAction_KeepsTheLabelOfTheStepUndoActuallyReturnsTo()
    {
        var (_, counter, history) = Setup(ctx => ctx.ActionName != "Add");

        counter.Increment();
        Assert.AreEqual("counter/Increment", history.NextUndoLabel);

        counter.Add(3); // filtered: no new step, so the boundary is still the increment
        Assert.AreEqual("counter/Increment", history.NextUndoLabel);
    }

    [TestMethod]
    public void FilteredAction_StillInvalidatesRedo()
    {
        var (_, counter, history) = Setup(ctx => ctx.ActionName != "Add");

        counter.Increment();
        history.Undo();
        Assert.IsTrue(history.CanRedo);

        counter.Add(2); // the state moved forward again; the redo branch is gone
        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void FilteringEverything_LeavesNothingToUndo_ButStateStillChanges()
    {
        var (_, counter, history) = Setup(_ => false);

        counter.Increment();
        counter.Increment();

        Assert.AreEqual(2, counter.Count);
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void AThrowingFilter_FailsOpenAndIsReported()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        var history = new BlexHistory(manager, 100, _ => throw new InvalidOperationException("filter boom"));
        history.Start();

        counter.Increment(); // must not throw

        // Losing an undo step silently is worse than keeping an extra one, so the action is
        // recorded anyway and the failure is surfaced.
        Assert.IsTrue(history.CanUndo);
        Assert.Contains(e => e.Source == "history", errors);
    }

    [TestMethod]
    public void WithoutAFilter_EveryActionIsRecorded()
    {
        var (_, counter, history) = Setup(filter: null);

        counter.Increment();
        counter.Count = 7;

        Assert.AreEqual(2, history.UndoCount);
    }

    [TestMethod]
    public void Checkpoint_TurnsFilteredWorkIntoItsOwnUndoStep()
    {
        // Without a checkpoint, the whole "typed edit" would be swallowed by the next recorded
        // action, so undo would discard the edit and that action together.
        var (_, counter, history) = Setup(ctx => !ctx.ActionName.StartsWith("Set ", StringComparison.Ordinal));

        counter.Count = 5;   // filtered
        counter.Count = 9;   // filtered
        history.Checkpoint("Edit");
        counter.Increment(); // recorded -> 10

        Assert.AreEqual(2, history.UndoCount);

        history.Undo();
        Assert.AreEqual(9, counter.Count); // back to the checkpoint, not past the whole edit

        history.Undo();
        Assert.AreEqual(0, counter.Count);
    }

    [TestMethod]
    public void Checkpoint_LabelsTheBoundaryItCreates()
    {
        var (_, counter, history) = Setup(_ => false);

        counter.Count = 5;
        history.Checkpoint("Edit");

        Assert.AreEqual("Edit", history.NextUndoLabel);
        Assert.IsTrue(history.CanUndo);
    }

    [TestMethod]
    public void Checkpoint_DefaultsItsLabel()
    {
        var (_, counter, history) = Setup(_ => false);

        counter.Count = 5;
        history.Checkpoint();

        Assert.AreEqual("Checkpoint", history.NextUndoLabel);
    }

    [TestMethod]
    public void Checkpoint_OnUnchangedState_AddsNothing()
    {
        // A debounced "the user stopped typing" timer fires whether or not anything was typed;
        // a checkpoint there must not create an undo step that reverts to its own state.
        var (_, counter, history) = Setup(_ => false);

        history.Checkpoint();
        Assert.IsFalse(history.CanUndo);

        counter.Count = 5;
        history.Checkpoint();
        history.Checkpoint();
        history.Checkpoint();

        Assert.AreEqual(1, history.UndoCount);
    }

    [TestMethod]
    public void Checkpoint_InvalidatesRedo()
    {
        var (_, counter, history) = Setup(filter: null);

        counter.Increment();
        history.Undo();
        Assert.IsTrue(history.CanRedo);

        counter.Count = 7;
        history.Checkpoint();

        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void Checkpoint_RaisesChanged()
    {
        var (_, counter, history) = Setup(_ => false);
        counter.Count = 5;

        // Subscribed after the mutation: a filtered action still raises Changed (its state moved),
        // so this isolates what the checkpoint itself reports.
        var fires = 0;
        history.Changed += () => fires++;

        history.Checkpoint();
        Assert.AreEqual(1, fires);

        history.Checkpoint(); // no-op, so nothing to report
        Assert.AreEqual(1, fires);
    }

    [TestMethod]
    public void Checkpoint_RespectsMaxEntries()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager, 2, _ => false);
        history.Start();

        counter.Count = 1;
        history.Checkpoint();
        counter.Count = 2;
        history.Checkpoint();
        counter.Count = 3;
        history.Checkpoint();

        Assert.AreEqual(2, history.UndoCount);
    }

    [TestMethod]
    public void Checkpoint_BeforeStart_StillEstablishesABaseline()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager, 100, _ => false);

        counter.Count = 5;
        history.Checkpoint(); // no present yet: nothing to push, but the baseline is taken
        counter.Count = 9;
        history.Checkpoint();

        history.Undo();
        Assert.AreEqual(5, counter.Count);
    }
}
