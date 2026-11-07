namespace Bit.Belemetry.Tests.AzureMonitor;

[TestClass]
public class AzureMonitorLiveMetricsTests
{
    [TestMethod]
    public async Task Exceptions_CountTheOnesRecordedOnSpans_AsTheyAreSent()
    {
        var exporter = new BelemetryAzureMonitorExporter(new BelemetryAzureMonitorExporterOptions { ConnectionString = "InstrumentationKey=11111111-2222-3333-4444-555555555555" }, "azuremonitor");
        await using var live = new AzureMonitorLiveMetrics(exporter, TestHost.CreateSettings(), (_, _) => { });

        static BelemetrySpanEvent Exception() => new() { Name = "exception", Attributes = new BelemetryAttributes().Set("exception.type", "System.TimeoutException") };

        var span = Records.Span(BelemetrySpanKind.Server);
        span.Events.Add(Exception());
        span.Events.Add(Exception());
        span.Events.Add(new BelemetrySpanEvent { Name = "exception" });
        span.Events.Add(new BelemetrySpanEvent { Name = "retry" });
        var limited = Exception();
        limited.Limited = true;
        span.Events.Add(limited);
        var logged = Exception();
        logged.LoggedAs = new BelemetryLoggedException("System.TimeoutException", null, BelemetrySeverity.Error, false);
        span.Events.Add(logged);

        var log = Records.Log(severity: BelemetrySeverity.Error);
        log.Attributes.Set("exception.type", "System.TimeoutException");

        live.OnRecord(span);
        live.OnRecord(log);

        Assert.AreEqual(3, live.Exceptions, "The 2 exceptions recorded on the span and the logged one - counted once, as its log record.");
    }
}
