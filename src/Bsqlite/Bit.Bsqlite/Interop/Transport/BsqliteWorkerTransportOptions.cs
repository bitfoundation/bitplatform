using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// How a <see cref="BsqliteWorkerTransport"/> opens a database in the browser.
/// </summary>
/// <remarks>
/// The transport does not resolve storage itself: selection happens first, and this carries the
/// outcome - which engine build the chosen backend needs, and which VFS module to register. That
/// keeps the worker host ignorant of preference, sticky binding, and fallback.
/// </remarks>
public sealed class BsqliteWorkerTransportOptions
{
    /// <summary>
    /// The ES module that exports <c>createHost</c>. Override only when an application hosts the
    /// worker from a path other than the RCL default.
    /// </summary>
    public string HostModuleUrl { get; init; } = BsqliteWorkerTransport.VersionedHostModuleUrl;

    /// <summary>
    /// The worker script the host should spawn. <see langword="null"/> uses the host module's own
    /// default, which is what production wants.
    /// </summary>
    public string? WorkerUrl { get; init; }

    /// <summary>The engine build the selected backend's VFS requires.</summary>
    public required BsqliteEngineBuild RequiredBuild { get; init; }

    /// <summary>
    /// The selected backend's VFS module, or <see langword="null"/> when the engine's built-in
    /// memory VFS is enough.
    /// </summary>
    public BsqliteJsModule? Vfs { get; init; }

    /// <summary>
    /// Whether the selected backend can honour <c>ATTACH</c>. Forwarded to the worker so a
    /// JavaScript caller cannot bypass the .NET guard.
    /// </summary>
    public bool SupportsMultiDatabaseTransactions { get; init; } = true;

    /// <summary>Whether the selected backend can honour <c>PRAGMA page_size=…</c>.</summary>
    public bool CanChangePageSize { get; init; } = true;

    /// <summary>
    /// Whether to run <c>PRAGMA foreign_keys = ON</c> when the database opens. On by default,
    /// because that is what Microsoft.Data.Sqlite does and what an EF Core model scaffolded on the
    /// server expects: a <c>RESTRICT</c> or <c>CASCADE</c> rule that silently does nothing in the
    /// browser is a data bug, not a performance setting.
    /// </summary>
    public bool EnableForeignKeys { get; init; } = true;

    /// <summary>
    /// Extra statements to run once the database is open, after the provider's own and after
    /// <see cref="EnableForeignKeys"/>: <c>PRAGMA cache_size</c>, <c>PRAGMA synchronous</c>, and
    /// the like. They run outside the SQL guards, so WAL cannot be enabled this way either.
    /// </summary>
    public IReadOnlyList<string> OnOpenSql { get; init; } = [];

    /// <summary>
    /// JavaScript modules that register user-defined functions, collations, or virtual tables on
    /// the open database. Each exports a function - <c>register</c> unless the module says
    /// otherwise - receiving <c>{ module, sqlite3, db, databaseName }</c>, the same handles the
    /// built-in EF function set uses. They load inside the worker, so they must be served from a URL
    /// the worker can import.
    /// </summary>
    public IReadOnlyList<BsqliteJsModule> ExtensionModules { get; init; } = [];
}
