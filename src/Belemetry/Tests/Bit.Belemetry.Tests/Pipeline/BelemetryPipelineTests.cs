using System.Diagnostics.Metrics;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Bit.Belemetry.Tests.Pipeline;

[TestClass]
public class BelemetryPipelineTests
{
    private static BelemetryLogRecord Log(string body, BelemetryFeatures feature) => new() { Body = body, Feature = feature, TimeUnixNano = Records.Start };

    [TestMethod]
    public async Task Undecided_RecordsAreHeld_ThenReleasedOrDroppedOnDecision()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.Errors);
        var pipeline = host.Pipeline;

        Assert.IsTrue(pipeline.CanRecord(BelemetryFeatures.Errors));
        Assert.IsFalse(pipeline.CanRecord(BelemetryFeatures.Logs));
        Assert.IsTrue(pipeline.CanRecord(BelemetryFeatures.Logs, allowPending: true));

        pipeline.Enqueue(Log("error", BelemetryFeatures.Errors));
        pipeline.Enqueue(Log("log", BelemetryFeatures.Logs));
        pipeline.Enqueue(Log("event", BelemetryFeatures.Events));
        pipeline.Enqueue(Records.Span());

        await host.FlushAsync();
        Assert.AreEqual("error", host.Memory.Logs.Single().Body, "The default grant is sent right away; the rest is held.");
        Assert.IsEmpty(host.Memory.Spans);

        host.Consent.Decide(BelemetryFeatures.Errors | BelemetryFeatures.Logs | BelemetryFeatures.Traces);

        await host.FlushAsync();
        CollectionAssert.AreEquivalent(new[] { "error", "log" }, host.Memory.Logs.Select(l => l.Body).ToArray());
        Assert.HasCount(1, host.Memory.Spans);

        // After the decision, records of features not granted are not even held.
        pipeline.Enqueue(Log("event2", BelemetryFeatures.Events));
        Assert.IsFalse(pipeline.CanRecord(BelemetryFeatures.Events, allowPending: true));
        await host.FlushAsync();
        Assert.HasCount(2, host.Memory.Logs);
    }

    [TestMethod]
    public async Task DecidingNothing_DropsEverythingHeld()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.None);
        host.Pipeline.Enqueue(Log("a", BelemetryFeatures.Logs));
        host.Pipeline.Enqueue(Records.Span());

        host.Consent.Decide(BelemetryFeatures.None);
        host.Consent.Decide(BelemetryFeatures.All);

        await host.FlushAsync();
        Assert.IsEmpty(host.Memory.Logs);
        Assert.IsEmpty(host.Memory.Spans);
    }

    [TestMethod]
    public async Task PendingLimit_KeepsTheNewest()
    {
        await using var host = TestHost.Create(o => o.Consent.PendingLimit = 3, BelemetryFeatures.None);
        for (var i = 0; i < 5; i++) host.Pipeline.Enqueue(Log("m" + i, BelemetryFeatures.Logs));

        host.Consent.Decide(BelemetryFeatures.Logs);

        await host.FlushAsync();
        CollectionAssert.AreEqual(new[] { "m2", "m3", "m4" }, host.Memory.Logs.Select(l => l.Body).ToArray());
    }

    [TestMethod]
    public async Task Revoking_PurgesQueuedRecords()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.None);
        host.Consent.Decide(BelemetryFeatures.Logs | BelemetryFeatures.Errors);

        host.Pipeline.Enqueue(Log("log", BelemetryFeatures.Logs));
        host.Pipeline.Enqueue(Log("error", BelemetryFeatures.Errors));

        host.Consent.Decide(BelemetryFeatures.Errors);

        await host.FlushAsync();
        Assert.AreEqual("error", host.Memory.Logs.Single().Body);
    }

    [TestMethod]
    public async Task Server_CollectsNothingByDefault()
    {
        await using var host = TestHost.Create(o => o.Host = BelemetryHostKind.Server);
        host.StartLogging();

        Assert.IsTrue(host.Settings.IsServer);
        Assert.IsFalse(host.Settings.CollectDotnet);
        Assert.IsFalse(host.Pipeline.IsCollecting);
    }

    [TestMethod]
    public async Task Server_WithCollection_UsesServerFeaturesNotConsent()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Server.CollectDotnetTelemetry = true;
            o.Server.Features = BelemetryFeatures.Errors | BelemetryFeatures.Logs;
        }, consentDefault: BelemetryFeatures.None);
        var logger = host.StartLogging().CreateLogger("MyApp");

        Assert.IsTrue(host.Pipeline.IsCollecting);
        Assert.IsTrue(host.Pipeline.CanRecord(BelemetryFeatures.Logs));
        Assert.IsFalse(host.Pipeline.CanRecord(BelemetryFeatures.Traces, allowPending: true));
        Assert.IsFalse(host.Pipeline.CanRecord(BelemetryFeatures.Events, allowPending: true));

        host.Consent.SetContext("session-1", null, null);
        logger.LogInformation("served");
        host.Pipeline.Enqueue(Records.Span());

        await host.FlushAsync();
        var log = host.Memory.Logs.Single();
        Assert.AreEqual("served", log.Body);
        Assert.IsFalse(log.Attributes.Contains("session.id"), "The process-wide server pipeline is not one user's session.");
        Assert.IsEmpty(host.Memory.Spans);

        // A user's consent decision does not purge operator telemetry.
        logger.LogInformation("second");
        host.Consent.Decide(BelemetryFeatures.None);
        await host.FlushAsync();
        Assert.HasCount(2, host.Memory.Logs);
    }

    [TestMethod]
    public async Task Relay_ConvertsValidatesAndMergesTheResource()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.None);

        var json = """
        {
          "resource": { "browser.language": "en-US", "telemetry.sdk.language": "webjs", "browser.mobile": false },
          "spans": [
            { "traceId": "0AF7651916CD43DD8448EB211C80319C", "spanId": "B7AD6B7169203331", "parentSpanId": "00f067aa0ba902b7", "name": "GET",
              "kind": 3, "startTime": 1700000000000.5, "endTime": 1700000000250.25,
              "attributes": { "http.request.method": "GET", "http.response.status_code": 200, "ratio": 0.5, "codes": [1, 2], "mixed": [1, 2.5], "flags": [true], "names": ["a", 1] },
              "status": { "code": 2, "message": "failed" },
              "events": [ { "name": "first-byte", "time": 1700000000100, "attributes": { "n": 1 } } ],
              "scope": "bit.belemetry.fetch", "feature": 16 },
            { "traceId": "00000000000000000000000000000000", "spanId": "b7ad6b7169203331", "name": "zero trace id" },
            { "traceId": "0af7651916cd43dd8448eb211c80319c", "spanId": "nothex!!nothex!!", "name": "bad span id" },
            { "traceId": "0af7651916cd43dd8448eb211c80319c", "spanId": "1111111111111111", "parentSpanId": "zz", "name": "", "kind": 9, "feature": 3,
              "startTime": 10, "endTime": 5 }
          ],
          "logs": [
            { "time": 1700000000000, "severityNumber": 9, "severityText": "INFO", "body": null, "eventName": "browser.page_view",
              "attributes": { "url.full": "https://app.test/p?token=abc" }, "traceId": "bad", "spanId": null, "scope": "bit.belemetry.page-views", "feature": 2 },
            { "time": 1, "severityNumber": 99, "feature": 0 }
          ]
        }
        """;
        var batch = JsonSerializer.Deserialize(json, BelemetryJsonContext.Default.BelemetryRelayBatch)!;

        host.Pipeline.Relay(batch);
        await host.FlushAsync();

        Assert.AreEqual("en-US", host.Memory.LastResource!.Attributes["browser.language"]);
        Assert.AreEqual("webjs", host.Memory.LastResource.Attributes["telemetry.sdk.language"], "The browser resource overlays the .NET one.");
        Assert.AreEqual("belemetry-tests", host.Memory.LastResource.ServiceName);

        var spans = host.Memory.Spans.ToList();
        Assert.HasCount(2, spans, "Spans with invalid ids are dropped.");

        var span = spans[0];
        Assert.AreEqual(Records.TraceId, span.TraceId);
        Assert.AreEqual(Records.SpanId, span.SpanId);
        Assert.AreEqual(Records.ParentSpanId, span.ParentSpanId);
        Assert.AreEqual(BelemetrySpanKind.Client, span.Kind);
        Assert.AreEqual(1_700_000_000_000_500_000L, span.StartTimeUnixNano);
        Assert.AreEqual(1_700_000_000_250_250_000L, span.EndTimeUnixNano);
        Assert.AreEqual(BelemetryStatusCode.Error, span.Status);
        Assert.AreEqual("failed", span.StatusMessage);
        Assert.AreEqual("bit.belemetry.fetch", span.Scope.Name);
        Assert.AreEqual(BelemetryFeatures.Dependencies, span.Feature);
        Assert.AreEqual(200L, span.Attributes["http.response.status_code"]);
        Assert.AreEqual(0.5, span.Attributes["ratio"]);
        CollectionAssert.AreEqual(new[] { 1L, 2L }, (long[])span.Attributes["codes"]!);
        CollectionAssert.AreEqual(new[] { 1.0, 2.5 }, (double[])span.Attributes["mixed"]!);
        CollectionAssert.AreEqual(new[] { true }, (bool[])span.Attributes["flags"]!);
        CollectionAssert.AreEqual(new[] { "a", "1" }, (string[])span.Attributes["names"]!);
        Assert.AreEqual("first-byte", span.Events.Single().Name);
        Assert.AreEqual(1_700_000_000_100_000_000L, span.Events[0].TimeUnixNano);
        Assert.AreEqual(1L, span.Events[0].Attributes["n"]);

        var fallback = spans[1];
        Assert.IsNull(fallback.ParentSpanId);
        Assert.AreEqual("span", fallback.Name);
        Assert.AreEqual(BelemetrySpanKind.Internal, fallback.Kind);
        Assert.AreEqual(BelemetryFeatures.Dependencies, fallback.Feature, "A feature that is not exactly one bit falls back.");
        Assert.AreEqual("bit.belemetry.browser", fallback.Scope.Name);
        Assert.AreEqual(fallback.StartTimeUnixNano, fallback.EndTimeUnixNano, "The end never precedes the start.");

        var logs = host.Memory.Logs.ToList();
        Assert.HasCount(2, logs);
        Assert.AreEqual(BelemetryFeatures.PageViews, logs[0].Feature);
        Assert.AreEqual("browser.page_view", logs[0].EventName);
        Assert.AreEqual(BelemetrySeverity.Info, logs[0].Severity);
        Assert.IsNull(logs[0].TraceId);
        Assert.AreEqual("https://app.test/p?token=REDACTED", logs[0].Attributes["url.full"], "Privacy rules apply to relayed records too.");
        Assert.AreEqual(BelemetryFeatures.Logs, logs[1].Feature);
        Assert.AreEqual(24, (int)logs[1].Severity);
    }

    [TestMethod]
    public async Task Relay_IsIgnoredWhenDisabled()
    {
        await using var host = TestHost.Create(o => o.Enabled = false);
        host.Pipeline.Relay(new BelemetryRelayBatch { Logs = [new BelemetryRelayLog { Body = "x", Feature = 128 }] });
        await host.FlushAsync();
        Assert.IsEmpty(host.Memory.Logs);
    }

    [TestMethod]
    public async Task DrainForUnload_SerializesQueuedRecordsForTheHttpExporters()
    {
        await using var host = TestHost.Create(o => o.AddOtlpExporter("http://collector.test:4318", e => e.Headers["x-key"] = "k"));
        host.Pipeline.Enqueue(Records.Span());
        host.Pipeline.Enqueue(Log("bye", BelemetryFeatures.Logs));

        var requests = host.Pipeline.DrainForUnload();

        Assert.HasCount(2, requests);
        var traces = requests.Single(r => r.Url == "http://collector.test:4318/v1/traces");
        var logs = requests.Single(r => r.Url == "http://collector.test:4318/v1/logs");
        Assert.AreEqual("otlp", traces.Exporter);
        Assert.AreEqual("application/x-protobuf", traces.ContentType);
        Assert.AreEqual("k", traces.Headers["x-key"]);
        Assert.AreEqual(Records.SpanId, Convert.ToHexString(ExportTraceServiceRequest.Parser.ParseFrom(traces.Body).ResourceSpans[0].ScopeSpans[0].Spans[0].SpanId.ToByteArray()).ToLowerInvariant());
        Assert.AreEqual("bye", ExportLogsServiceRequest.Parser.ParseFrom(logs.Body).ResourceLogs[0].ScopeLogs[0].LogRecords[0].Body.StringValue);

        // Drained records are gone: nothing is left for the regular exporters.
        await host.FlushAsync();
        Assert.IsEmpty(host.Memory.Logs);
        Assert.IsEmpty(host.Pipeline.DrainForUnload());

        // The request crosses to JS as camelCase JSON with a base64 body.
        var element = JsonSerializer.SerializeToElement(requests, BelemetryJsonContext.Default.ListBelemetryHttpRequest);
        var first = element[0];
        Assert.AreEqual(Convert.ToBase64String(requests[0].Body), first.GetProperty("body").GetString());
        Assert.AreEqual("otlp", first.GetProperty("exporter").GetString());
        Assert.IsTrue(first.TryGetProperty("contentType", out _));
        Assert.IsFalse(first.TryGetProperty("contentEncoding", out _));
    }

    [TestMethod]
    public async Task Processors_CanDropAndMutate()
    {
        var processor = new TestProcessor();
        await using var host = TestHost.Create(o =>
        {
            o.AddProcessor(new ThrowingProcessor());
            o.AddProcessor(processor);
        });

        host.Pipeline.Enqueue(Log("keep", BelemetryFeatures.Logs));
        host.Pipeline.Enqueue(Log("drop", BelemetryFeatures.Logs));
        host.Pipeline.Enqueue(Records.Span(name: "keep"));
        host.Pipeline.Enqueue(Records.Span(name: "drop"));

        await host.FlushAsync();
        var log = host.Memory.Logs.Single();
        Assert.AreEqual("keep", log.Body);
        Assert.AreEqual(true, log.Attributes["processed"]);
        var span = host.Memory.Spans.Single();
        Assert.AreEqual("keep", span.Name);
        Assert.AreEqual(true, span.Attributes["processed"]);
    }

    [TestMethod]
    public async Task MetricProcessors_CanDrop_AndMetricsNeedConsent()
    {
        using var meter = new Meter("Tests.Pipeline." + Guid.NewGuid().ToString("N"));
        await using var host = TestHost.Create(o =>
        {
            o.Metrics.Meters.Add(meter.Name);
            o.AddProcessor(new TestProcessor());
        }, consentDefault: BelemetryFeatures.None);
        host.StartLogging();

        var kept = meter.CreateCounter<int>("kept");
        kept.Add(1);
        meter.CreateCounter<int>("drop").Add(1);

        await host.Pipeline.FlushAsync(includeMetrics: true, CancellationToken.None);
        Assert.IsEmpty(host.Memory.Metrics, "No Metrics consent yet.");

        host.Consent.Decide(BelemetryFeatures.Metrics);
        kept.Add(1);
        await host.Pipeline.FlushAsync(includeMetrics: true, CancellationToken.None);

        var metric = host.Memory.Metrics.Single();
        Assert.AreEqual("kept", metric.Name);
        Assert.AreEqual(2L, metric.Points[0].LongValue);
        Assert.AreEqual(1L, metric.Points[0].DeltaLongValue, "Collected while not consented, so the deltas stay aligned.");
    }

    [TestMethod]
    public async Task Enrichment_AddsSessionAndUserOnlyWithUserTracking()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.None);
        host.Consent.Decide(BelemetryFeatures.Logs);
        host.Consent.SetContext("s-1", "pseudo-1", "user-1");
        host.Pipeline.Enqueue(Log("a", BelemetryFeatures.Logs));

        host.Consent.Decide(BelemetryFeatures.Logs | BelemetryFeatures.UserTracking);
        host.Consent.SetContext(null, "pseudo-1", "user-1");
        host.Pipeline.Enqueue(Log("b", BelemetryFeatures.Logs));

        await host.FlushAsync();
        var logs = host.Memory.Logs.ToList();
        Assert.AreEqual("s-1", logs[0].Attributes["session.id"]);
        Assert.IsFalse(logs[0].Attributes.Contains("enduser.pseudo.id"));
        Assert.IsFalse(logs[0].Attributes.Contains("user.id"));
        Assert.AreEqual("s-1", logs[1].Attributes["session.id"]);
        Assert.AreEqual("pseudo-1", logs[1].Attributes["enduser.pseudo.id"]);
        Assert.AreEqual("user-1", logs[1].Attributes["user.id"]);

        host.Consent.Decide(BelemetryFeatures.Logs);
        Assert.IsNull(host.Consent.PseudoId, "Revoking UserTracking forgets the ids.");
        Assert.IsNull(host.Consent.UserId);
    }

    [TestMethod]
    public async Task MaxQueueSize_DropsTheOldestOfTheSameSignal()
    {
        await using var host = TestHost.Create(o => o.Batch.MaxQueueSize = 2);
        for (var i = 0; i < 4; i++) host.Pipeline.Enqueue(Log("m" + i, BelemetryFeatures.Logs));
        host.Pipeline.Enqueue(Records.Span());

        await host.FlushAsync();
        CollectionAssert.AreEqual(new[] { "m2", "m3" }, host.Memory.Logs.Select(l => l.Body).ToArray());
        Assert.HasCount(1, host.Memory.Spans);
    }

    [TestMethod]
    public async Task Flush_SplitsIntoBatchesPerResource()
    {
        var batches = new List<BelemetryBatch>();
        await using var host = TestHost.Create(o => o.Batch.MaxBatchSize = 2);
        host.Memory.Exported += batches.Add;

        var other = new BelemetryResource(new BelemetryAttributes().Set("service.name", "browser"));
        host.Pipeline.Enqueue(Log("a", BelemetryFeatures.Logs));
        host.Pipeline.Enqueue(Log("b", BelemetryFeatures.Logs), other);
        host.Pipeline.Enqueue(Log("c", BelemetryFeatures.Logs));

        await host.FlushAsync();
        Assert.HasCount(3, host.Memory.Logs);
        Assert.IsTrue(batches.All(b => b.Logs.Count <= 2));
        Assert.IsTrue(batches.Any(b => b.Resource == other && b.Logs.Single().Body == "b"));
    }

    [TestMethod]
    public async Task Exports_RunSuppressed()
    {
        var exporter = new SuppressionProbe();
        await using var host = TestHost.Create(o => o.AddExporter(exporter));
        host.Pipeline.Enqueue(Log("a", BelemetryFeatures.Logs));

        await host.FlushAsync();
        Assert.IsTrue(exporter.SawSuppression);
        Assert.IsFalse(BelemetrySuppression.IsActive);
    }

    [TestMethod]
    public async Task Api_TrackCallsGoThroughThePipeline()
    {
        await using var host = TestHost.Create();
        using var scope = host.Services.CreateScope();
        var api = (IBelemetry)ActivatorUtilities.CreateInstance<BelemetryService>(scope.ServiceProvider, new BelemetryJsBridge(
            new Bunit.BunitJSInterop().JSRuntime, host.Settings, host.Pipeline, host.Consent, host.Services.GetRequiredService<BelemetryBrowserBridgeRegistry>()));

        api.TrackEvent("checkout", new Dictionary<string, object?> { ["amount"] = 9.5 });
        api.TrackPageView("Home", "https://app.test/home?x=1");
        api.TrackException(new InvalidOperationException("nope"));

        await host.FlushAsync();
        var logs = host.Memory.Logs.ToList();
        Assert.AreEqual("checkout", logs[0].EventName);
        Assert.AreEqual(BelemetryFeatures.Events, logs[0].Feature);
        Assert.AreEqual(9.5, logs[0].Attributes["amount"]);
        Assert.AreEqual("browser.page_view", logs[1].EventName);
        Assert.AreEqual("/home", logs[1].Attributes["url.path"]);
        Assert.AreEqual("Home", logs[1].Attributes["page.title"]);
        Assert.AreEqual(BelemetryFeatures.PageViews, logs[1].Feature);
        Assert.AreEqual("exception", logs[2].EventName);
        Assert.AreEqual(BelemetryFeatures.Errors, logs[2].Feature);
        Assert.AreEqual("System.InvalidOperationException", logs[2].Attributes["exception.type"]);
    }

    private sealed class TestProcessor : IBelemetryProcessor
    {
        public bool OnLog(BelemetryLogRecord log)
        {
            log.Attributes.Set("processed", true);
            return log.Body != "drop";
        }

        public bool OnSpan(BelemetrySpan span)
        {
            span.Attributes.Set("processed", true);
            return span.Name != "drop";
        }

        public bool OnMetric(BelemetryMetric metric) => metric.Name != "drop";
    }

    private sealed class ThrowingProcessor : IBelemetryProcessor
    {
        public bool OnLog(BelemetryLogRecord log) => throw new InvalidOperationException();

        public bool OnSpan(BelemetrySpan span) => throw new InvalidOperationException();
    }

    private sealed class SuppressionProbe : IBelemetryExporter
    {
        public string Name => "probe";

        public bool SawSuppression { get; private set; }

        public ValueTask<BelemetryExportResult> ExportAsync(BelemetryBatch batch, CancellationToken cancellationToken)
        {
            SawSuppression = BelemetrySuppression.IsActive;
            return ValueTask.FromResult(BelemetryExportResult.Success);
        }
    }
}
