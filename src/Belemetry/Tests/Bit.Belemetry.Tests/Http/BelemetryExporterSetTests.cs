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
}
