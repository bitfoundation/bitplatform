using System.Collections.Generic;
using System.Text.Json.Nodes;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class ManagerTests
{
    [TestMethod]
    public void GlobalState_AggregatesAllStores()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var todo = new TodoStore();
        manager.Register(counter);
        manager.Register(todo);

        counter.Count = 3;
        todo.AddItem("x");

        var global = manager.CaptureGlobalState();
        Assert.IsNotNull(global["counter"]);
        Assert.IsNotNull(global["TodoStore"]);
        Assert.AreEqual(3, global["counter"]!["Count"]!.GetValue<int>());
    }

    [TestMethod]
    public void Middleware_ReceivesActions_WithQualifiedName()
    {
        var seen = new List<string>();
        var manager = new BlexManager(new[] { new BlexDelegateMiddleware(ctx => seen.Add(ctx.QualifiedName)) });
        var counter = new CounterStore();
        manager.Register(counter);

        counter.Increment();
        counter.Add(2);

        Assert.AreSequenceEqual(["counter/Increment", "counter/Add"], seen);
    }

    [TestMethod]
    public void TimeTravel_RestoresPreviousState()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        counter.Count = 1;
        var snapshotAt1 = manager.CaptureGlobalState();
        counter.Count = 2;
        counter.Count = 3;
        Assert.AreEqual(3, counter.Count);

        // Simulate the DevTools JUMP_TO_STATE message carrying the snapshot state.
        var message = new JsonObject
        {
            ["type"] = "DISPATCH",
            ["payload"] = new JsonObject { ["type"] = "JUMP_TO_STATE" },
            ["state"] = snapshotAt1.ToJsonString(),
        };

        var notified = 0;
        counter.StateChanged += () => notified++;
        manager.HandleDevToolsMessage(message.ToJsonString());

        Assert.AreEqual(1, counter.Count);
        Assert.AreEqual(1, notified);
    }

    [TestMethod]
    public void TimeTravel_DoesNotRecordNewActions()
    {
        var seen = new List<string>();
        var manager = new BlexManager(new[] { new BlexDelegateMiddleware(ctx => seen.Add(ctx.ActionName)) });
        var counter = new CounterStore();
        manager.Register(counter);

        counter.Count = 5;
        var snap = manager.CaptureGlobalState();
        counter.Count = 9;
        seen.Clear();

        var message = new JsonObject
        {
            ["type"] = "DISPATCH",
            ["payload"] = new JsonObject { ["type"] = "JUMP_TO_ACTION" },
            ["state"] = snap.ToJsonString(),
        };
        manager.HandleDevToolsMessage(message.ToJsonString());

        Assert.AreEqual(5, counter.Count);
        Assert.IsEmpty(seen); // restoring must not dispatch
    }

    [TestMethod]
    public void DevToolsSink_ReceivesInitAndSend()
    {
        var sink = new FakeDevTools();
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        manager.ConnectDevTools(sink);

        Assert.AreEqual(1, sink.InitCount);

        counter.Increment();
        Assert.ContainsSingle(sink.Sent);
        Assert.AreEqual("counter/Increment", sink.Sent[0]);
    }

    [TestMethod]
    public void DevTools_Reset_RestoresInitialState()
    {
        var sink = new FakeDevTools();
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        manager.ConnectDevTools(sink); // captures initial state (Count == 0)

        counter.Count = 5;

        var message = new JsonObject
        {
            ["type"] = "DISPATCH",
            ["payload"] = new JsonObject { ["type"] = "RESET" },
        };
        manager.HandleDevToolsMessage(message.ToJsonString());

        Assert.AreEqual(0, counter.Count);
    }

    [TestMethod]
    public void DevTools_CommitThenRollback_RestoresCommittedState()
    {
        var sink = new FakeDevTools();
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        manager.ConnectDevTools(sink);

        counter.Count = 4;
        var commit = new JsonObject
        {
            ["type"] = "DISPATCH",
            ["payload"] = new JsonObject { ["type"] = "COMMIT" },
        };
        manager.HandleDevToolsMessage(commit.ToJsonString()); // committed at Count == 4

        counter.Count = 9;
        var rollback = new JsonObject
        {
            ["type"] = "DISPATCH",
            ["payload"] = new JsonObject { ["type"] = "ROLLBACK" },
        };
        manager.HandleDevToolsMessage(rollback.ToJsonString());

        Assert.AreEqual(4, counter.Count); // reverted to last committed snapshot
    }

    [TestMethod]
    public void DevTools_ImportState_RestoresLastComputedState()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        counter.Count = 2;
        var target = manager.CaptureGlobalState();
        counter.Count = 8;

        var message = new JsonObject
        {
            ["type"] = "DISPATCH",
            ["payload"] = new JsonObject
            {
                ["type"] = "IMPORT_STATE",
                ["nextLiftedState"] = new JsonObject
                {
                    ["computedStates"] = new JsonArray
                    {
                        new JsonObject { ["state"] = target.DeepClone() },
                    },
                },
            },
        };
        manager.HandleDevToolsMessage(message.ToJsonString());

        Assert.AreEqual(2, counter.Count);
    }

    private sealed class FakeDevTools : IBlexDevTools
    {
        public int InitCount { get; private set; }
        public List<string> Sent { get; } = new();
        public void Init(JsonObject globalState) => InitCount++;
        public void Send(string actionName, JsonObject globalState) => Sent.Add(actionName);
    }
}
