using System;
using System.Collections.Generic;
using System.Linq;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// Collection comparers for selector subscriptions -- the zustand <c>shallow</c> equivalent that
/// keeps a projection returning a fresh list from firing on every action.
/// </summary>
[TestClass]
public class ComparerTests
{
    [TestMethod]
    public void Sequence_ComparesContentsInOrder()
    {
        var comparer = BlexComparers.Sequence<int>();

        Assert.IsTrue(comparer.Equals(new List<int> { 1, 2, 3 }, new[] { 1, 2, 3 }));
        Assert.IsFalse(comparer.Equals(new[] { 1, 2, 3 }, new[] { 3, 2, 1 }));
        Assert.IsFalse(comparer.Equals(new[] { 1, 2 }, new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void Sequence_TreatsNullsConsistently()
    {
        var comparer = BlexComparers.Sequence<int>();

        Assert.IsTrue(comparer.Equals(null, null));
        Assert.IsFalse(comparer.Equals(null, Array.Empty<int>()));
        Assert.AreEqual(0, comparer.GetHashCode(null!));
    }

    [TestMethod]
    public void Sequence_HashesEqualSequencesAlike()
    {
        var comparer = BlexComparers.Sequence<string>();
        Assert.AreEqual(comparer.GetHashCode(new[] { "a", "b" }), comparer.GetHashCode(new List<string> { "a", "b" }));
    }

    [TestMethod]
    public void Set_IgnoresOrderAndRepetition()
    {
        var comparer = BlexComparers.Set<int>();

        Assert.IsTrue(comparer.Equals(new[] { 1, 2, 3 }, new[] { 3, 1, 2 }));
        Assert.IsTrue(comparer.Equals(new[] { 1, 1, 2 }, new[] { 2, 1 }));
        Assert.IsFalse(comparer.Equals(new[] { 1, 2 }, new[] { 1, 3 }));
        Assert.AreEqual(comparer.GetHashCode(new[] { 1, 2 }), comparer.GetHashCode(new[] { 2, 1 }));
    }

    [TestMethod]
    public void Sequence_HonoursACustomElementComparer()
    {
        var comparer = BlexComparers.Sequence<string>(StringComparer.OrdinalIgnoreCase);
        Assert.IsTrue(comparer.Equals(new[] { "AB" }, new[] { "ab" }));
    }

    [TestMethod]
    public void ASelectorProjectingAFreshList_FiresOnlyOnRealChanges()
    {
        var manager = new BlexManager();
        var store = new EntityTodoStore();
        manager.Register(store);

        var fired = 0;
        using var sub = store.Subscribe(
            () => store.Todos.All.Where(t => !t.Done),
            _ => fired++,
            BlexComparers.Sequence<Todo>());

        store.Add(new Todo(1, "write", false));
        Assert.AreEqual(1, fired);

        // An unrelated action still re-runs the projection, which builds a new sequence with the
        // same contents -- without a content comparer this would count as a change.
        store.Add(new Todo(2, "ship", true));
        Assert.AreEqual(1, fired);

        store.Toggle(1);
        Assert.AreEqual(2, fired);
    }

    [TestMethod]
    public void WithoutAComparer_AFreshListLooksLikeAChangeEveryTime()
    {
        var manager = new BlexManager();
        var store = new EntityTodoStore();
        manager.Register(store);

        var fired = 0;
        using var sub = store.Subscribe(() => store.Todos.All.ToList(), _ => fired++);

        store.Add(new Todo(1, "write", false));
        store.Add(new Todo(2, "ship", true));

        Assert.AreEqual(2, fired);   // the baseline the comparers exist to fix
    }
}
