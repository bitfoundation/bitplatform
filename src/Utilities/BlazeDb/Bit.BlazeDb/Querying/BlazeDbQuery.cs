using System.Runtime.CompilerServices;

namespace Bit.BlazeDb.Querying;

/// <summary>
/// A composable query plan over a table: a source (full scan, hash-index lookup or ordered
/// range), residual predicates, ordering, paging and projection. This is the low-level query
/// primitive both application code and the EF Core provider target - LINQ operators
/// translate 1:1 onto it, and anything untranslatable can fall back to LINQ-to-objects
/// cheaply because rows are live objects in memory.
/// </summary>
public sealed class BlazeDbQuery<TKey, TRow>
    where TKey : notnull
{
    private readonly BlazeDbTable<TKey, TRow> _table;
    private Func<IEnumerable<TRow>>? _source;
    private bool _sourceOrdered;
    private List<Func<TRow, bool>>? _predicates;
    private Func<IEnumerable<TRow>, IOrderedEnumerable<TRow>>? _order;
    private int _skip;
    private int _take = -1;

    private BlazeDbQuery(BlazeDbTable<TKey, TRow> table) => _table = table;

    public static BlazeDbQuery<TKey, TRow> From(BlazeDbTable<TKey, TRow> table) => new(table);

    /// <summary>
    /// Sources at most one row: the one with primary key <paramref name="key"/>. This is the
    /// primary-key dictionary, so it is O(1) regardless of what else the query does - a
    /// <c>Where(row => row.Id == id)</c> should land here rather than on a scan.
    /// </summary>
    public BlazeDbQuery<TKey, TRow> UseKey(TKey key)
    {
        EnsureNoSource();
        _source = () => _table.TryGet(key, out var row) ? [row] : [];
        return this;
    }

    /// <summary>
    /// Sources the rows whose primary key is one of <paramref name="keys"/>, in the order the keys
    /// are given (duplicates ignored, misses skipped). One dictionary probe per key, so an
    /// <c>ids.Contains(row.Id)</c> stays proportional to the list rather than to the table.
    /// </summary>
    public BlazeDbQuery<TKey, TRow> UseKeys(IEnumerable<TKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        EnsureNoSource();
        _source = () => LookupKeys(keys);
        return this;
    }

    private IEnumerable<TRow> LookupKeys(IEnumerable<TKey> keys)
    {
        var seen = new HashSet<TKey>();
        foreach (var key in keys)
        {
            if (seen.Add(key) && _table.TryGet(key, out var row))
            {
                yield return row;
            }
        }
    }

    /// <summary>Sources rows from a hash-index equality lookup instead of a full scan.</summary>
    public BlazeDbQuery<TKey, TRow> UseIndex<TIndexKey>(BlazeDbHashIndexDefinition<TRow, TIndexKey> index, TIndexKey equals)
    {
        EnsureNoSource();
        _source = () => _table.Lookup(index, equals);
        return this;
    }

    /// <summary>Sources rows from an ordered-index range scan; rows arrive already sorted.</summary>
    public BlazeDbQuery<TKey, TRow> UseIndex<TIndexKey>(
        BlazeDbOrderedIndexDefinition<TRow, TIndexKey> index,
        BlazeDbBound<TIndexKey> from,
        BlazeDbBound<TIndexKey> to,
        bool descending = false)
    {
        EnsureNoSource();
        _source = () => _table.Range(index, from, to, descending);
        _sourceOrdered = true;
        return this;
    }

    /// <summary>
    /// Sources rows from an index picked at runtime, with the key boxed. The EF Core provider uses
    /// this after matching a LINQ predicate to an index, since it has no static knowledge of the
    /// index key type.
    /// </summary>
    internal BlazeDbQuery<TKey, TRow> UseIndexBoxed(BlazeDbIndexDefinition<TRow> index, object? key)
    {
        EnsureNoSource();
        _source = () => _table.LookupBoxed(index, key);
        return this;
    }

    /// <summary>
    /// Sources the union of the lookups for several boxed keys against one index. Rows have one
    /// indexed value each, so the lookups are disjoint once the keys are distinct - which the caller
    /// guarantees - and no row is produced twice. A null key stands for the rows whose indexed value
    /// is null.
    /// </summary>
    internal BlazeDbQuery<TKey, TRow> UseIndexBoxed(BlazeDbIndexDefinition<TRow> index, IReadOnlyList<object?> keys)
    {
        EnsureNoSource();
        var store = _table.GetIndexStore(index);
        _source = () => keys.SelectMany(store.LookupBoxed);
        return this;
    }

    /// <summary>Ordered-index range scan with boxed bounds. See <see cref="UseIndexBoxed(BlazeDbIndexDefinition{TRow}, object?)"/>.</summary>
    internal BlazeDbQuery<TKey, TRow> UseIndexBoxed(
        BlazeDbIndexDefinition<TRow> index, bool hasFrom, object? from, bool hasTo, object? to, bool descending)
    {
        EnsureNoSource();
        _source = () => _table.RangeBoxed(index, hasFrom, from, hasTo, to, descending);
        _sourceOrdered = true;
        return this;
    }

    /// <summary>Adds a residual filter (multiple calls are ANDed).</summary>
    public BlazeDbQuery<TKey, TRow> Where(Func<TRow, bool> predicate)
    {
        (_predicates ??= []).Add(predicate);
        return this;
    }

    /// <summary>
    /// Sorts results by a key. Unnecessary when an ordered index already provides the order -
    /// prefer the ordered-index source in that case.
    /// </summary>
    public BlazeDbQuery<TKey, TRow> OrderBy<TSortKey>(Func<TRow, TSortKey> keySelector, bool descending = false)
    {
        _order = descending
            ? rows => rows.OrderByDescending(keySelector)
            : rows => rows.OrderBy(keySelector);
        return this;
    }

    /// <summary>
    /// Sorts with a caller-supplied ordering step, for composite sorts (<c>ThenBy</c>) the single
    /// key-selector overload cannot express.
    /// </summary>
    public BlazeDbQuery<TKey, TRow> OrderBy(Func<IEnumerable<TRow>, IOrderedEnumerable<TRow>> order)
    {
        _order = order;
        return this;
    }

    public BlazeDbQuery<TKey, TRow> Skip(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _skip = count;
        return this;
    }

    public BlazeDbQuery<TKey, TRow> Take(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _take = count;
        return this;
    }

    /// <summary>True when results arrive in index order without a post-sort.</summary>
    public bool IsIndexOrdered => _sourceOrdered && _order is null;

    public IEnumerable<TRow> Execute()
    {
        var rows = _source?.Invoke() ?? _table.Scan();
        if (_predicates is not null)
        {
            foreach (var predicate in _predicates)
            {
                rows = rows.Where(predicate);
            }
        }
        if (_order is not null)
        {
            rows = _order(rows);
        }
        if (_skip > 0)
        {
            rows = rows.Skip(_skip);
        }
        if (_take >= 0)
        {
            rows = rows.Take(_take);
        }
        return rows;
    }

    public IEnumerable<TResult> Execute<TResult>(Func<TRow, TResult> projection) =>
        Execute().Select(projection);

    /// <summary>
    /// Streams results, handing control back to the host every <paramref name="batchSize"/> rows
    /// so a large scan cannot freeze the UI. On WebAssembly there is only one thread, so a
    /// synchronous scan of a big table blocks rendering and input until it finishes; yielding
    /// lets the browser paint between batches. The trade-off is that the table must not be
    /// written to while the stream is open, exactly as with a <c>foreach</c> over
    /// <see cref="Execute"/> - batches resume the same underlying enumerator.
    /// </summary>
    /// <param name="batchSize">Rows to emit between yields. Larger is faster, smaller is smoother.</param>
    /// <param name="cancellationToken">Checked once per row, between yields.</param>
    public async IAsyncEnumerable<TRow> ExecuteAsync(
        int batchSize = 512,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be positive.");
        }

        var sinceYield = 0;
        foreach (var row in Execute())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
            if (++sinceYield == batchSize)
            {
                sinceYield = 0;
                await Task.Yield();
            }
        }
    }

    /// <summary>Streams projected results. See <see cref="ExecuteAsync(int, CancellationToken)"/>.</summary>
    public async IAsyncEnumerable<TResult> ExecuteAsync<TResult>(
        Func<TRow, TResult> projection,
        int batchSize = 512,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var row in ExecuteAsync(batchSize, cancellationToken).ConfigureAwait(false))
        {
            yield return projection(row);
        }
    }

    /// <summary>Materializes the whole result set without blocking the host for its duration.</summary>
    public async ValueTask<List<TRow>> ToListAsync(
        int batchSize = 512,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TRow>();
        await foreach (var row in ExecuteAsync(batchSize, cancellationToken).ConfigureAwait(false))
        {
            results.Add(row);
        }
        return results;
    }

    public List<TRow> ToList() => Execute().ToList();

    public TRow? FirstOrDefault() => Execute().FirstOrDefault();

    public int Count() => Execute().Count();

    private void EnsureNoSource()
    {
        if (_source is not null)
        {
            throw new InvalidOperationException("A query can use at most one index source.");
        }
    }
}
