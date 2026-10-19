namespace Bit.Blex;

/// <summary>
/// A memoized derived value over the whole application state -- what
/// <c>[BlexComputedAttribute]</c> is to a single store, lifted to the manager. Recomputes at most
/// once per action (or restore), however many readers ask for it in between.
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
/// Dispose it to detach from the manager. Selectors compose: one may read another, and each still
/// computes at most once per action.
/// </para>
/// </remarks>
/// <example><code>
/// using var overLimit = manager.CreateSelector(() => cart.Total > profile.CreditLimit);
/// if (overLimit.Value) ShowBanner();
/// </code></example>
public sealed class BlexSelector<T> : IDisposable
{
    private readonly BlexManager _manager;
    private readonly Func<T> _selector;
    private readonly IDisposable _actions;
    private T _value = default!;
    private bool _valid;
    private bool _disposed;

    internal BlexSelector(BlexManager manager, Func<T> selector)
    {
        _manager = manager;
        _selector = selector;

        // Invalidation only; the value is recomputed on the next read, so an action nobody's
        // readers care about costs a flag write.
        _actions = manager.Subscribe(_ => Invalidate());
        manager.StateRestored += Invalidate;
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
    /// Reading a disposed selector still works, but memoization stops: without a subscription there
    /// is nothing left to invalidate the cache, and a value frozen at the moment of disposal is the
    /// one failure mode a memo must not have. It degrades to a plain call of the projection.
    /// </para>
    /// </remarks>
    public T Value
    {
        get
        {
            if (_valid && !_disposed)
                return _value;

            var value = _selector();
            if (_disposed)
                return value;

            _value = value;
            _valid = true;
            return _value;
        }
    }

    /// <summary>Whether the memoized value is current (i.e. a read would not recompute).</summary>
    public bool IsValid => _valid;

    /// <summary>
    /// Discards the memoized value so the next read recomputes. Call it when the projection depends
    /// on something outside the state tree (the clock, a service) that has moved on its own.
    /// </summary>
    public void Invalidate()
    {
        _valid = false;
        _value = default!;
    }

    /// <summary>Detaches from the manager. Idempotent.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _manager.StateRestored -= Invalidate;
        _actions.Dispose();
        Invalidate();
    }
}
