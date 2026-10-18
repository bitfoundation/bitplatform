using Bit.Bsqlite.Storage;
using Microsoft.JSInterop;

namespace Bit.Bsqlite.Storage.Opfs;

/// <summary>
/// Persists SQLite databases in the origin's private file system via <c>OPFSCoopSyncVFS</c>.
/// </summary>
/// <remarks>
/// <para>
/// Chosen over AccessHandlePoolVFS because more than one connection may be open and the
/// <c>.db</c> is a real file - export is a read. Access handles are worker-only, so the engine
/// stays in a dedicated worker; probe and admin use the async file API and can run on the window.
/// </para>
/// <para>
/// Without an <see cref="IJSRuntime"/> the probe reports unavailable rather than throwing, so
/// desktop tests and selection can move on and still explain themselves.
/// </para>
/// </remarks>
public sealed class BsqliteOpfsStorageProvider : IBsqliteStorageProvider
{
    /// <summary>The name applications use to select this backend.</summary>
    public const string ProviderName = "opfs";

    /// <summary>The VFS name <c>open_v2</c> must ask for after registration.</summary>
    public const string VfsName = "opfs-coop-sync";

    /// <summary>
    /// The VFS module, named the way every Blazor static asset is named - relative to the
    /// document - so an application served under a sub-path still finds it. The worker host
    /// resolves it against the document base before the worker imports it.
    /// </summary>
    public const string VfsModuleUrl = "./_content/Bit.Bsqlite.Storage.Opfs/bit-bsqlite-opfs-vfs.js";

    /// <summary>The admin module, imported on the main thread relative to the document.</summary>
    public const string AdminModuleUrl = "./_content/Bit.Bsqlite.Storage.Opfs/bit-bsqlite-opfs-admin.js";

    private readonly IJSRuntime? _js;

    public BsqliteOpfsStorageProvider(IJSRuntime? js = null)
    {
        _js = js;
        Admin = new BsqliteOpfsStorageAdmin(js);
    }

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public BsqliteStorageCapabilities Capabilities { get; } = new()
    {
        RequiredBuild = BsqliteEngineBuild.Synchronous,
        IsPersistent = true,
        SupportsMultipleConnections = true,
        SupportsConcurrentReads = false,
        SupportsRelaxedDurability = false,
        SupportsMultiDatabaseTransactions = false,
        CanChangePageSize = true,
        SupportedContexts = BsqliteExecutionContexts.DedicatedWorker,
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
                "OPFS can only be probed in a browser. This process has no JavaScript runtime.",
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
                .InvokeAsync<OpfsProbeReport>("probe", cancellationToken)
                .ConfigureAwait(false);

            var environment = report.Environment ?? new Dictionary<string, string>(StringComparer.Ordinal);

            if (!report.Available)
            {
                return BsqliteProbeResult.Unavailable(
                    report.Reason ?? "OPFS probe failed without a reason.",
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

    private sealed class OpfsProbeReport
    {
        public bool Available { get; set; }

        public string? Reason { get; set; }

        public long? QuotaBytes { get; set; }

        public long? UsageBytes { get; set; }

        public Dictionary<string, string>? Environment { get; set; }
    }
}
