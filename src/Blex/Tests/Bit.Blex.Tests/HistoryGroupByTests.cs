using System;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// <c>groupBy</c> folds consecutive related actions into one undo step (redux-undo's
/// <c>groupBy</c>), where a filter would drop them from the stack altogether.
/// </summary>
[TestClass]
public class HistoryGroupByTests
{
    private static (CounterStore Store, BlexHistory History) Setup(Func<BlexActionContext, string?> groupBy)
    {
        var manager = new BlexManager();
        var store = new CounterStore();
        manager.Register(store);
        var history = new BlexHistory(manager, groupBy: groupBy);
        history.Start();
        return (store, history);
    }

    [TestMethod]
    public void ConsecutiveActionsWithTheSameKey_CollapseIntoOneStep()
    {
        var (store, history) = Setup(ctx => ctx.ActionName.StartsWith("Set ", StringComparison.Ordinal) ? ctx.QualifiedName : null);

        store.Count = 1;
        store.Count = 2;
        store.Count = 3;

        Assert.AreEqual(1, history.UndoCount);

        history.Undo();

        // One step reverts the whole burst, back to before the first keystroke.
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void ADifferentKey_StartsANewStep()
    {
        var (store, history) = Setup(ctx => ctx.ActionName.StartsWith("Set ", StringComparison.Ordinal) ? ctx.QualifiedName : null);

        store.Count = 1;
        store.Count = 2;
        store.Label = "typing";
        store.Label = "typed";

        Assert.AreEqual(2, history.UndoCount);

        history.Undo();
        Assert.AreEqual("idle", store.Label);
        Assert.AreEqual(2, store.Count);

        history.Undo();
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void ANullKey_GivesTheActionItsOwnStep()
    {
        var (store, history) = Setup(_ => null);

        store.Increment();
        store.Increment();

        Assert.AreEqual(2, history.UndoCount);
    }

    [TestMethod]
    public void AnUngroupedActionInBetween_BreaksTheGroup()
    {
        var (store, history) = Setup(ctx => ctx.ActionName.StartsWith("Set ", StringComparison.Ordinal) ? ctx.QualifiedName : null);

        store.Count = 1;
        store.Count = 2;
        store.Increment();   // its own step, and it ends the open group
        store.Count = 9;
        store.Count = 10;

        Assert.AreEqual(3, history.UndoCount);
    }

    [TestMethod]
    public void UndoEndsTheOpenGroup()
    {
        var (store, history) = Setup(ctx => ctx.ActionName.StartsWith("Set ", StringComparison.Ordinal) ? ctx.QualifiedName : null);

        store.Count = 1;
        store.Count = 2;
        history.Undo();

        // Without resetting the group key, this assignment would silently join the step the user
        // has already stepped away from.
        store.Count = 7;

        Assert.AreEqual(1, history.UndoCount);
        history.Undo();
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void ACheckpointEndsTheOpenGroup()
    {
        var (store, history) = Setup(ctx => ctx.ActionName.StartsWith("Set ", StringComparison.Ordinal) ? ctx.QualifiedName : null);

        store.Count = 1;
        store.Count = 2;
        history.Checkpoint("Edit count");
        store.Count = 3;

        Assert.AreEqual(2, history.UndoCount);
    }

    [TestMethod]
    public void AThrowingGroupSelector_FailsOpenAndIsReported()
    {
        var manager = new BlexManager();
        var store = new CounterStore();
        manager.Register(store);

        var errors = new System.Collections.Generic.List<BlexError>();
        manager.OnError = errors.Add;

        var history = new BlexHistory(manager, groupBy: _ => throw new InvalidOperationException("group-boom"));
        history.Start();

        store.Count = 1;
        store.Count = 2;

        Assert.AreEqual(2, history.UndoCount);   // each action kept its own step
        Assert.AreEqual(2, errors.Count);
        foreach (var e in errors)
        {
            Assert.AreEqual("history", e.Source);
        }
    }
}
