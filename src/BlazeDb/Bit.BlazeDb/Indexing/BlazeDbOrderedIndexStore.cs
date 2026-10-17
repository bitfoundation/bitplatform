namespace Bit.BlazeDb;

internal sealed class BlazeDbOrderedIndexStore<TRow, TIndexKey> : IBlazeDbIndexStore<TRow>
{
    // Entries are totally ordered by (indexed value, row sequence number); the sequence number
    // disambiguates duplicate index values without requiring the primary key to be comparable.
    internal readonly struct Entry
    {
        public Entry(TIndexKey key, long seq, TRow? row)
        {
            Key = key;
            Seq = seq;
            Row = row;
        }

        public TIndexKey Key { get; }

        public long Seq { get; }

        public TRow? Row { get; }
    }

    private sealed class EntryComparer : IComparer<Entry>
    {
        private readonly IComparer<TIndexKey> _keyComparer;

        public EntryComparer(IComparer<TIndexKey> keyComparer) => _keyComparer = keyComparer;

        public int Compare(Entry x, Entry y)
        {
            var byKey = _keyComparer.Compare(x.Key, y.Key);
            return byKey != 0 ? byKey : x.Seq.CompareTo(y.Seq);
        }
    }

    private readonly BlazeDbOrderedIndexDefinition<TRow, TIndexKey> _definition;
    private readonly SortedSet<Entry> _entries;

    // Rows whose indexed value is null, keyed by sequence number so they keep a stable order. They
    // cannot live in the sorted set - a comparer given a null key is free to throw - but they are
    // recorded rather than left out of the index altogether: an entry that does not exist cannot be
    // retracted, so a row whose value was changed to null in place would strand the entry it had
    // before, which is exactly what the table's in-place guard exists to catch. Walking the index in
    // order also has to see them, because sorting by a value that is sometimes null still returns
    // every row.
    private readonly SortedDictionary<long, TRow> _nulls = new();

    public BlazeDbOrderedIndexStore(BlazeDbOrderedIndexDefinition<TRow, TIndexKey> definition)
    {
        _definition = definition;
        _entries = new SortedSet<Entry>(new EntryComparer(definition.Comparer));
    }

    public object Definition => _definition;

    public void Add(TRow row, long seq)
    {
        var key = _definition.Selector(row);
        if (key is null)
        {
            _nulls[seq] = row;
            return;
        }
        _entries.Add(new Entry(key, seq, row));
    }

    public void Remove(TRow row, long seq)
    {
        var key = _definition.Selector(row);
        if (key is null)
        {
            _nulls.Remove(seq);
            return;
        }
        _entries.Remove(new Entry(key, seq, default));
    }

    public void Clear()
    {
        _entries.Clear();
        _nulls.Clear();
    }

    public void ValidateUnique(TRow row, long replacedSeq, string tableName)
    {
        if (!_definition.IsUnique)
        {
            return;
        }
        var key = _definition.Selector(row);
        // Nulls are recorded but never conflict, which is how a unique index behaves in SQL: several
        // rows may leave the value unset.
        if (key is null || _entries.Count == 0)
        {
            return;
        }
        // All entries sharing this key sort contiguously, bracketed by the sequence extremes.
        var span = _entries.GetViewBetween(
            new Entry(key, long.MinValue, default),
            new Entry(key, long.MaxValue, default));
        foreach (var entry in span)
        {
            if (entry.Seq != replacedSeq)
            {
                throw new BlazeDbUniqueConstraintViolationException(tableName, _definition.Name, key);
            }
        }
    }

    public bool EntryMatches(TRow row, long seq)
    {
        var key = _definition.Selector(row);
        return key is null ? _nulls.ContainsKey(seq) : _entries.Contains(new Entry(key, seq, default));
    }

    /// <summary>
    /// All rows in index order (ascending or descending). Rows whose indexed value is null sort
    /// before the rest, which is where <c>OrderBy</c> puts them, so walking the index answers an
    /// ordering exactly as sorting the rows would.
    /// </summary>
    public IEnumerable<TRow> All(bool descending)
    {
        if (!descending)
        {
            foreach (var row in _nulls.Values)
            {
                yield return row;
            }
        }
        foreach (var entry in descending ? _entries.Reverse() : _entries)
        {
            yield return entry.Row!;
        }
        if (descending)
        {
            foreach (var row in _nulls.Values.Reverse())
            {
                yield return row;
            }
        }
    }

    /// <summary>
    /// Rows whose indexed value lies in the inclusive range [from, to]. A null value is outside every
    /// bounded range, matching a comparison against null in LINQ, so those rows are not returned.
    /// </summary>
    public IEnumerable<TRow> Range(bool hasFrom, TIndexKey from, bool hasTo, TIndexKey to, bool descending)
    {
        if (_entries.Count == 0)
        {
            yield break;
        }

        var lower = hasFrom ? new Entry(from, long.MinValue, default) : _entries.Min;
        var upper = hasTo ? new Entry(to, long.MaxValue, default) : _entries.Max;
        if (_definition.Comparer.Compare(lower.Key, upper.Key) > 0)
        {
            yield break;
        }

        var view = _entries.GetViewBetween(lower, upper);
        var source = descending ? view.Reverse() : view;
        foreach (var entry in source)
        {
            yield return entry.Row!;
        }
    }

    public IEnumerable<TRow> LookupBoxed(object? key) =>
        // Nulls live outside the sorted set, so a range of [null, null] would find nothing; answer
        // from the bucket that holds them, the way the hash index does.
        key is null ? _nulls.Values : Range(true, (TIndexKey)key, true, (TIndexKey)key, descending: false);

    public IEnumerable<TRow> RangeBoxed(bool hasFrom, object? from, bool hasTo, object? to, bool descending) =>
        hasFrom || hasTo
            ? Range(hasFrom, hasFrom ? (TIndexKey)from! : default!, hasTo, hasTo ? (TIndexKey)to! : default!, descending)
            : All(descending);
}
