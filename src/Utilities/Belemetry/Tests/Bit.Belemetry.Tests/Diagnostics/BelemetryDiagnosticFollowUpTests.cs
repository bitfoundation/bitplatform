using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Diagnostics;

/// <summary>
/// Defects found after the first implementation - by the benchmarks, a real-browser run of the samples and the
/// independent review - each pinned by the test that would have caught it.
/// </summary>
[TestClass]
public class BelemetryDiagnosticFollowUpTests
{
    [TestMethod]
    public async Task AnExportedEntryBelowTheTimelinesLevel_StaysOffTheTimeline()
    {
        // Found by DiagnosticsBenchmarks: an Information entry the export wanted was recorded on the timeline too.
        await using var host = DiagnosticsTestHost.Create(o => o.Logs.MinimumLevel = LogLevel.Information);
        var logger = host.Factory.CreateLogger("App");

        logger.LogInformation("Exported, not on the timeline");
        logger.LogWarning("Both");

        await host.Host.FlushAsync();
        Assert.HasCount(2, host.Host.Memory.Logs);
        Assert.AreEqual("Both", host.Diagnostics.GetEvents().Single().Message);
    }

    [TestMethod]
    public void AnAppUnderTheLibrarysNamespace_IsApplicationCode()
    {
        // Found running the samples (Bit.Belemetry.Samples.*): their frames were passed over as Belemetry's own, so no
        // component was inferred from the stack.
        const string stack = """
               at Bit.Belemetry.BelemetryPipeline.Enqueue(BelemetryLogRecord log)
               at Bit.Belemetry.Samples.Core.Components.FaultyWidget.OnParametersSet()
               at Microsoft.AspNetCore.Components.ComponentBase.CallOnParametersSetAsync()
            """;

        Assert.AreEqual("Bit.Belemetry.Samples.Core.Components.FaultyWidget.OnParametersSet", BelemetryExceptionFingerprint.Anchor(stack));
        Assert.AreEqual("Bit.Belemetry.Samples.Core.Components.FaultyWidget",
            BelemetryDiagnosticEventFactory.InferComponent(new BelemetryDiagnosticException { Type = "E", Anchor = BelemetryExceptionFingerprint.Anchor(stack) }));
        Assert.IsTrue(BelemetryExceptionFingerprint.IsFrameworkFrame("Bit.Belemetry.BelemetryPipeline.Enqueue"));
        Assert.IsTrue(BelemetryExceptionFingerprint.IsFrameworkFrame("Bit.Belemetry.IBelemetry.TrackException"));
        Assert.IsFalse(BelemetryExceptionFingerprint.IsFrameworkFrame("Bit.Belemetry.Demo.Client.Pages.DiagnosticsPage.FailInHandler"));
    }

    [TestMethod]
    public async Task AnExceptionTheTimelineRecorded_IsNotReportedTwice_WhenTheAppPassesIt()
    {
        // Found running the samples: an ErrorBoundary's exception, logged by the framework and then passed to CaptureAsync,
        // became two events and a "tracked" subject instead of the error boundary's.
        await using var host = DiagnosticsTestHost.Create();
        var boundary = DiagnosticsTestHost.Thrown(() => new InvalidOperationException("The widget could not render"));
        host.Factory.CreateLogger("Microsoft.AspNetCore.Components.Web.ErrorBoundary").LogWarning(boundary, "Caught by an error boundary");

        var capture = await host.Diagnostics.CaptureAsync(new() { Exception = boundary, Reason = "error-boundary" });

        Assert.AreEqual("error-boundary", capture.Subject!.Exception!.Origin);
        Assert.HasCount(1, capture.Events.Where(e => e.Exception is not null));
        Assert.AreEqual(1, capture.ExceptionGroups.Single().Count);
    }

    [TestMethod]
    public void OneRequestSeenFromBothSides_IsOneFailedRequest()
    {
        // Found running the Server sample: the page's fetch span and the server's span of the same request (one trace)
        // were reported as two failed requests, each "a possible cause".
        const string trace = "0af7651916cd43dd8448eb211c80319c";
        var start = BelemetryTime.NowUnixNano() - 2_000_000_000;
        var browser = new BelemetryDiagnosticEvent
        {
            Sequence = 1, TimeUnixNano = start, Category = BelemetryDiagnosticCategories.Http, Name = "GET", Severity = BelemetrySeverity.Error,
            Outcome = BelemetryDiagnosticOutcome.Failure, Source = BelemetryDiagnosticSource.Browser, TraceId = trace, SpanId = "b7ad6b7169203331",
            Attributes = new Dictionary<string, object?> { ["url.template"] = "https://shop.test/api/fail", [BelemetrySemanticConventions.HttpResponseStatusCode] = 500L },
        };
        var server = new BelemetryDiagnosticEvent
        {
            Sequence = 7, TimeUnixNano = start + 1_000_000, Category = BelemetryDiagnosticCategories.Http, Name = "GET", Severity = BelemetrySeverity.Error,
            Outcome = BelemetryDiagnosticOutcome.Failure, Source = BelemetryDiagnosticSource.Dotnet, TraceId = trace, SpanId = "00f067aa0ba902b7", ParentSpanId = "b7ad6b7169203331",
            Route = "/api/fail",
        };
        var failure = new BelemetryDiagnosticEvent
        {
            Sequence = 8, TimeUnixNano = start + 1_500_000_000, Category = BelemetryDiagnosticCategories.Exception, Name = "exception",
            Severity = BelemetrySeverity.Error, Outcome = BelemetryDiagnosticOutcome.Failure, Source = BelemetryDiagnosticSource.Dotnet,
            Exception = new BelemetryDiagnosticException { Type = "System.InvalidOperationException", Fingerprint = "f" },
        };
        var capture = new BelemetryDiagnosticCapture(new BelemetryCaptureContent
        {
            Id = "0123456789abcdef", CreatedAt = DateTimeOffset.UtcNow, Trigger = new(BelemetryCaptureTriggerKind.Manual, "test"),
            Options = new BelemetryDiagnosticsOptions(), Events = [browser, server, failure], Subject = failure,
        });

        var report = capture.Report;

        Assert.AreEqual("GET https://shop.test/api/fail 500", Assert.ContainsSingle(report.FailedRequests).Text);
        Assert.AreEqual(1, report.Findings.Count(f => f.Text.StartsWith("The request", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void TwoFailedRequests_OfOneTrace_AreTwo()
    {
        // One event handler's two failed calls share its trace; only a server span whose parent is the page's span is the
        // same request as that span.
        const string trace = "0af7651916cd43dd8448eb211c80319c";
        var start = BelemetryTime.NowUnixNano() - 2_000_000_000;
        BelemetryDiagnosticEvent Request(long sequence, string span, string url) => new()
        {
            Sequence = sequence, TimeUnixNano = start + sequence, Category = BelemetryDiagnosticCategories.Http, Name = "GET", Severity = BelemetrySeverity.Error,
            Outcome = BelemetryDiagnosticOutcome.Failure, Source = BelemetryDiagnosticSource.Dotnet, TraceId = trace, SpanId = span, ParentSpanId = "53995c3f42cd8ad8",
            Attributes = new Dictionary<string, object?> { ["url.template"] = url, [BelemetrySemanticConventions.HttpResponseStatusCode] = 503L },
        };
        var failure = new BelemetryDiagnosticEvent
        {
            Sequence = 9, TimeUnixNano = start + 1_000_000, Category = BelemetryDiagnosticCategories.Exception, Name = "exception",
            Severity = BelemetrySeverity.Error, Outcome = BelemetryDiagnosticOutcome.Failure, Source = BelemetryDiagnosticSource.Dotnet, TraceId = trace,
            Exception = new BelemetryDiagnosticException { Type = "System.InvalidOperationException", Fingerprint = "f" },
        };
        var capture = new BelemetryDiagnosticCapture(new BelemetryCaptureContent
        {
            Id = "0123456789abcdef", CreatedAt = DateTimeOffset.UtcNow, Trigger = new(BelemetryCaptureTriggerKind.Manual, "test"),
            Options = new BelemetryDiagnosticsOptions(),
            Events = [Request(1, "1111111111111111", "https://api.test/orders"), Request(2, "2222222222222222", "https://api.test/stock"), failure], Subject = failure,
        });

        var report = capture.Report;

        CollectionAssert.AreEqual(new[] { "GET https://api.test/orders 503", "GET https://api.test/stock 503" }, report.FailedRequests.Select(r => r.Text).ToArray());
        Assert.AreEqual(2, report.Findings.Count(f => f.Text.EndsWith("failed in the same trace.", StringComparison.Ordinal)));
    }
}
