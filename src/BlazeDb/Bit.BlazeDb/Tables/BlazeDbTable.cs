using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb;

/// <summary>
/// An in-memory table: a primary-key hash index over live row objects, plus any secondary
/// indexes declared on the descriptor. All reads are plain in-memory operations with no I/O.
/// Mutations flow through the owning <see cref="BlazeDbDatabase"/>'s transaction machinery.
/// </summary>
public sealed class BlazeDbTable<TKey, TRow> : IBlazeDbTableInternal
    where TKey : notnull
{
    internal readonly record struct Entry(TRow Row, long Seq);

    private readonly BlazeDbDatabase _db;
    private readonly Dictionary<TKey, Entry> _rows = new();
    private readonly IBlazeDbIndexStore<TRow>[] _indexStores;

    internal BlazeDbTable(BlazeDbDatabase db, BlazeDbTableDescriptor<TKey, TRow> descriptor)
    {
        _db = db;
        Descriptor = descriptor;
        _indexStores = new IBlazeDbIndexStore<TRow>[descriptor.Indexes.Count];
        for (var i = 0; i < _indexStores.Length; i++)
        {
            _indexStores[i] = descriptor.Indexes[i].CreateStore();
        }
    }

    public BlazeDbTableDescriptor<TKey, TRow> Descriptor { get; }

    public string Name => Descriptor.Name;

    public int Count => _rows.Count;

    // ---- Reads (no I/O, no locking: single-writer model) ----

    public bool TryGet(TKey key, out TRow row)
    {
        if (_rows.TryGetValue(key, out var entry))
        {
            row = entry.Row;
            return true;
        }
        row = default!;
        return false;
    }

    public TRow? Get(TKey key) => _rows.TryGetValue(key, out var entry) ? entry.Row : default;

    public bool Contains(TKey key) => _rows.ContainsKey(key);

    /// <summary>
    /// Lazily enumerates all rows. Like any dictionary enumeration, mutating the table while
    /// enumerating invalidates the enumerator; materialize with ToList() if needed.
    /// </summary>
    public IEnumerable<TRow> Scan()
    {
        foreach (var entry in _rows.Values)
        {
            yield return entry.Row;
        }
    }

    // ---- Index reads (O(1) / O(log n), no I/O) ----

    /// <summary>All rows whose indexed value equals <paramref name="key"/> (hash index).</summary>
    public IEnumerable<TRow> Lookup<TIndexKey>(BlazeDbHashIndexDefinition<TRow, TIndexKey> index, TIndexKey key) =>
        ((BlazeDbHashIndexStore<TRow, TIndexKey>)GetIndexStore(index)).Lookup(key);

    /// <summary>Number of rows whose indexed value equals <paramref name="key"/>.</summary>
    public int CountBy<TIndexKey>(BlazeDbHashIndexDefinition<TRow, TIndexKey> index, TIndexKey key) =>
        ((BlazeDbHashIndexStore<TRow, TIndexKey>)GetIndexStore(index)).CountOf(key);

    /// <summary>All rows sorted by the ordered index.</summary>
    public IEnumerable<TRow> OrderBy<TIndexKey>(BlazeDbOrderedIndexDefinition<TRow, TIndexKey> index, bool descending = false) =>
        ((BlazeDbOrderedIndexStore<TRow, TIndexKey>)GetIndexStore(index)).All(descending);

    /// <summary>
    /// Rows whose indexed value lies in the inclusive range [from, to], in index order.
    /// Use <see cref="BlazeDbBound{T}.Unbounded"/> for an open end.
    /// </summary>
    public IEnumerable<TRow> Range<TIndexKey>(
        BlazeDbOrderedIndexDefinition<TRow, TIndexKey> index,
        BlazeDbBound<TIndexKey> from,
        BlazeDbBound<TIndexKey> to,
        bool descending = false) =>
        ((BlazeDbOrderedIndexStore<TRow, TIndexKey>)GetIndexStore(index))
            .Range(from.HasValue, from.Value, to.HasValue, to.Value, descending);

    /// <summary>
    /// Equality lookup against an index chosen at runtime. See
    /// <see cref="IBlazeDbIndexStore{TRow}.LookupBoxed"/> - this exists for the EF Core provider, which
    /// picks an index from a LINQ predicate and has no static knowledge of the key type.
    /// </summary>
    internal IEnumerable<TRow> LookupBoxed(BlazeDbIndexDefinition<TRow> index, object? key) =>
        GetIndexStore(index).LookupBoxed(key);

    /// <summary>Range scan against an index chosen at runtime. See <see cref="LookupBoxed"/>.</summary>
    internal IEnumerable<TRow> RangeBoxed(
        BlazeDbIndexDefinition<TRow> index, bool hasFrom, object? from, bool hasTo, object? to, bool descending) =>
        GetIndexStore(index).RangeBoxed(hasFrom, from, hasTo, to, descending);

    // ---- Writes ----

    /// <summary>Adds a new row; throws <see cref="BlazeDbDuplicateKeyException"/> if the key exists.</summary>
    public void Insert(TRow row)
    {
        // The checks and the write they guard happen under one lock, so two writers cannot both find
        // the key free and then overwrite one another instead of one of them being refused. Every
        // write below is locked for the same reason; the lock is the one ApplyWrite takes.
        lock (_db.SyncRoot)
        {
            var key = Descriptor.KeySelector(row);
            if (_rows.ContainsKey(key))
            {
                throw new BlazeDbDuplicateKeyException(Name, key);
            }
            ValidateUnique(row, replacedSeq: 0);
            _db.ApplyWrite(new SetOp(this, key, row, hadOld: false, default, 0));
        }
    }

    /// <summary>
    /// Replaces an existing row; throws <see cref="KeyNotFoundException"/> if missing. Pass a new
    /// instance (or an unmodified one): the table finds the index entries to retract through the
    /// values of the row it currently holds, so mutating that instance's indexed properties first
    /// and then passing it back is rejected - use <see cref="UpdateInPlace"/> for that.
    /// </summary>
    public void Update(TRow row)
    {
        lock (_db.SyncRoot)
        {
            var key = Descriptor.KeySelector(row);
            if (!_rows.TryGetValue(key, out var old))
            {
                throw new KeyNotFoundException($"Table '{Name}' has no row with key '{key}'.");
            }
            EnsureNotMutatedInPlace(row, old, nameof(UpdateInPlace));
            ValidateUnique(row, old.Seq);
            _db.ApplyWrite(new SetOp(this, key, row, hadOld: true, old.Row, old.Seq));
        }
    }

    /// <summary>Inserts or replaces. See <see cref="Update"/> for the rules on replacing.</summary>
    public void Upsert(TRow row)
    {
        lock (_db.SyncRoot)
        {
            var key = Descriptor.KeySelector(row);
            var had = _rows.TryGetValue(key, out var old);
            if (had)
            {
                EnsureNotMutatedInPlace(row, old, nameof(UpdateInPlace));
            }
            ValidateUnique(row, had ? old.Seq : 0);
            _db.ApplyWrite(new SetOp(this, key, row, had, had ? old.Row : default, old.Seq));
        }
    }

    /// <summary>
    /// Refuses a write that would strand index entries: the caller mutated the very instance the
    /// table holds and handed it back, so the values the indexes were built from are gone. Failing
    /// here, with a pointer to the right call, beats an index that quietly returns wrong rows. The
    /// check is only paid on that path - a fresh instance never triggers it - and it covers a value
    /// that was changed to null as well, since the indexes record those entries too.
    /// </summary>
    private void EnsureNotMutatedInPlace(TRow row, Entry held, string alternative)
    {
        if (!ReferenceEquals(row, held.Row))
        {
            return;
        }
        foreach (var store in _indexStores)
        {
            if (!store.EntryMatches(row, held.Seq))
            {
                throw new InvalidOperationException(
                    $"The row being written to table '{Name}' is the instance the table already holds, and " +
                    $"its value for index '{((BlazeDbIndexDefinition<TRow>)store.Definition).Name}' was changed in place. " +
                    "The table can no longer find the entry to retract, so the write is refused. Pass a new " +
                    $"row instance instead, or call {alternative} with the values the row had when it was last written.");
            }
        }
    }

    /// <summary>
    /// Rejects the write before anything is mutated, so a violation leaves the table and its
    /// indexes untouched and needs no rollback.
    /// </summary>
    private void ValidateUnique(TRow row, long replacedSeq)
    {
        foreach (var store in _indexStores)
        {
            store.ValidateUnique(row, replacedSeq, Name);
        }
    }

    /// <summary>
    /// Removes a row; returns false if the key does not exist. If the held instance was mutated in
    /// place since it was written, the delete is refused for the same reason as <see cref="Update"/>;
    /// use <see cref="DeleteInPlace"/> then.
    /// </summary>
    public bool Delete(TKey key)
    {
        lock (_db.SyncRoot)
        {
            if (!_rows.TryGetValue(key, out var old))
            {
                return false;
            }
            EnsureNotMutatedInPlace(old.Row, old, nameof(DeleteInPlace));
            _db.ApplyWrite(new DeleteOp(this, key, old.Row, old.Seq));
            return true;
        }
    }

    // ---- Writes against rows that were mutated in place ----
    //
    // Callers normally hand the table a new row object, so the entry it still holds describes the
    // values its indexes were built from. A caller that instead mutates the stored instance -
    // which is what an EF Core change tracker does, since queries hand back the live row - leaves
    // the table unable to work out which index entries to retract. These two methods take that
    // earlier state explicitly. Everything downstream (index maintenance, undo, WAL records) is
    // the ordinary path.

    /// <summary>
    /// Records an update to a row that was mutated in place. <paramref name="previousValues"/> must
    /// hold the values the row had when the table last indexed it; passing the mutated instance
    /// would leave stale index entries behind.
    /// </summary>
    public void UpdateInPlace(TRow row, TRow previousValues)
    {
        _db.EnsureWritable();
        lock (_db.SyncRoot)
        {
            var key = Descriptor.KeySelector(row);
            if (!_rows.TryGetValue(key, out var old))
            {
                throw new KeyNotFoundException($"Table '{Name}' has no row with key '{key}'.");
            }
            if (!EqualityComparer<TKey>.Default.Equals(Descriptor.KeySelector(previousValues), key))
            {
                throw new InvalidOperationException(
                    $"The primary key of a row in table '{Name}' cannot be changed in place; " +
                    "delete the old row and insert a new one.");
            }

            // Both checks run before anything is touched. The caller's instance stays the row the
            // table holds - a change tracker keeps its identity, and a corrected retry with the same
            // previousValues still finds the index entries - while the indexes go on describing the
            // pre-mutation values until a write moves them.
            EnsurePreviousValuesMatch(previousValues, old.Seq);
            ValidateUnique(row, old.Seq);

            // Point the entry at the pre-mutation values so the write's index maintenance retracts
            // the entries that actually exist (and so a rollback restores them).
            _rows[key] = new Entry(previousValues, old.Seq);
            _db.ApplyWrite(new SetOp(this, key, row, hadOld: true, previousValues, old.Seq));
        }
    }

    /// <summary>
    /// Removes a row that was mutated in place, using <paramref name="previousValues"/> to find the
    /// index entries to retract. Returns false if the key does not exist.
    /// </summary>
    public bool DeleteInPlace(TKey key, TRow previousValues)
    {
        _db.EnsureWritable();
        lock (_db.SyncRoot)
        {
            if (!_rows.TryGetValue(key, out var old))
            {
                return false;
            }
            EnsurePreviousValuesMatch(previousValues, old.Seq);
            _rows[key] = new Entry(previousValues, old.Seq);
            _db.ApplyWrite(new DeleteOp(this, key, previousValues, old.Seq));
            return true;
        }
    }

    /// <summary>
    /// Refuses an in-place write whose <c>previousValues</c> are not what the indexes were built
    /// from. Retracting an entry that is not there leaves the row indexed under a value it no longer
    /// carries and adds a second entry under the value it does, so the same row comes back twice from
    /// one lookup - silent, and impossible to explain later. The caller's state is the stale thing
    /// here, so it is told so rather than the table quietly going wrong.
    /// </summary>
    private void EnsurePreviousValuesMatch(TRow previousValues, long seq)
    {
        foreach (var store in _indexStores)
        {
            if (!store.EntryMatches(previousValues, seq))
            {
                throw new BlazeDbStaleRowException(Name, ((BlazeDbIndexDefinition<TRow>)store.Definition).Name);
            }
        }
    }

    // ---- Raw mutation primitives (index-maintaining; used by ops, replay and snapshot load) ----

    private void SetEntry(TKey key, TRow row, long seq)
    {
        if (_rows.TryGetValue(key, out var old))
        {
            RemoveFromIndexes(old);
        }
        _rows[key] = new Entry(row, seq);
        foreach (var store in _indexStores)
        {
            store.Add(row, seq);
        }
    }

    private void RemoveEntry(TKey key)
    {
        if (_rows.Remove(key, out var old))
        {
            RemoveFromIndexes(old);
        }
    }

    private void RemoveFromIndexes(Entry entry)
    {
        foreach (var store in _indexStores)
        {
            store.Remove(entry.Row, entry.Seq);
        }
    }

    internal IBlazeDbIndexStore<TRow> GetIndexStore(BlazeDbIndexDefinition<TRow> definition)
    {
        foreach (var store in _indexStores)
        {
            if (ReferenceEquals(store.Definition, definition))
            {
                return store;
            }
        }
        throw new BlazeDbException($"Index '{definition.Name}' is not defined on table '{Name}'.");
    }

    // ---- IBlazeDbTableInternal (replay / snapshot) ----

    void IBlazeDbTableInternal.ReplaySet(ReadOnlySpan<byte> keyBytes, ReadOnlySpan<byte> rowBytes)
    {
        var keyReader = new BlazeDbBufferReader(keyBytes);
        var key = Descriptor.KeyReader(ref keyReader);
        var rowReader = new BlazeDbBufferReader(rowBytes);
        var row = Descriptor.RowReader(ref rowReader);
        SetEntry(key, row, _db.NextSeq());
    }

    void IBlazeDbTableInternal.ReplayDelete(ReadOnlySpan<byte> keyBytes)
    {
        var keyReader = new BlazeDbBufferReader(keyBytes);
        var key = Descriptor.KeyReader(ref keyReader);
        RemoveEntry(key);
    }

    void IBlazeDbTableInternal.WriteSnapshot(BlazeDbBufferWriter writer, BlazeDbBufferWriter scratch)
    {
        writer.WriteVarUInt((ulong)_rows.Count);
        foreach (var (key, entry) in _rows)
        {
            // The key goes in front of the row, as in a WAL record, so a database opened without
            // this table's descriptor can still carry the rows along (see BlazeDbOpaqueTable).
            scratch.Reset();
            Descriptor.KeyWriter(scratch, key);
            writer.WriteBytes(scratch.WrittenSpan);
            scratch.Reset();
            Descriptor.RowWriter(scratch, entry.Row);
            writer.WriteBytes(scratch.WrittenSpan);
        }
    }

    void IBlazeDbTableInternal.Clear()
    {
        _rows.Clear();
        foreach (var store in _indexStores)
        {
            store.Clear();
        }
    }

    void IBlazeDbTableInternal.LoadSnapshot(ref BlazeDbBufferReader reader, byte formatVersion)
    {
        var count = BlazeDbSnapshotFormat.ReadRowCount(ref reader, Name);
        var keyed = formatVersion >= BlazeDbSnapshotFormat.KeyedRows;
        _rows.EnsureCapacity(count);
        for (var i = 0; i < count; i++)
        {
            if (keyed)
            {
                reader.ReadBytes(); // The key is derived from the row here; it is stored for opaque tables.
            }
            var rowBytes = reader.ReadBytes();
            var rowReader = new BlazeDbBufferReader(rowBytes);
            var row = Descriptor.RowReader(ref rowReader);
            SetEntry(Descriptor.KeySelector(row), row, _db.NextSeq());
        }
    }

    // ---- BlazeDbTransaction ops ----

    private sealed class SetOp : IBlazeDbTxnOp
    {
        private readonly BlazeDbTable<TKey, TRow> _table;
        private readonly TKey _key;
        private readonly TRow _row;
        private readonly bool _hadOld;
        private readonly TRow? _oldRow;
        private readonly long _oldSeq;

        public SetOp(BlazeDbTable<TKey, TRow> table, TKey key, TRow row, bool hadOld, TRow? oldRow, long oldSeq)
        {
            _table = table;
            _key = key;
            _row = row;
            _hadOld = hadOld;
            _oldRow = oldRow;
            _oldSeq = oldSeq;
        }

        public void Apply() => _table.SetEntry(_key, _row, _table._db.NextSeq());

        public void Revert()
        {
            if (_hadOld)
            {
                _table.SetEntry(_key, _oldRow!, _oldSeq);
            }
            else
            {
                _table.RemoveEntry(_key);
            }
        }

        public void EncodeRedo(BlazeDbBufferWriter payload, BlazeDbBufferWriter scratch)
        {
            payload.WriteByte(BlazeDbWalOp.Set);
            payload.WriteString(_table.Name);
            scratch.Reset();
            _table.Descriptor.KeyWriter(scratch, _key);
            payload.WriteBytes(scratch.WrittenSpan);
            scratch.Reset();
            _table.Descriptor.RowWriter(scratch, _row);
            payload.WriteBytes(scratch.WrittenSpan);
        }
    }

    private sealed class DeleteOp : IBlazeDbTxnOp
    {
        private readonly BlazeDbTable<TKey, TRow> _table;
        private readonly TKey _key;
        private readonly TRow _oldRow;
        private readonly long _oldSeq;

        public DeleteOp(BlazeDbTable<TKey, TRow> table, TKey key, TRow oldRow, long oldSeq)
        {
            _table = table;
            _key = key;
            _oldRow = oldRow;
            _oldSeq = oldSeq;
        }

        public void Apply() => _table.RemoveEntry(_key);

        public void Revert() => _table.SetEntry(_key, _oldRow, _oldSeq);

        public void EncodeRedo(BlazeDbBufferWriter payload, BlazeDbBufferWriter scratch)
        {
            payload.WriteByte(BlazeDbWalOp.Delete);
            payload.WriteString(_table.Name);
            scratch.Reset();
            _table.Descriptor.KeyWriter(scratch, _key);
            payload.WriteBytes(scratch.WrittenSpan);
        }
    }
}
