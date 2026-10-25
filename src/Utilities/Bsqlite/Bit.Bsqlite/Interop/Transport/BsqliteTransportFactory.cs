using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// The transport factory <c>AddBitBsqliteServices</c> registers: the native transport for a backend whose
/// databases are files the process opens itself, and the worker transport for every other one.
/// </summary>
/// <remarks>
/// The worker factory is built only when a browser backend is opened, so a Blazor Hybrid app, whose
/// databases never touch the web view, does not need its <c>IJSRuntime</c> to be ready.
/// </remarks>
internal sealed class BsqliteTransportFactory(
    Func<IBsqliteTransportFactory> workerTransports,
    BsqliteWorkerTransportDefaults defaults,
    ILogger<BsqliteTransportFactory>? logger = null) : IBsqliteTransportFactory
{
    private readonly ILogger _logger = logger ?? NullLogger<BsqliteTransportFactory>.Instance;
    private IBsqliteTransportFactory? _workerTransports;

    public IBsqliteTransport Create(IBsqliteStorageProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (provider is IBsqliteFileStorageProvider files)
        {
            if (defaults.ExtensionModules.Count > 0)
            {
                _logger.LogWarning(
                    "Storage provider {Provider} runs the native engine in this process, which does not load the "
                    + "JavaScript extension modules registered with AddExtensionModule ({Modules}); SQL that calls "
                    + "the functions they register fails with 'no such function'.",
                    provider.Name,
                    string.Join(", ", defaults.ExtensionModules.Select(module => module.ModuleUrl)));
            }

            return new BsqliteNativeTransport(files, defaults.EnableForeignKeys, defaults.OnOpenSql);
        }

        if (provider.Capabilities.SupportedContexts.HasFlag(BsqliteExecutionContexts.NativeProcess)
            && !provider.Capabilities.SupportedContexts.HasFlag(BsqliteExecutionContexts.DedicatedWorker))
        {
            throw new InvalidOperationException(
                $"Storage provider '{provider.Name}' runs in the native process but does not implement "
                + $"{nameof(IBsqliteFileStorageProvider)}, so there is no file to open its databases from.");
        }

        return (_workerTransports ??= workerTransports()).Create(provider);
    }
}
