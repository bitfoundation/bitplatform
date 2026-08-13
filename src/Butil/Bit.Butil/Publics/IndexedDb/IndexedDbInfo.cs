namespace Bit.Butil;

/// <summary>Name and version of a database, as reported by the browser.</summary>
public sealed class IndexedDbDatabaseInfo
{
    public string Name { get; set; } = string.Empty;

    public int Version { get; set; }

    /// <summary>
    /// Object stores in the database. Populated by <see cref="IndexedDbHandle.GetInfo"/>; empty for
    /// entries from <see cref="IndexedDb.Databases"/>, which can't open the databases it lists.
    /// </summary>
    public string[] StoreNames { get; set; } = [];
}

/// <summary>Schema of a single object store.</summary>
public sealed class IndexedDbStoreInfo
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The store's keypath, flattened to an array: empty for out-of-line keys, one entry for a
    /// simple keypath, several for a compound one.
    /// </summary>
    public string[] KeyPath { get; set; } = [];

    /// <summary>True when the store generates its own keys.</summary>
    public bool AutoIncrement { get; set; }

    /// <summary>Indexes defined on the store.</summary>
    public string[] IndexNames { get; set; } = [];
}

/// <summary>Schema of a single index.</summary>
public sealed class IndexedDbIndexInfo
{
    public string Name { get; set; } = string.Empty;

    /// <summary>The index's keypath, flattened to an array (several entries for a compound index).</summary>
    public string[] KeyPath { get; set; } = [];

    /// <summary>True when the index rejects duplicate keys.</summary>
    public bool Unique { get; set; }

    /// <summary>True when an array-valued keypath produces one index entry per element.</summary>
    public bool MultiEntry { get; set; }
}

/// <summary>What <see cref="IndexedDb.Open"/> learned while opening the database.</summary>
internal sealed class IndexedDbOpenInfo
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string[] StoreNames { get; set; } = [];
    public int OldVersion { get; set; }
    public int NewVersion { get; set; }
    public bool Upgraded { get; set; }
}
