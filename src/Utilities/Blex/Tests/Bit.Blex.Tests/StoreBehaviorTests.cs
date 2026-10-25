using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class StoreBehaviorTests
{
    [TestMethod]
    public void StateProperty_RaisesStateChanged()
    {
        var store = new CounterStore();
        var raised = 0;
        store.StateChanged += () => raised++;

        store.Count = 5;

        Assert.AreEqual(5, store.Count);
        Assert.AreEqual(1, raised);
    }

    [TestMethod]
    public void StateProperty_NoChange_DoesNotNotify()
    {
        var store = new CounterStore();
        store.Count = 5;
        var raised = 0;
        store.StateChanged += () => raised++;

        store.Count = 5; // same value

        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void Action_MutatesState()
    {
        var store = new CounterStore();
        store.Increment();
        store.Add(10);
        Assert.AreEqual(11, store.Count);
    }

    [TestMethod]
    public void Action_BatchesMultipleMutations_IntoSingleNotification()
    {
        var store = new CounterStore();
        store.Count = 7;
        store.Label = "x";
        var raised = 0;
        store.StateChanged += () => raised++;

        store.Reset(); // sets Count and Label

        Assert.AreEqual(0, store.Count);
        Assert.AreEqual("idle", store.Label);
        Assert.AreEqual(1, raised); // batched into one
    }

    [TestMethod]
    public void Computed_ReflectsState()
    {
        var store = new CounterStore();
        store.Count = 6;
        Assert.AreEqual(12, store.DoubleCount);
    }

    [TestMethod]
    public void Computed_IsMemoized_UntilStateChanges()
    {
        var store = new CounterStore();
        _ = store.TrackedCount;
        _ = store.TrackedCount;
        _ = store.TrackedCount;
        Assert.AreEqual(1, store.ComputeCalls); // memoized

        store.Increment(); // invalidates
        _ = store.TrackedCount;
        Assert.AreEqual(2, store.ComputeCalls);
    }

    [TestMethod]
    public async Task AsyncAction_Works_AndUsesExplicitName()
    {
        var store = new CounterStore();
        await store.LoadData();
        Assert.AreEqual(42, store.Count);
        Assert.AreEqual("loaded", store.Label);
    }

    [TestMethod]
    public async Task AsyncAction_NotifiesDuringAwait_ForLoadingState()
    {
        var store = new CounterStore();
        var raised = 0;
        store.StateChanged += () => raised++;

        var task = store.LoadData(); // sets Label="loading" synchronously, then awaits
        Assert.AreEqual("loading", store.Label);
        Assert.IsTrue(raised >= 1); // UI was notified before completion

        await task;
        Assert.AreEqual("loaded", store.Label);
    }

    [TestMethod]
    public void StandaloneSetter_RecordsActionOnManager()
    {
        var seen = new List<string>();
        var manager = new BlexManager(new[] { new BlexDelegateMiddleware(c => seen.Add(c.ActionName)) });
        var store = new CounterStore();
        manager.Register(store);

        store.Count = 3; // direct property set, no Dispatch

        Assert.AreSequenceEqual(["Set Count"], seen);
    }

    [TestMethod]
    public async Task AsyncAction_RecordsExactlyOneAction()
    {
        var seen = new List<string>();
        var manager = new BlexManager(new[] { new BlexDelegateMiddleware(c => seen.Add(c.ActionName)) });
        var store = new CounterStore();
        manager.Register(store);

        await store.LoadData(); // sets Label then Count across an await

        Assert.AreSequenceEqual(["LoadData"], seen); // batched into a single recorded action
    }

    [TestMethod]
    public async Task ValueTaskAction_IsAwaited_NotFireAndForget()
    {
        var store = new CounterStore();

        await store.LoadValue(); // returns ValueTask<int>; wrapper exposes Task

        Assert.AreEqual(7, store.Count); // mutation completed because the action was awaited
    }

    [TestMethod]
    public async Task Action_ExplicitLabelWithSpaces_KeepsValidWrapperName_AndLabel()
    {
        var seen = new List<string>();
        var manager = new BlexManager(new[] { new BlexDelegateMiddleware(c => seen.Add(c.ActionName)) });
        var store = new CounterStore();
        manager.Register(store);

        await store.LoadValue(); // wrapper name derived from OnLoadValue -> LoadValue

        Assert.AreSequenceEqual(["Load Value"], seen); // label honors the spaced display name
    }

    [TestMethod]
    public void Serialize_Deserialize_RoundTrips()
    {
        var store = new CounterStore();
        store.Count = 99;
        store.Label = "saved";

        var json = store.SerializeState();
        var restored = new CounterStore();
        restored.DeserializeState(json);

        Assert.AreEqual(99, restored.Count);
        Assert.AreEqual("saved", restored.Label);
    }

    [TestMethod]
    public void StoreName_HonorsAttribute_AndDefaults()
    {
        Assert.AreEqual("counter", new CounterStore().Name);
        Assert.AreEqual("TodoStore", new TodoStore().Name);
    }

    [TestMethod]
    public void ComplexState_SerializesCorrectly()
    {
        var store = new TodoStore();
        store.AddItem("a");
        store.AddItem("b");

        var json = store.SerializeState();
        var restored = new TodoStore();
        restored.DeserializeState(json);

        Assert.AreSequenceEqual(new List<string> { "a", "b" }, restored.Items);
    }
}
