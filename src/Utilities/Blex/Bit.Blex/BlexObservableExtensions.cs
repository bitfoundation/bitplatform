namespace Bit.Blex;

/// <summary>
/// <see cref="IObservable{T}"/> views over stores and the manager, for code that composes change streams
/// with Reactive Extensions (<c>System.Reactive</c>, ReactiveUI) rather than callbacks. Blex itself
/// takes no dependency on Rx: these return the BCL interface, which every Rx operator accepts.
/// </summary>
/// <example><code>
/// // System.Reactive: search as the query settles, cancelling stale requests.
/// store.Observe(() => store.Query)
///      .Throttle(TimeSpan.FromMilliseconds(300))
///      .Subscribe(query => store.Search(query));
/// </code></example>
public static class BlexObservableExtensions
{
    /// <summary>
    /// Observes a projection of the store: the current value when an observer subscribes (a
    /// <c>BehaviorSubject</c>'s semantics), then every value that differs from the last one delivered.
    /// </summary>
    /// <param name="store">The store to observe.</param>
    /// <param name="selector">Projects the observed value from the store.</param>
    /// <param name="comparer">Decides what counts as a change; defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <remarks>
    /// The sequence never completes on its own -- dispose the subscription to stop observing. A
    /// selector that throws ends the subscription with <see cref="IObserver{T}.OnError"/>, the Rx
    /// contract for a failed source, instead of being reported and retried on the next change.
    /// </remarks>
    public static IObservable<T> Observe<T>(this IBlexStore store, Func<T> selector, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);
        return new StoreObservable<T>(store, selector, comparer ?? EqualityComparer<T>.Default);
    }

    /// <summary>
    /// Every action dispatched through the manager, as it is recorded -- the stream an NgRx
    /// <c>Actions</c> observable gives an effect. Filter it with a generated descriptor's
    /// <see cref="BlexActionDescriptor.Matches(BlexActionContext)"/>.
    /// </summary>
    /// <remarks>
    /// Observers run inside the dispatch pipeline, isolated like every other subscriber: one that
    /// throws is reported through <see cref="BlexManager.OnError"/> and stays subscribed.
    /// </remarks>
    public static IObservable<BlexActionContext> ObserveActions(this BlexManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        return new ActionObservable(manager);
    }

    private sealed class StoreObservable<T>(IBlexStore store, Func<T> selector, IEqualityComparer<T> comparer) : IObservable<T>
    {
        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);

            T initial;
            try
            {
                initial = selector();
            }
            catch (Exception ex)
            {
                // Rx sources report failures to the observer rather than throwing from Subscribe.
                observer.OnError(ex);
                return Subscription.Empty;
            }

            // A failing selector ends the sequence (OnError) rather than being reported and retried, and
            // an observer whose first OnNext throws is detached before the exception leaves Subscribe.
            return new BlexSelectorSubscription<T>(store, selector, (_, current) => observer.OnNext(current), comparer, initial, observer.OnError)
                .FireImmediately();
        }
    }

    private sealed class ActionObservable(BlexManager manager) : IObservable<BlexActionContext>
    {
        public IDisposable Subscribe(IObserver<BlexActionContext> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            return manager.Subscribe(observer.OnNext);
        }
    }

    private sealed class Subscription : IDisposable
    {
        public static readonly Subscription Empty = new();

        public void Dispose()
        {
        }
    }
}
