using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blex;

internal sealed class BlexQueryEntry<T>(BlexQueryClient client, BlexQueryKey key) : BlexQueryEntry(client, key)
{
    private readonly List<BlexQuery<T>> _observers = [];
    private T? _data;
    private bool _hasData;
    private Func<CancellationToken, Task<T>>? _fetch;
    private int _retryCount;
    private TimeSpan _retryDelay;
    private BlexNetworkMode? _networkMode;
    private Task<FetchResult>? _inFlight;
    private TaskCompletionSource<FetchResult>? _done;
    private CancellationTokenSource? _cts;
    private int _fetchId;

    // The invalidations the running fetch started after: one made since means its answer predates a change.
    private int _fetchInvalidations;

    public T? Data => _data;

    public override bool HasData => _hasData;

    public override Type DataType => typeof(T);

    public override int ObserverCount => _observers.Count;

    public override bool IsActive
    {
        get
        {
            // Under the gate: an observer attaching or leaving on another thread must not break the walk.
            lock (Client.Gate)
            {
                foreach (var observer in _observers)
                {
                    if (observer.Options.Enabled)
                        return true;
                }

                return false;
            }
        }
    }

    /// <summary>The observers watching the entry now, for moving them to a fresh entry when this one is removed.</summary>
    public override IReadOnlyList<BlexQuery> Observers()
    {
        lock (Client.Gate)
            return [.. _observers];
    }

    /// <summary>The running fetch, or a completed task when none is running.</summary>
    public Task Settled
    {
        get
        {
            lock (Client.Gate)
                return _inFlight ?? Task.CompletedTask;
        }
    }

    /// <summary>Adds an observer; the caller holds the gate.</summary>
    public void Attach(BlexQuery<T> observer) => _observers.Add(observer);

    /// <summary>Removes an observer; the caller holds the gate.</summary>
    public void Detach(BlexQuery<T> observer) => _observers.Remove(observer);

    /// <summary>
    /// Starts a fetch -- or, unless <paramref name="cancelRunning"/>, joins the one already running, so a
    /// burst of observers mounting at once costs one request. The task never faults: it completes with how the
    /// fetch ended (or the one that superseded it), and the data or error lands in the entry's state.
    /// </summary>
    /// <param name="fetch">The fetch to run, which the entry also keeps for later refetches; <c>null</c> reuses the last one.</param>
    /// <param name="retryCount">How many failures are retried.</param>
    /// <param name="retryDelay">The wait before the first retry.</param>
    /// <param name="cancelRunning">
    /// Cancel a running fetch and start over rather than join it: an explicit refetch or an invalidation,
    /// which asks for data as of now, not for the answer to a request made before the change.
    /// </param>
    /// <param name="once">
    /// Run <paramref name="fetch"/> this time only, keeping the fetch later refetches use: an infinite query's next
    /// page, which a refetch must not repeat.
    /// </param>
    /// <param name="networkMode">Whether the fetch waits for the network while offline; <c>null</c> uses the client's.</param>
    public Task<FetchResult> FetchAsync(
        Func<CancellationToken, Task<T>>? fetch,
        int retryCount,
        TimeSpan retryDelay,
        bool cancelRunning,
        bool once = false,
        BlexNetworkMode? networkMode = null)
        => FetchCore(fetch, retryCount, retryDelay, cancelRunning, once, networkMode, joinRunning: true)!;

    /// <summary>
    /// Runs <paramref name="fetch"/> this time only, as <see cref="FetchAsync"/> with <c>once</c> does -- or returns
    /// <c>null</c> while a fetch is running, rather than hand that fetch back as if it were this one: an infinite
    /// query's next page must follow the pages a refetch brings, not be mistaken for the refetch.
    /// </summary>
    public Task<FetchResult>? TryFetchOnceAsync(
        Func<CancellationToken, Task<T>> fetch,
        int retryCount,
        TimeSpan retryDelay,
        BlexNetworkMode? networkMode)
        => FetchCore(fetch, retryCount, retryDelay, cancelRunning: false, once: true, networkMode, joinRunning: false);

    private Task<FetchResult>? FetchCore(
        Func<CancellationToken, Task<T>>? fetch,
        int retryCount,
        TimeSpan retryDelay,
        bool cancelRunning,
        bool once,
        BlexNetworkMode? networkMode,
        bool joinRunning)
    {
        TaskCompletionSource<FetchResult> done;
        CancellationTokenSource cts;
        CancellationTokenSource? superseded = null;
        int id;
        bool pausesOffline;
        lock (Client.Gate)
        {
            if (fetch is not null && !once)
                _fetch = fetch;

            if ((fetch ?? _fetch) is null || Client.IsDisposed)
                return Task.FromResult(default(FetchResult));

            if (_inFlight is not null && _done is not null)
            {
                if (!cancelRunning)
                    return joinRunning ? _inFlight : null;

                // Superseded: its outcome is ignored once it settles, whatever it turns out to be. The new fetch
                // takes over its completion, so whoever awaits it waits for an answer as of now, not for the
                // cancellation of the one it replaced. Cancelled below, outside the gate and only once the new
                // fetch id is in place: cancelling runs the old fetch's continuation inline.
                superseded = _cts;
                done = _done;
            }
            else
            {
                done = _done = new TaskCompletionSource<FetchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                _inFlight = done.Task;
            }

            id = ++_fetchId;
            _fetchInvalidations = Invalidations;
            cts = _cts = new CancellationTokenSource();
            _retryCount = retryCount;
            _retryDelay = retryDelay;
            _networkMode = networkMode;
            fetch = once ? fetch! : _fetch!;
            pausesOffline = Client.PausesOffline(networkMode);

            FetchStatus = pausesOffline && !Client.IsOnline ? BlexFetchStatus.Paused : BlexFetchStatus.Fetching;
            FailureCount = 0;
            FailureReason = null;

            // An entry that failed keeps its error, and its Error status, until the new answer lands: the retry
            // is a refetch of what is showing (IsRefetching), not a return to loading, so the error does not give
            // way to a spinner on every focus or mount only to come back when the retry fails too.
            if (!_hasData && Status != BlexQueryStatus.Error)
                Status = BlexQueryStatus.Pending;

            GcTimer?.Dispose();
            GcTimer = null;
            GcTicket = null;
        }

        try
        {
            superseded?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The superseded fetch settled on its own just now; its outcome is ignored all the same.
        }

        Notify();

        // Outside the gate: the fetch is user code, and runs up to its first await right here.
        _ = RunAsync(id, cts, fetch, retryCount, retryDelay, pausesOffline, done);
        return done.Task;
    }

    public override Task RefetchAsync(bool cancelRunning, Func<BlexQuery, bool>? observerFilter = null)
    {
        int retryCount;
        TimeSpan retryDelay;
        BlexNetworkMode? networkMode;
        Func<CancellationToken, Task<T>>? fetch = null;
        lock (Client.Gate)
        {
            if (observerFilter is not null && !_observers.Exists(o => o.Options.Enabled && observerFilter(o)))
                return Task.CompletedTask;

            // The retry policy of whoever observes it; with nobody left, the one the last fetch used.
            var options = _observers.Count > 0 ? _observers[0].Options : null;
            retryCount = options is null ? _retryCount : Client.RetryCountFor(options);
            retryDelay = options is null ? _retryDelay : Client.RetryDelayFor(options);
            networkMode = options is null ? _networkMode : options.NetworkMode;

            // Never fetched here -- handed over by the prerender, or set with SetQueryData, and observed while fresh:
            // an observer knows how -- an enabled one, whose fetch has what it needs.
            if (_fetch is null && _observers.Count > 0)
                fetch = (_observers.Find(o => o.Options.Enabled) ?? _observers[0]).Fetch;
        }

        return FetchAsync(fetch, retryCount, retryDelay, cancelRunning, networkMode: networkMode);
    }

    private async Task RunAsync(
        int id,
        CancellationTokenSource cts,
        Func<CancellationToken, Task<T>> fetch,
        int retryCount,
        TimeSpan retryDelay,
        bool pausesOffline,
        TaskCompletionSource<FetchResult> done)
    {
        var telemetry = BlexDiagnostics.StartQuery(Key);
        var outcome = "cancelled";
        Exception? failure = null;
        try
        {
            for (var failures = 0; ; failures++)
            {
                // Offline: wait for the network -- paused, not failed -- rather than spend the retries on requests that
                // cannot succeed. A retry waits the same way. Cancelling, superseding or evicting the fetch ends the wait.
                if (pausesOffline && !Client.IsOnline)
                {
                    SetFetchStatus(id, BlexFetchStatus.Paused);
                    try
                    {
                        await Client.WhenOnlineAsync(cts.Token).ConfigureAwait(true);
                    }
                    catch (OperationCanceledException)
                    {
                        SettleCancelled(id);
                        return;
                    }

                    SetFetchStatus(id, BlexFetchStatus.Fetching);
                }

                T result;
                try
                {
                    // Not ConfigureAwait(false): the commit notifies observers, whose components re-render
                    // through the dispatcher the fetch was started on.
                    result = await fetch(cts.Token).ConfigureAwait(true);
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    SettleCancelled(id);
                    return;
                }
                catch (Exception) when (cts.IsCancellationRequested)
                {
                    // Cancelled, and the fetch said so in its own words -- gRPC's RpcException(Cancelled), an IOException
                    // from an aborted stream: the failure is the cancellation's doing, not the server's answer, and is
                    // settled the way an OperationCanceledException would be.
                    SettleCancelled(id);
                    return;
                }
                catch (Exception ex)
                {
                    if (failures >= retryCount)
                    {
                        (outcome, failure) = ("error", ex);
                        CommitError(id, ex, failures + 1);
                        return;
                    }

                    if (!RecordFailure(id, ex, failures + 1))
                        return;

                    try
                    {
                        await Task.Delay(Backoff(retryDelay, failures), Client.TimeProvider, cts.Token).ConfigureAwait(true);
                    }
                    catch (OperationCanceledException)
                    {
                        SettleCancelled(id);
                        return;
                    }

                    continue;
                }

                // A fetch that ignored its token still finished after it was cancelled or superseded; what it
                // brought back is no longer anyone's answer.
                if (cts.IsCancellationRequested)
                {
                    SettleCancelled(id);
                }
                else
                {
                    outcome = "success";
                    CommitSuccess(id, result);
                }

                return;
            }
        }
        catch (Exception ex)
        {
            // Only an observer handler could get here, and those are isolated; this is the last line.
            Client.ReportError(ex, Key);
        }
        finally
        {
            bool latest;
            lock (Client.Gate)
            {
                latest = _fetchId == id;
                if (latest)
                {
                    _inFlight = null;
                    _done = null;
                    _cts = null;
                }
            }

            cts.Dispose();

            // A superseded fetch leaves the completion to the one that replaced it.
            if (latest)
            {
                done.TrySetResult(new FetchResult(outcome == "success", failure));
            }

            Client.OnFetchSettled(this);

            // Last, so nothing a telemetry listener does can keep the entry from settling.
            telemetry.Stop(outcome, failure);
        }
    }

    private void CommitSuccess(int id, T result)
    {
        // An equal result keeps the instance already cached, so a component comparing by reference does not
        // re-render for a refetch that brought back the same thing. Compared outside the gate: Equals is user code.
        T? cached;
        bool hadData;
        lock (Client.Gate)
        {
            cached = _data;
            hadData = _hasData;
        }

        var same = hadData && EqualityComparer<T>.Default.Equals(cached, result);

        lock (Client.Gate)
        {
            if (id != _fetchId)
                return;

            // Keep the cached instance only if it is still the one compared against.
            if (!(same && _hasData && ReferenceEquals(_data, cached)))
                _data = result;

            _hasData = true;
            DataUpdatedAt = Client.TimeProvider.GetUtcNow();
            Status = BlexQueryStatus.Success;
            Error = null;
            FailureCount = 0;
            FailureReason = null;
            FetchStatus = BlexFetchStatus.Idle;

            // Invalidated while it ran (an unobserved entry's fetch is not superseded): the answer predates the
            // change, so it is kept but stays stale, and whoever observes the entry next fetches again.
            IsInvalidated = Invalidations != _fetchInvalidations;
        }

        Notify();
    }

    private void CommitError(int id, Exception error, int failures)
    {
        lock (Client.Gate)
        {
            if (id != _fetchId)
                return;

            Error = error;
            Status = BlexQueryStatus.Error;
            FailureCount = failures;
            FailureReason = error;
            FetchStatus = BlexFetchStatus.Idle;

            // Data kept from an earlier success is still shown, but must not count as fresh.
            IsInvalidated = true;
        }

        Notify();
    }

    /// <summary>Records a failure that is about to be retried; <c>false</c> when the fetch was superseded meanwhile.</summary>
    private bool RecordFailure(int id, Exception error, int failures)
    {
        lock (Client.Gate)
        {
            if (id != _fetchId)
                return false;

            FailureCount = failures;
            FailureReason = error;
        }

        Notify();
        return true;
    }

    /// <summary>Moves the latest fetch between running and waiting for the network; ignored for a superseded one.</summary>
    private void SetFetchStatus(int id, BlexFetchStatus status)
    {
        lock (Client.Gate)
        {
            if (id != _fetchId || FetchStatus == status)
                return;

            FetchStatus = status;
        }

        Notify();
    }

    private void SettleCancelled(int id)
    {
        lock (Client.Gate)
        {
            if (id != _fetchId)
                return;

            FetchStatus = BlexFetchStatus.Idle;
            FailureCount = 0;
            FailureReason = null;
        }

        Notify();
    }

    public override void Cancel()
    {
        CancellationTokenSource? cts;
        lock (Client.Gate)
            cts = _cts;

        // Outside the gate: cancelling runs the fetch's continuation -- and the observers it notifies -- inline.
        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // It settled and was disposed in between; nothing left to cancel.
        }
    }

    /// <summary>Replaces the data, as a successful fetch would, without touching a fetch that is running.</summary>
    public void SetData(T data, DateTimeOffset? updatedAt = null)
    {
        lock (Client.Gate)
        {
            _data = data;
            _hasData = true;
            DataUpdatedAt = updatedAt ?? Client.TimeProvider.GetUtcNow();
            Status = BlexQueryStatus.Success;
            Error = null;
            IsInvalidated = false;
        }

        Notify();
    }

    /// <summary>Reads the data and when it was written, atomically.</summary>
    public bool TryGetData([MaybeNullWhen(false)] out T data, out DateTimeOffset? updatedAt)
    {
        lock (Client.Gate)
        {
            data = _data;
            updatedAt = DataUpdatedAt;
            return _hasData;
        }
    }

    /// <summary>
    /// Puts back what an optimistic update replaced -- or, when there was no data, forgets the optimistic value. The
    /// error and the stale mark come back too: data a failed refetch left behind is still the data of a failed refetch,
    /// and an invalidated query is still owed its refetch.
    /// </summary>
    public void Restore(bool hadData, T? data, DateTimeOffset? updatedAt, BlexQueryStatus status, Exception? error, bool invalidated)
    {
        lock (Client.Gate)
        {
            _data = data;
            _hasData = hadData;
            DataUpdatedAt = updatedAt;
            Status = status;
            Error = error;
            IsInvalidated = invalidated;
        }

        Notify();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "BlexJson.Options is the app's trimming seam (BlexJson.Configure sets a source-generated TypeInfoResolver), as for every store snapshot; a value that cannot be serialized is reported and left out of the handoff.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "BlexJson.Options is the app's trimming seam (BlexJson.Configure sets a source-generated TypeInfoResolver), as for every store snapshot.")]
    public override bool TrySerializeData(out JsonNode? data, out DateTimeOffset updatedAt)
    {
        T? value;
        lock (Client.Gate)
        {
            if (!_hasData || Status != BlexQueryStatus.Success || DataUpdatedAt is not { } at)
            {
                data = null;
                updatedAt = default;
                return false;
            }

            value = _data;
            updatedAt = at;
        }

        data = JsonSerializer.SerializeToNode(value, BlexJson.Options);
        return true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "BlexJson.Options is the app's trimming seam (BlexJson.Configure sets a source-generated TypeInfoResolver), as for every store snapshot; a payload that cannot be read is reported and ignored.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "BlexJson.Options is the app's trimming seam (BlexJson.Configure sets a source-generated TypeInfoResolver), as for every store snapshot.")]
    public override void Hydrate(JsonNode? data, DateTimeOffset updatedAt)
    {
        var value = data is null ? default : JsonSerializer.Deserialize<T>(data, BlexJson.Options);

        // Stamped by the server's clock: one running ahead of this one (a browser's, as often as not) would make the
        // data younger than it is -- fresh for the skew, even with no stale time at all. It is no newer than now here.
        var now = Client.TimeProvider.GetUtcNow();
        if (updatedAt > now)
            updatedAt = now;

        lock (Client.Gate)
        {
            // Newer data -- fetched here since -- wins over what the prerender saw.
            if (_hasData && DataUpdatedAt >= updatedAt)
                return;

            _data = value;
            _hasData = true;
            DataUpdatedAt = updatedAt;
            Status = BlexQueryStatus.Success;
            Error = null;
        }

        Notify();
    }

    public override void Notify()
    {
        BlexQuery<T>[] observers;
        lock (Client.Gate)
            observers = [.. _observers];

        foreach (var observer in observers)
            observer.OnEntryChanged();

        Client.OnEntryChanged();
    }
}
