namespace Bit.Blex;

/// <summary>
/// One memoized selector per argument -- the line with id 42, the total of one category, the page a list shows --
/// created on first use and evicted least recently used once more than <see cref="MaxSize"/> arguments are held.
/// Each value is computed at most once per state change, like <see cref="BlexSelector{T}"/>'s.
/// </summary>
/// <remarks>
/// <para>
/// A selector per argument created by hand leaks: every id ever shown keeps one alive (Jotai's <c>atomFamily</c> warns
/// about exactly this). The family bounds what it holds -- Reselect's <c>maxSize</c> -- and an evicted argument simply
/// computes again on its next read. <see cref="Remove"/> drops one at once (the row was deleted).
/// </para>
/// <para>
/// Like a selector, it subscribes to nothing: a value is stale the moment any registered store changes, it is
/// recomputed only when read, and the projection's exceptions propagate to the reader and are not cached. Not
/// synchronized, like the stores it reads -- use it on their dispatcher.
/// </para>
/// </remarks>
/// <typeparam name="TArg">The argument; compared with the family's key comparer.</typeparam>
/// <typeparam name="T">The derived value.</typeparam>
/// <example><code>
/// using var lineTotal = manager.CreateSelectorFamily((int id) => cart.Lines.First(l => l.Id == id).Total * tax.Rate);
/// var total = lineTotal[42];
/// </code></example>
public sealed class BlexSelectorFamily<TArg, T> : IDisposable
    where TArg : notnull
{
    // Never a version the manager reports: StateVersion starts at zero and only increases.
    private const long Stale = -1;

    private sealed class Member(TArg arg)
    {
        public TArg Arg { get; } = arg;

        public long Version { get; set; } = Stale;

        public T Value { get; set; } = default!;
    }

    private readonly BlexManager _manager;
    private readonly Func<TArg, T> _selector;
    private readonly Dictionary<TArg, LinkedListNode<Member>> _members;

    // Most recently read first; the last one is the next to go.
    private readonly LinkedList<Member> _recency = new();
    private bool _disposed;

    internal BlexSelectorFamily(BlexManager manager, Func<TArg, T> selector, int maxSize, IEqualityComparer<TArg>? comparer)
    {
        _manager = manager;
        _selector = selector;
        MaxSize = maxSize;
        _members = new Dictionary<TArg, LinkedListNode<Member>>(comparer);
    }

    /// <summary>How many arguments the family holds a value for at most; the least recently read goes first.</summary>
    public int MaxSize { get; }

    /// <summary>How many arguments the family holds a value for now.</summary>
    public int Count => _members.Count;

    /// <summary>The derived value for <paramref name="arg"/>; see <see cref="Get"/>.</summary>
    public T this[TArg arg] => Get(arg);

    /// <summary>
    /// The derived value for <paramref name="arg"/>, recomputed only when the state has moved since it was last read
    /// for that argument. A disposed family still answers, but memoizes nothing.
    /// </summary>
    public T Get(TArg arg)
    {
        ArgumentNullException.ThrowIfNull(arg);
        if (_disposed)
            return _selector(arg);

        // Read before computing: a projection that itself changes state leaves the memo stale.
        var version = _manager.StateVersion;
        if (_members.TryGetValue(arg, out var node))
        {
            Touch(node);
            if (node.Value.Version == version)
                return node.Value.Value;
        }

        var value = _selector(arg);

        // The projection may have read this family for other arguments -- or this one -- and evicted or added
        // members meanwhile: look again rather than trust the node found before.
        if (_disposed)
            return value;

        if (!_members.TryGetValue(arg, out node))
        {
            node = _recency.AddFirst(new Member(arg));
            _members[arg] = node;
            TrimToMaxSize();
        }

        node.Value.Value = value;
        node.Value.Version = version;
        return value;
    }

    /// <summary>Whether a read of <paramref name="arg"/> would return a memoized value rather than compute.</summary>
    public bool IsValid(TArg arg)
        => !_disposed && _members.TryGetValue(arg, out var node) && node.Value.Version == _manager.StateVersion;

    /// <summary>Drops the value held for <paramref name="arg"/>: the entity it derived from is gone. Returns whether there was one.</summary>
    public bool Remove(TArg arg)
    {
        if (!_members.Remove(arg, out var node))
            return false;

        _recency.Remove(node);
        return true;
    }

    /// <summary>
    /// Discards every memoized value so the next reads recompute, keeping the arguments: for a projection that depends
    /// on something outside the state tree (the clock, a service) that has moved on its own.
    /// </summary>
    public void Invalidate()
    {
        foreach (var member in _recency)
        {
            member.Version = Stale;
            member.Value = default!;
        }
    }

    /// <summary>Drops every value the family holds.</summary>
    public void Clear()
    {
        _members.Clear();
        _recency.Clear();
    }

    /// <summary>Stops memoizing and releases every value held. Idempotent.</summary>
    public void Dispose()
    {
        _disposed = true;
        Clear();
    }

    private void Touch(LinkedListNode<Member> node)
    {
        if (ReferenceEquals(_recency.First, node))
            return;

        _recency.Remove(node);
        _recency.AddFirst(node);
    }

    private void TrimToMaxSize()
    {
        while (_members.Count > MaxSize && _recency.Last is { } oldest)
        {
            _recency.RemoveLast();
            _members.Remove(oldest.Value.Arg);
        }
    }
}
