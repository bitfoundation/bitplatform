using System.Runtime.CompilerServices;

namespace Bit.Bsqlite.Data;

/// <summary>Re-runs a query when watched tables are written.</summary>
public sealed class BsqliteLiveQuery<T> : IBsqliteLiveQuery<T>
{
    private readonly BsqliteConnection _connection;
    private readonly Func<CancellationToken, Task<T>> _execute;
    private readonly HashSet<string> _tables;
    private readonly List<TaskCompletionSource> _waiters = [];
    private readonly Action? _onDispose;
    private readonly Lock _refreshGate = new();

    // One run of the query at a time, explicit or not: an EF query runs on a context that allows one
    // operation at a time, and two runs finishing out of order would publish the older snapshot last.
    private readonly SemaphoreSlim _runGate = new(1, 1);
    private bool _refreshing;
    private bool _refreshRequested;
    private int _disposed;

    // Guarded by _waiters. Every published snapshot bumps the version, so an enumerator that was
    // busy yielding while a refresh completed can tell it missed one and catch up rather than
    // waiting for the next.
    private long _version;
    private T? _latest;

    /// <summary>How many times one notification's refresh is attempted before it is given up on.</summary>
    private const int RefreshAttempts = 2;

    public BsqliteLiveQuery(
        BsqliteConnection connection,
        Func<CancellationToken, Task<T>> execute,
        IEnumerable<string> tables)
        : this(connection, execute, tables, onDispose: null)
    {
    }

    /// <summary>
    /// As the public constructor, plus <paramref name="onDispose"/>: runs once when the live query
    /// is disposed, after it has stopped listening to the connection. Lets a caller that wired the
    /// query to something else - the EF entry point subscribes to the context's SaveChanges events -
    /// release that too.
    /// </summary>
    internal BsqliteLiveQuery(
        BsqliteConnection connection,
        Func<CancellationToken, Task<T>> execute,
        IEnumerable<string> tables,
        Action? onDispose)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(tables);

        _connection = connection;
        _execute = execute;
        _tables = new HashSet<string>(tables, StringComparer.OrdinalIgnoreCase);
        _onDispose = onDispose;
        _connection.TablesChanged += OnTablesChanged;
    }

    /// <inheritdoc />
    public event EventHandler<T>? Changed;

    /// <inheritdoc />
    public event EventHandler<Exception>? RefreshFailed;

    /// <inheritdoc />
    public T? Current { get; private set; }

    /// <summary>The tables whose writes trigger a refresh. Empty means every write does.</summary>
    public IReadOnlySet<string> Tables => _tables;

    /// <inheritdoc />
    public async Task<T> RefreshAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        await _runGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Again: disposal may have come while this run waited its turn.
            ObjectDisposedException.ThrowIf(_disposed != 0, this);

            var snapshot = await _execute(cancellationToken).ConfigureAwait(false);
            Current = snapshot;

            // Enumerators first, so a Changed handler that throws cannot starve them of a result.
            Publish(snapshot);
            Changed?.Invoke(this, snapshot);
            return snapshot;
        }
        finally
        {
            _runGate.Release();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Yields the current result first, then every result of a later refresh. A refresh that
    /// completes while the consumer is still processing the previous item is not lost: the next
    /// iteration yields its snapshot at once instead of waiting for another write.
    /// </remarks>
    public async IAsyncEnumerable<T> WithCancellation([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var first = await RefreshAsync(cancellationToken).ConfigureAwait(false);

        // Captured before yielding: the consumer may take a while to come back for the next item.
        long seenVersion;
        lock (_waiters)
        {
            seenVersion = _version;
        }

        yield return first;

        while (!cancellationToken.IsCancellationRequested)
        {
            TaskCompletionSource? waiter = null;
            bool disposed;
            lock (_waiters)
            {
                // Checked under the lock DisposeAsync cancels the waiters under: a waiter added after
                // that would never be completed, and a consumer that was busy when the query was
                // disposed would come back to wait forever.
                disposed = _disposed != 0;
                if (!disposed && _version == seenVersion)
                {
                    waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    _waiters.Add(waiter);
                }
            }

            if (disposed)
            {
                yield break;
            }

            if (waiter is not null)
            {
                using var registration = cancellationToken.Register(() => waiter.TrySetCanceled(cancellationToken));
                try
                {
                    await waiter.Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }
            }

            // Always the newest snapshot, whatever woke us: an intermediate one is stale by now.
            T snapshot;
            lock (_waiters)
            {
                seenVersion = _version;
                snapshot = _latest!;
            }

            yield return snapshot;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        _connection.TablesChanged -= OnTablesChanged;
        lock (_waiters)
        {
            foreach (var waiter in _waiters)
            {
                waiter.TrySetCanceled();
            }

            _waiters.Clear();
        }

        _onDispose?.Invoke();
        return ValueTask.CompletedTask;
    }

    private void OnTablesChanged(object? sender, BsqliteTablesChangedEventArgs e)
    {
        if (_tables.Count > 0 && !_tables.Overlaps(e.Tables))
        {
            return;
        }

        RequestRefresh();
    }

    /// <summary>
    /// Asks for a refresh, coalescing with one already in flight.
    /// </summary>
    /// <remarks>
    /// Refreshes are single-file because they usually run against an EF <c>DbContext</c>, which
    /// rejects a second concurrent operation. Overlapping notifications - a burst of writes, or a
    /// local write and another tab's arriving together - would otherwise throw and be swallowed,
    /// leaving the query showing data that is one write out of date. A request that arrives while a
    /// refresh is running therefore sets a flag the running refresh honours before it finishes,
    /// so the last state of the database is always the state that is read.
    /// </remarks>
    private void RequestRefresh()
    {
        lock (_refreshGate)
        {
            _refreshRequested = true;
            if (_refreshing)
            {
                return;
            }

            _refreshing = true;
        }

        _ = RefreshQuietlyAsync();
    }

    private async Task RefreshQuietlyAsync()
    {
        // Yield so the write that raised TablesChanged can finish (EF SaveChanges is still
        // consuming the insert result when the in-process transport notifies).
        await Task.Yield();

        while (true)
        {
            lock (_refreshGate)
            {
                if (!_refreshRequested || _disposed != 0)
                {
                    _refreshing = false;
                    return;
                }

                _refreshRequested = false;
            }

            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await RefreshAsync().ConfigureAwait(false);
                    break;
                }
                catch (Exception) when (_disposed != 0)
                {
                    // Disposed mid-refresh: nobody is listening for the result, or for its failure.
                    break;
                }
                catch (Exception) when (attempt < RefreshAttempts - 1)
                {
                    // Almost always the write that triggered this is still finishing and the
                    // context is busy. Give it the rest of the turn and read again.
                    await Task.Yield();
                }
                catch (Exception ex)
                {
                    // A failed refresh must not take down the write that triggered it, but it must
                    // not vanish either: there is no caller to throw to, so it is reported.
                    ReportRefreshFailure(ex);
                    break;
                }
            }
        }
    }

    private void ReportRefreshFailure(Exception exception)
    {
        try
        {
            RefreshFailed?.Invoke(this, exception);
        }
        catch (Exception)
        {
            // A handler that throws is not allowed to stop later refreshes.
        }
    }

    private void Publish(T snapshot)
    {
        List<TaskCompletionSource> waiters;
        lock (_waiters)
        {
            _version++;
            _latest = snapshot;
            waiters = [.. _waiters];
            _waiters.Clear();
        }

        foreach (var waiter in waiters)
        {
            waiter.TrySetResult();
        }
    }
}
