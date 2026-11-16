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
    : IBsqliteTransportFactory, IBsqliteOpenLock, IAsyncDisposable
{
    // Static: the JavaScript side keeps every held lock of the page in one map, and a factory is made per DI
    // scope, so ids a factory counted on its own would be handed out again by the next scope's - and releasing
    // one would release the other scope's lock.
    private static int _nextLockId;

    private readonly IJSRuntime _js = js ?? throw new ArgumentNullException(nameof(js));
    private readonly BsqliteWorkerTransportDefaults _defaults = defaults ?? new BsqliteWorkerTransportDefaults();
    private readonly Lock _hostModuleGate = new();
    private Task<IJSObjectReference>? _hostModule;

    /// <summary>
    /// A Web Lock on the database's name, taken through the host module without a worker: see
    /// <see cref="IBsqliteOpenLock"/>.
    /// </summary>
    async Task<IAsyncDisposable?> IBsqliteOpenLock.AcquireOpenLockAsync(string databaseName, CancellationToken cancellationToken)
    {
        var module = await HostModuleAsync(cancellationToken).ConfigureAwait(false);
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
            throw;
        }

        return new OpenLock(module, id);
    }

    /// <summary>
    /// The host module, imported once for the factory's locks rather than once per open. A failed or cancelled
    /// import is not kept, so the next open tries again.
    /// </summary>
    private async Task<IJSObjectReference> HostModuleAsync(CancellationToken cancellationToken)
    {
        Task<IJSObjectReference> import;
        lock (_hostModuleGate)
        {
            // Not cancellable itself: it is shared, and one open giving up must not fail another's.
            import = _hostModule ??= _js
                .InvokeAsync<IJSObjectReference>("import", CancellationToken.None, _defaults.HostModuleUrl)
                .AsTask();
        }

        try
        {
            return await import.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch when (import.IsFaulted || import.IsCanceled)
        {
            lock (_hostModuleGate)
            {
                if (_hostModule == import)
                {
                    _hostModule = null;
                }
            }

            throw;
        }
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

    /// <summary>Releases the host module the locks were taken through.</summary>
    public async ValueTask DisposeAsync()
    {
        Task<IJSObjectReference>? import;
        lock (_hostModuleGate)
        {
            import = _hostModule;
            _hostModule = null;
        }

        if (import is null)
        {
            return;
        }

        try
        {
            await (await import.ConfigureAwait(false)).DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is JSException or JSDisconnectedException or ObjectDisposedException or OperationCanceledException)
        {
            // Never imported, or the page is going away and takes the module with it.
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
