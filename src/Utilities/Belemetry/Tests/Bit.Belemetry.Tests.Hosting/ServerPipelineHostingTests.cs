using System.Net;
using Bit.Belemetry.Tests.Harness;
using Bit.Belemetry.Tests.Harness.Web.Collector;
using Bit.Belemetry.Tests.Hosting.Infrastructure;
using Modes = Bit.Belemetry.Tests.Harness.Web.HarnessRenderModes;

namespace Bit.Belemetry.Tests.Hosting;

/// <summary>
/// The server pipeline (<see cref="BelemetryServerOptions.CollectDotnetTelemetry"/>): off by default, a web host only
/// serves the browser script; on, its own ILogger, Activity and Meter data is exported. Each test owns its host, so its
/// receiver only holds its own exports.
/// </summary>
[TestClass]
public class ServerPipelineHostingTests
{
    private static readonly TimeSpan _exportTimeout = TimeSpan.FromSeconds(20);

    [TestMethod]
    public async Task With_the_server_pipeline_on_a_server_log_span_and_metric_reach_the_receiver()
    {
        await using var factory = new HarnessHostFactory(Modes.Ssr, serverPipeline: true);
        var marker = Guid.NewGuid().ToString("N");

        using var response = await factory.CreateClient().GetAsync($"/test-api/server-telemetry?marker={marker}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var span = await Eventually.WaitAsync(
            () => factory.Collector.Snapshot(marker: marker).Spans.FirstOrDefault(s => s.Name == "harness.server-activity"),
            _exportTimeout, () => Describe(factory.Collector));
        var log = await Eventually.WaitAsync(
            () => factory.Collector.Snapshot(marker: marker).Logs.FirstOrDefault(l => l.Body == $"Harness server log {marker}"),
            _exportTimeout, () => Describe(factory.Collector));

        Assert.AreEqual(HarnessTelemetry.Name, span.Scope);
        Assert.AreEqual("dotnet", span.Resource.GetValueOrDefault("telemetry.sdk.language"));
        Assert.AreEqual("belemetry-harness", span.Resource.GetValueOrDefault("service.name"));

        // The log was written inside the activity, so it is correlated with it.
        Assert.AreEqual(HarnessTelemetry.Name, log.Scope, "An app category under Bit.Belemetry.* is collected.");
        Assert.AreEqual(span.TraceId, log.TraceId);
        Assert.AreEqual(span.SpanId, log.SpanId);

        // A server host adds the Microsoft.AspNetCore source: the request span is the parent of the harness span.
        var request = await Eventually.WaitAsync(
            () => factory.Collector.Snapshot().Spans.FirstOrDefault(s => s.SpanId == span.ParentSpanId),
            _exportTimeout, () => Describe(factory.Collector));
        Assert.AreEqual(2, request.Kind, "The parent of the harness span is not a SERVER span.");

        // Belemetry fills in the HTTP conventions ASP.NET Core leaves off its request activity.
        Assert.AreEqual("GET /test-api/server-telemetry", request.Name);
        Assert.AreEqual("GET", request.Attributes.GetValueOrDefault("http.request.method"));
        Assert.AreEqual("/test-api/server-telemetry", request.Attributes.GetValueOrDefault("http.route"));
        Assert.AreEqual("200", request.Attributes.GetValueOrDefault("http.response.status_code"));

        // The same records went out as Azure Monitor envelopes too.
        await Eventually.WaitAsync(
            () => factory.Collector.Snapshot(marker: marker).Envelopes.FirstOrDefault(e => e.Name == "Microsoft.ApplicationInsights.Message"),
            _exportTimeout, () => Describe(factory.Collector));
        await Eventually.WaitAsync(
            () => factory.Collector.Snapshot(marker: marker).Envelopes.FirstOrDefault(e => e.Name == "Microsoft.ApplicationInsights.RemoteDependency"),
            _exportTimeout, () => Describe(factory.Collector));

        // Metrics go out on their own interval (2 seconds in the harness).
        await Eventually.WaitAsync(
            () => factory.Collector.Snapshot(marker: marker).Metrics.FirstOrDefault(m => m.Name == "harness.clicks"),
            _exportTimeout, () => Describe(factory.Collector));
    }

    [TestMethod]
    public async Task With_the_server_pipeline_on_the_exports_do_not_trace_themselves()
    {
        await using var factory = new HarnessHostFactory(Modes.Ssr, serverPipeline: true);

        using var response = await factory.CreateClient().GetAsync($"/test-api/server-telemetry?marker={Guid.NewGuid():N}");
        await Eventually.WaitAsync(() => factory.Collector.Snapshot().Requests.Count >= 2 ? factory.Collector : null, _exportTimeout, () => Describe(factory.Collector));
        await Task.Delay(TimeSpan.FromSeconds(2));

        var spans = factory.Collector.Snapshot().Spans;
        // The exporter's own HttpClient calls run suppressed, and the receiver's server spans are filtered out by the
        // harness's processor - otherwise every export would be exported by the next one, forever.
        Assert.IsFalse(spans.Any(s => s.Scope == "System.Net.Http"), $"An export was traced: {string.Join(", ", spans.Select(s => s.Name))}");
        Assert.IsFalse(spans.Any(s => (s.Attributes.GetValueOrDefault("url.path") ?? s.Attributes.GetValueOrDefault("http.route"))?.StartsWith(HarnessCollectorFilter.CollectorPath, StringComparison.Ordinal) is true),
            "A request to the receiver was exported.");
    }

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    [DataRow(Modes.Auto)]
    public async Task By_default_nothing_from_dotnet_is_exported_on_the_server(string mode)
    {
        await using var factory = new HarnessHostFactory(mode);
        using var client = factory.CreateClient();

        using (var page = await client.GetAsync("/")) Assert.AreEqual(HttpStatusCode.OK, page.StatusCode);
        using (var telemetry = await client.GetAsync($"/test-api/server-telemetry?marker={Guid.NewGuid():N}")) Assert.AreEqual(HttpStatusCode.OK, telemetry.StatusCode);

        // Several flush intervals (500 ms), and a metrics interval (2 s).
        await Task.Delay(TimeSpan.FromSeconds(3));

        var received = factory.Collector.Snapshot();
        Assert.AreEqual(0, received.Requests.Count, $"The server exported with CollectDotnetTelemetry off. {Describe(factory.Collector)}");
    }

    private static string Describe(HarnessCollector collector)
    {
        var all = collector.Snapshot();
        return $"Received {all.Requests.Count} request(s): spans [{string.Join(", ", all.Spans.Select(s => s.Name).Distinct())}], " +
               $"logs [{string.Join(" | ", all.Logs.Select(l => l.Body).Take(20))}], metrics [{string.Join(", ", all.Metrics.Select(m => m.Name).Distinct())}], " +
               $"envelopes [{string.Join(", ", all.Envelopes.Select(e => e.Name).Distinct())}].";
    }
}
