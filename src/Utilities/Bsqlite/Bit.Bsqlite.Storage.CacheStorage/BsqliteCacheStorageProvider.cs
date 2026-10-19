using Bit.Bsqlite.Storage;
using Microsoft.JSInterop;

namespace Bit.Bsqlite.Storage.CacheStorage;

/// <summary>
/// Persists SQLite databases in the Cache Storage API as 4096-byte pages.
/// </summary>
/// <remarks>
/// Built against the frozen storage contract with no core changes. Batch-atomic writes are not
/// offered - the Cache API cannot commit several entries as one. A database left behind by besql
/// is imported losslessly on first open.
/// </remarks>
public sealed class BsqliteCacheStorageProvider : IBsqliteStorageProvider
{
    public const string ProviderName = "cache-storage";

    public const string VfsName = "cache-storage";

    /// <summary>
    /// The VFS module, named the way every Blazor static asset is named - relative to the
    /// document - so an application served under a sub-path still finds it. The worker host
    /// resolves it against the document base before the worker imports it.
    /// </summary>
    public const string VfsModuleUrl = "./_content/Bit.Bsqlite.Storage.CacheStorage/bit-bsqlite-cache-register.js";

    /// <summary>The admin module, imported on the main thread relative to the document.</summary>
    public const string AdminModuleUrl = "./_content/Bit.Bsqlite.Storage.CacheStorage/bit-bsqlite-cache-admin.js";

    private readonly IJSRuntime? _js;

    public BsqliteCacheStorageProvider(IJSRuntime? js = null)
    {
        _js = js;
        Admin = new BsqliteCacheStorageAdmin(js);
    }

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public BsqliteStorageCapabilities Capabilities { get; } = new()
    {
        RequiredBuild = BsqliteEngineBuild.AsyncCapable,
        IsPersistent = true,
        SupportsMultipleConnections = true,
        SupportsConcurrentReads = true,
        SupportsRelaxedDurability = false,
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
                "Cache Storage can only be probed in a browser. This process has no JavaScript runtime.",
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
                .InvokeAsync<CacheProbeReport>("probe", cancellationToken)
                .ConfigureAwait(false);

            var environment = report.Environment ?? new Dictionary<string, string>(StringComparer.Ordinal);

            if (!report.Available)
            {
                return BsqliteProbeResult.Unavailable(
                    report.Reason ?? "Cache Storage probe failed without a reason.",
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

    private sealed class CacheProbeReport
    {
        public bool Available { get; set; }

        public string? Reason { get; set; }

        public long? QuotaBytes { get; set; }

        public long? UsageBytes { get; set; }

        public Dictionary<string, string>? Environment { get; set; }
    }
}
