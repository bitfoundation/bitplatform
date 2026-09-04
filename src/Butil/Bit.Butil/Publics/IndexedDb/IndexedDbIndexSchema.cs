namespace Bit.Butil;

/// <summary>Index schema inside an <see cref="IndexedDbStoreSchema"/>.</summary>
public class IndexedDbIndexSchema
{
    /// <summary>The index's name, unique within its store.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The keypath the index is built over. Ignored when <see cref="KeyPaths"/> is set.</summary>
    public string KeyPath { get; set; } = string.Empty;

    /// <summary>
    /// Compound index - two or more keypaths making up one index key. Takes precedence over
    /// <see cref="KeyPath"/> when non-empty.
    /// </summary>
    public string[]? KeyPaths { get; set; }

    /// <summary>True to reject a second record carrying a key this index has already seen.</summary>
    public bool Unique { get; set; }

    /// <summary>True to index each element of an array-valued keypath separately, rather than the array as one key.</summary>
    public bool MultiEntry { get; set; }

    /// <summary>True to delete this index during the upgrade. Every other member is ignored when set.</summary>
    public bool Drop { get; set; }
}
