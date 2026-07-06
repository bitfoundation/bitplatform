using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
// DotNetObjectReference.Create demands every public method of this type be preserved for trimming, and
// this type's public surface includes [RequiresUnreferencedCode] JSON APIs (GetItem<T>/SetItem<T>), so
// holding a DotNetObjectReference<ButilStorage> field/property raises IL2026. The interop ref only ever
// dispatches the [JSInvokable] callback, never the JSON generics, and those generics keep their own
// RUC/RDC attributes so a trimming/AOT consumer is still warned at the real call site. Scoped to this type.
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "DotNetObjectReference.Create preserves all public methods; the RUC JSON APIs it pulls in are never invoked through this ref and stay annotated for consumers.")]
public class ButilStorage(IJSRuntime js, string storageName) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeStorageEvent);

    private readonly ConcurrentDictionary<Guid, Action<StorageEvent>> _handlers = new();

    // Per-instance callback reference (see Keyboard): subscriptions are isolated per circuit / WASM
    // app and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<ButilStorage>? _dotNetRef;
    private DotNetObjectReference<ButilStorage> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// Invoked from JS on a cross-tab <c>storage</c> event. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>. Only
    /// events for this instance's storage area are forwarded to the handler.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeStorageEvent(Guid id, StorageEvent evt)
    {
        if (_handlers.TryGetValue(id, out var handler) &&
            (string.IsNullOrEmpty(storageName) || string.Equals(storageName, evt.StorageArea, StringComparison.Ordinal)))
        {
            handler.Invoke(evt);
        }
    }
    /// <summary>
    /// Returns an integer representing the number of data items stored in the Storage object.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/length">https://developer.mozilla.org/en-US/docs/Web/API/Storage/length</see>
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<int> GetLength()
        => await js.InvokeFast<int>("BitButil.storage.length", storageName);

    /// <summary>
    /// When passed a number n, this method will return the name of the nth key in the storage.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/key">https://developer.mozilla.org/en-US/docs/Web/API/Storage/key</see>
    /// </summary>
    public async Task<string?> GetKey(int index)
        => await js.InvokeFast<string?>("BitButil.storage.key", storageName, index);

    /// <summary>
    /// True when the storage contains an item with the given key.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public async Task<bool> ContainsKey(string key)
        => await js.InvokeFast<bool>("BitButil.storage.containsKey", storageName, key);

    /// <summary>
    /// When passed a key name, will return that key's value.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/getItem">https://developer.mozilla.org/en-US/docs/Web/API/Storage/getItem</see>
    /// </summary>
    public async Task<string?> GetItem(string? key)
        => await js.InvokeFast<string?>("BitButil.storage.getItem", storageName, key);

    /// <summary>
    /// Returns a JSON-deserialized value, or default(<typeparamref name="T"/>) when the key is missing.
    /// </summary>
    /// <remarks>
    /// The stored value is expected to be valid JSON for <typeparamref name="T"/> (i.e. written via
    /// <see cref="SetItem{T}(string, T, JsonSerializerOptions)"/>). When <typeparamref name="T"/> is
    /// <see cref="string"/> the raw value is returned as-is. For any other type a value written through
    /// the untyped <see cref="SetItem(string, string)"/> may not be valid JSON (for example the bare
    /// text <c>"123"</c> deserializes fine to <see cref="int"/>, but <c>"abc"</c> does not), in which
    /// case <see cref="JsonException"/> is thrown. Use <see cref="SetItem{T}(string, T, JsonSerializerOptions)"/>
    /// to write values you intend to read back with this overload.
    /// </remarks>
    /// <exception cref="JsonException">The stored value is not valid JSON for <typeparamref name="T"/>.</exception>
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
        => await js.InvokeVoidFast("BitButil.storage.setItem", storageName, key, value);

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
        => await js.InvokeVoidFast("BitButil.storage.removeItem", storageName, key);

    /// <summary>
    /// When invoked, will empty all keys out of the storage.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/clear">https://developer.mozilla.org/en-US/docs/Web/API/Storage/clear</see>
    /// </summary>
    public async Task Clear()
        => await js.InvokeVoidFast("BitButil.storage.clear", storageName);

    /// <summary>
    /// Subscribes to cross-tab <c>storage</c> events for this storage area
    /// (<c>localStorage</c> or <c>sessionStorage</c>). The event only fires when another
    /// tab/window of the same origin modifies the matching storage.
    /// <br />
    /// <b>Note:</b> the DOM <c>storage</c> event only propagates across tabs for
    /// <c>localStorage</c>. <c>sessionStorage</c> is scoped to a single tab/window, so changes to a
    /// <c>sessionStorage</c> area are never observed by other tabs and this subscription will not
    /// receive cross-tab notifications for it.
    /// <br />
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/storage_event">window.storage</see>
    /// </summary>
    [DynamicDependency(nameof(InvokeStorageEvent), typeof(ButilStorage))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StorageEvent))]
    public async Task<ButilSubscription> SubscribeChanges(Action<StorageEvent> handler)
    {
        var id = Guid.NewGuid();
        _handlers.TryAdd(id, handler);
        await js.InvokeVoid("BitButil.storage.subscribe", DotNetRef, id);
        return new ButilSubscription(id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.storage.unsubscribe", id);
        });
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids)
            {
                await js.InvokeVoid("BitButil.storage.unsubscribe", id);
            }
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}
