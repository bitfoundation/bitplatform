namespace Bit.Butil;

/// <summary>
/// Object-store schema definition supplied to <see cref="IndexedDb.Open"/>.
/// </summary>
/// <remarks>
/// The schema is applied inside the version-change transaction and is idempotent: a store or index
/// that already matches is left alone, a missing one is created, and an index whose definition has
/// changed is dropped and re-created (IndexedDB has no way to alter one in place). Set
/// <see cref="Drop"/> to remove a store instead.
/// </remarks>
public class IndexedDbStoreSchema
{
    /// <summary>The object store's name. It is the key the schema is matched on, so renaming one creates a second store rather than renaming the first.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The keypath to use as the store's primary key. Null means out-of-line keys.</summary>
    public string? KeyPath { get; set; }

    /// <summary>
    /// Compound primary key - two or more keypaths making up one key. Takes precedence over
    /// <see cref="KeyPath"/> when non-empty.
    /// </summary>
    public string[]? KeyPaths { get; set; }

    /// <summary>True to auto-generate keys (only meaningful when <see cref="KeyPath"/> is null).</summary>
    public bool AutoIncrement { get; set; }

    /// <summary>
    /// True to delete this store during the upgrade, discarding its records. Every other member is
    /// ignored when this is set.
    /// </summary>
    public bool Drop { get; set; }

    /// <summary>Indexes to create alongside the store.</summary>
    public IndexedDbIndexSchema[] Indexes { get; set; } = [];
}
