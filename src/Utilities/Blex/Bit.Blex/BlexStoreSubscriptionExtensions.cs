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
