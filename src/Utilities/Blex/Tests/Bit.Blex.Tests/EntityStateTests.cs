using System.Collections.Generic;
using System.Linq;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class EntityStateTests
{
    [TestMethod]
    public void TryGet_DistinguishesAbsentFromDefault()
    {
        var adapter = new BlexEntityAdapter<int, int>(v => v);
        var state = adapter.SetAll(adapter.GetInitialState(), [0, 5]);

        Assert.IsTrue(state.TryGet(0, out var zero));
        Assert.AreEqual(0, zero);

        // Find cannot tell these apart when default is a legitimate value.
        Assert.IsFalse(state.TryGet(9, out var missing));
        Assert.AreEqual(0, missing);
        Assert.AreEqual(0, state.Find(9));
    }

    [TestMethod]
    public void SelectId_ExposesTheAdaptersOwnKeySelector()
    {
        var adapter = new BlexEntityAdapter<Todo, int>(t => t.Id);
        Assert.AreEqual(7, adapter.SelectId(new Todo(7, "write", false)));
        Assert.IsNull(adapter.SortComparer);
    }

    [TestMethod]
    public void SortComparer_IsExposedWhenConfigured()
    {
        var comparer = Comparer<Todo>.Create((a, b) => string.CompareOrdinal(a.Text, b.Text));
        var adapter = new BlexEntityAdapter<Todo, int>(t => t.Id, comparer);
        Assert.AreSame(comparer, adapter.SortComparer);
    }

    private static readonly BlexEntityAdapter<Todo, int> Adapter = new(t => t.Id);

    [TestMethod]
    public void AddOne_AppendsEntity()
    {
        var state = Adapter.GetInitialState();
        state = Adapter.AddOne(state, new Todo(1, "a", false));
        state = Adapter.AddOne(state, new Todo(2, "b", false));

        Assert.AreEqual(2, state.Count);
        Assert.AreSequenceEqual([1, 2], state.Ids);
        Assert.AreEqual("a", state.Find(1)!.Text);
    }

    [TestMethod]
    public void AddOne_IgnoresDuplicateId()
    {
        var state = Adapter.GetInitialState();
        state = Adapter.AddOne(state, new Todo(1, "a", false));
        state = Adapter.AddOne(state, new Todo(1, "dupe", false));

        Assert.AreEqual(1, state.Count);
        Assert.AreEqual("a", state.Find(1)!.Text); // original kept
    }

    [TestMethod]
    public void UpsertOne_AddsOrReplaces()
    {
        var state = Adapter.GetInitialState();
        state = Adapter.UpsertOne(state, new Todo(1, "a", false));
        state = Adapter.UpsertOne(state, new Todo(1, "updated", true));

        Assert.AreEqual(1, state.Count);
        Assert.AreEqual("updated", state.Find(1)!.Text);
        Assert.IsTrue(state.Find(1)!.Done);
    }

    [TestMethod]
    public void UpdateOne_MutatesViaUpdater()
    {
        var state = Adapter.GetInitialState();
        state = Adapter.AddOne(state, new Todo(1, "a", false));
        state = Adapter.UpdateOne(state, 1, t => t with { Done = true });

        Assert.IsTrue(state.Find(1)!.Done);
    }

    [TestMethod]
    public void RemoveOne_RemovesEntityAndId()
    {
        var state = Adapter.GetInitialState();
        state = Adapter.AddMany(state, new[] { new Todo(1, "a", false), new Todo(2, "b", false) });
        state = Adapter.RemoveOne(state, 1);

        Assert.AreEqual(1, state.Count);
        Assert.IsFalse(state.Contains(1));
        Assert.AreSequenceEqual([2], state.Ids);
    }

    [TestMethod]
    public void SetAll_ReplacesEverything()
    {
        var state = Adapter.GetInitialState();
        state = Adapter.AddOne(state, new Todo(99, "old", false));
        state = Adapter.SetAll(state, new[] { new Todo(1, "a", false), new Todo(2, "b", false) });

        Assert.AreSequenceEqual([1, 2], state.Ids);
    }

    [TestMethod]
    public void UpdateMany_UpdatesAllMatchedIds()
    {
        var adapter = new BlexEntityAdapter<Todo, int>(t => t.Id);
        var state = adapter.SetAll(adapter.GetInitialState(),
            [new Todo(1, "a", false), new Todo(2, "b", false), new Todo(3, "c", false)]);

        state = adapter.UpdateMany(state, [1, 3, 99], t => t with { Done = true });

        Assert.IsTrue(state.Find(1)!.Done);
        Assert.IsFalse(state.Find(2)!.Done);
        Assert.IsTrue(state.Find(3)!.Done);
        Assert.AreEqual(3, state.Count); // unknown id ignored
    }

    [TestMethod]
    public void Map_TransformsEveryEntity()
    {
        var adapter = new BlexEntityAdapter<Todo, int>(t => t.Id);
        var state = adapter.SetAll(adapter.GetInitialState(),
            [new Todo(1, "a", false), new Todo(2, "b", true)]);

        state = adapter.Map(state, t => t with { Text = t.Text.ToUpperInvariant() });

        Assert.AreEqual("A", state.Find(1)!.Text);
        Assert.AreEqual("B", state.Find(2)!.Text);
    }

    [TestMethod]
    public void SortComparer_KeepsIdsSorted_AcrossOperations()
    {
        var adapter = new BlexEntityAdapter<Todo, int>(
            t => t.Id,
            Comparer<Todo>.Create((a, b) => string.CompareOrdinal(a.Text, b.Text)));

        var state = adapter.GetInitialState();
        state = adapter.AddOne(state, new Todo(1, "zebra", false));
        state = adapter.AddOne(state, new Todo(2, "apple", false));
        state = adapter.AddOne(state, new Todo(3, "mango", false));

        Assert.AreSequenceEqual([2, 3, 1], state.Ids); // apple, mango, zebra

        // Renaming an entity re-positions it.
        state = adapter.UpdateOne(state, 2, t => t with { Text = "watermelon" });
        Assert.AreSequenceEqual([3, 2, 1], state.Ids); // mango, watermelon, zebra
    }

    [TestMethod]
    public void UpdateOne_IdChangeOntoExistingId_MergesWithoutDuplicatingIds()
    {
        var state = Adapter.SetAll(Adapter.GetInitialState(),
            [new Todo(1, "one", false), new Todo(2, "two", false)]);

        // Rekey entity 1 to id 2: it must overwrite entity 2 and drop the stale slot.
        state = Adapter.UpdateOne(state, 1, t => t with { Id = 2 });

        Assert.AreSequenceEqual([2], state.Ids);
        Assert.AreEqual(1, state.Count);
        Assert.AreEqual("one", state.Find(2)!.Text);
    }

    [TestMethod]
    public void Mutations_ReturnNewInstances()
    {
        var state = Adapter.GetInitialState();
        var next = Adapter.AddOne(state, new Todo(1, "a", false));
        Assert.AreNotSame(state, next); // immutability => change detection works
    }

    [TestMethod]
    public void EntityStore_IntegratesWithActionsAndComputed()
    {
        var store = new EntityTodoStore();
        store.Add(new Todo(1, "a", false));
        store.Add(new Todo(2, "b", false));
        Assert.AreEqual(2, store.Remaining);

        store.Toggle(1);
        Assert.AreEqual(1, store.Remaining);

        store.Remove(2);
        Assert.AreEqual(1, store.Todos.Count);
    }

    [TestMethod]
    public void EntityStore_SerializesAndRoundTrips()
    {
        var store = new EntityTodoStore();
        store.Add(new Todo(1, "a", false));
        store.Add(new Todo(2, "b", true));

        var json = store.SerializeState();
        var restored = new EntityTodoStore();
        restored.DeserializeState(json);

        Assert.AreEqual(2, restored.Todos.Count);
        Assert.AreSequenceEqual([1, 2], restored.Todos.Ids);
        Assert.IsTrue(restored.Todos.Find(2)!.Done);
    }
}
