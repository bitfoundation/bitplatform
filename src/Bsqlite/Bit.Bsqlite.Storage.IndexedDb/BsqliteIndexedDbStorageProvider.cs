using Bit.Bsqlite.Storage;
using Microsoft.JSInterop;

namespace Bit.Bsqlite.Storage.IndexedDb;

/// <summary>
/// Persists SQLite databases in IndexedDB via <c>IDBBatchAtomicVFS</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the backend that reaches browsers OPFS does not: it needs an async-capable engine
/// (JSPI, or Asyncify where JSPI is missing) and cannot change the page size. Those limits are
/// declared here and enforced generically by the core.
/// </para>
/// <para>
/// Without an <see cref="IJSRuntime"/> the probe reports unavailable rather than throwing, so
/// desktop tests and selection can move on and still explain themselves.
/// </para>
/// </remarks>
public sealed class BsqliteIndexedDbStorageProvider : IBsqliteStorageProvider
{
    /// <summary>The name applications use to select this backend.</summary>
    public const string ProviderName = "indexeddb";

    /// <summary>The VFS name <c>open_v2</c> must ask for after registration.</summary>
    public const string VfsName = "idb-batch-atomic";

    /// <summary>
    /// The VFS module, named the way every Blazor static asset is named - relative to the
    /// document - so an application served under a sub-path still finds it. The worker host
    /// resolves it against the document base before the worker imports it.
    /// </summary>
    public const string VfsModuleUrl = "./_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-vfs.js";

    /// <summary>The admin module, imported on the main thread relative to the document.</summary>
    public const string AdminModuleUrl = "./_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-admin.js";

    private readonly IJSRuntime? _js;

    public BsqliteIndexedDbStorageProvider(IJSRuntime? js = null)
    {
        _js = js;
        Admin = new BsqliteIndexedDbStorageAdmin(js);
    }

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public BsqliteStorageCapabilities Capabilities { get; } = new()
    {
        RequiredBuild = BsqliteEngineBuild.AsyncCapable,
        IsPersistent = true,
        SupportsMultipleConnections = true,

        // False because of how bit-bsqlite-idb-vfs.js registers the VFS, not because IndexedDB could not do it:
        // WebLocksMixin defaults to the 'exclusive' lock policy, under which a read takes the same
        // exclusive Web Lock a write does and readers queue behind each other. Declaring otherwise
        // would promise concurrency the registration does not provide. Passing
        // `lockPolicy: 'shared'` is what would make this true, and is a change to durability-
        // sensitive locking rather than a declaration.
        SupportsConcurrentReads = false,

        SupportsRelaxedDurability = true,
        SupportsMultiDatabaseTransactions = true,
        CanChangePageSize = false,
        SupportedContexts =
            BsqliteExecutionContexts.DedicatedWorker
            | BsqliteExecutionContexts.Window
            | BsqliteExecutionContexts.SharedWorker
            | BsqliteExecutionContexts.ServiceWorker,
    };

    /// <inheritdoc />
    public BsqliteJsModule VfsModule { get; } = new(VfsModuleUrl);

    /// <inheritdoc />
    public IBsqliteStorageAdmin Admin { get; }

    /// <inheritdoc />
    public async ValueTask<BsqliteProbeResult> ProbeAsync(
        CancellationToken cancellationToken = default)
    {
        if (_js is null)
        {
            return BsqliteProbeResult.Unavailable(
                "IndexedDB can only be probed in a browser. This process has no JavaScript runtime.",
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["javascript"] = "false",
                });
        }

        var module = await _js
            .InvokeAsync<IJSObjectReference>("import", cancellationToken, AdminModuleUrl)
            .ConfigureAwait(false);

        try
        {
            var report = await module
                .InvokeAsync<IndexedDbProbeReport>("probe", cancellationToken)
                .ConfigureAwait(false);

            var environment = report.Environment ?? new Dictionary<string, string>(StringComparer.Ordinal);

            if (!report.Available)
            {
                return BsqliteProbeResult.Unavailable(
                    report.Reason ?? "IndexedDB probe failed without a reason.",
                    environment);
            }

            return BsqliteProbeResult.Available(
                report.QuotaBytes,
                report.UsageBytes,
                environment);
        }
        finally
        {
            await module.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class IndexedDbProbeReport
    {
        public bool Available { get; set; }

        public string? Reason { get; set; }

        public long? QuotaBytes { get; set; }

        public long? UsageBytes { get; set; }

        public Dictionary<string, string>? Environment { get; set; }
    }
}
