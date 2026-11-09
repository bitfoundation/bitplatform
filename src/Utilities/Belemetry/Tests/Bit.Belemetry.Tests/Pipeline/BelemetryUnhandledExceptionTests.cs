using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace Bit.Belemetry.Tests.Pipeline;

[TestClass]
public class BelemetryUnhandledExceptionTests
{
    private string _directory = default!;

    [TestInitialize]
    public void Initialize() => _directory = Path.Combine(Path.GetTempPath(), "bit-belemetry-tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    private string[] StoredFiles() => Directory.Exists(_directory) ? Directory.GetFiles(_directory) : [];

    private static TestHost CreateHost(Action<BelemetryOptions>? configure = null, BelemetryFeatures consentDefault = BelemetryFeatures.All)
    {
        var host = TestHost.Create(o =>
        {
            o.Errors.CaptureUnhandledExceptions = true;
            configure?.Invoke(o);
        }, consentDefault);
        host.StartLogging();
        return host;
    }

    /// <summary>A pipeline with an OTLP exporter and a file store, as a MAUI or desktop app has, started as a host starts it.</summary>
    private ServiceProvider CreateProvider(FakeHttpMessageHandler handler, BelemetryFeatures consent = BelemetryFeatures.All)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new BelemetryHttpMessageHandlerProvider(handler));
        services.AddBitBelemetryServices(o =>
        {
            TestHost.ApplyTestDefaults(o);
            o.Errors.CaptureUnhandledExceptions = true;
            o.Consent.Default = consent;
            o.Persistence.Enabled = true;
            o.Persistence.Directory = _directory;
            o.AddOtlpExporter("http://collector.test:4318", e =>
            {
                e.Signals = BelemetrySignals.Logs;
                e.Headers["x-api-key"] = "pw-7f3a";
            });
        });

        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ILoggerFactory>();
        return provider;
    }

    [TestMethod]
    public async Task TheHooks_AreInstalledByDefault_AndOnlyWhenSomethingTakesTheirRecords()
    {
        Assert.IsTrue(new BelemetryOptions().Errors.CaptureUnhandledExceptions, "On by default.");

        await using (var host = CreateHost()) Assert.IsNotNull(host.Pipeline.UnhandledExceptions);
        await using (var host = CreateHost(o => o.Errors.CaptureUnhandledExceptions = false)) Assert.IsNull(host.Pipeline.UnhandledExceptions);
        await using (var host = CreateHost(o => o.Features = BelemetryFeatures.All & ~BelemetryFeatures.Errors)) Assert.IsNull(host.Pipeline.UnhandledExceptions);
        await using (var host = CreateHost(o => o.Enabled = false)) Assert.IsNull(host.Pipeline.UnhandledExceptions);

        // A server pipeline that does not collect .NET telemetry exports none of it; its diagnostics still want them.
        var server = TestHost.CreateSettings(o =>
        {
            o.Errors.CaptureUnhandledExceptions = true;
            o.AddConsoleExporter();
        }, webHost: true);
        Assert.IsFalse(BelemetryUnhandledExceptions.AppliesTo(server, diagnostics: false));
        Assert.IsTrue(BelemetryUnhandledExceptions.AppliesTo(server, diagnostics: true));
    }

    [TestMethod]
    public async Task AnUnhandledException_IsAnErrorsEvent_CarryingTheException()
    {
        await using var host = CreateHost();
        var exception = Thrown(new InvalidOperationException("The order book is closed."));

        host.Pipeline.UnhandledExceptions!.OnUnhandled(exception, terminating: true);
        await host.FlushAsync();

        var log = host.Memory.Logs.Single();
        Assert.AreEqual(BelemetryFeatures.Errors, log.Feature);
        Assert.AreEqual(BelemetrySeverity.Fatal, log.Severity, "It ended the process.");
        Assert.AreEqual("Critical", log.SeverityText);
        Assert.AreEqual(BelemetrySemanticConventions.ExceptionEvent, log.EventName, "An event: every exporter gets it, whatever its minimum level.");
        Assert.AreEqual(BelemetryUnhandledExceptions.ScopeName, log.Scope.Name);
        Assert.AreEqual("An unhandled exception ended the process.", log.Body);
        Assert.AreEqual("System.InvalidOperationException", log.Attributes[BelemetrySemanticConventions.ExceptionType]);
        Assert.AreEqual("The order book is closed.", log.Attributes[BelemetrySemanticConventions.ExceptionMessage]);
        StringAssert.Contains((string)log.Attributes[BelemetrySemanticConventions.ExceptionStacktrace]!, nameof(Thrown));
        Assert.IsNull(log.TraceId);
    }

    [TestMethod]
    public async Task AnUnhandledException_ThatDoesNotEndTheProcess_IsAnError_UnderTheSpanItWasThrownIn()
    {
        await using var host = CreateHost();
        using var activity = new Activity("checkout").SetIdFormat(ActivityIdFormat.W3C).Start();

        host.Pipeline.UnhandledExceptions!.OnUnhandled(new InvalidOperationException("boom"), terminating: false);
        activity.Stop();
        await host.FlushAsync();

        var log = host.Memory.Logs.Single();
        Assert.AreEqual(BelemetrySeverity.Error, log.Severity);
        Assert.AreEqual("Error", log.SeverityText);
        Assert.AreEqual("An unhandled exception was thrown.", log.Body);
        Assert.AreEqual(activity.TraceId.ToHexString(), log.TraceId);
        Assert.AreEqual(activity.SpanId.ToHexString(), log.SpanId);
    }

    [TestMethod]
    public async Task AnUnobservedTaskException_IsRecorded_WithTheTasksOwnException()
    {
        await using var host = CreateHost();
        var hooks = host.Pipeline.UnhandledExceptions!;

        hooks.OnUnobserved(new AggregateException(new AggregateException(new TimeoutException("one"))));
        hooks.OnUnobserved(new AggregateException(new TimeoutException("two"), new ArgumentException("three")));
        await host.FlushAsync();

        var logs = host.Memory.Logs.ToList();
        Assert.HasCount(2, logs);
        Assert.AreEqual("System.TimeoutException", logs[0].Attributes[BelemetrySemanticConventions.ExceptionType], "A lone exception is unwrapped, nested aggregates included.");
        Assert.AreEqual("one", logs[0].Attributes[BelemetrySemanticConventions.ExceptionMessage]);
        Assert.AreEqual("System.AggregateException", logs[1].Attributes[BelemetrySemanticConventions.ExceptionType], "Several stay together.");
        StringAssert.Contains((string)logs[1].Attributes[BelemetrySemanticConventions.ExceptionStacktrace]!, "three");
        Assert.IsTrue(logs.All(l => l.Severity == BelemetrySeverity.Error && l.Feature == BelemetryFeatures.Errors && l.Body == "A faulted task's exception was never observed."));
    }

    [TestMethod]
    public async Task AFaultedTaskNothingObserved_IsReportedWhenItIsCollected()
    {
        await using var host = CreateHost();
        var marker = "unobserved-" + Guid.NewGuid().ToString("N");

        DropAFaultedTask(marker);
        await host.FlushUntilAsync(() =>
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return host.Memory.Logs.Any(l => Equals(l.Attributes[BelemetrySemanticConventions.ExceptionMessage], marker));
        });

        var log = host.Memory.Logs.First(l => Equals(l.Attributes[BelemetrySemanticConventions.ExceptionMessage], marker));
        Assert.AreEqual("System.InvalidOperationException", log.Attributes[BelemetrySemanticConventions.ExceptionType]);
    }

    [TestMethod]
    public async Task TheErrorLimits_ApplyToThem()
    {
        await using var host = CreateHost(o => o.Errors.MaxPerMinute = 1);
        var hooks = host.Pipeline.UnhandledExceptions!;

        hooks.OnUnobserved(new AggregateException(new TimeoutException("first")));
        hooks.OnUnobserved(new AggregateException(new TimeoutException("second")));
        await host.FlushAsync();

        Assert.AreEqual("first", host.Memory.Logs.Single().Attributes[BelemetrySemanticConventions.ExceptionMessage]);
    }

    [TestMethod]
    public async Task WithoutErrorsConsent_NothingIsRecorded()
    {
        await using var host = CreateHost(consentDefault: BelemetryFeatures.All & ~BelemetryFeatures.Errors);
        host.Consent.Decide(BelemetryFeatures.All & ~BelemetryFeatures.Errors);

        host.Pipeline.UnhandledExceptions!.OnUnhandled(new InvalidOperationException("boom"), terminating: false);
        await host.FlushAsync();

        Assert.IsEmpty(host.Memory.Logs);
    }

    [TestMethod]
    public async Task ATerminatingException_SavesTheQueueToDisk_AndTheNextLaunchSendsIt()
    {
        var handler = new FakeHttpMessageHandler();
        await using (var provider = CreateProvider(handler))
        {
            var pipeline = provider.GetRequiredService<BelemetryPipeline>();
            pipeline.Enqueue(Records.Log("placing the order", severity: BelemetrySeverity.Warn));

            pipeline.UnhandledExceptions!.OnUnhandled(new InvalidOperationException("The order book is closed."), terminating: true);

            Assert.IsEmpty(handler.Requests, "Nothing async completes as the process ends: it is saved, not sent.");
            var files = StoredFiles();
            Assert.HasCount(1, files);
            Assert.IsFalse(File.ReadAllText(files[0]).Contains("pw-7f3a", StringComparison.Ordinal), "Headers (API keys) are never written to disk.");
            Assert.IsEmpty(pipeline.DrainForUnload(), "The queue went to disk.");
        }

        // The next launch: its first resend pass delivers the crash with what led to it.
        await using (var provider = CreateProvider(handler))
        {
            await provider.GetRequiredService<BelemetryPipeline>().Exporters.RetryStoredAsync(persistenceAllowed: true, CancellationToken.None);
        }

        var request = handler.Requests.Single();
        Assert.AreEqual("http://collector.test:4318/v1/logs", request.Message.RequestUri!.ToString());
        Assert.AreEqual("pw-7f3a", request.Message.Headers.GetValues("x-api-key").Single(), "The exporter's current headers are added when it is sent.");
        var logs = ExportLogsServiceRequest.Parser.ParseFrom(request.Body).ResourceLogs.SelectMany(r => r.ScopeLogs).SelectMany(s => s.LogRecords).ToList();
        CollectionAssert.AreEqual(new[] { "placing the order", "An unhandled exception ended the process." }, logs.Select(l => l.Body.StringValue).ToArray());
        Assert.AreEqual("System.InvalidOperationException", logs[1].Attributes.Single(a => a.Key == BelemetrySemanticConventions.ExceptionType).Value.StringValue);
        Assert.IsEmpty(StoredFiles());
    }

    [TestMethod]
    public async Task ATerminatingException_WithoutPersistenceConsent_WritesNothingToDisk()
    {
        var handler = new FakeHttpMessageHandler();
        await using var provider = CreateProvider(handler, consent: BelemetryFeatures.All & ~BelemetryFeatures.Persistence);
        var pipeline = provider.GetRequiredService<BelemetryPipeline>();

        pipeline.UnhandledExceptions!.OnUnhandled(new InvalidOperationException("boom"), terminating: true);

        Assert.IsEmpty(StoredFiles());

        // Should the process go on after all, the record is still queued for the export.
        await pipeline.FlushAsync(false, CancellationToken.None);
        Assert.HasCount(1, handler.Requests);
    }

    [TestMethod]
    public async Task ATerminatingException_BeforeAConsentDecision_StaysOffTheDisk()
    {
        var handler = new FakeHttpMessageHandler();
        await using var provider = CreateProvider(handler, consent: BelemetryFeatures.Persistence);
        var pipeline = provider.GetRequiredService<BelemetryPipeline>();

        pipeline.UnhandledExceptions!.OnUnhandled(new InvalidOperationException("boom"), terminating: true);

        Assert.IsEmpty(StoredFiles(), "Records held for a decision never go on the wire or on disk.");
    }

    [TestMethod]
    public async Task InWebAssembly_ATerminatingException_HandsTheQueueToThePage()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var options = new BelemetryOptions();
        TestHost.ApplyTestDefaults(options);
        options.Errors.CaptureUnhandledExceptions = true;
        options.Consent.Default = BelemetryFeatures.All;
        options.AddOtlpExporter("http://collector.test:4318", e => e.Headers["x-api-key"] = "pw-7f3a");
        services.AddSingleton(new BelemetrySettings(options, new BelemetryHostDetection(false), isBrowser: true));
        services.AddBitBelemetryServices();
        await using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ILoggerFactory>();
        var pipeline = provider.GetRequiredService<BelemetryPipeline>();
        var sent = new List<BelemetryHttpRequest>();
        provider.GetRequiredService<BelemetryBrowserBridgeRegistry>().SendRequests = requests =>
        {
            sent.AddRange(requests);
            return ValueTask.CompletedTask;
        };

        pipeline.UnhandledExceptions!.OnUnhandled(new InvalidOperationException("boom"), terminating: true);

        var request = sent.Single();
        Assert.AreEqual("http://collector.test:4318/v1/logs", request.Url);
        Assert.AreEqual("pw-7f3a", request.Headers!["x-api-key"], "The page sends it itself, as it sends the unload drain's.");
        Assert.IsTrue(request.Features!.Value.HasFlag(BelemetryFeatures.Errors));
        var log = ExportLogsServiceRequest.Parser.ParseFrom(request.Body).ResourceLogs[0].ScopeLogs[0].LogRecords.Single();
        Assert.AreEqual("An unhandled exception ended the process.", log.Body.StringValue);
    }

    [TestMethod]
    public void DiagnosticsCountThem_AsUnhandled()
    {
        Assert.AreEqual("unhandled", BelemetryDiagnosticEventFactory.OriginOf(BelemetryUnhandledExceptions.ScopeName, "System.InvalidOperationException"));
    }

    private static Exception Thrown(Exception exception)
    {
        try
        {
            throw exception;
        }
        catch (Exception caught)
        {
            return caught;
        }
    }

    // Not inlined, so no local of the caller keeps the task alive for the collector.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropAFaultedTask(string message) => _ = Task.FromException(new InvalidOperationException(message));
}
