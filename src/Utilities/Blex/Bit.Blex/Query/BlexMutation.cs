using System.ComponentModel;

namespace Bit.Blex;

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

    /// <summary>The latest call is pending, waiting for the network to come back before it is sent.</summary>
    public bool IsPaused { get; private set; }

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
            result = await SendAsync(call, arg, cancellationToken).ConfigureAwait(true);
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
                // Offline, the refetches wait for the network; the change itself has settled, and so does the mutation.
                var refetches = Task.WhenAll(keys.Select(key => _client.InvalidateAsync(key)));
                await Task.WhenAny(refetches, _client.WhenOfflineAsync()).ConfigureAwait(true);
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

    private async Task<TResult> SendAsync(int call, TArg arg, CancellationToken cancellationToken)
    {
        for (var failures = 0; ; failures++)
        {
            // Offline: held until the network returns, then sent -- a retry waits the same way.
            if (_client.PausesOffline(_options.NetworkMode) && !_client.IsOnline)
            {
                SetPaused(call, true);
                try
                {
                    await _client.WhenOnlineAsync(cancellationToken).ConfigureAwait(true);
                }
                finally
                {
                    SetPaused(call, false);
                }
            }

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

    private void SetPaused(int call, bool paused)
    {
        if (call != _call || IsPaused == paused)
            return;

        IsPaused = paused;
        RaiseChanged();
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
        IsPaused = false;
        RaiseChanged();
    }

    private void RaiseChanged() => _client.RaiseIsolated(Changed, PropertyChanged, this, null);

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
