using System.Diagnostics.Metrics;

namespace Bit.Bcrdt;

/// <summary>
/// The metrics bit Bcrdt publishes, for watching synchronization and storage in production.
/// </summary>
/// <remarks>
/// <para>
/// Everything is published on one <see cref="Meter"/> named <see cref="MeterName"/>, through
/// <c>System.Diagnostics.Metrics</c>: subscribe with OpenTelemetry (<c>.AddMeter(BcrdtDiagnostics.MeterName)</c>),
/// <c>dotnet-counters monitor --counters Bit.Bcrdt</c>, or a <see cref="MeterListener"/>. With nobody
/// listening an instrument costs a single check, so the metrics are always on.
/// </para>
/// <para>
/// No measurement carries a document identifier, a client identifier, a user or any content - only
/// counts, sizes, durations and the kind of thing measured. Identifiers routinely come from URLs and say
/// more than they should; correlate per document in your own code, where you know what is safe to record.
/// </para>
/// <list type="table">
/// <listheader><term>Instrument</term><description>What it counts</description></listheader>
/// <item><term><c>bcrdt.sync.messages.sent</c>, <c>bcrdt.sync.bytes.sent</c></term><description>Protocol messages a provider handed to its transport, by <c>bcrdt.message.kind</c>.</description></item>
/// <item><term><c>bcrdt.sync.messages.received</c>, <c>bcrdt.sync.bytes.received</c></term><description>Protocol messages a provider received, by <c>bcrdt.message.kind</c>.</description></item>
/// <item><term><c>bcrdt.sync.handshake.duration</c></term><description>Milliseconds from connecting to synced.</description></item>
/// <item><term><c>bcrdt.sync.reconnects</c></term><description>Reconnection attempts scheduled.</description></item>
/// <item><term><c>bcrdt.sync.errors</c></term><description>Failures reported through a provider's <c>Error</c> event, by <c>error.type</c>.</description></item>
/// <item><term><c>bcrdt.persistence.writes</c>, <c>bcrdt.persistence.bytes</c></term><description>Records written to a store.</description></item>
/// <item><term><c>bcrdt.persistence.failures</c></term><description>Store writes and compactions that failed.</description></item>
/// <item><term><c>bcrdt.persistence.compactions</c></term><description>Store compactions that completed.</description></item>
/// <item><term><c>bcrdt.host.documents</c>, <c>bcrdt.host.connections</c></term><description>Documents a document host has loaded, and connections to them.</description></item>
/// <item><term><c>bcrdt.host.messages.dropped</c></term><description>Messages a document host refused to apply or relay, by <c>bcrdt.reason</c>.</description></item>
/// </list>
/// </remarks>
public static class BcrdtDiagnostics
{
    /// <summary>The name of the meter every bit Bcrdt metric is published on.</summary>
    public const string MeterName = "Bit.Bcrdt";

    internal static readonly Meter Meter = new(MeterName, typeof(BcrdtDiagnostics).Assembly.GetName().Version?.ToString());

    internal static readonly Counter<long> MessagesSent = Meter.CreateCounter<long>(
        "bcrdt.sync.messages.sent", "{message}", "Protocol messages handed to a transport.");

    internal static readonly Counter<long> BytesSent = Meter.CreateCounter<long>(
        "bcrdt.sync.bytes.sent", "By", "Bytes of protocol messages handed to a transport.");

    internal static readonly Counter<long> MessagesReceived = Meter.CreateCounter<long>(
        "bcrdt.sync.messages.received", "{message}", "Protocol messages received from a transport.");

    internal static readonly Counter<long> BytesReceived = Meter.CreateCounter<long>(
        "bcrdt.sync.bytes.received", "By", "Bytes of protocol messages received from a transport.");

    internal static readonly Histogram<double> HandshakeDuration = Meter.CreateHistogram<double>(
        "bcrdt.sync.handshake.duration", "ms", "How long a connection took to become synced.");

    internal static readonly Counter<long> Reconnects = Meter.CreateCounter<long>(
        "bcrdt.sync.reconnects", "{attempt}", "Reconnection attempts scheduled.");

    internal static readonly Counter<long> SyncErrors = Meter.CreateCounter<long>(
        "bcrdt.sync.errors", "{error}", "Failures a synchronization provider reported.");

    internal static readonly Counter<long> PersistenceWrites = Meter.CreateCounter<long>(
        "bcrdt.persistence.writes", "{record}", "Records written to a document store.");

    internal static readonly Counter<long> PersistenceBytes = Meter.CreateCounter<long>(
        "bcrdt.persistence.bytes", "By", "Bytes written to a document store.");

    internal static readonly Counter<long> PersistenceFailures = Meter.CreateCounter<long>(
        "bcrdt.persistence.failures", "{failure}", "Document store writes and compactions that failed.");

    internal static readonly Counter<long> PersistenceCompactions = Meter.CreateCounter<long>(
        "bcrdt.persistence.compactions", "{compaction}", "Document store compactions that completed.");

    internal static KeyValuePair<string, object?> Kind(BcrdtSyncMessageKind kind) => new("bcrdt.message.kind", KindName(kind));

    internal static KeyValuePair<string, object?> Kind(ReadOnlySpan<byte> message) =>
        message.Length >= 2 && Enum.IsDefined((BcrdtSyncMessageKind)message[1])
            ? Kind((BcrdtSyncMessageKind)message[1])
            : new("bcrdt.message.kind", "unknown");

    // Fixed strings rather than ToString, so a tag never allocates on the hot path.
    private static string KindName(BcrdtSyncMessageKind kind) => kind switch
    {
        BcrdtSyncMessageKind.SyncStep1 => "sync_step1",
        BcrdtSyncMessageKind.SyncStep2 => "sync_step2",
        BcrdtSyncMessageKind.Update => "update",
        BcrdtSyncMessageKind.AwarenessUpdate => "awareness_update",
        BcrdtSyncMessageKind.AwarenessQuery => "awareness_query",
        BcrdtSyncMessageKind.Auth => "auth",
        BcrdtSyncMessageKind.Ping => "ping",
        BcrdtSyncMessageKind.Pong => "pong",
        BcrdtSyncMessageKind.SyncStep1Reply => "sync_step1_reply",
        _ => "unknown",
    };
}
