using System.Text.Json.Nodes;

namespace Bit.Blex.Tests;

/// <summary>
/// The monitor's timeline after the messages that rewrite it -- ROLLBACK, RESET, IMPORT_STATE -- and the
/// status flags a page shows beside it. The extension only drops actions it shows when the app re-initializes
/// it, and only shows an imported session once the app hands it back.
/// </summary>
[TestClass]
public class DevToolsTimelineTests
{
    private sealed class RecordingDevTools : IBlexDevTools
    {
        public List<JsonObject> Inits { get; } = [];

        public List<JsonObject> Lifted { get; } = [];

        public void Init(JsonObject globalState) => Inits.Add(globalState);

        public void Send(string actionName, JsonObject globalState) { }

        public void SendLiftedState(JsonObject liftedState) => Lifted.Add(liftedState);
    }

    private static (BlexManager Manager, CounterStore Store, RecordingDevTools DevTools) Setup()
    {
        var manager = new BlexManager();
        var store = new CounterStore();
        manager.Register(store);
        var devTools = new RecordingDevTools();
        manager.ConnectDevTools(devTools);
        return (manager, store, devTools);
    }

    private static string Dispatch(string payloadType)
        => new JsonObject { ["type"] = "DISPATCH", ["payload"] = new JsonObject { ["type"] = payloadType } }.ToJsonString();

    [TestMethod]
    public void Rollback_reinitializes_the_monitor_at_the_committed_state()
    {
        var (manager, store, devTools) = Setup();
        store.Increment();
        store.Increment();
        manager.HandleDevToolsMessage(Dispatch("COMMIT"));
        store.Increment();
        devTools.Inits.Clear();

        manager.HandleDevToolsMessage(Dispatch("ROLLBACK"));

        Assert.AreEqual(2, store.Count);
        var init = Assert.ContainsSingle(devTools.Inits, "without an init the monitor keeps the actions after the commit");
        Assert.AreEqual(2, init["counter"]!["Count"]!.GetValue<int>());
    }

    [TestMethod]
    public void Rollback_after_a_reset_returns_to_the_reset_not_to_an_earlier_commit()
    {
        var (manager, store, _) = Setup();
        store.Increment();
        store.Increment();
        store.Increment();
        manager.HandleDevToolsMessage(Dispatch("COMMIT"));
        manager.HandleDevToolsMessage(Dispatch("RESET"));
        Assert.AreEqual(0, store.Count);
        store.Increment();

        manager.HandleDevToolsMessage(Dispatch("ROLLBACK"));

        Assert.AreEqual(0, store.Count, "the monitor's baseline after RESET is the reset state");
    }

    [TestMethod]
    public void Import_applies_the_last_state_and_hands_the_session_back_to_the_monitor()
    {
        var (manager, store, devTools) = Setup();
        var message = new JsonObject
        {
            ["type"] = "DISPATCH",
            ["payload"] = new JsonObject
            {
                ["type"] = "IMPORT_STATE",
                ["nextLiftedState"] = new JsonObject
                {
                    ["committedState"] = new JsonObject { ["counter"] = new JsonObject { ["Count"] = 4, ["Label"] = "idle" } },
                    ["computedStates"] = new JsonArray
                    {
                        new JsonObject { ["state"] = new JsonObject { ["counter"] = new JsonObject { ["Count"] = 5, ["Label"] = "idle" } } },
                        new JsonObject { ["state"] = new JsonObject { ["counter"] = new JsonObject { ["Count"] = 9, ["Label"] = "idle" } } },
                    },
                },
            },
        };

        manager.HandleDevToolsMessage(message.ToJsonString());

        Assert.AreEqual(9, store.Count);
        var lifted = Assert.ContainsSingle(devTools.Lifted);
        Assert.AreEqual(2, lifted["computedStates"]!.AsArray().Count);

        // The imported session's commit is what a rollback returns to.
        store.Increment();
        manager.HandleDevToolsMessage(Dispatch("ROLLBACK"));
        Assert.AreEqual(4, store.Count);
    }

    [TestMethod]
    public void Status_changes_are_raised_once_per_actual_change()
    {
        var (manager, _, _) = Setup();
        var raised = 0;
        manager.DevToolsStatusChanged += () => raised++;

        manager.HandleDevToolsMessage("""{"type":"DISPATCH","payload":{"type":"PAUSE_RECORDING","status":true}}""");
        manager.HandleDevToolsMessage("""{"type":"DISPATCH","payload":{"type":"PAUSE_RECORDING","status":true}}""");
        Assert.AreEqual(1, raised, "pausing an already paused monitor changes nothing");

        manager.HandleDevToolsMessage("""{"type":"DISPATCH","payload":{"type":"LOCK_CHANGES","status":true}}""");
        Assert.AreEqual(2, raised);
        Assert.IsTrue(manager.AreChangesLocked);

        manager.DisconnectDevTools();
        Assert.AreEqual(3, raised, "disconnecting clears both flags");
        Assert.IsFalse(manager.IsDevToolsPaused);
        Assert.IsFalse(manager.AreChangesLocked);
    }

    [TestMethod]
    public void A_throwing_status_handler_is_reported_not_thrown()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        manager.Register(new CounterStore());
        manager.ConnectDevTools(new RecordingDevTools());
        manager.DevToolsStatusChanged += () => throw new InvalidOperationException("boom");

        manager.HandleDevToolsMessage("""{"type":"DISPATCH","payload":{"type":"PAUSE_RECORDING","status":true}}""");

        Assert.IsTrue(manager.IsDevToolsPaused);
        Assert.Contains(e => e.Source == "devtools", errors);
    }
}
