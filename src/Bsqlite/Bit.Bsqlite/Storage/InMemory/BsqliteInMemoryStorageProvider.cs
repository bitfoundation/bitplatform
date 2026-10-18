using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Storage.InMemory;

/// <summary>
/// Keeps databases in the engine's own memory. Available everywhere, survives nothing.
/// </summary>
/// <remarks>
/// <para>
/// Useful for tests, for demos, and as a declared last resort when persistence genuinely cannot be
/// had. Because it loses data on reload, selection refuses to fall back to it unless the application
/// opts in explicitly.
/// </para>
/// <para>
/// It is also the reference implementation of the contract: the smallest thing that passes the
/// conformance kit, and the one to read before writing a backend.
/// </para>
/// <para>
/// Its <see cref="Admin"/> is a store of its own, not a window onto the running engine. This
/// backend declares no <see cref="VfsModule"/>, so a database opened on it lives in SQLite's
/// built-in memory VFS inside the worker, where nothing outside the worker can reach it. Import,
/// export, exists, and list therefore describe images handed to the admin API and nothing else - a
/// database written through a connection does not appear here, and migrating away from this
/// backend copies nothing. That is a consequence of the storage being the engine's own heap, and
/// the reason this backend is for tests and demos rather than for data anyone wants to keep.
/// </para>
/// </remarks>
public sealed class BsqliteInMemoryStorageProvider : IBsqliteStorageProvider
{
    /// <summary>The name applications use to select this backend.</summary>
    public const string ProviderName = "in-memory";

    private readonly BsqliteInMemoryStorageAdmin _admin = new();

    /// <summary>Creates the default in-memory backend, or a named instance for migration tests.</summary>
    public BsqliteInMemoryStorageProvider(string? name = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? ProviderName : name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Needs no asynchronous VFS, since memory reads never suspend, which is also why it is the only
    /// backend that can run on the plain synchronous engine build unconditionally.
    /// </remarks>
    public BsqliteStorageCapabilities Capabilities { get; } = new()
    {
        RequiredBuild = BsqliteEngineBuild.Synchronous,
        IsPersistent = false,
        SupportsMultiDatabaseTransactions = true,
        CanChangePageSize = true,

        // Every connection is its own worker with its own heap, so a second connection to the
        // "same" in-memory database is a different, empty database. Claiming otherwise would let a
        // second session silently read nothing where the first wrote everything.
        SupportsMultipleConnections = false,
        SupportsConcurrentReads = false,

        // Durability is meaningless without persistence: there is nothing to flush to.
        SupportsRelaxedDurability = false,

        SupportedContexts = BsqliteExecutionContexts.Window
            | BsqliteExecutionContexts.DedicatedWorker
            | BsqliteExecutionContexts.SharedWorker
            | BsqliteExecutionContexts.ServiceWorker,
    };

    /// <inheritdoc />
    /// <remarks>None: SQLite's built-in memory VFS does the work, so there is no JavaScript to load.</remarks>
    public BsqliteJsModule? VfsModule => null;

    /// <inheritdoc />
    public IBsqliteStorageAdmin Admin => _admin;

    /// <inheritdoc />
    /// <remarks>Always available - there is no browser capability to depend on.</remarks>
    public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(BsqliteProbeResult.Available(
            environment: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["persistent"] = "false",
                ["requiredEngineBuild"] = nameof(BsqliteEngineBuild.Synchronous),
            }));
}
