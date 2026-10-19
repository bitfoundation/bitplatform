using System.Threading.Tasks;
using Bit.Blex;
using Bit.Blex.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class TestingHarnessTests
{
    [TestMethod]
    public void Harness_RecordsActions()
    {
        using var harness = BlexTestHarness.For<CounterStore>();

        harness.Store.Increment();
        harness.Store.Add(5);

        Assert.AreEqual(2, harness.Log.Count);
        Assert.AreSequenceEqual(["Increment", "Add"], harness.Log.Names);
        Assert.AreEqual("Add", harness.Log.Last!.ActionName);
    }

    [TestMethod]
    public void Harness_TracksQualifiedNamesAndCounts()
    {
        using var harness = BlexTestHarness.For<CounterStore>();

        harness.Store.Increment();
        harness.Store.Increment();

        Assert.IsTrue(harness.Log.Contains("counter/Increment"));
        Assert.AreEqual(2, harness.Log.CountOf("Increment"));
    }

    [TestMethod]
    public void Harness_SnapshotReflectsState()
    {
        using var harness = BlexTestHarness.For<CounterStore>();
        harness.Store.Add(7);

        var snap = harness.Snapshot();
        Assert.AreEqual(7, snap["Count"]!.GetValue<int>());
        Assert.AreEqual(7, harness.State["counter"]!["Count"]!.GetValue<int>());
    }

    [TestMethod]
    public async Task Harness_RecordsAsyncEffectAsSingleAction()
    {
        using var harness = BlexTestHarness.For<DataStore>();

        await harness.Store.Load("hi");

        Assert.AreSequenceEqual(["Load"], harness.Log.Names);
    }

    [TestMethod]
    public void RecordActions_Extension_WorksOnManager()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        using var log = manager.RecordActions();

        counter.Increment();

        Assert.AreEqual(1, log.Count);
    }

    [TestMethod]
    public void CountNotifications_AssertsBatching()
    {
        var store = new CounterStore();
        store.Count = 5;
        store.Label = "x";
        var notifications = store.CountNotifications(() => store.Reset()); // resets two fields
        Assert.AreEqual(1, notifications); // batched into one
    }

    [TestMethod]
    public void Snapshot_Extension_ReturnsState()
    {
        var store = new CounterStore();
        store.Add(3);
        Assert.AreEqual(3, store.Snapshot()["Count"]!.GetValue<int>());
    }
}
