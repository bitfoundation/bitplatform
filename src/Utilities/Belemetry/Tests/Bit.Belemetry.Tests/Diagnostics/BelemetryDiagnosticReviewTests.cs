using System.IO.Compression;
using Bunit;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Diagnostics;

/// <summary>
/// The independent review's findings on the first implementation, each pinned by a test that fails without its fix.
/// </summary>
[TestClass]
public class BelemetryDiagnosticReviewTests
{
    private sealed class CircuitContext : SynchronizationContext;

    private static T On<T>(SynchronizationContext context, Func<T> action)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            return action();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [TestMethod]
    public async Task FrameworkActivities_AreNotCreated_WhereThatWouldChangeTheAppsTraces()
    {
        // An ignored name must not exist: an unsampled HandleEvent would make a parent-based sampler drop the spans under it.
        await using (var host = DiagnosticsTestHost.Create(o =>
        {
            o.Traces.Sources.Add("Microsoft.AspNetCore.Components*");
            o.Traces.IgnoredActivityNames.Add(BelemetryDiagnosticEventFactory.HandleEventActivity);
        }))
        {
            Assert.IsFalse(host.Hub.CreatesActivity(BelemetryDiagnosticEventFactory.ComponentsSource, BelemetryDiagnosticEventFactory.HandleEventActivity));
            Assert.IsTrue(host.Hub.CreatesActivity(BelemetryDiagnosticEventFactory.ComponentsSource, BelemetryDiagnosticEventFactory.NavigateActivity));
            using var source = new ActivitySource(BelemetryDiagnosticEventFactory.ComponentsSource);
            Assert.IsNull(source.StartActivity(BelemetryDiagnosticEventFactory.HandleEventActivity));
        }

        // A server (the app's own tracer): not unless asked to.
        await using (var server = DiagnosticsTestHost.Create(o => o.Host = BelemetryHostKind.Server))
        {
            Assert.IsFalse(server.Hub.CreatesActivity(BelemetryDiagnosticEventFactory.ComponentsSource, BelemetryDiagnosticEventFactory.HandleEventActivity));
        }
        await using (var opted = DiagnosticsTestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Diagnostics.CreateFrameworkActivities = true;
        }))
        {
            Assert.IsTrue(opted.Hub.CreatesActivity(BelemetryDiagnosticEventFactory.ComponentsSource, BelemetryDiagnosticEventFactory.HandleEventActivity));
        }

        // A client host without Belemetry traces: Belemetry is the app's tracer there.
        await using var client = DiagnosticsTestHost.Create();
        Assert.IsTrue(client.Hub.CreatesActivity(BelemetryDiagnosticEventFactory.ComponentsSource, BelemetryDiagnosticEventFactory.HandleEventActivity));
    }

    [TestMethod]
    public async Task WorkStartedUnderACircuitsEvent_IsNotTheCircuits_AfterTheEventEnded()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Diagnostics.Enabled = true;
            o.Diagnostics.CreateFrameworkActivities = true;
        });
        var logger = host.StartLogging().CreateLogger("MyApp.Background");
        await using var scope = host.Services.CreateAsyncScope();
        var circuit = new CircuitContext();
        var diagnostics = On(circuit, () => scope.ServiceProvider.GetRequiredService<IBelemetryDiagnostics>());
        using var source = new ActivitySource(BelemetryDiagnosticEventFactory.ComponentsSource);

        // A singleton's loop started inside the circuit's click handler captures the handler's execution context.
        var handle = On(circuit, () => source.StartActivity(BelemetryDiagnosticEventFactory.HandleEventActivity))!;
        var captured = ExecutionContext.Capture()!;
        On(circuit, () =>
        {
            handle.Stop();
            return 0;
        });
        Activity.Current = null;

        ExecutionContext.Run(captured, _ => logger.LogWarning("Another user's data, later"), null);

        Assert.DoesNotContain(e => e.Message == "Another user's data, later", diagnostics.GetEvents());
    }

    [TestMethod]
    public async Task ANestedScope_SharesTheCircuitsTimeline_AndLeavingItKeepsTheCircuitRecording()
    {
        await using var host = TestHost.Create(o =>
        {
            o.Host = BelemetryHostKind.Server;
            o.Diagnostics.Enabled = true;
        });
        var logger = host.StartLogging().CreateLogger("App");
        await using var circuitScope = host.Services.CreateAsyncScope();
        var circuit = new CircuitContext();
        var diagnostics = On(circuit, () => circuitScope.ServiceProvider.GetRequiredService<IBelemetryDiagnostics>());

        // An OwningComponentBase's scope, on the same renderer.
        var nestedScope = host.Services.CreateAsyncScope();
        var nested = On(circuit, () => nestedScope.ServiceProvider.GetRequiredService<IBelemetryDiagnostics>());
        On(circuit, () =>
        {
            logger.LogWarning("while the nested scope lives");
            return 0;
        });
        Assert.ContainsSingle(nested.GetEvents(), "One timeline per circuit, whichever scope reads it.");
        await nestedScope.DisposeAsync();

        On(circuit, () =>
        {
            logger.LogWarning("after it is gone");
            return 0;
        });
        Assert.AreSequenceEqual(new[] { "while the nested scope lives", "after it is gone" }, diagnostics.GetEvents().Select(e => e.Message).ToArray());
    }

    [TestMethod]
    public async Task ThePreviewsRedactions_ReachTheReport()
    {
        await using var host = DiagnosticsTestHost.Create();
        var span = Records.Span();
        span.Status = BelemetryStatusCode.Error;
        span.Attributes.Set(BelemetrySemanticConventions.HttpRequestMethod, "GET");
        span.Attributes.Set(BelemetrySemanticConventions.UrlFull, "https://api.shop.test/customers/jane-doe/orders");
        host.Host.Pipeline.Enqueue(span);
        host.Factory.CreateLogger("App").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("e")), "failed");

        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        capture.RedactAttribute("url.template");

        Assert.IsTrue(capture.Report.FailedRequests.Single().Text.Contains("REDACTED", StringComparison.Ordinal));
        Assert.DoesNotContain("jane-doe", capture.ToMarkdown());
    }

    [TestMethod]
    public async Task ARemovedExceptionEvent_LeavesTheExceptionGroupsToo()
    {
        await using var host = DiagnosticsTestHost.Create();
        host.Factory.CreateLogger("App").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("Customer Jane Doe has no address")), "failed");
        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        Assert.ContainsSingle(capture.ExceptionGroups);

        capture.ExcludeEvent(capture.Events.Single(e => e.Exception is not null));

        Assert.IsEmpty(capture.ExceptionGroups);
        capture.Confirm();
        using var zip = new ZipArchive(new MemoryStream(await capture.ExportAsync(TestContext.CancellationToken)));
        Assert.DoesNotContain("Jane Doe", string.Concat(zip.Entries.Select(e => new StreamReader(e.Open()).ReadToEnd())));
    }

    [TestMethod]
    public async Task ABlockedOrRewrittenExceptionMessage_StaysOutOfTheTimeline()
    {
        await using var host = DiagnosticsTestHost.Create(o =>
        {
            o.Privacy.BlockedAttributes.Add(BelemetrySemanticConventions.ExceptionMessage);
            o.AddProcessor(new RewriteStack());
        });

        host.Factory.CreateLogger("App").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("secret-value 42")), "failed");

        var exception = host.Diagnostics.GetEvents().Single().Exception!;
        Assert.IsNull(exception.Message, "Blocked for the export: blocked here.");
        Assert.AreEqual("(stack removed by the app)", exception.StackTrace, "A processor's rewrite holds here too.");
    }

    [TestMethod]
    public async Task ACaptureOpenInThePreview_LosesWhatTheUserRevokes()
    {
        await using var host = DiagnosticsTestHost.Create();
        var logger = host.Factory.CreateLogger("App");
        logger.LogWarning("a log");
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("e")), "an error");
        var capture = await host.Diagnostics.CaptureAsync(cancellationToken: TestContext.CancellationToken);
        Assert.HasCount(2, capture.Events);

        host.Host.Consent.Decide(BelemetryFeatures.All & ~BelemetryFeatures.Logs);

        Assert.AreEqual(BelemetryFeatures.Errors, capture.Events.Single().Feature);
        capture.Confirm();
        using var zip = new ZipArchive(new MemoryStream(await capture.ExportAsync(TestContext.CancellationToken)));
        Assert.DoesNotContain("a log", new StreamReader(zip.GetEntry("timeline.json")!.Open()).ReadToEnd());
    }

    [TestMethod]
    public void AnAutomaticCaptureBuiltAcrossARevoke_IsDiscarded()
    {
        var recorder = new BelemetryDiagnosticRecorder(new BelemetryDiagnosticsOptions());
        Assert.IsTrue(recorder.TryBeginAutomaticCapture("f", DateTimeOffset.UtcNow, out var generation));
        recorder.Purge(BelemetryFeatures.Logs, discardCaptures: true);

        recorder.EndAutomaticCapture(BelemetryDiagnosticCapture.Failed(new(BelemetryCaptureTriggerKind.Automatic, "x"), "stand-in"), generation);

        Assert.IsEmpty(recorder.Pending());
        Assert.AreEqual(1, recorder.Dropped()["consent_revoked_capture"]);
    }

    [TestMethod]
    public async Task Clearing_ForgetsThePagesTimelineToo()
    {
        await using var context = new BunitContext();
        context.Services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Diagnostics.Enabled = true;
        });
        var module = context.JSInterop.SetupModule(BelemetryJsBridge.ModulePath);
        module.Mode = JSRuntimeMode.Loose;

        await context.Services.GetRequiredService<IBelemetryDiagnostics>().ClearAsync(TestContext.CancellationToken);

        Assert.Contains(i => i.Identifier == "clearDiagnostics", module.Invocations);
    }

    [TestMethod]
    public async Task WithoutExceptionMessages_TheirTemplatesGoToo()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.IncludeExceptionMessages = false);
        host.Host.Pipeline.Enqueue(new BelemetryLogRecord
        {
            TimeUnixNano = BelemetryTime.NowUnixNano(),
            Feature = BelemetryFeatures.Errors,
            Severity = BelemetrySeverity.Error,
            Attributes = new BelemetryAttributes().Set(BelemetrySemanticConventions.ExceptionType, "ShippingException").Set(BelemetrySemanticConventions.ExceptionMessage, "Cannot ship to 'Jane Doe'"),
        });

        var exception = host.Diagnostics.GetEvents().Single().Exception!;
        Assert.IsNull(exception.Message);
        Assert.IsNull(exception.MessageTemplate);
        Assert.IsNull(host.Diagnostics.GetExceptionGroups().Single().MessageTemplate);
        Assert.AreEqual(24, exception.Fingerprint.Length, "Still grouped, by a hash.");
    }

    [TestMethod]
    public void AnUnhandledInteropException_IsUnhandled_AndAnInteropFailure()
    {
        Assert.AreEqual("unhandled", BelemetryDiagnosticEventFactory.OriginOf("Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer", "Microsoft.JSInterop.JSException"));
        Assert.AreEqual("js-interop", BelemetryDiagnosticEventFactory.OriginOf("MyApp.Interop", "Microsoft.JSInterop.JSException"));
    }

    [TestMethod]
    public async Task AllExceptions_TakesTheBrowsersTriggersToo()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.AutoCaptureTriggers = BelemetryDiagnosticTriggers.AllExceptions);
        _ = host.Diagnostics;
        using var error = JsonDocument.Parse("""{"type":"TypeError","message":"x is undefined"}""");

        host.Bridge.OnDiagnosticTrigger(error.RootElement);

        await BelemetryDiagnosticCaptureTests.WaitForAsync(() => host.Diagnostics.PendingCaptures.Count == 1);
        Assert.AreEqual("browser-error", host.Diagnostics.PendingCaptures.Single().Trigger.Reason);
    }

    [TestMethod]
    public async Task ThePagesEvents_FollowTheCapturesLookbackAndActivityChoice()
    {
        await using var context = new BunitContext();
        context.Services.AddLogging();
        context.Services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Consent.Default = BelemetryFeatures.All;
            o.Diagnostics.Enabled = true;
        });
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using var payload = JsonDocument.Parse($$$"""
            {"v":1,"capacity":200,"dropped":{},"resource":{},"events":[
              {"seq":1,"time":{{{now - 3_600_000}}},"cat":"navigation","name":"page_view","sev":9,"feature":2,"route":"/old"},
              {"seq":2,"time":{{{now - 1000}}},"cat":"browser","name":"interaction","sev":9,"feature":4,"attrs":{"event.type":"click"}},
              {"seq":3,"time":{{{now - 500}}},"cat":"exception","name":"exception","sev":17,"outcome":"failure","feature":1,"exc":{"type":"TypeError","message":"boom"}}
            ]}
            """);
        var module = context.JSInterop.SetupModule(BelemetryJsBridge.ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<JsonElement>("getDiagnostics").SetResult(payload.RootElement.Clone());
        context.Services.GetRequiredService<BelemetryPipeline>().Start();
        var diagnostics = context.Services.GetRequiredService<IBelemetryDiagnostics>();

        var lookback = await diagnostics.CaptureAsync(new() { Lookback = TimeSpan.FromMinutes(1) }, TestContext.CancellationToken);
        Assert.AreSequenceEqual(new[] { "interaction", "exception" }, lookback.Events.Select(e => e.Name).ToArray(), Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);

        var failuresOnly = await diagnostics.CaptureAsync(new() { IncludeRecentActivity = false }, TestContext.CancellationToken);
        Assert.AreEqual("exception", failuresOnly.Events.Single().Name);
        Assert.AreEqual("TypeError", failuresOnly.Subject!.Exception!.Type);
    }

    [TestMethod]
    public async Task ARenderedCircuitId_IsNeverKept_EvenWithLogMessages()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.IncludeLogMessages = true);
        host.Factory.CreateLogger("Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost").LogWarning("Circuit {CircuitId} disconnected", "CfDJ8secret");

        var e = host.Diagnostics.GetEvents().Single();
        Assert.AreEqual("Circuit {CircuitId} disconnected", e.Message);
    }

    [TestMethod]
    public async Task ACancelledRequest_IsNotAFailure()
    {
        Assert.AreEqual(BelemetryDiagnosticOutcome.Cancelled, BelemetryDiagnosticEventFactory.OutcomeOf(BelemetryStatusCode.Error, "System.Threading.Tasks.TaskCanceledException"));

        await using var host = DiagnosticsTestHost.Create();
        var span = Records.Span();
        span.Status = BelemetryStatusCode.Error;
        span.Attributes.Set(BelemetrySemanticConventions.HttpRequestMethod, "GET");
        span.Attributes.Set(BelemetrySemanticConventions.ErrorType, "System.Threading.Tasks.TaskCanceledException");
        host.Host.Pipeline.Enqueue(span);

        var e = host.Diagnostics.GetEvents().Single();
        Assert.AreEqual(BelemetryDiagnosticOutcome.Cancelled, e.Outcome);
        Assert.IsFalse(e.IsFailure);
    }

    private sealed class RewriteStack : IBelemetryProcessor
    {
        public bool OnLog(BelemetryLogRecord log)
        {
            if (log.Attributes.Contains(BelemetrySemanticConventions.ExceptionStacktrace)) log.Attributes.Set(BelemetrySemanticConventions.ExceptionStacktrace, "(stack removed by the app)");
            return true;
        }
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
