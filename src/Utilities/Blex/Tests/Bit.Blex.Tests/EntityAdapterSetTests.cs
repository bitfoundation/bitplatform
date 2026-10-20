using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// Covers the write operations that overwrite existing entities (<c>UpsertOne</c>/<c>UpsertMany</c>,
/// <c>SetOne</c>/<c>SetMany</c>, <c>SetAll</c>) and the reconciliation the state performs on an
/// untrusted payload. The unifying rule is the one every adapter operation obeys: an operation that
/// changes nothing must hand back the very same instance, because state fields compare by reference
/// and a fresh-but-equivalent instance would notify subscribers and record a time-travel action for
/// a mutation that never happened.
/// </summary>
[TestClass]
public class EntityAdapterSetTests
{
    private static readonly BlexEntityAdapter<Todo, int> Adapter = new(t => t.Id);

    private static readonly BlexEntityAdapter<Todo, int> SortedAdapter =
        new(t => t.Id, Comparer<Todo>.Create((a, b) => string.CompareOrdinal(a.Text, b.Text)));

    private static BlexEntityState<Todo, int> Seeded()
        => Adapter.SetAll(Adapter.GetInitialState(), [new Todo(1, "a", false), new Todo(2, "b", false)]);

    [TestMethod]
    public void UpsertOne_WithAnEqualEntity_ReturnsSameInstance()
    {
        var state = Seeded();

        Assert.AreSame(state, Adapter.UpsertOne(state, new Todo(1, "a", false)));   // equal record
        Assert.AreSame(state, Adapter.UpsertOne(state, state.Find(1)!));            // same instance
        Assert.AreNotSame(state, Adapter.UpsertOne(state, new Todo(1, "a", true))); // real change
        Assert.AreNotSame(state, Adapter.UpsertOne(state, new Todo(3, "c", false))); // new id
    }

    [TestMethod]
    public void UpsertMany_WithOnlyEqualEntities_ReturnsSameInstance()
    {
        var state = Seeded();

        Assert.AreSame(state, Adapter.UpsertMany(state, [new Todo(1, "a", false), new Todo(2, "b", false)]));

        // One real change among no-ops still produces a new state, and only that entity moves.
        var next = Adapter.UpsertMany(state, [new Todo(1, "a", false), new Todo(2, "b", true)]);
        Assert.AreNotSame(state, next);
        Assert.AreSame(state.Find(1), next.Find(1));
        Assert.IsTrue(next.Find(2)!.Done);
        Assert.AreSequenceEqual([1, 2], next.Ids);
    }

    [TestMethod]
    public void SetAll_WithAnEquivalentCollection_ReturnsSameInstance()
    {
        var state = Seeded();

        // Reseeding from an unchanged server response must not record an action.
        Assert.AreSame(state, Adapter.SetAll(state, [new Todo(1, "a", false), new Todo(2, "b", false)]));

        // Order is part of the identity: the same entities in a different order *is* a change.
        Assert.AreNotSame(state, Adapter.SetAll(state, [new Todo(2, "b", false), new Todo(1, "a", false)]));
        Assert.AreNotSame(state, Adapter.SetAll(state, [new Todo(1, "a", true), new Todo(2, "b", false)]));
        Assert.AreNotSame(state, Adapter.SetAll(state, [new Todo(1, "a", false)]));
    }

    [TestMethod]
    public void SetAll_ComparesAfterSorting_SoAnUnsortedButEquivalentInputIsANoOp()
    {
        var state = SortedAdapter.SetAll(
            SortedAdapter.GetInitialState(),
            [new Todo(1, "a", false), new Todo(2, "b", false)]);

        // The comparer puts these back into the same order, so nothing actually changes.
        Assert.AreSame(state, SortedAdapter.SetAll(state, [new Todo(2, "b", false), new Todo(1, "a", false)]));
    }

    [TestMethod]
    public void SetOne_OnlyReplacesEntitiesThatAlreadyExist()
    {
        var state = Seeded();

        Assert.AreSame(state, Adapter.SetOne(state, new Todo(99, "missing", false))); // unknown id
        Assert.AreSame(state, Adapter.SetOne(state, new Todo(1, "a", false)));        // equal

        var next = Adapter.SetOne(state, new Todo(1, "a", true));
        Assert.AreNotSame(state, next);
        Assert.AreSequenceEqual([1, 2], next.Ids); // order untouched
        Assert.IsTrue(next.Find(1)!.Done);
    }

    [TestMethod]
    public void SetMany_ReplacesKnownIdsAndIgnoresTheRest()
    {
        var state = Seeded();

        Assert.AreSame(state, Adapter.SetMany(state, []));
        Assert.AreSame(state, Adapter.SetMany(state, [new Todo(99, "missing", false)]));
        Assert.AreSame(state, Adapter.SetMany(state, [new Todo(1, "a", false)]));

        var next = Adapter.SetMany(state, [new Todo(1, "a", true), new Todo(99, "missing", true)]);
        Assert.AreNotSame(state, next);
        Assert.AreSequenceEqual([1, 2], next.Ids);
        Assert.IsTrue(next.Find(1)!.Done);
        Assert.IsFalse(next.Contains(99));
    }

    [TestMethod]
    public void SetOne_WithASortComparer_RepositionsTheReplacedEntity()
    {
        var state = SortedAdapter.SetAll(
            SortedAdapter.GetInitialState(),
            [new Todo(1, "a", false), new Todo(2, "b", false)]);
        Assert.AreSequenceEqual([1, 2], state.Ids);

        var next = SortedAdapter.SetOne(state, new Todo(1, "z", false));
        Assert.AreSequenceEqual([2, 1], next.Ids);
    }

    [TestMethod]
    public void NullArguments_ThrowArgumentNullException()
    {
        var state = Seeded();
        Assert.ThrowsExactly<System.ArgumentNullException>(() => Adapter.SetMany(state, null!));
        Assert.ThrowsExactly<System.ArgumentNullException>(() => Adapter.UpdateMany(state, null!, t => t));
        Assert.ThrowsExactly<System.ArgumentNullException>(() => Adapter.UpdateMany(state, [1], null!));
    }

    // UpdateMany applies its updates in one pass rather than delegating to UpdateOne per id (which
    // copied the whole entity map each time). These pin the semantics that pass has to preserve.

    [TestMethod]
    public void UpdateMany_AppliesEveryMatchAndIgnoresMissingIds()
    {
        var state = Seeded();

        var next = Adapter.UpdateMany(state, [1, 99, 2], t => t with { Done = true });

        Assert.AreSequenceEqual([1, 2], next.Ids);
        Assert.IsTrue(next.Find(1)!.Done);
        Assert.IsTrue(next.Find(2)!.Done);
    }

    [TestMethod]
    public void UpdateMany_WithOnlyNoOpUpdates_ReturnsSameInstance()
    {
        var state = Seeded();

        Assert.AreSame(state, Adapter.UpdateMany(state, [1, 2], t => t));
        Assert.AreSame(state, Adapter.UpdateMany(state, [99], t => t with { Done = true })); // no match
        Assert.AreSame(state, Adapter.UpdateMany(state, [], t => t with { Done = true }));
    }

    [TestMethod]
    public void UpdateMany_IsSequential_SoALaterIdSeesAnEarlierUpdate()
    {
        var state = Seeded();

        // Updating id 1 to id 2 overwrites the existing entity there; the later pass over id 2 then
        // sees the overwritten entity, not the original.
        var next = Adapter.UpdateMany(state, [1, 2], t => t.Id == 1 ? t with { Id = 2 } : t with { Text = t.Text + "!" });

        Assert.AreSequenceEqual([2], next.Ids);
        Assert.AreEqual("a!", next.Find(2)!.Text);
    }

    [TestMethod]
    public void UpdateMany_ChangingAnIdToAFreeOne_KeepsThePosition()
    {
        var state = Seeded();

        var next = Adapter.UpdateMany(state, [1], t => t with { Id = 7 });

        Assert.AreSequenceEqual([7, 2], next.Ids);
        Assert.IsFalse(next.Contains(1));
        Assert.AreEqual("a", next.Find(7)!.Text);
    }

    [TestMethod]
    public void UpdateMany_MatchesUpdateOneForTheSameWork()
    {
        var state = Adapter.SetAll(
            Adapter.GetInitialState(),
            [new Todo(1, "a", false), new Todo(2, "b", false), new Todo(3, "c", false)]);

        var viaMany = Adapter.UpdateMany(state, [1, 2, 3], t => t with { Done = !t.Done });

        var viaOne = state;
        foreach (var id in new[] { 1, 2, 3 })
            viaOne = Adapter.UpdateOne(viaOne, id, t => t with { Done = !t.Done });

        Assert.AreSequenceEqual(viaOne.Ids, viaMany.Ids);
        Assert.AreSequenceEqual(viaOne.All, viaMany.All);
    }

    [TestMethod]
    public void Map_TransformsEveryEntity_AndIsFreeWhenNothingChanges()
    {
        var state = Seeded();

        Assert.AreSame(state, Adapter.Map(state, t => t));

        var next = Adapter.Map(state, t => t with { Done = true });
        Assert.AreSequenceEqual([1, 2], next.Ids);
        foreach (var t in next.All)
        {
            Assert.IsTrue(t.Done);
        }
    }

    [TestMethod]
    public void UpdateMany_WithASortComparer_RepositionsEverythingOnce()
    {
        var state = SortedAdapter.SetAll(
            SortedAdapter.GetInitialState(),
            [new Todo(1, "a", false), new Todo(2, "b", false), new Todo(3, "c", false)]);
        Assert.AreSequenceEqual([1, 2, 3], state.Ids);

        // Reverse the sort keys: the result must be fully re-sorted, not partially.
        var next = SortedAdapter.UpdateMany(state, [1, 3], t => t with { Text = t.Text == "a" ? "z" : "0" });

        Assert.AreSequenceEqual([3, 2, 1], next.Ids);
    }

    [TestMethod]
    public void DanglingIdInAPayload_IsDropped_InsteadOfThrowingOnEnumeration()
    {
        // A hand-edited or partially-written payload can list an id the entity map has no entry
        // for. Enumerating it used to throw KeyNotFoundException deep inside a render.
        var node = JsonNode.Parse("""
            {"ids":[1,2,3],"entities":{"1":{"Id":1,"Text":"a","Done":false},"3":{"Id":3,"Text":"c","Done":false}}}
            """)!;

        var state = JsonSerializer.Deserialize<BlexEntityState<Todo, int>>(node, BlexJson.Options)!;

        Assert.AreSequenceEqual([1, 3], state.Ids);
        Assert.AreEqual(2, state.Count);
        Assert.AreSequenceEqual(["a", "c"], System.Linq.Enumerable.Select(state.All, t => t.Text));
    }

    [TestMethod]
    public void EntityMissingFromTheIdList_IsAppended_InsteadOfBecomingUnreachable()
    {
        var node = JsonNode.Parse("""
            {"ids":[1],"entities":{"1":{"Id":1,"Text":"a","Done":false},"2":{"Id":2,"Text":"b","Done":false}}}
            """)!;

        var state = JsonSerializer.Deserialize<BlexEntityState<Todo, int>>(node, BlexJson.Options)!;

        Assert.AreSequenceEqual([1, 2], state.Ids);
        Assert.AreEqual(2, state.Count);
        Assert.AreEqual("b", state.Find(2)!.Text);
    }

    [TestMethod]
    public void DuplicateIdsInAPayload_AreCollapsed()
    {
        var node = JsonNode.Parse("""
            {"ids":[1,1,2],"entities":{"1":{"Id":1,"Text":"a","Done":false},"2":{"Id":2,"Text":"b","Done":false}}}
            """)!;

        var state = JsonSerializer.Deserialize<BlexEntityState<Todo, int>>(node, BlexJson.Options)!;

        Assert.AreSequenceEqual([1, 2], state.Ids);
        Assert.AreEqual(2, state.Count);
    }

    [TestMethod]
    public void ThePublicConstructor_CopiesWhatItIsGiven()
    {
        // The caller still holds these. Aliasing them would let a later mutation change an
        // "immutable" state in place -- and reference-based change detection would never notice.
        var ids = new List<int> { 1, 2 };
        var entities = new Dictionary<int, Todo>
        {
            [1] = new(1, "a", false),
            [2] = new(2, "b", false),
        };

        var state = new BlexEntityState<Todo, int>(ids, entities);

        ids.Add(3);
        entities.Clear();

        Assert.AreSequenceEqual([1, 2], state.Ids);
        Assert.AreEqual(2, state.Count);
        Assert.AreEqual("a", state.Find(1)!.Text);
    }

    [TestMethod]
    public void AWellFormedPayload_RoundTripsUnchanged()
    {
        var state = Seeded();
        var json = JsonSerializer.SerializeToNode(state, BlexJson.Options)!;
        var restored = JsonSerializer.Deserialize<BlexEntityState<Todo, int>>(json, BlexJson.Options)!;

        Assert.AreSequenceEqual(state.Ids, restored.Ids);
        Assert.AreSequenceEqual(state.All, restored.All);
    }
}
