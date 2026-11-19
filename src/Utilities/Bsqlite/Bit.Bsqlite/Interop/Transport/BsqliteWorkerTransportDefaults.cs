namespace Bit.Bsqlite;

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
