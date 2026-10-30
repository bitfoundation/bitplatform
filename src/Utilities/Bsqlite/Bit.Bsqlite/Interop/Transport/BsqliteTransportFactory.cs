using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// The transport factory <c>AddBitBsqliteServices</c> registers: the native transport for a backend whose
/// databases are files this process opens itself, and the worker transport for every other one.
/// </summary>
/// <remarks>
/// <para>
/// Which one is decided by where .NET runs and where the backend can run - the rule selection uses to decide
/// whether a backend can serve at all: in the browser every backend runs in the worker, since the native engine
/// is not there; where .NET runs natively a backend that runs in the native process gets the native transport,
/// and a browser backend - a database a Blazor Hybrid app kept in its web view before - the worker. Not by
/// whether the backend implements <see cref="IBsqliteFileStorageProvider"/>, which one that also runs in the
/// browser may do.
/// </para>
/// <para>
/// The worker factory is built only when a browser backend is opened, so a Blazor Hybrid app, whose
/// databases never touch the web view, does not need its <c>IJSRuntime</c> to be ready.
/// </para>
/// </remarks>
internal sealed class BsqliteTransportFactory(
    Func<IBsqliteTransportFactory> workerTransports,
    BsqliteWorkerTransportDefaults defaults,
    BsqliteHostKind host,
    ILogger<BsqliteTransportFactory>? logger = null,
    ILogger<BsqliteNativeTransport>? nativeTransportLogger = null) : IBsqliteTransportFactory
{
    private readonly ILogger _logger = logger ?? NullLogger<BsqliteTransportFactory>.Instance;
    private IBsqliteTransportFactory? _workerTransports;

    public IBsqliteTransport Create(IBsqliteStorageProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (host is not BsqliteHostKind.Browser && BsqliteStorageProviderResolver.RunsNatively(provider))
        {
            if (provider is not IBsqliteFileStorageProvider files)
            {
                throw new InvalidOperationException(
                    $"Storage provider '{provider.Name}' runs in the native process but does not implement "
                    + $"{nameof(IBsqliteFileStorageProvider)}, so there is no file to open its databases from.");
            }

            if (defaults.ExtensionModules.Count > 0)
            {
                _logger.LogWarning(
                    "Storage provider {Provider} runs the native engine in this process, which does not load the "
                    + "JavaScript extension modules registered with AddExtensionModule ({Modules}); SQL that calls "
                    + "the functions they register fails with 'no such function'.",
                    provider.Name,
                    string.Join(", ", defaults.ExtensionModules.Select(module => module.ModuleUrl)));
            }

            return new BsqliteNativeTransport(files, defaults.EnableForeignKeys, defaults.OnOpenSql, nativeTransportLogger);
        }

        if (!BsqliteStorageProviderResolver.RunsInBrowser(provider))
        {
            throw new InvalidOperationException(
                $"Storage provider '{provider.Name}' runs in {provider.Capabilities.SupportedContexts}, none of which "
                + (host is BsqliteHostKind.Browser
                    ? "is in the browser, where .NET runs here and the engine is the worker's."
                    : "hosts the engine."));
        }

        return (_workerTransports ??= workerTransports()).Create(provider);
    }
}
