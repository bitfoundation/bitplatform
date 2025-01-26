using System.Threading.Tasks;
using Microsoft.JSInterop;

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
        => await js.FastInvokeAsync<int>("BitButil.storage.length", storageName);

    /// <summary>
    /// When passed a number n, this method will return the name of the nth key in the storage.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/key">https://developer.mozilla.org/en-US/docs/Web/API/Storage/key</see>
    /// </summary>
    public async Task<string?> GetKey(int index)
        => await js.FastInvokeAsync<string?>("BitButil.storage.key", storageName, index);

    /// <summary>
    /// When passed a key name, will return that key's value.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/getItem">https://developer.mozilla.org/en-US/docs/Web/API/Storage/getItem</see>
    /// </summary>
    public async Task<string?> GetItem(string? key)
        => await js.FastInvokeAsync<string?>("BitButil.storage.getItem", storageName, key);

    /// <summary>
    /// When passed a key name and value, will add that key to the storage, or update that key's value if it already exists.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/setItem">https://developer.mozilla.org/en-US/docs/Web/API/Storage/setItem</see>
    /// </summary>
    public async Task SetItem(string? key, string? value)
        => await js.FastInvokeVoidAsync("BitButil.storage.setItem", storageName, key, value);

    /// <summary>
    /// When passed a key name, will remove that key from the storage.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/removeItem">https://developer.mozilla.org/en-US/docs/Web/API/Storage/removeItem</see>
    /// </summary>
    public async Task RemoveItem(string? key)
        => await js.FastInvokeVoidAsync("BitButil.storage.removeItem", storageName, key);

    /// <summary>
    /// When invoked, will empty all keys out of the storage.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage/clear">https://developer.mozilla.org/en-US/docs/Web/API/Storage/clear</see>
    /// </summary>
    public async Task Clear()
        => await js.FastInvokeVoidAsync("BitButil.storage.clear", storageName);
}
