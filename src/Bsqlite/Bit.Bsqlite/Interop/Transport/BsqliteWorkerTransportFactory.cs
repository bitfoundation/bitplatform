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
    : IBsqliteTransportFactory
{
    private readonly IJSRuntime _js = js ?? throw new ArgumentNullException(nameof(js));
    private readonly BsqliteWorkerTransportDefaults _defaults = defaults ?? new BsqliteWorkerTransportDefaults();

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
