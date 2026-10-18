using Bit.BlazeDb.Serialization;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Wal;

/// <summary>
/// Owns the durability pipeline: encodes committed operations into WAL records buffered in
/// memory, flushes them to storage on a group-commit interval, checkpoints (snapshot + fresh
/// WAL) when the log grows, and recovers state on open.
///
/// File layout (all referenced from an atomic manifest):
///   manifest.blz     magic BLZM, version, generation, snapshot file, wal file, snapshot LSN, CRC
///   snapshot-N.blz   magic BLZS, version, LSN, per-table [key][row] dumps, CRC
///                    (version 1 snapshots hold rows only and are still readable)
///   wal-N.blz        sequence of records: magic BLZR, [len:4][lsn:8][crc:4][payload]
/// Recovery = load snapshot, then replay WAL records with LSN &gt; snapshot LSN, stopping at the
/// first torn/corrupt record. The checksum covers the length and the LSN as well as the payload,
/// so a corrupted header cannot make replay skip a commit or misread where a record ends.
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
    private readonly BlazeDbBufferWriter _recordScratch = new(4 * 1024);
    private readonly BlazeDbBufferWriter _payloadScratch = new(4 * 1024);
    private readonly BlazeDbBufferWriter _valueScratch = new(1024);

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

        _payloadScratch.Reset();
        _payloadScratch.WriteVarUInt((ulong)ops.Count);
        foreach (var op in ops)
        {
            op.EncodeRedo(_payloadScratch, _valueScratch);
        }
        var payload = _payloadScratch.WrittenSpan;

        // The record is assembled in full before it joins the buffer, and the LSN is only taken
        // once it has: a failure part-way through encoding - a transaction too large to grow the
        // buffer for - must not leave a header in the log ahead of a payload that never arrived.
        _recordScratch.Reset();
        _recordScratch.WriteRaw(RecordMagic);
        _recordScratch.WriteFixed32((uint)payload.Length);
        _recordScratch.WriteFixed64((ulong)lsn);
        _recordScratch.WriteFixed32(
            BlazeDbCrc32.Compute(_recordScratch.WrittenSpan.Slice(RecordMagicSize), payload));
        _recordScratch.WriteRaw(payload);

        _pending.WriteRaw(_recordScratch.WrittenSpan);
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
                // record in front of the retry, and replay stops at the first torn record.
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
            var validLength = ScanRecords(raw, replay: false);
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
            byte[] snapshotBytes;
            byte[] supersededPending;
            long snapshotLsn;
            ulong newGeneration;
            lock (_db.SyncRoot)
            {
                if (_db.HasActiveTransaction)
                {
                    return false;
                }
                snapshotLsn = _lastLsn;
                newGeneration = _generation + 1;
                snapshotBytes = BuildSnapshot(snapshotLsn);
                // Everything buffered so far has LSN <= snapshotLsn and is superseded by the
                // snapshot - but only once that snapshot is actually durable, so keep a copy.
                supersededPending = _pending.ToArray();
                _pending.Reset();
            }

            var newSnapshotFile = $"snapshot-{newGeneration}.blz";
            var newWalFile = $"wal-{newGeneration}.blz";

            try
            {
                await _storage.WriteAtomicAsync(newSnapshotFile, snapshotBytes, ct).ConfigureAwait(false);
                // The new generation's log has to start empty, and nothing guarantees that no file
                // by its name is left over - from a cleanup that failed, or from an earlier database
                // on the same backend that a delete could not completely remove. Appending behind
                // one would make the next open replay its records into this database.
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
    /// Deleting only the manifest is not enough and is worse than doing nothing: recovery reads a
    /// missing manifest as an empty database and writes a fresh one pointing at the first
    /// generation's log, without truncating the log that is still sitting there - so the next open
    /// replays the rows the caller asked to be rid of.
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
        // The first generation's log exists before any manifest names it, and a manifest too corrupt
        // to read names nothing at all, so that generation is cleared by name as well.
        await storage.DeleteAsync("snapshot-1.blz", ct).ConfigureAwait(false);
        await storage.DeleteAsync("wal-1.blz", ct).ConfigureAwait(false);
        // Last, so a failure part-way through cannot leave a manifest pointing at files that are gone.
        await storage.DeleteAsync(ManifestFile, ct).ConfigureAwait(false);
    }

    /// <summary>What a manifest names: the generation and the files that make it up.</summary>
    private readonly record struct ManifestInfo(ulong Generation, string SnapshotFile, string WalFile, long SnapshotLsn);

    /// <summary>A generation read off storage, before any of it has been decoded into the tables.</summary>
    private readonly record struct LoadedGeneration(ManifestInfo Info, byte[]? SnapshotBytes, byte[]? WalBytes);

    private async ValueTask RecoverAsync(CancellationToken ct)
    {
        var manifestBytes = await _storage.ReadAsync(ManifestFile, ct).ConfigureAwait(false);
        if (manifestBytes is null)
        {
            ApplyGeneration(new LoadedGeneration(new ManifestInfo(1, "", "wal-1.blz", 0), null, null));
            if (!_options.ReadOnly)
            {
                // No manifest means no database, whatever else the backend holds: a writer writes the
                // manifest before its first append, so a first-generation log already sitting here
                // belongs to one that was deleted only in part, and must not be appended to.
                await _storage.DeleteAsync(_walFile, ct).ConfigureAwait(false);
                await _storage.WriteAtomicAsync(
                    ManifestFile, BuildManifest(_generation, _snapshotFile, _walFile, _snapshotLsn), ct).ConfigureAwait(false);
            }
            return;
        }

        var loaded = await LoadGenerationAsync(manifestBytes, ct).ConfigureAwait(false);
        var validLength = ApplyGeneration(loaded);

        if (loaded.WalBytes is not null && validLength < loaded.WalBytes.Length && !_options.ReadOnly)
        {
            // Torn tail detected: truncate so future appends continue from a clean point.
            // A replica leaves it alone - repair is the writer's job, and the tail may simply
            // be a commit the writer is in the middle of appending.
            await _storage.WriteAtomicAsync(_walFile, loaded.WalBytes.AsMemory(0, validLength), ct).ConfigureAwait(false);
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
            return new LoadedGeneration(info, snapshotBytes, walBytes);
        }
    }

    /// <summary>
    /// Installs a generation that has already been read and checksummed, and returns the length of
    /// the log's valid prefix. Clearing the tables, decoding the snapshot and replaying the log all
    /// happen under the lock with no await between them, so a reload cannot be observed halfway -
    /// and, because everything was read first, cannot leave the tables empty because storage failed.
    /// </summary>
    private int ApplyGeneration(LoadedGeneration loaded)
    {
        lock (_db.SyncRoot)
        {
            _db.ResetTables();
            _pending.Reset();
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
                return 0;
            }
            int validLength;
            try
            {
                validLength = ScanRecords(loaded.WalBytes, replay: true);
            }
            catch (Exception ex) when (IsDecodeFailure(ex))
            {
                throw new BlazeDbCorruptDatabaseException(
                    $"A record in '{_walFile}' passed its checksum but could not be decoded; the row " +
                    "format may not match the registered table descriptors.", ex);
            }
            _walSize = validLength;
            return validLength;
        }
    }

    /// <summary>
    /// Walks the log's records and returns the length of the valid prefix, replaying every record
    /// past the snapshot's LSN when asked to.
    ///
    /// Scanning stops at the first record that is torn or fails its checksum. Everything after such
    /// a record was written later, so carrying on could apply a commit whose predecessor was lost -
    /// a state the database was never in. A fragment a failed append left behind is cut off by
    /// <see cref="RepairWalAsync"/> before the retry is written, so stopping here does not cost the
    /// commits that followed it.
    /// </summary>
    private int ScanRecords(byte[] walBytes, bool replay)
    {
        var offset = 0;
        var lastLsn = _lastLsn;
        while (walBytes.Length - offset >= RecordHeaderSize)
        {
            var record = walBytes.AsSpan(offset);
            if (!record.Slice(0, RecordMagicSize).SequenceEqual(RecordMagic))
            {
                break;
            }

            var header = new BlazeDbBufferReader(record.Slice(RecordMagicSize, RecordHeaderSize - RecordMagicSize));
            var payloadLength = (int)header.ReadFixed32();
            var lsn = (long)header.ReadFixed64();
            var expectedCrc = header.ReadFixed32();

            if (payloadLength < 0 || payloadLength > MaxRecordSize ||
                walBytes.Length - offset - RecordHeaderSize < payloadLength)
            {
                break;
            }

            var payload = record.Slice(RecordHeaderSize, payloadLength);
            if (BlazeDbCrc32.Compute(record.Slice(RecordMagicSize, RecordChecksummedSize), payload) != expectedCrc)
            {
                break;
            }

            if (replay && lsn > _snapshotLsn)
            {
                ReplayCommit(payload);
            }
            lastLsn = Math.Max(lastLsn, lsn);
            offset += RecordHeaderSize + payloadLength;
        }

        if (replay)
        {
            _lastLsn = lastLsn;
        }
        return offset;
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

    private byte[] BuildSnapshot(long lsn)
    {
        var writer = new BlazeDbBufferWriter(64 * 1024);
        writer.WriteRaw(SnapshotMagic);
        writer.WriteByte(BlazeDbSnapshotFormat.Current);
        writer.WriteVarUInt((ulong)lsn);
        var tables = _db.TableList;
        writer.WriteVarUInt((ulong)tables.Count);
        foreach (var table in tables)
        {
            writer.WriteString(table.Name);
            table.WriteSnapshot(writer, _valueScratch);
        }
        writer.WriteFixed32(BlazeDbCrc32.Compute(writer.WrittenSpan));
        return writer.ToArray();
    }

    /// <summary>
    /// Checks a snapshot's header and checksum. Kept apart from decoding it so the tables are only
    /// cleared once the bytes that will replace their contents are known to be intact.
    /// </summary>
    private static void VerifySnapshot(byte[] bytes, string name)
    {
        if (bytes.Length < SnapshotMagic.Length + 1 + 4 ||
            !bytes.AsSpan(0, 4).SequenceEqual(SnapshotMagic))
        {
            throw new BlazeDbCorruptDatabaseException($"Snapshot '{name}' has an invalid header.");
        }
        var body = bytes.AsSpan(0, bytes.Length - 4);
        var crcReader = new BlazeDbBufferReader(bytes.AsSpan(bytes.Length - 4));
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
