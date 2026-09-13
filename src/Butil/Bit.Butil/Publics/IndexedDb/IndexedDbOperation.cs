using System.Diagnostics.CodeAnalysis;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// One write inside an <see cref="IndexedDbHandle.Transact"/> batch. Build these with the static
/// factories rather than setting <see cref="Type"/> by hand.
/// </summary>
public sealed class IndexedDbOperation
{
    /// <summary>Operation discriminator read by the JS side: <c>put</c>, <c>add</c>, <c>delete</c> or <c>clear</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Object store this operation targets.</summary>
    public string Store { get; set; } = string.Empty;

    /// <summary>Value to write, for <c>put</c> and <c>add</c>.</summary>
    public object? Value { get; set; }

    /// <summary>Explicit key, for stores without a keypath.</summary>
    public object? Key { get; set; }

    /// <summary>Key or <see cref="IndexedDbKeyRange"/> to remove, for <c>delete</c>.</summary>
    public object? Query { get; set; }

    /// <summary>Inserts or updates <paramref name="value"/>. Pass <paramref name="key"/> for stores without a keypath.</summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public static IndexedDbOperation Put<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, T value, object? key = null)
        => new() { Type = "put", Store = store, Value = value, Key = key };

    /// <summary>Inserts <paramref name="value"/>, aborting the whole batch if the key already exists.</summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public static IndexedDbOperation Add<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, T value, object? key = null)
        => new() { Type = "add", Store = store, Value = value, Key = key };

    /// <summary>Removes the record(s) matching <paramref name="query"/> (a key or an <see cref="IndexedDbKeyRange"/>).</summary>
    public static IndexedDbOperation Delete(string store, object query)
        => new() { Type = "delete", Store = store, Query = query };

    /// <summary>Empties <paramref name="store"/>.</summary>
    public static IndexedDbOperation Clear(string store)
        => new() { Type = "clear", Store = store };
}
