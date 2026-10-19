using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Bit.Bcrdt;

/// <summary>
/// A store that appends updates to a length-prefixed, checksummed log file per document.
/// </summary>
/// <remarks>
/// <para>
/// Each record is a length, a CRC-32 and a payload. That framing is what makes the store survive a
/// crash: an append that was interrupted leaves a record whose length runs past the end of the file
/// or whose checksum does not match. The first time a log is touched after that, the damaged tail is
/// cut off, so loading stops cleanly where the good records end and later appends land after them
/// rather than behind unreadable bytes. The cost is at most the update that was being written, which
/// the peer still holds and will resend on the next sync.
/// </para>
/// <para>
/// Compaction reads, merges and replaces the log as one step that no append can interleave with. It
/// writes the merged log to a temporary file, forces it to disk, and only then renames it over the
/// old one, so a crash or power loss leaves either the old log or the new one, never a half-merged
/// or empty one. A merged document too large to be one record is left uncompacted rather than
/// written as a record the reader would refuse.
/// </para>
/// <para>
/// Every operation on a document is serialized with every other operation on the same document;
/// different documents proceed independently.
/// </para>
/// </remarks>
public sealed class BcrdtFileSystemDocumentStore : IBcrdtDocumentStore
{
    private const int HeaderSize = 8;
    private const int MaxRecordSize = 64 * 1024 * 1024;

    private readonly string _root;
    private readonly bool _flushToDisk;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> _validated = new(StringComparer.Ordinal);

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
        _root = rootDirectory;
        _flushToDisk = flushToDisk;
        Directory.CreateDirectory(_root);
    }

    /// <inheritdoc/>
    public async ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default)
    {
        (string name, string path) = PathFor(documentId);

        SemaphoreSlim gate = GateFor(name);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(path)) return default;

            byte[] contents = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            List<ReadOnlyMemory<byte>> records = ReadRecords(contents, out int validLength);
            if (validLength < contents.Length) TruncateTail(path, validLength);
            _validated[name] = true;

            if (records.Count == 0) return default;
            byte[] merged = records.Count == 1 ? records[0].ToArray() : BcrdtUpdateOperations.Merge([.. records]);
            return new BcrdtStoredDocument(merged, records.Count);
        }
        finally
        {
            gate.Release();
        }
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

        SemaphoreSlim gate = GateFor(name);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // A torn record left by a crash must be removed before anything is appended after it,
            // or every later record would be stranded behind bytes the reader stops at.
            if (!_validated.ContainsKey(name))
            {
                if (File.Exists(path))
                {
                    byte[] contents = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
                    ReadRecords(contents, out int validLength);
                    if (validLength < contents.Length) TruncateTail(path, validLength);
                }
                _validated[name] = true;
            }

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
                _validated.TryRemove(name, out _);
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
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public async ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default)
    {
        (string name, string path) = PathFor(documentId);

        SemaphoreSlim gate = GateFor(name);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(path)) return;

            byte[] contents = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            List<ReadOnlyMemory<byte>> records = ReadRecords(contents, out int validLength);
            if (records.Count <= 1)
            {
                if (validLength < contents.Length) TruncateTail(path, validLength);
                _validated[name] = true;
                return;
            }

            byte[] merged = BcrdtUpdateOperations.Merge([.. records]);

            // A record this large would be refused by the reader, turning the whole document into
            // nothing. Several smaller records are better than one unreadable one.
            if (merged.Length > MaxRecordSize) return;

            string temporary = path + ".compacting";
            await using (var stream = new FileStream(
                temporary, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await stream.WriteAsync(Frame(merged), cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                // The rename must not reach the disk before the data it names does.
                if (_flushToDisk) stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, path, overwrite: true);
            _validated[name] = true;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        (string name, string path) = PathFor(documentId);

        SemaphoreSlim gate = GateFor(name);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(path)) File.Delete(path);

            string sidecar = SidecarPath(name);
            if (File.Exists(sidecar)) File.Delete(sidecar);

            _validated.TryRemove(name, out _);
        }
        finally
        {
            gate.Release();
        }
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
    /// Walks the record framing, stopping at the first record that is incomplete or corrupt.
    /// </summary>
    /// <param name="contents">The log's bytes.</param>
    /// <param name="validLength">How many leading bytes hold intact records.</param>
    private static List<ReadOnlyMemory<byte>> ReadRecords(byte[] contents, out int validLength)
    {
        List<ReadOnlyMemory<byte>> records = [];
        int offset = 0;

        while (offset + HeaderSize <= contents.Length)
        {
            int length = BinaryPrimitives.ReadInt32LittleEndian(contents.AsSpan(offset, 4));
            uint expected = BinaryPrimitives.ReadUInt32LittleEndian(contents.AsSpan(offset + 4, 4));

            if (length <= 0 || length > MaxRecordSize || length > contents.Length - offset - HeaderSize) break;

            ReadOnlyMemory<byte> payload = contents.AsMemory(offset + HeaderSize, length);
            if (BcrdtCrc32.Compute(payload.Span) != expected) break;

            records.Add(payload);
            offset += HeaderSize + length;
        }

        validLength = offset;
        return records;
    }

    private static byte[] Frame(ReadOnlySpan<byte> payload)
    {
        byte[] record = new byte[HeaderSize + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(record.AsSpan(0, 4), payload.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(4, 4), BcrdtCrc32.Compute(payload));
        payload.CopyTo(record.AsSpan(HeaderSize));
        return record;
    }

    private void TruncateTail(string path, int validLength)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
        stream.SetLength(validLength);
        if (_flushToDisk) stream.Flush(flushToDisk: true);
    }

    private SemaphoreSlim GateFor(string name) => _gates.GetOrAdd(name, static _ => new SemaphoreSlim(1, 1));

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
}
