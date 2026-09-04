namespace Bit.Butil;

/// <summary>Schema of a single object store.</summary>
public sealed class IndexedDbStoreInfo
{
    /// <summary>The object store's name.</summary>
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
