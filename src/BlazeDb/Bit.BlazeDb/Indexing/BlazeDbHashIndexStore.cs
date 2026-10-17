namespace Bit.BlazeDb;

// TIndexKey is intentionally unconstrained so nullable properties can be indexed; a null value
// cannot be a dictionary key, so those rows go in a bucket of their own (see _nulls).
#pragma warning disable CS8714
internal sealed class BlazeDbHashIndexStore<TRow, TIndexKey> : IBlazeDbIndexStore<TRow>
{
    private readonly BlazeDbHashIndexDefinition<TRow, TIndexKey> _definition;
    private readonly Dictionary<TIndexKey, Bucket> _buckets;

    // Rows whose indexed value is null. Held apart from the dictionary rather than left out of the
    // index altogether: an entry that does not exist cannot be retracted, and a row whose value was
    // changed to null in place would strand the entry it had before - exactly what the table's
    // in-place guard exists to catch, and what it could not see while nulls went unrecorded.
    // Keeping them also lets an equality lookup for null answer from the index like any other value.
    private readonly Bucket _nulls = new();

    public BlazeDbHashIndexStore(BlazeDbHashIndexDefinition<TRow, TIndexKey> definition)
    {
        _definition = definition;
        _buckets = new Dictionary<TIndexKey, Bucket>(definition.Comparer);
    }

    public object Definition => _definition;

    public void Add(TRow row, long seq)
    {
        var key = _definition.Selector(row);
        if (key is null)
        {
            _nulls.Add(seq, row);
            return;
        }
        if (!_buckets.TryGetValue(key, out var bucket))
        {
            _buckets[key] = bucket = new Bucket();
        }
        bucket.Add(seq, row);
    }

    public void Remove(TRow row, long seq)
    {
        var key = _definition.Selector(row);
        if (key is null)
        {
            _nulls.Remove(seq);
            return;
        }
        if (_buckets.TryGetValue(key, out var bucket) && bucket.Remove(seq) && bucket.Count == 0)
        {
            _buckets.Remove(key);
        }
    }

    public void Clear()
    {
        _buckets.Clear();
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
        if (key is null || !_buckets.TryGetValue(key, out var bucket))
        {
            return;
        }
        if (bucket.HasEntryOtherThan(replacedSeq))
        {
            throw new BlazeDbUniqueConstraintViolationException(tableName, _definition.Name, key);
        }
    }

    public bool EntryMatches(TRow row, long seq)
    {
        var key = _definition.Selector(row);
        return key is null ? _nulls.Contains(seq) : _buckets.TryGetValue(key, out var bucket) && bucket.Contains(seq);
    }

    // Resolved when enumeration starts, so an enumerable obtained before a write sees the table as it
    // is when it is walked - the same deferred semantics as Scan and Range.
    public IEnumerable<TRow> Lookup(TIndexKey key) => LookupCore(key);

    private IEnumerable<TRow> LookupCore(TIndexKey key)
    {
        var bucket = BucketFor(key);
        if (bucket is null)
        {
            yield break;
        }
        foreach (var row in bucket.Rows())
        {
            yield return row;
        }
    }

    public int CountOf(TIndexKey key) => BucketFor(key)?.Count ?? 0;

    private Bucket? BucketFor(TIndexKey key) =>
        key is null ? _nulls : _buckets.TryGetValue(key, out var bucket) ? bucket : null;

    public IEnumerable<TRow> LookupBoxed(object? key) => Lookup((TIndexKey)key!);

    public IEnumerable<TRow> RangeBoxed(bool hasFrom, object? from, bool hasTo, object? to, bool descending) =>
        throw new NotSupportedException(
            $"Index '{_definition.Name}' is a hash index and answers equality only. " +
            "Declare it with [BlazeDbOrderedIndex] to scan ranges.");

    /// <summary>
    /// The rows sharing one indexed value, keyed by row sequence number. Most values - every value
    /// of a unique index, and most values of a selective one - map to a single row, so the bucket
    /// holds that one entry inline and only grows a dictionary once a second row arrives. That keeps
    /// a high-cardinality index at one small object per distinct value instead of one hash table
    /// per distinct value, which matters when the whole database lives in a browser tab's memory.
    /// </summary>
    private sealed class Bucket
    {
        private bool _hasInline;
        private long _seq;
        private TRow _row = default!;
        private Dictionary<long, TRow>? _many;

        public int Count => _many?.Count ?? (_hasInline ? 1 : 0);

        public void Add(long seq, TRow row)
        {
            if (_many is not null)
            {
                _many[seq] = row;
            }
            else if (!_hasInline || _seq == seq)
            {
                (_hasInline, _seq, _row) = (true, seq, row);
            }
            else
            {
                _many = new Dictionary<long, TRow> { [_seq] = _row, [seq] = row };
                (_hasInline, _row) = (false, default!);
            }
        }

        public bool Remove(long seq)
        {
            if (_many is not null)
            {
                return _many.Remove(seq);
            }
            if (!_hasInline || _seq != seq)
            {
                return false;
            }
            (_hasInline, _row) = (false, default!);
            return true;
        }

        public bool Contains(long seq) =>
            _many is not null ? _many.ContainsKey(seq) : _hasInline && _seq == seq;

        public void Clear()
        {
            _many = null;
            (_hasInline, _row) = (false, default!);
        }

        public bool HasEntryOtherThan(long seq)
        {
            if (_many is null)
            {
                return _hasInline && _seq != seq;
            }
            foreach (var other in _many.Keys)
            {
                if (other != seq)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The rows as of now; the caller (LookupCore) only asks once enumeration has begun.</summary>
        public IEnumerable<TRow> Rows() =>
            _many is not null ? _many.Values : _hasInline ? [_row] : [];
    }
}
#pragma warning restore CS8714
