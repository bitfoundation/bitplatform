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

    /// <inheritdoc />
    public async Task<BsqliteSession> OpenAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

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

        // Only after the open: a failed open must not leave a binding claiming the database exists
        // somewhere it does not.
        await _resolver.CommitBindingAsync(resolution, cancellationToken).ConfigureAwait(false);

        var capabilities = resolution.Provider.Capabilities;
        var connection = new BsqliteConnection(transport, databaseName)
        {
            RuntimeLimits = new BsqliteRuntimeLimits
            {
                SupportsMultiDatabaseTransactions = capabilities.SupportsMultiDatabaseTransactions,
                CanChangePageSize = capabilities.CanChangePageSize,
            },
        };
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return new BsqliteSession(connection, transport, resolution);
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
