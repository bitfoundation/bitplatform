using System.ComponentModel;

namespace Bit.Blex.Query;

/// <summary>Something a failed mutation undoes: an optimistic cache update, or any change of your own.</summary>
public interface IBlexRollback
{
    /// <summary>Undoes the change. Called at most once.</summary>
    void Rollback();
}

/// <summary>Builds and combines <see cref="IBlexRollback"/>s.</summary>
public static class BlexRollback
{
    /// <summary>A rollback that runs <paramref name="rollback"/>.</summary>
    public static IBlexRollback From(Action rollback)
    {
        ArgumentNullException.ThrowIfNull(rollback);
        return new DelegateRollback(rollback);
    }

    /// <summary>One rollback for several changes, undone in reverse order -- the last applied, the first undone.</summary>
    public static IBlexRollback Combine(params IBlexRollback?[] rollbacks)
    {
        ArgumentNullException.ThrowIfNull(rollbacks);
        return From(() =>
        {
            for (var i = rollbacks.Length - 1; i >= 0; i--)
                rollbacks[i]?.Rollback();
        });
    }

    private sealed class DelegateRollback(Action rollback) : IBlexRollback
    {
        private Action? _rollback = rollback;

        public void Rollback() => Interlocked.Exchange(ref _rollback, null)?.Invoke();
    }
}

/// <summary>
/// An optimistic update of one cached query (<see cref="BlexQueryClient.Optimistic{T}"/>), and the way
/// back from it.
/// </summary>
/// <typeparam name="T">The query's data.</typeparam>
public sealed class BlexOptimisticUpdate<T> : IBlexRollback
{
    private readonly BlexQueryClient _client;
    private readonly BlexQueryEntry<T> _entry;
    private readonly bool _hadData;
    private readonly T? _previous;
    private readonly DateTimeOffset? _previousUpdatedAt;
    private readonly BlexQueryStatus _previousStatus;
    private int _done;

    internal BlexOptimisticUpdate(
        BlexQueryClient client,
        BlexQueryEntry<T> entry,
        bool hadData,
        T? previous,
        DateTimeOffset? previousUpdatedAt,
        BlexQueryStatus previousStatus,
        T value)
    {
        _client = client;
        _entry = entry;
        _hadData = hadData;
        _previous = previous;
        _previousUpdatedAt = previousUpdatedAt;
        _previousStatus = previousStatus;
        Value = value;
    }

    /// <summary>The key of the updated query.</summary>
    public BlexQueryKey Key => _entry.Key;

    /// <summary>The optimistic value the update wrote.</summary>
    public T Value { get; }

    /// <summary>
    /// Restores the data from before the update -- if the cache still holds this update's value. When
    /// something newer has landed since, the key is invalidated instead, so it is refetched rather than
    /// overwritten with a snapshot older than what replaced it. Called at most once; later calls do nothing.
    /// </summary>
    public void Rollback()
    {
        if (Interlocked.Exchange(ref _done, 1) != 0 || _client.IsDisposed)
            return;

        // Read under the gate (inside TryGetData), compared outside it: Equals is user code.
        var current = _entry.TryGetData(out var data, out _) && EqualityComparer<T>.Default.Equals(data, Value);

        if (current)
        {
            _entry.Restore(_hadData, _previous, _previousUpdatedAt, _previousStatus);
            if (!_hadData)
                _entry.Invalidate();
        }
        else
        {
            _ = _client.InvalidateAsync(_entry.Key, exact: true);
        }
    }
}

/// <summary>Callbacks, optimistic updates and cache invalidation around a <see cref="BlexMutation{TArg, TResult}"/>.</summary>
/// <typeparam name="TArg">What the mutation is called with.</typeparam>
/// <typeparam name="TResult">What the server answers.</typeparam>
public sealed record BlexMutationOptions<TArg, TResult>
{
    /// <summary>
    /// Runs before the mutation is sent -- the place for an optimistic update. Return the
    /// <see cref="IBlexRollback"/> that undoes it (<see cref="BlexQueryClient.Optimistic{T}"/> returns one;
    /// <see cref="BlexRollback.Combine"/> joins several) and a failure rolls it back automatically.
    /// </summary>
    public Func<TArg, IBlexRollback?>? OnMutate { get; init; }

    /// <summary>Runs after the server accepted the change, with its answer -- the place to <see cref="BlexQueryClient.SetQueryData{T}(BlexQueryKey, T)">write it into the cache</see>.</summary>
    public Action<TResult, TArg>? OnSuccess { get; init; }

    /// <summary>Runs after the change failed (and its optimistic update was rolled back).</summary>
    public Action<Exception, TArg>? OnError { get; init; }

    /// <summary>Runs last, whichever way the change went.</summary>
    public Action<TArg>? OnSettled { get; init; }

    /// <summary>
    /// Keys to invalidate once the change settles, success or failure -- the server's view has moved either
    /// way. The mutation stays pending until the observed ones have refetched, so the UI never shows
    /// "saved" next to the list from before the save.
    /// </summary>
    public IReadOnlyList<BlexQueryKey>? Invalidates { get; init; }

    /// <summary>How many times a failed call is retried. Defaults to <c>0</c>: a change is not repeated unless asked to.</summary>
    public int RetryCount { get; init; }

    /// <summary>The wait before the first retry; it doubles before each further one, up to 30 seconds.</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// A change sent to the server, with the status of its latest call and the cache updates around it.
/// Create it with <see cref="BlexQueryClient.Mutation{TArg, TResult}"/>.
/// </summary>
/// <typeparam name="TArg">What the mutation is called with.</typeparam>
/// <typeparam name="TResult">What the server answers.</typeparam>
/// <remarks>
/// Calls may overlap; each runs to the end with its own optimistic update and rollback, and the state
/// properties follow the latest call. <see cref="MutateAsync"/> never throws -- a failure lands in
/// <see cref="Error"/>, like an effect's -- so it can be awaited straight from an event handler.
/// </remarks>
public sealed class BlexMutation<TArg, TResult> : INotifyPropertyChanged
{
    private static readonly PropertyChangedEventArgs AllPropertiesChanged = new(string.Empty);

    private readonly BlexQueryClient _client;
    private readonly Func<TArg, CancellationToken, Task<TResult>> _mutate;
    private readonly BlexMutationOptions<TArg, TResult> _options;
    private int _call;

    internal BlexMutation(BlexQueryClient client, Func<TArg, CancellationToken, Task<TResult>> mutate, BlexMutationOptions<TArg, TResult> options)
    {
        _client = client;
        _mutate = mutate;
        _options = options;
    }

    /// <summary>How the latest call went.</summary>
    public BlexMutationStatus Status { get; private set; }

    /// <summary>The server's answer to the latest call, once it succeeded.</summary>
    public TResult? Data { get; private set; }

    /// <summary>Why the latest call failed.</summary>
    public Exception? Error { get; private set; }

    /// <summary>What the latest call was made with.</summary>
    public TArg? Arg { get; private set; }

    /// <summary>Never called, or reset since.</summary>
    public bool IsIdle => Status == BlexMutationStatus.Idle;

    /// <summary>The latest call is running.</summary>
    public bool IsPending => Status == BlexMutationStatus.Pending;

    /// <summary>The latest call succeeded.</summary>
    public bool IsSuccess => Status == BlexMutationStatus.Success;

    /// <summary>The latest call failed.</summary>
    public bool IsError => Status == BlexMutationStatus.Error;

    /// <summary>Raised after the state changed. Handler exceptions are isolated and reported.</summary>
    public event Action? Changed;

    /// <summary>Raised with <see cref="Changed"/>, always with an empty property name, for XAML bindings.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sends the change: runs <see cref="BlexMutationOptions{TArg, TResult}.OnMutate"/>, calls the server
    /// (retrying as configured), then the success or error callback -- rolling back the optimistic update on
    /// failure -- invalidates the configured keys, and runs the settled callback. Never throws.
    /// </summary>
    /// <param name="arg">What to send.</param>
    /// <param name="cancellationToken">
    /// Cancels the call. A cancelled call rolls back like a failed one, but is not an error: the mutation
    /// returns to <see cref="BlexMutationStatus.Idle"/>.
    /// </param>
    public async Task MutateAsync(TArg arg, CancellationToken cancellationToken = default)
    {
        var call = ++_call;
        Commit(call, BlexMutationStatus.Pending, arg, default, null);

        IBlexRollback? rollback = null;
        var succeeded = false;
        var cancelled = false;
        TResult? result = default;
        Exception? error = null;
        try
        {
            rollback = _options.OnMutate?.Invoke(arg);
            result = await SendAsync(arg, cancellationToken).ConfigureAwait(true);
            succeeded = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancelled = true;
        }
        catch (Exception ex)
        {
            error = ex;
        }

        if (!succeeded)
            Isolated(() => rollback?.Rollback());

        if (succeeded)
            Isolated(() => _options.OnSuccess?.Invoke(result!, arg));
        else if (error is not null)
            Isolated(() => _options.OnError?.Invoke(error, arg));

        if (_options.Invalidates is { Count: > 0 } keys && !_client.IsDisposed)
        {
            try
            {
                await Task.WhenAll(keys.Select(key => _client.InvalidateAsync(key))).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _client.ReportError(ex, null);
            }
        }

        if (succeeded)
            Commit(call, BlexMutationStatus.Success, arg, result, null);
        else if (cancelled)
            Commit(call, BlexMutationStatus.Idle, arg, default, null);
        else
            Commit(call, BlexMutationStatus.Error, arg, default, error);

        Isolated(() => _options.OnSettled?.Invoke(arg));
    }

    /// <summary>Returns to <see cref="BlexMutationStatus.Idle"/>, forgetting the latest call's outcome; a call still running no longer reports to it.</summary>
    public void Reset() => Commit(++_call, BlexMutationStatus.Idle, default, default, null);

    private async Task<TResult> SendAsync(TArg arg, CancellationToken cancellationToken)
    {
        for (var failures = 0; ; failures++)
        {
            try
            {
                return await _mutate(arg, cancellationToken).ConfigureAwait(true);
            }
            catch (Exception) when (failures < _options.RetryCount && !cancellationToken.IsCancellationRequested)
            {
            }

            await Task.Delay(BlexQueryEntry.Backoff(_options.RetryDelay, failures), _client.TimeProvider, cancellationToken).ConfigureAwait(true);
        }
    }

    private void Commit(int call, BlexMutationStatus status, TArg? arg, TResult? data, Exception? error)
    {
        // A call superseded by a later one (or by Reset) no longer speaks for the mutation.
        if (call != _call)
            return;

        Status = status;
        Arg = arg;
        Data = data;
        Error = error;

        if (Changed is { } changed)
        {
            foreach (var handler in changed.GetInvocationList())
                Isolated((Action)handler);
        }

        if (PropertyChanged is { } propertyChanged)
        {
            foreach (var handler in propertyChanged.GetInvocationList())
                Isolated(() => ((PropertyChangedEventHandler)handler)(this, AllPropertiesChanged));
        }
    }

    private void Isolated(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _client.ReportError(ex, null);
        }
    }
}
