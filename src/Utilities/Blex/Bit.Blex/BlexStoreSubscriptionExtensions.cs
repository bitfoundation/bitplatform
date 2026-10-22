namespace Bit.Blex;

/// <summary>
/// Selector-based subscription helpers. Where <see cref="IBlexStore.StateChanged"/> fires on every
/// change, these fire a callback only when a projected value actually changes, so consumers
/// (e.g. components) can avoid re-rendering for unrelated state mutations.
/// </summary>
public static class BlexStoreSubscriptionExtensions
{
    /// <summary>
    /// Observes a projection of the store and invokes <paramref name="onChanged"/> only when the
    /// selected value differs from the previous one (per <paramref name="comparer"/> or the default
    /// equality comparer). Dispose the returned token to stop observing.
    /// </summary>
    /// <param name="store">The store to observe.</param>
    /// <param name="selector">Projects the observed value from the store.</param>
    /// <param name="onChanged">Invoked with the new value whenever it changes.</param>
    /// <param name="comparer">Custom equality; defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <param name="fireImmediately">When <c>true</c>, invokes the callback once with the current value on subscription (like MobX's <c>fireImmediately</c>).</param>
    public static IDisposable Subscribe<T>(
        this IBlexStore store,
        Func<T> selector,
        Action<T> onChanged,
        IEqualityComparer<T>? comparer = null,
        bool fireImmediately = false)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(onChanged);
        var subscription = new BlexSelectorSubscription<T>(store, selector, (_, current) => onChanged(current), comparer ?? EqualityComparer<T>.Default, selector());
        return fireImmediately ? subscription.FireImmediately() : subscription;
    }

    /// <summary>
    /// Observes a projection of the store and invokes <paramref name="onChanged"/> with the
    /// previous and new values whenever the selection changes -- handy for transition logic
    /// (analytics, animations, "changed from X to Y" logging).
    /// </summary>
    /// <param name="store">The store to observe.</param>
    /// <param name="selector">Projects the observed value from the store.</param>
    /// <param name="onChanged">Invoked with <c>(previous, current)</c> whenever the value changes.</param>
    /// <param name="comparer">Custom equality; defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <param name="fireImmediately">When <c>true</c>, invokes the callback once on subscription; previous and current are both the value at subscription time.</param>
    public static IDisposable Subscribe<T>(
        this IBlexStore store,
        Func<T> selector,
        Action<T, T> onChanged,
        IEqualityComparer<T>? comparer = null,
        bool fireImmediately = false)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(onChanged);
        var subscription = new BlexSelectorSubscription<T>(store, selector, onChanged, comparer ?? EqualityComparer<T>.Default, selector());
        return fireImmediately ? subscription.FireImmediately() : subscription;
    }

}

/// <summary>
/// Observes a projection of a store and calls back when the selected value changes -- the one
/// implementation behind <see cref="BlexStoreSubscriptionExtensions"/> and
/// <see cref="BlexObservableExtensions.Observe{T}(IBlexStore, Func{T}, IEqualityComparer{T})"/>.
/// </summary>
internal sealed class BlexSelectorSubscription<T> : IDisposable
{
    private readonly IBlexStore _store;
    private readonly Func<T> _selector;
    private readonly Action<T, T> _onChanged;
    private readonly IEqualityComparer<T> _comparer;
    private readonly Action<Exception>? _onSelectorError;
    private T _last;
    private bool _disposed;

    /// <param name="store">The store to observe.</param>
    /// <param name="selector">Projects the observed value from the store.</param>
    /// <param name="onChanged">Invoked with <c>(previous, current)</c> whenever the value changes.</param>
    /// <param name="comparer">Decides what counts as a change.</param>
    /// <param name="initial">The selected value at subscription time, which the first change is measured against.</param>
    /// <param name="onSelectorError">
    /// Receives a selector failure once the subscription has ended over it. Without one, the failure
    /// propagates to the store, which reports it and keeps the subscription for the next change.
    /// </param>
    public BlexSelectorSubscription(
        IBlexStore store,
        Func<T> selector,
        Action<T, T> onChanged,
        IEqualityComparer<T> comparer,
        T initial,
        Action<Exception>? onSelectorError = null)
    {
        _store = store;
        _selector = selector;
        _onChanged = onChanged;
        _comparer = comparer;
        _onSelectorError = onSelectorError;
        _last = initial;
        store.StateChanged += OnStoreChanged;
    }

    /// <summary>Invokes the callback once with the value at subscription time; previous and current are both that value.</summary>
    public BlexSelectorSubscription<T> FireImmediately()
    {
        try
        {
            _onChanged(_last, _last);
        }
        catch
        {
            // The caller never receives the token when this throws, so the handler would stay
            // attached to the store for the rest of its life -- a leak that also keeps firing a
            // callback its owner believes it never registered. Detach, then let the exception
            // surface at the call site where it belongs.
            Dispose();
            throw;
        }

        return this;
    }

    private void OnStoreChanged()
    {
        if (_disposed)
            return;

        T current;
        try
        {
            current = _selector();
        }
        catch (Exception ex) when (_onSelectorError is not null)
        {
            Dispose();
            _onSelectorError(ex);
            return;
        }

        if (_comparer.Equals(current, _last))
            return;

        var previous = _last;
        _last = current;
        _onChanged(previous, current);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _store.StateChanged -= OnStoreChanged;
    }
}
