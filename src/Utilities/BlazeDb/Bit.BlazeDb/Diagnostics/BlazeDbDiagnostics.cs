using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bit.BlazeDb;

/// <summary>
/// The metrics BlazeDb publishes, for watching commits and the durability pipeline in production.
/// </summary>
/// <remarks>
/// <para>
/// Everything is published on one <see cref="Meter"/> named <see cref="MeterName"/>, through
/// <c>System.Diagnostics.Metrics</c>: subscribe with OpenTelemetry (<c>.AddMeter(BlazeDbDiagnostics.MeterName)</c>),
/// Belemetry, <c>dotnet-counters monitor --counters Bit.BlazeDb</c>, or a <see cref="MeterListener"/>. With nobody
/// listening an instrument costs a single check, so the metrics are always on. Reads are never measured: they
/// are method calls on memory, and timing them would cost more than they do.
/// </para>
/// <para>
/// No measurement carries a table name, a key, a row or any other content - only counts, sizes, durations and
/// the kind of operation. Measurements from every open database are aggregated together.
/// </para>
/// <list type="table">
/// <listheader><term>Instrument</term><description>What it measures</description></listheader>
/// <item><term><c>blazedb.commits</c></term><description>Commits that changed rows: autocommit writes, committed transactions and bulk writes.</description></item>
/// <item><term><c>blazedb.rollbacks</c></term><description>Transactions rolled back: explicitly, by disposing them uncommitted, or because their commit was refused.</description></item>
/// <item><term><c>blazedb.wal.flushes</c>, <c>blazedb.wal.flushed_bytes</c></term><description>Appends of buffered commits to the write-ahead log, and their size.</description></item>
/// <item><term><c>blazedb.wal.flush.duration</c></term><description>Milliseconds a log append took on storage - how far durability lags a commit, beyond the flush interval.</description></item>
/// <item><term><c>blazedb.checkpoints</c>, <c>blazedb.checkpoint.duration</c>, <c>blazedb.checkpoint.snapshot_bytes</c></term><description>Checkpoints that completed, how long each took and how big its snapshot was.</description></item>
/// <item><term><c>blazedb.recovery.duration</c>, <c>blazedb.recovery.replayed_commits</c></term><description>Rebuilding memory from storage on open (and on a replica's reload), and the log records it replayed.</description></item>
/// <item><term><c>blazedb.storage.failures</c></term><description>Flushes, checkpoints and recoveries that failed, by <c>blazedb.operation</c> and <c>error.type</c> - a checkpoint attempted to free space counts on its own.</description></item>
/// <item><term><c>blazedb.quota.refusals</c></term><description>Flush attempts refused for lack of space - predicted from the estimate, or reported by the storage - after compacting did not help. The flusher retries every interval, so a full origin counts once per attempt.</description></item>
/// </list>
/// </remarks>
public static class BlazeDbDiagnostics
{
    /// <summary>The name of the meter every BlazeDb metric is published on.</summary>
    public const string MeterName = "Bit.BlazeDb";

    internal static readonly Meter Meter = new(MeterName, typeof(BlazeDbDiagnostics).Assembly.GetName().Version?.ToString());

    internal static readonly Counter<long> Commits = Meter.CreateCounter<long>(
        "blazedb.commits", "{commit}", "Commits that changed rows.");

    internal static readonly Counter<long> Rollbacks = Meter.CreateCounter<long>(
        "blazedb.rollbacks", "{rollback}", "Transactions rolled back.");

    internal static readonly Counter<long> WalFlushes = Meter.CreateCounter<long>(
        "blazedb.wal.flushes", "{flush}", "Appends of buffered commits to the write-ahead log.");

    internal static readonly Counter<long> WalFlushedBytes = Meter.CreateCounter<long>(
        "blazedb.wal.flushed_bytes", "By", "Bytes appended to the write-ahead log.");

    internal static readonly Histogram<double> WalFlushDuration = Meter.CreateHistogram<double>(
        "blazedb.wal.flush.duration", "ms", "How long an append to the write-ahead log took on storage.");

    internal static readonly Counter<long> Checkpoints = Meter.CreateCounter<long>(
        "blazedb.checkpoints", "{checkpoint}", "Checkpoints that completed.");

    internal static readonly Histogram<double> CheckpointDuration = Meter.CreateHistogram<double>(
        "blazedb.checkpoint.duration", "ms", "How long a checkpoint took, from encoding the snapshot to publishing the new generation.");

    internal static readonly Histogram<long> CheckpointSnapshotBytes = Meter.CreateHistogram<long>(
        "blazedb.checkpoint.snapshot_bytes", "By", "The size of the snapshot a checkpoint wrote.");

    internal static readonly Histogram<double> RecoveryDuration = Meter.CreateHistogram<double>(
        "blazedb.recovery.duration", "ms", "How long rebuilding memory from storage took.");

    internal static readonly Counter<long> RecoveryReplayedCommits = Meter.CreateCounter<long>(
        "blazedb.recovery.replayed_commits", "{commit}", "Write-ahead log records replayed while recovering.");

    internal static readonly Counter<long> StorageFailures = Meter.CreateCounter<long>(
        "blazedb.storage.failures", "{failure}", "Flushes, checkpoints and recoveries that failed.");

    internal static readonly Counter<long> QuotaRefusals = Meter.CreateCounter<long>(
        "blazedb.quota.refusals", "{refusal}", "Flush attempts refused for lack of storage space.");

    internal const string FlushOperation = "flush";
    internal const string CheckpointOperation = "checkpoint";
    internal const string RecoveryOperation = "recovery";

    /// <summary>Counts a failed storage operation, tagged with what failed and the exception's type - never its message, which can quote data. Cancellation is not counted.</summary>
    internal static void RecordFailure(string operation, Exception exception)
    {
        // A cancelled operation did not fail; it was asked to stop.
        if (StorageFailures.Enabled && exception is not OperationCanceledException)
        {
            StorageFailures.Add(1,
                new KeyValuePair<string, object?>("blazedb.operation", operation),
                new KeyValuePair<string, object?>("error.type", exception.GetType().FullName));
        }
    }

    internal static double ElapsedMilliseconds(long startTimestamp) =>
        Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
}
