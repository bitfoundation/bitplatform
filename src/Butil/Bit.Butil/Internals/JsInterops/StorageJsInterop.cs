using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class StorageJsInterop
{
    internal static async Task<int> StorageGetLength(this IJSRuntime js, string storageName)
        => await js.InvokeAsync<int>("BitButil.storage.length", storageName);

    internal static async Task<string> StorageGetKey(this IJSRuntime js, string storageName, int index)
        => await js.InvokeAsync<string>("BitButil.storage.key", storageName, index);

    internal static async Task<string> StorageGetItem(this IJSRuntime js, string storageName, string key)
        => await js.InvokeAsync<string>("BitButil.storage.getItem", storageName, key);

    internal static async Task StorageSetItem(this IJSRuntime js, string storageName, string key, string value)
        => await js.InvokeVoidAsync("BitButil.storage.setItem", storageName, key, value);

    internal static async Task StorageRemoveItem(this IJSRuntime js, string storageName, string key)
        => await js.InvokeVoidAsync("BitButil.storage.removeItem", storageName, key);

    internal static async Task StorageClear(this IJSRuntime js, string storageName)
        => await js.InvokeVoidAsync("BitButil.storage.clear", storageName);
}
