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
    private long _version;

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

    /// <summary>
    /// A number that changes every time a row of this table is written, deleted, rolled back or
    /// reloaded, and never goes back. Two reads that see the same version saw the same rows, so a
    /// component can skip re-querying when it has not moved. It says nothing about what changed.
    /// </summary>
    public long Version => Volatile.Read(ref _version);

    /// <summary>
    /// Raised after each commit that changed rows of this table - an autocommit write, a committed
    /// transaction, a bulk write - with the net change to every row it touched. Never raised for a write
    /// that is rolled back, nor part-way through a transaction: subscribers only ever see committed
    /// states. It runs synchronously on the thread that committed, after the commit has completed and
    /// the write lock is released, so a handler may read and write the database; a write it makes is
    /// delivered once the current notification has reached every subscriber. A handler that throws does
    /// not stop the others, and its exception surfaces from the write that committed - which has still
    /// committed. On a read-only replica it is raised with <see cref="BlazeDbTableChangedEventArgs{TKey, TRow}.IsReset"/>
    /// set after a reload.
    /// </summary>
    public event EventHandler<BlazeDbTableChangedEventArgs<TKey, TRow>>? Changed;

    /// <summary>
    /// Runs <paramref name="query"/> against this table now and again after every commit that changed it,
    /// keeping the latest result in <see cref="BlazeDbLiveQuery{TResult}.Result"/>. With
    /// <paramref name="relevantTo"/>, a commit re-runs the query only when a row it changed - as it was
    /// before or as it is after - passes the test, which keeps a narrow query from re-running on every
    /// write to a busy table. Dispose the live query to stop it.
    /// <code>
    /// using var open = todos.Watch(t =&gt; t.Lookup(TodoItem.Indexes.Done, false).ToList(), relevantTo: t =&gt; !t.Done);
    /// open.Changed += (_, _) =&gt; InvokeAsync(StateHasChanged);
    /// </code>
    /// </summary>
    public BlazeDbLiveQuery<TResult> Watch<TResult>(Func<BlazeDbTable<TKey, TRow>, TResult> query, Func<TRow, bool>? relevantTo = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        var live = new BlazeDbLiveQuery<TResult>(() => query(this));
        EventHandler<BlazeDbTableChangedEventArgs<TKey, TRow>> handler = (_, args) =>
        {
            if (!live.IsDisposed && (relevantTo is null || args.IsReset || args.Changes.Any(change => Concerns(change, relevantTo))))
            {
                live.Refresh();
            }
        };
        Changed += handler;
        live.Attach(() => Changed -= handler);
        return live;
    }

    private static bool Concerns(in BlazeDbRowChange<TKey, TRow> change, Func<TRow, bool> relevantTo) =>
        (change.Kind != BlazeDbChangeKind.Inserted && relevantTo(change.OldRow!)) ||
        (change.Kind != BlazeDbChangeKind.Deleted && relevantTo(change.NewRow!));

    /// <summary>Whether anything is listening for this table's changes, so a commit knows whether to collect them.</summary>
    internal bool IsObserved => Changed is not null;

    /// <summary>The engine's own metadata table, whose writes are not the application's changes to report.</summary>
    internal bool IsEngineTable => ReferenceEquals(Descriptor, BlazeDbMetaTable.Descriptor);

    internal void RaiseChanged(BlazeDbTableChangedEventArgs<TKey, TRow> args, ref List<Exception>? errors) =>
        BlazeDbChangeCollector.Raise(Changed, this, args, ref errors);

    void IBlazeDbTableInternal.RaiseReset(ref List<Exception>? errors) =>
        RaiseChanged(new BlazeDbTableChangedEventArgs<TKey, TRow>([], isReset: true), ref errors);

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
    /// Rows whose indexed value lies between <paramref name="from"/> and <paramref name="to"/>, in
    /// index order. Bounds are inclusive unless made with <see cref="BlazeDbBound{T}.Exclusive"/>;
    /// use <see cref="BlazeDbBound{T}.Unbounded"/> for an open end.
    /// </summary>
    public IEnumerable<TRow> Range<TIndexKey>(
        BlazeDbOrderedIndexDefinition<TRow, TIndexKey> index,
        BlazeDbBound<TIndexKey> from,
        BlazeDbBound<TIndexKey> to,
        bool descending = false) =>
        ((BlazeDbOrderedIndexStore<TRow, TIndexKey>)GetIndexStore(index))
            .Range(from.HasValue, from.Value, from.IsExclusive, to.HasValue, to.Value, to.IsExclusive, descending);

    /// <summary>
    /// Rows whose indexed string starts with <paramref name="prefix"/>, in index order - a range scan
    /// from the prefix to the first string that sorts after every string carrying it. The index has to
    /// compare ordinally, which is the default for a string index; under any other comparer the
    /// strings that share a prefix need not sort together, so the call is refused. The match is
    /// ordinal too: <c>"Ab"</c> does not find <c>"ab"</c>.
    /// </summary>
    public IEnumerable<TRow> StartsWith(
        BlazeDbOrderedIndexDefinition<TRow, string> index,
        string prefix,
        bool descending = false)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(prefix);
        if (!index.HasDefaultComparer)
        {
            throw new NotSupportedException(
                $"Index '{index.Name}' orders strings with a comparer of its own, so the strings that share a " +
                "prefix are not guaranteed to sort together. Prefix scans need an ordinal string index.");
        }
        var upper = PrefixUpperBound(prefix);
        return Range(index, prefix, upper is null ? BlazeDbBound<string>.Unbounded : BlazeDbBound<string>.Exclusive(upper), descending);
    }

    /// <summary>
    /// The smallest string, in ordinal order, that is greater than every string starting with
    /// <paramref name="prefix"/>: the prefix cut after its last character below <c>char.MaxValue</c>,
    /// with that character incremented. Null when there is none - an empty prefix, or one made only of
    /// <c>char.MaxValue</c> - and the range then has no upper end.
    /// </summary>
    internal static string? PrefixUpperBound(string prefix)
    {
        for (var i = prefix.Length - 1; i >= 0; i--)
        {
            if (prefix[i] != char.MaxValue)
            {
                return string.Create(i + 1, prefix, static (span, source) =>
                {
                    source.AsSpan(0, span.Length).CopyTo(span);
                    span[^1]++;
                });
            }
        }
        return null;
    }

    /// <summary>
    /// Every distinct value the hash index holds, with how many rows carry it - the counts a filter
    /// panel shows next to each option. It walks the index's buckets, not the rows, so it costs one
    /// step per distinct value. Rows whose value is null are counted under null. Order is unspecified.
    /// </summary>
    public IEnumerable<KeyValuePair<TIndexKey, int>> CountBy<TIndexKey>(BlazeDbHashIndexDefinition<TRow, TIndexKey> index) =>
        ((BlazeDbHashIndexStore<TRow, TIndexKey>)GetIndexStore(index)).Counts();

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
            var key = KeyOf(row);
            if (_rows.ContainsKey(key))
            {
                throw new BlazeDbDuplicateKeyException(Name, key);
            }
            InsertCore(key, row);
        }
        _db.DeliverChanges();
    }

    /// <summary>
    /// Adds <paramref name="row"/> unless a row with its key already exists, and says which happened.
    /// Only the key decides: a write that would duplicate a unique index value still throws
    /// <see cref="BlazeDbUniqueConstraintViolationException"/>, because that is a different row in the way.
    /// </summary>
    public bool TryInsert(TRow row)
    {
        lock (_db.SyncRoot)
        {
            var key = KeyOf(row);
            if (_rows.ContainsKey(key))
            {
                return false;
            }
            InsertCore(key, row);
        }
        _db.DeliverChanges();
        return true;
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
            var key = KeyOf(row);
            if (!_rows.TryGetValue(key, out var old))
            {
                throw new KeyNotFoundException($"Table '{Name}' has no row with key '{key}'.");
            }
            ReplaceCore(key, row, old);
        }
        _db.DeliverChanges();
    }

    /// <summary>
    /// Replaces the row with <paramref name="row"/>'s key if there is one, and returns false - writing
    /// nothing - if there is not. The rules on replacing are those of <see cref="Update"/>.
    /// </summary>
    public bool TryUpdate(TRow row)
    {
        lock (_db.SyncRoot)
        {
            var key = KeyOf(row);
            if (!_rows.TryGetValue(key, out var old))
            {
                return false;
            }
            ReplaceCore(key, row, old);
        }
        _db.DeliverChanges();
        return true;
    }

    /// <summary>
    /// Replaces the row with <paramref name="row"/>'s key only while the table still holds
    /// <paramref name="comparisonValue"/> for it - an optimistic concurrency check, in the manner of
    /// <c>ConcurrentDictionary.TryUpdate</c>. Read a row, build its replacement, and write it with the
    /// row you read as the comparison: if anything replaced or deleted the row in between, nothing is
    /// written and the call returns false, so the caller can re-read and decide again.
    /// <para>
    /// The comparison uses <see cref="EqualityComparer{T}.Default"/>: by reference for an ordinary class,
    /// by value for a record. Rows are replaced rather than mutated (see <see cref="Update"/>), so a
    /// class row that compares by reference changes identity on every write and the check is exact.
    /// </para>
    /// </summary>
    public bool TryUpdate(TRow row, TRow comparisonValue)
    {
        lock (_db.SyncRoot)
        {
            var key = KeyOf(row);
            if (!_rows.TryGetValue(key, out var old) || !EqualityComparer<TRow>.Default.Equals(old.Row, comparisonValue))
            {
                return false;
            }
            ReplaceCore(key, row, old);
        }
        _db.DeliverChanges();
        return true;
    }

    /// <summary>Inserts or replaces. See <see cref="Update"/> for the rules on replacing.</summary>
    public void Upsert(TRow row)
    {
        lock (_db.SyncRoot)
        {
            var key = KeyOf(row);
            if (_rows.TryGetValue(key, out var old))
            {
                ReplaceCore(key, row, old);
            }
            else
            {
                InsertCore(key, row);
            }
        }
        _db.DeliverChanges();
    }

    private void InsertCore(TKey key, TRow row)
    {
        ValidateUnique(row, replacedSeq: 0);
        _db.ApplyWrite(new SetOp(this, key, row, hadOld: false, default, 0));
    }

    private void ReplaceCore(TKey key, TRow row, Entry old)
    {
        EnsureNotMutatedInPlace(row, old, nameof(UpdateInPlace));
        ValidateUnique(row, old.Seq);
        _db.ApplyWrite(new SetOp(this, key, row, hadOld: true, old.Row, old.Seq));
    }

    /// <summary>
    /// The primary key of a row about to be written. A null row or a null key is refused here, with
    /// the table and the key property named, rather than surfacing as a bare null-argument error from
    /// deep inside the dictionary.
    /// </summary>
    private TKey KeyOf(TRow row)
    {
        if (row is null)
        {
            throw new ArgumentNullException(nameof(row), $"Table '{Name}' cannot store a null row.");
        }
        var key = Descriptor.KeySelector(row);
        if (key is null)
        {
            var member = Descriptor.KeyMember is { } name ? $" ({name})" : "";
            throw new ArgumentException($"The row's primary key{member} is null; table '{Name}' cannot store a row without a key.", nameof(row));
        }
        return key;
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
    /// use <see cref="DeleteInPlace(TKey, TRow)"/> then.
    /// </summary>
    public bool Delete(TKey key)
    {
        lock (_db.SyncRoot)
        {
            if (!_rows.TryGetValue(key, out var old))
            {
                return false;
            }
            DeleteCore(key, old);
        }
        _db.DeliverChanges();
        return true;
    }

    /// <summary>
    /// Removes the row with <paramref name="key"/> only while the table still holds
    /// <paramref name="comparisonValue"/> for it; see <see cref="TryUpdate(TRow, TRow)"/> for how rows are
    /// compared. Returns false, removing nothing, when the row is missing or has been replaced.
    /// </summary>
    public bool TryDelete(TKey key, TRow comparisonValue)
    {
        lock (_db.SyncRoot)
        {
            if (!_rows.TryGetValue(key, out var old) || !EqualityComparer<TRow>.Default.Equals(old.Row, comparisonValue))
            {
                return false;
            }
            DeleteCore(key, old);
        }
        _db.DeliverChanges();
        return true;
    }

    private void DeleteCore(TKey key, Entry old)
    {
        EnsureNotMutatedInPlace(old.Row, old, nameof(DeleteInPlace));
        _db.ApplyWrite(new DeleteOp(this, key, old.Row, old.Seq));
    }

    // ---- Bulk writes ----
    //
    // Each is atomic: it either applies every row or, when one is refused part-way, none of them. With
    // no transaction open the batch commits as one transaction and one log record; inside an open
    // transaction it joins it behind a savepoint, so a failure takes back only the batch's own writes
    // and leaves the transaction as it was. A refused row raises the same exception its single-row
    // counterpart would.

    /// <summary>Inserts every row, atomically. See <see cref="Insert"/> for what each row must satisfy.</summary>
    public void InsertMany(IEnumerable<TRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _db.RunInTransaction(() =>
        {
            foreach (var row in rows)
            {
                Insert(row);
            }
        });
    }

    /// <summary>Inserts or replaces every row, atomically. Later rows win when two share a key.</summary>
    public void UpsertMany(IEnumerable<TRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _db.RunInTransaction(() =>
        {
            foreach (var row in rows)
            {
                Upsert(row);
            }
        });
    }

    /// <summary>
    /// Removes the rows with the given keys, atomically, and returns how many existed. Keys are read
    /// in full before anything is removed, so they may come straight from a query over this table -
    /// <c>DeleteMany(table.Range(index, from, to).Select(r =&gt; r.Id))</c> - without the deletes
    /// invalidating the enumeration that produces them.
    /// </summary>
    public int DeleteMany(IEnumerable<TKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var buffered = keys.ToArray();
        return _db.RunInTransaction(() =>
        {
            var deleted = 0;
            foreach (var key in buffered)
            {
                if (Delete(key))
                {
                    deleted++;
                }
            }
            return deleted;
        });
    }

    /// <summary>
    /// Removes every row <paramref name="predicate"/> accepts, atomically, and returns how many. The
    /// predicate sees every row - a scan - so prefer <see cref="DeleteMany"/> over an index query when
    /// an index can find the rows.
    /// </summary>
    public int DeleteWhere(Func<TRow, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        int deleted;
        lock (_db.SyncRoot)
        {
            var keys = new List<TKey>();
            foreach (var (key, entry) in _rows)
            {
                if (predicate(entry.Row))
                {
                    keys.Add(key);
                }
            }
            deleted = DeleteMany(keys);
        }
        _db.DeliverChanges();
        return deleted;
    }

    /// <summary>
    /// Removes every row, atomically, and returns how many there were. Each removal is journaled like
    /// any delete, so a database opened by an earlier version of the engine still replays it.
    /// </summary>
    public int DeleteAll()
    {
        int deleted;
        lock (_db.SyncRoot)
        {
            deleted = DeleteMany(_rows.Keys);
        }
        _db.DeliverChanges();
        return deleted;
    }

    // ---- Writes against rows that were mutated in place ----
    //
    // Callers normally hand the table a new row object, so the entry it still holds describes the
    // values its indexes were built from. A caller that instead mutates the stored instance -
    // which is what an EF Core change tracker does, since queries hand back the live row - leaves
    // the table unable to work out which index entries to retract. These methods take that
    // earlier state explicitly. Everything downstream (index maintenance, undo, WAL records) is
    // the ordinary path.

    /// <summary>
    /// Records an update to a row that was mutated in place. <paramref name="previousValues"/> must
    /// hold the values the row had when the table last indexed it; passing the mutated instance
    /// would leave stale index entries behind. When <paramref name="row"/> is not the instance the
    /// table holds - a detached object written back by key - and <paramref name="previousValues"/>
    /// do not match, the held instance's own values are used instead, provided it was not itself
    /// mutated in place.
    /// </summary>
    public void UpdateInPlace(TRow row, TRow previousValues)
    {
        _db.EnsureWritable();
        lock (_db.SyncRoot)
        {
            var key = KeyOf(row);
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
            var indexed = IndexedValues(row, previousValues, old);
            ValidateUnique(row, old.Seq);

            // Point the entry at the pre-mutation values so the write's index maintenance retracts
            // the entries that actually exist (and so a rollback restores them).
            _rows[key] = new Entry(indexed, old.Seq);
            _db.ApplyWrite(new SetOp(this, key, row, hadOld: true, indexed, old.Seq));
        }
        _db.DeliverChanges();
    }

    /// <summary>
    /// Removes a row that was mutated in place, using <paramref name="previousValues"/> to find the
    /// index entries to retract. Returns false if the key does not exist.
    /// </summary>
    public bool DeleteInPlace(TKey key, TRow previousValues) => DeleteInPlace(key, previousValues, instance: previousValues);

    /// <summary>
    /// <see cref="DeleteInPlace(TKey, TRow)"/> for a caller that also knows which object it is
    /// deleting - the EF Core provider, whose entity may be a detached one rather than the instance
    /// the table holds. See <see cref="UpdateInPlace"/> for what that changes.
    /// </summary>
    internal bool DeleteInPlace(TKey key, TRow previousValues, TRow instance)
    {
        _db.EnsureWritable();
        lock (_db.SyncRoot)
        {
            if (!_rows.TryGetValue(key, out var old))
            {
                return false;
            }
            var indexed = IndexedValues(instance, previousValues, old);
            _rows[key] = new Entry(indexed, old.Seq);
            _db.ApplyWrite(new DeleteOp(this, key, indexed, old.Seq));
        }
        _db.DeliverChanges();
        return true;
    }

    /// <summary>
    /// The values the held row's index entries were built from, for an in-place write to retract.
    /// <para>
    /// <paramref name="previousValues"/> normally are those values. When they are not, it depends on
    /// whose object is being written. If it is the instance the table holds, its indexed values have
    /// moved on since the caller's snapshot was taken - another change tracker sharing the row saved
    /// it in between - and the write is refused: retracting an entry that is not there leaves the
    /// row indexed under a value it no longer carries and adds a second entry under the value it
    /// does, so the same row comes back twice from one lookup. If it is a different object - written
    /// back by key, the way an EF Core context attaches a detached entity, whose "original values"
    /// are only its own current ones - the caller never knew what was indexed, and the held instance
    /// still carries exactly that unless it was itself mutated in place. Only then is the caller
    /// told its state is stale.
    /// </para>
    /// </summary>
    private TRow IndexedValues(TRow instance, TRow previousValues, Entry held)
    {
        var mismatch = FirstMismatchedIndex(previousValues, held.Seq);
        if (mismatch is null)
        {
            return previousValues;
        }
        if (!ReferenceEquals(instance, held.Row) && FirstMismatchedIndex(held.Row, held.Seq) is null)
        {
            return held.Row;
        }
        throw new BlazeDbStaleRowException(Name, mismatch);
    }

    /// <summary>The name of the first index whose entry for <paramref name="seq"/> the values do not describe, or null.</summary>
    private string? FirstMismatchedIndex(TRow values, long seq)
    {
        foreach (var store in _indexStores)
        {
            if (!store.EntryMatches(values, seq))
            {
                return ((BlazeDbIndexDefinition<TRow>)store.Definition).Name;
            }
        }
        return null;
    }

    // ---- Raw mutation primitives (index-maintaining; used by ops, replay and snapshot load) ----
    //
    // Each one either completes or leaves the table exactly as it found it. Index maintenance runs
    // code the table does not control - a selector or comparer handed to a hand-written index
    // definition, or a key type whose comparison throws - and a write that failed half-way through
    // would leave a row the indexes no longer agree on, with nothing above to notice. So a failing
    // store is unwound here: the stores already updated are put back, and the exception goes on to
    // the caller, which sees an ordinary refused write.

    private void SetEntry(TKey key, TRow row, long seq)
    {
        var hadOld = _rows.TryGetValue(key, out var old);
        var removed = 0;
        var added = 0;
        var replaced = false;
        try
        {
            if (hadOld)
            {
                for (; removed < _indexStores.Length; removed++)
                {
                    _indexStores[removed].Remove(old.Row, old.Seq);
                }
            }
            _rows[key] = new Entry(row, seq);
            replaced = true;
            for (; added < _indexStores.Length; added++)
            {
                _indexStores[added].Add(row, seq);
            }
        }
        catch
        {
            for (var i = added - 1; i >= 0; i--)
            {
                _indexStores[i].Remove(row, seq);
            }
            if (replaced)
            {
                if (hadOld)
                {
                    _rows[key] = old;
                }
                else
                {
                    _rows.Remove(key);
                }
            }
            for (var i = removed - 1; i >= 0; i--)
            {
                _indexStores[i].Add(old.Row, old.Seq);
            }
            throw;
        }
        Interlocked.Increment(ref _version);
    }

    private void RemoveEntry(TKey key)
    {
        if (!_rows.TryGetValue(key, out var old))
        {
            return;
        }
        var removed = 0;
        try
        {
            for (; removed < _indexStores.Length; removed++)
            {
                _indexStores[removed].Remove(old.Row, old.Seq);
            }
        }
        catch
        {
            for (var i = removed - 1; i >= 0; i--)
            {
                _indexStores[i].Add(old.Row, old.Seq);
            }
            throw;
        }
        _rows.Remove(key);
        Interlocked.Increment(ref _version);
    }

    /// <summary>
    /// Every way this table's rows and indexes disagree, for <see cref="BlazeDbDatabase.VerifyIntegrity"/>.
    /// A row whose key no longer matches the key it is filed under, or whose indexed values no longer
    /// match its index entries, was mutated in place after it was written; an index holding more or
    /// fewer entries than there are rows has stale or missing entries.
    /// </summary>
    void IBlazeDbTableInternal.VerifyIntegrity(List<BlazeDbIntegrityIssue> issues)
    {
        foreach (var (key, entry) in _rows)
        {
            var actualKey = Descriptor.KeySelector(entry.Row);
            if (!EqualityComparer<TKey>.Default.Equals(actualKey, key))
            {
                issues.Add(new BlazeDbIntegrityIssue(Name, null,
                    $"The row filed under key '{key}' now has key '{actualKey}': its key was changed in place."));
            }
            foreach (var store in _indexStores)
            {
                if (!store.EntryMatches(entry.Row, entry.Seq))
                {
                    var index = ((BlazeDbIndexDefinition<TRow>)store.Definition).Name;
                    issues.Add(new BlazeDbIntegrityIssue(Name, index,
                        $"The row with key '{key}' no longer carries the value index '{index}' filed it under: it was changed in place."));
                }
            }
        }
        foreach (var store in _indexStores)
        {
            if (store.Count != _rows.Count)
            {
                var index = ((BlazeDbIndexDefinition<TRow>)store.Definition).Name;
                issues.Add(new BlazeDbIntegrityIssue(Name, index,
                    $"Index '{index}' holds {store.Count} entries for {_rows.Count} rows."));
            }
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
        Interlocked.Increment(ref _version);
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

        public bool IsObserved => _table.IsObserved;

        public bool IsEngineTable => _table.IsEngineTable;

        public void CollectChange(BlazeDbChangeCollector collector) =>
            collector.For(_table).Set(_key, _hadOld, _oldRow, _row);

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

        public bool IsObserved => _table.IsObserved;

        public bool IsEngineTable => _table.IsEngineTable;

        public void CollectChange(BlazeDbChangeCollector collector) =>
            collector.For(_table).Delete(_key, _oldRow);

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
