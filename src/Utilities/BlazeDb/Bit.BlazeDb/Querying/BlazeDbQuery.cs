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
    private int _orderKeys;
    private int _skip;
    private int _take = -1;

    // What Explain reports about the source. Names indexes and bound kinds, never values: a plan
    // description ends up in logs, and the values are the application's data.
    private string _sourceDescription = "full scan";

    private BlazeDbQuery(BlazeDbTable<TKey, TRow> table) => _table = table;

    public static BlazeDbQuery<TKey, TRow> From(BlazeDbTable<TKey, TRow> table)
    {
        ArgumentNullException.ThrowIfNull(table);
        return new(table);
    }

    /// <summary>
    /// Sources at most one row: the one with primary key <paramref name="key"/>. This is the
    /// primary-key dictionary, so it is O(1) regardless of what else the query does - a
    /// <c>Where(row => row.Id == id)</c> should land here rather than on a scan.
    /// </summary>
    public BlazeDbQuery<TKey, TRow> UseKey(TKey key)
    {
        EnsureNoSource();
        _source = () => _table.TryGet(key, out var row) ? [row] : [];
        _sourceDescription = "primary key lookup";
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
        _sourceDescription = "primary key lookups for a list of keys";
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
        _sourceDescription = $"hash index '{index.Name}' lookup";
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
        _sourceDescription = $"ordered index '{index.Name}' range {Describe(from, lower: true)}, {Describe(to, lower: false)}{(descending ? " descending" : "")}";
        return this;
    }

    /// <summary>
    /// Sources the rows whose indexed string starts with <paramref name="prefix"/>, in index order.
    /// See <see cref="BlazeDbTable{TKey, TRow}.StartsWith"/> for the rules.
    /// </summary>
    public BlazeDbQuery<TKey, TRow> UseIndexPrefix(BlazeDbOrderedIndexDefinition<TRow, string> index, string prefix, bool descending = false)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(prefix);
        EnsureNoSource();
        _source = () => _table.StartsWith(index, prefix, descending);
        _sourceOrdered = true;
        _sourceDescription = $"ordered index '{index.Name}' prefix scan{(descending ? " descending" : "")}";
        return this;
    }

    private static string Describe<T>(BlazeDbBound<T> bound, bool lower) =>
        !bound.HasValue ? (lower ? "(unbounded" : "unbounded)")
        : lower ? (bound.IsExclusive ? "(from" : "[from")
        : bound.IsExclusive ? "to)" : "to]";

    /// <summary>
    /// Sources rows from an index picked at runtime, with the key boxed. The EF Core provider uses
    /// this after matching a LINQ predicate to an index, since it has no static knowledge of the
    /// index key type.
    /// </summary>
    internal BlazeDbQuery<TKey, TRow> UseIndexBoxed(BlazeDbIndexDefinition<TRow> index, object? key)
    {
        EnsureNoSource();
        _source = () => _table.LookupBoxed(index, key);
        _sourceDescription = $"index '{index.Name}' lookup";
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
        _sourceDescription = $"index '{index.Name}' lookups for a list of values";
        return this;
    }

    /// <summary>Ordered-index range scan with boxed bounds. See <see cref="UseIndexBoxed(BlazeDbIndexDefinition{TRow}, object?)"/>.</summary>
    internal BlazeDbQuery<TKey, TRow> UseIndexBoxed(
        BlazeDbIndexDefinition<TRow> index, bool hasFrom, object? from, bool hasTo, object? to, bool descending)
    {
        EnsureNoSource();
        _source = () => _table.RangeBoxed(index, hasFrom, from, hasTo, to, descending);
        _sourceOrdered = true;
        _sourceDescription = $"ordered index '{index.Name}' range {(hasFrom ? "[from" : "(unbounded")}, {(hasTo ? "to]" : "unbounded)")}{(descending ? " descending" : "")}";
        return this;
    }

    /// <summary>Adds a residual filter (multiple calls are ANDed).</summary>
    public BlazeDbQuery<TKey, TRow> Where(Func<TRow, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        (_predicates ??= []).Add(predicate);
        return this;
    }

    /// <summary>
    /// Sorts results by a key. Unnecessary when an ordered index already provides the order -
    /// prefer the ordered-index source in that case.
    /// </summary>
    public BlazeDbQuery<TKey, TRow> OrderBy<TSortKey>(Func<TRow, TSortKey> keySelector, bool descending = false)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        _order = descending
            ? rows => rows.OrderByDescending(keySelector)
            : rows => rows.OrderBy(keySelector);
        _orderKeys = 1;
        return this;
    }

    /// <summary>
    /// Breaks ties left by the previous ordering with another key, as LINQ's <c>ThenBy</c> does. It
    /// needs an <see cref="OrderBy{TSortKey}(Func{TRow, TSortKey}, bool)"/> before it: an ordered-index
    /// source sorts by the index alone and gives this query nothing to refine.
    /// </summary>
    public BlazeDbQuery<TKey, TRow> ThenBy<TSortKey>(Func<TRow, TSortKey> keySelector, bool descending = false)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        var previous = _order ?? throw new InvalidOperationException("ThenBy refines an ordering; call OrderBy first.");
        _order = descending
            ? rows => previous(rows).ThenByDescending(keySelector)
            : rows => previous(rows).ThenBy(keySelector);
        _orderKeys++;
        return this;
    }

    /// <summary>
    /// Sorts with a caller-supplied ordering step, for sorts the key-selector overloads cannot
    /// express - a custom comparer, say.
    /// </summary>
    public BlazeDbQuery<TKey, TRow> OrderBy(Func<IEnumerable<TRow>, IOrderedEnumerable<TRow>> order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _order = order;
        _orderKeys = 1;
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

    /// <summary>
    /// Describes how the query will run - where its rows come from, what is filtered and sorted in
    /// memory, and the paging - one step per line, for diagnostics and for checking that a query uses
    /// the index meant for it. It names indexes but never the values a query compares against.
    /// <code>
    /// source: ordered index 'CreatedAt' range [from, unbounded) descending
    /// filter: 1 predicate, evaluated on each source row
    /// order: index order, no sort
    /// skip: 20
    /// take: 10
    /// </code>
    /// </summary>
    public string Explain()
    {
        var lines = new List<string> { $"source: {_sourceDescription}" };
        if (_predicates is { Count: > 0 })
        {
            lines.Add($"filter: {_predicates.Count} predicate{(_predicates.Count == 1 ? "" : "s")}, evaluated on each source row");
        }
        lines.Add(_order is not null
            ? $"order: sorted in memory by {_orderKeys} key{(_orderKeys == 1 ? "" : "s")}"
            : _sourceOrdered ? "order: index order, no sort" : "order: unspecified");
        if (_skip > 0)
        {
            lines.Add($"skip: {_skip}");
        }
        if (_take >= 0)
        {
            lines.Add($"take: {_take}");
        }
        return string.Join(Environment.NewLine, lines);
    }

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
    /// lets the browser paint between batches.
    /// <para>
    /// The batches resume one underlying enumerator, which a write to the table can invalidate. The
    /// consumer's own loop body may write - exactly what a <c>foreach</c> over <see cref="Execute"/>
    /// tolerates - but a write made by other code while the stream had yielded (another event handler)
    /// would leave the rest of the results describing neither the table as it was nor as it is, so the
    /// stream detects it and throws <see cref="InvalidOperationException"/> instead. Re-run the query,
    /// or materialize it first, when other code may write meanwhile.
    /// </para>
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
                var version = _table.Version;
                await Task.Yield();
                if (_table.Version != version)
                {
                    throw new InvalidOperationException(
                        $"Table '{_table.Name}' was written to while this streaming query had yielded to other work, " +
                        "so the rest of its results would no longer be consistent. Run the query again, or materialize " +
                        "it with ToList() when other code may write to the table meanwhile.");
                }
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
