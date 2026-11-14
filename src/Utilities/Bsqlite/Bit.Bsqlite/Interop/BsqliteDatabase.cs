using System.Runtime.ExceptionServices;
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

    /// <summary>
    /// Set while code this instance calls back runs holding <see cref="_gate"/> - <see cref="Closing"/> handlers
    /// and <see cref="WhileClosedAsync"/> work - so a <see cref="DisposeAsync"/> from there does not wait for a
    /// gate its own caller holds.
    /// </summary>
    private readonly AsyncLocal<bool> _holdingGate = new();
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
            // Disposal waits for the gate, so an open that was waiting for it when the database was disposed
            // gets here after - and a session opened now would belong to nobody.
            ObjectDisposedException.ThrowIf(_disposed, this);

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

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Under the gate, so the session Closing announces is the one disposed: decided before it, a
            // WhileClosedAsync could reopen in between, and the session its Opened handlers attached to
            // would be disposed without a Closing of its own. Closing handlers still find the session open:
            // an OpenAsync from one returns it without waiting on the gate.
            if (_session is { } session)
            {
                await RaiseUnderGateAsync(Closing).ConfigureAwait(false);
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
    public Task WhileClosedAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        return RunClosedAsync((_, ct) => work(ct), resolve: false, reopen: null, cancellationToken);
    }

    /// <summary>
    /// Replaces the database with <paramref name="image"/> - a backup taken with <see cref="ExportAsync"/>, or
    /// any SQLite database file - and reopens it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The image is checked before anything is touched: a file that is not a SQLite database, or is cut
    /// short, is refused with <see cref="BsqliteCorruptDatabaseException"/> and the database is left as it was.
    /// A file last written in write-ahead-log mode - every database EF Core creates on a server or a desktop
    /// - is marked for a rollback journal on the way in, which is the only mode a web VFS can open.
    /// </para>
    /// <para>
    /// The database is closed around the import, as <see cref="WhileClosedAsync"/> closes it: <see cref="Closing"/>
    /// handlers let go first, and <see cref="Opened"/> handlers see the restored database. It is imported onto
    /// the backend that holds the database now, and read back to check that every byte landed. When the import
    /// or that check fails, the database as it was is put back where it can be read out first, and a database
    /// that was open is reopened either way. An EF Core
    /// context factory applies the migrations the restored database is missing the next time it creates a
    /// context, so a backup from an earlier release of the app opens on the current model.
    /// </para>
    /// <para>
    /// Other tabs that have the database open are not closed: close them first, or restore before they open
    /// it. On OPFS, IndexedDB and a Blazor Hybrid file the import replaces the database in one step; on Cache
    /// Storage an import cut short - the tab closed part way - leaves neither the old database nor the new one.
    /// </para>
    /// </remarks>
    /// <exception cref="BsqliteCorruptDatabaseException"><paramref name="image"/> is empty, or not a whole SQLite database.</exception>
    /// <exception cref="NotSupportedException">The database lives on a backend that does not persist.</exception>
    public Task RestoreAsync(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Before the database closes: a bad image never costs the application its open session.
        if (image.IsEmpty)
        {
            // An empty file is a valid empty database - and restoring one would wipe this one. A failed upload
            // looks exactly like that; DeleteAsync is the way to start over.
            throw new BsqliteCorruptDatabaseException(
                "The backup is empty. Restoring it would leave an empty database; call DeleteAsync to start over.");
        }

        var prepared = BsqliteDatabaseImage.Prepare(image, "The backup");

        // And before closing, too, where the session says where the database lives: closing a database that
        // does not persist is what loses its data.
        if (_session?.Resolution.Provider is { Capabilities.IsPersistent: false } volatileProvider)
        {
            throw NotPersistent(volatileProvider);
        }

        return RunClosedAsync(async (resolution, ct) =>
        {
            var provider = resolution!.Provider;
            if (!provider.Capabilities.IsPersistent)
            {
                throw NotPersistent(provider);
            }

            var previous = await ReadForRollbackAsync(provider, ct).ConfigureAwait(false);
            try
            {
                await provider.Admin.ImportAsync(Name, prepared, ct).ConfigureAwait(false);

                var copy = await provider.Admin.ExportAsync(Name, ct).ConfigureAwait(false);
                if (!copy.AsSpan().SequenceEqual(prepared.Span))
                {
                    throw new BsqliteCorruptDatabaseException(
                        $"The restored copy of '{Name}' on '{provider.Name}' does not match the backup.");
                }
            }
            catch (Exception failure) when (previous.CanRollBack)
            {
                try
                {
                    // Not on the caller's token: a restore cancelled part way is exactly what has to be undone.
                    if (previous.Image is { } image)
                    {
                        await provider.Admin.ImportAsync(Name, image, CancellationToken.None).ConfigureAwait(false);
                    }
                    else
                    {
                        await provider.Admin.DeleteAsync(Name, CancellationToken.None).ConfigureAwait(false);
                    }
                }
                catch (Exception rollbackFailure)
                {
                    throw new AggregateException(
                        $"Restoring '{Name}' on '{provider.Name}' failed, and so did putting the database back as it was.",
                        failure,
                        rollbackFailure);
                }

                throw;
            }
        }, resolve: true, reopen: true, cancellationToken);
    }

    /// <summary>
    /// The database as it is before a restore overwrites it, to put back if the restore fails: its image, or
    /// none when there is no database yet - one to delete again. Nothing to roll back to when it cannot be read
    /// out - a journal holding a write cut short, say - which must not stop a restore meant to replace it.
    /// </summary>
    private async Task<(bool CanRollBack, byte[]? Image)> ReadForRollbackAsync(
        IBsqliteStorageProvider provider,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.Admin.ExistsAsync(Name, cancellationToken).ConfigureAwait(false)
                ? (true, await provider.Admin.ExportAsync(Name, cancellationToken).ConfigureAwait(false))
                : (true, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (false, null);
        }
    }

    /// <summary>Its admin store is not where a connection's data lives: an import would never be read.</summary>
    private NotSupportedException NotPersistent(IBsqliteStorageProvider provider)
        => new($"The database '{Name}' lives on '{provider.Name}', which does not persist, so it cannot be restored "
            + "from a backup. Restore onto a persistent backend.");

    /// <summary>
    /// Closes the database and deletes it - from every backend that holds a copy - together with the
    /// record of where it lived. The next <see cref="OpenAsync"/> starts an empty database, chosen afresh.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What signing a user out of an offline app calls for. Every backend is cleared, not only the one in use:
    /// a database whose binding is gone is looked for on every backend before it is created anew, so a copy
    /// left elsewhere - by an interrupted migration, say - would otherwise come back on the next open. A backend
    /// that cannot be reached is logged and skipped - unless the database lives there - and the record is then
    /// kept, marked deleted, so a copy it may still hold is never looked for.
    /// </para>
    /// <para>
    /// <see cref="Closing"/> handlers let go first; the database is not reopened. Close other tabs that have it
    /// open first: a backend that cannot delete a file another tab holds - OPFS, a Blazor Hybrid file - throws,
    /// and IndexedDB and Cache Storage would leave that tab working on a database that no longer exists.
    /// </para>
    /// </remarks>
    /// <exception cref="BsqliteStorageUnavailableException">The backend the database lives on cannot be reached.</exception>
    /// <exception cref="NotSupportedException">The session factory is not the one <c>AddBitBsqliteServices</c> registers.</exception>
    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_sessions is not BsqliteSessionFactory factory)
        {
            throw new NotSupportedException(
                $"Deleting a database needs the {nameof(BsqliteSessionFactory)} AddBitBsqliteServices registers, which "
                + "knows every backend; delete it through each provider's Admin instead.");
        }

        return RunClosedAsync(
            (_, ct) => factory.DeleteEverywhereAsync(Name, ct),
            resolve: false,
            reopen: false,
            cancellationToken);
    }

    /// <summary>
    /// Closes the database, runs <paramref name="work"/>, and reopens it - all holding <see cref="_gate"/>, so
    /// no open from elsewhere slips in between.
    /// </summary>
    /// <param name="work">Given the resolution of the session that was closed, or of a resolve made to learn it.</param>
    /// <param name="resolve">
    /// Whether <paramref name="work"/> needs to know where the database lives when it is not open.
    /// </param>
    /// <param name="reopen">
    /// Whether to open afterwards; <see langword="null"/> reopens what was open. When the work fails, what was
    /// open is reopened unless this is <see langword="false"/>.
    /// </param>
    /// <param name="cancellationToken">Stops the work; a database that was open reopens regardless.</param>
    private async Task RunClosedAsync(
        Func<BsqliteStorageResolution?, CancellationToken, Task> work,
        bool resolve,
        bool? reopen,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var reopened = false;
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Decided under the gate, so an open still under way when this was called has finished and is
            // announced and closed like any other. Closing handlers still find the session open: an
            // OpenAsync from one returns it without waiting on the gate.
            var wasOpen = false;
            BsqliteStorageResolution? resolution = null;
            if (_session is { } session)
            {
                wasOpen = true;
                resolution = session.Resolution;
                await RaiseUnderGateAsync(Closing).ConfigureAwait(false);
                _session = null;
                await session.DisposeAsync().ConfigureAwait(false);
            }
            else if (resolve)
            {
                resolution = await ResolveClosedAsync(cancellationToken).ConfigureAwait(false);
            }

            ExceptionDispatchInfo? failure = null;
            _closedForWork.Value = true;
            _holdingGate.Value = true;
            try
            {
                await work(resolution, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                _closedForWork.Value = false;
                _holdingGate.Value = false;
            }

            // Not on the caller's token: a cancelled or failed work must not leave closed a database the
            // application had open - and disposed meanwhile, there is nobody left to open it for.
            var open = failure is null ? reopen ?? wasOpen : wasOpen && reopen != false;
            if (open && !_disposed)
            {
                try
                {
                    _session = await _sessions.OpenAsync(Name, CancellationToken.None).ConfigureAwait(false);
                    reopened = true;
                }
                catch (Exception reopenFailure) when (failure is not null)
                {
                    throw new AggregateException(
                        $"The work on '{Name}' failed, and so did reopening the database afterwards.",
                        failure.SourceException,
                        reopenFailure);
                }
            }

            failure?.Throw();
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
    /// Where the closed database lives, without opening it: the registered session factory resolves it - no
    /// worker, engine, migration or binding. Any other factory is asked the only way it can be, by opening a
    /// session - unannounced, since nobody saw it - and closing it again.
    /// </summary>
    private async Task<BsqliteStorageResolution> ResolveClosedAsync(CancellationToken cancellationToken)
    {
        if (_sessions is BsqliteSessionFactory factory)
        {
            return await factory.ResolveAsync(Name, cancellationToken).ConfigureAwait(false);
        }

        await using var probe = await _sessions.OpenAsync(Name, cancellationToken).ConfigureAwait(false);
        return probe.Resolution;
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

        // Called back from code that runs holding the gate - a Closing handler, WhileClosedAsync's work - the gate
        // is never released while this waits. Not waited for, then: the caller up the stack disposes the session
        // it is closing anyway, and does not reopen one for a disposed database.
        if (_holdingGate.Value)
        {
            return;
        }

        // An open or a WhileClosedAsync under way holds the gate: waited for, the session it leaves is disposed
        // here, rather than opened after disposal and kept - with its worker, or on a device its place among
        // the sessions every other window's admin operations wait for - for the life of the application. The
        // gate itself is not disposed: an open waiting for it is let through, and then refused.
        await _gate.WaitAsync().ConfigureAwait(false);
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

    /// <summary>Raises <paramref name="handlers"/> from code that holds <see cref="_gate"/>.</summary>
    private async Task RaiseUnderGateAsync(Func<Task>? handlers)
    {
        _holdingGate.Value = true;
        try
        {
            await RaiseAsync(handlers).ConfigureAwait(false);
        }
        finally
        {
            _holdingGate.Value = false;
        }
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
