using System.Security.Cryptography;
using Bit.BlazeDb.Serialization;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Wal;

/// <summary>
/// Owns the durability pipeline: encodes committed operations into WAL records buffered in
/// memory, flushes them to storage on a group-commit interval, checkpoints (snapshot + fresh
/// WAL) when the log grows, and recovers state on open.
///
/// File layout (all referenced from an atomic manifest):
///   manifest.blz          magic BLZM, version, generation, snapshot file, wal file, snapshot LSN, CRC
///   snapshot-{id}-N.blz   magic BLZS, version, LSN, per-table [key][row] dumps, CRC
///                         (version 1 snapshots hold rows only and are still readable)
///   wal-{id}-N.blz        sequence of records: magic BLZR, [len:4][lsn:8][crc:4][payload]
/// {id} is random and chosen once per database, so no two databases ever share a file name - not even one
/// deleted and created again on the same backend. Databases written before it existed name their files
/// snapshot-N.blz and wal-N.blz, and take an id at their next checkpoint.
/// Recovery = load snapshot, then replay WAL records in LSN order from snapshot LSN + 1; a record whose
/// LSN was already replayed is skipped. A torn tail - a record cut short or failing its checksum with
/// nothing intact after it - ends the log, as a crash mid-append leaves it. A record that is damaged or
/// out of sequence with intact records after it is not a crash but corruption, and is reported as such
/// rather than cut away along with the commits behind it. The checksum covers the length and the LSN as
/// well as the payload, so a corrupted header cannot make replay skip a commit or misread where a record ends.
/// </summary>
internal sealed class BlazeDbWalManager : IAsyncDisposable
{
    private const string ManifestFile = "manifest.blz";
    private static ReadOnlySpan<byte> ManifestMagic => "BLZM"u8;
    private static ReadOnlySpan<byte> SnapshotMagic => "BLZS"u8;
    private static ReadOnlySpan<byte> RecordMagic => "BLZR"u8;
    private const byte FormatVersion = 1;
    private const int RecordMagicSize = 4;
    private const int RecordChecksummedSize = sizeof(uint) + sizeof(ulong); // length + LSN
    private const int RecordHeaderSize = RecordMagicSize + RecordChecksummedSize + sizeof(uint);
    private const int MaxRecordSize = 256 * 1024 * 1024;

    /// <summary>
    /// How many times a read of a whole generation is retried when a checkpoint replaces it
    /// underneath. Each retry starts from the manifest the checkpoint left, so one is normally
    /// enough; the limit only stops an unbounded loop against a pathologically busy writer.
    /// </summary>
    private const int MaxGenerationRetries = 3;

    private readonly BlazeDbDatabase _db;
    private readonly IBlazeDbStorage _storage;
    private readonly BlazeDbDatabaseOptions _options;

    // Pending WAL bytes not yet flushed; guarded by _db.SyncRoot.
    private readonly BlazeDbBufferWriter _pending = new(16 * 1024);
    private readonly BlazeDbBufferWriter _valueScratch = new(1024);

    // The size of the last snapshot, so the next one is written into a buffer of about the right size
    // rather than one that doubles its way there - each doubling a copy of everything so far.
    private int _lastSnapshotSize = 64 * 1024;

    // Serializes all storage I/O (flushes vs checkpoints).
    private readonly SemaphoreSlim _ioLock = new(1, 1);

    // Serializes a whole flush attempt - the quota headroom check and the append it guards - so the
    // background loop and an explicit FlushAsync cannot interleave over the quota bookkeeping
    // below, which _ioLock cannot cover because the compaction it may trigger needs that lock.
    private readonly SemaphoreSlim _flushGate = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private Task _loop = Task.CompletedTask;
    private volatile Exception? _fault;

    private ulong _generation;
    private string _walFile = "";
    private string _snapshotFile = "";

    // The id in this database's file names; null for one whose files predate ids, until its next checkpoint.
    private string? _databaseId;
    private long _snapshotLsn;
    private long _lastLsn;
    private long _walSize;

    // Set when an append threw: it may have left a partial record on storage, which has to be cut
    // off before the retry appends behind it. See RepairWalAsync.
    private bool _walNeedsRepair;

    // Files of superseded generations whose deletion failed; retried at each checkpoint. Guarded by _ioLock.
    private readonly List<string> _staleFiles = [];

    private BlazeDbStorageQuota? _lastQuota;
    private bool _quotaPressureReported;
    private long _bytesSinceQuotaCheck;

    private BlazeDbWalManager(BlazeDbDatabase db, BlazeDbDatabaseOptions options)
    {
        _db = db;
        _storage = options.Storage!;
        _options = options;
    }

    public static async ValueTask<BlazeDbWalManager> OpenAsync(BlazeDbDatabase db, BlazeDbDatabaseOptions options, CancellationToken ct)
    {
        var manager = new BlazeDbWalManager(db, options);
        await manager.RecoverAsync(ct).ConfigureAwait(false);
        if (!options.ReadOnly)
        {
            manager._loop = manager.RunFlusherLoopAsync();
        }
        return manager;
    }

    /// <summary>
    /// Rebuilds in-memory state from what is currently on storage. Used by replica tabs to catch
    /// up with the writer; recovery is re-run from scratch rather than diffed, which keeps the
    /// path identical to opening the database and so avoids a second, rarely exercised code path.
    /// <para>
    /// The tables are not touched until the whole generation has been read and checksummed, so a
    /// reload that cannot get at the newer state leaves the replica serving the older one rather
    /// than emptying it, and no reader can catch the tables mid-rebuild.
    /// </para>
    /// </summary>
    public async ValueTask ReloadAsync(CancellationToken ct = default)
    {
        await _ioLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await RecoverAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    // ---- Commit path (called under _db.SyncRoot) ----

    public void AppendCommit(IReadOnlyList<IBlazeDbTxnOp> ops)
    {
        var lsn = _lastLsn + 1;

        // The record is encoded straight into the buffer behind a header whose length and checksum
        // are filled in once the payload is there - one copy of each row rather than three. A failure
        // part-way through encoding (a row that cannot be serialized, or a transaction too large to
        // grow the buffer for) cuts the buffer back to where the record began, and the LSN is only
        // taken once the record is whole, so the log never holds a header without its payload.
        var start = _pending.Length;
        try
        {
            _pending.WriteRaw(RecordMagic);
            var header = _pending.Length;
            _pending.WriteFixed32(0);
            _pending.WriteFixed64((ulong)lsn);
            _pending.WriteFixed32(0);
            var payloadStart = _pending.Length;
            _pending.WriteVarUInt((ulong)ops.Count);
            foreach (var op in ops)
            {
                op.EncodeRedo(_pending, _valueScratch);
            }
            var payloadLength = _pending.Length - payloadStart;
            if (payloadLength > MaxRecordSize)
            {
                throw new BlazeDbException(
                    $"The commit encodes to {payloadLength} bytes, more than a log record may hold ({MaxRecordSize}). " +
                    "Split the transaction into smaller ones.");
            }

            _pending.PatchFixed32(header, (uint)payloadLength);
            var written = _pending.WrittenSpan;
            _pending.PatchFixed32(header + RecordChecksummedSize, BlazeDbCrc32.Compute(
                written.Slice(header, RecordChecksummedSize),
                written.Slice(payloadStart, payloadLength)));
        }
        catch
        {
            _pending.Truncate(start);
            throw;
        }
        _lastLsn = lsn;
    }

    // ---- Flush / checkpoint ----

    public async ValueTask FlushAsync(CancellationToken ct = default)
    {
        await _flushGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await EnsureQuotaHeadroomAsync(ct).ConfigureAwait(false);
            // Cleared only when something actually reached storage, as in the loop: a flush with
            // nothing buffered proves nothing and must not hide a checkpoint that keeps failing.
            if (await FlushPendingAsync(ct).ConfigureAwait(false))
            {
                _fault = null;
            }
        }
        finally
        {
            _flushGate.Release();
        }
    }

    /// <summary>
    /// The error the most recent background flush or checkpoint failed with, or null once one has
    /// since succeeded. Lets an application notice that durability is lagging without waiting for
    /// an explicit flush to hit the same problem.
    /// </summary>
    public Exception? LastBackgroundError => _fault;

    /// <summary>Current storage usage, or null when the backend cannot report it.</summary>
    public async ValueTask<BlazeDbStorageQuota?> GetQuotaAsync(CancellationToken ct = default)
    {
        if (_storage is not IBlazeDbQuotaAwareStorage quotaAware)
        {
            return null;
        }
        var quota = await quotaAware.GetQuotaAsync(ct).ConfigureAwait(false);
        _lastQuota = quota;
        _bytesSinceQuotaCheck = 0;
        return quota;
    }

    /// <summary>
    /// Refuses to flush when the write would eat into the configured reserve, compacting first in
    /// case that frees enough. Called before <see cref="FlushPendingAsync"/> rather than inside it
    /// because a checkpoint needs the same I/O lock.
    /// </summary>
    private async ValueTask EnsureQuotaHeadroomAsync(CancellationToken ct)
    {
        if (_storage is not IBlazeDbQuotaAwareStorage quotaAware)
        {
            return;
        }

        long pendingBytes;
        lock (_db.SyncRoot)
        {
            pendingBytes = _pending.Length;
        }
        var neededBytes = SpaceNeededFor(quotaAware, pendingBytes);
        if (pendingBytes == 0 || await HasRoomForAsync(quotaAware, neededBytes, pendingBytes, ct).ConfigureAwait(false))
        {
            return;
        }

        // A checkpoint replaces the WAL with a compact snapshot and deletes the superseded files,
        // which is usually enough to get back under the limit.
        try
        {
            if (await CheckpointCoreAsync(ct).ConfigureAwait(false))
            {
                _lastQuota = null;
                lock (_db.SyncRoot)
                {
                    pendingBytes = _pending.Length;
                }
                neededBytes = SpaceNeededFor(quotaAware, pendingBytes);
                if (pendingBytes == 0 ||
                    await HasRoomForAsync(quotaAware, neededBytes, pendingBytes, ct).ConfigureAwait(false))
                {
                    return;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not BlazeDbException)
        {
            // Compaction itself ran out of room; fall through to the deliberate failure below. (An
            // engine-level error - a checkpoint that succeeded but whose announcement failed - is
            // not a quota problem and propagates as itself.)
        }

        var quota = _lastQuota ?? default;
        _lastQuota = null; // Force a fresh estimate next time; space may have been freed since.
        if (!_quotaPressureReported)
        {
            // Once per episode: the flusher retries every tick, and an application that is asked to
            // prompt the user does not want to be asked again 100 ms later. A successful flush ends
            // the episode, so a later refusal is reported afresh.
            _quotaPressureReported = true;
            _options.OnQuotaPressure?.Invoke(quota);
        }
        throw new BlazeDbStorageQuotaExceededException(quota, neededBytes);
    }

    /// <summary>
    /// Room a flush of <paramref name="pendingBytes"/> has to find. A backend that appends in place
    /// needs only those bytes; one that rewrites the file needs the log it already holds as well,
    /// because for a moment both copies exist.
    /// </summary>
    private long SpaceNeededFor(IBlazeDbQuotaAwareStorage storage, long pendingBytes) =>
        storage.AppendRewritesWholeFile ? _walSize + pendingBytes : pendingBytes;

    /// <summary>
    /// Estimates are comparatively expensive - in the browser it is an async trip through
    /// <c>navigator.storage.estimate()</c> - so the last one is carried forward by adding the bytes
    /// written since. That projection only ever overstates usage (deletes are ignored), so it is
    /// safe to trust until it says we are nearing the limit, which is when a fresh reading is taken.
    /// </summary>
    // neededBytes is the room the write has to find while it runs; growthBytes is how much bigger
    // the database ends up, which is what the projection carries forward.
    private async ValueTask<bool> HasRoomForAsync(
        IBlazeDbQuotaAwareStorage storage, long neededBytes, long growthBytes, CancellationToken ct)
    {
        if (_lastQuota is not { } quota || quota.QuotaBytes <= 0 || WouldCrowd(quota, neededBytes))
        {
            _lastQuota = await storage.GetQuotaAsync(ct).ConfigureAwait(false);
            _bytesSinceQuotaCheck = 0;
            if (_lastQuota is not { } refreshed || refreshed.QuotaBytes <= 0)
            {
                return true; // Backend cannot say; do not stand in the way of the write.
            }
            quota = refreshed;
        }

        if (WouldCrowd(quota, neededBytes))
        {
            return false;
        }
        _bytesSinceQuotaCheck += growthBytes;
        return true;
    }

    private bool WouldCrowd(BlazeDbStorageQuota quota, long bytes) =>
        quota.UsageBytes + _bytesSinceQuotaCheck + bytes + _options.QuotaReserveBytes > quota.QuotaBytes;

    public async ValueTask<bool> CheckpointAsync(CancellationToken ct = default)
    {
        if (_options.ReadOnly)
        {
            throw new InvalidOperationException("A read-only replica cannot checkpoint.");
        }
        if (_db.HasActiveTransaction)
        {
            throw new InvalidOperationException("Cannot checkpoint while a transaction is active.");
        }
        return await CheckpointCoreAsync(ct).ConfigureAwait(false);
    }

    private async Task RunFlusherLoopAsync()
    {
        using var timer = new PeriodicTimer(_options.FlushInterval);
        while (true)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
                {
                    return;
                }
                // Only the wait is cancellable. Once a flush has started it runs to completion:
                // interrupting an append mid-write and re-issuing it later could leave a torn
                // record in front of the retry, which the next open would report as corruption.
                await FlushAsync(CancellationToken.None).ConfigureAwait(false);
                if (Volatile.Read(ref _walSize) > _options.CheckpointWalSize)
                {
                    await CheckpointCoreAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Keep the loop alive and retry on the next tick; un-flushed bytes were put back.
                // The failure is visible through LastBackgroundError until a later flush or
                // checkpoint succeeds, and an explicit FlushAsync throws if it hits the same problem.
                _fault = ex;
            }
        }
    }

    private async ValueTask<bool> FlushPendingAsync(CancellationToken ct)
    {
        await _ioLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Before anything is drained, so a repair that fails leaves the buffer untouched and
            // the bytes are still there to be written once the log ends on a record boundary again.
            if (_walNeedsRepair)
            {
                await RepairWalAsync(ct).ConfigureAwait(false);
            }

            byte[]? chunk = null;
            lock (_db.SyncRoot)
            {
                if (_pending.Length > 0)
                {
                    chunk = _pending.ToArray();
                    _pending.Reset();
                }
            }
            if (chunk is null)
            {
                return false;
            }
            try
            {
                await _storage.AppendAsync(_walFile, chunk, ct).ConfigureAwait(false);
                _walSize += chunk.Length;
                _quotaPressureReported = false;
            }
            catch
            {
                // The append may have got part of the chunk onto storage before it failed, so the
                // log has to be trimmed back to whole records before these bytes are written again.
                _walNeedsRepair = true;
                // Put the un-flushed bytes back in front of anything committed meanwhile.
                lock (_db.SyncRoot)
                {
                    var newer = _pending.ToArray();
                    _pending.Reset();
                    _pending.WriteRaw(chunk);
                    _pending.WriteRaw(newer);
                }
                throw;
            }
            return true;
        }
        finally
        {
            _ioLock.Release();
        }
    }

    /// <summary>
    /// Cuts a partially written record off the end of the log.
    ///
    /// An append that throws can still have put bytes on storage - a disk or a quota that ran out
    /// part-way through the write - while the retry appends the whole record again. Left alone the
    /// log would hold a fragment in front of that retry, and replay stops at the first fragment, so
    /// the retry and every commit after it would be dropped on the next open. Reading the log back
    /// to find where the records stop is only ever paid after a failure.
    /// </summary>
    private async ValueTask RepairWalAsync(CancellationToken ct)
    {
        var raw = await _storage.ReadAsync(_walFile, ct).ConfigureAwait(false);
        if (raw is not null)
        {
            var validLength = ScanLog(raw, _walFile, _snapshotLsn).ValidLength;
            if (validLength != raw.Length)
            {
                await _storage.WriteAtomicAsync(_walFile, raw.AsMemory(0, validLength), ct).ConfigureAwait(false);
            }
            _walSize = validLength;
        }
        // Cleared only once the log is known to end on a record boundary: a repair that failed has
        // to run again before the next append, or that append lands behind the fragment after all.
        _walNeedsRepair = false;
    }

    /// <summary>
    /// Removes superseded generations' files, keeping the names of any that could not be removed for
    /// the next attempt. Never throws: what is left behind is unreferenced and only costs space.
    /// Called under <see cref="_ioLock"/>.
    /// </summary>
    private async ValueTask DeleteStaleFilesAsync()
    {
        for (var i = _staleFiles.Count - 1; i >= 0; i--)
        {
            try
            {
                await _storage.DeleteAsync(_staleFiles[i], CancellationToken.None).ConfigureAwait(false);
                _staleFiles.RemoveAt(i);
            }
            catch
            {
                // Kept for the next checkpoint.
            }
        }
    }

    /// <summary>Best-effort cleanup of a half-written generation; never masks the original error.</summary>
    private async ValueTask TryDeleteAsync(string name)
    {
        try
        {
            await _storage.DeleteAsync(name, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // The file is unreferenced by any manifest, so leaving it behind is harmless.
        }
    }

    private async ValueTask<bool> CheckpointCoreAsync(CancellationToken ct)
    {
        await _ioLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ReadOnlyMemory<byte> snapshotBytes;
            byte[] supersededPending;
            long snapshotLsn;
            ulong newGeneration;
            lock (_db.SyncRoot)
            {
                if (_db.HasActiveTransaction)
                {
                    return false;
                }
                if (_db.StorageUndoFailure is { } cause)
                {
                    // A snapshot of memory now could make a write durable that the log never recorded.
                    throw new BlazeDbException(
                        "Cannot checkpoint: an earlier rollback could not undo a write, so memory may hold one the log " +
                        "never recorded. Reopen the database from storage.", cause);
                }
                snapshotLsn = _lastLsn;
                newGeneration = _generation + 1;
                snapshotBytes = BuildSnapshot(_db.TableList, snapshotLsn, _lastSnapshotSize + _lastSnapshotSize / 8);
                _lastSnapshotSize = snapshotBytes.Length;
                // Everything buffered so far has LSN <= snapshotLsn and is superseded by the
                // snapshot - but only once that snapshot is actually durable, so keep a copy.
                supersededPending = _pending.ToArray();
                _pending.Reset();
            }

            // A database whose files predate ids takes one now, so from this generation on its files are
            // its own by name.
            var databaseId = _databaseId ?? NewDatabaseId();
            var newSnapshotFile = FileName("snapshot", databaseId, newGeneration);
            var newWalFile = FileName("wal", databaseId, newGeneration);

            try
            {
                await _storage.WriteAtomicAsync(newSnapshotFile, snapshotBytes, ct).ConfigureAwait(false);
                // The new generation's log has to start empty, and nothing guarantees that no file
                // by its name is left over - from a cleanup that failed, or from a generation that was
                // rolled back by putting an older manifest in place. Appending behind one would make
                // the next open replay its records into this one.
                await _storage.DeleteAsync(newWalFile, ct).ConfigureAwait(false);
                await _storage.WriteAtomicAsync(
                    ManifestFile,
                    BuildManifest(newGeneration, newSnapshotFile, newWalFile, snapshotLsn),
                    ct).ConfigureAwait(false);
            }
            catch
            {
                // The manifest still points at the previous generation, so those records are the
                // only record of these commits: put them back ahead of anything committed since.
                lock (_db.SyncRoot)
                {
                    var newer = _pending.ToArray();
                    _pending.Reset();
                    _pending.WriteRaw(supersededPending);
                    _pending.WriteRaw(newer);
                }
                await TryDeleteAsync(newSnapshotFile).ConfigureAwait(false);
                throw;
            }

            var oldWalFile = _walFile;
            var oldSnapshotFile = _snapshotFile;
            _databaseId = databaseId;
            _generation = newGeneration;
            _snapshotFile = newSnapshotFile;
            _walFile = newWalFile;
            _snapshotLsn = snapshotLsn;
            _walSize = 0;
            _walNeedsRepair = false; // The new generation's log is a fresh file with nothing to trim.
            _quotaPressureReported = false; // Compaction freed space; the next refusal is a new episode.

            // The checkpoint is in place - the manifest names the new generation - so removing the
            // previous one is cleanup, and a failure there must not read as a failed checkpoint: not
            // to the flusher, which would report it as lagging durability, nor to the quota path,
            // which would call it "out of space", nor to replicas, which would never hear of the new
            // generation. The files are unreferenced, and no later generation can mistake them for
            // its own (see above), so whatever cannot be removed now is retried at the next checkpoint.
            _staleFiles.Add(oldWalFile);
            if (oldSnapshotFile.Length > 0)
            {
                _staleFiles.Add(oldSnapshotFile);
            }
            await DeleteStaleFilesAsync().ConfigureAwait(false);

            // Announced only once the new generation is durable, so a replica that reloads on this
            // signal cannot land on a half-published generation. The checkpoint itself has
            // succeeded by now, so a failing announcement is reported as its own error rather than
            // as a failed checkpoint - a caller (or the flusher) must not redo a checkpoint that is
            // already in place.
            try
            {
                _options.OnCheckpoint?.Invoke(newGeneration);
            }
            catch (Exception ex)
            {
                throw new BlazeDbException(
                    $"Checkpoint {newGeneration} is durable, but the OnCheckpoint callback failed.", ex);
            }
            // A completed checkpoint means the log did reach storage, so a standing failure is
            // over - whoever asked for the checkpoint. If durability starts lagging again, the
            // flusher records it afresh on its next tick.
            _fault = null;
            return true;
        }
        finally
        {
            _ioLock.Release();
        }
    }

    // ---- Recovery ----

    /// <summary>
    /// Removes a database from a backend: everything it holds when it can list its files, otherwise
    /// the manifest together with the snapshot and log that manifest names.
    ///
    /// Deleting only the manifest is not enough: the rows the caller asked to be rid of would still
    /// be sitting in the files it named. A database created on the backend afterwards names its files
    /// with an id of its own, so it never reads them - but they are still there to be read.
    /// </summary>
    internal static async ValueTask DeleteAsync(IBlazeDbStorage storage, CancellationToken ct)
    {
        // A wrapper - encryption, say - can only enumerate when what it wraps can, so a backend that
        // says it lists its files may still turn out not to; fall through to the manifest then.
        if (storage is IBlazeDbEnumerableStorage enumerable)
        {
            IReadOnlyCollection<string>? names = null;
            try
            {
                names = await enumerable.ListAsync(ct).ConfigureAwait(false);
            }
            catch (NotSupportedException)
            {
            }
            if (names is not null)
            {
                foreach (var name in names)
                {
                    await storage.DeleteAsync(name, ct).ConfigureAwait(false);
                }
                return;
            }
        }

        var manifestBytes = await storage.ReadAsync(ManifestFile, ct).ConfigureAwait(false);
        if (manifestBytes is not null && TryParseManifest(manifestBytes, out var info))
        {
            if (info.SnapshotFile.Length > 0)
            {
                await storage.DeleteAsync(info.SnapshotFile, ct).ConfigureAwait(false);
            }
            await storage.DeleteAsync(info.WalFile, ct).ConfigureAwait(false);
        }
        // A database from before file names carried an id could hold a first-generation log no manifest
        // named yet, and a manifest too corrupt to read names nothing at all, so that generation is
        // cleared by its fixed names as well. Files named with an id cannot be found without a listing
        // or a manifest; whatever is left of them belongs to no database that can ever be opened.
        await storage.DeleteAsync("snapshot-1.blz", ct).ConfigureAwait(false);
        await storage.DeleteAsync("wal-1.blz", ct).ConfigureAwait(false);
        // Last, so a failure part-way through cannot leave a manifest pointing at files that are gone.
        await storage.DeleteAsync(ManifestFile, ct).ConfigureAwait(false);
    }

    /// <summary>What a manifest names: the generation and the files that make it up.</summary>
    private readonly record struct ManifestInfo(ulong Generation, string SnapshotFile, string WalFile, long SnapshotLsn);

    /// <summary>A generation read off storage, before any of it has been decoded into the tables.</summary>
    private readonly record struct LoadedGeneration(ManifestInfo Info, byte[]? SnapshotBytes, byte[]? WalBytes, ScannedLog Log);

    /// <summary>
    /// A log whose records have been checked: the length of its intact prefix, the LSN that prefix ends
    /// at, and where in it the payload of each commit to replay lies, in order.
    /// </summary>
    private readonly record struct ScannedLog(int ValidLength, long LastLsn, List<Range> Commits);

    private async ValueTask RecoverAsync(CancellationToken ct)
    {
        var manifestBytes = await _storage.ReadAsync(ManifestFile, ct).ConfigureAwait(false);
        if (manifestBytes is null)
        {
            // No manifest means no database, whatever else the backend holds. The new one's files are
            // named with an id nothing on the backend can already carry, so it starts from an empty log
            // even beside the files of one that was deleted only in part.
            var walFile = FileName("wal", NewDatabaseId(), 1);
            ApplyGeneration(new LoadedGeneration(new ManifestInfo(1, "", walFile, 0), null, null, new ScannedLog(0, 0, [])));
            if (!_options.ReadOnly)
            {
                await _storage.WriteAtomicAsync(
                    ManifestFile, BuildManifest(_generation, _snapshotFile, _walFile, _snapshotLsn), ct).ConfigureAwait(false);
            }
            return;
        }

        var loaded = await LoadGenerationAsync(manifestBytes, ct).ConfigureAwait(false);
        ApplyGeneration(loaded);

        if (loaded.WalBytes is not null && loaded.Log.ValidLength < loaded.WalBytes.Length && !_options.ReadOnly)
        {
            // Torn tail detected: truncate so future appends continue from a clean point.
            // A replica leaves it alone - repair is the writer's job, and the tail may simply
            // be a commit the writer is in the middle of appending.
            await _storage.WriteAtomicAsync(_walFile, loaded.WalBytes.AsMemory(0, loaded.Log.ValidLength), ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads a whole generation - manifest, snapshot, log - into memory and checks every checksum,
    /// without decoding any of it into the tables.
    /// <para>
    /// The manifest is read again at the end because a writer's checkpoint deletes the files the
    /// first read named as soon as the new generation is durable: a reader that started before it
    /// can otherwise find the log already gone and mistake that for a log with nothing in it,
    /// silently losing every commit the snapshot it did read does not cover. When the generation has
    /// moved on underneath, the read simply starts again from the manifest the checkpoint left.
    /// </para>
    /// </summary>
    private async ValueTask<LoadedGeneration> LoadGenerationAsync(byte[] manifestBytes, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var info = ParseManifest(manifestBytes);

            var snapshotBytes = info.SnapshotFile.Length > 0
                ? await _storage.ReadAsync(info.SnapshotFile, ct).ConfigureAwait(false)
                : null;
            // A missing log is normal: a generation has no log file until its first append.
            var walBytes = await _storage.ReadAsync(info.WalFile, ct).ConfigureAwait(false);

            var current = await _storage.ReadAsync(ManifestFile, ct).ConfigureAwait(false)
                ?? throw new BlazeDbCorruptDatabaseException(
                    "The manifest disappeared while the database was being read.");
            if (!current.AsSpan().SequenceEqual(manifestBytes))
            {
                if (attempt >= MaxGenerationRetries)
                {
                    throw new BlazeDbCorruptDatabaseException(
                        $"The database was checkpointed {attempt + 1} times while it was being read, so no " +
                        "single generation could be read whole. Retry once the writer settles.");
                }
                manifestBytes = current;
                continue;
            }

            if (info.SnapshotFile.Length > 0 && snapshotBytes is null)
            {
                throw new BlazeDbCorruptDatabaseException($"Manifest references missing snapshot '{info.SnapshotFile}'.");
            }
            if (snapshotBytes is not null)
            {
                VerifySnapshot(snapshotBytes, info.SnapshotFile);
            }
            var log = walBytes is null
                ? new ScannedLog(0, info.SnapshotLsn, [])
                : ScanLog(walBytes, info.WalFile, info.SnapshotLsn);
            return new LoadedGeneration(info, snapshotBytes, walBytes, log);
        }
    }

    /// <summary>
    /// Installs a generation that has already been read and checksummed. Clearing the tables, decoding
    /// the snapshot and replaying the log all happen under the lock with no await between them, so a
    /// reload cannot be observed halfway - and, because everything was read and checked first, cannot
    /// leave the tables empty because storage failed or the log turned out to be damaged.
    /// </summary>
    private void ApplyGeneration(LoadedGeneration loaded)
    {
        lock (_db.SyncRoot)
        {
            _db.ResetTables();
            _pending.Reset();
            _databaseId = DatabaseIdOf(loaded.Info.WalFile);
            _generation = loaded.Info.Generation;
            _snapshotFile = loaded.Info.SnapshotFile;
            _walFile = loaded.Info.WalFile;
            _snapshotLsn = loaded.Info.SnapshotLsn;
            _lastLsn = loaded.Info.SnapshotLsn;
            _walSize = 0;
            _walNeedsRepair = false;

            if (loaded.SnapshotBytes is not null)
            {
                try
                {
                    LoadSnapshot(loaded.SnapshotBytes);
                }
                catch (Exception ex) when (IsDecodeFailure(ex))
                {
                    throw new BlazeDbCorruptDatabaseException(
                        $"Snapshot '{_snapshotFile}' passed its checksum but could not be decoded; the row " +
                        "format may not match the registered table descriptors.", ex);
                }
            }

            if (loaded.WalBytes is null)
            {
                return;
            }
            try
            {
                foreach (var commit in loaded.Log.Commits)
                {
                    ReplayCommit(loaded.WalBytes.AsSpan(commit));
                }
            }
            catch (Exception ex) when (IsDecodeFailure(ex))
            {
                throw new BlazeDbCorruptDatabaseException(
                    $"A record in '{_walFile}' passed its checksum but could not be decoded; the row " +
                    "format may not match the registered table descriptors.", ex);
            }
            _lastLsn = loaded.Log.LastLsn;
            _walSize = loaded.Log.ValidLength;
        }
    }

    /// <summary>
    /// Checks a log's records and finds each commit past the snapshot's LSN to replay, once and in LSN
    /// order.
    ///
    /// The log ends at its torn tail: the first record that is cut short or fails its checksum, with
    /// nothing intact after it. That is what a crash part-way through an append leaves, and what comes
    /// after it was never acknowledged as durable. A fragment a failed append left behind is cut off by
    /// <see cref="RepairWalAsync"/> before the retry is written, so a live log never holds one in front
    /// of later records.
    ///
    /// Damage anywhere else is corruption, and is reported rather than treated as the end of the log:
    /// ending there would replay a prefix - a state the database was in once - and the writer would then
    /// truncate the log to it, destroying every intact commit behind the damage for good. So a broken
    /// record with an intact one after it, and an intact record whose LSN skips past the next one
    /// expected, both throw, leaving the files untouched. The gap matters for an encrypted log in
    /// particular: its frames are authenticated one at a time, so a frame dropped from the middle leaves
    /// every frame around it intact.
    ///
    /// A record whose LSN was already seen is skipped rather than replayed. An append that failed
    /// after some of its records reached storage is retried whole, so those records appear twice;
    /// applying the second copy would undo whatever the commits between the two copies changed.
    /// </summary>
    private static ScannedLog ScanLog(byte[] walBytes, string walFile, long snapshotLsn)
    {
        var commits = new List<Range>();
        var offset = 0;
        var lastLsn = snapshotLsn;
        while (TryReadRecord(walBytes, offset, out var lsn, out var payload))
        {
            if (lsn > lastLsn + 1)
            {
                throw new BlazeDbCorruptDatabaseException(
                    $"'{walFile}' is damaged: commit {lsn} follows commit {lastLsn}, so the commits between them are " +
                    "missing from the log. The file is left as it is.");
            }
            if (lsn == lastLsn + 1)
            {
                commits.Add(payload);
                lastLsn = lsn;
            }
            offset = payload.End.Value;
        }

        if (offset < walBytes.Length && HasIntactRecordAfter(walBytes, offset))
        {
            throw new BlazeDbCorruptDatabaseException(
                $"'{walFile}' is damaged at byte {offset}: the record there is broken, but intact records follow it, " +
                "which a crash part-way through an append does not leave. The file is left as it is, so the commits " +
                "after the damage are not lost.");
        }
        return new ScannedLog(offset, lastLsn, commits);
    }

    /// <summary>
    /// Reads the record at <paramref name="offset"/>, giving its LSN and where its payload lies, or returns
    /// false when no whole record with a matching checksum starts there.
    /// </summary>
    private static bool TryReadRecord(ReadOnlySpan<byte> walBytes, int offset, out long lsn, out Range payload)
    {
        lsn = 0;
        payload = default;
        if (walBytes.Length - offset < RecordHeaderSize)
        {
            return false;
        }

        var record = walBytes.Slice(offset);
        if (!record.Slice(0, RecordMagicSize).SequenceEqual(RecordMagic))
        {
            return false;
        }

        var header = new BlazeDbBufferReader(record.Slice(RecordMagicSize, RecordHeaderSize - RecordMagicSize));
        var payloadLength = (int)header.ReadFixed32();
        var recordLsn = (long)header.ReadFixed64();
        var expectedCrc = header.ReadFixed32();

        if (payloadLength < 0 || payloadLength > MaxRecordSize ||
            walBytes.Length - offset - RecordHeaderSize < payloadLength)
        {
            return false;
        }

        if (BlazeDbCrc32.Compute(record.Slice(RecordMagicSize, RecordChecksummedSize), record.Slice(RecordHeaderSize, payloadLength)) != expectedCrc)
        {
            return false;
        }

        lsn = recordLsn;
        payload = new Range(offset + RecordHeaderSize, offset + RecordHeaderSize + payloadLength);
        return true;
    }

    /// <summary>Whether a whole record with a matching checksum starts anywhere past the broken one at <paramref name="broken"/>.</summary>
    private static bool HasIntactRecordAfter(ReadOnlySpan<byte> walBytes, int broken)
    {
        for (var at = broken + 1; ; at++)
        {
            var found = walBytes.Slice(at).IndexOf(RecordMagic);
            if (found < 0)
            {
                return false;
            }
            at += found;
            if (TryReadRecord(walBytes, at, out _, out _))
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Decoding errors that a checksum cannot catch: a well-formed file whose contents do not match
    /// the descriptors this database was opened with. Readers report malformed bytes as
    /// <see cref="InvalidDataException"/>; the checked count casts in the loaders overflow instead.
    /// </summary>
    private static bool IsDecodeFailure(Exception ex) => ex is InvalidDataException or OverflowException;

    private void ReplayCommit(ReadOnlySpan<byte> payload)
    {
        var reader = new BlazeDbBufferReader(payload);
        var count = checked((int)reader.ReadVarUInt());
        for (var i = 0; i < count; i++)
        {
            var opType = reader.ReadByte();
            var tableName = reader.ReadString();
            var table = _db.GetTableByName(tableName);
            var keyBytes = reader.ReadBytes();
            switch (opType)
            {
                case BlazeDbWalOp.Set:
                    var rowBytes = reader.ReadBytes();
                    table.ReplaySet(keyBytes, rowBytes);
                    break;
                case BlazeDbWalOp.Delete:
                    table.ReplayDelete(keyBytes);
                    break;
                default:
                    throw new BlazeDbCorruptDatabaseException($"Unknown WAL op code {opType}.");
            }
        }
    }

    // ---- Snapshot / manifest encoding ----

    /// <summary>
    /// Serializes <paramref name="tables"/> into a snapshot, for a checkpoint or an export. Called under the
    /// database's lock. <paramref name="sizeHint"/> sizes the buffer - a checkpoint passes a little over the
    /// last snapshot's size, since a database mostly grows between checkpoints - and the written bytes are
    /// returned as they are, without a copy into an exact-size array: the buffer belongs to this one
    /// snapshot and is dropped once it has been written out.
    /// <para>
    /// The engine's own table is left out while it is empty, so a database that never used it writes the
    /// same snapshot an earlier version of the engine would, and opens there without an unregistered table.
    /// </para>
    /// </summary>
    internal static ReadOnlyMemory<byte> BuildSnapshot(IReadOnlyList<IBlazeDbTableInternal> tables, long lsn, int sizeHint)
    {
        var writer = new BlazeDbBufferWriter(sizeHint);
        var scratch = new BlazeDbBufferWriter(1024);
        writer.WriteRaw(SnapshotMagic);
        writer.WriteByte(BlazeDbSnapshotFormat.Current);
        writer.WriteVarUInt((ulong)lsn);
        var written = tables.Where(table => table.Count > 0 || table.Name != BlazeDbMetaTable.Name).ToList();
        writer.WriteVarUInt((ulong)written.Count);
        foreach (var table in written)
        {
            writer.WriteString(table.Name);
            table.WriteSnapshot(writer, scratch);
        }
        writer.WriteFixed32(BlazeDbCrc32.Compute(writer.WrittenSpan));
        return writer.WrittenMemory;
    }

    /// <summary>
    /// Makes <paramref name="snapshot"/> - a file <see cref="BlazeDbDatabase.ExportAsync"/> wrote - the
    /// first generation of a new database on <paramref name="storage"/>. The snapshot is checked first, and a
    /// backend that already holds a database is refused, so an import can neither land corrupt data nor
    /// overwrite a database. The manifest is written last: until it is, the backend still holds no database.
    /// </summary>
    internal static async ValueTask ImportAsync(IBlazeDbStorage storage, ReadOnlyMemory<byte> snapshot, CancellationToken ct)
    {
        // Verified where it lies and written out as it is: a whole database's worth of bytes is not copied
        // again here, which on WebAssembly can be the difference between fitting in the tab and not.
        VerifySnapshot(snapshot.Span, "import");
        var reader = new BlazeDbBufferReader(snapshot.Span.Slice(SnapshotMagic.Length + 1));
        var lsn = (long)reader.ReadVarUInt();

        if (await storage.ReadAsync(ManifestFile, ct).ConfigureAwait(false) is not null)
        {
            throw new InvalidOperationException(
                "The destination already holds a database. Delete it first with BlazeDbDatabase.DeleteAsync, or import somewhere else.");
        }
        // A database of its own, under an id no file a partly deleted one left behind can carry, so none
        // of them is replayed on top of the imported rows.
        var databaseId = NewDatabaseId();
        var snapshotFile = FileName("snapshot", databaseId, 1);
        var walFile = FileName("wal", databaseId, 1);
        await storage.WriteAtomicAsync(snapshotFile, snapshot, ct).ConfigureAwait(false);
        await storage.WriteAtomicAsync(ManifestFile, BuildManifest(1, snapshotFile, walFile, lsn), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks a snapshot's header and checksum. Kept apart from decoding it so the tables are only
    /// cleared once the bytes that will replace their contents are known to be intact.
    /// </summary>
    private static void VerifySnapshot(ReadOnlySpan<byte> bytes, string name)
    {
        if (bytes.Length < SnapshotMagic.Length + 1 + 4 ||
            !bytes.Slice(0, 4).SequenceEqual(SnapshotMagic))
        {
            throw new BlazeDbCorruptDatabaseException($"Snapshot '{name}' has an invalid header.");
        }
        var body = bytes.Slice(0, bytes.Length - 4);
        var crcReader = new BlazeDbBufferReader(bytes.Slice(bytes.Length - 4));
        if (BlazeDbCrc32.Compute(body) != crcReader.ReadFixed32())
        {
            throw new BlazeDbCorruptDatabaseException($"Snapshot '{name}' failed checksum validation.");
        }
        var version = body.Slice(4)[0];
        if (version is < BlazeDbSnapshotFormat.RowsOnly or > BlazeDbSnapshotFormat.Current)
        {
            throw new BlazeDbCorruptDatabaseException($"Unsupported snapshot format version {version}.");
        }
    }

    private void LoadSnapshot(byte[] bytes)
    {
        var reader = new BlazeDbBufferReader(bytes.AsSpan(4, bytes.Length - 4 - 4));
        var version = reader.ReadByte(); // already range-checked by VerifySnapshot
        reader.ReadVarUInt(); // snapshot LSN; authoritative value comes from the manifest
        var tableCount = checked((int)reader.ReadVarUInt());
        for (var i = 0; i < tableCount; i++)
        {
            var tableName = reader.ReadString();
            _db.GetTableByName(tableName).LoadSnapshot(ref reader, version);
        }
    }

    /// <summary>
    /// The name of one generation's snapshot or log: <c>{kind}-{id}-{generation}.blz</c>, or
    /// <c>{kind}-{generation}.blz</c> for a database whose files predate ids.
    /// </summary>
    private static string FileName(string kind, string? databaseId, ulong generation) =>
        databaseId is null ? $"{kind}-{generation}.blz" : $"{kind}-{databaseId}-{generation}.blz";

    /// <summary>
    /// A new database's id. It is in every file name, and an encrypted backend seals each frame with its
    /// file's name as associated data, so a frame another database sealed under the same key - one deleted
    /// and created again on the same backend, say - does not open as one of this database's.
    /// </summary>
    private static string NewDatabaseId() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

    /// <summary>The id in a log's name, or null for a name from before ids.</summary>
    private static string? DatabaseIdOf(string walFile)
    {
        // wal-{id}-{generation}.blz has two dashes; wal-{generation}.blz, from before ids, has one.
        var first = walFile.IndexOf('-');
        var last = walFile.LastIndexOf('-');
        return first >= 0 && last > first ? walFile[(first + 1)..last] : null;
    }

    private static byte[] BuildManifest(ulong generation, string snapshotFile, string walFile, long snapshotLsn)
    {
        var writer = new BlazeDbBufferWriter(128);
        writer.WriteRaw(ManifestMagic);
        writer.WriteByte(FormatVersion);
        writer.WriteVarUInt(generation);
        writer.WriteString(snapshotFile);
        writer.WriteString(walFile);
        writer.WriteVarUInt((ulong)snapshotLsn);
        writer.WriteFixed32(BlazeDbCrc32.Compute(writer.WrittenSpan));
        return writer.ToArray();
    }

    /// <summary>Parses a manifest, reporting a corrupt one as false rather than throwing.</summary>
    private static bool TryParseManifest(byte[] bytes, out ManifestInfo info)
    {
        try
        {
            info = ParseManifest(bytes);
            return true;
        }
        catch (Exception ex) when (ex is BlazeDbCorruptDatabaseException or InvalidDataException)
        {
            info = default;
            return false;
        }
    }

    private static ManifestInfo ParseManifest(byte[] bytes)
    {
        if (bytes.Length < ManifestMagic.Length + 1 + 4 ||
            !bytes.AsSpan(0, 4).SequenceEqual(ManifestMagic))
        {
            throw new BlazeDbCorruptDatabaseException("Manifest file has an invalid header.");
        }
        var body = bytes.AsSpan(0, bytes.Length - 4);
        var crcReader = new BlazeDbBufferReader(bytes.AsSpan(bytes.Length - 4));
        if (BlazeDbCrc32.Compute(body) != crcReader.ReadFixed32())
        {
            throw new BlazeDbCorruptDatabaseException("Manifest file failed checksum validation.");
        }

        var reader = new BlazeDbBufferReader(body.Slice(4));
        var version = reader.ReadByte();
        if (version != FormatVersion)
        {
            throw new BlazeDbCorruptDatabaseException($"Unsupported manifest format version {version}.");
        }
        var generation = reader.ReadVarUInt();
        var snapshotFile = reader.ReadString();
        var walFile = reader.ReadString();
        return new ManifestInfo(generation, snapshotFile, walFile, (long)reader.ReadVarUInt());
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try
        {
            await _loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        if (!_options.ReadOnly)
        {
            await FlushPendingAsync(CancellationToken.None).ConfigureAwait(false);
        }
        _cts.Dispose();
        _ioLock.Dispose();
        _flushGate.Dispose();
    }
}
