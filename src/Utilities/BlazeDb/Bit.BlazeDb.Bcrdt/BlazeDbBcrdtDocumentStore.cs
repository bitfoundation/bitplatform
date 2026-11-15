using Bit.Bcrdt;

namespace Bit.BlazeDb.Bcrdt;

/// <summary>
/// An <see cref="IBcrdtDocumentStore"/> that keeps each document's updates as rows of a BlazeDb table
/// (<see cref="BlazeDbBcrdtRecord"/>), in the same database as the application's other data.
/// </summary>
/// <remarks>
/// <para>
/// Bcrdt stays in charge of the document: what an update means, how updates merge, and when to compact. The store keeps
/// bytes. An append is one insert - committed in memory at once and journaled by BlazeDb's write-ahead log, which reaches
/// storage within the database's <see cref="BlazeDbDatabaseOptions.FlushInterval"/>, the durability Bcrdt asks of a store:
/// one that loses the last record in a crash is still correct, because a peer resends it. Call
/// <see cref="BlazeDbDatabase.FlushAsync"/> where an application needs an edit on storage at once.
/// </para>
/// <para>
/// A compaction merges a document's records and replaces them in one transaction, so a crash leaves either every record
/// or the merged one, never neither; a delete removes a document's records in one transaction too. Updates are held in
/// memory with the rest of the database's rows, which is what makes loading a document a lookup rather than a read; keep
/// the number of records down by letting Bcrdt compact.
/// </para>
/// <para>
/// Calls from several threads are safe - each write takes the database's write lock - but, as for any BlazeDb database,
/// reads concurrent with writes on another thread are the host's to serialize.
/// </para>
/// </remarks>
public sealed class BlazeDbBcrdtDocumentStore : IBcrdtDocumentStore
{
    private readonly Func<CancellationToken, ValueTask<BlazeDbDatabase>> _database;
    private readonly SemaphoreSlim _opening = new(1, 1);
    private BlazeDbDatabase? _db;
    private BlazeDbTable<Guid, BlazeDbBcrdtRecord>? _table;

    /// <summary>Stores documents in <paramref name="database"/>, which has the table registered (<see cref="BlazeDbBcrdtExtensions.AddBcrdtDocuments"/>).</summary>
    public BlazeDbBcrdtDocumentStore(BlazeDbDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = _ => ValueTask.FromResult(database);
    }

    /// <summary>Stores documents in the database <paramref name="database"/> opens, on first use. A failed open is tried again on the next use.</summary>
    public BlazeDbBcrdtDocumentStore(Func<CancellationToken, ValueTask<BlazeDbDatabase>> database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <inheritdoc />
    public async ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        var (_, table) = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var records = Records(table, documentId).ToList();
        return records.Count switch
        {
            0 => default,
            // A copy, so a caller that changes the buffer cannot change the row the table holds.
            1 => new BcrdtStoredDocument((byte[])records[0].Update.Clone(), 1),
            _ => new BcrdtStoredDocument(
                BcrdtUpdateOperations.Merge([.. records.Select(record => (ReadOnlyMemory<byte>)record.Update)]),
                records.Count),
        };
    }

    /// <inheritdoc />
    public async ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        var (db, table) = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var bytes = update.ToArray();
        // Numbered and inserted as one unit, so two appends to one document on different threads never take one number.
        db.RunInTransaction(() => table.Insert(new BlazeDbBcrdtRecord
        {
            Id = Guid.CreateVersion7(),
            DocumentId = documentId,
            Sequence = LastSequence(table, documentId) + 1,
            Update = bytes,
        }));
    }

    /// <inheritdoc />
    public async ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        var (db, table) = await OpenAsync(cancellationToken).ConfigureAwait(false);
        db.RunInTransaction(() =>
        {
            // Read, merged and replaced under the write lock, so an append on another thread lands before or after the
            // compaction - never between the read and the replacement, where it would be dropped.
            var records = Records(table, documentId).ToList();
            if (records.Count <= 1)
            {
                return;
            }
            var merged = BcrdtUpdateOperations.Merge([.. records.Select(record => (ReadOnlyMemory<byte>)record.Update)]);
            table.DeleteMany(records.Select(record => record.Id));
            table.Insert(new BlazeDbBcrdtRecord
            {
                Id = Guid.CreateVersion7(),
                DocumentId = documentId,
                Sequence = records[^1].Sequence,
                Update = merged,
            });
        });
    }

    /// <inheritdoc />
    public async ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        var (_, table) = await OpenAsync(cancellationToken).ConfigureAwait(false);
        table.DeleteMany(table.Lookup(BlazeDbBcrdtRecord.Indexes.DocumentId, documentId).Select(record => record.Id));
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        var (_, table) = await OpenAsync(cancellationToken).ConfigureAwait(false);
        return table.CountBy(BlazeDbBcrdtRecord.Indexes.DocumentId)
            .Where(entry => entry.Value > 0)
            .Select(entry => entry.Key)
            .ToArray();
    }

    /// <summary>A document's records, oldest first.</summary>
    private static IEnumerable<BlazeDbBcrdtRecord> Records(BlazeDbTable<Guid, BlazeDbBcrdtRecord> table, string documentId) =>
        table.Range(BlazeDbBcrdtRecord.Indexes.ByDocument, (documentId, long.MinValue), (documentId, long.MaxValue));

    private static long LastSequence(BlazeDbTable<Guid, BlazeDbBcrdtRecord> table, string documentId) =>
        table.Range(BlazeDbBcrdtRecord.Indexes.ByDocument, (documentId, long.MinValue), (documentId, long.MaxValue), descending: true)
            .Select(record => record.Sequence)
            .FirstOrDefault();

    private async ValueTask<(BlazeDbDatabase, BlazeDbTable<Guid, BlazeDbBcrdtRecord>)> OpenAsync(CancellationToken cancellationToken)
    {
        if (_table is { } table)
        {
            return (_db!, table);
        }
        await _opening.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_table is null)
            {
                var database = await _database(cancellationToken).ConfigureAwait(false);
                try
                {
                    _table = database.GetTable(BlazeDbBcrdtRecord.Table);
                }
                catch (BlazeDbException ex)
                {
                    throw new InvalidOperationException(
                        "The BlazeDb database Bcrdt stores documents in has no Bcrdt record table. Register it where the " +
                        "database is opened: new BlazeDbDatabaseOptions().AddBcrdtDocuments().", ex);
                }
                _db = database;
            }
            return (_db!, _table);
        }
        finally
        {
            _opening.Release();
        }
    }
}
