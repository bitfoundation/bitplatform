using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// The Storage interface of the Web Storage API provides access to a particular domain's session or local storage. 
/// It allows, for example, the addition, modification, or deletion of stored data items.
/// <br />
/// More info: <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage">https://developer.mozilla.org/en-US/docs/Web/API/Storage</see>
/// </summary>
public class ButilStorage(IJSRuntime js, string storageName)
{
    /// <summary>
    /// Returns an integer representing the number of data items stored in the Storage object.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/length">https://developer.mozilla.org/en-US/docs/Web/API/Storage/length</see>
    /// </summary>
    public async Task<int> GetLength()
        => await js.Invoke<int>("BitButil.storage.length", storageName);

    /// <summary>
    /// When passed a number n, this method will return the name of the nth key in the storage.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/key">https://developer.mozilla.org/en-US/docs/Web/API/Storage/key</see>
    /// </summary>
    public async Task<string?> GetKey(int index)
        => await js.Invoke<string?>("BitButil.storage.key", storageName, index);

    /// <summary>
    /// True when the storage contains an item with the given key.
    /// </summary>
    public async Task<bool> ContainsKey(string key)
        => await js.Invoke<bool>("BitButil.storage.containsKey", storageName, key);

    /// <summary>
    /// When passed a key name, will return that key's value.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/getItem">https://developer.mozilla.org/en-US/docs/Web/API/Storage/getItem</see>
    /// </summary>
    public async Task<string?> GetItem(string? key)
        => await js.Invoke<string?>("BitButil.storage.getItem", storageName, key);

    /// <summary>
    /// Returns a JSON-deserialized value, or default(<typeparamref name="T"/>) when the key is missing.
    /// </summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public async Task<T?> GetItem<[DynamicallyAccessedMembers(JsonSerialized)] T>(string key, JsonSerializerOptions? options = null)
    {
        var raw = await GetItem(key);
        if (raw is null) return default;

        // Strings round-trip without an extra Deserialize for the common case.
        if (typeof(T) == typeof(string)) return (T?)(object?)raw;

        return JsonSerializer.Deserialize<T>(raw, options);
    }

    /// <summary>
    /// When passed a key name and value, will add that key to the storage, or update that key's value if it already exists.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/setItem">https://developer.mozilla.org/en-US/docs/Web/API/Storage/setItem</see>
    /// </summary>
    public async Task SetItem(string? key, string? value)
        => await js.InvokeVoid("BitButil.storage.setItem", storageName, key, value);

    /// <summary>
    /// JSON-serializes <paramref name="value"/> and stores it under <paramref name="key"/>.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public Task SetItem<[DynamicallyAccessedMembers(JsonSerialized)] T>(string key, T? value, JsonSerializerOptions? options = null)
    {
        if (value is null) return SetItem(key, (string?)null);
        if (value is string s) return SetItem(key, s);
        return SetItem(key, JsonSerializer.Serialize(value, options));
    }

    /// <summary>
    /// When passed a key name, will remove that key from the storage.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/removeItem">https://developer.mozilla.org/en-US/docs/Web/API/Storage/removeItem</see>
    /// </summary>
    public async Task RemoveItem(string? key)
        => await js.InvokeVoid("BitButil.storage.removeItem", storageName, key);

    /// <summary>
    /// When invoked, will empty all keys out of the storage.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/clear">https://developer.mozilla.org/en-US/docs/Web/API/Storage/clear</see>
    /// </summary>
    public async Task Clear()
        => await js.InvokeVoid("BitButil.storage.clear", storageName);

    /// <summary>
    /// Subscribes to cross-tab <c>storage</c> events for this storage area
    /// (<c>localStorage</c> or <c>sessionStorage</c>). The event only fires when another
    /// tab/window of the same origin modifies the matching storage.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/storage_event">window.storage</see>
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StorageEvent))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StorageListenersManager))]
    public async Task<ButilSubscription> SubscribeChanges(Action<StorageEvent> handler)
    {
        var id = StorageListenersManager.AddListener(handler, storageName);
        await js.InvokeVoid("BitButil.storage.subscribe", StorageListenersManager.InvokeMethodName, id);
        return new ButilSubscription(id, async () =>
        {
            StorageListenersManager.RemoveListeners([id]);
            if (OperatingSystem.IsBrowser() is false) return;
            await js.InvokeVoid("BitButil.storage.unsubscribe", id);
        });
    }
}
