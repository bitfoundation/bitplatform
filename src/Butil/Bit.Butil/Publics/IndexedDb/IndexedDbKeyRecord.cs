using System.Text.Json;

namespace Bit.Butil;

/// <summary>
/// One record from a key-only cursor walk - the keys without the cost of deserializing the value.
/// </summary>
public sealed class IndexedDbKeyRecord
{
    /// <summary>Store key, or index key when walking an index.</summary>
    public JsonElement Key { get; set; }

    /// <summary>The record's primary key in the object store.</summary>
    public JsonElement PrimaryKey { get; set; }
}
