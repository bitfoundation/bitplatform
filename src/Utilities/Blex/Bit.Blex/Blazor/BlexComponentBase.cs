using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bit.Blex.Query;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Blex.Blazor;

/// <summary>
/// Optional base component that re-renders automatically when subscribed stores change.
/// Call <see cref="Subscribe(ReadOnlySpan{IBlexStore})"/> in <c>OnInitialized</c>; unsubscription is automatic.
/// </summary>
public abstract class BlexComponentBase : ComponentBase, IDisposable
{
    private readonly List<IBlexStore> _subscriptions = [];
    private readonly List<IDisposable> _selectorSubscriptions = [];
    private readonly List<INotifyPropertyChanged> _watched = [];
    private Dictionary<(string File, int Line, BlexQueryKey Key), QuerySlot>? _queries;
    private Dictionary<(string File, int Line), QuerySite>? _querySites;
    private int _parameterPass;
    private bool _disposed;

    /// <summary>One observer <c>UseQuery</c> keeps, and the parameter pass that last asked for it.</summary>
    private sealed class QuerySlot(BlexQuery query, int pass)
    {
        public BlexQuery Query { get; } = query;

        public int Pass { get; set; } = pass;
    }

    /// <summary>A <c>UseQuery</c> call site: the pass that last called it, and what it returned in that pass and the one before.</summary>
    private sealed class QuerySite
    {
        public int Pass { get; set; } = -1;

        public int Calls { get; set; }

        public BlexQuery? Latest { get; set; }

        // The previous pass's calls: one call is a key following a parameter (the next page), whose data can stand
        // in while the new key loads; several are a loop over items, where the previous call is a neighbour's.
        public int PreviousCalls { get; set; }

        public BlexQuery? PreviousLatest { get; set; }
    }

    [Inject] private IServiceProvider BlexServices { get; set; } = default!;

    /// <summary>Subscribes to one or more stores; the component re-renders on any change.</summary>
    protected void Subscribe(params ReadOnlySpan<IBlexStore> stores)
    {
        // Subscribing after disposal would attach a handler nothing will ever detach.
        if (_disposed)
            return;

        foreach (var store in stores)
        {
            if (store is null || _subscriptions.Contains(store))
                continue;
            store.StateChanged += OnStoreChanged;
            _subscriptions.Add(store);
        }
    }

    /// <summary>
    /// Subscribes to a projection of a store; the component re-renders only when the selected
    /// value changes, ignoring unrelated mutations to the same store. Useful for stores with many
    /// independent <c>[BlexStateAttribute]</c> fields or large collections.
    /// </summary>
    /// <example><code>Subscribe(Store, () => Store.Count);</code></example>
    protected void Subscribe<T>(IBlexStore store, Func<T> selector, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selector);
        if (_disposed)
            return;

        _selectorSubscriptions.Add(store.Subscribe(selector, (Action<T>)(_ => OnStoreChanged()), comparer));
    }

    /// <summary>
    /// Re-renders the component whenever <paramref name="source"/> raises
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> -- a <see cref="BlexMutation{TArg, TResult}"/>, a
    /// <see cref="BlexQuery{T}"/> created by hand, any view model -- and stops when the component is disposed.
    /// Returns <paramref name="source"/>, so the call can initialize a field.
    /// </summary>
    /// <example><code>
    /// _save = Watch(Queries.Mutation&lt;Todo, Todo&gt;((todo, ct) => Api.SaveAsync(todo, ct)));
    /// </code></example>
    protected TSource Watch<TSource>(TSource source) where TSource : INotifyPropertyChanged
    {
        ArgumentNullException.ThrowIfNull(source);
        if (_disposed || _watched.Contains(source))
            return source;

        source.PropertyChanged += OnWatchedChanged;
        _watched.Add(source);
        return source;
    }

    /// <summary>
    /// Observes a cached query (<see cref="BlexQueryClient.Query{T}(BlexQueryKey, Func{CancellationToken, Task{T}}, BlexQueryOptions)"/>)
    /// for as long as the component asks for it, re-rendering it as the query changes. Call it from
    /// <c>OnParametersSet</c> (before any <c>await</c>) with the current key: a call site keeps an observer per
    /// key it asks for, reused on every later pass that asks for that key again. Once a parameter pass in which
    /// the call site ran is over, the keys it did not ask for that time are released -- so a key that follows a
    /// parameter is swapped, and one call in a loop over items keeps one observer per item.
    /// </summary>
    /// <param name="key">Which query: <c>["todos", Id]</c>.</param>
    /// <param name="fetch">Fetches the data; the latest closure is the one used.</param>
    /// <param name="options">How to fetch it.</param>
    /// <param name="callerFilePath">Identifies the call site. Supplied by the compiler.</param>
    /// <param name="callerLineNumber">Identifies the call site. Supplied by the compiler.</param>
    /// <exception cref="InvalidOperationException">No <see cref="BlexQueryClient"/> is registered (<c>AddBitBlexQueries()</c>).</exception>
    /// <example><code>
    /// private BlexQuery&lt;Todo&gt; _todo = default!;
    ///
    /// protected override void OnParametersSet()
    ///     => _todo = UseQuery(["todos", Id], ct => Api.GetTodoAsync(Id, ct));
    /// </code></example>
    protected BlexQuery<T> UseQuery<T>(
        BlexQueryKey key,
        Func<CancellationToken, Task<T>> fetch,
        BlexQueryOptions? options = null,
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(fetch);
        return Use<BlexQuery<T>>(
            key,
            callerFilePath,
            callerLineNumber,
            existing => existing.Update(fetch, options),
            (client, previousAtSite) =>
            {
                // A new key at this call site -- the next page, another id. Keep showing the previous key's data
                // meanwhile when asked to, if this site follows one key at a time.
                var previous = previousAtSite as BlexQuery<T>;
                var keep = previous is { HasData: true } && (options ?? previous.Options).KeepPreviousData;
                return client.Query(key, fetch, options, keep, keep ? previous!.Data : default);
            });
    }

    /// <summary>
    /// Observes a paged query (<see cref="BlexQueryClient.InfiniteQuery{TPage, TParam}"/>) for as long as the
    /// component asks for it, re-rendering it as pages arrive -- the call-site rules of <see cref="UseQuery{T}"/>.
    /// </summary>
    /// <param name="key">Which query: <c>["feed", Filter]</c>.</param>
    /// <param name="fetchPage">Fetches one page by its param; the latest closure is the one used.</param>
    /// <param name="initialPageParam">The first page's param.</param>
    /// <param name="getNextPageParam">The next page's param from the last page, or <c>null</c> when there is none.</param>
    /// <param name="options">How to fetch the pages.</param>
    /// <param name="callerFilePath">Identifies the call site. Supplied by the compiler.</param>
    /// <param name="callerLineNumber">Identifies the call site. Supplied by the compiler.</param>
    /// <exception cref="InvalidOperationException">No <see cref="BlexQueryClient"/> is registered (<c>AddBitBlexQueries()</c>).</exception>
    /// <example><code>
    /// private BlexInfiniteQuery&lt;FeedPage, string?&gt; _feed = default!;
    ///
    /// protected override void OnParametersSet()
    ///     => _feed = UseInfiniteQuery&lt;FeedPage, string?&gt;(["feed", Filter], (cursor, ct) => Api.GetFeedAsync(Filter, cursor, ct),
    ///         initialPageParam: null, getNextPageParam: (last, _) => last.NextCursor);
    /// </code></example>
    protected BlexInfiniteQuery<TPage, TParam> UseInfiniteQuery<TPage, TParam>(
        BlexQueryKey key,
        Func<TParam, CancellationToken, Task<TPage>> fetchPage,
        TParam initialPageParam,
        Func<TPage, IReadOnlyList<TPage>, TParam?> getNextPageParam,
        BlexQueryOptions? options = null,
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(fetchPage);
        ArgumentNullException.ThrowIfNull(getNextPageParam);
        return Use<BlexInfiniteQuery<TPage, TParam>>(
            key,
            callerFilePath,
            callerLineNumber,
            existing => existing.Update(fetchPage, getNextPageParam, options),
            (client, _) => client.InfiniteQuery(key, fetchPage, initialPageParam, getNextPageParam, options));
    }

    /// <summary>
    /// The call-site bookkeeping of <see cref="UseQuery{T}"/>: reuses the observer this call site keeps for
    /// <paramref name="key"/> (updating it), or creates one -- told what the site observed in its previous pass.
    /// </summary>
    private TQuery Use<TQuery>(
        BlexQueryKey key,
        string callerFilePath,
        int callerLineNumber,
        Action<TQuery> update,
        Func<BlexQueryClient, BlexQuery?, TQuery> create)
        where TQuery : BlexQuery
    {
        ArgumentNullException.ThrowIfNull(key);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var client = BlexServices.GetService<BlexQueryClient>()
            ?? throw new InvalidOperationException("UseQuery needs a BlexQueryClient: call services.AddBitBlexQueries() at startup.");

        _queries ??= [];
        _querySites ??= [];
        var siteKey = (callerFilePath, callerLineNumber);
        if (!_querySites.TryGetValue(siteKey, out var site))
            _querySites[siteKey] = site = new QuerySite();

        if (site.Pass != _parameterPass)
        {
            site.PreviousCalls = site.Calls;
            site.PreviousLatest = site.Latest;
            site.Calls = 0;
            site.Pass = _parameterPass;
        }

        site.Calls++;
        var slotKey = (callerFilePath, callerLineNumber, key);
        if (_queries.TryGetValue(slotKey, out var slot))
        {
            if (slot.Query is TQuery existing)
            {
                slot.Pass = _parameterPass;
                update(existing);
                site.Latest = existing;
                return existing;
            }

            // The same key asked for with another data type: the caller's code changed shape under hot reload.
            Release(slot.Query);
            _queries.Remove(slotKey);
        }

        // What the site observed in its previous pass counts only when it follows one key at a time.
        var previous = site.PreviousCalls == 1 && site.Calls == 1 ? site.PreviousLatest : null;
        var query = create(client, previous);
        query.Changed += OnStoreChanged;
        _queries[slotKey] = new QuerySlot(query, _parameterPass);
        site.Latest = query;
        return query;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Each call is a parameter pass for <see cref="UseQuery{T}"/>: once it is over, a call site that ran during
    /// it lets go of the keys it did not ask for.
    /// </remarks>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        var pass = ++_parameterPass;
        var task = base.SetParametersAsync(parameters);
        if (task.IsCompleted)
        {
            ReleaseUnusedQueries(pass);
            return task;
        }

        return ReleaseUnusedQueriesAfterAsync(task, pass);
    }

    private async Task ReleaseUnusedQueriesAfterAsync(Task task, int pass)
    {
        try
        {
            await task;
        }
        finally
        {
            ReleaseUnusedQueries(pass);
        }
    }

    private void ReleaseUnusedQueries(int pass)
    {
        if (_queries is null || _querySites is null || _disposed)
            return;

        List<(string, int, BlexQueryKey)>? unused = null;
        foreach (var (slotKey, slot) in _queries)
        {
            // Only call sites that ran in this pass, and only what they did not ask for in it. A site that did
            // not run (UseQuery in OnInitialized, or behind a condition) keeps what it has.
            if (slot.Pass != pass && _querySites.TryGetValue((slotKey.Item1, slotKey.Item2), out var site) && site.Pass == pass)
                (unused ??= []).Add(slotKey);
        }

        if (unused is null)
            return;

        foreach (var slotKey in unused)
        {
            Release(_queries[slotKey].Query);
            _queries.Remove(slotKey);
        }
    }

    private void Release(BlexQuery query)
    {
        query.Changed -= OnStoreChanged;
        query.Dispose();
    }

    /// <summary>
    /// Registers an arbitrary subscription token to be disposed with the component (e.g. a
    /// cross-store <c>manager.SubscribeTo&lt;T&gt;(...)</c> token or a selector subscription
    /// created manually).
    /// </summary>
    protected void OwnsSubscription(IDisposable subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        if (_disposed)
        {
            // Nothing left to own it; dispose immediately rather than leak it.
            subscription.Dispose();
            return;
        }

        _selectorSubscriptions.Add(subscription);
    }

    /// <summary>
    /// The shortest time between two renders that the changes this component follows cause -- its stores, selectors,
    /// queries and watched sources. Defaults to zero: every change renders. Set it for a component bound to state
    /// that changes many times a second (a live feed, a progress counter): the first change after a quiet interval
    /// still renders at once, and the rest within the interval collapse into one render of the latest state.
    /// Renders for any other reason (parameters, events) are not affected.
    /// </summary>
    /// <example><code>
    /// protected override void OnInitialized()
    /// {
    ///     RenderInterval = TimeSpan.FromMilliseconds(100);   // at most ten renders a second
    ///     Subscribe(Ticker);
    /// }
    /// </code></example>
    protected TimeSpan RenderInterval { get; set; }

    private BlexRenderThrottle? _throttle;
    private TimeProvider? _throttleClock;

    private void OnWatchedChanged(object? sender, PropertyChangedEventArgs e) => OnStoreChanged();

    private void OnStoreChanged()
    {
        if (_disposed)
            return;

        if (RenderInterval <= TimeSpan.Zero)
        {
            RequestRender();
            return;
        }

        // Changes arrive on any thread; one throttle per component, whichever change creates it. The clock is
        // looked up once: this is the path a store changing many times a second takes, and resolving the manager
        // from the container on every change would cost what the throttle is there to save. A race here writes
        // the same clock twice.
        var clock = _throttleClock ??= BlexServices.GetService<BlexManager>()?.TimeProvider ?? TimeProvider.System;
        LazyInitializer.EnsureInitialized(ref _throttle, () => new BlexRenderThrottle(RequestRender)).Request(RenderInterval, clock);
    }

    private void RequestRender() => BlexRender.Observe(InvokeAsync(StateHasChanged), DispatchExceptionAsync);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _throttle?.Dispose();
        foreach (var store in _subscriptions)
            store.StateChanged -= OnStoreChanged;
        _subscriptions.Clear();
        foreach (var sub in _selectorSubscriptions)
            sub.Dispose();
        _selectorSubscriptions.Clear();
        foreach (var source in _watched)
            source.PropertyChanged -= OnWatchedChanged;
        _watched.Clear();
        if (_queries is not null)
        {
            foreach (var slot in _queries.Values)
                Release(slot.Query);

            _queries.Clear();
            _querySites?.Clear();
        }

        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override to release additional resources when the component is disposed (the store
    /// subscriptions are already detached when this runs).
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
    }
}
