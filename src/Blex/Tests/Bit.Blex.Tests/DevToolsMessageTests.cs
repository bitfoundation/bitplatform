using System.Collections.Generic;
using System.Text.Json.Nodes;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// <see cref="BlexManager.HandleDevToolsMessage"/> is reached from a JS interop callback with
/// whatever the browser extension sent, so no message shape may throw back into interop.
/// </summary>
[TestClass]
public class DevToolsMessageTests
{
    private static (CounterStore Store, BlexManager Manager, List<BlexError> Errors) Setup()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        var store = new CounterStore();
        manager.Register(store);
        return (store, manager, errors);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not json")]
    [DataRow("null")]
    [DataRow("[]")]
    [DataRow("123")]
    [DataRow("""{"type":42}""")]                                     // non-string type
    [DataRow("""{"type":"DISPATCH","payload":"oops"}""")]            // payload is not an object
    [DataRow("""{"type":"DISPATCH","payload":{"type":7}}""")]        // non-string payload type
    [DataRow("""{"type":"DISPATCH","payload":{"type":"JUMP_TO_STATE"},"state":99}""")]
    [DataRow("""{"type":"DISPATCH","payload":{"type":"JUMP_TO_STATE"},"state":"{bad json"}""")]
    [DataRow("""{"type":"DISPATCH","payload":{"type":"IMPORT_STATE"}}""")]
    [DataRow("""{"type":"DISPATCH","payload":{"type":"UNKNOWN_KIND"}}""")]
    [DataRow("""{"type":"START"}""")]
    public void MalformedMessage_NeverThrows(string message)
    {
        var (store, manager, _) = Setup();
        store.Increment();

        manager.HandleDevToolsMessage(message); // must not throw

        Assert.AreEqual(1, store.Count); // and must not corrupt state
    }

    [TestMethod]
    public void JumpToState_AppliesTheSnapshot()
    {
        var (store, manager, errors) = Setup();
        store.Increment();
        var snapshot = manager.CaptureGlobalState().ToJsonString();
        store.Increment();
        Assert.AreEqual(2, store.Count);

        manager.HandleDevToolsMessage(
            $$"""{"type":"DISPATCH","payload":{"type":"JUMP_TO_STATE"},"state":{{JsonValue.Create(snapshot)!.ToJsonString()}}}""");

        Assert.AreEqual(1, store.Count);
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void StartMessage_ReSendsTheCurrentStateAsTheInitialSnapshot()
    {
        // The extension sends START when its monitor is (re)opened, which can be long after the
        // app booted. Without re-initializing, the monitor stays blank until the next action.
        var sink = new RecordingDevTools();
        var (store, manager, errors) = Setup();
        manager.ConnectDevTools(sink);
        store.Increment();
        store.Increment();
        sink.Inits.Clear();

        manager.HandleDevToolsMessage("""{"type":"START"}""");

        var init = Assert.ContainsSingle(sink.Inits);
        Assert.AreEqual(2, init["counter"]!["Count"]!.GetValue<int>());
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void StartMessage_WhenNotConnected_IsIgnored()
    {
        var sink = new RecordingDevTools();
        var (_, manager, errors) = Setup();
        manager.ConnectDevTools(sink);
        manager.DisconnectDevTools();
        sink.Inits.Clear();

        manager.HandleDevToolsMessage("""{"type":"START"}""");

        Assert.IsEmpty(sink.Inits);
        Assert.IsEmpty(errors);
    }

    private sealed class RecordingDevTools : IBlexDevTools
    {
        public List<JsonObject> Inits { get; } = [];
        public void Init(JsonObject globalState) => Inits.Add(globalState);
        public void Send(string actionName, JsonObject globalState) { }
    }

    [TestMethod]
    public void UnparseableJumpState_IsReportedNotThrown()
    {
        var (store, manager, errors) = Setup();
        store.Increment();

        manager.HandleDevToolsMessage(
            """{"type":"DISPATCH","payload":{"type":"JUMP_TO_STATE"},"state":"{not-json"}""");

        Assert.AreEqual(1, store.Count);
        Assert.Contains(e => e.Source == "devtools", errors);
    }
}
