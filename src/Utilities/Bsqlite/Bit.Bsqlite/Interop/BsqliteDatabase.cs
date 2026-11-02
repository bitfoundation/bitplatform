using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// One named database for the lifetime of the application: opened on first use, shared by every
/// caller, and closable so that admin operations can replace the file underneath.
/// </summary>
/// <remarks>
/// <para>
/// A Blazor WebAssembly application is one process with one user, so the natural shape is a
/// singleton session per database rather than a pool. This type is that singleton. It is what
/// the EF Core integration's context factory opens through, and what an application should inject
/// when it wants the connection directly.
/// </para>
/// <para>
/// <see cref="CloseAsync"/> tears the worker down. Subscribers to <see cref="Closing"/> drop the
/// connections and live queries they hold; subscribers to <see cref="Opened"/> reopen. That is the
/// protocol for moving a database between backends or importing a file over it.
/// </para>
/// </remarks>
public sealed class BsqliteDatabase : IAsyncDisposable, IDisposable
{
    private readonly IBsqliteSessionFactory _sessions;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Set while <see cref="WhileClosedAsync"/> runs its work, which holds <see cref="_gate"/>.</summary>
    private readonly AsyncLocal<bool> _closedForWork = new();
    private BsqliteSession? _session;
    private bool _disposed;

    public BsqliteDatabase(IBsqliteSessionFactory sessions, string databaseName)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        _sessions = sessions;
        Name = databaseName;
    }

    /// <summary>The database this instance opens.</summary>
    public string Name { get; }

    /// <summary>The open session, or <see langword="null"/> before the first open or after a close.</summary>
    public BsqliteSession? Session => _session;

    /// <summary>The open connection, or <see langword="null"/> when the database is not open.</summary>
    public BsqliteConnection? Connection => _session?.Connection;

    /// <summary>Why the current backend was chosen, when the database is open.</summary>
    public BsqliteStorageResolution? Resolution => _session?.Resolution;

    /// <summary>The provider holding the data, when the database is open.</summary>
    public IBsqliteStorageProvider? Provider => _session?.Resolution.Provider;

    /// <summary>Whether a session is currently open.</summary>
    public bool IsOpen => _session is not null;

    /// <summary>
    /// Raised before the session is closed. Handlers must release every connection, context, and
    /// live query bound to it; the worker is terminated once they return.
    /// </summary>
    public event Func<Task>? Closing;

    /// <summary>Raised after a session opens, including the first time.</summary>
    public event Func<Task>? Opened;

    /// <summary>Opens the database if it is not open and returns the session.</summary>
    public async ValueTask<BsqliteSession> OpenAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_session is { } open)
        {
            return open;
        }

        // The gate is WhileClosedAsync's for as long as its work runs, so waiting on it here would
        // never end.
        if (_closedForWork.Value)
        {
            throw new InvalidOperationException(
                $"The database '{Name}' cannot be opened from inside WhileClosedAsync: the work runs with "
                + "the database closed, and the database reopens once the work has finished.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var opened = false;
        try
        {
            if (_session is null)
            {
                _session = await _sessions.OpenAsync(Name, cancellationToken).ConfigureAwait(false);
                opened = true;
            }

            return _session;
        }
        finally
        {
            _gate.Release();

            if (opened)
            {
                await RaiseAsync(Opened).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Closes the session and terminates its worker, after giving <see cref="Closing"/> subscribers
    /// the chance to let go. A no-op when the database is not open.
    /// </summary>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_session is null)
        {
            return;
        }

        await RaiseAsync(Closing).ConfigureAwait(false);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_session is { } session)
            {
                _session = null;
                await session.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Closes and reopens, which is how a changed binding or an imported file takes effect.</summary>
    public async Task<BsqliteSession> ReopenAsync(CancellationToken cancellationToken = default)
    {
        await CloseAsync(cancellationToken).ConfigureAwait(false);
        return await OpenAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs <paramref name="work"/> with the database closed and reopens afterwards, so an admin
    /// operation never races the worker that holds the file open.
    /// </summary>
    /// <remarks>
    /// The database stays closed for the whole of <paramref name="work"/>: an <see cref="OpenAsync"/>
    /// from anywhere else - a page loading, an <see cref="Opened"/> handler still running from the last
    /// open - waits until the work is done and the database has reopened, rather than opening it on the
    /// binding the work is about to change. The work itself must not open this database; it gets an
    /// <see cref="InvalidOperationException"/> if it tries.
    /// </remarks>
    public async Task WhileClosedAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var wasOpen = false;
        var reopened = false;
        try
        {
            // Decided under the gate, so an open still under way when this was called has finished and is
            // announced and closed like any other. Closing handlers still find the session open: an
            // OpenAsync from one returns it without waiting on the gate.
            if (_session is { } session)
            {
                wasOpen = true;
                await RaiseAsync(Closing).ConfigureAwait(false);
                _session = null;
                await session.DisposeAsync().ConfigureAwait(false);
            }

            _closedForWork.Value = true;
            try
            {
                await work(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _closedForWork.Value = false;
            }

            if (wasOpen)
            {
                _session = await _sessions.OpenAsync(Name, cancellationToken).ConfigureAwait(false);
                reopened = true;
            }
        }
        finally
        {
            _gate.Release();

            if (reopened)
            {
                await RaiseAsync(Opened).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Reads the open database out as a SQLite file image, consistent as of the last committed
    /// write, without closing it.
    /// </summary>
    public async Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
    {
        var session = await OpenAsync(cancellationToken).ConfigureAwait(false);
        return await session.Connection.ExportAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_session is { } session)
        {
            _session = null;
            await session.DisposeAsync().ConfigureAwait(false);
        }

        _gate.Dispose();
    }

    /// <summary>
    /// Synchronous disposal, for containers that dispose synchronously. The worker is torn down
    /// asynchronously in the background; prefer <see cref="DisposeAsync"/> where possible.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ = DisposeAsync().AsTask();
    }

    private static async Task RaiseAsync(Func<Task>? handlers)
    {
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<Task>>())
        {
            await handler().ConfigureAwait(false);
        }
    }
}
