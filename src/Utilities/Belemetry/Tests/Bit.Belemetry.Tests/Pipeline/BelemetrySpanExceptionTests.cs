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

    private static BelemetryLogRecord LoggedUnder(string spanId, string message = "slow")
    {
        var log = Records.Log("Charge failed", severity: BelemetrySeverity.Error);
        log.Feature = BelemetryFeatures.Errors;
        log.TraceId = Records.TraceId;
        log.SpanId = spanId;
        log.Attributes.Set("exception.type", "System.TimeoutException").Set("exception.message", message).Set("exception.stacktrace", "at Y()");
        return log;
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
    public async Task AnExceptionLoggedUnderTheSpan_IsLinkedToIt_AndSentToAzureMonitorOnce()
    {
        await using var host = TestHost.Create();

        host.Pipeline.Enqueue(LoggedUnder(Records.SpanId));
        host.Pipeline.Enqueue(LoggedUnder("2222222222222222", message: "elsewhere"));
        host.Pipeline.Enqueue(Span());
        await host.FlushAsync();

        var exception = host.Memory.Spans.Single().Events.Single(e => e.IsException);
        Assert.AreEqual(new BelemetryLoggedException("System.TimeoutException", "slow", BelemetrySeverity.Error, false), exception.LoggedAs);
        Assert.IsFalse(exception.Limited, "The log record took its place under the error limits.");

        var batch = new BelemetryBatch(Records.Resource(), host.Memory.Spans.ToList(), [host.Memory.Logs.First(l => l.SpanId == Records.SpanId)]);
        AzureMonitorEnvelopeSerializer.Serialize(batch, new AzureMonitorEnvelopeSerializer.Context("ikey", true), out var count);
        Assert.AreEqual(3, count, "The span, its other event, and one Exception: the log's.");

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
