namespace Bit.Butil;

/// <summary>Schema of a single index.</summary>
public sealed class IndexedDbIndexInfo
{
    /// <summary>The index's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The index's keypath, flattened to an array (several entries for a compound index).</summary>
    public string[] KeyPath { get; set; } = [];

    /// <summary>True when the index rejects duplicate keys.</summary>
    public bool Unique { get; set; }

    /// <summary>True when an array-valued keypath produces one index entry per element.</summary>
    public bool MultiEntry { get; set; }
}
