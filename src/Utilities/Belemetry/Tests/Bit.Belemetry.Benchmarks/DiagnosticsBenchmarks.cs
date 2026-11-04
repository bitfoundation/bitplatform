using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Benchmarks;

/// <summary>
/// What production diagnostics add to the recording paths (the same entries and activities with the diagnostic timeline
/// off and on), and what a capture and its export cost. The error limits are off so every exception takes the full path,
/// as the first occurrence of each failure does.
/// </summary>
[MemoryDiagnoser]
public class DiagnosticsBenchmarks
{
    private const string ComponentsSource = "Microsoft.AspNetCore.Components";

    private ServiceProvider _services = default!;
    private AsyncServiceScope _scope;
    private IBelemetryDiagnostics _diagnostics = default!;
    private ILogger _logger = default!;
    private ActivitySource _components = default!;
    private Exception _exception = default!;

    [Params(false, true)]
    public bool Diagnostics { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _components = new ActivitySource(ComponentsSource);

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Information));
        services.AddBitBelemetryServices(o =>
        {
            o.ServiceName = "bench";
            o.ServiceVersion = "1.0.0";
            o.UseEnvironmentVariables = false;
            o.Consent.Default = BelemetryFeatures.All;
            o.Consent.Persist = false;
            o.Persistence.Enabled = false;
            o.Errors.DuplicateWindow = TimeSpan.Zero;
            o.Errors.MaxPerMinute = 0;
            o.Traces.Sources.Clear();
            o.Metrics.Meters.Clear();
            o.Diagnostics.Enabled = Diagnostics;
            o.AddExporter(new DiscardingExporter());
        });
        _services = services.BuildServiceProvider();
        _logger = _services.GetRequiredService<ILoggerFactory>().CreateLogger("Bench.Orders");
        _scope = _services.CreateAsyncScope();
        _diagnostics = _scope.ServiceProvider.GetRequiredService<IBelemetryDiagnostics>();

        try
        {
            throw new InvalidOperationException("Order 10423 could not be saved for jane@example.com", new TimeoutException("The database did not answer in 5000 ms."));
        }
        catch (Exception ex)
        {
            _exception = ex;
        }

        // A full timeline for the capture benchmarks: 300 activity events and 50 failures.
        for (var i = 0; i < 300; i++) _logger.LogWarning("Inventory slow for {Sku}", i);
        for (var i = 0; i < 50; i++) _logger.LogError(_exception, "Saving order {OrderId} failed", i);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _scope.DisposeAsync();
        await _services.DisposeAsync();
        _components.Dispose();
    }

    [Benchmark(Description = "ILogger: information (exported, below the timeline's level)")]
    public void LogInformation() => _logger.LogInformation("Order {OrderId} placed", 42);

    [Benchmark(Description = "ILogger: warning (exported, and on the timeline)")]
    public void LogWarning() => _logger.LogWarning("Inventory slow for {Sku}", 42);

    [Benchmark(Description = "ILogger: error with an inner exception (fingerprinted)")]
    public void LogException() => _logger.LogError(_exception, "Saving order {OrderId} failed", 42);

    [Benchmark(Description = "Framework HandleEvent activity: start + stop")]
    public void HandleEvent()
    {
        using var activity = _components.StartActivity("Microsoft.AspNetCore.Components.HandleEvent");
        activity?.SetTag("aspnetcore.components.type", "MyApp.Pages.Orders");
        activity?.SetTag("code.function.name", "Save");
    }

    [Benchmark(Description = "Capture: a full timeline (350 events), no browser")]
    public async Task<int> Capture()
    {
        var capture = await _diagnostics.CaptureAsync(new BelemetryDiagnosticCaptureOptions { IncludeBrowserEvidence = false });
        return capture.Events.Count;
    }

    [Benchmark(Description = "Capture + export: the support bundle's bytes")]
    public async Task<int> CaptureAndExport()
    {
        var capture = await _diagnostics.CaptureAsync(new BelemetryDiagnosticCaptureOptions { IncludeBrowserEvidence = false });
        if (capture.Succeeded is false) return 0;
        capture.Confirm();
        return (await capture.ExportAsync()).Length;
    }

    [Benchmark(Description = "Fingerprint: type, message, stack")]
    public string Fingerprint() => BelemetryExceptionFingerprint.Compute(_exception.GetType().FullName, _exception.Message, _exception.StackTrace);

    private sealed class DiscardingExporter : IBelemetryExporter
    {
        public string Name => "discard";

        public ValueTask<BelemetryExportResult> ExportAsync(BelemetryBatch batch, CancellationToken cancellationToken) => ValueTask.FromResult(BelemetryExportResult.Success);
    }
}
