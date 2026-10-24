using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Bit.Bcrdt;

/// <summary>Damage a store found in a document's log, and what it did about it.</summary>
/// <param name="DocumentId">The document whose log was damaged.</param>
/// <param name="QuarantinePath">
/// Where an untouched copy of the damaged log was put before anything was removed from it, so nothing
/// that might still be recoverable by hand is lost.
/// </param>
/// <param name="RecordsKept">How many intact records the log holds once repaired.</param>
/// <param name="BytesDiscarded">How many bytes of the log could not be read as intact records.</param>
public sealed record BcrdtStoreCorruption(string DocumentId, string QuarantinePath, int RecordsKept, long BytesDiscarded);

/// <summary>
/// A store that appends updates to a length-prefixed, checksummed log file per document.
/// </summary>
/// <remarks>
/// <para>
/// Each record is a length, a CRC-32 and a payload. That framing is what makes the store survive a
/// crash: an append that was interrupted leaves a record whose length runs past the end of the file
/// or whose checksum does not match, with nothing intact after it. The first time a log is touched
/// after that, the damaged tail is cut off, so loading stops cleanly where the good records end and
/// later appends land after them rather than behind unreadable bytes. The cost is at most the update
/// that was being written, which the peer still holds and will resend on the next sync.
/// </para>
/// <para>
/// Damage with intact records after it is not a torn append - appends are serialized, so only the
/// last one can be torn - but corruption: a bad sector, an external program writing to the file. Then
/// nothing is thrown away blindly. The log is copied aside untouched, the store finds the next intact
/// record and carries on from there, the log is rewritten with every intact record it holds, and
/// <see cref="CorruptionDetected"/> says what happened. A record that is intact on disk but is not a
/// valid update is set aside the same way rather than making the document impossible to open.
/// </para>
/// <para>
/// Compaction reads, merges and replaces the log as one step that no append can interleave with. It
/// writes the merged log to a temporary file, forces it to disk, and only then renames it over the
/// old one, so a crash or power loss leaves either the old log or the new one, never a half-merged
/// or empty one. A merged document too large to be one record is left uncompacted rather than
/// written as a record the reader would refuse.
/// </para>
/// <para>
/// Every operation on a document is serialized with every other operation on the same document's
/// file - across every instance of this store in the process, so two instances pointed at the same
/// directory (one per Blazor Server circuit, say) cannot interleave an append with a compaction.
/// Different documents proceed independently. Two <i>processes</i> must not share a directory.
/// </para>
/// </remarks>
public sealed class BcrdtFileSystemDocumentStore : IBcrdtDocumentStore
{
    private const int HeaderSize = 8;
    private const int MaxRecordSize = 64 * 1024 * 1024;

    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    // Process-wide, so separate instances on one directory share them; an entry lives only while
    // somebody holds or waits for it, so probing arbitrary identifiers leaves nothing behind.
    private static readonly Dictionary<string, FileGate> Gates = new(PathComparer);

    private readonly string _root;
    private readonly bool _flushToDisk;
    private readonly HashSet<string> _validated = new(PathComparer);

    /// <summary>Creates a store rooted at a directory, creating it if necessary.</summary>
    /// <param name="rootDirectory">Where the logs are kept.</param>
    /// <param name="flushToDisk">
    /// Whether every append and compaction waits for the operating system to put the data on disk.
    /// On by default: without it an acknowledged write can still be lost to a power failure or an
    /// operating-system crash. Turn it off only for disposable data, such as tests.
    /// </param>
    public BcrdtFileSystemDocumentStore(string rootDirectory, bool flushToDisk = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _root = Path.GetFullPath(rootDirectory);
        _flushToDisk = flushToDisk;
        Directory.CreateDirectory(_root);
    }

    /// <summary>
    /// Raised when a document's log turns out to be damaged in a way an interrupted append cannot
    /// explain, after the damaged log was copied aside and repaired.
    /// </summary>
    /// <remarks>Raised on the thread doing the store operation that found it.</remarks>
    public event Action<BcrdtStoreCorruption>? CorruptionDetected;

    /// <inheritdoc/>
    public async ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default)
    {
        (_, string path) = PathFor(documentId);

        using FileGate.Lease lease = await FileGate.AcquireAsync(path, cancellationToken).ConfigureAwait(false);

        if (!File.Exists(path)) return default;

        List<ReadOnlyMemory<byte>> records = await ReadAndRepairAsync(documentId, path, cancellationToken).ConfigureAwait(false);
        (byte[]? merged, int count) = await MergeAsync(documentId, path, records, cancellationToken).ConfigureAwait(false);
        return merged is null ? default : new BcrdtStoredDocument(merged, count);
    }

    /// <inheritdoc/>
    public async ValueTask AppendUpdateAsync(
        string documentId,
        ReadOnlyMemory<byte> update,
        CancellationToken cancellationToken = default)
    {
        if (update.Length == 0) return;
        if (update.Length > MaxRecordSize)
        {
            throw new ArgumentException($"Update of {update.Length} bytes exceeds the {MaxRecordSize} byte record limit.");
        }

        (string name, string path) = PathFor(documentId);
        byte[] record = Frame(update.Span);

        using FileGate.Lease lease = await FileGate.AcquireAsync(path, cancellationToken).ConfigureAwait(false);

        // A torn record left by a crash must be removed before anything is appended after it,
        // or every later record would be stranded behind bytes the reader stops at.
        if (!IsValidated(path) && File.Exists(path))
        {
            await ReadAndRepairAsync(documentId, path, cancellationToken).ConfigureAwait(false);
        }
        MarkValidated(path);

        WriteSidecar(name, documentId);

        // Unbuffered, so nothing of a failed write lingers in the stream to be written again when
        // it is disposed; opened rather than appended to, so the file can be cut back on failure.
        await using var stream = new FileStream(
            path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, bufferSize: 0, useAsync: true);
        long start = stream.Seek(0, SeekOrigin.End);
        try
        {
            await stream.WriteAsync(record, cancellationToken).ConfigureAwait(false);
            if (_flushToDisk) stream.Flush(flushToDisk: true);
        }
        catch
        {
            // Part of the record may have reached the file. A retry appended after it would sit
            // behind bytes the reader stops at and be lost, so cut it off; and in case that fails
            // too, make the next append check the tail before writing.
            ForgetValidated(path);
            try
            {
                stream.SetLength(start);
            }
            catch (Exception)
            {
                // Best effort; the next append or load truncates the torn tail instead.
            }
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default)
    {
        (_, string path) = PathFor(documentId);

        using FileGate.Lease lease = await FileGate.AcquireAsync(path, cancellationToken).ConfigureAwait(false);

        if (!File.Exists(path)) return;

        List<ReadOnlyMemory<byte>> records = await ReadAndRepairAsync(documentId, path, cancellationToken).ConfigureAwait(false);
        if (records.Count <= 1) return;

        (byte[]? merged, int count) = await MergeAsync(documentId, path, records, cancellationToken).ConfigureAwait(false);

        // A record this large would be refused by the reader, turning the whole document into
        // nothing. Several smaller records are better than one unreadable one.
        if (merged is null || count <= 1 || merged.Length > MaxRecordSize) return;

        await ReplaceLogAsync(path, [merged], cancellationToken).ConfigureAwait(false);
        MarkValidated(path);
    }

    /// <inheritdoc/>
    public async ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        (string name, string path) = PathFor(documentId);

        using FileGate.Lease lease = await FileGate.AcquireAsync(path, cancellationToken).ConfigureAwait(false);

        if (File.Exists(path)) File.Delete(path);

        string sidecar = SidecarPath(name);
        if (File.Exists(sidecar)) File.Delete(sidecar);

        ForgetValidated(path);
    }

    /// <inheritdoc/>
    public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        var ids = new List<string>();
        foreach (string file in Directory.EnumerateFiles(_root, "*.bcl"))
        {
            string? mapped = ReadDocumentId(file);
            if (mapped is not null) ids.Add(mapped);
        }
        return new ValueTask<IReadOnlyList<string>>(ids);
    }

    /// <summary>
    /// Reads every intact record, repairing the log on disk if it held anything else.
    /// </summary>
    /// <remarks>
    /// A damaged tail - an interrupted append - is cut off. Damage with intact records after it is
    /// copied aside first and reported, and the log is rewritten with what is intact.
    /// </remarks>
    private async Task<List<ReadOnlyMemory<byte>>> ReadAndRepairAsync(string documentId, string path, CancellationToken cancellationToken)
    {
        byte[] contents = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        List<ReadOnlyMemory<byte>> records = ReadRecords(contents, out int intactEnd, out long skipped);

        if (skipped > 0)
        {
            await QuarantineAsync(documentId, path, records, skipped + (contents.Length - intactEnd), cancellationToken).ConfigureAwait(false);
            return records;
        }

        if (intactEnd < contents.Length) TruncateTail(path, intactEnd);
        MarkValidated(path);
        return records;
    }

    /// <summary>
    /// Merges the records into one update, setting aside any that is intact on disk but is not a
    /// readable update.
    /// </summary>
    /// <remarks>
    /// Such a record - written by something else, or by a newer build - would otherwise make every
    /// merge, and so every load, fail: the document could never be opened again.
    /// </remarks>
    /// <returns>The merged update, or <see langword="null"/> if nothing readable remains, and how many records it came from.</returns>
    private async Task<(byte[]? Merged, int Count)> MergeAsync(
        string documentId,
        string path,
        List<ReadOnlyMemory<byte>> records,
        CancellationToken cancellationToken)
    {
        if (records.Count == 0) return (null, 0);

        try
        {
            if (records.Count > 1) return (BcrdtUpdateOperations.Merge([.. records]), records.Count);
            if (IsDecodable(records[0])) return (records[0].ToArray(), 1);
        }
        catch (BcrdtFormatException)
        {
            // Some record is unreadable; find out which below.
        }

        List<ReadOnlyMemory<byte>> readable = [];
        long discarded = 0;
        foreach (ReadOnlyMemory<byte> record in records)
        {
            if (IsDecodable(record)) readable.Add(record);
            else discarded += HeaderSize + record.Length;
        }

        await QuarantineAsync(documentId, path, readable, discarded, cancellationToken).ConfigureAwait(false);

        return readable.Count switch
        {
            0 => (null, 0),
            1 => (readable[0].ToArray(), 1),
            _ => (BcrdtUpdateOperations.Merge([.. readable]), readable.Count),
        };
    }

    /// <summary>
    /// Copies a damaged log aside untouched, rewrites it with the records worth keeping, and reports it.
    /// </summary>
    private async Task QuarantineAsync(
        string documentId,
        string path,
        IReadOnlyList<ReadOnlyMemory<byte>> keep,
        long discarded,
        CancellationToken cancellationToken)
    {
        string quarantine = $"{path}.{DateTime.UtcNow:yyyyMMddHHmmssfff}.{Guid.NewGuid():N}.corrupt";
        File.Copy(path, quarantine, overwrite: false);
        await ReplaceLogAsync(path, keep, cancellationToken).ConfigureAwait(false);
        MarkValidated(path);

        CorruptionDetected?.Invoke(new BcrdtStoreCorruption(documentId, quarantine, keep.Count, discarded));
    }

    /// <summary>
    /// Walks the record framing, resynchronizing past damage to the next intact record.
    /// </summary>
    /// <param name="contents">The log's bytes.</param>
    /// <param name="intactEnd">Where the last intact record ends; anything after it is a torn tail.</param>
    /// <param name="skipped">How many bytes between intact records could not be read.</param>
    private static List<ReadOnlyMemory<byte>> ReadRecords(byte[] contents, out int intactEnd, out long skipped)
    {
        List<ReadOnlyMemory<byte>> records = [];
        int offset = 0;
        intactEnd = 0;
        skipped = 0;

        while (offset + HeaderSize <= contents.Length)
        {
            if (TryReadRecord(contents, offset, out ReadOnlyMemory<byte> payload))
            {
                records.Add(payload);
                offset += HeaderSize + payload.Length;
                intactEnd = offset;
                continue;
            }

            // Damage. If an intact record follows somewhere, this is corruption in the middle of the
            // log and everything from that record on is still good; if none does, it is a torn tail.
            int next = FindNextRecord(contents, offset + 1);
            if (next < 0) break;

            skipped += next - offset;
            offset = next;
        }

        return records;
    }

    private static bool TryReadRecord(byte[] contents, int offset, out ReadOnlyMemory<byte> payload)
    {
        payload = default;
        if (offset + HeaderSize > contents.Length) return false;

        int length = BinaryPrimitives.ReadInt32LittleEndian(contents.AsSpan(offset, 4));
        uint expected = BinaryPrimitives.ReadUInt32LittleEndian(contents.AsSpan(offset + 4, 4));
        if (length <= 0 || length > MaxRecordSize || length > contents.Length - offset - HeaderSize) return false;

        ReadOnlyMemory<byte> candidate = contents.AsMemory(offset + HeaderSize, length);
        if (BcrdtCrc32.Compute(candidate.Span) != expected) return false;

        payload = candidate;
        return true;
    }

    /// <summary>The offset of the next intact record at or after <paramref name="from"/>, or -1.</summary>
    private static int FindNextRecord(byte[] contents, int from)
    {
        for (int offset = from; offset + HeaderSize <= contents.Length; offset++)
        {
            if (TryReadRecord(contents, offset, out _)) return offset;
        }
        return -1;
    }

    private static bool IsDecodable(ReadOnlyMemory<byte> record)
    {
        try
        {
            var reader = new BcrdtBufferReader(record);
            BcrdtUpdateCodec.ReadClientBlocks(ref reader);
            BcrdtDeleteSet.Read(ref reader);
            return true;
        }
        catch (BcrdtFormatException)
        {
            return false;
        }
    }

    private static byte[] Frame(ReadOnlySpan<byte> payload)
    {
        byte[] record = new byte[HeaderSize + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(record.AsSpan(0, 4), payload.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(4, 4), BcrdtCrc32.Compute(payload));
        payload.CopyTo(record.AsSpan(HeaderSize));
        return record;
    }

    /// <summary>Replaces a log with the given records, atomically.</summary>
    private async Task ReplaceLogAsync(string path, IReadOnlyList<ReadOnlyMemory<byte>> records, CancellationToken cancellationToken)
    {
        string temporary = path + ".compacting";
        await using (var stream = new FileStream(
            temporary, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
        {
            foreach (ReadOnlyMemory<byte> record in records)
            {
                await stream.WriteAsync(Frame(record.Span), cancellationToken).ConfigureAwait(false);
            }
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            // The rename must not reach the disk before the data it names does.
            if (_flushToDisk) stream.Flush(flushToDisk: true);
        }

        File.Move(temporary, path, overwrite: true);
    }

    private void TruncateTail(string path, int validLength)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
        stream.SetLength(validLength);
        if (_flushToDisk) stream.Flush(flushToDisk: true);
    }

    private bool IsValidated(string path)
    {
        lock (_validated) return _validated.Contains(path);
    }

    private void MarkValidated(string path)
    {
        lock (_validated) _validated.Add(path);
    }

    private void ForgetValidated(string path)
    {
        lock (_validated) _validated.Remove(path);
    }

    /// <summary>
    /// Maps a document identifier to a file name safely.
    /// </summary>
    /// <remarks>
    /// Document identifiers come from callers and often from URLs, so they cannot be used as path
    /// components directly - <c>../../etc/passwd</c> is a document identifier as far as the type
    /// system is concerned. Hashing sidesteps path traversal, illegal characters and case-insensitive
    /// collisions in one step. This has no side effects, so probing identifiers that were never
    /// stored creates no files.
    /// </remarks>
    private (string Name, string Path) PathFor(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(documentId));
#if NET9_0_OR_GREATER
        string name = Convert.ToHexStringLower(hash.AsSpan(0, 16));
#else
        string name = Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
#endif
        return (name, Path.Combine(_root, name + ".bcl"));
    }

    private string SidecarPath(string name) => Path.Combine(_root, name + ".id");

    /// <summary>
    /// Records the original identifier beside the log, so the store can enumerate what it holds.
    /// </summary>
    /// <remarks>
    /// Written once, when the document is first stored, under the document's lock, and atomically:
    /// a crash mid-write must not leave a truncated identifier that would later be listed as real.
    /// With durability on, the temporary file reaches the disk before it is renamed, so the rename
    /// can never expose a name whose contents were not yet written.
    /// </remarks>
    private void WriteSidecar(string name, string documentId)
    {
        string sidecar = SidecarPath(name);
        if (File.Exists(sidecar)) return;

        string temporary = sidecar + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(documentId));
            if (_flushToDisk) stream.Flush(flushToDisk: true);
        }
        File.Move(temporary, sidecar, overwrite: true);
    }

    private static string? ReadDocumentId(string logPath)
    {
        string sidecar = Path.ChangeExtension(logPath, ".id");
        return File.Exists(sidecar) ? File.ReadAllText(sidecar, Encoding.UTF8) : null;
    }

    /// <summary>A lock on one log file, shared by every store instance in the process.</summary>
    private sealed class FileGate
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private int _users;

        public static async ValueTask<Lease> AcquireAsync(string path, CancellationToken cancellationToken)
        {
            FileGate gate;
            lock (Gates)
            {
                if (!Gates.TryGetValue(path, out gate!))
                {
                    gate = new FileGate();
                    Gates[path] = gate;
                }
                gate._users++;
            }

            try
            {
                await gate._semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                Release(path, gate, holding: false);
                throw;
            }

            return new Lease(path, gate);
        }

        private static void Release(string path, FileGate gate, bool holding)
        {
            if (holding) gate._semaphore.Release();

            lock (Gates)
            {
                if (--gate._users == 0) Gates.Remove(path);
            }
        }

        public readonly struct Lease(string path, FileGate gate) : IDisposable
        {
            public void Dispose() => Release(path, gate, holding: true);
        }
    }
}
