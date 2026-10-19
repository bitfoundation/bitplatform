using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Nodes;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// XAML binding support: every store raises <see cref="INotifyPropertyChanged.PropertyChanged"/>
/// with an empty property name ("all properties changed") whenever <c>StateChanged</c> fires.
/// </summary>
[TestClass]
public class StoreInpcTests
{
    private static List<string?> Track(BlexStoreBase store)
    {
        var raised = new List<string?>();
        ((INotifyPropertyChanged)store).PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        return raised;
    }

    [TestMethod]
    public void Store_ImplementsINotifyPropertyChanged()
    {
        Assert.IsInstanceOfType<INotifyPropertyChanged>(new CounterStore());
    }

    [TestMethod]
    public void Action_WithMultipleMutations_RaisesOnce_WithEmptyName()
    {
        var store = new CounterStore();
        var raised = Track(store);

        store.Reset(); // no-op values, but Count/Label assignments are equal -> nothing dirty
        Assert.IsEmpty(raised);

        store.Increment();
        Assert.AreSequenceEqual(new string?[] { string.Empty }, raised);

        raised.Clear();
        store.Add(2);
        store.Reset(); // mutates both Count and Label -> still a single batched raise
        Assert.AreSequenceEqual(new string?[] { string.Empty, string.Empty }, raised);
    }

    [TestMethod]
    public void StandaloneSet_Raises_AndEqualValueDoesNot()
    {
        var store = new CounterStore();
        var raised = Track(store);

        store.Count = 5;
        Assert.ContainsSingle(raised);

        store.Count = 5; // unchanged value -> no notification
        Assert.ContainsSingle(raised);
    }

    [TestMethod]
    public void Batch_RaisesOnce()
    {
        var store = new CounterStore();
        var raised = Track(store);

        store.Batch("Apply preset", () =>
        {
            store.Count = 10;
            store.Label = "preset";
        });

        Assert.ContainsSingle(raised);
    }

    [TestMethod]
    public void RestoreState_Raises()
    {
        var store = new CounterStore();
        var snapshot = (JsonObject)store.SerializeState().DeepClone();
        store.Increment();

        var raised = Track(store);
        store.RestoreState(snapshot);

        Assert.ContainsSingle(raised);
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void PropertyChanged_StillRaised_WhenStateChangedSubscriberThrows()
    {
        var manager = new BlexManager();
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        var store = new CounterStore();
        manager.Register(store);
        var raised = Track(store);
        store.StateChanged += () => throw new InvalidOperationException("boom");

        try
        {
            store.Increment();
        }
        catch (InvalidOperationException)
        {
            // Raw StateChanged subscribers are not isolated; the throw may propagate.
        }

        Assert.ContainsSingle(raised); // ...but XAML bindings still observed the applied mutation.
        Assert.AreEqual(1, store.Count);
    }
}
