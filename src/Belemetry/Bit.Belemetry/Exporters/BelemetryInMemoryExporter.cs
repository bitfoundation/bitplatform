using System.Collections.Concurrent;

namespace Bit.Belemetry;

/// <summary>
/// Keeps exported records in memory - for tests, and for in-app telemetry viewers. Add it with
/// <see cref="BelemetryOptions.AddExporter"/>.
/// </summary>
/// <param name="capacity">How many records of each signal are kept; the oldest are discarded past it.</param>
public sealed class BelemetryInMemoryExporter(int capacity = 1000) : IBelemetryExporter
{
    private readonly ConcurrentQueue<BelemetrySpan> _spans = new();
    private readonly ConcurrentQueue<BelemetryLogRecord> _logs = new();
    private readonly ConcurrentQueue<BelemetryMetric> _metrics = new();

    /// <inheritdoc />
    public string Name => "in-memory";

    /// <summary>The exported spans, oldest first.</summary>
    public IReadOnlyCollection<BelemetrySpan> Spans => _spans;

    /// <summary>The exported log records, oldest first.</summary>
    public IReadOnlyCollection<BelemetryLogRecord> Logs => _logs;

    /// <summary>The exported metrics, oldest first.</summary>
    public IReadOnlyCollection<BelemetryMetric> Metrics => _metrics;

    /// <summary>The resource of the last exported batch.</summary>
    public BelemetryResource? LastResource { get; private set; }

    /// <summary>Raised after every export.</summary>
    public event Action<BelemetryBatch>? Exported;

    /// <inheritdoc />
    public ValueTask<BelemetryExportResult> ExportAsync(BelemetryBatch batch, CancellationToken cancellationToken)
    {
        LastResource = batch.Resource;
        Add(_spans, batch.Spans);
        Add(_logs, batch.Logs);
        Add(_metrics, batch.Metrics);
        Exported?.Invoke(batch);
        return ValueTask.FromResult(BelemetryExportResult.Success);
    }

    /// <summary>Removes every kept record.</summary>
    public void Clear()
    {
        _spans.Clear();
        _logs.Clear();
        _metrics.Clear();
    }

    private void Add<T>(ConcurrentQueue<T> queue, IReadOnlyList<T> items)
    {
        foreach (var item in items)
        {
            queue.Enqueue(item);
            while (queue.Count > capacity && queue.TryDequeue(out _)) { }
        }
    }
}
