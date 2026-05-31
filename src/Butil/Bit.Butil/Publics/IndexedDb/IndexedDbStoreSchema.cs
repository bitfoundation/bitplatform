namespace Bit.Butil;

/// <summary>
/// Object-store schema definition supplied to <see cref="IndexedDb.Open"/>.
/// </summary>
public class IndexedDbStoreSchema
{
    public string Name { get; set; } = string.Empty;

    /// <summary>The keypath to use as the store's primary key. Null means out-of-line keys.</summary>
    public string? KeyPath { get; set; }

    /// <summary>True to auto-generate keys (only meaningful when <see cref="KeyPath"/> is null).</summary>
    public bool AutoIncrement { get; set; }

    /// <summary>Indexes to create alongside the store.</summary>
    public IndexedDbIndexSchema[] Indexes { get; set; } = [];
}

/// <summary>Index schema inside an <see cref="IndexedDbStoreSchema"/>.</summary>
public class IndexedDbIndexSchema
{
    public string Name { get; set; } = string.Empty;
    public string KeyPath { get; set; } = string.Empty;
    public bool Unique { get; set; }
    public bool MultiEntry { get; set; }
}
