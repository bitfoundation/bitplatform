using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Benchmarks;

/// <summary>
/// What instrumented app code pays, synchronously, for one log entry, one span or one measurement with Belemetry
/// listening: the conversion, consent, the privacy rules, processors and the queue - everything but the export, which
/// runs later on the pipeline's loop (here into an exporter that discards).
/// </summary>
[MemoryDiagnoser]
public class RecordingBenchmarks
{
    private const string SourceName = "Bench.Recording";

    private ServiceProvider _services = default!;
    private ILogger _logger = default!;
    private ActivitySource _source = default!;
    private Meter _meter = default!;
    private Counter<long> _counter = default!;
    private Histogram<double> _histogram = default!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new ActivitySource(SourceName);
        _meter = new Meter(SourceName);
        _counter = _meter.CreateCounter<long>("bench.orders");
        _histogram = _meter.CreateHistogram<double>("bench.duration", "ms");

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Information));
        services.AddBitBelemetryServices(o =>
        {
            o.ServiceName = "bench";
            o.UseEnvironmentVariables = false;
            o.Consent.Default = BelemetryFeatures.All;
            o.Consent.Persist = false;
            o.Persistence.Enabled = false;
            o.Traces.Sources.Clear();
            o.Traces.Sources.Add(SourceName);
            o.Metrics.Meters.Clear();
            o.Metrics.Meters.Add(SourceName);
            o.AddExporter(new DiscardingExporter());
        });
        _services = services.BuildServiceProvider();
        _logger = _services.GetRequiredService<ILoggerFactory>().CreateLogger("Bench.Orders");
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _services.DisposeAsync();
        _source.Dispose();
        _meter.Dispose();
    }

    [Benchmark(Description = "ILogger: structured entry")]
    public void LogStructured() => _logger.LogInformation("Order {OrderId} placed by {Email}", 42, "jane@example.com");

    [Benchmark(Description = "ILogger: below the minimum level")]
    public void LogFiltered() => _logger.LogDebug("Order {OrderId} cached", 42);

    [Benchmark(Description = "Activity: start + stop, two tags")]
    public void Span()
    {
        using var activity = _source.StartActivity("PlaceOrder");
        activity?.SetTag("order.id", 42);
        activity?.SetTag("order.channel", "web");
    }

    [Benchmark(Description = "Counter: Add, two tags")]
    public void CounterAdd() => _counter.Add(1, new KeyValuePair<string, object?>("channel", "web"), new KeyValuePair<string, object?>("region", "eu"));

    [Benchmark(Description = "Histogram: Record, one tag")]
    public void HistogramRecord() => _histogram.Record(12.5, new KeyValuePair<string, object?>("route", "/orders/{id}"));

    private sealed class DiscardingExporter : IBelemetryExporter
    {
        public string Name => "discard";

        public ValueTask<BelemetryExportResult> ExportAsync(BelemetryBatch batch, CancellationToken cancellationToken) => ValueTask.FromResult(BelemetryExportResult.Success);
    }
}
