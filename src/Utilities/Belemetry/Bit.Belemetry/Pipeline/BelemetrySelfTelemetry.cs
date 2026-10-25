using System.Diagnostics.Metrics;

namespace Bit.Belemetry;

/// <summary>
/// Belemetry's own health, as metrics on the <see cref="MeterName"/> meter: the records it dropped and why, and what
/// became of the records it exported. Collected like any meter once its name is in
/// <see cref="BelemetryMetricsOptions.Meters"/>, so an operator can watch for data loss on a dashboard instead of
/// finding out from a gap; the counters cost nothing while nobody listens.
/// </summary>
internal sealed class BelemetrySelfTelemetry : IDisposable
{
    /// <summary>The meter name to add to <see cref="BelemetryMetricsOptions.Meters"/>.</summary>
    public const string MeterName = "Bit.Belemetry";

    private readonly Meter _meter = new(MeterName, BelemetrySettings.SdkVersion);
    private readonly Counter<long> _dropped;
    private readonly Counter<long> _exported;

    public BelemetrySelfTelemetry()
    {
        _dropped = _meter.CreateCounter<long>("belemetry.records.dropped", "{record}",
            "Records Belemetry dropped before export, by reason: queue_full, pending_full (the hold before a consent decision), not_consented, consent_revoked, error_limit, relay_limit, filtered (a processor).");
        _exported = _meter.CreateCounter<long>("belemetry.records.exported", "{record}",
            "Records Belemetry handed to an exporter, by outcome: delivered, rejected (refused for good), stored (kept for a later retry), failed (lost).");
    }

    public void Dropped(string reason, bool span, long count = 1)
    {
        if (count <= 0 || _dropped.Enabled is false) return;
        _dropped.Add(count, new KeyValuePair<string, object?>("belemetry.reason", reason), new KeyValuePair<string, object?>("belemetry.signal", span ? "traces" : "logs"));
    }

    public void Exported(string exporter, string outcome, long count)
    {
        if (count <= 0 || _exported.Enabled is false) return;
        _exported.Add(count, new KeyValuePair<string, object?>("belemetry.exporter", exporter), new KeyValuePair<string, object?>("belemetry.outcome", outcome));
    }

    public void Dispose() => _meter.Dispose();
}
