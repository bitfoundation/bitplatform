namespace Bit.Blex;

/// <summary>
/// A memoized derived value over the whole application state -- what
/// <c>[BlexComputedAttribute]</c> is to a single store, lifted to the manager. Recomputes at most
/// once per state change, however many readers ask for it in between.
/// </summary>
/// <remarks>
/// <para>
/// Reach for it when a derived value spans stores <em>and</em> is read from more than one place.
/// <see cref="BlexManager.Subscribe{T}(Func{T}, Action{T}, IEqualityComparer{T}, bool)"/> answers a
/// different question - it tells one observer when a projection changed - and it evaluates the
/// projection eagerly after every action whether anyone reads it or not. This one is lazy and
/// shared: three components binding to the same cross-store total pay for one computation per
/// action rather than three, and an action nobody's readers care about costs nothing at all.
/// </para>
/// <para>
/// It is stale the moment any registered store changes -- an action, a restore, an effect's
/// <c>IsLoading</c>/<c>Error</c>, a mutation an async action makes while still running -- and that is
/// known before the store's subscribers run, so a component re-rendering for the change never reads
/// the value from before it. Selectors compose: one may read another, and each still computes at most
/// once per change. Dispose it when done; it subscribes to nothing, so disposal only drops the memo.
/// </para>
/// </remarks>
/// <example><code>
/// using var overLimit = manager.CreateSelector(() => cart.Total > profile.CreditLimit);
/// if (overLimit.Value) ShowBanner();
/// </code></example>
public sealed class BlexSelector<T> : IDisposable
{
    // Never a version the manager reports: StateVersion starts at zero and only increases.
    private const long Stale = -1;

    private readonly BlexManager _manager;
    private readonly Func<T> _selector;
    private T _value = default!;
    private long _version = Stale;
    private bool _disposed;

    internal BlexSelector(BlexManager manager, Func<T> selector)
    {
        _manager = manager;
        _selector = selector;
    }

    /// <summary>
    /// The derived value, recomputed only when the state has moved since it was last read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The selector runs on the calling thread and its exceptions propagate to the caller -- unlike
    /// a subscription callback, this is a value you asked for, and silently handing back a stale one
    /// would be worse than the throw. A failed computation is not cached, so a later read retries.
    /// </para>
    /// <para>
    /// Reading a disposed selector still works, but memoization stops: it degrades to a plain call of
    /// the projection, and holds on to none of its results.
    /// </para>
    /// </remarks>
    public T Value
    {
        get
        {
            // Read before computing: a projection that itself changes state leaves the memo stale.
            var version = _manager.StateVersion;
            if (_version == version && !_disposed)
                return _value;

            var value = _selector();
            if (_disposed)
                return value;

            _value = value;
            _version = version;
            return _value;
        }
    }

    /// <summary>Whether the memoized value is current (i.e. a read would not recompute).</summary>
    public bool IsValid => !_disposed && _version == _manager.StateVersion;

    /// <summary>
    /// Discards the memoized value so the next read recomputes. Call it when the projection depends
    /// on something outside the state tree (the clock, a service) that has moved on its own.
    /// </summary>
    public void Invalidate()
    {
        _version = Stale;
        _value = default!;
    }

    /// <summary>Stops memoizing and releases the memoized value. Idempotent.</summary>
    public void Dispose()
    {
        _disposed = true;
        Invalidate();
    }
}
