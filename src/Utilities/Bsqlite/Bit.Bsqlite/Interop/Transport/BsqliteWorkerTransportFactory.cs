using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;
using Microsoft.JSInterop;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Builds a <see cref="BsqliteWorkerTransport"/> from whatever backend selection chose.
/// </summary>
/// <remarks>
/// The <paramref name="defaults"/> given at construction supply everything that is not decided by
/// the provider: host and worker URLs, open-time pragmas, and extension modules. The provider
/// supplies the engine build, the VFS, and the capability limits.
/// </remarks>
public sealed class BsqliteWorkerTransportFactory(IJSRuntime js, BsqliteWorkerTransportDefaults? defaults = null)
    : IBsqliteTransportFactory, IBsqliteOpenLock
{
    private readonly IJSRuntime _js = js ?? throw new ArgumentNullException(nameof(js));
    private readonly BsqliteWorkerTransportDefaults _defaults = defaults ?? new BsqliteWorkerTransportDefaults();
    private int _nextLockId;

    /// <summary>
    /// A Web Lock on the database's name, taken through the host module without a worker: see
    /// <see cref="IBsqliteOpenLock"/>.
    /// </summary>
    async Task<IAsyncDisposable?> IBsqliteOpenLock.AcquireOpenLockAsync(string databaseName, CancellationToken cancellationToken)
    {
        var module = await _js
            .InvokeAsync<IJSObjectReference>("import", cancellationToken, _defaults.HostModuleUrl)
            .ConfigureAwait(false);
        var id = Interlocked.Increment(ref _nextLockId);
        try
        {
            await module
                .InvokeVoidAsync("acquireOpenLock", cancellationToken, $"bit-bsqlite:{databaseName}:open", id)
                .ConfigureAwait(false);
        }
        catch
        {
            // Given up on here, but the request may still be queued - or granted by now - and a lock nobody
            // releases keeps every other tab from opening the database until this one closes.
            await ReleaseOpenLockAsync(module, id).ConfigureAwait(false);
            await module.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return new OpenLock(module, id);
    }

    private static async ValueTask ReleaseOpenLockAsync(IJSObjectReference module, int id)
    {
        try
        {
            await module.InvokeVoidAsync("releaseOpenLock", id).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is JSException or JSDisconnectedException or ObjectDisposedException)
        {
            // The page is going away, and the browser drops the lock with it.
        }
    }

    private sealed class OpenLock(IJSObjectReference module, int id) : IAsyncDisposable
    {
        private int _released;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
            {
                return;
            }

            await ReleaseOpenLockAsync(module, id).ConfigureAwait(false);
            try
            {
                await module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    /// <inheritdoc />
    public IBsqliteTransport Create(IBsqliteStorageProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return new BsqliteWorkerTransport(_js, new BsqliteWorkerTransportOptions
        {
            HostModuleUrl = _defaults.HostModuleUrl,
            WorkerUrl = _defaults.WorkerUrl,
            RequiredBuild = provider.Capabilities.RequiredBuild,
            Vfs = provider.VfsModule,
            SupportsMultiDatabaseTransactions = provider.Capabilities.SupportsMultiDatabaseTransactions,
            CanChangePageSize = provider.Capabilities.CanChangePageSize,
            EnableForeignKeys = _defaults.EnableForeignKeys,
            OnOpenSql = _defaults.OnOpenSql,
            ExtensionModules = _defaults.ExtensionModules,
        });
    }
}

/// <summary>
/// The parts of <see cref="BsqliteWorkerTransportOptions"/> that do not depend on which storage
/// backend was selected, applied to every transport the factory creates.
/// </summary>
public sealed class BsqliteWorkerTransportDefaults
{
    /// <inheritdoc cref="BsqliteWorkerTransportOptions.HostModuleUrl"/>
    public string HostModuleUrl { get; init; } = BsqliteWorkerTransport.VersionedHostModuleUrl;

    /// <inheritdoc cref="BsqliteWorkerTransportOptions.WorkerUrl"/>
    public string? WorkerUrl { get; init; }

    /// <inheritdoc cref="BsqliteWorkerTransportOptions.EnableForeignKeys"/>
    public bool EnableForeignKeys { get; init; } = true;

    /// <inheritdoc cref="BsqliteWorkerTransportOptions.OnOpenSql"/>
    public IReadOnlyList<string> OnOpenSql { get; init; } = [];

    /// <inheritdoc cref="BsqliteWorkerTransportOptions.ExtensionModules"/>
    public IReadOnlyList<BsqliteJsModule> ExtensionModules { get; init; } = [];
}
