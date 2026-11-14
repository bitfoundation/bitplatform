using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Resolves a storage backend, opens a transport against it, and only then records the binding.
/// </summary>
/// <remarks>
/// <para>
/// This is the composition <c>UseBitBsqlite</c> will call. It lives here rather than in the EF
/// package so raw ADO.NET can open a database without taking a dependency on EF, and so the
/// "commit the binding only after the open succeeds" rule has one implementation.
/// </para>
/// <para>
/// <see cref="BsqliteStorageMigrationMode.AutomaticOnOpen"/> copies to the preferred backend,
/// checks the image, then flips the binding. A failed migration is logged and does not stop the
/// open - an opportunistic move is not a reason to keep users from their data - and the database
/// opens wherever the binding then names: the source after a failed copy, the target when only
/// deleting the source failed.
/// </para>
/// </remarks>
public sealed class BsqliteSessionFactory : IBsqliteSessionFactory
{
    private readonly BsqliteStorageProviderResolver _resolver;
    private readonly IBsqliteTransportFactory _transports;
    private readonly BsqliteStorageSelection _selection;
    private readonly BsqliteStorageMigrator _migrator;
    private readonly IBsqliteStorageBindingStore _bindings;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _beforeFirstOpenGate = new(1, 1);
    private bool _ranBeforeFirstOpen;

    public BsqliteSessionFactory(
        BsqliteStorageProviderResolver resolver,
        IBsqliteTransportFactory transports,
        BsqliteStorageSelection selection,
        IBsqliteStorageBindingStore bindings,
        BsqliteStorageMigrator? migrator = null,
        ILogger<BsqliteSessionFactory>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(transports);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(bindings);

        _resolver = resolver;
        _transports = transports;
        _selection = selection;
        _bindings = bindings;
        _migrator = migrator ?? new BsqliteStorageMigrator();
        _logger = logger ?? NullLogger<BsqliteSessionFactory>.Instance;
    }

    /// <summary>
    /// The steps <c>BsqliteBuilder.BeforeFirstOpen</c> added, bound to this scope's services: run
    /// before the first open, and again on the next one if they fail.
    /// </summary>
    internal Func<CancellationToken, Task>? BeforeFirstOpen { get; init; }

    /// <inheritdoc />
    public async Task<BsqliteSession> OpenAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        if (BeforeFirstOpen is { } beforeFirstOpen && !_ranBeforeFirstOpen)
        {
            await _beforeFirstOpenGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_ranBeforeFirstOpen)
                {
                    await beforeFirstOpen(cancellationToken).ConfigureAwait(false);
                    _ranBeforeFirstOpen = true;
                }
            }
            finally
            {
                _beforeFirstOpenGate.Release();
            }
        }

        // A method of its own: on WebAssembly the runtime prepares the whole of a method before running
        // any of it, the connection type included - and that type needs what the steps above may load.
        return await OpenCoreAsync(databaseName, cancellationToken).ConfigureAwait(false);
    }

    private async Task<BsqliteSession> OpenCoreAsync(string databaseName, CancellationToken cancellationToken)
    {
        var resolution = await _resolver
            .ResolveAsync(databaseName, _selection, cancellationToken)
            .ConfigureAwait(false);

        if (_selection.MigrationMode is BsqliteStorageMigrationMode.AutomaticOnOpen
            && resolution.BetterProviderAvailable is { } better)
        {
            await TryMigrateAsync(databaseName, resolution.Provider, better, cancellationToken)
                .ConfigureAwait(false);

            // Whether it succeeded or not: a migration that failed after the binding moved leaves the
            // database on the target, and opening the source instead would send this session's
            // writes somewhere the next load no longer reads.
            resolution = await _resolver
                .ResolveAsync(databaseName, _selection, cancellationToken)
                .ConfigureAwait(false);
        }

        var transport = _transports.Create(resolution.Provider);

        try
        {
            await transport.OpenAsync(databaseName, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transport.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        BsqliteConnection? connection = null;
        try
        {
            // Only after the open: a failed open must not leave a binding claiming the database exists
            // somewhere it does not.
            await _resolver.CommitBindingAsync(resolution, cancellationToken).ConfigureAwait(false);

            var capabilities = resolution.Provider.Capabilities;
            connection = new BsqliteConnection(transport, databaseName)
            {
                RuntimeLimits = new BsqliteRuntimeLimits
                {
                    SupportsMultiDatabaseTransactions = capabilities.SupportsMultiDatabaseTransactions,
                    CanChangePageSize = capabilities.CanChangePageSize,
                },
            };
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Nothing owns the open transport yet: left open, it would keep the file - and, on a device,
            // its place among the sessions the admin surface waits for - for the life of the process.
            if (connection is not null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }

            await transport.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return new BsqliteSession(connection, transport, resolution);
    }

    /// <summary>
    /// Where <paramref name="databaseName"/> lives - or would be created - without opening it or changing
    /// anything: no worker, no engine, no migration, no binding.
    /// </summary>
    internal async Task<BsqliteStorageResolution> ResolveAsync(string databaseName, CancellationToken cancellationToken)
        => await _resolver.ResolveAsync(databaseName, _selection, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Deletes <paramref name="databaseName"/> from every backend that can be reached, then its binding.
    /// The database must be closed - <see cref="BsqliteDatabase.DeleteAsync"/> sees to that.
    /// </summary>
    /// <remarks>
    /// Every backend, not only the bound one: a copy a failed migration left on another backend would
    /// otherwise be found once the binding is gone - that is how a database whose binding was lost is
    /// recovered - and a database deleted on sign-out would come back with the previous user's rows.
    /// The binding goes last, so a failure part way leaves it naming where the data still is. A backend
    /// that cannot be reached is skipped with a warning, unless it is the one the binding names - and the binding
    /// is then marked deleted rather than removed, so a copy left there is not found again. One that cannot take
    /// the name at all holds no copy and is skipped too - found out before anything is deleted.
    /// </remarks>
    internal async Task DeleteEverywhereAsync(string databaseName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var bound = await _bindings.GetProviderNameAsync(databaseName, cancellationToken).ConfigureAwait(false);

        // Every backend is probed before any is touched, so a database whose own backend cannot be reached is
        // left whole - not stripped of the copies elsewhere first.
        var probes = new List<(IBsqliteStorageProvider Provider, BsqliteProbeResult Probe)>();
        foreach (var provider in _resolver.Providers)
        {
            probes.Add((provider, await _resolver.ProbeHolderAsync(provider, cancellationToken).ConfigureAwait(false)));
        }

        foreach (var (provider, probe) in probes)
        {
            if (!probe.IsAvailable && string.Equals(provider.Name, bound, StringComparison.OrdinalIgnoreCase))
            {
                throw new BsqliteStorageUnavailableException(
                    databaseName,
                    [
                        new BsqliteStorageCandidateOutcome
                        {
                            ProviderName = provider.Name,
                            Status = BsqliteStorageCandidateStatus.Unavailable,
                            Probe = probe,
                            Explanation = "The database lives here, and it cannot be reached to delete it.",
                        },
                    ]);
            }
        }

        // And every backend that can be reached is asked whether it holds the database before any is touched: a
        // backend that cannot take the name at all - the file system refuses 'CON.db' or a ':', which a browser
        // backend keeps - holds no copy to delete, and finding that out part way would leave the delete half done.
        var holders = new List<IBsqliteStorageProvider>();
        foreach (var (provider, probe) in probes)
        {
            if (!probe.IsAvailable)
            {
                continue;
            }

            try
            {
                await provider.Admin.ExistsAsync(databaseName, cancellationToken).ConfigureAwait(false);
            }
            catch (ArgumentException ex) when (!string.Equals(provider.Name, bound, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    ex,
                    "Storage provider {Provider} cannot keep a database named {DatabaseName}, so it holds no copy to delete.",
                    provider.Name,
                    databaseName);
                continue;
            }

            holders.Add(provider);
        }

        foreach (var (provider, probe) in probes)
        {
            if (!probe.IsAvailable)
            {
                _logger.LogWarning(
                    "Storage provider {Provider} is unavailable ({Reason}), so database {DatabaseName} was not "
                    + "deleted from it. Its binding names {Bound}; a copy left there would be found if the "
                    + "binding were lost.",
                    provider.Name,
                    probe.UnavailableReason,
                    databaseName,
                    bound ?? "no backend");
            }
        }

        foreach (var provider in holders)
        {
            await provider.Admin.DeleteAsync(databaseName, cancellationToken).ConfigureAwait(false);
        }

        // A persistent backend that could not be reached may still hold a copy, and a database with no binding is
        // looked for on every backend - so once that backend answered again, the next open would find the deleted
        // database and bring its rows back. The binding is marked deleted instead: the next open starts a new
        // database, whose binding replaces the mark.
        var unreached = probes.Any(entry => !entry.Probe.IsAvailable
            && entry.Provider.Capabilities.IsPersistent
            && _resolver.CanHoldData(entry.Provider));
        if (unreached)
        {
            await _bindings
                .SetProviderNameAsync(databaseName, BsqliteStorageProviderResolver.DeletedBinding, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await _bindings.RemoveAsync(databaseName, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Runs the automatic migration, reporting rather than throwing when it fails. The caller
    /// resolves again either way: the binding the migrator leaves behind - unchanged, or moved when
    /// only the source's deletion failed - is what says where the database now lives.
    /// </summary>
    private async Task TryMigrateAsync(
        string databaseName,
        IBsqliteStorageProvider source,
        IBsqliteStorageProvider target,
        CancellationToken cancellationToken)
    {
        try
        {
            await _migrator
                .MigrateAsync(databaseName, source, target, _bindings, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Automatic migration of database {DatabaseName} from storage provider {Source} to "
                + "{Target} failed; the database opens wherever its binding now points.",
                databaseName,
                source.Name,
                target.Name);
        }
    }
}
