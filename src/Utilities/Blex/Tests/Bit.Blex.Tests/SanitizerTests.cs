using System.Collections.Generic;
using System.Text.Json.Nodes;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class SanitizerTests
{
    private sealed class CapturingDevTools : IBlexDevTools
    {
        public List<JsonObject> Inits { get; } = new();
        public List<(string Action, JsonObject State)> Sends { get; } = new();
        public List<JsonObject?> Payloads { get; } = new();
        public void Init(JsonObject globalState) => Inits.Add(globalState);
        public void Send(string actionName, JsonObject globalState) => Send(actionName, globalState, null);

        public void Send(string actionName, JsonObject globalState, JsonObject? payload)
        {
            Sends.Add((actionName, globalState));
            Payloads.Add(payload);
        }
    }

    [TestMethod]
    public void StateSanitizer_AppliesToSentState_NotInternalState()
    {
        var sink = new CapturingDevTools();
        var manager = new BlexManager
        {
            DevToolsStateSanitizer = state =>
            {
                var clone = (JsonObject)state.DeepClone();
                clone["counter"]!["Label"] = "<redacted>";
                return clone;
            },
        };
        var counter = new CounterStore();
        manager.Register(counter);
        manager.ConnectDevTools(sink);

        counter.Increment();

        // Sent state is sanitized...
        Assert.AreEqual("<redacted>", sink.Sends[0].State["counter"]!["Label"]!.GetValue<string>());
        // ...but the actual store/global state is untouched.
        Assert.AreEqual("idle", manager.CaptureGlobalState()["counter"]!["Label"]!.GetValue<string>());
    }

    [TestMethod]
    public void ActionSanitizer_RewritesActionLabel()
    {
        var sink = new CapturingDevTools();
        var manager = new BlexManager
        {
            DevToolsActionSanitizer = label => label.ToUpperInvariant(),
        };
        var counter = new CounterStore();
        manager.Register(counter);
        manager.ConnectDevTools(sink);

        counter.Increment();

        Assert.AreEqual("COUNTER/INCREMENT", sink.Sends[0].Action);
    }

    [TestMethod]
    public void RedactDevToolsKeys_RedactsMatchingKeysRecursively()
    {
        var options = new BlexOptions();
        options.RedactDevToolsKeys("Label");

        var state = new JsonObject
        {
            ["counter"] = new JsonObject { ["Count"] = 1, ["Label"] = "secret" },
        };

        var sanitized = options.DevToolsStateSanitizer!(state);

        Assert.AreEqual("<redacted>", sanitized["counter"]!["Label"]!.GetValue<string>());
        Assert.AreEqual(1, sanitized["counter"]!["Count"]!.GetValue<int>());
        // Original untouched (sanitizer clones).
        Assert.AreEqual("secret", state["counter"]!["Label"]!.GetValue<string>());
    }

    [TestMethod]
    public void RedactDevToolsKeys_CalledTwice_WidensTheRedactionSetInsteadOfReplacingIt()
    {
        // Redaction is a fail-closed feature: a second call silently discarding the keys the first
        // call registered would leak exactly the data the caller asked to hide.
        var options = new BlexOptions();
        options.RedactDevToolsKeys("Label");
        options.RedactDevToolsKeys("Token");

        var state = new JsonObject
        {
            ["counter"] = new JsonObject { ["Count"] = 1, ["Label"] = "secret", ["Token"] = "abc" },
        };

        var sanitized = options.DevToolsStateSanitizer!(state);

        Assert.AreEqual("<redacted>", sanitized["counter"]!["Label"]!.GetValue<string>());
        Assert.AreEqual("<redacted>", sanitized["counter"]!["Token"]!.GetValue<string>());
        Assert.AreEqual(1, sanitized["counter"]!["Count"]!.GetValue<int>());
    }

    [TestMethod]
    public void RedactDevToolsKeys_ComposesWithASanitizerAssignedBeforeIt()
    {
        var options = new BlexOptions
        {
            DevToolsStateSanitizer = state =>
            {
                var clone = (JsonObject)state.DeepClone();
                clone["counter"]!["Count"] = -1;
                return clone;
            },
        };
        options.RedactDevToolsKeys("Label");

        var state = new JsonObject
        {
            ["counter"] = new JsonObject { ["Count"] = 1, ["Label"] = "secret" },
        };

        var sanitized = options.DevToolsStateSanitizer!(state);

        Assert.AreEqual(-1, sanitized["counter"]!["Count"]!.GetValue<int>()); // the earlier sanitizer ran
        Assert.AreEqual("<redacted>", sanitized["counter"]!["Label"]!.GetValue<string>());
    }

    [TestMethod]
    public void RedactDevToolsKeys_RedactsInsideArrays()
    {
        var options = new BlexOptions();
        options.RedactDevToolsKeys("token");

        // Entity-style collection state: objects nested inside arrays must be redacted too.
        var state = new JsonObject
        {
            ["sessions"] = new JsonObject
            {
                ["Items"] = new JsonArray(
                    new JsonObject { ["Id"] = 1, ["token"] = "secret-1" },
                    new JsonObject { ["Id"] = 2, ["token"] = "secret-2" }),
            },
        };

        var sanitized = options.DevToolsStateSanitizer!(state);

        var items = sanitized["sessions"]!["Items"]!.AsArray();
        Assert.AreEqual("<redacted>", items[0]!["token"]!.GetValue<string>());
        Assert.AreEqual("<redacted>", items[1]!["token"]!.GetValue<string>());
        Assert.AreEqual(1, items[0]!["Id"]!.GetValue<int>());
    }

    [TestMethod]
    public void RedactDevToolsKeys_MatchesCaseInsensitively()
    {
        var options = new BlexOptions();
        // State slices are keyed by the generated PascalCase property name while action payloads
        // use the camelCase parameter name; a redaction helper must catch both spellings.
        options.RedactDevToolsKeys("token", "password");

        var state = new JsonObject
        {
            ["auth"] = new JsonObject { ["Token"] = "secret", ["PASSWORD"] = "hunter2", ["User"] = "sam" },
        };

        var sanitized = options.DevToolsStateSanitizer!(state);

        Assert.AreEqual("<redacted>", sanitized["auth"]!["Token"]!.GetValue<string>());
        Assert.AreEqual("<redacted>", sanitized["auth"]!["PASSWORD"]!.GetValue<string>());
        Assert.AreEqual("sam", sanitized["auth"]!["User"]!.GetValue<string>());
    }

    [TestMethod]
    public void RedactDevToolsKeys_RedactsActionPayloads()
    {
        var sink = new CapturingDevTools();
        var options = new BlexOptions();
        options.RedactDevToolsKeys("Label");

        var manager = new BlexManager { DevToolsStateSanitizer = options.DevToolsStateSanitizer };
        var counter = new CounterStore();
        manager.Register(counter);
        manager.ConnectDevTools(sink);

        counter.Label = "secret"; // standalone set: the payload carries the assigned value

        Assert.AreEqual("<redacted>", sink.Payloads[^1]!["Label"]!.GetValue<string>());
    }

    [TestMethod]
    public void Sanitizer_Exception_FailsClosed_AndReportsError()
    {
        var sink = new CapturingDevTools();
        var errors = new List<BlexError>();
        var manager = new BlexManager
        {
            DevToolsStateSanitizer = _ => throw new System.Exception("boom"),
        };
        manager.OnError = errors.Add;
        var counter = new CounterStore();
        manager.Register(counter);
        manager.ConnectDevTools(sink);

        counter.Increment(); // must not throw

        // A broken redaction sanitizer must not leak the data it was meant to hide: the state
        // sent to the extension is withheld (empty), and the failure is reported.
        Assert.ContainsSingle(sink.Sends);
        Assert.IsEmpty(sink.Sends[0].State);
        Assert.Contains(e => e.Source == "devtools", errors);
    }
}
