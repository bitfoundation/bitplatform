namespace Bit.Bsqlite.Storage;

/// <summary>
/// What a storage backend can and cannot do. The core enforces every one of these generically, so
/// that adding a backend never requires a change in the core.
/// </summary>
/// <remarks>
/// <para>
/// Declared with init properties rather than a positional record on purpose: this type is part of a
/// semver-stable contract, and a new positional parameter would break every existing provider,
/// whereas a new optional property does not.
/// </para>
/// <para>
/// Defaults are the *least* capable answer, so a provider that omits a property loses a feature
/// rather than making a promise it cannot keep. Only the two facts that cannot be guessed safely -
/// which engine build the VFS needs, and whether data survives a reload - are required.
/// </para>
/// </remarks>
public sealed record BsqliteStorageCapabilities
{
    /// <summary>The engine build this backend's VFS requires.</summary>
    public required BsqliteEngineBuild RequiredBuild { get; init; }

    /// <summary>
    /// Whether data survives a page reload. Guarded because falling back to a non-persistent
    /// backend silently is a data-loss bug, so selection demands a separate opt-in for it.
    /// </summary>
    public required bool IsPersistent { get; init; }

    /// <summary>Whether more than one connection may be open against one database.</summary>
    public bool SupportsMultipleConnections { get; init; }

    /// <summary>Whether readers can proceed concurrently rather than being serialized.</summary>
    public bool SupportsConcurrentReads { get; init; }

    /// <summary>
    /// Whether the backend can offer a durability level below full flush-on-commit. Relaxed
    /// durability is only offered to callers when this is true.
    /// </summary>
    public bool SupportsRelaxedDurability { get; init; }

    /// <summary>
    /// Whether a transaction may span <c>ATTACH</c>ed databases. When false the core rejects
    /// cross-database transactions instead of letting them corrupt silently.
    /// </summary>
    public bool SupportsMultiDatabaseTransactions { get; init; }

    /// <summary>
    /// Whether <c>PRAGMA page_size</c> may be changed. Block-oriented backends fix the page size to
    /// their block size, and the core guards the pragma for any backend answering false.
    /// </summary>
    public bool CanChangePageSize { get; init; }

    /// <summary>
    /// Contexts this backend works in. Defaults to <see cref="BsqliteExecutionContexts.DedicatedWorker"/>,
    /// the only context in which v1 hosts the engine.
    /// </summary>
    public BsqliteExecutionContexts SupportedContexts { get; init; }
        = BsqliteExecutionContexts.DedicatedWorker;
}
