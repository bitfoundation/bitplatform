using System.ComponentModel;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text.Json.Nodes;

namespace Bit.Blex.Query;

/// <summary>A cached query as <see cref="BlexQueryClient.GetQueries"/> reports it, for inspection and diagnostics.</summary>
/// <param name="Key">The query's key.</param>
/// <param name="DataType">The type of its data.</param>
/// <param name="Status">Whether it has data, an error, or neither.</param>
/// <param name="FetchStatus">Whether a fetch is running.</param>
/// <param name="ObserverCount">How many observers watch it; with none, it is removed once its GC time passes.</param>
/// <param name="DataUpdatedAt">When its data was fetched or set.</param>
/// <param name="IsInvalidated">Whether it was invalidated (or failed) since.</param>
/// <param name="Error">The last fetch's error, if it failed.</param>
public sealed record BlexQueryInfo(
    BlexQueryKey Key,
    Type DataType,
    BlexQueryStatus Status,
    BlexFetchStatus FetchStatus,
    int ObserverCount,
    DateTimeOffset? DataUpdatedAt,
    bool IsInvalidated,
    Exception? Error);

/// <summary>
/// A cache of server state -- data the app shows but does not own: fetched, kept while it is fresh,
/// shared by every component that asks for the same key, refetched in the background when it goes
/// stale, invalidated after a change, and dropped once nobody has looked at it for a while.
/// </summary>
/// <remarks>
/// <para>
/// Kept apart from stores on purpose. Stores hold state the app owns -- what the user typed, selected or
/// toggled -- and that is what undo, persistence and DevTools time travel are about. A server's answer is
/// a copy with a shelf life: undoing it, persisting it or replaying it would only bring back a stale copy.
/// Effects in a store can still read through the cache (<see cref="FetchAsync{T}"/>).
/// </para>
/// <para>
/// One fetch runs per key at a time: observers mounting together share it. A refetch or an invalidation
/// cancels a running fetch and starts over, and only the latest fetch's outcome is committed, so a slow
/// response can never overwrite a newer one. Fetches resume on the context they started on (in Blazor, the
/// renderer's dispatcher); timers -- garbage collection, polling -- go through the manager's
/// <see cref="BlexManager.Dispatcher"/>.
/// </para>
/// </remarks>
public sealed class BlexQueryClient : IDisposable
{
    private readonly Dictionary<BlexQueryKey, BlexQueryEntry> _entries = [];
    private readonly Dictionary<string, (JsonNode? Data, DateTimeOffset UpdatedAt)> _hydration = new(StringComparer.Ordinal);
    private readonly BlexManager? _manager;

    /// <summary>Creates a client.</summary>
    /// <param name="manager">
    /// The manager whose <see cref="BlexManager.TimeProvider"/> the client keeps time by, whose
    /// <see cref="BlexManager.Dispatcher"/> its timers run on and whose <see cref="BlexManager.OnError"/>
    /// receives failures it isolates. Optional: without it, the system clock and stderr are used.
    /// </param>
    /// <param name="options">The defaults every query starts from.</param>
    public BlexQueryClient(BlexManager? manager = null, BlexQueryClientOptions? options = null)
    {
        _manager = manager;

        // A copy: AddBitBlexQueries hands every scope the same options object, and one circuit adjusting its
        // client's defaults must not change them for every other user.
        options ??= new BlexQueryClientOptions();
        Options = new BlexQueryClientOptions
        {
            StaleTime = options.StaleTime,
            GcTime = options.GcTime,
            RetryCount = options.RetryCount,
            RetryDelay = options.RetryDelay,
            NetworkMode = options.NetworkMode,
            SyncAcrossTabs = options.SyncAcrossTabs,
        };
    }

    /// <summary>The defaults every query of this client starts from (a copy of the options it was created with).</summary>
    public BlexQueryClientOptions Options { get; }

    // Drops handed-over queries nobody asked for; disposed with the client.
    private ITimer? _hydrationTimer;
    private List<string>? _hydrationKeys;

    internal Lock Gate { get; } = new();

    internal bool IsDisposed { get; private set; }

    internal TimeProvider TimeProvider => _manager?.TimeProvider ?? TimeProvider.System;

    internal TimeSpan StaleTimeFor(BlexQueryOptions options) => options.StaleTime ?? Options.StaleTime;

    internal TimeSpan GcTimeFor(BlexQueryOptions options) => options.GcTime ?? Options.GcTime;

    internal int RetryCountFor(BlexQueryOptions options) => options.RetryCount ?? Options.RetryCount;

    internal TimeSpan RetryDelayFor(BlexQueryOptions options) => options.RetryDelay ?? Options.RetryDelay;

    internal bool PausesOffline(BlexNetworkMode? mode) => (mode ?? Options.NetworkMode) == BlexNetworkMode.Online;

    // Completed by NotifyReconnected; null while online.
    private TaskCompletionSource? _backOnline;

    /// <summary>
    /// Whether the app has the network, as far as the cache was told: <c>true</c> until <see cref="NotifyOffline"/>,
    /// and again after <see cref="NotifyReconnected"/>. While it is <c>false</c>, fetches and mutations in
    /// <see cref="BlexNetworkMode.Online"/> mode wait -- <see cref="BlexFetchStatus.Paused"/> -- instead of failing.
    /// <c>&lt;BlexProvider&gt;</c> follows the browser's <c>offline</c>/<c>online</c> events where requests leave from
    /// the device (WebAssembly, Hybrid); on Blazor Server they leave from the server, which the browser's network
    /// says nothing about.
    /// </summary>
    public bool IsOnline { get; private set; } = true;

    /// <summary>
    /// Tells the cache the network is gone: fetches and mutations in <see cref="BlexNetworkMode.Online"/> mode wait
    /// until <see cref="NotifyReconnected"/>. Native MAUI calls it from <c>Connectivity.ConnectivityChanged</c>.
    /// </summary>
    public void NotifyOffline()
    {
        TaskCompletionSource? wentOffline;
        lock (Gate)
        {
            if (!IsOnline)
                return;

            IsOnline = false;
            wentOffline = _wentOffline;
            _wentOffline = null;
        }

        wentOffline?.TrySetResult();
        OnEntryChanged();
    }

    // Completed by NotifyOffline; null while offline.
    private TaskCompletionSource? _wentOffline;

    /// <summary>Completes once the app is offline -- at once when it is. What waits for refetches stops waiting there.</summary>
    internal Task WhenOfflineAsync()
    {
        lock (Gate)
        {
            if (!IsOnline)
                return Task.CompletedTask;

            _wentOffline ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return _wentOffline.Task;
        }
    }

    /// <summary>Completes once the app is online -- at once when it is; cancelled with <paramref name="cancellationToken"/>.</summary>
    internal Task WhenOnlineAsync(CancellationToken cancellationToken)
    {
        lock (Gate)
        {
            if (IsOnline)
                return Task.CompletedTask;

            _backOnline ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return _backOnline.Task.WaitAsync(cancellationToken);
        }
    }

    /// <summary>Raised after any cached query changes, or joins or leaves the cache -- for inspectors. Handler exceptions are isolated.</summary>
    public event Action? CacheChanged;

    /// <summary>
    /// Observes the query <paramref name="key"/> names: serves what is cached at once, and fetches when there
    /// is nothing cached or it is stale (unless <see cref="BlexQueryOptions.Enabled"/> is off). Dispose the
    /// observer when done; in a component, <c>UseQuery</c> does both for you.
    /// </summary>
    /// <param name="key">Which query: <c>["todos", id]</c>.</param>
    /// <param name="fetch">Fetches the data; honour the token, which is cancelled when the fetch is superseded or cancelled.</param>
    /// <param name="options">How this observer wants it fetched; unset values come from <see cref="Options"/>.</param>
    /// <exception cref="InvalidOperationException">The key is already cached with data of another type.</exception>
    public BlexQuery<T> Query<T>(BlexQueryKey key, Func<CancellationToken, Task<T>> fetch, BlexQueryOptions? options = null)
        => Query(key, fetch, options, hasPlaceholder: false, placeholder: default);

    /// <summary>
    /// Observes a paged query -- a feed, "load more", infinite scroll: the pages loaded so far, and
    /// <see cref="BlexInfiniteQuery{TPage, TParam}.FetchNextPageAsync"/> to load the next (TanStack Query's
    /// <c>useInfiniteQuery</c>, forward only). The pages are one cached query under <paramref name="key"/>. Dispose
    /// the observer when done; in a component, <c>UseInfiniteQuery</c> does both for you.
    /// </summary>
    /// <param name="key">Which query: <c>["feed", filter]</c>.</param>
    /// <param name="fetchPage">Fetches one page by its param; honour the token.</param>
    /// <param name="initialPageParam">The first page's param: <c>1</c>, an empty cursor.</param>
    /// <param name="getNextPageParam">
    /// The next page's param from the last page (and all of them), or <c>null</c> when there is none. Called on every
    /// read of <see cref="BlexInfiniteQuery{TPage, TParam}.HasNextPage"/>; keep it cheap.
    /// </param>
    /// <param name="options">How to fetch the pages; unset values come from <see cref="Options"/>.</param>
    /// <exception cref="InvalidOperationException">The key is already cached with data of another type.</exception>
    /// <example><code>
    /// var feed = client.InfiniteQuery&lt;FeedPage, string?&gt;(
    ///     ["feed"],
    ///     (cursor, ct) => api.GetFeedAsync(cursor, ct),
    ///     initialPageParam: null,
    ///     getNextPageParam: (last, _) => last.NextCursor);
    /// await feed.FetchNextPageAsync();
    /// </code></example>
    public BlexInfiniteQuery<TPage, TParam> InfiniteQuery<TPage, TParam>(
        BlexQueryKey key,
        Func<TParam, CancellationToken, Task<TPage>> fetchPage,
        TParam initialPageParam,
        Func<TPage, IReadOnlyList<TPage>, TParam?> getNextPageParam,
        BlexQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(fetchPage);
        ArgumentNullException.ThrowIfNull(getNextPageParam);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        options ??= new BlexQueryOptions();

        var infinite = new BlexInfiniteQuery<TPage, TParam>(this, options, fetchPage, initialPageParam, getNextPageParam);
        var pages = new BlexQuery<BlexInfiniteData<TPage, TParam>>(this, infinite.FetchPagesAsync, options, hasPlaceholder: false, placeholder: null);
        Attach(key, pages);

        // Before mounting: the first fetch reads the pages loaded so far through it.
        infinite.Observe(pages);
        pages.Mount();
        OnEntryChanged();
        return infinite;
    }

    internal BlexQuery<T> Query<T>(BlexQueryKey key, Func<CancellationToken, Task<T>> fetch, BlexQueryOptions? options, bool hasPlaceholder, T? placeholder)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(fetch);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        options ??= new BlexQueryOptions();

        var observer = new BlexQuery<T>(this, fetch, options, hasPlaceholder, placeholder);
        Attach(key, observer);
        observer.Mount();
        OnEntryChanged();
        return observer;
    }

    /// <summary>
    /// Attaches <paramref name="observer"/> to the cached entry for <paramref name="key"/>, creating it when
    /// there is none, and cancels the entry's pending removal.
    /// </summary>
    internal void Attach<T>(BlexQueryKey key, BlexQuery<T> observer)
    {
        while (true)
        {
            var entry = GetOrCreate<T>(key);
            lock (Gate)
            {
                // Collected between being looked up and being observed (a GC timer running off the dispatcher):
                // an observer of an entry that left the cache would never hear of an invalidation again.
                if (!_entries.TryGetValue(key, out var current) || !ReferenceEquals(current, entry))
                    continue;

                entry.Attach(observer);
                observer.Entry = entry;
                entry.GcTime = Max(entry.GcTime, GcTimeFor(observer.Options));
                CancelGc(entry);
                return;
            }
        }
    }

    /// <summary>Whether <paramref name="entry"/> is still the one cached for its key.</summary>
    internal bool IsCached(BlexQueryEntry entry)
    {
        lock (Gate)
            return _entries.TryGetValue(entry.Key, out var current) && ReferenceEquals(current, entry);
    }

    /// <summary>Cancels an entry's pending removal; the caller holds the gate.</summary>
    private static void CancelGc(BlexQueryEntry entry)
    {
        entry.GcTimer?.Dispose();
        entry.GcTimer = null;
        entry.GcTicket = null;
    }

    /// <summary>
    /// Returns the cached data for <paramref name="key"/> while it is fresh, and fetches it otherwise -- the
    /// read-through an effect or a service uses, sharing the cache (and any fetch already running) with the
    /// components observing the same key.
    /// </summary>
    /// <exception cref="Exception">The fetch failed: its exception, after the retries <paramref name="options"/> allow.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled, or the fetch was.</exception>
    public async Task<T> FetchAsync<T>(
        BlexQueryKey key,
        Func<CancellationToken, Task<T>> fetch,
        BlexQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(fetch);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        options ??= new BlexQueryOptions();

        var entry = GetOrCreate<T>(key);
        lock (Gate)
            entry.GcTime = Max(entry.GcTime, GcTimeFor(options));

        if (!entry.IsStale(StaleTimeFor(options), TimeProvider.GetUtcNow()) && entry.TryGetData(out var cached, out _))
        {
            // Looking it up cancelled its pending removal; an unobserved entry must get one back.
            ScheduleGc(entry);
            return cached;
        }

        BlexQueryEntry.FetchResult result;
        try
        {
            result = await entry.FetchAsync(fetch, RetryCountFor(options), RetryDelayFor(options), cancelRunning: false, networkMode: options.NetworkMode)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(true);
        }
        finally
        {
            ScheduleGc(entry);
        }

        // By how the fetch ended, not by the entry's status: a cancelled fetch (a Clear on sign-out) leaves the data
        // from before in place, which is not this caller's answer.
        if (result.Succeeded && entry.TryGetData(out var data, out _))
            return data;

        if (result.Error is { } error)
            ExceptionDispatchInfo.Throw(error);

        throw new OperationCanceledException($"The fetch of {key} was cancelled.");
    }

    /// <summary>
    /// Fetches <paramref name="key"/> into the cache when it is missing or stale, so a component about to
    /// need it finds it there -- on hover, before navigating. Never throws; a failure lands in the cache.
    /// </summary>
    public Task PrefetchAsync<T>(BlexQueryKey key, Func<CancellationToken, Task<T>> fetch, BlexQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(fetch);
        if (IsDisposed)
            return Task.CompletedTask;

        options ??= new BlexQueryOptions();
        var entry = GetOrCreate<T>(key);
        lock (Gate)
            entry.GcTime = Max(entry.GcTime, GcTimeFor(options));

        if (!entry.IsStale(StaleTimeFor(options), TimeProvider.GetUtcNow()))
        {
            ScheduleGc(entry);
            return Task.CompletedTask;
        }

        return entry.FetchAsync(fetch, RetryCountFor(options), RetryDelayFor(options), cancelRunning: false, networkMode: options.NetworkMode)
            .ContinueWith(_ => ScheduleGc(entry), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>Returns the cached data for <paramref name="key"/>, or <c>default</c> when there is none.</summary>
    public T? GetQueryData<T>(BlexQueryKey key)
        => TryGetQueryData<T>(key, out var data) ? data : default;

    /// <summary>Reads the cached data for <paramref name="key"/>, telling apart "none" from a cached <c>default</c>.</summary>
    public bool TryGetQueryData<T>(BlexQueryKey key, out T? data)
    {
        ArgumentNullException.ThrowIfNull(key);
        BlexQueryEntry? entry;
        lock (Gate)
            _entries.TryGetValue(key, out entry);

        if (entry is null)
        {
            data = default;
            return false;
        }

        return Typed<T>(entry).TryGetData(out data, out _);
    }

    /// <summary>
    /// Writes data into the cache as if it had just been fetched -- the response of a mutation that returns
    /// the updated entity, say -- and tells every observer. A fetch already running still lands afterwards.
    /// </summary>
    public void SetQueryData<T>(BlexQueryKey key, T data)
    {
        ArgumentNullException.ThrowIfNull(key);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var entry = GetOrCreate<T>(key);
        entry.SetData(data);
        ScheduleGc(entry);
    }

    /// <summary>
    /// Writes <paramref name="update"/>'s result into the cache; it receives the data cached now, or
    /// <c>default</c> when there is none.
    /// </summary>
    public void SetQueryData<T>(BlexQueryKey key, Func<T?, T> update)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(update);
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var entry = GetOrCreate<T>(key);
        try
        {
            entry.SetData(update(entry.TryGetData(out var current, out _) ? current : default));
        }
        finally
        {
            // Even when the update throws: looking the entry up cancelled its pending removal.
            ScheduleGc(entry);
        }
    }

    /// <summary>
    /// Applies <paramref name="update"/> to the cached data for <paramref name="key"/> ahead of the server
    /// confirming it, after cancelling a fetch of that key that could otherwise overwrite it, and returns the
    /// handle that undoes it. A mutation whose <see cref="BlexMutationOptions{TArg, TResult}.OnMutate"/>
    /// returns it rolls back automatically when it fails.
    /// </summary>
    /// <remarks>
    /// Rolling back restores the previous data only while the cache still holds this update's value. If
    /// something newer has landed since -- another mutation's optimistic value, a refetch -- the key is
    /// invalidated instead: restoring an old snapshot over newer data is how overlapping optimistic updates
    /// corrupt each other.
    /// </remarks>
    /// <example><code>
    /// var save = queries.Mutation&lt;Todo, Todo&gt;((todo, ct) => api.SaveAsync(todo, ct), new()
    /// {
    ///     OnMutate = todo => queries.Optimistic&lt;IReadOnlyList&lt;Todo&gt;&gt;(["todos"], list => [.. list ?? [], todo]),
    ///     Invalidates = [["todos"]],
    /// });
    /// </code></example>
    public BlexOptimisticUpdate<T> Optimistic<T>(BlexQueryKey key, Func<T?, T> update)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(update);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var entry = GetOrCreate<T>(key);
        entry.Cancel();

        bool hadData;
        T? previous;
        DateTimeOffset? updatedAt;
        BlexQueryStatus status;
        lock (Gate)
        {
            hadData = entry.TryGetData(out previous, out updatedAt);
            status = entry.Status;
        }

        T next;
        try
        {
            next = update(hadData ? previous : default);
            entry.SetData(next);
        }
        finally
        {
            ScheduleGc(entry);
        }

        return new BlexOptimisticUpdate<T>(this, entry, hadData, previous, updatedAt, status, next);
    }

    /// <summary>Creates a mutation: a change sent to the server, with its own status and the cache updates around it.</summary>
    /// <param name="mutate">Sends the change; honour the token, which is the caller's.</param>
    /// <param name="options">Callbacks, optimistic updates and the keys to invalidate once it settles.</param>
    public BlexMutation<TArg, TResult> Mutation<TArg, TResult>(
        Func<TArg, CancellationToken, Task<TResult>> mutate,
        BlexMutationOptions<TArg, TResult>? options = null)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        return new BlexMutation<TArg, TResult>(this, mutate, options ?? new BlexMutationOptions<TArg, TResult>());
    }

    /// <summary>
    /// Marks every query <paramref name="key"/> covers as stale and refetches the ones an enabled observer is
    /// watching -- cancelling a fetch of theirs already running, whose answer predates the change. The others
    /// are refetched when next observed. Completes once those refetches have settled.
    /// </summary>
    /// <param name="key">A key, or the leading parts of keys: <c>["todos"]</c> covers <c>["todos", 42]</c>.</param>
    /// <param name="exact">Cover only the query with exactly this key.</param>
    public Task InvalidateAsync(BlexQueryKey key, bool exact = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        var refetches = InvalidateAsync(Matcher(key, exact));

        // For the other tabs (SyncAcrossTabs); what they pass on arrives through InvalidateFromElsewhereAsync, which
        // does not travel again.
        if (InvalidatedHere is { } invalidated)
        {
            try
            {
                invalidated(key.ToString(), exact);
            }
            catch (Exception ex)
            {
                ReportError(ex, key);
            }
        }

        return refetches;
    }

    /// <summary>Raised after an invalidation by key made here, with the key's text: what the cross-tab bridge passes on.</summary>
    internal event Action<string, bool>? InvalidatedHere;

    /// <summary>
    /// Invalidates what another tab invalidated, by the key's text: a key here is covered when its text is that one, or
    /// -- unless <paramref name="exact"/> -- starts with it at a part boundary. Each part's text delimits itself
    /// (strings are quoted and escaped), so that is exactly a prefix match.
    /// </summary>
    internal Task InvalidateFromElsewhereAsync(string keyText, bool exact)
    {
        ArgumentNullException.ThrowIfNull(keyText);
        if (keyText.Length < 2 || keyText[0] != '[' || keyText[^1] != ']')
            return Task.CompletedTask;

        var prefix = keyText[..^1];
        return InvalidateAsync(key =>
        {
            var text = key.ToString();
            if (text == keyText)
                return true;

            return !exact && text.Length > prefix.Length && text.StartsWith(prefix, StringComparison.Ordinal) && text[prefix.Length] == ',';
        });
    }

    /// <summary>Invalidates every query whose key <paramref name="predicate"/> accepts; see <see cref="InvalidateAsync(BlexQueryKey, bool)"/>.</summary>
    public Task InvalidateAsync(Func<BlexQueryKey, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var refetches = new List<Task>();
        foreach (var entry in Match(predicate))
        {
            entry.Invalidate();
            if (entry.IsActive)
                refetches.Add(entry.RefetchAsync(cancelRunning: true));
        }

        return Task.WhenAll(refetches);
    }

    /// <summary>Refetches the observed queries <paramref name="key"/> covers now, without marking the rest stale.</summary>
    public Task RefetchAsync(BlexQueryKey key, bool exact = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        var refetches = new List<Task>();
        foreach (var entry in Match(Matcher(key, exact)))
        {
            if (entry.IsActive)
                refetches.Add(entry.RefetchAsync(cancelRunning: true));
        }

        return Task.WhenAll(refetches);
    }

    /// <summary>Cancels the fetches running for the queries <paramref name="key"/> covers; they keep what they had.</summary>
    public void Cancel(BlexQueryKey key, bool exact = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        foreach (var entry in Match(Matcher(key, exact)))
            entry.Cancel();
    }

    /// <summary>
    /// Removes the queries <paramref name="key"/> covers from the cache, cancelling their fetches. An observer
    /// still watching one starts over on a fresh entry -- no data, and a fetch if it is enabled -- so a removal
    /// on sign-out never leaves the previous user's data on screen.
    /// </summary>
    public void Remove(BlexQueryKey key, bool exact = false)
    {
        ArgumentNullException.ThrowIfNull(key);
        var removed = new List<BlexQueryEntry>();
        foreach (var entry in Match(Matcher(key, exact)))
            removed.Add(entry);

        var text = key.ToString();
        var prefix = text[..^1] + ",";

        lock (Gate)
        {
            for (var i = removed.Count - 1; i >= 0; i--)
            {
                // Only what is still cached: another removal may have taken it meanwhile.
                if (_entries.TryGetValue(removed[i].Key, out var current) && ReferenceEquals(current, removed[i]))
                    _entries.Remove(removed[i].Key);
                else
                    removed.RemoveAt(i);
            }

            string[] handedOver = [.. _hydration.Keys];
            foreach (var pending in handedOver)
            {
                if (pending == text || (!exact && pending.StartsWith(prefix, StringComparison.Ordinal)))
                    _hydration.Remove(pending);
            }
        }

        Evict(removed);
    }

    /// <summary>Removes every query; observers still watching one start over, as with <see cref="Remove"/>.</summary>
    public void Clear()
    {
        BlexQueryEntry[] removed;
        lock (Gate)
        {
            removed = [.. _entries.Values];
            _entries.Clear();
            _hydration.Clear();
            _hydrationTimer?.Dispose();
            _hydrationTimer = null;
            _hydrationKeys = null;
        }

        Evict(removed);
    }

    private void Evict(IReadOnlyCollection<BlexQueryEntry> removed)
    {
        if (removed.Count == 0)
            return;

        var observers = new List<BlexQuery>();
        foreach (var entry in removed)
        {
            observers.AddRange(entry.Observers());
            entry.Evict();
        }

        // Moved to fresh entries after every removed one is gone, so two observers of one key share one. Once the
        // client is disposed there is nothing to move to; an observer nobody disposed just stops polling.
        foreach (var observer in observers)
        {
            if (IsDisposed)
                observer.StopPolling();
            else
                observer.Rebind();
        }

        OnEntryChanged();
    }

    /// <summary>
    /// Refetches the stale queries an enabled observer with <see cref="BlexQueryOptions.RefetchOnFocus"/>
    /// watches -- call it when the app regains focus (a browser tab becoming visible, a MAUI window activating).
    /// </summary>
    public void NotifyFocused() => RefetchStaleObserved();

    /// <summary>
    /// Tells the cache the network is back: fetches and mutations paused by <see cref="NotifyOffline"/> go on, and stale
    /// observed queries refetch the way <see cref="NotifyFocused"/> does.
    /// </summary>
    public void NotifyReconnected()
    {
        TaskCompletionSource? backOnline;
        bool wasOffline;
        lock (Gate)
        {
            wasOffline = !IsOnline;
            IsOnline = true;
            backOnline = _backOnline;
            _backOnline = null;
        }

        // Outside the gate: the paused requests' continuations run asynchronously, but their observers are notified.
        backOnline?.TrySetResult();
        if (wasOffline)
            OnEntryChanged();

        RefetchStaleObserved();
    }

    private void RefetchStaleObserved()
    {
        foreach (var entry in Match(static _ => true))
            _ = entry.RefetchAsync(cancelRunning: false, static o => o.Options.RefetchOnFocus && o.IsStale);
    }

    /// <summary>The number of queries fetching right now.</summary>
    public int FetchingCount
    {
        get
        {
            lock (Gate)
                return _entries.Values.Count(e => e.FetchStatus == BlexFetchStatus.Fetching);
        }
    }

    /// <summary>A snapshot of every cached query, for inspection and diagnostics.</summary>
    public IReadOnlyList<BlexQueryInfo> GetQueries()
    {
        lock (Gate)
        {
            return [.. _entries.Values.Select(e => new BlexQueryInfo(
                e.Key, e.DataType, e.Status, e.FetchStatus, e.ObserverCount, e.DataUpdatedAt, e.IsInvalidated, e.Error))];
        }
    }

    /// <summary>
    /// The successfully fetched queries as JSON -- what a prerender hands the interactive render so it does
    /// not fetch them again. <c>&lt;BlexProvider&gt;</c> does this through <c>PersistentComponentState</c>.
    /// </summary>
    public JsonObject Dehydrate()
    {
        BlexQueryEntry[] entries;
        lock (Gate)
            entries = [.. _entries.Values];

        var state = new JsonObject();
        foreach (var entry in entries)
        {
            try
            {
                if (!entry.TrySerializeData(out var data, out var updatedAt))
                    continue;

                state[entry.Key.ToString()] = new JsonObject
                {
                    ["data"] = data,
                    ["at"] = updatedAt.ToString("O", CultureInfo.InvariantCulture),
                };
            }
            catch (Exception ex)
            {
                ReportError(ex, entry.Key);
            }
        }

        return state;
    }

    /// <summary>
    /// Seeds the cache from <see cref="Dehydrate"/>'s output. A query already cached with newer data keeps
    /// it; one not cached yet takes the handed-over data the moment it is first asked for.
    /// </summary>
    /// <remarks>
    /// Hydrated data ages like fetched data: with the default zero <see cref="BlexQueryClientOptions.StaleTime"/>
    /// it is stale at once, so the interactive render shows it and refetches in the background. Give queries
    /// that are prerendered a stale time to skip that second request.
    /// </remarks>
    public void Hydrate(JsonObject state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (IsDisposed)
            return;

        // The cache's keys as text, indexed once: scanning every entry per handed-over key was O(keys x entries)
        // under the gate, and a handoff of a few hundred queries into a cache of as many is ordinary. An entry
        // created for a handed-over key while this runs is missed, as the scan missed it one lock later.
        Dictionary<string, BlexQueryEntry> byText;
        lock (Gate)
        {
            byText = new Dictionary<string, BlexQueryEntry>(_entries.Count, StringComparer.Ordinal);
            foreach (var entry in _entries.Values)
                byText[entry.Key.ToString()] = entry;
        }

        var pending = new List<string>();
        foreach (var (key, value) in state)
        {
            if (value is not JsonObject item
                || item["at"] is not JsonValue at
                || !at.TryGetValue<string>(out var text)
                || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var updatedAt))
            {
                continue;
            }

            var data = item["data"]?.DeepClone();
            if (byText.TryGetValue(key, out var existing))
            {
                try
                {
                    existing.Hydrate(data, updatedAt);
                }
                catch (Exception ex)
                {
                    ReportError(ex, existing.Key);
                }

                continue;
            }

            lock (Gate)
            {
                _hydration[key] = (data, updatedAt);
                pending.Add(key);
            }
        }

        // A handed-over query nobody asks for within the GC time is dropped like any unobserved one, rather
        // than kept for the life of the app. The timer is the client's, so disposing the client stops it.
        if (pending.Count > 0 && Options.GcTime != TimeSpan.MaxValue && Options.GcTime != Timeout.InfiniteTimeSpan)
        {
            // An earlier handoff's still-unclaimed keys move to this timer (a second handoff is rare: a scope
            // hydrated again), so one timer is ever pending.
            lock (Gate)
            {
                if (_hydrationKeys is not null)
                    pending.AddRange(_hydrationKeys);

                _hydrationKeys = pending;
            }

            ScheduleHydrationDrop(pending, Options.GcTime < TimeSpan.Zero ? TimeSpan.Zero : Options.GcTime);
        }
    }

    /// <summary>
    /// Drops what is still unclaimed of the handed-over queries in <paramref name="keys"/> after <paramref name="due"/>
    /// -- waited out in more than one go when that is longer than a timer can wait (<see cref="MaxTimerDue"/>).
    /// </summary>
    private void ScheduleHydrationDrop(List<string> keys, TimeSpan due)
    {
        var wait = due > MaxTimerDue ? MaxTimerDue : due;
        var dueAt = due > MaxTimerDue ? DueAt(due) : (DateTimeOffset?)null;
        var timer = TimeProvider.CreateTimer(static state =>
        {
            var (client, keys, dueAt) = ((BlexQueryClient, List<string>, DateTimeOffset?))state!;
            client.Post(() =>
            {
                var remaining = dueAt is { } at ? at - client.TimeProvider.GetUtcNow() : TimeSpan.Zero;
                lock (client.Gate)
                {
                    // A later handoff took the keys over, with a timer of its own; or the client is disposed.
                    if (!ReferenceEquals(client._hydrationKeys, keys))
                        return;

                    if (remaining <= TimeSpan.Zero)
                    {
                        // Only what the handoffs left unclaimed; entries already asked for took theirs.
                        foreach (var key in keys)
                            client._hydration.Remove(key);

                        client._hydrationKeys = null;
                        return;
                    }
                }

                // The wait was capped: the rest of it.
                client.ScheduleHydrationDrop(keys, remaining);
            });
        }, (this, keys, dueAt), wait, Timeout.InfiniteTimeSpan);

        ITimer? previous;
        lock (Gate)
        {
            previous = _hydrationTimer;
            _hydrationTimer = IsDisposed ? null : timer;
        }

        previous?.Dispose();
        if (IsDisposed)
            timer.Dispose();
    }

    /// <summary>Stops every fetch and timer and empties the cache. Idempotent.</summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        Clear();
    }

    private BlexQueryEntry<T> GetOrCreate<T>(BlexQueryKey key)
    {
        BlexQueryEntry<T> created;
        (JsonNode? Data, DateTimeOffset UpdatedAt) handoff;
        bool handedOver;
        lock (Gate)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                // In use again: its pending removal is off; whoever is using it schedules a new one when done.
                var typed = Typed<T>(existing);
                CancelGc(existing);
                return typed;
            }

            created = new BlexQueryEntry<T>(this, key) { GcTime = Options.GcTime };
            _entries[key] = created;

            var text = key.ToString();
            handedOver = _hydration.Remove(text, out handoff);
        }

        if (handedOver)
        {
            try
            {
                created.Hydrate(handoff.Data, handoff.UpdatedAt);
            }
            catch (Exception ex)
            {
                // A payload this build cannot read is not worth failing the query over: it fetches instead.
                ReportError(ex, key);
            }
        }

        return created;
    }

    private static BlexQueryEntry<T> Typed<T>(BlexQueryEntry entry)
        => entry as BlexQueryEntry<T>
            ?? throw new InvalidOperationException(
                $"The query {entry.Key} is cached with data of type {entry.DataType}, not {typeof(T)}. Give queries of different types different keys.");

    private static Func<BlexQueryKey, bool> Matcher(BlexQueryKey key, bool exact)
        => exact ? k => k.Equals(key) : k => k.StartsWith(key);

    private List<BlexQueryEntry> Match(Func<BlexQueryKey, bool> predicate)
    {
        // The predicate may be the caller's own code: run it on a copy, outside the gate.
        KeyValuePair<BlexQueryKey, BlexQueryEntry>[] entries;
        lock (Gate)
            entries = [.. _entries];

        var matches = new List<BlexQueryEntry>();
        foreach (var (key, entry) in entries)
        {
            if (predicate(key))
                matches.Add(entry);
        }

        return matches;
    }

    internal void Detach<T>(BlexQuery<T> observer)
    {
        lock (Gate)
            observer.Entry.Detach(observer);

        ScheduleGc(observer.Entry);
        OnEntryChanged();
    }

    internal void OnObserverOptions(BlexQueryEntry entry, BlexQueryOptions options)
    {
        lock (Gate)
            entry.GcTime = Max(entry.GcTime, GcTimeFor(options));
    }

    internal void OnFetchSettled(BlexQueryEntry entry) => ScheduleGc(entry);

    /// <summary>
    /// Schedules the removal of an entry nobody observes and nothing is fetching, after its GC time. The
    /// timer is created outside the gate -- a zero due time can fire at once, on another thread -- and only
    /// kept if the entry still needs it by then.
    /// </summary>
    private void ScheduleGc(BlexQueryEntry entry, TimeSpan? remaining = null)
    {
        TimeSpan wait;
        object ticket;
        lock (Gate)
        {
            if (IsDisposed || entry.ObserverCount > 0 || entry.FetchStatus == BlexFetchStatus.Fetching
                || entry.GcTicket is not null || entry.GcTime == TimeSpan.MaxValue || entry.GcTime == Timeout.InfiniteTimeSpan
                || !_entries.TryGetValue(entry.Key, out var current) || !ReferenceEquals(current, entry))
            {
                return;
            }

            var due = remaining ?? (entry.GcTime < TimeSpan.Zero ? TimeSpan.Zero : entry.GcTime);

            // A timer cannot wait longer than MaxTimerDue; a longer GC time is waited out in more than one go.
            wait = due > MaxTimerDue ? MaxTimerDue : due;
            entry.GcDueAt = due > MaxTimerDue ? DueAt(due) : null;

            // Reserved before the timer exists, so a timer that fires before it is stored still finds its ticket.
            ticket = entry.GcTicket = new object();
        }

        var timer = TimeProvider.CreateTimer(static state =>
        {
            var (client, target, ticket) = ((BlexQueryClient, BlexQueryEntry, object))state!;
            client.Post(() => client.Collect(target, ticket));
        }, (this, entry, ticket), wait, Timeout.InfiniteTimeSpan);

        lock (Gate)
        {
            if (ReferenceEquals(entry.GcTicket, ticket))
            {
                entry.GcTimer = timer;
                return;
            }
        }

        // Touched (observed, fetched, written) before the timer was even stored: no longer due.
        timer.Dispose();
    }

    private void Collect(BlexQueryEntry entry, object ticket)
    {
        bool removed;
        TimeSpan rest;
        lock (Gate)
        {
            // Touched since this removal was scheduled: a newer schedule, or none, owns it now.
            if (!ReferenceEquals(entry.GcTicket, ticket))
                return;

            // Spent either way; declining leaves the entry free to be scheduled again once it is idle.
            CancelGc(entry);
            rest = entry.GcDueAt is { } dueAt ? dueAt - TimeProvider.GetUtcNow() : TimeSpan.Zero;
            removed = rest <= TimeSpan.Zero
                && entry.ObserverCount == 0
                && entry.FetchStatus == BlexFetchStatus.Idle
                && _entries.TryGetValue(entry.Key, out var current)
                && ReferenceEquals(current, entry);

            if (removed)
                _entries.Remove(entry.Key);
        }

        // The wait was capped: the rest of it, from a new timer -- unless the entry is in use again, which the
        // schedule checks; then its next idle spell starts the GC time over.
        if (rest > TimeSpan.Zero)
        {
            ScheduleGc(entry, rest);
            return;
        }

        if (removed)
        {
            entry.Evict();
            OnEntryChanged();
        }
    }

    /// <summary>
    /// Runs timer work on the manager's dispatcher, where the state it touches belongs -- or right here when
    /// none is bound (WebAssembly, tests, headless hosts).
    /// </summary>
    internal void Post(Action work)
    {
        if (_manager is { Dispatcher: { } dispatcher } manager && !dispatcher.CheckAccess())
        {
            var task = manager.InvokeAsync(work);
            if (!task.IsCompletedSuccessfully)
                _ = ObserveAsync(task);

            return;
        }

        try
        {
            work();
        }
        catch (Exception ex)
        {
            ReportError(ex, null);
        }

        async Task ObserveAsync(Task pending)
        {
            try
            {
                await pending.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                // ObjectDisposedException: the renderer is gone with its circuit, and the work with it.
                ReportError(ex, null);
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    internal void OnEntryChanged()
    {
        if (CacheChanged is not { } handler)
            return;

        foreach (var target in handler.GetInvocationList())
        {
            try
            {
                ((Action)target)();
            }
            catch (Exception ex)
            {
                ReportError(ex, null);
            }
        }
    }

    internal void ReportError(Exception exception, BlexQueryKey? key)
    {
        if (_manager is { } manager)
            manager.ReportError("query", exception, key?.ToString());
        else
            Console.Error.WriteLine($"[Blex] query failed{(key is null ? "" : $" ({key})")}: {exception.Message}");
    }

    /// <summary>The longest a timer can wait (about 49 days); a longer wait is made in more than one go.</summary>
    internal static readonly TimeSpan MaxTimerDue = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    /// <summary>When a wait of <paramref name="due"/> from now ends -- the end of time, for one that would not fit a date.</summary>
    internal DateTimeOffset DueAt(TimeSpan due)
    {
        var now = TimeProvider.GetUtcNow();
        return due >= DateTimeOffset.MaxValue - now ? DateTimeOffset.MaxValue : now + due;
    }

    private static readonly PropertyChangedEventArgs AllPropertiesChanged = new(string.Empty);

    /// <summary>
    /// Raises an observer's change notification -- its <c>Changed</c> event and its
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/>, always with an empty property name ("all properties
    /// changed") so XAML bindings can bind straight to it -- with every handler isolated: one that throws is reported
    /// against <paramref name="key"/>, and the rest still run.
    /// </summary>
    internal void RaiseIsolated(Action? changed, PropertyChangedEventHandler? propertyChanged, object sender, BlexQueryKey? key)
    {
        if (changed is not null)
        {
            foreach (var handler in changed.GetInvocationList())
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception ex)
                {
                    ReportError(ex, key);
                }
            }
        }

        if (propertyChanged is not null)
        {
            foreach (var handler in propertyChanged.GetInvocationList())
            {
                try
                {
                    ((PropertyChangedEventHandler)handler)(sender, AllPropertiesChanged);
                }
                catch (Exception ex)
                {
                    ReportError(ex, key);
                }
            }
        }
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a >= b ? a : b;
}
