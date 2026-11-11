using Bunit;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Belemetry.Tests.Diagnostics;

/// <summary>
/// The second review's findings (on the support-bundle work), each pinned by a test that fails without its fix.
/// </summary>
[TestClass]
public class BelemetryDiagnosticSecondReviewTests
{
    [TestMethod]
    public void AUrlWithoutAPath_KeepsNeitherItsQueryNorItsFragment()
    {
        Assert.AreEqual("https://api.example.com/", BelemetryUrlTemplate.TemplateUrl("https://api.example.com?customer=jdoe&orderId=991#tab", ["page"]));
        Assert.AreEqual("https://api.example.com/?page=2", BelemetryUrlTemplate.TemplateUrl("https://api.example.com?page=2&token=x", ["page"]));
        Assert.AreEqual("https://api.example.com/", BelemetryUrlTemplate.TemplateUrl("https://jane:pw@api.example.com#top", []));
    }

    [TestMethod]
    public void TheReport_IsRebuilt_WhenThePageRevokesWhatTheServerStillCollects()
    {
        // A server: its own features (Errors) are unchanged, the page revokes Errors - the union of both did not change.
        var page = BelemetryFeatures.All;
        var error = new BelemetryDiagnosticEvent
        {
            Sequence = 1,
            TimeUnixNano = BelemetryTime.NowUnixNano(),
            Category = BelemetryDiagnosticCategories.Exception,
            Name = "exception",
            Severity = BelemetrySeverity.Error,
            Outcome = BelemetryDiagnosticOutcome.Failure,
            Source = BelemetryDiagnosticSource.Browser,
            Feature = BelemetryFeatures.Errors,
            Exception = new BelemetryDiagnosticException { Type = "TypeError", Message = "x is undefined", Fingerprint = "f", Origin = "browser" },
        };
        var content = new BelemetryCaptureContent
        {
            Id = BelemetryCaptureContent.NewId(),
            CreatedAt = DateTimeOffset.UtcNow,
            Trigger = new(BelemetryCaptureTriggerKind.Manual, "manual"),
            Options = new BelemetryDiagnosticsOptions(),
            MayCapture = _ => true,
            MayCaptureBrowser = f => (page & f) == f,
            Events = [error],
            Subject = error,
        };
        var capture = new BelemetryDiagnosticCapture(content);
        Assert.AreEqual("TypeError", capture.Report.ExceptionType);

        page &= ~BelemetryFeatures.Errors;

        Assert.IsNull(capture.Report.ExceptionType);
        Assert.DoesNotContain("x is undefined", capture.ToMarkdown());
    }

    [TestMethod]
    public void AnAutomaticCaptureBuiltAcrossAClear_IsDiscarded_AndNotCountedAsARevoke()
    {
        var recorder = new BelemetryDiagnosticRecorder(new BelemetryDiagnosticsOptions());
        Assert.IsTrue(recorder.TryBeginAutomaticCapture("f", DateTimeOffset.UtcNow, out var generation));
        recorder.Clear();

        recorder.EndAutomaticCapture(BelemetryDiagnosticCapture.Failed(new(BelemetryCaptureTriggerKind.Automatic, "x"), "stand-in"), generation);

        Assert.IsEmpty(recorder.Pending());
        Assert.IsFalse(recorder.Dropped().ContainsKey("consent_revoked_capture"));
    }

    [TestMethod]
    public void AFullReviewQueue_TakesTheNewestFailure_AndDropsTheOldestCapture()
    {
        var recorder = new BelemetryDiagnosticRecorder(new BelemetryDiagnosticsOptions { MaxPendingCaptures = 2 });
        var captures = new List<BelemetryDiagnosticCapture>();
        foreach (var key in new[] { "a", "b", "c" })
        {
            Assert.IsTrue(recorder.TryBeginAutomaticCapture(key, DateTimeOffset.UtcNow, out var generation), $"The trigger '{key}' was refused.");
            var capture = BelemetryDiagnosticCapture.Failed(new(BelemetryCaptureTriggerKind.Automatic, key), "stand-in");
            captures.Add(capture);
            recorder.EndAutomaticCapture(capture, generation);
        }

        Assert.AreSequenceEqual(new[] { captures[1], captures[2] }, recorder.Pending());
        Assert.AreEqual(1, recorder.Dropped()["pending_capture_limit"]);
    }

    [TestMethod]
    public async Task ABrowserTimelineTooLargeToRead_IsMissingEvidence_NotAFailedCapture()
    {
        await using var context = new BunitContext();
        context.Services.AddLogging();
        context.Services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Host = BelemetryHostKind.Server;
            o.Diagnostics.Enabled = true;
        });
        var module = context.JSInterop.SetupModule(BelemetryJsBridge.ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<IJSStreamReference>("getDiagnosticsStream").SetResult(new OversizedStream());
        var diagnostics = context.Services.GetRequiredService<IBelemetryDiagnostics>();

        var capture = await diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);

        Assert.IsTrue(capture.Succeeded, capture.Error);
        Assert.Contains(m => m.StartsWith("Browser evidence", StringComparison.Ordinal), capture.MissingEvidence);
    }

    private sealed class OversizedStream : IJSStreamReference
    {
        public long Length => 5 * 1024 * 1024;

        // What Blazor Server's RemoteJSDataStream throws for a stream longer than maxAllowedSize.
        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
            throw new ArgumentOutOfRangeException(nameof(maxAllowedSize), $"The incoming data stream of length {Length} exceeds the maximum allowed length {maxAllowedSize}.");

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [TestMethod]
    public void OnlyTheFrameworksRenderers_ReportUnhandledExceptions()
    {
        Assert.AreEqual("log", BelemetryDiagnosticEventFactory.OriginOf("MyApp.Reports.PdfRenderer", "System.InvalidOperationException"));
        Assert.AreEqual("log", BelemetryDiagnosticEventFactory.OriginOf("MyApp.Hosting.CircuitHost", "System.InvalidOperationException"));
        Assert.AreEqual("unhandled", BelemetryDiagnosticEventFactory.OriginOf("Microsoft.AspNetCore.Components.RenderTree.Renderer", "System.InvalidOperationException"));
        Assert.AreEqual("unhandled", BelemetryDiagnosticEventFactory.OriginOf("Microsoft.AspNetCore.Components.Server.Circuits.RemoteRenderer", null));
        Assert.AreEqual("unhandled", BelemetryDiagnosticEventFactory.OriginOf("Microsoft.AspNetCore.Components.WebView.Services.WebViewRenderer", null));
        Assert.AreEqual("unhandled", BelemetryDiagnosticEventFactory.OriginOf("Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost", null));
    }

    [TestMethod]
    public async Task AnAppsOwnRenderer_LoggingAHandledError_TriggersNoUnhandledCapture()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.AutoCaptureTriggers = BelemetryDiagnosticTriggers.UnhandledExceptions);
        host.Factory.CreateLogger("MyApp.Reports.PdfRenderer").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("handled")), "handled");
        await Task.Delay(50, TestContext.CancellationToken);

        Assert.AreEqual("log", host.Diagnostics.GetEvents().Single().Exception!.Origin);
        Assert.IsEmpty(host.Diagnostics.PendingCaptures);
    }

    [TestMethod]
    public void TheComponentSource_IsExported_OnAClientHost_OnlyWithDiagnostics()
    {
        static BelemetrySettings Settings(BelemetryHostKind host, bool diagnostics)
        {
            var options = new BelemetryOptions { Host = host, UseEnvironmentVariables = false };
            options.Diagnostics.Enabled = diagnostics;
            return new BelemetrySettings(options, new BelemetryHostDetection(HasWebHost: host == BelemetryHostKind.Server), isBrowser: host == BelemetryHostKind.WebAssembly);
        }

        const string components = BelemetryDiagnosticEventFactory.ComponentsSource;
        Assert.IsFalse(Settings(BelemetryHostKind.WebAssembly, diagnostics: false).ExportsTraceSource(components));
        Assert.IsFalse(Settings(BelemetryHostKind.Hybrid, diagnostics: false).ExportsTraceSource(components));
        Assert.IsTrue(Settings(BelemetryHostKind.WebAssembly, diagnostics: false).ExportsTraceSource("System.Net.Http"));
        Assert.IsTrue(Settings(BelemetryHostKind.WebAssembly, diagnostics: true).ExportsTraceSource(components));
        Assert.IsTrue(Settings(BelemetryHostKind.Server, diagnostics: false).ExportsTraceSource(components));
    }

    [TestMethod]
    public async Task AnExceptionGroup_ExportsTheOccurrencesOfTheCapturedEventsOnly()
    {
        // One group ("Order {n} failed"). The first failure is evicted from the timeline (one failure kept); its
        // occurrence, with its message, stays in the group's history - which the preview never listed.
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.MaxFailureEvents = 1);
        var logger = host.Factory.CreateLogger("MyApp.Orders");
        foreach (var order in new[] { 10423, 99871 })
        {
            logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException($"Order {order} failed")), "failed");
        }

        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        var group = capture.ExceptionGroups.Single();

        Assert.AreEqual(2, group.Count, "The history's counts stay.");
        Assert.AreEqual(capture.Events.Single(e => e.Exception is not null).Sequence, group.Occurrences.Single().Sequence);
        capture.Confirm();
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(await capture.ExportAsync(TestContext.CancellationToken)));
        var exceptions = new StreamReader(zip.GetEntry("exceptions.json")!.Open()).ReadToEnd();
        Assert.DoesNotContain("10423", exceptions);
        Assert.Contains("99871", exceptions);
    }

    [TestMethod]
    public async Task ABrowserTimeBeyondWhatALongHolds_IsMalformed_AndNeverOverflows()
    {
        await using var host = DiagnosticsTestHost.Create();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using var payload = JsonDocument.Parse($$$"""
            {"v":1,"events":[
              {"seq":1,"time":1e16,"cat":"exception","name":"exception","sev":17,"outcome":"failure","feature":1,"exc":{"type":"TypeError","message":"boom"}},
              {"seq":2,"time":{{{now}}},"cat":"exception","name":"exception","sev":17,"outcome":"failure","feature":1,"exc":{"type":"TypeError","message":"boom"}}
            ]}
            """);

        var result = BelemetryBrowserEvidence.Read(payload.RootElement, host.Hub);

        Assert.AreEqual(2L, result.Events.Single().Sequence);
        Assert.AreEqual(1, result.Dropped["browser.malformed"]);
        _ = BelemetryTime.ToDateTime(BelemetryTime.FromUnixMilliseconds(1e16));
        _ = BelemetryTime.ToDateTime(BelemetryTime.FromUnixMilliseconds(-1e16));
        Assert.AreEqual(0L, BelemetryTime.FromUnixMilliseconds(double.NaN));
    }

    [TestMethod]
    public async Task AHybridAppOutsideWindows_DoesNotReportADownloadItsWebViewIgnores()
    {
        await using var context = new BunitContext();
        context.Services.AddLogging();
        context.Services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Host = BelemetryHostKind.Hybrid;
        });
        var module = context.JSInterop.SetupModule(BelemetryJsBridge.ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        var bridge = context.Services.GetRequiredService<BelemetryJsBridge>();

        var saved = await bridge.DownloadAsync("bundle.zip", [1, 2, 3], CancellationToken.None);

        // WebView2 saves downloads; Android's WebView and WKWebView ignore them.
        Assert.AreEqual(OperatingSystem.IsWindows(), saved);
        Assert.AreEqual(OperatingSystem.IsWindows(), module.Invocations.Any(i => i.Identifier == "downloadFile"));
    }

    [TestMethod]
    public async Task ACapture_HoldsThePagesGrant_NotItsSessionOrBridge()
    {
        await using var context = new BunitContext();
        context.Services.AddLogging();
        context.Services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Host = BelemetryHostKind.Server;
            o.Diagnostics.Enabled = true;
        });
        var bridge = context.Services.GetRequiredService<BelemetryJsBridge>();
        bridge.Apply(new BelemetryJsContext { Consent = (int)BelemetryFeatures.All, Decided = true });
        var capture = await context.Services.GetRequiredService<IBelemetryDiagnostics>().CaptureAsync(new() { IncludeBrowserEvidence = false }, TestContext.CancellationToken);

        Assert.IsInstanceOfType<BelemetryPageConsent>(capture.Content.MayCaptureBrowser!.Target);
        Assert.IsInstanceOfType<BelemetryDiagnosticHub>(capture.Content.MayCapture!.Target);

        // Still live: a revoke the page reports reaches the capture.
        bridge.Apply(new BelemetryJsContext { Consent = (int)(BelemetryFeatures.All & ~BelemetryFeatures.Errors), Decided = true });
        Assert.IsFalse(capture.Content.MayCaptureBrowser(BelemetryFeatures.Errors));
    }

    [TestMethod]
    public async Task NobodysServerSpans_AreIndexed_OnlyWhenTheyFailed()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Host = BelemetryHostKind.Server);
        var index = host.Hub.TraceIndex!;
        var ok = Records.Span(BelemetrySpanKind.Server);
        ok.TraceId = ActivityTraceId.CreateRandom().ToHexString();
        ok.Attributes.Set(BelemetrySemanticConventions.HttpRequestMethod, "GET");
        var failed = Records.Span(BelemetrySpanKind.Server);
        failed.TraceId = ActivityTraceId.CreateRandom().ToHexString();
        failed.Status = BelemetryStatusCode.Error;
        failed.Attributes.Set(BelemetrySemanticConventions.HttpRequestMethod, "POST");

        host.Host.Pipeline.Enqueue(ok);
        host.Host.Pipeline.Enqueue(failed);

        Assert.IsEmpty(index.Find([ok.TraceId]));
        Assert.ContainsSingle(index.Find([failed.TraceId]));
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
