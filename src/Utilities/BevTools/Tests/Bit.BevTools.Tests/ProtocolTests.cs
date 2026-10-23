using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// The protocol external frontends (the companion window, the browser extension) speak through the page hub, and the
/// page-level reports the hub sends back: WebSocket traffic, resource failures, CSP violations, enhanced navigation.
/// </summary>
[TestClass]
public class ProtocolTests : BunitContext
{
    public ProtocolTests()
    {
        Services.AddSingleton<IJSRuntime>(new FakeJSRuntime());
        Services.AddBitBevToolsServices(o => o.Enabled = true);
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private JsonElement Ask(string method, object? args = null) =>
        JsonDocument.Parse(BevToolsProtocol.Handle(Session, method, args is null ? null : JsonSerializer.Serialize(args))).RootElement;

    [TestMethod]
    public void Hello_names_the_runtime_and_the_versions_spoken()
    {
        var hello = Ask("hello");

        Assert.AreEqual(BevToolsProtocol.Version, hello.GetProperty("protocol").GetInt32());
        Assert.AreEqual(Session.Id, hello.GetProperty("sessionId").GetInt64());
        Assert.AreEqual("Full", hello.GetProperty("valueAccess").GetString());
        Assert.AreEqual(BevSessionExporter.Format, hello.GetProperty("exportFormat").GetString());
    }

    [TestMethod]
    public void A_snapshot_is_the_export_and_says_when_nothing_changed()
    {
        Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));

        var first = Ask("snapshot");
        var version = first.GetProperty("version").GetInt64();
        var again = Ask("snapshot", new { since = version });

        Assert.AreEqual(BevSessionExporter.Format, first.GetProperty("session").GetProperty("format").GetString());
        Assert.IsTrue(first.GetProperty("session").GetProperty("components").GetProperty("items").GetArrayLength() >= 2);
        Assert.IsTrue(again.GetProperty("unchanged").GetBoolean());
        Assert.IsFalse(first.GetProperty("session").TryGetProperty("profile", out _), "polls leave the render profile out");
    }

    [TestMethod]
    public void A_baseline_reset_from_a_frontend_switches_the_profiler_to_since_the_reset()
    {
        Ask("command", new { name = "baseline" });

        Assert.IsNotNull(Session.Components.BaselineAt);
        Assert.IsTrue(Session.Ui.Profiler.SinceBaseline);
    }

    [TestMethod]
    public void The_render_profile_is_sent_when_asked_for_by_name()
    {
        Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));

        var snapshot = Ask("snapshot", new { sections = new[] { "profile" } }).GetProperty("session");

        var types = snapshot.GetProperty("profile").GetProperty("types").EnumerateArray().Select(t => t.GetProperty("displayName").GetString()).ToList();
        Assert.Contains("ParentComponent", types);
        Assert.Contains("ChildComponent", types);
    }

    [TestMethod]
    public void A_snapshot_can_be_limited_to_sections_and_to_new_timeline_events()
    {
        for (var i = 0; i < 300; i++)
        {
            Session.Timeline.Record(new BevToolsEvent { Title = "event " + i });
        }

        var latest = Session.Timeline.Snapshot()[^1].Id;
        var snapshot = Ask("snapshot", new { sections = new[] { "timeline" }, timelineAfter = latest - 10 }).GetProperty("session");

        Assert.IsFalse(snapshot.TryGetProperty("components", out _));
        Assert.IsFalse(snapshot.TryGetProperty("http", out _));
        var events = snapshot.GetProperty("timeline").EnumerateArray().Select(e => e.GetProperty("id").GetInt64()).ToList();
        Assert.Contains(latest, events);
        Assert.IsTrue(events.Count is > 10 and < 300, "the newest events are sent again, the rest are not: " + events.Count);
    }

    [TestMethod]
    public void Inspect_and_expand_show_a_component_through_the_same_redaction()
    {
        Render<SecretHolder>();
        var record = Session.Components.All.Single(r => r.Type == typeof(SecretHolder));

        var inspected = Ask("inspect", new { instanceId = record.InstanceId });
        var parameters = inspected.GetProperty("parameters").EnumerateArray().ToList();
        var settings = parameters.Single(p => p.GetProperty("name").GetString() == nameof(SecretHolder.Settings));
        var expanded = Ask("expand", new { instanceId = record.InstanceId, section = "parameters", name = nameof(SecretHolder.Settings), path = "" });

        Assert.AreEqual("SecretHolder", inspected.GetProperty("displayName").GetString());
        Assert.AreEqual(BevRedactor.RedactedValue, parameters.Single(p => p.GetProperty("name").GetString() == nameof(SecretHolder.ApiKey)).GetProperty("display").GetString());
        Assert.IsTrue(settings.GetProperty("expandable").GetBoolean());
        var children = expanded.GetProperty("children").EnumerateArray().ToDictionary(c => c.GetProperty("name").GetString()!, c => c.GetProperty("display").GetString());
        Assert.AreEqual("\"smtp.example.com\"", children["Host"]);
        Assert.AreEqual(BevRedactor.RedactedValue, children["Password"]);
        Assert.IsTrue(record.CaptureState, "inspecting from outside captures its state like selecting it in the panel");
        Assert.AreEqual("No such member.", Ask("expand", new { instanceId = record.InstanceId, section = "parameters", name = "Nope", path = "" }).GetProperty("error").GetString());
    }

    [TestMethod]
    public void Commands_and_exports_do_what_the_panel_buttons_do()
    {
        Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));

        Ask("command", new { name = "pause" });
        Assert.IsFalse(Session.IsCapturing);
        Ask("command", new { name = "resume" });
        Assert.IsTrue(Session.IsCapturing);

        var trace = Ask("export", new { format = "trace" });
        StringAssert.EndsWith(trace.GetProperty("fileName").GetString(), ".json");
        Assert.AreEqual(BevTraceExporter.Format, JsonDocument.Parse(trace.GetProperty("content").GetString()!).RootElement.GetProperty("otherData").GetProperty("format").GetString());
        StringAssert.StartsWith(Ask("export", new { format = "markdown" }).GetProperty("content").GetString(), "# BevTools report");
        Assert.AreEqual("Unknown method 'nope'.", Ask("nope").GetProperty("error").GetString());
    }

    [TestMethod]
    public void Traffic_reports_become_timeline_events_and_a_large_batch_finding()
    {
        var bridge = new BevJsBridge(Session, () => { });
        var socket = new BevSocketStats
        {
            Id = 1,
            Url = "wss://app.example/_blazor?id=«redacted»",
            Kind = "blazor-circuit",
            OpenedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ObservedFrom = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            RenderBatches = new BevRenderBatchTraffic { Count = 3, Bytes = 400_000, MaxBytes = 300_000, ApplyMs = 90, MaxApplyMs = 60 },
            Largest = [new BevLargeMessage { Direction = "←", Kind = "JS.RenderBatch", Bytes = 300_000, At = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }],
        };

        bridge.ReportTraffic([socket]);
        bridge.ReportTraffic([socket]);
        Session.Diagnostics.Evaluate(force: true);

        var events = Session.Timeline.Snapshot().Where(e => e.Category == "websocket").ToList();
        Assert.AreEqual(1, events.Count(e => e.Title.StartsWith("WebSocket opened", StringComparison.Ordinal)));
        Assert.AreEqual(1, events.Count(e => e.Title.StartsWith("Large message", StringComparison.Ordinal)), "a large message is reported once, not on every report");
        Assert.IsTrue(events.All(e => e.Kind == BevToolsEventKind.Circuit));
        Assert.Contains(f => f.Diagnostic.RuleId == "circuit.large-render-batch", Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void Resource_failures_CSP_violations_and_enhanced_navigation_are_recorded_by_kind()
    {
        var bridge = new BevJsBridge(Session, () => { });

        bridge.OnJsError(new BevJsErrorPayload { Type = "ResourceLoadError", Message = "Failed to load <script> /_framework/app.wasm", Source = "/_framework/app.wasm" });
        bridge.OnJsError(new BevJsErrorPayload { Type = "CspViolation", Message = "Content Security Policy blocked inline (script-src)" });
        bridge.OnBrowserEvent("enhancedload", "/products?token=abc123");

        Assert.AreEqual(1, Session.Browser.ResourceLoadErrors);
        Assert.AreEqual(1, Session.Browser.CspViolations);
        Assert.AreEqual(0, Session.Browser.JsErrorCount);
        Assert.AreEqual(1, Session.Browser.EnhancedNavigations);
        CollectionAssert.AreEquivalent(new[] { "resource", "csp" }, Session.Errors.Snapshot().Select(e => e.Source).ToArray());
        var navigation = Session.Timeline.Snapshot().Single(e => e.Category == "enhanced-navigation");
        Assert.AreEqual(BevToolsEventKind.Navigation, navigation.Kind);
        Assert.IsFalse(navigation.Title.Contains("abc123", StringComparison.Ordinal));
    }

    public sealed class SecretHolder : ComponentBase
    {
        [Parameter] public string ApiKey { get; set; } = "opaque-api-key";

        [Parameter] public MailSettings Settings { get; set; } = new();

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "holder");
    }

    public sealed class MailSettings
    {
        public string Host { get; set; } = "smtp.example.com";

        public string Password { get; set; } = "hunter2";
    }
}
