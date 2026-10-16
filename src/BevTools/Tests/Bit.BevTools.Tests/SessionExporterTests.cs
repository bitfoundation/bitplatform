using System.Net;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// An export is what ends up attached to an issue, so it has to be complete, valid, and no more revealing than the
/// panels: the same redaction, and never a value the inspector would not have shown.
/// </summary>
[TestClass]
public class SessionExporterTests : BunitContext
{
    public SessionExporterTests() => Services.AddBitBevToolsServices(o => o.Enabled = true);

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public async Task Json_export_is_valid_and_contains_every_captured_section()
    {
        var cut = Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));
        await cut.InvokeAsync(() => cut.Instance.Increment());
        var handler = new BevToolsHttpMessageHandler(Session) { InnerHandler = new StubHandler() };
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "super-secret-token");
        await client.GetAsync("https://example.test/api/orders?apikey=hunter2&page=2");
        Session.Errors.Record(new InvalidOperationException("exported failure"), "test");
        Session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Custom,
            Category = "app",
            Title = "custom event",
            Data = new Dictionary<string, object?> { ["explodes"] = new ThrowingToString(), ["tags"] = new[] { "a", "b" }, ["n"] = 4.5 },
        });
        Session.Components.MarkBaseline();
        Session.Diagnostics.Evaluate(force: true);

        var json = BevSessionExporter.ToJson(Session);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.AreEqual(BevSessionExporter.Format, root.GetProperty("format").GetString());
        Assert.AreEqual(Session.Id, root.GetProperty("session").GetProperty("id").GetInt64());
        Assert.IsTrue(root.GetProperty("session").GetProperty("capturing").GetBoolean());
        Assert.IsTrue(root.GetProperty("capabilities").GetArrayLength() > 10);
        Assert.IsTrue(root.GetProperty("overhead").GetProperty("appRenderCount").GetInt64() >= 2);

        var components = root.GetProperty("components");
        Assert.AreNotEqual(JsonValueKind.Undefined, components.GetProperty("baselineAt").ValueKind);
        var items = components.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(c => c.GetProperty("displayName").GetString() == "ParentComponent" && c.GetProperty("renders").GetInt32() == 2, items);
        Assert.Contains(c => c.GetProperty("displayName").GetString() == "ChildComponent" && c.GetProperty("lastCause").GetString() == "ParentRender", items);
        Assert.DoesNotContain(c => c.GetProperty("type").GetString()!.StartsWith("Bit.BevTools.Bev", StringComparison.Ordinal), items);

        var timeline = root.GetProperty("timeline").EnumerateArray().ToList();
        Assert.Contains(e => e.GetProperty("kind").GetString() == "Render", timeline);
        var custom = Assert.ContainsSingle(e => e.GetProperty("title").GetString() == "custom event", timeline);
        Assert.Contains("ToString threw", custom.GetProperty("data").GetProperty("explodes").GetString()!, StringComparison.Ordinal);
        Assert.AreEqual(2, custom.GetProperty("data").GetProperty("tags").GetArrayLength());
        Assert.AreEqual(4.5, custom.GetProperty("data").GetProperty("n").GetDouble());

        var http = Assert.ContainsSingle(root.GetProperty("http").EnumerateArray());
        Assert.AreEqual(200, http.GetProperty("statusCode").GetInt32());
        Assert.Contains("page=2", http.GetProperty("url").GetString()!, StringComparison.Ordinal);

        var error = Assert.ContainsSingle(e => e.GetProperty("source").GetString() == "test", root.GetProperty("errors").EnumerateArray());
        Assert.AreEqual("exported failure", error.GetProperty("message").GetString());

        Assert.AreEqual(JsonValueKind.Array, root.GetProperty("diagnostics").ValueKind);
        Assert.AreEqual(JsonValueKind.Array, root.GetProperty("state").ValueKind);
        Assert.AreEqual(JsonValueKind.Object, root.GetProperty("jsInterop").ValueKind);
        Assert.AreEqual(JsonValueKind.Object, root.GetProperty("browser").ValueKind);
    }

    [TestMethod]
    public async Task Export_never_contains_redacted_secrets()
    {
        var handler = new BevToolsHttpMessageHandler(Session) { InnerHandler = new StubHandler() };
        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "super-secret-token");
        await client.GetAsync("https://user:pa55@example.test/api?apikey=hunter2");
        var state = new StateStoreTests.CounterState { Password = "p@ss-plain" };
        Session.State.Attach(state, "test");
        state.Increment();

        var json = BevSessionExporter.ToJson(Session);
        var report = BevSessionExporter.ToMarkdownReport(Session);

        foreach (var text in new[] { json, report })
        {
            Assert.DoesNotContain("super-secret-token", text, StringComparison.Ordinal);
            Assert.DoesNotContain("hunter2", text, StringComparison.Ordinal);
            Assert.DoesNotContain("pa55", text, StringComparison.Ordinal);
            Assert.DoesNotContain("p@ss-plain", text, StringComparison.Ordinal);
        }

        Assert.Contains(BevRedactor.RedactedValue, json, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Options_trim_the_largest_sections()
    {
        Render<ParentComponent>();
        var json = BevSessionExporter.ToJson(Session, new BevSessionExportOptions { IncludeTimeline = false, Indented = false });
        using var document = JsonDocument.Parse(json);

        Assert.IsFalse(document.RootElement.TryGetProperty("timeline", out _));
        Assert.DoesNotContain('\n', json);
    }

    [TestMethod]
    public void Markdown_report_summarises_findings_errors_components_and_the_timeline_tail()
    {
        var cut = Render<ParentComponent>();
        cut.InvokeAsync(() => cut.Instance.Increment());
        Session.Errors.Record(new InvalidOperationException("boom in report"), "render", component: Session.Components.All.First(c => c.Type == typeof(ParentComponent)));
        Session.PauseCapture();

        var report = BevSessionExporter.ToMarkdownReport(Session, timelineTail: 5);

        Assert.StartsWith("# BevTools report", report, StringComparison.Ordinal);
        Assert.Contains("capture paused", report, StringComparison.Ordinal);
        Assert.Contains("## Diagnostics", report, StringComparison.Ordinal);
        Assert.Contains("## Errors (1 distinct)", report, StringComparison.Ordinal);
        Assert.Contains("boom in report", report, StringComparison.Ordinal);
        Assert.Contains("| ParentComponent | 2 |", report, StringComparison.Ordinal);
        Assert.Contains("## Timeline (last 5 of", report, StringComparison.Ordinal);
        Assert.Contains("Capture paused", report, StringComparison.Ordinal);
    }

    private sealed class ThrowingToString
    {
        public override string ToString() => throw new InvalidOperationException("no");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });
    }
}
