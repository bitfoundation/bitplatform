namespace Bit.Belemetry;

/// <summary>Sends batches of telemetry to a backend.</summary>
/// <remarks>
/// Export calls are serialized - the pipeline never calls <see cref="ExportAsync"/> concurrently on one exporter - and
/// run with Belemetry's own instrumentation suppressed, so HTTP calls made inside them are not traced.
/// </remarks>
public interface IBelemetryExporter
{
    /// <summary>A short name for diagnostics.</summary>
    string Name { get; }

    /// <summary>Exports <paramref name="batch"/>.</summary>
    ValueTask<BelemetryExportResult> ExportAsync(BelemetryBatch batch, CancellationToken cancellationToken);
}
