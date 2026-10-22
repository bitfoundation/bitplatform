using Bit.Blex.Testing;

namespace Bit.Blex.Tests;

/// <summary>
/// Deterministic tests: the action log read through typed descriptors, and replayed onto a fresh
/// store to prove the same actions arrive at the same state.
/// </summary>
[TestClass]
public class TestingReplayTests
{
    [TestMethod]
    public void The_log_answers_in_terms_of_descriptors()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        harness.Store.Place("pen", 2);
        harness.Store.AddUnits(1);
        harness.Store.Place("ink", 5);

        Assert.IsTrue(harness.Log.Contains(OrderBookStore.Actions.Place));
        Assert.IsFalse(harness.Log.Contains(OrderBookStore.Actions.Clear));
        Assert.AreEqual(2, harness.Log.CountOf(OrderBookStore.Actions.Place));

        var orders = harness.Log.PayloadsOf(OrderBookStore.Actions.Place);
        CollectionAssert.AreEqual(new[] { ("pen", 2), ("ink", 5) }, orders.Select(o => (o.Name, o.Quantity)).ToArray());
    }

    [TestMethod]
    public async Task Replaying_a_log_onto_a_fresh_store_arrives_at_the_same_state()
    {
        using var original = BlexTestHarness.For<OrderBookStore>();
        original.Store.Place("pen", 2);
        original.Store.SetPriority(OrderPriority.High);
        original.Store.Units = 40;              // a direct assignment: "Set Units"
        await original.Store.ImportAsync(3);
        await original.Store.Sync("nightly");
        original.Store.Tag("a", "b");

        using var replica = BlexTestHarness.For<OrderBookStore>();
        await replica.Manager.ReplayAsync(original.Log.Actions);

        Assert.AreEqual(original.Snapshot().ToJsonString(), replica.Snapshot().ToJsonString());
        CollectionAssert.AreEqual(original.Log.QualifiedNames.ToArray(), replica.Log.QualifiedNames.ToArray());
    }

    [TestMethod]
    public async Task Replay_spans_stores()
    {
        using var original = BlexTestHarness.For<OrderBookStore>();
        var counter = original.Register(new CounterStore());
        original.Store.AddUnits(2);
        counter.Add(7);
        original.Store.ResetState();

        using var replica = BlexTestHarness.For<OrderBookStore>();
        var replicaCounter = replica.Register(new CounterStore());
        await replica.Manager.ReplayAsync(original.Log.Actions);

        Assert.AreEqual(0, replica.Store.Units);
        Assert.AreEqual(7, replicaCounter.Count);
    }

    [TestMethod]
    public async Task Replaying_into_the_recording_manager_is_safe()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();
        harness.Store.AddUnits(1);

        await harness.Manager.ReplayAsync(harness.Log.Actions);

        Assert.AreEqual(2, harness.Store.Units);
        Assert.AreEqual(2, harness.Log.Count);
    }

    [TestMethod]
    public async Task A_batch_cannot_be_replayed_and_says_so()
    {
        using var original = BlexTestHarness.For<OrderBookStore>();
        original.Store.Batch("Bulk edit", () => original.Store.Units = 3);

        using var replica = BlexTestHarness.For<OrderBookStore>();
        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => replica.Manager.ReplayAsync(original.Log.Actions));

        StringAssert.Contains(error.Message, "orders/Bulk edit");
    }
}
