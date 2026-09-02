namespace Bit.Butil;

/// <summary>Name and version of a database, as reported by the browser.</summary>
public sealed class IndexedDbDatabaseInfo
{
    /// <summary>The database name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The version currently on disk.</summary>
    public int Version { get; set; }

    /// <summary>
    /// Object stores in the database. Populated by <see cref="IndexedDbHandle.GetInfo"/>; empty for
    /// entries from <see cref="IndexedDb.Databases"/>, which can't open the databases it lists.
    /// </summary>
    public string[] StoreNames { get; set; } = [];
}
