using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// Live handle to an IndexedDB database. Operations are forwarded to JS, which keeps the
/// underlying <c>IDBDatabase</c> alive until <see cref="DisposeAsync"/> closes it.
/// </summary>
public sealed class IndexedDbHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private bool _disposed;

    internal IndexedDbHandle(IJSRuntime js, Guid id) { _js = js; _id = id; }

    /// <summary>Internal handle id (database is keyed by this in JS).</summary>
    public Guid Id => _id;

    /// <summary>Inserts or updates a value. Pass <paramref name="key"/> for stores without a keypath.</summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask Put<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, T value, object? key = null)
        => _js.InvokeVoid("BitButil.indexedDb.put", _id, store, value, key);

    /// <summary>Inserts a new value. Throws on duplicate key.</summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask Add<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, T value, object? key = null)
        => _js.InvokeVoid("BitButil.indexedDb.add", _id, store, value, key);

    /// <summary>Reads a value by key. Returns default when the key isn't present.</summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<T?> Get<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, object key)
        => _js.Invoke<T?>("BitButil.indexedDb.get", _id, store, key);

    /// <summary>Reads a value by key as a <see cref="JsonElement"/> (no static type required).</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<JsonElement> GetRaw(string store, object key)
        => _js.Invoke<JsonElement>("BitButil.indexedDb.get", _id, store, key);

    /// <summary>Reads all values in a store, optionally limited to <paramref name="count"/>.</summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<T[]> GetAll<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, int? count = null)
        => _js.Invoke<T[]>("BitButil.indexedDb.getAll", _id, store, count);

    /// <summary>Lists every key in a store.</summary>
    public ValueTask<JsonElement[]> GetAllKeys(string store, int? count = null)
        => _js.Invoke<JsonElement[]>("BitButil.indexedDb.getAllKeys", _id, store, count);

    /// <summary>Deletes the value with the given key.</summary>
    public ValueTask Delete(string store, object key)
        => _js.InvokeVoid("BitButil.indexedDb.delete", _id, store, key);

    /// <summary>Empties the store.</summary>
    public ValueTask Clear(string store) => _js.InvokeVoid("BitButil.indexedDb.clear", _id, store);

    /// <summary>Counts records in a store.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<int> Count(string store) => _js.Invoke<int>("BitButil.indexedDb.count", _id, store);

    /// <summary>
    /// Reads via an index. The lookup mode is implied by <paramref name="key"/>:
    /// pass a single value for an exact match, or use <see cref="GetAllByIndex{T}"/> for ranged queries.
    /// </summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<T?> GetByIndex<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, string index, object key)
        => _js.Invoke<T?>("BitButil.indexedDb.getByIndex", _id, store, index, key);

    /// <summary>Reads every match for the given index value.</summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<T[]> GetAllByIndex<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, string index, object key, int? count = null)
        => _js.Invoke<T[]>("BitButil.indexedDb.getAllByIndex", _id, store, index, key, count);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.indexedDb.close", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
    }
}
