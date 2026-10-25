using System.Text.Json.Nodes;

namespace Bit.Blex.Tests;

/// <summary>
/// Undo/redo serializes only the stores an action changed: snapshots share every other store's slice, so
/// recording an action costs what changed, not the whole tree.
/// </summary>
[TestClass]
public class HistorySharingTests
{
    /// <summary>A hand-written store that counts how often it is serialized.</summary>
    private sealed class CountingStore : BlexStoreBase
    {
        private int _value;

        public int Serializations { get; private set; }

        public override string Name => "counting";

        // Through SetState, as generated setters do, so the change is batched into the action and recorded.
        public void Set(int value) => Dispatch("Set", () => SetState(ref _value, value, "Value"));

        public override JsonObject SerializeState()
        {
            Serializations++;
            return new JsonObject { ["Value"] = _value };
        }

        public int Deserializations { get; private set; }

        public override void DeserializeState(JsonObject state)
        {
            Deserializations++;
            _value = state["Value"]!.GetValue<int>();
        }
    }

    [TestMethod]
    public void Undo_and_redo_restore_only_the_stores_that_moved()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var other = new CountingStore();
        manager.Register(counter);
        manager.Register(other);
        var history = new BlexHistory(manager);
        history.Start();
        var restored = new List<string>();
        manager.StoreRestored += store => restored.Add(store.Name);

        counter.Increment();
        counter.Increment();
        history.Undo();
        history.Redo();

        Assert.AreEqual(0, other.Deserializations, "the store no step changed is never restored");
        Assert.AreSequenceEqual(["counter", "counter"], restored);
        Assert.AreEqual(2, counter.Count);
    }

    [TestMethod]
    public void A_store_that_did_not_change_is_not_serialized_again()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var other = new CountingStore();
        manager.Register(counter);
        manager.Register(other);
        var history = new BlexHistory(manager);
        history.Start();
        var baseline = other.Serializations;

        for (var i = 0; i < 5; i++)
            counter.Increment();

        Assert.AreEqual(baseline, other.Serializations, "five counter actions must not serialize the untouched store");
        Assert.AreEqual(5, history.UndoCount);
    }

    [TestMethod]
    public void Undo_through_shared_slices_restores_every_store()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var other = new CountingStore();
        manager.Register(counter);
        manager.Register(other);
        var history = new BlexHistory(manager);
        history.Start();

        counter.Increment();
        other.Set(7);
        counter.Increment();
        Assert.AreEqual(3, history.UndoCount, "all three actions are recorded steps");

        history.Jump(-3);
        Assert.AreEqual(0, counter.Count);
        Assert.AreEqual(0, other.SerializeState()["Value"]!.GetValue<int>());

        history.Jump(3);
        Assert.AreEqual(2, counter.Count);
        Assert.AreEqual(7, other.SerializeState()["Value"]!.GetValue<int>());
    }

    [TestMethod]
    public void A_checkpoint_after_nothing_changed_is_still_collapsed()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager, filter: _ => false);
        history.Start();

        counter.Increment();
        history.Checkpoint("one");
        history.Checkpoint("again");   // nothing moved: no new step

        Assert.AreEqual(1, history.UndoCount);
    }
}
