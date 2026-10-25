using System.Buffers.Binary;
using Microsoft.JSInterop;

namespace Bit.Bcrdt;

/// <summary>
/// A store that keeps documents in the browser's IndexedDB, so a Blazor WebAssembly application keeps
/// its documents - and the edits made offline - across a reload, a closed tab and a restarted browser.
/// </summary>
/// <remarks>
/// <para>
/// Each update is one record, appended in its own IndexedDB transaction with strict durability: the
/// write is reported done only once the browser has committed it to disk, so an update the store has
/// accepted survives a crash. Compaction reads a document's records, merges them in .NET and then, in
/// one transaction, deletes exactly the records it read and adds the merged one. A record appended in
/// the meantime - by another tab of the same application, say - is never touched, so two tabs writing
/// and compacting the same document cannot lose each other's work.
/// </para>
/// <para>
/// Writes happen in the background, so a change is stored a few milliseconds after it is made: a tab
/// closed or reloaded in that moment loses it from this browser - though not from a peer or server it had
/// already reached. <see cref="BcrdtCollaborativeDocument.FlushAsync"/> waits for it, for an application
/// that is about to navigate away on purpose.
/// </para>
/// <para>
/// Several tabs may hold the same document open at once. Each is its own replica with its own client
/// identity, all writing to the same store; updates merge, so what any one of them loads is the union of
/// everything every tab stored.
/// </para>
/// <para>
/// IndexedDB is reached through JavaScript interop, which does not exist while a component is
/// prerendered: open documents backed by this store only once the component is interactive.
/// <see cref="Blazor.BcrdtCollaborationScope"/> does that by itself. A whole stored document is read back
/// as a stream, so in Blazor Server - where the store holds the circuit's replica in the browser - it is
/// not subject to the circuit's message size limit.
/// </para>
/// <para>
/// A browser may refuse a write when the site has used up its storage quota; the failure is reported
/// as an <see cref="IOException"/> naming the quota, and <see cref="BcrdtDocumentPersistence"/> keeps the
/// update and retries it, as with any write that may yet succeed.
/// </para>
/// </remarks>
public sealed class BcrdtIndexedDbDocumentStore : IBcrdtDocumentStore, IAsyncDisposable
{
    /// <summary>The database name used when none is given.</summary>
    public const string DefaultDatabaseName = "bit-bcrdt";

    /// <summary>The largest stored document read back in one load: 1 GB.</summary>
    private const long MaxLoadSize = 1L << 30;

    private const int RecordHeaderSize = 12;

    /// <summary>The first byte of what the module returns from a load; it must match the module's.</summary>
    private const byte FrameFormat = 1;

    private readonly IJSRuntime _js;
    private readonly string _database;
    private readonly SemaphoreSlim _moduleGate = new(1, 1);
    private IJSObjectReference? _module;
    private bool _disposed;

    /// <summary>Creates a store over a database in the browser this application runs in.</summary>
    /// <param name="js">The JavaScript runtime of the browser holding the database.</param>
    /// <param name="databaseName">
    /// The IndexedDB database to use. Applications on the same origin that must not see each other's
    /// documents use different names.
    /// </param>
    public BcrdtIndexedDbDocumentStore(IJSRuntime js, string databaseName = DefaultDatabaseName)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentException.ThrowIfNullOrEmpty(databaseName);

        _js = js;
        _database = databaseName;
    }

    /// <summary>
    /// Raised when a stored record cannot be read as an update - written by something else, or by a newer
    /// build. The record is left where it is and skipped, so the rest of the document still opens.
    /// </summary>
    public event Action<string>? UnreadableRecordSkipped;

    /// <inheritdoc/>
    public async ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        List<(double Id, byte[] Data)> records = await ReadRecordsAsync(documentId, cancellationToken).ConfigureAwait(false);
        List<byte[]> readable = Readable(documentId, records);

        return readable.Count switch
        {
            0 => default,
            1 => new BcrdtStoredDocument(readable[0], 1),
            _ => new BcrdtStoredDocument(BcrdtUpdateOperations.Merge([.. readable.Select(record => (ReadOnlyMemory<byte>)record)]), readable.Count),
        };
    }

    /// <inheritdoc/>
    public async ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        if (update.Length == 0) return;

        IJSObjectReference module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        await CallAsync(() => module.InvokeVoidAsync("idbAppend", cancellationToken, _database, documentId, update.ToArray())).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        List<(double Id, byte[] Data)> records = await ReadRecordsAsync(documentId, cancellationToken).ConfigureAwait(false);

        // Only readable records are replaced: one that cannot be read is kept as it is rather than
        // destroyed by a merge that cannot include it.
        List<(double Id, byte[] Data)> readable = [.. records.Where(record => IsDecodable(record.Data))];
        if (readable.Count <= 1) return;

        byte[] merged = BcrdtUpdateOperations.Merge([.. readable.Select(record => (ReadOnlyMemory<byte>)record.Data)]);
        double[] replaced = [.. readable.Select(record => record.Id)];

        IJSObjectReference module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        await CallAsync(() => module.InvokeVoidAsync("idbReplace", cancellationToken, _database, documentId, replaced, merged)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        IJSObjectReference module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        await CallAsync(() => module.InvokeVoidAsync("idbDelete", cancellationToken, _database, documentId)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        IJSObjectReference module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        string[] ids = await CallAsync(() => module.InvokeAsync<string[]>("idbList", cancellationToken, _database)).ConfigureAwait(false);
        return ids;
    }

    /// <summary>Reads a document's records, each with the key it is stored under.</summary>
    /// <remarks>
    /// The records come back as one stream - a format byte, then <c>[float64 key][uint32 length][bytes]</c>
    /// per record - so a large document crosses the interop boundary in chunks rather than as one message.
    /// </remarks>
    private async Task<List<(double Id, byte[] Data)>> ReadRecordsAsync(string documentId, CancellationToken cancellationToken)
    {
        IJSObjectReference module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        IJSStreamReference stream = await CallAsync(
            () => module.InvokeAsync<IJSStreamReference>("idbLoad", cancellationToken, _database, documentId)).ConfigureAwait(false);

        byte[] framed;
        await using (stream.ConfigureAwait(false))
        {
            await using Stream data = await stream.OpenReadStreamAsync(MaxLoadSize, cancellationToken).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            await data.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            framed = buffer.ToArray();
        }

        return Unframe(framed);
    }

    internal static List<(double Id, byte[] Data)> Unframe(ReadOnlySpan<byte> framed)
    {
        if (framed.Length == 0 || framed[0] != FrameFormat)
        {
            throw new InvalidDataException("The browser store returned records in a format this build does not read.");
        }

        List<(double, byte[])> records = [];
        int offset = 1;
        while (offset + RecordHeaderSize <= framed.Length)
        {
            double id = BinaryPrimitives.ReadDoubleLittleEndian(framed[offset..]);
            int length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(framed[(offset + 8)..]));
            offset += RecordHeaderSize;
            if (length > framed.Length - offset) throw new InvalidDataException("The browser store returned a truncated record.");

            records.Add((id, framed.Slice(offset, length).ToArray()));
            offset += length;
        }

        if (offset != framed.Length) throw new InvalidDataException("The browser store returned a truncated record.");
        return records;
    }

    private List<byte[]> Readable(string documentId, List<(double Id, byte[] Data)> records)
    {
        List<byte[]> readable = new(records.Count);
        foreach ((double _, byte[] data) in records)
        {
            if (IsDecodable(data)) readable.Add(data);
            else UnreadableRecordSkipped?.Invoke(documentId);
        }
        return readable;
    }

    private static bool IsDecodable(byte[] record)
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

    private async ValueTask<IJSObjectReference> ModuleAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_module is { } loaded) return loaded;

        await _moduleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _module ??= await CallAsync(
                () => _js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./_content/Bit.Bcrdt/bit-bcrdt.js")).ConfigureAwait(false);
        }
        finally
        {
            _moduleGate.Release();
        }
    }

    /// <summary>Runs an interop call, turning the browser's quota refusal into an error that says so.</summary>
    private static async ValueTask CallAsync(Func<ValueTask> call)
    {
        try
        {
            await call().ConfigureAwait(false);
        }
        catch (JSException ex) when (IsQuotaError(ex))
        {
            throw QuotaExceeded(ex);
        }
    }

    private static async ValueTask<T> CallAsync<T>(Func<ValueTask<T>> call)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (JSException ex) when (IsQuotaError(ex))
        {
            throw QuotaExceeded(ex);
        }
    }

    private static bool IsQuotaError(JSException exception) =>
        exception.Message.Contains("QuotaExceededError", StringComparison.Ordinal);

    private static IOException QuotaExceeded(JSException exception) => new(
        "The browser refused to store more for this site: its storage quota is used up. The change is kept and retried; " +
        "freeing space - or asking for persistent storage with navigator.storage.persist() - lets it through.",
        exception);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        IJSObjectReference? module = _module;
        _module = null;
        if (module is null) return;

        try
        {
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // The browser is gone, and the module with it.
        }
    }
}
