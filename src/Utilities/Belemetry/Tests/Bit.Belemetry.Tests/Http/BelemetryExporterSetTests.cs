using System.Net;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Http;

[TestClass]
public class BelemetryExporterSetTests
{
    [TestMethod]
    public void MinimumSeverity_MapsTheLogLevelToTheFirstNumberOfItsRange()
    {
        Assert.IsNull(BelemetryLogger.ToMinimumSeverity(null));
        Assert.IsNull(BelemetryLogger.ToMinimumSeverity(LogLevel.Trace), "Trace keeps everything, like no minimum.");
        Assert.AreEqual(BelemetrySeverity.Debug, BelemetryLogger.ToMinimumSeverity(LogLevel.Debug));
        Assert.AreEqual(BelemetrySeverity.Info, BelemetryLogger.ToMinimumSeverity(LogLevel.Information));
        Assert.AreEqual(BelemetrySeverity.Warn, BelemetryLogger.ToMinimumSeverity(LogLevel.Warning));
        Assert.AreEqual(BelemetrySeverity.Error, BelemetryLogger.ToMinimumSeverity(LogLevel.Error));
        Assert.AreEqual(BelemetrySeverity.Fatal, BelemetryLogger.ToMinimumSeverity(LogLevel.Critical));
        Assert.IsTrue(BelemetryLogger.ToMinimumSeverity(LogLevel.None) > BelemetrySeverity.Fatal + 3, "None passes nothing, FATAL4 included.");
    }

    [TestMethod]
    public void WithMinimumSeverity_CopiesOnlyWhenSomethingIsBelowIt()
    {
        var info = Records.Log("info", severity: BelemetrySeverity.Info);
        var warn = Records.Log("warn", severity: BelemetrySeverity.Warn);
        var error = Records.Log("error", severity: BelemetrySeverity.Error);
        var batch = new BelemetryBatch(Records.Resource(), [Records.Span()], [info, warn, error], Tests.Otlp.OtlpProtobufConformanceTests.MetricsSample());

        Assert.AreSame(batch, BelemetryExporterSet.WithMinimumSeverity(batch, null));
        Assert.AreSame(batch, BelemetryExporterSet.WithMinimumSeverity(batch, BelemetrySeverity.Info), "Nothing below the minimum: the shared batch is reused.");
        var noLogs = new BelemetryBatch(Records.Resource(), [Records.Span()]);
        Assert.AreSame(noLogs, BelemetryExporterSet.WithMinimumSeverity(noLogs, BelemetrySeverity.Fatal));

        var filtered = BelemetryExporterSet.WithMinimumSeverity(batch, BelemetrySeverity.Warn);
        Assert.AreNotSame(batch, filtered);
        CollectionAssert.AreEqual(new[] { "warn", "error" }, filtered.Logs.Select(l => l.Body).ToArray());

        // Events are not logs to a log level: a page view or a custom event is Info and must still reach the backend.
        var pageView = Records.Log("Home", "browser.page_view");
        var withEvents = BelemetryExporterSet.WithMinimumSeverity(new BelemetryBatch(Records.Resource(), logs: [info, pageView, error]), BelemetrySeverity.Error);
        CollectionAssert.AreEqual(new[] { "Home", "error" }, withEvents.Logs.Select(l => l.Body).ToArray());
        Assert.AreSame(batch.Spans, filtered.Spans);
        Assert.AreSame(batch.Metrics, filtered.Metrics);
        Assert.AreSame(batch.Resource, filtered.Resource);

        Assert.IsEmpty(BelemetryExporterSet.WithMinimumSeverity(batch, BelemetrySeverity.Fatal).Logs);
    }

    [TestMethod]
    public async Task MinimumLogLevel_AppliesPerExporter()
    {
        var handler = new FakeHttpMessageHandler { Fallback = HttpStatusCode.OK };
        await using var host = TestHost.Create(o =>
        {
            o.AddOtlpExporter("http://everything.test", e => e.Protocol = BelemetryOtlpProtocol.HttpJson);
            o.AddOtlpExporter("http://errors.test", e =>
            {
                e.Protocol = BelemetryOtlpProtocol.HttpJson;
                e.MinimumLogLevel = LogLevel.Error;
            });
        }, configureServices: s => s.AddSingleton(new BelemetryHttpMessageHandlerProvider(handler)));

        var logger = host.StartLogging().CreateLogger("Tests");
        logger.LogInformation("fine");
        logger.LogError("broken");
        await host.FlushAsync();

        var everything = handler.Requests.Single(r => r.Message.RequestUri!.Host == "everything.test");
        var errorsOnly = handler.Requests.Single(r => r.Message.RequestUri!.Host == "errors.test");
        StringAssert.Contains(System.Text.Encoding.UTF8.GetString(everything.Body), "fine");
        StringAssert.Contains(System.Text.Encoding.UTF8.GetString(everything.Body), "broken");
        StringAssert.DoesNotMatch(System.Text.Encoding.UTF8.GetString(errorsOnly.Body), new System.Text.RegularExpressions.Regex("fine"));
        StringAssert.Contains(System.Text.Encoding.UTF8.GetString(errorsOnly.Body), "broken");
        Assert.HasCount(2, host.Memory.Logs, "The in-memory exporter has no minimum: it gets both.");
    }

    [TestMethod]
    public async Task Sentry_GetsTheLoggedErrors_AsEvents_WithTheExceptionsFrames()
    {
        var handler = new FakeHttpMessageHandler { Fallback = HttpStatusCode.OK };
        await using var host = TestHost.Create(o => o.AddSentryExporter("https://abc123@o1.ingest.sentry.io/42"),
                                               configureServices: s => s.AddSingleton(new BelemetryHttpMessageHandlerProvider(handler)));

        var logger = host.StartLogging().CreateLogger("Tests.Orders");
        logger.LogInformation("fine");
        logger.LogWarning("slow");
        logger.LogError(Sentry.SentryStackTraceTests.Thrower.Catch(Sentry.SentryStackTraceTests.Thrower.Wrapped), "Order {OrderId} failed", 7);
        await host.FlushAsync();

        var request = handler.Requests.Single();
        Assert.AreEqual("https://o1.ingest.sentry.io/api/42/envelope/?sentry_key=abc123&sentry_version=7", request.Message.RequestUri!.ToString());
        Assert.AreEqual("application/x-sentry-envelope", request.Message.Content!.Headers.ContentType!.MediaType);

        var lines = System.Text.Encoding.UTF8.GetString(request.Body).Split('\n');
        using var e = JsonDocument.Parse(lines[2]);
        Assert.AreEqual("Order 7 failed", e.RootElement.GetProperty("message").GetProperty("formatted").GetString());
        Assert.AreEqual("Tests.Orders", e.RootElement.GetProperty("logger").GetString());
        var frame = e.RootElement.GetProperty("exception").GetProperty("values")[0].GetProperty("stacktrace").GetProperty("frames").EnumerateArray().Last();
        Assert.AreEqual("Fail", frame.GetProperty("function").GetString());
        Assert.IsTrue(frame.TryGetProperty("function_id", out _), "The ILogger record kept the exception object for its frames.");
    }

    [TestMethod]
    public async Task AFailedRequest_HoldsBackTheRestOfTheBatchToItsUrl()
    {
        // Sentry makes a request per event: once its endpoint failed, the batch's other events do not each wait out
        // their own retries and timeouts in turn, with the export lock held.
        var handler = new FakeHttpMessageHandler { Fallback = HttpStatusCode.ServiceUnavailable };
        for (var i = 0; i < 3; i++) handler.Enqueue(HttpStatusCode.ServiceUnavailable, r => r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero));
        await using var host = TestHost.Create(o => o.AddSentryExporter("https://abc123@o1.ingest.sentry.io/42"),
                                               configureServices: s => s.AddSingleton(new BelemetryHttpMessageHandlerProvider(handler)));

        var logger = host.StartLogging().CreateLogger("Tests");
        logger.LogError("one");
        logger.LogError("two");
        logger.LogError("three");
        await host.FlushAsync();

        Assert.HasCount(3, handler.Requests, "The first event's three attempts, and none for the other two.");
        Assert.IsTrue(handler.Requests.All(r => System.Text.Encoding.UTF8.GetString(r.Body).Contains("\"one\"", StringComparison.Ordinal)));
        Assert.HasCount(3, host.Memory.Logs, "The other exporters still get the batch.");
    }

    [TestMethod]
    public async Task SentryAlone_QueuesOnlyWhatItSends()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Exporters.Clear();
            o.AddSentryExporter("https://abc123@o1.ingest.sentry.io/42");
        });
        var accepted = new List<object>();
        host.Pipeline.RecordAccepted += accepted.Add;

        var info = Records.Log("fine", severity: BelemetrySeverity.Info);
        var pageView = Records.Log("Home", BelemetrySemanticConventions.PageViewEvent, BelemetrySeverity.Error);
        var span = Records.Span();
        var error = Records.Log("broken", severity: BelemetrySeverity.Error);
        var failed = Records.Span();
        failed.Events.Add(new BelemetrySpanEvent
        {
            Name = BelemetrySemanticConventions.ExceptionEvent,
            TimeUnixNano = Records.Start,
            Attributes = new BelemetryAttributes().Set(BelemetrySemanticConventions.ExceptionType, "System.Exception").Set(BelemetrySemanticConventions.ExceptionMessage, "boom"),
        });
        foreach (var log in new[] { info, pageView, error }) host.Pipeline.Enqueue(log);
        foreach (var s in new[] { span, failed }) host.Pipeline.Enqueue(s);

        CollectionAssert.AreEqual(new object[] { error, failed }, accepted, "Sentry sends neither a span without an exception, a page view nor a log below its level.");
    }
}
