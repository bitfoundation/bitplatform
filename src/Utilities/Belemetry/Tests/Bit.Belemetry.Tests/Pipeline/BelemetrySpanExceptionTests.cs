namespace Bit.Belemetry.Tests.Pipeline;

/// <summary>The exceptions recorded on a span (<c>Activity.AddException</c>) are Errors records, whichever span carries them.</summary>
[TestClass]
public class BelemetrySpanExceptionTests
{
    private static BelemetrySpan Span(string spanId = Records.SpanId, string message = "slow")
    {
        var span = Records.Span();
        span.SpanId = spanId;
        span.Events.Add(new BelemetrySpanEvent { Name = "retry", TimeUnixNano = Records.Start });
        span.Events.Add(new BelemetrySpanEvent
        {
            Name = "exception",
            TimeUnixNano = Records.Start,
            Attributes = new BelemetryAttributes().Set("exception.type", "System.TimeoutException").Set("exception.message", message).Set("exception.stacktrace", "at Y()"),
        });
        return span;
    }

    private static BelemetryLogRecord LoggedUnder(string spanId, string message = "slow", string stack = "at Y()")
    {
        var log = Records.Log("Charge failed", severity: BelemetrySeverity.Error);
        log.Feature = BelemetryFeatures.Errors;
        log.TraceId = Records.TraceId;
        log.SpanId = spanId;
        log.Attributes.Set("exception.type", "System.TimeoutException").Set("exception.message", message).Set("exception.stacktrace", stack);
        return log;
    }

    private static int AzureItems(BelemetrySpan span, params BelemetryLogRecord[] logs)
    {
        AzureMonitorEnvelopeSerializer.Serialize(new BelemetryBatch(Records.Resource(), [span], logs), new AzureMonitorEnvelopeSerializer.Context("ikey", true), out var count);
        return count;
    }

    private static string[] EventNames(BelemetrySpan span) => span.Events.Select(e => e.Name).ToArray();

    [TestMethod]
    public async Task WithoutErrors_TheExceptionsComeOffTheSpan_TheRestStays()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.None);
        host.Consent.Decide(BelemetryFeatures.Traces);

        host.Pipeline.Enqueue(Span());
        await host.FlushAsync();

        CollectionAssert.AreEqual(new[] { "retry" }, EventNames(host.Memory.Spans.Single()));
    }

    [TestMethod]
    public async Task WithoutErrors_AStackTraceAlone_ComesOffTheSpanToo()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.None);
        host.Consent.Decide(BelemetryFeatures.Traces);

        var span = Records.Span();
        span.Events.Add(new BelemetrySpanEvent { Name = "exception", Attributes = new BelemetryAttributes().Set("exception.stacktrace", "at Y()") });
        host.Pipeline.Enqueue(span);
        await host.FlushAsync();

        Assert.IsEmpty(host.Memory.Spans.Single().Events, "A stack trace is Errors data, with or without a type or message.");
    }

    [TestMethod]
    public async Task BeforeADecisionOnErrors_ASpanWithExceptions_Waits()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.Traces);

        host.Pipeline.Enqueue(Span("1111111111111111"));
        var plain = Records.Span();
        plain.SpanId = "2222222222222222";
        host.Pipeline.Enqueue(plain);
        await host.FlushAsync();
        Assert.AreEqual("2222222222222222", host.Memory.Spans.Single().SpanId, "Traces is granted by default; its exceptions wait for a decision on Errors.");

        host.Consent.Decide(BelemetryFeatures.Traces);
        await host.FlushAsync();
        var held = host.Memory.Spans.Single(s => s.SpanId == "1111111111111111");
        CollectionAssert.AreEqual(new[] { "retry" }, EventNames(held), "Errors was not granted: the span goes without its exceptions.");
    }

    [TestMethod]
    public async Task ASpanReleasedByTheDecision_IsAccepted_ForLiveMetrics()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.Traces);
        var accepted = new List<object>();
        host.Pipeline.RecordAccepted += accepted.Add;

        var span = Span();
        host.Pipeline.Enqueue(span);
        Assert.IsEmpty(accepted, "Held for a decision on Errors.");

        host.Consent.Decide(BelemetryFeatures.Traces | BelemetryFeatures.Errors);
        CollectionAssert.AreEqual(new object[] { span }, accepted);
    }

    [TestMethod]
    public async Task ARevoke_WaitsForTheHandlersOfTheRecordAccepted()
    {
        await using var host = TestHost.Create();
        BelemetrySpan? accepted = null;
        var eventsSeen = -1;
        Task? revoke = null;
        host.Pipeline.RecordAccepted += record =>
        {
            accepted = (BelemetrySpan)record;
            revoke = Task.Run(() => host.Consent.Decide(BelemetryFeatures.All & ~BelemetryFeatures.Errors));
            Assert.IsFalse(revoke.Wait(200), "Live Metrics reads the record before a revoke changes it.");
            eventsSeen = accepted.Events.Count;
        };

        host.Pipeline.Enqueue(Span());
        await revoke!;

        Assert.AreEqual(2, eventsSeen);
        CollectionAssert.AreEqual(new[] { "retry" }, EventNames(accepted!), "Then the revoke takes the exception off the queued span.");
    }

    [TestMethod]
    public async Task BeforeADecisionOnErrors_ASpanWithExceptions_KeepsThemWhenErrorsIsGranted()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.Traces);

        host.Pipeline.Enqueue(Span());
        host.Consent.Decide(BelemetryFeatures.Traces | BelemetryFeatures.Errors);
        await host.FlushAsync();

        CollectionAssert.AreEqual(new[] { "retry", "exception" }, EventNames(host.Memory.Spans.Single()));
    }

    [TestMethod]
    public async Task RevokingErrors_TakesTheExceptionsOffQueuedSpans()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.None);
        host.Consent.Decide(BelemetryFeatures.All);

        host.Pipeline.Enqueue(Span());
        host.Consent.Decide(BelemetryFeatures.All & ~BelemetryFeatures.Errors);
        await host.FlushAsync();

        CollectionAssert.AreEqual(new[] { "retry" }, EventNames(host.Memory.Spans.Single()), "The span stays: its feature is Traces.");
    }

    [TestMethod]
    public async Task OnAServer_TheOperatorsErrorsFeatureDecides()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Server.Features = BelemetryFeatures.Traces;
        }, consentDefault: BelemetryFeatures.None);

        host.Pipeline.Enqueue(Span());
        await host.FlushAsync();

        CollectionAssert.AreEqual(new[] { "retry" }, EventNames(host.Memory.Spans.Single()));
    }

    [TestMethod]
    public async Task TheErrorLimits_ApplyToSpanExceptions_WhichStayOnTheSpan()
    {
        await using var host = TestHost.Create();

        host.Pipeline.Enqueue(Span("1111111111111111"));
        host.Pipeline.Enqueue(Span("2222222222222222"));
        host.Pipeline.Enqueue(Span("3333333333333333", message: "other"));
        await host.FlushAsync();

        var exceptions = host.Memory.Spans.Select(s => s.Events.Single(e => e.IsException)).ToArray();
        CollectionAssert.AreEqual(new[] { false, true, false }, exceptions.Select(e => e.Limited).ToArray(),
            "A repeat within the duplicate window is not an error record of its own; another exception is.");
    }

    [TestMethod]
    public async Task TheErrorRateCap_AppliesToSpanExceptions_WithoutTheDuplicateWindow()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Errors.DuplicateWindow = TimeSpan.Zero;
            o.Errors.MaxPerMinute = 2;
        });

        for (var i = 0; i < 4; i++) host.Pipeline.Enqueue(Span($"{i + 1}{i + 1}{i + 1}{i + 1}000000000000", message: $"failure {i}"));
        await host.FlushAsync();

        var exceptions = host.Memory.Spans.Select(s => s.Events.Single(e => e.IsException)).ToArray();
        CollectionAssert.AreEqual(new[] { false, false, true, true }, exceptions.Select(e => e.Limited).ToArray(),
            "Distinct exceptions take the per-minute tokens one each; past the burst they are not error records of their own.");
    }

    [TestMethod]
    public async Task AnExceptionLoggedUnderTheSpan_IsLinkedToIt_AndSentToAzureMonitorOnce()
    {
        await using var host = TestHost.Create();

        host.Pipeline.Enqueue(LoggedUnder(Records.SpanId));
        host.Pipeline.Enqueue(LoggedUnder("2222222222222222", message: "elsewhere"));
        host.Pipeline.Enqueue(Span());
        await host.FlushAsync();

        var exception = host.Memory.Spans.Single().Events.Single(e => e.IsException);
        Assert.IsNotNull(exception.LoggedAs);
        Assert.AreEqual(BelemetrySeverity.Error, exception.LoggedAs.Severity);
        Assert.IsFalse(exception.LoggedAs.IsEvent);
        Assert.IsFalse(exception.LoggedAs.IsDropped);
        Assert.IsFalse(exception.Limited, "The log record took its place under the error limits.");

        Assert.AreEqual(3, AzureItems(host.Memory.Spans.Single(), host.Memory.Logs.First(l => l.SpanId == Records.SpanId)), "The span, its other event, and one Exception: the log's.");

        // Taken once: a later span with the same id (none should exist) finds nothing left.
        host.Pipeline.Enqueue(Span());
        await host.FlushAsync();
        Assert.IsNull(host.Memory.Spans.Last().Events.Single(e => e.IsException).LoggedAs);
    }

    [TestMethod]
    public async Task ADifferentExceptionLoggedUnderTheSpan_IsNotLinked()
    {
        await using var host = TestHost.Create();

        host.Pipeline.Enqueue(LoggedUnder(Records.SpanId, message: "other"));
        host.Pipeline.Enqueue(Span());
        await host.FlushAsync();

        Assert.IsNull(host.Memory.Spans.Single().Events.Single(e => e.IsException).LoggedAs);
    }

    [TestMethod]
    public async Task AnotherFailureWithTheSameTypeAndMessage_IsNotLinked()
    {
        await using var host = TestHost.Create();

        host.Pipeline.Enqueue(LoggedUnder(Records.SpanId, stack: "at Z()"));
        host.Pipeline.Enqueue(Span());
        await host.FlushAsync();

        Assert.IsNull(host.Memory.Spans.Single().Events.Single(e => e.IsException).LoggedAs, "Thrown elsewhere: another failure, sent on its own.");
    }

    [TestMethod]
    public async Task WithoutAStackTrace_ALoggedException_IsNotLinked()
    {
        await using var host = TestHost.Create();

        var log = LoggedUnder(Records.SpanId);
        log.Attributes.Remove("exception.stacktrace");
        var span = Span();
        span.Events.Single(e => e.IsException).Attributes.Remove("exception.stacktrace");
        host.Pipeline.Enqueue(log);
        host.Pipeline.Enqueue(span);
        await host.FlushAsync();

        Assert.IsNull(host.Memory.Spans.Single().Events.Single(e => e.IsException).LoggedAs, "Its identity is unknown: the span keeps its exception.");
    }

    [TestMethod]
    public async Task ALoggedExceptionDroppedBeforeExport_LeavesTheExceptionToTheSpan()
    {
        await using var host = TestHost.Create(consentDefault: BelemetryFeatures.None);
        host.Consent.Decide(BelemetryFeatures.All);

        var log = LoggedUnder(Records.SpanId);
        host.Pipeline.Enqueue(log);

        // Revoked before the export (the queued log is purged), granted again before the span ends.
        host.Consent.Decide(BelemetryFeatures.All & ~BelemetryFeatures.Errors);
        host.Consent.Decide(BelemetryFeatures.All);
        host.Pipeline.Enqueue(Span());
        await host.FlushAsync();

        Assert.IsEmpty(host.Memory.Logs);
        var span = host.Memory.Spans.Single();
        Assert.IsTrue(span.Events.Single(e => e.IsException).LoggedAs?.IsDropped);
        Assert.AreEqual(3, AzureItems(span), "The span, its other event, and its Exception: the log never left.");
    }

    [TestMethod]
    public async Task ALoggedExceptionTheQueueDrops_LeavesTheExceptionToTheSpan()
    {
        await using var host = TestHost.Create(o => o.Batch.MaxQueueSize = 1);

        host.Pipeline.Enqueue(LoggedUnder(Records.SpanId));
        host.Pipeline.Enqueue(Span());
        host.Pipeline.Enqueue(Records.Log("Newer"));
        await host.FlushAsync();

        Assert.AreEqual("Newer", host.Memory.Logs.Single().Body);
        Assert.AreEqual(3, AzureItems(host.Memory.Spans.Single()), "The span, its other event, and its Exception: the log was dropped.");
    }

    [TestMethod]
    public async Task ARelayedSpan_KeepsItsExceptions_ThePageAppliedConsent()
    {
        await using var host = TestHost.Create(o => o.Browser.Transport = BelemetryBrowserTransport.Relay, consentDefault: BelemetryFeatures.None);
        host.Consent.Decide(BelemetryFeatures.Traces);

        host.Pipeline.Enqueue(Span(), relayed: true);
        await host.FlushAsync();

        var exception = host.Memory.Spans.Single().Events.Single(e => e.IsException);
        Assert.IsFalse(exception.Limited);
    }

    [TestMethod]
    public async Task ARelayedException_ThePagesErrorLimitsDropped_StaysLimited()
    {
        await using var host = TestHost.Create();
        var json = """
        { "spans": [ { "traceId": "0af7651916cd43dd8448eb211c80319c", "spanId": "b7ad6b7169203331", "name": "GET", "kind": 3, "startTime": 1, "endTime": 2, "feature": 16,
                       "events": [ { "name": "exception", "time": 1, "attributes": { "exception.type": "TypeError" }, "limited": true },
                                   { "name": "exception", "time": 1, "attributes": { "exception.type": "RangeError" } } ] } ] }
        """;

        host.Pipeline.Relay(System.Text.Json.JsonSerializer.Deserialize(json, BelemetryJsonContext.Default.BelemetryRelayBatch)!);
        await host.FlushAsync();

        var span = host.Memory.Spans.Single();
        CollectionAssert.AreEqual(new[] { true, false }, span.Events.Select(e => e.Limited).ToArray());
        Assert.AreEqual(2, AzureItems(span), "The span and the exception the page did not limit.");
    }

    [TestMethod]
    public async Task ARelayedSpan_LosesItsExceptions_WhenTheConfigurationSwitchesErrorsOff()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Browser.Transport = BelemetryBrowserTransport.Relay;
            o.Features = BelemetryFeatures.All & ~BelemetryFeatures.Errors;
        });

        host.Pipeline.Enqueue(Span(), relayed: true);
        await host.FlushAsync();

        CollectionAssert.AreEqual(new[] { "retry" }, EventNames(host.Memory.Spans.Single()), "A page can be anything that speaks the interop protocol.");
    }

    [TestMethod]
    public async Task ARevokeOfErrorsDuringAnExport_WithdrawsASpanWithExceptions_CountedAsItsItems()
    {
        var handler = new FakeHttpMessageHandler();
        await using var host = TestHost.Create(o =>
        {
            o.Metrics.Meters.Add(BelemetrySelfTelemetry.MeterName);
            o.AddOtlpExporter("http://collector.test:4318", e => e.Signals = BelemetrySignals.Traces);
            o.AddAzureMonitorExporter("InstrumentationKey=11111111-2222-3333-4444-555555555555;IngestionEndpoint=https://ingest.test/", e => e.EnableLiveMetrics = false);
        }, configureServices: s => s.AddSingleton(new BelemetryHttpMessageHandlerProvider(handler)));
        handler.Enqueue(System.Net.HttpStatusCode.ServiceUnavailable, r =>
        {
            r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
            host.Consent.Decide(BelemetryFeatures.All & ~BelemetryFeatures.Errors);
        });
        host.StartLogging();

        host.Pipeline.Enqueue(Span());
        await host.FlushAsync();
        Assert.HasCount(1, handler.Requests, "Neither retried nor sent to Azure Monitor.");

        // Azure Monitor gets the metrics (the self-telemetry among them); OTLP sends traces only.
        await host.FlushAsync(includeMetrics: true);
        var exported = host.Memory.Metrics.Last(m => m.Name == "belemetry.records.exported").Points;
        long Withdrawn(string exporter) => exported.Single(p => p.Attributes.GetString("belemetry.exporter") == exporter && p.Attributes.GetString("belemetry.outcome") == "withdrawn").LongValue;
        Assert.AreEqual(1, Withdrawn("otlp"));
        Assert.AreEqual(3, Withdrawn("azuremonitor"), "Withdrawn before it was serialized, in the unit of a delivery: the span and its 2 events.");
    }
}
