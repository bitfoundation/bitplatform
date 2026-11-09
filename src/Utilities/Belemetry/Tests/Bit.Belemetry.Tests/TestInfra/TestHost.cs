using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests;

/// <summary>
/// A Belemetry service provider wired for tests: an in-memory exporter, no environment variables, no persisted
/// consent, no persistence, no process-wide unhandled exception hooks, and timers long enough that only explicit
/// flushes export.
/// </summary>
internal sealed class TestHost : IAsyncDisposable
{
    private TestHost(ServiceProvider services, BelemetryInMemoryExporter memory)
    {
        Services = services;
        Memory = memory;
    }

    public ServiceProvider Services { get; }

    public BelemetryInMemoryExporter Memory { get; }

    public BelemetryPipeline Pipeline => Services.GetRequiredService<BelemetryPipeline>();

    public BelemetrySettings Settings => Services.GetRequiredService<BelemetrySettings>();

    public BelemetryConsentState Consent => Services.GetRequiredService<BelemetryConsentState>();

    /// <param name="configureServices">Extra registrations, e.g. a logger provider that captures Belemetry's own diagnostics.</param>
    public static TestHost Create(Action<BelemetryOptions>? configure = null, BelemetryFeatures consentDefault = BelemetryFeatures.All, Action<IServiceCollection>? configureServices = null)
    {
        var memory = new BelemetryInMemoryExporter();
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace));
        configureServices?.Invoke(services);
        services.AddBitBelemetryServices(o =>
        {
            ApplyTestDefaults(o);
            o.Consent.Default = consentDefault;
            o.AddExporter(memory);
            configure?.Invoke(o);
        });

        return new TestHost(services.BuildServiceProvider(), memory);
    }

    public static void ApplyTestDefaults(BelemetryOptions o)
    {
        o.ServiceName = "belemetry-tests";
        o.ServiceVersion = "1.2.3";
        o.ServiceInstanceId = "instance-1";
        o.UseEnvironmentVariables = false;
        o.Consent.Persist = false;
        o.Persistence.Enabled = false;
        // Process-wide: a faulted task another test dropped would be finalized into whichever test's pipeline is
        // listening. BelemetryUnhandledExceptionTests turns it on.
        o.Errors.CaptureUnhandledExceptions = false;
        o.Batch.FlushInterval = TimeSpan.FromHours(1);
        o.Metrics.ExportInterval = TimeSpan.FromHours(1);
        o.Traces.Sources.Clear();
        o.Metrics.Meters.Clear();
        // What discovery finds depends on the entry assembly - the test runner's; BelemetryPageRoutesTests covers it.
        o.Browser.DiscoverPageRoutes = false;
    }

    /// <summary>Builds settings straight from options, without DI.</summary>
    public static BelemetrySettings CreateSettings(Action<BelemetryOptions>? configure = null, bool webHost = false)
    {
        var options = new BelemetryOptions();
        ApplyTestDefaults(options);
        configure?.Invoke(options);
        return new BelemetrySettings(options, new BelemetryHostDetection(webHost));
    }

    /// <summary>Starts the pipeline (and its collectors) by resolving the logger factory, as a real host does.</summary>
    public ILoggerFactory StartLogging()
    {
        var factory = Services.GetRequiredService<ILoggerFactory>();
        Pipeline.Start();
        return factory;
    }

    public Task FlushAsync(bool includeMetrics = false) => Pipeline.FlushAsync(includeMetrics, CancellationToken.None);

    /// <summary>Flushes, then waits for a condition the background loop may still be completing.</summary>
    public async Task FlushUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var started = Environment.TickCount64;
        while (true)
        {
            await FlushAsync();
            if (condition()) return;
            if (Environment.TickCount64 - started > timeoutMs) Assert.Fail("The condition was not met in time.");
            await Task.Delay(20);
        }
    }

    public async ValueTask DisposeAsync() => await Services.DisposeAsync();
}
