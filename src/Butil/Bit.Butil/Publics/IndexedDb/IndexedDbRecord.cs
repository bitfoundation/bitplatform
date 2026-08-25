using System.Text.Json;

namespace Bit.Butil;

/// <summary>
/// One record produced by a cursor walk, carrying the keys the cursor saw alongside the value.
/// </summary>
/// <typeparam name="T">Type the stored value deserializes into.</typeparam>
public sealed class IndexedDbRecord<T>
{
    /// <summary>
    /// The key the cursor is positioned on. For a store cursor this equals <see cref="PrimaryKey"/>;
    /// for an index cursor it's the <em>index</em> key.
    /// </summary>
    public JsonElement Key { get; set; }

    /// <summary>The record's primary key in the object store.</summary>
    public JsonElement PrimaryKey { get; set; }

    /// <summary>The stored value.</summary>
    public T? Value { get; set; }
}

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
