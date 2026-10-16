using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// State fields compare by reference, so an adapter operation that changes nothing must hand back
/// the very same <see cref="BlexEntityState{TEntity, TKey}"/> instance -- otherwise it raises a change
/// notification and records a time-travel action for a mutation that never happened.
/// </summary>
[TestClass]
public class EntityNoOpTests
{
    private static readonly BlexEntityAdapter<Todo, int> Adapter = new(t => t.Id);

    private static BlexEntityState<Todo, int> Seeded()
        => Adapter.SetAll(Adapter.GetInitialState(), [new Todo(1, "a", false), new Todo(2, "b", false)]);

    [TestMethod]
    public void AddMany_WithNoNewEntities_ReturnsSameInstance()
    {
        var state = Seeded();
        Assert.AreSame(state, Adapter.AddMany(state, []));
        Assert.AreSame(state, Adapter.AddMany(state, [new Todo(1, "dupe", true)]));
    }

    [TestMethod]
    public void UpsertMany_WithNoEntities_ReturnsSameInstance()
    {
        var state = Seeded();
        Assert.AreSame(state, Adapter.UpsertMany(state, []));
        Assert.AreNotSame(state, Adapter.UpsertMany(state, [new Todo(1, "changed", true)]));
    }

    [TestMethod]
    public void RemoveMany_WithNoMatchingIds_ReturnsSameInstance()
    {
        var state = Seeded();
        Assert.AreSame(state, Adapter.RemoveMany(state, []));
        Assert.AreSame(state, Adapter.RemoveMany(state, [42, 43]));

        var removed = Adapter.RemoveMany(state, [1, 99]);
        Assert.AreNotSame(state, removed);
        Assert.AreSequenceEqual([2], removed.Ids);
    }

    [TestMethod]
    public void RemoveAll_OnEmptyState_ReturnsSameInstance()
    {
        var empty = Adapter.GetInitialState();
        Assert.AreSame(empty, Adapter.RemoveAll(empty));
        Assert.AreNotSame(Seeded(), Adapter.RemoveAll(Seeded()));
    }

    [TestMethod]
    public void SetAll_EmptyOnEmpty_ReturnsSameInstance()
    {
        var empty = Adapter.GetInitialState();
        Assert.AreSame(empty, Adapter.SetAll(empty, []));
    }

    [TestMethod]
    public void UpdateOne_ProducingAnEqualEntity_ReturnsSameInstance()
    {
        var state = Seeded();

        Assert.AreSame(state, Adapter.UpdateOne(state, 1, t => t));                       // same instance
        Assert.AreSame(state, Adapter.UpdateOne(state, 1, t => t with { Text = "a" }));   // equal record
        Assert.AreNotSame(state, Adapter.UpdateOne(state, 1, t => t with { Done = true }));
    }

    [TestMethod]
    public void Map_WithAnIdentityTransform_ReturnsSameInstance()
    {
        var state = Seeded();
        Assert.AreSame(state, Adapter.Map(state, t => t));
        Assert.AreNotSame(state, Adapter.Map(state, t => t with { Done = true }));
    }

    [TestMethod]
    public void NoOpAdapterCall_InsideAnAction_RecordsNothingAndDoesNotNotify()
    {
        var store = new EntityTodoStore();
        var manager = new BlexManager();
        manager.Register(store);
        var recorded = new List<string>();
        using var sub = manager.Subscribe(ctx => recorded.Add(ctx.ActionName));

        store.Add(new Todo(1, "a", false));
        recorded.Clear();

        var notifications = 0;
        store.StateChanged += () => notifications++;

        store.Remove(99);           // no such id
        store.Toggle(99);           // no such id

        Assert.IsEmpty(recorded);
        Assert.AreEqual(0, notifications);
    }

    [TestMethod]
    public void NullArguments_ThrowArgumentNullException()
    {
        var state = Seeded();
        Assert.ThrowsExactly<ArgumentNullException>(() => Adapter.AddMany(state, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => Adapter.UpsertMany(state, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => Adapter.RemoveMany(state, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => Adapter.SetAll(state, null!));
    }

    [TestMethod]
    public void DeserializingAHalfWrittenPayload_DegradesToEmpty_InsteadOfThrowing()
    {
        // A persisted payload whose halves are missing/null must not produce an instance that
        // throws the moment a component enumerates it.
        var node = JsonNode.Parse("""{"ids":null,"entities":null}""")!;
        var state = JsonSerializer.Deserialize<BlexEntityState<Todo, int>>(node, BlexJson.Options)!;

        Assert.AreEqual(0, state.Count);
        Assert.IsEmpty(state.All);
        Assert.IsFalse(state.Contains(1));
        Assert.IsNull(state.Find(1));
    }
}
