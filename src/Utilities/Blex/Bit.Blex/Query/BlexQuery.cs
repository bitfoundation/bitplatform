using System.ComponentModel;

namespace Bit.Blex.Query;

/// <summary>
/// An observer of one cached query: what a component reads -- the status, the error, whether a fetch is
/// running -- whatever the data's type. <see cref="BlexQuery{T}"/> adds the data.
/// </summary>
/// <remarks>
/// <see cref="BlexQueryStatus"/> says whether there is data; <see cref="BlexFetchStatus"/> says whether a
/// request is running. They are independent: a query refetching in the background is
/// <c>Success</c> and <c>Fetching</c> at once, which is why <see cref="IsLoading"/> (no data yet) and
/// <see cref="IsRefetching"/> (data shown, newer on its way) are different things.
/// </remarks>
public abstract class BlexQuery : INotifyPropertyChanged, IDisposable
{
    private static readonly PropertyChangedEventArgs AllPropertiesChanged = new(string.Empty);

    private protected BlexQuery(BlexQueryClient client, BlexQueryOptions options)
    {
        Client = client;
        Options = options;
    }

    private protected BlexQueryClient Client { get; }

    /// <summary>The key of the observed query.</summary>
    public abstract BlexQueryKey Key { get; }

    /// <summary>How this observer wants the query fetched.</summary>
    public BlexQueryOptions Options { get; private protected set; }

    /// <summary>Whether there is data, an error, or neither yet. Placeholder data counts as <see cref="BlexQueryStatus.Success"/>.</summary>
    public abstract BlexQueryStatus Status { get; }

    /// <summary>Whether a fetch is running.</summary>
    public abstract BlexFetchStatus FetchStatus { get; }

    /// <summary>The error of the last fetch, or <c>null</c>. Data from an earlier success is kept alongside it.</summary>
    public abstract Exception? Error { get; }

    /// <summary>When the cached data was fetched (or set), or <c>null</c> when there is none.</summary>
    public abstract DateTimeOffset? DataUpdatedAt { get; }

    /// <summary>How many times the current (or last) fetch failed, retries included.</summary>
    public abstract int FailureCount { get; }

    /// <summary>The most recent failure of the current (or last) fetch -- set while it is still being retried.</summary>
    public abstract Exception? FailureReason { get; }

    /// <summary>Whether there is data to show -- cached, or placeholder.</summary>
    public abstract bool HasData { get; }

    /// <summary>Whether the data shown is the previous key's, kept while this key loads (<see cref="BlexQueryOptions.KeepPreviousData"/>).</summary>
    public abstract bool IsPlaceholderData { get; }

    /// <summary>Whether the cached data is older than this observer's stale time, invalidated, or missing.</summary>
    public abstract bool IsStale { get; }

    /// <summary>No data yet: the first fetch has not finished.</summary>
    public bool IsPending => Status == BlexQueryStatus.Pending;

    /// <summary>The last fetch failed.</summary>
    public bool IsError => Status == BlexQueryStatus.Error;

    /// <summary>There is data.</summary>
    public bool IsSuccess => Status == BlexQueryStatus.Success;

    /// <summary>A fetch is running.</summary>
    public bool IsFetching => FetchStatus == BlexFetchStatus.Fetching;

    /// <summary>The first fetch is running: nothing to show but a loading state.</summary>
    public bool IsLoading => IsFetching && IsPending;

    /// <summary>Data is shown and a newer copy is being fetched.</summary>
    public bool IsRefetching => IsFetching && !IsPending;

    /// <summary>A fetch is waiting for the network to come back (<see cref="BlexFetchStatus.Paused"/>).</summary>
    public bool IsPaused => FetchStatus == BlexFetchStatus.Paused;

    /// <summary>Raised after the observed query changed. Handler exceptions are isolated and reported.</summary>
    public event Action? Changed;

    /// <summary>
    /// Raised with the other change notification, always with an empty property name ("all properties
    /// changed"), so XAML bindings can bind straight to an observer.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Fetches again now, cancelling a fetch already running. Completes once it has settled; never throws.</summary>
    public abstract Task RefetchAsync();

    /// <summary>Completes once no fetch is running -- at once when none is. Never throws.</summary>
    /// <example><code>
    /// // Prerendering: wait, so the HTML carries the data rather than a spinner.
    /// protected override async Task OnParametersSetAsync()
    ///     => await UseQuery(["todo", Id], ct => Api.GetTodoAsync(Id, ct)).WhenSettledAsync();
    /// </code></example>
    public abstract Task WhenSettledAsync();

    /// <summary>Stops observing. The query stays cached for its GC time, so another observer can pick it up.</summary>
    public abstract void Dispose();

    /// <summary>Moves to a fresh entry for the same key after the cached one was removed.</summary>
    internal abstract void Rebind();

    /// <summary>Stops polling for good: the client was disposed, and an observer nobody disposed must not keep it alive.</summary>
    internal abstract void StopPolling();

    private protected void RaiseChanged()
    {
        if (Changed is { } changed)
        {
            foreach (var handler in changed.GetInvocationList())
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception ex)
                {
                    Client.ReportError(ex, Key);
                }
            }
        }

        if (PropertyChanged is { } propertyChanged)
        {
            foreach (var handler in propertyChanged.GetInvocationList())
            {
                try
                {
                    ((PropertyChangedEventHandler)handler)(this, AllPropertiesChanged);
                }
                catch (Exception ex)
                {
                    Client.ReportError(ex, Key);
                }
            }
        }
    }
}

/// <summary>
/// An observer of one cached query with data of type <typeparamref name="T"/>. Created by
/// <see cref="BlexQueryClient.Query{T}(BlexQueryKey, Func{CancellationToken, Task{T}}, BlexQueryOptions)"/>
/// (or a component's <c>UseQuery</c>); dispose it when done.
/// </summary>
/// <typeparam name="T">The data the query fetches.</typeparam>
public sealed class BlexQuery<T> : BlexQuery
{
    private BlexQueryEntry<T> _entry = default!;
    private Func<CancellationToken, Task<T>> _fetch;
    private T? _placeholder;
    private bool _hasPlaceholder;
    private ITimer? _interval;
    private ITimer? _staleTimer;
    private bool _disposed;

    internal BlexQuery(
        BlexQueryClient client,
        Func<CancellationToken, Task<T>> fetch,
        BlexQueryOptions options,
        bool hasPlaceholder,
        T? placeholder)
        : base(client, options)
    {
        _fetch = fetch;
        _hasPlaceholder = hasPlaceholder;
        _placeholder = placeholder;
    }

    /// <summary>The fetch this observer was given; a cache-level refetch of an entry that never fetched uses it.</summary>
    internal Func<CancellationToken, Task<T>> Fetch => _fetch;

    /// <summary>The cached entry observed; assigned by <see cref="BlexQueryClient.Attach{T}"/> under the gate.</summary>
    internal BlexQueryEntry<T> Entry
    {
        get => _entry;
        set => _entry = value;
    }

    /// <inheritdoc />
    public override BlexQueryKey Key => _entry.Key;

    /// <summary>The cached data -- or the placeholder while there is none -- or <c>default</c>.</summary>
    public T? Data
    {
        get
        {
            lock (Client.Gate)
                return _entry.HasData ? _entry.Data : _hasPlaceholder ? _placeholder : default;
        }
    }

    /// <inheritdoc />
    public override BlexQueryStatus Status => IsPlaceholderData ? BlexQueryStatus.Success : _entry.Status;

    /// <inheritdoc />
    public override BlexFetchStatus FetchStatus => _entry.FetchStatus;

    /// <inheritdoc />
    public override Exception? Error => _entry.Error;

    /// <inheritdoc />
    public override DateTimeOffset? DataUpdatedAt => _entry.DataUpdatedAt;

    /// <inheritdoc />
    public override int FailureCount => _entry.FailureCount;

    /// <inheritdoc />
    public override Exception? FailureReason => _entry.FailureReason;

    /// <inheritdoc />
    public override bool HasData => _entry.HasData || _hasPlaceholder;

    /// <inheritdoc />
    public override bool IsPlaceholderData => !_entry.HasData && _hasPlaceholder;

    /// <inheritdoc />
    public override bool IsStale => _entry.IsStale(Client.StaleTimeFor(Options), Client.TimeProvider.GetUtcNow());

    /// <summary>
    /// Adopts a newer fetch and options for the same key -- the closure a component builds on every
    /// parameter change may capture newer values. Turning <see cref="BlexQueryOptions.Enabled"/> on fetches
    /// stale data; a changed <see cref="BlexQueryOptions.RefetchInterval"/> restarts the polling.
    /// </summary>
    public void Update(Func<CancellationToken, Task<T>> fetch, BlexQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(fetch);
        if (_disposed)
            return;

        options ??= Options;
        var previous = Options;
        _fetch = fetch;
        Options = options;

        // The entry was removed (Remove/Clear) since: start over on a fresh one rather than keep showing it.
        if (!Client.IsCached(_entry))
        {
            Rebind();
            return;
        }

        Client.OnObserverOptions(_entry, options);

        if (previous.RefetchInterval != options.RefetchInterval || previous.Enabled != options.Enabled)
            RestartInterval();

        if (previous.StaleTime != options.StaleTime)
            ScheduleStaleNotice();

        if (options.Enabled && !previous.Enabled)
            Mount();
    }

    internal override void Rebind()
    {
        if (_disposed || Client.IsDisposed)
            return;

        lock (Client.Gate)
        {
            _entry.Detach(this);
            _hasPlaceholder = false;
            _placeholder = default;
        }

        Client.Attach(Key, this);
        RaiseChanged();
        Mount();
    }

    /// <summary>Fetches if this observer is enabled, wants a refetch on mount and the data is stale; joins a fetch already running.</summary>
    internal void Mount()
    {
        RestartInterval();
        ScheduleStaleNotice();
        if (Options.Enabled && Options.RefetchOnMount && IsStale)
            _ = FetchAsync(cancelRunning: false);
    }

    internal Task FetchAsync(bool cancelRunning)
        => _entry.FetchAsync(_fetch, Client.RetryCountFor(Options), Client.RetryDelayFor(Options), cancelRunning, networkMode: Options.NetworkMode);

    /// <summary>Runs <paramref name="fetch"/> once, keeping the fetch refetches use; joins a fetch already running.</summary>
    internal Task FetchOnceAsync(Func<CancellationToken, Task<T>> fetch)
        => _entry.FetchAsync(fetch, Client.RetryCountFor(Options), Client.RetryDelayFor(Options), cancelRunning: false, once: true, networkMode: Options.NetworkMode);

    /// <inheritdoc />
    public override Task RefetchAsync() => _disposed ? Task.CompletedTask : FetchAsync(cancelRunning: true);

    /// <inheritdoc />
    public override Task WhenSettledAsync() => _entry.Settled;

    internal void OnEntryChanged()
    {
        if (_disposed)
            return;

        // The real data has arrived -- or the fetch for it failed, which the placeholder must not hide behind a
        // Success status; either way the previous key's data is not needed any more.
        if (_hasPlaceholder && (_entry.HasData || _entry.Status == BlexQueryStatus.Error))
        {
            lock (Client.Gate)
            {
                _hasPlaceholder = false;
                _placeholder = default;
            }
        }

        RaiseChanged();
        ScheduleStaleNotice();
    }

    internal override void StopPolling()
    {
        _interval?.Dispose();
        _interval = null;
        _staleTimer?.Dispose();
        _staleTimer = null;
    }

    /// <summary>
    /// Raises <see cref="BlexQuery.Changed"/> at the moment the data turns stale. <see cref="IsStale"/> is
    /// worked out on read, and nothing else happens when the stale time runs out -- no fetch, no new data --
    /// so a component showing it (or deciding what to render by it) would keep showing "fresh" until
    /// something unrelated re-rendered it. Rescheduled whenever the entry changes.
    /// </summary>
    private void ScheduleStaleNotice()
    {
        _staleTimer?.Dispose();
        _staleTimer = null;

        if (_disposed || Client.IsDisposed)
            return;

        var staleTime = Client.StaleTimeFor(Options);
        if (staleTime == TimeSpan.MaxValue || _entry.IsInvalidated || !_entry.HasData || _entry.DataUpdatedAt is not { } updatedAt)
            return;

        var due = staleTime - (Client.TimeProvider.GetUtcNow() - updatedAt);
        if (due <= TimeSpan.Zero)
            return; // stale already, and the notification that delivered the data said so

        // A timer cannot wait longer than this; a stale time beyond it is waited out in more than one go.
        if (due > MaxTimerDue)
            due = MaxTimerDue;

        _staleTimer = Client.TimeProvider.CreateTimer(
            static state => ((BlexQuery<T>)state!).OnStale(),
            this,
            due,
            Timeout.InfiniteTimeSpan);
    }

    private static readonly TimeSpan MaxTimerDue = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    private void OnStale()
        => Client.Post(() =>
        {
            if (_disposed)
                return;

            // Checked rather than assumed: the wait may have been capped, or the data replaced since.
            if (IsStale)
                RaiseChanged();
            else
                ScheduleStaleNotice();
        });

    private void RestartInterval()
    {
        _interval?.Dispose();
        _interval = null;

        if (_disposed || Client.IsDisposed || !Options.Enabled || Options.RefetchInterval is not { } interval || interval <= TimeSpan.Zero)
            return;

        _interval = Client.TimeProvider.CreateTimer(
            static state => ((BlexQuery<T>)state!).OnInterval(),
            this,
            interval,
            interval);
    }

    private void OnInterval()
        => Client.Post(() =>
        {
            if (!_disposed && Options.Enabled)
                _ = FetchAsync(cancelRunning: false);
        });

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _interval?.Dispose();
        _interval = null;
        _staleTimer?.Dispose();
        _staleTimer = null;
        Client.Detach(this);
    }
}
