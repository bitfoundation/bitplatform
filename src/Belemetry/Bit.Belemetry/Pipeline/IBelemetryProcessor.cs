namespace Bit.Belemetry;

/// <summary>
/// Inspects, enriches or drops records on the .NET side before they are queued for export - including records
/// relayed from the browser.
/// </summary>
/// <remarks>
/// Processors run synchronously on the thread that produced the record (inside the code that logged or stopped the
/// activity), so they must be fast and must not block or throw. A processor that throws is skipped for that record.
/// </remarks>
public interface IBelemetryProcessor
{
    /// <summary>Called for every finished span. Return <see langword="false"/> to drop it.</summary>
    bool OnSpan(BelemetrySpan span) => true;

    /// <summary>Called for every log record or event. Return <see langword="false"/> to drop it.</summary>
    bool OnLog(BelemetryLogRecord log) => true;

    /// <summary>Called for every collected metric. Return <see langword="false"/> to drop it.</summary>
    bool OnMetric(BelemetryMetric metric) => true;
}
