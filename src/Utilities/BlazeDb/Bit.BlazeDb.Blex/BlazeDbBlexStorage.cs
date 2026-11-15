using Bit.Blex;

namespace Bit.BlazeDb.Blex;

/// <summary>
/// An <see cref="IBlexStorage"/> that keeps Blex's persisted state in a BlazeDb table (<see cref="BlazeDbBlexEntry"/>), so
/// it lives in the same database as the application's data: one write-ahead log, one backup, one key for encryption at
/// rest, and the browser's quota handling - on OPFS or IndexedDB in the browser, in files on the desktop and the server.
/// </summary>
/// <remarks>
/// <para>
/// Blex stays in charge of state: what is persisted, when, how it is serialized and protected. This class only stores the
/// strings it is handed, one row per key. A save commits at once in memory; with <see cref="FlushOnSave"/> (the default) it
/// also waits until the write is on storage, so Blex reports a save as done only once it is durable. A value equal to the
/// one stored is not written again.
/// </para>
/// <para>
/// In a read-only replica tab saves fail - the writer tab owns the database - and Blex reports them through its error
/// handling; the replica re-reads every store when it reloads from the writer's checkpoints
/// (<see cref="IBlexExternalStorageChange"/>). A write to the table that this instance did not make - another instance
/// over the same database, or the application's own code - is reported to Blex as well, key by key.
/// </para>
/// <para>
/// The rows are the database's, not a user's: every instance over one database reads and writes the same ones. That is
/// what a browser tab's database is - one user's - but on a Blazor Server host, where every circuit would otherwise
/// share them, give each user a database of their own (or keep Blex's per-browser storage there).
/// </para>
/// </remarks>
public sealed class BlazeDbBlexStorage : IBlexStorage, IBlexExternalStorageChange, IAsyncDisposable
{
    private readonly Func<CancellationToken, ValueTask<BlazeDbDatabase>> _database;
    private readonly SemaphoreSlim _opening = new(1, 1);
    private BlazeDbTable<string, BlazeDbBlexEntry>? _table;
    private bool _watching;

    /// <summary>Stores state in <paramref name="database"/>, which has the table registered (<see cref="BlazeDbBlexExtensions.AddBlexState"/>).</summary>
    public BlazeDbBlexStorage(BlazeDbDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = _ => ValueTask.FromResult(database);
    }

    /// <summary>
    /// Stores state in the database <paramref name="database"/> opens, on first use - the way an application that opens its
    /// database lazily, or downloads BlazeDb only when it needs it, hands it over. A failed open is tried again on the next use.
    /// </summary>
    public BlazeDbBlexStorage(Func<CancellationToken, ValueTask<BlazeDbDatabase>> database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <summary>
    /// Whether a save waits for the write to reach storage before it completes. On by default: Blex debounces its saves, so
    /// waiting costs one flush per burst of changes, and a save it reports done is one a closed tab keeps. Off, a save is
    /// durable within the database's <see cref="BlazeDbDatabaseOptions.FlushInterval"/>.
    /// </summary>
    public bool FlushOnSave { get; init; } = true;

    /// <inheritdoc />
    public event Action<string?>? ExternalChange;

    /// <inheritdoc />
    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var table = await TableAsync(cancellationToken).ConfigureAwait(false);
        return table.TryGet(key, out var entry) ? entry.Value : null;
    }

    /// <inheritdoc />
    public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        var table = await TableAsync(cancellationToken).ConfigureAwait(false);
        if (!table.TryGet(key, out var current) || current.Value != value)
        {
            Write(() => table.Upsert(new BlazeDbBlexEntry { Key = key, Value = value, UpdatedAt = DateTime.UtcNow }));
        }
        // Even for a value already stored: an earlier save of it may have committed and then failed to flush, and a save
        // reported done has to be on storage. With nothing buffered the flush costs nothing.
        await FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var table = await TableAsync(cancellationToken).ConfigureAwait(false);
        Write(() => table.Delete(key));
        await FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts telling Blex when the state was replaced from outside: a replica tab reloading from the writer's checkpoint.
    /// The application's own saves are never reported back.
    /// </summary>
    public async ValueTask StartWatchingAsync(CancellationToken cancellationToken = default)
    {
        var table = await TableAsync(cancellationToken).ConfigureAwait(false);
        lock (_opening)
        {
            if (_watching)
            {
                return;
            }
            _watching = true;
        }
        table.Changed += OnChanged;
    }

    private void OnChanged(object? sender, BlazeDbTableChangedEventArgs<string, BlazeDbBlexEntry> args)
    {
        if (args.IsReset)
        {
            // A replica reloaded from the writer's checkpoint: anything may have changed.
            ExternalChange?.Invoke(null);
            return;
        }
        // A commit is delivered on the thread that made it, before the write returns, so this instance's own writes
        // arrive while it is still writing - and are not echoed back. Any other commit to the table - another instance
        // over the same database (a second scope), or the application's own code - is reported per key.
        if (_writingThread == Environment.CurrentManagedThreadId)
        {
            return;
        }
        foreach (var change in args.Changes)
        {
            ExternalChange?.Invoke(change.Key);
        }
    }

    // The thread this instance is writing on, while it is; 0 otherwise.
    private volatile int _writingThread;

    private void Write(Action write)
    {
        _writingThread = Environment.CurrentManagedThreadId;
        try
        {
            write();
        }
        finally
        {
            _writingThread = 0;
        }
    }

    private async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        if (FlushOnSave)
        {
            var database = await _database(cancellationToken).ConfigureAwait(false);
            await database.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<BlazeDbTable<string, BlazeDbBlexEntry>> TableAsync(CancellationToken cancellationToken)
    {
        if (_table is { } table)
        {
            return table;
        }
        await _opening.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_table is null)
            {
                var database = await _database(cancellationToken).ConfigureAwait(false);
                try
                {
                    _table = database.GetTable(BlazeDbBlexEntry.Table);
                }
                catch (BlazeDbException ex)
                {
                    throw new InvalidOperationException(
                        "The BlazeDb database Blex persists to has no Blex state table. Register it where the database is " +
                        "opened: new BlazeDbDatabaseOptions().AddBlexState().", ex);
                }
            }
            return _table;
        }
        finally
        {
            _opening.Release();
        }
    }

    /// <summary>Stops watching the table. The database is the application's, and stays open.</summary>
    public ValueTask DisposeAsync()
    {
        if (_watching && _table is { } table)
        {
            table.Changed -= OnChanged;
        }
        _opening.Dispose();
        return ValueTask.CompletedTask;
    }
}
