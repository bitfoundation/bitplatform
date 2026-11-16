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

    // Under _hostModuleGate. A lock is released through the module it was taken through, so the module outlives
    // the factory's disposal for as long as a lock is held - or being acquired - and goes with the last of them.
    private int _leases;
    private bool _disposed;

    /// <summary>
    /// A Web Lock on the database's name, taken through the host module without a worker: see
    /// <see cref="IBsqliteOpenLock"/>. Taken whatever the database is bound to: every backend this factory serves
    /// runs in the browser, where another tab may be opening it.
    /// </summary>
    async Task<IAsyncDisposable?> IBsqliteOpenLock.AcquireOpenLockAsync(
        string databaseName,
        Func<CancellationToken, ValueTask<IBsqliteStorageProvider?>> boundProvider,
        CancellationToken cancellationToken)
    {
        var import = Lease();
        IJSObjectReference module;
        try
        {
            module = await HostModuleAsync(import, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await EndLeaseAsync().ConfigureAwait(false);
            throw;
        }

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
            await EndLeaseAsync().ConfigureAwait(false);
            throw;
        }

        return new OpenLock(this, module, id);
    }

    /// <summary>
    /// Counts a lock in and hands back the host module's import, started here the first time: imported once for
    /// the factory's locks rather than once per open.
    /// </summary>
    private Task<IJSObjectReference> Lease()
    {
        lock (_hostModuleGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _leases++;

            // Not cancellable itself: it is shared, and one open giving up must not fail another's.
            return _hostModule ??= _js
                .InvokeAsync<IJSObjectReference>("import", CancellationToken.None, _defaults.HostModuleUrl)
                .AsTask();
        }
    }

    /// <summary>The host module, once imported. A failed or cancelled import is not kept, so the next open tries again.</summary>
    private async Task<IJSObjectReference> HostModuleAsync(Task<IJSObjectReference> import, CancellationToken cancellationToken)
    {
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

    /// <summary>Counts a lock out, disposing the host module when it was the last one of a disposed factory.</summary>
    private async ValueTask EndLeaseAsync()
    {
        Task<IJSObjectReference>? import = null;
        lock (_hostModuleGate)
        {
            if (--_leases == 0 && _disposed)
            {
                import = _hostModule;
                _hostModule = null;
            }
        }

        await DisposeModuleAsync(import).ConfigureAwait(false);
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

    /// <summary>
    /// Releases the host module the locks were taken through - once the last lock still held, or still being
    /// acquired, has been released through it.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Task<IJSObjectReference>? import = null;
        lock (_hostModuleGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_leases == 0)
            {
                import = _hostModule;
                _hostModule = null;
            }
        }

        await DisposeModuleAsync(import).ConfigureAwait(false);
    }

    private static async ValueTask DisposeModuleAsync(Task<IJSObjectReference>? import)
    {
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

    private sealed class OpenLock(BsqliteWorkerTransportFactory owner, IJSObjectReference module, int id) : IAsyncDisposable
    {
        private int _released;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
            {
                return;
            }

            try
            {
                await ReleaseOpenLockAsync(module, id).ConfigureAwait(false);
            }
            finally
            {
                await owner.EndLeaseAsync().ConfigureAwait(false);
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
